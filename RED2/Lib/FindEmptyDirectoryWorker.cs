using FileAttributes = System.IO.FileAttributes;

namespace RED2.Lib
{

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using Properties;

    /// <summary>
    ///     Searches for empty directories
    /// </summary>
    public class FindEmptyDirectoryWorker : BackgroundWorker
    {

        public FindEmptyDirectoryWorker()
        {
            this.WorkerReportsProgress      = true;
            this.WorkerSupportsCancellation = true;
        }

        public RuntimeData Data{get; set;}

        public DeletionErrorEventArgs ErrorInfo{get; set;}

        public int FolderCount{get; private set;}

        public int PossibleEndlessLoop{get; set;}

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            DirectoryInfo startFolder = (DirectoryInfo)e.Argument;

            this.PossibleEndlessLoop = 0;


            // Clean dir list
            this.Data.EmptyFolderList = new List<string>();

            this.ignoreFileList   = this.Data.GetIgnoreFileList();
            this.ignoreFolderList = this.Data.GetIgnoreDirectories();

            try
            {
                var rootStatusType = this.CheckIfDirectoryEmpty(startFolder, 1);

                this.ReportProgress(0, new FoundEmptyDirInfoEventArgs(startFolder.FullName, rootStatusType));

                if (this.PossibleEndlessLoop > this.Data.InfiniteLoopDetectionCount)
                {
                    this.Data.AddLogMessage("Detected possible infinite-loop somewhere in the target path \"" + startFolder + "\" (symbolic links can cause this)");

                    throw new Exception("Possible infinite-loop detected (symbolic links can cause this)");
                }
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                this.Data.AddLogMessage("An error occurred during the scan process: " + ex.Message);
                this.ErrorInfo = new DeletionErrorEventArgs(startFolder.FullName, ex.Message);

                return;
            }

            if (this.CancellationPending)
            {
                this.Data.AddLogMessage("Scan process was cancelled");
                e.Cancel = true;
                e.Result = 0;

                return;
            }

            e.Result = 1;
        }

