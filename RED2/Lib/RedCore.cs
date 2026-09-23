namespace RED2.Lib;

/// <summary>RED core class, handles events and communicates with the GUI</summary>
public class RedCore
{

    public RedCore(MainWindow mainWindow, RuntimeData data)
    {
        redMainWindow = mainWindow;
        this.data     = data;
    }

    public event EventHandler OnAborted;

    public event EventHandler OnCancelled;

    public event EventHandler<DeletionErrorEventArgs> OnDeleteError;

    public event EventHandler<DeleteProcessUpdateEventArgs> OnDeleteProcessChanged;

    public event EventHandler<DeleteProcessFinishedEventArgs> OnDeleteProcessFinished;


    // Events
    public event EventHandler<ErrorEventArgs> OnError;

    public event EventHandler<FinishedScanForEmptyDirsEventArgs> OnFinishedScanForEmptyDirs;

    public event EventHandler<FoundEmptyDirInfoEventArgs> OnFoundEmptyDirectory;

    public event EventHandler<ProgressChangedEventArgs> OnProgressChanged;

    public string GetLogMessages()
    {
        return data.LogMessages.ToString();
    }

    /// <summary>Start searching empty folders</summary>
    public void SearchingForEmptyDirectories()
    {
        CurrentProcessStep = WorkflowSteps.StartSearchingForEmptyDirs;


        // Rest folder list
        data.ProtectedFolderList = new Dictionary<string, bool>();


        // Start async empty directory search worker
        searchEmptyFoldersWorker      = new FindEmptyDirectoryWorker();
        searchEmptyFoldersWorker.Data = data;

        searchEmptyFoldersWorker.ProgressChanged += searchEmptyFoldersWorker_ProgressChanged;

        searchEmptyFoldersWorker.RunWorkerCompleted += searchEmptyFoldersWorker_RunWorkerCompleted;

        searchEmptyFoldersWorker.RunWorkerAsync(data.StartFolder);
    }

    public void StartDeleteProcess()
    {
        CurrentProcessStep = WorkflowSteps.DeleteProcessRunning;


        // Kick-off deletion worker to async delete directories
        deletionWorker      = new DeletionWorker();
        deletionWorker.Data = data;

        deletionWorker.ProgressChanged += deletionWorker_ProgressChanged;

        deletionWorker.RunWorkerCompleted += deletionWorker_RunWorkerCompleted;

        deletionWorker.RunWorkerAsync();
    }

    internal void AbortDeletion()
    {
        CurrentProcessStep = WorkflowSteps.Idle;

        deletionWorker.Dispose();
        deletionWorker = null;

        if (OnAborted != null)
        {
            OnAborted(this, new EventArgs());
        }
    }

    internal void AddProtectedFolder(string path)
    {
        if (!data.ProtectedFolderList.ContainsKey(path))
        {
            data.ProtectedFolderList.Add(path, true);
        }
    }

    internal void CancelCurrentProcess()
    {
        if (CurrentProcessStep == WorkflowSteps.StartSearchingForEmptyDirs)
        {
            if (searchEmptyFoldersWorker == null)
            {
                return;
            }

            if (searchEmptyFoldersWorker.IsBusy || !searchEmptyFoldersWorker.CancellationPending)
            {
                searchEmptyFoldersWorker.CancelAsync();
            }
        }
        else if (CurrentProcessStep == WorkflowSteps.DeleteProcessRunning)
        {
            if (deletionWorker == null)
            {
                return;
            }

            if (deletionWorker.IsBusy || !deletionWorker.CancellationPending)
            {
                deletionWorker.CancelAsync();
            }
        }
    }

    internal void ContinueDeleteProcess()
    {
        CurrentProcessStep = WorkflowSteps.DeleteProcessRunning;
        deletionWorker.RunWorkerAsync();
    }

    internal void RemoveProtected(string folderFullName)
    {
        if (data.ProtectedFolderList.ContainsKey(folderFullName))
        {
            data.ProtectedFolderList.Remove(folderFullName);
        }
    }

    private void deletionWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        var state = e.UserState as DeleteProcessUpdateEventArgs;

