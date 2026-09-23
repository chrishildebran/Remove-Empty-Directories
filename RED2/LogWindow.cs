namespace RED2;

public partial class LogWindow : Form
{

    public LogWindow()
    {
        InitializeComponent();
    }

    public void SetLog(string log)
    {
        tbLog.Text = log;
    }

    private void LogWindow_Load(object sender, EventArgs e) { }

    private void tbLog_DoubleClick(object sender, EventArgs e)
    {
        tbLog.SelectAll();
    }

}