        private DirectorySearchStatusTypes CheckIfDirectoryEmpty(DirectoryInfo startDir, int depth)
        {
            if (this.PossibleEndlessLoop > this.Data.InfiniteLoopDetectionCount)
            {
                this.ReportProgress(0, new FoundEmptyDirInfoEventArgs(startDir.FullName, DirectorySearchStatusTypes.Error, "Aborted - possible infinite-loop detected"));

                return DirectorySearchStatusTypes.Error;
            }

            try
            {
                // Thread.Sleep(500); -> ?

                if (this.Data.MaxDepth != -1 && depth > this.Data.MaxDepth)
                {
                    return DirectorySearchStatusTypes.NotEmpty;
                }


                // Cancel process if the user hits stop
                if (this.CancellationPending)
                {
                    return DirectorySearchStatusTypes.NotEmpty;
                }

                this.FolderCount++;


                // update status progress bar after 100 steps:
                if (this.FolderCount % 100 == 0)
                {
                    this.ReportProgress(this.FolderCount, "Checking directory: " + startDir.Name);
                }

                var containsFiles = false;


                // Get file list
                FileInfo[] fileList = null;


                // some directories could trigger an exception:
                try
                {
                    fileList = startDir.GetFiles();
                }
                catch
                {
                    fileList = null;
                }

                if (fileList == null)
                {
                    // CF = true = folder does not get deleted:
                    containsFiles = true; // secure way
                    this.Data.AddLogMessage("Failed to access files in \"" + startDir.FullName + "\"");

                    this.ReportProgress(0, new FoundEmptyDirInfoEventArgs(startDir.FullName, DirectorySearchStatusTypes.Error, "Failed to access files"));
                }
                else if (fileList.Length == 0)
                {
                    containsFiles = false;
                }
                else
                {
                    var delPattern = "";


                    // loop trough files and cancel if containsFiles == true
                    for (var f = 0; f < fileList.Length && !containsFiles; f++)
                    {
                        FileInfo file     = null;
                        var      filesize = 0;

                        try
                        {
                            file     = fileList[f];
                            filesize = (int)file.Length;
                        }
                        catch
                        {
                            // keep folder if there is a strange file that
                            // triggers a exception:
                            containsFiles = true;

                            break;
                        }


                        // If only one file is good, then stop.
                        if (!SystemFunctions.MatchesIgnorePattern(file, filesize, this.Data.IgnoreEmptyFiles, this.ignoreFileList, out delPattern))
                        {
                            containsFiles = true;
                        }
                    }
                }


                // If the folder does not contain any files -> get subfolders:
                DirectoryInfo[] subFolderList = null;

                try
                {
                    subFolderList = startDir.GetDirectories();
                }
                catch
                {
                    // If we can not read the folder -> don't delete it:
                    this.Data.AddLogMessage("Failed to access subdirectories in \"" + startDir.FullName + "\"");

                    this.ReportProgress(0, new FoundEmptyDirInfoEventArgs(startDir.FullName, DirectorySearchStatusTypes.Error, "Failed to access subdirectories"));

                    return DirectorySearchStatusTypes.Error;
                }


                // The folder is empty, break here:
                if (!containsFiles && subFolderList.Length == 0)
                {
                    return DirectorySearchStatusTypes.Empty;
                }

                var allSubDirectoriesEmpty = true;

                foreach (var curDir in subFolderList)
                {
                    var attribs = curDir.Attributes;

                    var ignoreSystemDir = this.Data.KeepSystemFolders   && (attribs & FileAttributes.System) == FileAttributes.System;
                    var ignoreHiddenDir = this.Data.IgnoreHiddenFolders && (attribs & FileAttributes.Hidden) == FileAttributes.Hidden;

                    var ignoreSubDirectory = ignoreSystemDir || ignoreHiddenDir;

                    if (!ignoreSubDirectory && this.checkIfDirectoryIsOnIgnoreList(curDir))
                    {
                        this.Data.AddLogMessage("Aborted scan of \"" + curDir.FullName + "\" because it is on the ignore list.");

                        this.ReportProgress(0, new FoundEmptyDirInfoEventArgs(curDir.FullName, DirectorySearchStatusTypes.Ignore));
                        ignoreSubDirectory = true;
                    }

                    if (!ignoreSubDirectory && (attribs & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                    {
                        this.Data.AddLogMessage("Aborted scan of \"" + curDir.FullName + "\" because it is a symbolic link");

                        this.ReportProgress(0, new FoundEmptyDirInfoEventArgs(curDir.FullName, DirectorySearchStatusTypes.Error, "Aborted because dir is a symbolic link"));
                        ignoreSubDirectory = true;
                    }


                    // TODO: Implement more checks
                    //else if ((attribs & FileAttributes.Device) == FileAttributes.Device) msg = "Device - Aborted - found";
                    //else if ((attribs & FileAttributes.Encrypted) == FileAttributes.Encrypted) msg = "Encrypted -  found";
                    // The file will not be indexed by the operating system's content indexing service.
                    // else if ((attribs & FileAttributes.NotContentIndexed) == FileAttributes.NotContentIndexed) msg = "NotContentIndexed - Device found";
                    //else if ((attribs & FileAttributes.Offline) == FileAttributes.Offline) msg = "Offline -  found";
                    //else if ((attribs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) msg = "ReadOnly -  found";
                    //else if ((attribs & FileAttributes.Temporary) == FileAttributes.Temporary) msg = "Temporary -  found";

                    // Scan sub folder:
                    var subFolderStatus = DirectorySearchStatusTypes.NotEmpty;

                    if (!ignoreSubDirectory)
                    {
                        // JRS ADDED check for AGE of folder
                        if (curDir.CreationTime.AddHours(this.Data.MinFolderAgeHours) < DateTime.Now)
                        {
                            subFolderStatus = this.CheckIfDirectoryEmpty(curDir, depth + 1);
                        }
                        else
                        {
                            this.Data.AddLogMessage(string.Format(Resources.young_folder_skipped, curDir.FullName, this.Data.MinFolderAgeHours.ToString(), curDir.CreationTime.ToString()));
                        }


                        // Report status to the GUI
                        if (subFolderStatus == DirectorySearchStatusTypes.Empty)
                        {
                            this.ReportProgress(0, new FoundEmptyDirInfoEventArgs(curDir.FullName, subFolderStatus));
                        }
                    }


                    // this folder is not empty:
                    if (subFolderStatus != DirectorySearchStatusTypes.Empty || ignoreSubDirectory)
                    {
                        allSubDirectoriesEmpty = false;
                    }
                }


                // All subdirectories are empty
                return allSubDirectoriesEmpty && !containsFiles ? DirectorySearchStatusTypes.Empty : DirectorySearchStatusTypes.NotEmpty;
            }
            catch (Exception ex)
            {
                // Error handling

                if (ex is PathTooLongException)
                {
                    this.PossibleEndlessLoop++;
                }

                this.Data.AddLogMessage("An unknown error occurred while trying to scan this directory: \"" + startDir.FullName + "\" - Error message: " + ex.Message);

                this.ReportProgress(0, new FoundEmptyDirInfoEventArgs(startDir.FullName, DirectorySearchStatusTypes.Error, ex.Message));

                return DirectorySearchStatusTypes.Error;
            }
        }

        private bool checkIfDirectoryIsOnIgnoreList(DirectoryInfo folder)
        {
            var ignoreFolder = false;

            if (this.ignoreFolderList.Length > 0)
            {
                foreach (var currentPath in this.ignoreFolderList)
                {
                    if (currentPath == "")
                    {
                        continue;
                    }


                    // skip directory if a part of it is on the filterlist
                    // TODO: Use better compare method
                    if (folder.FullName.ToLower().Contains(currentPath.ToLower()))
                    {
                        ignoreFolder = true;
                    }
                }
            }

            return ignoreFolder;
        }

        private string[] ignoreFileList;

        private string[] ignoreFolderList;

    }

}