namespace RED2.Lib;

public class DeleteRequestFromTreeEventArgs : EventArgs
{

    public DeleteRequestFromTreeEventArgs(string directory)
    {
        this.Directory = directory;
    }

    public string Directory{get; set;}

}