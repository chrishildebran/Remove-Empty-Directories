namespace RED2.Lib;

using System;

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