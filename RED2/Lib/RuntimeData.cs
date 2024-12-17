namespace RED2.Lib;

/// <summary>
///     Container for runtime related data
/// </summary>
public class RuntimeData
{

    public RuntimeData()
    {
        this.LogMessages         = new StringBuilder();
        this.ProtectedFolderList = new Dictionary<string, bool>();
        this.EmptyFolderList     = new List<string>();
    }

    public DeleteModes DeleteMode{get; set;}

    public bool DisableLogging{get; set;}

    /// <summary>
    ///     List containing all empty directories that were found
    /// </summary>
    public List<string> EmptyFolderList{get; set;}

    public bool HideScanErrors{get; set;}

    public bool IgnoreAllErrors{get; set;}

    public string IgnoreDirectoriesList{get; set;}

    public bool IgnoreEmptyFiles{get; set;}

    public string IgnoreFiles{get; set;}

    public bool IgnoreHiddenFolders{get; set;}

    public int InfiniteLoopDetectionCount{get; set;}

    public bool KeepSystemFolders{get; set;}

    public int MaxDepth{get; set;}

    public uint MinFolderAgeHours{get; set;}

    public double PauseTime{get; set;}


    // Configuration

    public DirectoryInfo StartFolder{get; set;}

    public void AddLogMessage(string msg)
    {
        this.LogMessages.AppendLine(DateTime.Now.ToString("r") + "\t" + msg);
    }

    public string[] GetIgnoreDirectories()
    {
        return this.FixNewLines(this.IgnoreDirectoriesList);
    }

    public string[] GetIgnoreFileList()
    {
        return this.FixNewLines(this.IgnoreFiles);
    }

    internal void AddLogSpacer()
    {
        if (this.LogMessages.Length > 0)
        {
            this.LogMessages.Append(Environment.NewLine);
        }
    }

    private string[] FixNewLines(string input)
    {
        return input.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
    }

    public StringBuilder LogMessages;

    public Dictionary<string, bool> ProtectedFolderList = new();

}