namespace RED2.Lib;

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