namespace RED2.Lib;

using System;

[Serializable]
public class RedPermissionDeniedException : Exception
{

    public RedPermissionDeniedException() { }

    public RedPermissionDeniedException(string message) : base(message) { }

    public RedPermissionDeniedException(string message, Exception inner) : base(message, inner) { }

}