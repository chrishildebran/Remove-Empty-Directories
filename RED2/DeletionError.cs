namespace RED2;

using System;
using System.Windows.Forms;

public partial class DeletionError : Form
{

    public DeletionError()
    {
        this.InitializeComponent();
    }

    internal void SetErrorMessage(string msg)
    {
        this.tbErrorMessage.Text = msg;
    }

    internal void SetPath(string path)
    {
        this.tbPath.Text = path;
    }

    private void DeletionError_Load(object sender, EventArgs e) { }

}