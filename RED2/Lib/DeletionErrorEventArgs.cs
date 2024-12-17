﻿namespace RED2.Lib;

using System;

public class DeletionErrorEventArgs : EventArgs
{

    public DeletionErrorEventArgs(string path, string errorMessage)
    {
        this.Path         = path;
        this.ErrorMessage = errorMessage;
    }

    public string ErrorMessage{get; set;}

    public string Path{get; set;}

}