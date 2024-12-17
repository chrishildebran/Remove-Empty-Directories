namespace RED2.Lib;

public class ErrorEventArgs : EventArgs
{

    public ErrorEventArgs(string msg)
    {
        this.Message = msg;
    }

    public string Message{get; set;}

}