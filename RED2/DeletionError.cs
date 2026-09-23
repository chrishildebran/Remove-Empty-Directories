namespace RED2;

public partial class DeletionError : Form
{

    public DeletionError()
    {
        InitializeComponent();
    }

    internal void SetErrorMessage(string msg)
    {
        tbErrorMessage.Text = msg;
    }

    internal void SetPath(string path)
    {
        tbPath.Text = path;
    }

    private void DeletionError_Load(object sender, EventArgs e) { }

}