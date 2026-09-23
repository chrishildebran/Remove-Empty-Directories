namespace RED2.Lib;

public class DeletionErrorEventArgs : EventArgs
{

    public DeletionErrorEventArgs(string path, string errorMessage)
    {
        Path         = path;
        ErrorMessage = errorMessage;
    }

    public string ErrorMessage{get; set;}

    public string Path{get; set;}

}