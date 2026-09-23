namespace RED2.Lib;

public class ProtectionStatusChangedEventArgs : EventArgs
{

    public ProtectionStatusChangedEventArgs(string path, bool @protected)
    {
        Path      = path;
        Protected = @protected;
    }

    public string Path{get; set;}

    public bool Protected{get; set;}

}