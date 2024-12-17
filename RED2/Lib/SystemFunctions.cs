using FileAccess = System.IO.FileAccess;
using FileMode = System.IO.FileMode;
using FileShare = System.IO.FileShare;

namespace RED2.Lib
{

    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Permissions;
    using System.Text.RegularExpressions;
    using System.Windows.Forms;
    using Microsoft.VisualBasic.FileIO;
    using Microsoft.Win32;
    using Properties;

    public enum DeleteModes
    {

        RecycleBin = 0,

        RecycleBinShowErrors = 1,

        RecycleBinWithQuestion = 2,

        Direct = 3,

        Simulate = 4

    }

    [Serializable]
    public class RedPermissionDeniedException : Exception
    {

        public RedPermissionDeniedException() { }

        public RedPermissionDeniedException(string message) : base(message) { }

        public RedPermissionDeniedException(string message, Exception inner) : base(message, inner) { }

    }

    /// <summary>
    ///     A collection of (generic) system functions
    ///     Exception handling should be made by the caller
    /// </summary>
    public class SystemFunctions
    {

        public static string ChooseDirectoryDialog(string path)
        {
            var folderDialog = new FolderBrowserDialog();

            folderDialog.Description         = Resources.please_select;
            folderDialog.ShowNewFolderButton = false;

            if (path != "")
            {
                DirectoryInfo dir = new DirectoryInfo(path);

                if (dir.Exists)
                {
                    folderDialog.SelectedPath = path;
                }
            }

            if (folderDialog.ShowDialog() == DialogResult.OK)
            {
                path = folderDialog.SelectedPath;
            }

            folderDialog.Dispose();
            folderDialog = null;

            return path;
        }

        public static string ConvertLineBreaks(string str)
        {
            return str.Replace(@"\r\n", "\r\n").Replace(@"\n", "\n");
        }

        public static bool IsDirLocked(string path)
        {
            try
            {
                // UGLY hack to determine whether we have write access
                // to a specific directory

                var r        = new Random();
                var tempName = path + "deltest";

                var counter = 0;

                while (Directory.Exists(tempName))
                {
                    tempName = path + "deltest" + r.Next(0, 9999);

                    if (counter > 100)
                    {
                        return true; // Something strange is going on... stop here...
                    }

                    counter++;
                }

                Directory.Move(path,     tempName);
                Directory.Move(tempName, path);

                return false;
            }
            catch //(Exception ex)
            {
                // Could not rename -> probably we have no 
                // write access to the directory
                return true;
            }
        }

        public static bool IsFileLocked(FileInfo file)
        {
            try
            {
                using (file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    return false;
                }
            }
            catch //(IOException)
            {
                // Could not open file -> probably we have no 
                // write access to the file
                return true;
            }
        }

        /// <summary>
        ///     Check for the registry key
        /// </summary>
        /// <returns></returns>
        public static bool IsRegKeyIntegratedIntoWindowsExplorer()
        {
            return Registry.ClassesRoot.OpenSubKey(registryMenuName) != null;
        }

