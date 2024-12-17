namespace RED2.Lib;

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