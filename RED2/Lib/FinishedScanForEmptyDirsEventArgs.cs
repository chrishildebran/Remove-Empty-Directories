namespace RED2.Lib;

public class FinishedScanForEmptyDirsEventArgs : EventArgs
{

    public FinishedScanForEmptyDirsEventArgs(int emptyFolderCount, int folderCount)
    {
        EmptyFolderCount = emptyFolderCount;
        FolderCount      = folderCount;
    }

    public int EmptyFolderCount{get; set;}

    public int FolderCount{get; set;}

}