namespace RED2.Lib;

using System;

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