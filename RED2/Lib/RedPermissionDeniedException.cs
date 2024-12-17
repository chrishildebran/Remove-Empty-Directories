namespace RED2.Lib;

[Serializable]
public class RedPermissionDeniedException : Exception
{

    public RedPermissionDeniedException() { }

    public RedPermissionDeniedException(string message) : base(message) { }

    public RedPermissionDeniedException(string message, Exception inner) : base(message, inner) { }

}