        if (OnDeleteProcessChanged != null)
        {
            OnDeleteProcessChanged(this, state);
        }
    }

    private void deletionWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        CurrentProcessStep = WorkflowSteps.Idle;

        if (e.Error != null)
        {
            ShowErrorMsg(e.Error.Message);

            deletionWorker.Dispose();
            deletionWorker = null;
        }
        else if (e.Cancelled)
        {
            if (deletionWorker.ErrorInfo != null)
            {
                // A error occurred, process was stopped
                //
                // -> Ask user to continue

                if (OnDeleteError != null)
                {
                    OnDeleteError(this, deletionWorker.ErrorInfo);
                }
                else
                {
                    throw new Exception("Internal error: event handler is missing.");
                }
            }
            else
            {
                // The user cancelled the process
                if (OnCancelled != null)
                {
                    OnCancelled(this, new EventArgs());
                }
            }
        }
        else
        {
            // TODO: Use separate class here?
            var deletedCount   = deletionWorker.DeletedCount;
            var failedCount    = deletionWorker.FailedCount;
            var protectedCount = deletionWorker.ProtectedCount;

            deletionWorker.Dispose();
            deletionWorker = null;

            if (OnDeleteProcessFinished != null)
            {
                OnDeleteProcessFinished(this, new DeleteProcessFinishedEventArgs(deletedCount, failedCount, protectedCount));
            }
        }
    }

    /// <summary>This function gets called on a status update of the find worker</summary>
    private void searchEmptyFoldersWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        if (e.UserState is FoundEmptyDirInfoEventArgs)
        {
            var info = (FoundEmptyDirInfoEventArgs)e.UserState;

            if (info.Type == DirectorySearchStatusTypes.Empty)


                // Found an empty dir, add it to the list
            {
                data.EmptyFolderList.Add(info.Directory);
            }
            else if (info.Type == DirectorySearchStatusTypes.Error && data.HideScanErrors)
            {
                return;
            }

            if (OnFoundEmptyDirectory != null)
            {
                OnFoundEmptyDirectory(this, info);
            }
        }
        else if (e.UserState is string)
        {
            if (OnProgressChanged != null)
            {
                OnProgressChanged(this, new ProgressChangedEventArgs(0, (string)e.UserState));
            }
        }


        // TODO: Handle unknown types
    }

    private void searchEmptyFoldersWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        CurrentProcessStep = WorkflowSteps.Idle;

        if (e.Error != null)
        {
            searchEmptyFoldersWorker.Dispose();
            searchEmptyFoldersWorker = null;

            ShowErrorMsg(e.Error.Message);
        }
        else if (e.Cancelled)
        {
            if (searchEmptyFoldersWorker.ErrorInfo != null)
            {
                // A error occurred, process was stopped
                ShowErrorMsg(searchEmptyFoldersWorker.ErrorInfo.ErrorMessage);

                searchEmptyFoldersWorker.Dispose();
                searchEmptyFoldersWorker = null;

                if (OnAborted != null)
                {
                    OnAborted(this, new EventArgs());
                }
            }
            else
            {
                searchEmptyFoldersWorker.Dispose();
                searchEmptyFoldersWorker = null;

                if (OnCancelled != null)
                {
                    OnCancelled(this, new EventArgs());
                }
            }
        }
        else
        {
            var folderCount = searchEmptyFoldersWorker.FolderCount;

            searchEmptyFoldersWorker.Dispose();
            searchEmptyFoldersWorker = null;

            if (OnFinishedScanForEmptyDirs != null)
            {
                OnFinishedScanForEmptyDirs(this, new FinishedScanForEmptyDirsEventArgs(data.EmptyFolderList.Count, folderCount));
            }
        }
    }

    private void ShowErrorMsg(string errorMessage)
    {
        if (OnError != null)
        {
            OnError(this, new ErrorEventArgs(errorMessage));
        }
    }

    public WorkflowSteps CurrentProcessStep = WorkflowSteps.Idle;

    private readonly RuntimeData data;

    private DeletionWorker deletionWorker;

    private MainWindow redMainWindow;


    // Workers
    private FindEmptyDirectoryWorker searchEmptyFoldersWorker;

}