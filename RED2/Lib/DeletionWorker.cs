namespace RED2.Lib;

/// <summary>Deletes the empty directories RED found</summary>
public class DeletionWorker : BackgroundWorker
{

    public DeletionWorker()
    {
        WorkerReportsProgress      = true;
        WorkerSupportsCancellation = true;

        ListPos = 0;
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

        if (CancellationPending)
        {
            e.Cancel = true;

            return;
        }

        var stopNow      = false;
        var errorMessage = "";
        ErrorInfo = null;

        var count = Data.EmptyFolderList.Count;

        while (ListPos < Data.EmptyFolderList.Count)
        {
            if (CancellationPending)
            {
                e.Cancel = true;

                return;
            }

            var folder = Data.EmptyFolderList[ListPos];
            var status = DirectoryDeletionStatusTypes.Ignored;


            // Do not delete one time protected folders
            if (!Data.ProtectedFolderList.ContainsKey(folder))
            {
                try
                {
                    // Try to delete the directory
                    SecureDelete(folder);

                    Data.AddLogMessage($"Successfully deleted dir \"{folder}\"");

                    status = DirectoryDeletionStatusTypes.Deleted;
                    DeletedCount++;
                }
                catch (RedPermissionDeniedException ex)
                {
                    errorMessage = ex.Message;

                    Data.AddLogMessage($"Directory is protected by the system \"{folder}\" - Message: \"{errorMessage}\"");

                    status = DirectoryDeletionStatusTypes.Protected;
                    ProtectedCount++;
                }
                catch (Exception ex)
                {
                    errorMessage = ex.Message;
                    stopNow      = !Data.IgnoreAllErrors;

                    Data.AddLogMessage($"Failed to delete dir \"{folder}\" - Error message: \"{errorMessage}\"");

                    status = DirectoryDeletionStatusTypes.Warning;
                    FailedCount++;
                }

                if (!stopNow && Data.PauseTime > 0)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(Data.PauseTime));
                }
            }
            else
            {
                status = DirectoryDeletionStatusTypes.Protected;
            }

            ReportProgress(1, new DeleteProcessUpdateEventArgs(ListPos, folder, status, count));

            ListPos++;

            if (stopNow)
            {
                // stop here for now
                if (errorMessage == "")
                {
                    errorMessage = "Unknown error";
                }

                e.Cancel  = true;
                ErrorInfo = new DeletionErrorEventArgs(folder, errorMessage);

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

        var ignoreFileList = Data.GetIgnoreFileList();

        var files = emptyDirectory.GetFiles();

        if (files != null && files.Length != 0)
        {
            // loop through files and cancel if containsFiles == true

            for (var f = 0; f < files.Length; f++)
            {
                var file = files[f];

                var deleteTrashFile = SystemFunctions.MatchesIgnorePattern(file, (int)file.Length, Data.IgnoreEmptyFiles, ignoreFileList, out var delPattern);


                // If only one file is good, then stop.
                if (deleteTrashFile)
                {
                    try
                    {
                        SystemFunctions.SecureDeleteFile(file, Data.DeleteMode);

                        Data.AddLogMessage($"-> Successfully deleted file \"{file.FullName}\" because it matched the ignore pattern \"{delPattern}\"");
                    }
                    catch (Exception ex)
                    {
                        Data.AddLogMessage($"Failed to delete file \"{file.FullName}\" - Error message: \"{ex.Message}\"");

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
        SystemFunctions.SecureDeleteDirectory(emptyDirectory.FullName, Data.DeleteMode);
    }

}