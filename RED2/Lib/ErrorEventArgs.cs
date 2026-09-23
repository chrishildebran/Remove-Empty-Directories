namespace RED2.Lib;

public class ErrorEventArgs : EventArgs
{

    public ErrorEventArgs(string msg)
    {
        Message = msg;
    }

    public string Message{get; set;}

}