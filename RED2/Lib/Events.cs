namespace RED2.Lib
{

    using System;

    public class WorkflowStepChangedEventArgs : EventArgs
    {

        public WorkflowStepChangedEventArgs(WorkflowSteps newStep)
        {
            this.NewStep = newStep;
        }

        public WorkflowSteps NewStep{get; set;}

    }

    public class ErrorEventArgs : EventArgs
    {

        public ErrorEventArgs(string msg)
        {
            this.Message = msg;
        }

        public string Message{get; set;}

    }

    public class FinishedScanForEmptyDirsEventArgs : EventArgs
    {

        public FinishedScanForEmptyDirsEventArgs(int emptyFolderCount, int folderCount)
        {
            this.EmptyFolderCount = emptyFolderCount;
            this.FolderCount      = folderCount;
        }

        public int EmptyFolderCount{get; set;}

        public int FolderCount{get; set;}

    }

    public class DeleteProcessUpdateEventArgs : EventArgs
    {

        public DeleteProcessUpdateEventArgs(int progressStatus, string path, DirectoryDeletionStatusTypes status, int folderCount)
        {
            this.ProgressStatus = progressStatus;
            this.Path           = path;
            this.Status         = status;
            this.FolderCount    = folderCount;
        }

        public int FolderCount{get; set;}

        public string Path{get; set;}

        public int ProgressStatus{get; set;}

        public DirectoryDeletionStatusTypes Status{get; set;}

    }

    public class DeleteProcessFinishedEventArgs : EventArgs
    {

        public DeleteProcessFinishedEventArgs(int deletedFolderCount, int failedFolderCount, int protectedCount)
        {
            this.DeletedFolderCount = deletedFolderCount;
            this.FailedFolderCount  = failedFolderCount;
            this.ProtectedCount     = protectedCount;
        }

        public int DeletedFolderCount{get; set;}

        public int FailedFolderCount{get; set;}

        public int ProtectedCount{get; set;}

    }

    public class ProtectionStatusChangedEventArgs : EventArgs
    {

        public ProtectionStatusChangedEventArgs(string path, bool @protected)
        {
            this.Path      = path;
            this.Protected = @protected;
        }

        public string Path{get; set;}

        public bool Protected{get; set;}

    }

    public class DeleteRequestFromTreeEventArgs : EventArgs
    {

        public DeleteRequestFromTreeEventArgs(string directory)
        {
            this.Directory = directory;
        }

        public string Directory{get; set;}

    }

    public class DeletionErrorEventArgs : EventArgs
    {

        public DeletionErrorEventArgs(string path, string errorMessage)
        {
            this.Path         = path;
            this.ErrorMessage = errorMessage;
        }

        public string ErrorMessage{get; set;}

        public string Path{get; set;}

    }

    public class FoundEmptyDirInfoEventArgs : EventArgs
    {

        public FoundEmptyDirInfoEventArgs(string directory, DirectorySearchStatusTypes type)
        {
            this.Directory    = directory;
            this.Type         = type;
            this.ErrorMessage = "";
        }

        public FoundEmptyDirInfoEventArgs(string directory, DirectorySearchStatusTypes type, string errorMessage)
        {
            this.Directory    = directory;
            this.Type         = type;
            this.ErrorMessage = errorMessage;
        }

        public string Directory{get; set;}

        public string ErrorMessage{get; set;}

        public DirectorySearchStatusTypes Type{get; set;}

    }

}