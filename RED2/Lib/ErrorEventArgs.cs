namespace RED2.Lib;

using System;

public class ErrorEventArgs : EventArgs
{

    public ErrorEventArgs(string msg)
    {
        this.Message = msg;
    }

    public string Message{get; set;}

}