        public static void ManuallyDeleteDirectory(string path, DeleteModes deleteMode)
        {
            if (deleteMode == DeleteModes.Simulate)
            {
                return;
            }

            if (path == "")
            {
                throw new Exception("Could not delete directory because the path was empty.");
            }


            //TODO: Add FileIOPermission code?

            FileSystem.DeleteDirectory(path, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
        }

        public static bool MatchesIgnorePattern(FileInfo file, int filesize, bool ignore0KbFiles, string[] ignoreFileList, out string delPattern)
        {
            var   matchesPattern = false;
            Regex regexPattern    = null;
            delPattern = "";

            for (var pos = 0; pos < ignoreFileList.Length && !matchesPattern; pos++)
            {
                var pattern = ignoreFileList[pos];


                // TODO: Check patterns for errors

                // Skip empty patterns
                if (pattern == "")
                {
                    continue;
                }

                if (ignore0KbFiles && filesize == 0)
                {
                    delPattern      = "[Empty file]";
                    matchesPattern = true;
                }
                else if (pattern.ToLower() == file.Name.ToLower())
                {
                    // Direct match - ignore case
                    delPattern      = pattern;
                    matchesPattern = true;
                }
                else if (pattern.Contains("*") || (pattern.StartsWith("/") && pattern.EndsWith("/")))
                {
                    // Pattern is a regex
                    if (pattern.StartsWith("/") && pattern.EndsWith("/"))
                    {
                        regexPattern = new Regex(pattern.Substring(1, pattern.Length - 2));
                    }
                    else
                    {
                        pattern      = Regex.Escape(pattern).Replace("\\*", ".*");
                        regexPattern = new Regex("^" + pattern + "$");
                    }

                    if (regexPattern.IsMatch(file.Name))
                    {
                        delPattern      = pattern;
                        matchesPattern = true;
                    }
                }
            }

            return matchesPattern;
        }

        /// <summary>
        ///     Opens a folder
        /// </summary>
        public static void OpenDirectoryWithExplorer(string path)
        {
            if (path == "")
            {
                return;
            }

            var windowsFolder = Environment.GetEnvironmentVariable("SystemRoot");

            Process.Start(windowsFolder + "\\explorer.exe", "/e,\"" + path + "\"");
        }

        public static void SecureDeleteDirectory(string path, DeleteModes deleteMode)
        {
            if (deleteMode == DeleteModes.Simulate)
            {
                return;
            }

            if (deleteMode == DeleteModes.Direct)
            {
                Directory.Delete(path, recursive: false, ignoreReadOnly: true); //throws IOException if not empty anymore

                return;
            }


            // Last security check before deletion
            if (Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0)
            {
                if (deleteMode == DeleteModes.RecycleBin)
                {
                    // Check CLR permissions -> could raise a exception
                    new FileIOPermission(FileIOPermissionAccess.Write, path + Path.DirectorySeparatorChar.ToString()).Demand();


                    //if (!CheckWriteAccess(Directory.GetAccessControl(path)))
                    if (IsDirLocked(path))
                    {
                        throw new RedPermissionDeniedException("Could not delete directory \"" + path + "\" because the access is protected by the (file) system (permission denied).");
                    }

                    FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
                }
                else if (deleteMode == DeleteModes.RecycleBinShowErrors)
                {
                    FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
                }
                else if (deleteMode == DeleteModes.RecycleBinWithQuestion)
                {
                    FileSystem.DeleteDirectory(path, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
                }
                else
                {
                    throw new Exception("Internal error: Unknown delete mode: \"" + deleteMode + "\"");
                }
            }
            else
            {
                throw new Exception("Aborted deletion of the directory \"" + path + "\" because it is no longer empty. This can happen if RED previously failed to delete a empty (trash) file.");
            }
        }

        public static void SecureDeleteFile(FileInfo file, DeleteModes deleteMode)
        {
            if (deleteMode == DeleteModes.Simulate)
            {
                return;
            }

            if (deleteMode == DeleteModes.RecycleBin)
            {
                // Check CLR permissions -> could raise a exception
                new FileIOPermission(FileIOPermissionAccess.Write, file.FullName).Demand();

                if (IsFileLocked(file))
                {
                    throw new RedPermissionDeniedException("Could not delete file \"" + file.FullName + "\" because the access is protected by the (file) system (permission denied).");
                }

                FileSystem.DeleteFile(file.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
            }
            else if (deleteMode == DeleteModes.RecycleBinShowErrors)
            {
                FileSystem.DeleteFile(file.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
            }
            else if (deleteMode == DeleteModes.RecycleBinWithQuestion)
            {
                FileSystem.DeleteFile(file.FullName, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
            }
            else if (deleteMode == DeleteModes.Direct)
            {
                // Was used for testing the error handling:
                // if (SystemFunctions.random.NextDouble() > 0.5) throw new Exception("Test error");
                file.Delete(ignoreReadOnly: true);
            }
            else
            {
                throw new Exception("Internal error: Unknown delete mode: \"" + deleteMode + "\"");
            }
        }

        internal static void AddOrRemoveRegKey(bool add)
        {
            RegistryKey regmenu = null;
            RegistryKey regcmd  = null;

            if (add)
            {
                try
                {
                    regmenu = Registry.ClassesRoot.CreateSubKey(registryMenuName);

                    if (regmenu != null)
                    {
                        regmenu.SetValue("", "Remove empty dirs");
                    }

                    regcmd = Registry.ClassesRoot.CreateSubKey(registryCommand);

                    if (regcmd != null)
                    {
                        regcmd.SetValue("", Application.ExecutablePath + " \"%1\"");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                finally
                {
                    if (regmenu != null)
                    {
                        regmenu.Close();
                    }

                    if (regcmd != null)
                    {
                        regcmd.Close();
                    }
                }
            }
            else
            {
                try
                {
                    var reg = Registry.ClassesRoot.OpenSubKey(registryCommand);

                    if (reg != null)
                    {
                        reg.Close();
                        Registry.ClassesRoot.DeleteSubKey(registryCommand);
                    }

                    reg = Registry.ClassesRoot.OpenSubKey(registryMenuName);

                    if (reg != null)
                    {
                        reg.Close();
                        Registry.ClassesRoot.DeleteSubKey(registryMenuName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(Resources.error + "\nCould not change registry settings: " + ex);
                }
            }
        }

        private const string registryCommand = "Folder\\shell\\Remove empty dirs\\command";


        // Registry keys
        private const string registryMenuName = "Folder\\shell\\Remove empty dirs";

    }

}