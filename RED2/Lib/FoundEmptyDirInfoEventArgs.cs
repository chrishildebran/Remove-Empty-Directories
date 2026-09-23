namespace RED2.Lib;

public class FoundEmptyDirInfoEventArgs : EventArgs
{

    public FoundEmptyDirInfoEventArgs(string directory, DirectorySearchStatusTypes type)
    {
        Directory    = directory;
        Type         = type;
        ErrorMessage = "";
    }

    public FoundEmptyDirInfoEventArgs(string directory, DirectorySearchStatusTypes type, string errorMessage)
    {
        Directory    = directory;
        Type         = type;
        ErrorMessage = errorMessage;
    }

    public string Directory{get; set;}

    public string ErrorMessage{get; set;}

    public DirectorySearchStatusTypes Type{get; set;}

}