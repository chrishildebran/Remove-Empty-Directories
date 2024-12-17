namespace RED2.Lib;

using System;

public class ProtectionStatusChangedEventArgs : EventArgs
{

    public ProtectionStatusChangedEventArgs(string path, bool @protected)
    {
        this.Path      = path;
        this.Protected = @protected;
    }

    public string Path{get; set;}

    public bool Protected{get; set;}

}