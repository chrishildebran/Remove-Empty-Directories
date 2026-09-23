namespace RED2.Lib;

public class DeleteProcessUpdateEventArgs : EventArgs
{

    public DeleteProcessUpdateEventArgs(int progressStatus, string path, DirectoryDeletionStatusTypes status, int folderCount)
    {
        ProgressStatus = progressStatus;
        Path           = path;
        Status         = status;
        FolderCount    = folderCount;
    }

    public int FolderCount{get; set;}

    public string Path{get; set;}

    public int ProgressStatus{get; set;}

    public DirectoryDeletionStatusTypes Status{get; set;}

}