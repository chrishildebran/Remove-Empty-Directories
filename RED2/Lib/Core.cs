namespace RED2.Lib
{

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;

    /// <summary>
    ///     RED core class, handles events and communicates with the GUI
    /// </summary>
    public class RedCore
    {

        public RedCore(MainWindow mainWindow, RuntimeData data)
        {
            this.redMainWindow = mainWindow;
            this.data          = data;
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
            return this.data.LogMessages.ToString();
        }

        /// <summary>
        ///     Start searching empty folders
        /// </summary>
        public void SearchingForEmptyDirectories()
        {
            this.CurrentProcessStep = WorkflowSteps.StartSearchingForEmptyDirs;


            // Rest folder list
            this.data.ProtectedFolderList = new Dictionary<string, bool>();


            // Start async empty directory search worker
            this.searchEmptyFoldersWorker      = new FindEmptyDirectoryWorker();
            this.searchEmptyFoldersWorker.Data = this.data;

            this.searchEmptyFoldersWorker.ProgressChanged += this.searchEmptyFoldersWorker_ProgressChanged;

            this.searchEmptyFoldersWorker.RunWorkerCompleted += this.searchEmptyFoldersWorker_RunWorkerCompleted;

            this.searchEmptyFoldersWorker.RunWorkerAsync(this.data.StartFolder);
        }

        public void StartDeleteProcess()
        {
            this.CurrentProcessStep = WorkflowSteps.DeleteProcessRunning;


            // Kick-off deletion worker to async delete directories
            this.deletionWorker      = new DeletionWorker();
            this.deletionWorker.Data = this.data;

            this.deletionWorker.ProgressChanged += this.deletionWorker_ProgressChanged;

            this.deletionWorker.RunWorkerCompleted += this.deletionWorker_RunWorkerCompleted;

            this.deletionWorker.RunWorkerAsync();
        }

        internal void AbortDeletion()
        {
            this.CurrentProcessStep = WorkflowSteps.Idle;

            this.deletionWorker.Dispose();
            this.deletionWorker = null;

            if (this.OnAborted != null)
            {
                this.OnAborted(this, new EventArgs());
            }
        }

        internal void AddProtectedFolder(string path)
        {
            if (!this.data.ProtectedFolderList.ContainsKey(path))
            {
                this.data.ProtectedFolderList.Add(path, true);
            }
        }

        internal void CancelCurrentProcess()
        {
            if (this.CurrentProcessStep == WorkflowSteps.StartSearchingForEmptyDirs)
            {
                if (this.searchEmptyFoldersWorker == null)
                {
                    return;
                }

                if (this.searchEmptyFoldersWorker.IsBusy || this.searchEmptyFoldersWorker.CancellationPending == false)
                {
                    this.searchEmptyFoldersWorker.CancelAsync();
                }
            }
            else if (this.CurrentProcessStep == WorkflowSteps.DeleteProcessRunning)
            {
                if (this.deletionWorker == null)
                {
                    return;
                }

                if (this.deletionWorker.IsBusy || this.deletionWorker.CancellationPending == false)
                {
                    this.deletionWorker.CancelAsync();
                }
            }
        }

        internal void ContinueDeleteProcess()
        {
            this.CurrentProcessStep = WorkflowSteps.DeleteProcessRunning;
            this.deletionWorker.RunWorkerAsync();
        }

        internal void RemoveProtected(string folderFullName)
        {
            if (this.data.ProtectedFolderList.ContainsKey(folderFullName))
            {
                this.data.ProtectedFolderList.Remove(folderFullName);
            }
        }

        private void deletionWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var state = e.UserState as DeleteProcessUpdateEventArgs;

            if (this.OnDeleteProcessChanged != null)
            {
                this.OnDeleteProcessChanged(this, state);
            }
        }

        private void deletionWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.CurrentProcessStep = WorkflowSteps.Idle;

            if (e.Error != null)
            {
                this.ShowErrorMsg(e.Error.Message);

                this.deletionWorker.Dispose();
                this.deletionWorker = null;
            }
            else if (e.Cancelled)
            {
                if (this.deletionWorker.ErrorInfo != null)
                {
                    // A error occurred, process was stopped
                    //
                    // -> Ask user to continue

                    if (this.OnDeleteError != null)
                    {
                        this.OnDeleteError(this, this.deletionWorker.ErrorInfo);
                    }
                    else
                    {
                        throw new Exception("Internal error: event handler is missing.");
                    }
                }
                else
                {
                    // The user cancelled the process
                    if (this.OnCancelled != null)
                    {
                        this.OnCancelled(this, new EventArgs());
                    }
                }
            }
            else
            {
                // TODO: Use separate class here?
                var deletedCount   = this.deletionWorker.DeletedCount;
                var failedCount    = this.deletionWorker.FailedCount;
                var protectedCount = this.deletionWorker.ProtectedCount;

                this.deletionWorker.Dispose();
                this.deletionWorker = null;

                if (this.OnDeleteProcessFinished != null)
                {
                    this.OnDeleteProcessFinished(this, new DeleteProcessFinishedEventArgs(deletedCount, failedCount, protectedCount));
                }
            }
        }

        /// <summary>
        ///     This function gets called on a status update of the find worker
        /// </summary>
        private void searchEmptyFoldersWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState is FoundEmptyDirInfoEventArgs)
            {
                var info = (FoundEmptyDirInfoEventArgs)e.UserState;

                if (info.Type == DirectorySearchStatusTypes.Empty)


                    // Found an empty dir, add it to the list
                {
                    this.data.EmptyFolderList.Add(info.Directory);
                }
                else if (info.Type == DirectorySearchStatusTypes.Error && this.data.HideScanErrors)
                {
                    return;
                }

                if (this.OnFoundEmptyDirectory != null)
                {
                    this.OnFoundEmptyDirectory(this, info);
                }
            }
            else if (e.UserState is string)
            {
                if (this.OnProgressChanged != null)
                {
                    this.OnProgressChanged(this, new ProgressChangedEventArgs(0, (string)e.UserState));
                }
            }


            // TODO: Handle unknown types
        }

        private void searchEmptyFoldersWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.CurrentProcessStep = WorkflowSteps.Idle;

            if (e.Error != null)
            {
                this.searchEmptyFoldersWorker.Dispose();
                this.searchEmptyFoldersWorker = null;

                this.ShowErrorMsg(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                if (this.searchEmptyFoldersWorker.ErrorInfo != null)
                {
                    // A error occurred, process was stopped
                    this.ShowErrorMsg(this.searchEmptyFoldersWorker.ErrorInfo.ErrorMessage);

                    this.searchEmptyFoldersWorker.Dispose();
                    this.searchEmptyFoldersWorker = null;

                    if (this.OnAborted != null)
                    {
                        this.OnAborted(this, new EventArgs());
                    }
                }
                else
                {
                    this.searchEmptyFoldersWorker.Dispose();
                    this.searchEmptyFoldersWorker = null;

                    if (this.OnCancelled != null)
                    {
                        this.OnCancelled(this, new EventArgs());
                    }
                }
            }
            else
            {
                var folderCount = this.searchEmptyFoldersWorker.FolderCount;

                this.searchEmptyFoldersWorker.Dispose();
                this.searchEmptyFoldersWorker = null;

                if (this.OnFinishedScanForEmptyDirs != null)
                {
                    this.OnFinishedScanForEmptyDirs(this, new FinishedScanForEmptyDirsEventArgs(this.data.EmptyFolderList.Count, folderCount));
                }
            }
        }

        private void ShowErrorMsg(string errorMessage)
        {
            if (this.OnError != null)
            {
                this.OnError(this, new ErrorEventArgs(errorMessage));
            }
        }

        public WorkflowSteps CurrentProcessStep = WorkflowSteps.Idle;

        private readonly RuntimeData data;

        private DeletionWorker deletionWorker;

        private MainWindow redMainWindow;


        // Workers
        private FindEmptyDirectoryWorker searchEmptyFoldersWorker;

    }

}