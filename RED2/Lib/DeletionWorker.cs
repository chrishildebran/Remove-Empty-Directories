namespace RED2.Lib;

using System;
using System.ComponentModel;
using System.IO;
using System.Threading;

/// <summary>
///     Deletes the empty directories RED found
/// </summary>
public class DeletionWorker : BackgroundWorker
{

    public DeletionWorker()
    {
        this.WorkerReportsProgress      = true;
        this.WorkerSupportsCancellation = true;

        this.ListPos = 0;
    }

    public RuntimeData Data{get; set;}

    public int DeletedCount{get; set;}

    public DeletionErrorEventArgs ErrorInfo{get; set;}

    public int FailedCount{get; set;}

    public int ListPos{get; set;}

    public int ProtectedCount{get; set;}

    protected override void OnDoWork(DoWorkEventArgs e)
    {
        // This method will run on a thread other than the UI thread.
        // Be sure not to manipulate any Windows Forms controls created
        // on the UI thread from this method.

        if (this.CancellationPending)
        {
            e.Cancel = true;

            return;
        }

        var stopNow      = false;
        var errorMessage = "";
        this.ErrorInfo = null;

        var count = this.Data.EmptyFolderList.Count;

        while (this.ListPos < this.Data.EmptyFolderList.Count)
        {
            if (this.CancellationPending)
            {
                e.Cancel = true;

                return;
            }

            var folder = this.Data.EmptyFolderList[this.ListPos];
            var status = DirectoryDeletionStatusTypes.Ignored;


            // Do not delete one time protected folders
            if (!this.Data.ProtectedFolderList.ContainsKey(folder))
            {
                try
                {
                    // Try to delete the directory
                    this.SecureDelete(folder);

                    this.Data.AddLogMessage($"Successfully deleted dir \"{folder}\"");

                    status = DirectoryDeletionStatusTypes.Deleted;
                    this.DeletedCount++;
                }
                catch (RedPermissionDeniedException ex)
                {
                    errorMessage = ex.Message;

                    this.Data.AddLogMessage($"Directory is protected by the system \"{folder}\" - Message: \"{errorMessage}\"");

                    status = DirectoryDeletionStatusTypes.Protected;
                    this.ProtectedCount++;
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    stopNow      = !this.Data.IgnoreAllErrors;

                    this.Data.AddLogMessage($"Failed to delete dir \"{folder}\" - Error message: \"{errorMessage}\"");

                    status = DirectoryDeletionStatusTypes.Warning;
                    this.FailedCount++;
                }

                if (!stopNow && this.Data.PauseTime > 0)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(this.Data.PauseTime));
                }
            }
            else
            {
                status = DirectoryDeletionStatusTypes.Protected;
            }

            this.ReportProgress(1, new DeleteProcessUpdateEventArgs(this.ListPos, folder, status, count));

            this.ListPos++;

            if (stopNow)
            {
                // stop here for now
                if (errorMessage == "")
                {
                    errorMessage = "Unknown error";
                }

                e.Cancel       = true;
                this.ErrorInfo = new DeletionErrorEventArgs(folder, errorMessage);

                return;
            }
        }

        e.Result = count;
    }

    private void SecureDelete(string path)
    {
        var emptyDirectory = new DirectoryInfo(path);

        if (!emptyDirectory.Exists)
        {
            throw new Exception("Could not delete the directory \"" + emptyDirectory.FullName + "\" because it does not exist anymore.");
        }


        // Cleanup folder

        var ignoreFileList = this.Data.GetIgnoreFileList();

        var files = emptyDirectory.GetFiles();

        if (files != null && files.Length != 0)
        {
            // loop through files and cancel if containsFiles == true

            for (var f = 0; f < files.Length; f++)
            {
                var file = files[f];

                var deleteTrashFile = SystemFunctions.MatchesIgnorePattern(file, (int)file.Length, this.Data.IgnoreEmptyFiles, ignoreFileList, out var delPattern);


                // If only one file is good, then stop.
                if (deleteTrashFile)
                {
                    try
                    {
                        SystemFunctions.SecureDeleteFile(file, this.Data.DeleteMode);

                        this.Data.AddLogMessage($"-> Successfully deleted file \"{file.FullName}\" because it matched the ignore pattern \"{delPattern}\"");
                    }
                    catch (Exception ex)
                    {
                        this.Data.AddLogMessage($"Failed to delete file \"{file.FullName}\" - Error message: \"{ex.Message}\"");

                        var msg = "Could not delete this empty (trash) file:" + Environment.NewLine + file.FullName + Environment.NewLine + Environment.NewLine + "Error message: " + ex.Message;

                        if (ex is RedPermissionDeniedException)
                        {
                            throw new RedPermissionDeniedException(msg, ex);
                        }

                        throw new Exception(msg, ex);
                    }
                }
            }
        }


        // End cleanup

        // This function will ensure that the directory is really empty before it gets deleted
        SystemFunctions.SecureDeleteDirectory(emptyDirectory.FullName, this.Data.DeleteMode);
    }

}