namespace RED2.Lib;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

/// <summary>
///     Handles tree related things
///     TODO: Handle null references within tree nodes
/// </summary>
public class TreeManager
{

    public TreeManager(TreeView dirTree, Label fastModeInfoLabel)
    {
        this.treeView            =  dirTree;
        this.treeView.MouseClick += this.tvFolders_MouseClick;

        this.fastModeInfoLabel = fastModeInfoLabel;

        this.ResetTree();

        this.rootPath = "";
    }

    public event EventHandler<DeleteRequestFromTreeEventArgs> OnDeleteRequest;

    public event EventHandler<ProtectionStatusChangedEventArgs> OnProtectionStatusChanged;

    private bool FastMode{get; set;} = true;

    /// <summary>
    ///     Add or update directory tree node
    /// </summary>
    /// <param
    ///     name="path">
    ///     Directory path
    /// </param>
    /// <param
    ///     name="statusType">
    ///     Result status
    /// </param>
    /// <param
    ///     name="optionalErrorMsg">
    ///     Error message (optional)
    /// </param>
    /// <returns></returns>
    public TreeNode AddOrUpdateDirectoryNode(string path, DirectorySearchStatusTypes statusType, string optionalErrorMsg)
    {
        if (this.directoryToTreeNodeMapping.ContainsKey(path))
        {
            // Just update the style if the node already exists
            var node = this.directoryToTreeNodeMapping[path];
            this.ApplyNodeStyle(node, path, statusType, optionalErrorMsg);

            return node;
        }

        var directory = new DirectoryInfo(path);


        // Create new tree node
        var newTreeNode = new TreeNode(directory.Name);

        this.ApplyNodeStyle(newTreeNode, path, statusType, optionalErrorMsg);

        newTreeNode.Tag = directory;

        if (directory.Parent.FullName.Trim('\\').Equals(this.rootPath, StringComparison.OrdinalIgnoreCase))
        {
            this.rootNode.Nodes.Add(newTreeNode);
        }
        else
        {
            var parentNode = this.FindOrCreateDirectoryNodeByPath(directory.Parent.FullName);
            parentNode.Nodes.Add(newTreeNode);
        }

        this.directoryToTreeNodeMapping.Add(path, newTreeNode);

        this.ScrollToNode(newTreeNode);

        return newTreeNode;
    }

    /// <summary>
    ///     Returns the selected folder path
    /// </summary>
    public string GetSelectedFolderPath()
    {
        if (this.treeView.SelectedNode != null && this.treeView.SelectedNode.Tag != null && this.treeView.SelectedNode.Tag is DirectoryInfo)
        {
            return ((DirectoryInfo)this.treeView.SelectedNode.Tag).FullName;
        }

        return "";
    }

    public void OnDeletionProcessFinished()
    {
        this.ShowFastModeResults();
    }

    public void OnDeletionProcessStart()
    {
        if (this.FastMode)
        {
            this.treeView.Nodes.Clear();
            this.SuspendTreeViewForFastMode();
        }
    }

    public void OnProcessCancelled()
    {
        this.ShowFastModeResults();
    }

    public void OnSearchFinished()
    {
        this.ShowFastModeResults();
    }

    public void OnSearchStart(DirectoryInfo directory)
    {
        this.ResetTree();


        // Disable UI updates when fast mode is enabled
        if (this.FastMode)
        {
            this.SuspendTreeViewForFastMode();
        }

        this.CreateRootNode(directory, DirectoryIcons.Home);
    }

    public void SetFastMode(bool fastModeActive)
    {
        this.FastMode = fastModeActive;

        if (this.FastMode)
        {
            this.treeView.SuspendLayout();
        }
        else
        {
            this.ClearFastMode();
            this.treeView.ResumeLayout();
        }
    }

    internal void DeleteSelectedDirectory()
    {
        if (this.treeView.SelectedNode != null && this.treeView.SelectedNode.Tag != null && this.treeView.SelectedNode.Tag is DirectoryInfo)
        {
            var folder = (DirectoryInfo)this.treeView.SelectedNode.Tag;

            if (this.OnDeleteRequest != null)
            {
                this.OnDeleteRequest(this, new DeleteRequestFromTreeEventArgs(folder.FullName));
            }
        }
    }

    internal void ProtectSelected()
    {
        if (this.treeView.SelectedNode != null)
        {
            this.ProtectNode(this.treeView.SelectedNode);
        }
    }

    internal void RemoveNode(string path)
    {
        if (this.nodePropsBackup.ContainsKey(path))
        {
            this.nodePropsBackup.Remove(path);
        }

        if (this.directoryToTreeNodeMapping.ContainsKey(path))
        {
            this.directoryToTreeNodeMapping[path].Remove();
            this.directoryToTreeNodeMapping.Remove(path);
        }
    }

    internal void UnprotectSelected()
    {
        this.UnprotectNode(this.treeView.SelectedNode);
    }

    /// <summary>
    ///     Marks a folder with the warning or deleted icon
    /// </summary>
    /// <param
    ///     name="path">
    ///     Dir path
    /// </param>
    /// <param
    ///     name="iconKey">
    ///     Icon
    /// </param>
    internal void UpdateItemIcon(string path, DirectoryIcons iconKey)
    {
        var treeNode = this.FindOrCreateDirectoryNodeByPath(path);

        treeNode.ImageKey         = iconKey.ToString();
        treeNode.SelectedImageKey = iconKey.ToString();

        this.ScrollToNode(treeNode);
    }

    private void AddRootNode()
    {
        if (this.rootNode == null || (this.treeView.Nodes.Count == 1 && this.treeView.Nodes[0] == this.rootNode))
        {
            return;
        }

        this.treeView.Nodes.Clear();
        this.treeView.Nodes.Add(this.rootNode);
    }

    private void ApplyNodeStyle(TreeNode treeNode, string path, DirectorySearchStatusTypes statusType, string optionalErrorMsg)
    {
        var directory = new DirectoryInfo(path);


        // TODO: use enums for icon names
        treeNode.ForeColor = statusType == DirectorySearchStatusTypes.Empty ? Color.Red : Color.Gray;
        var iconKey = "";

        if (statusType == DirectorySearchStatusTypes.Empty)
        {
            var fileCount     = directory.GetFiles().Length;
            var containsTrash = fileCount > 0;

            iconKey = containsTrash ? "folder_trash_files" : "folder";


            // TODO: use data from scan thread
            if ((directory.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
            {
                iconKey = containsTrash ? "folder_hidden_trash_files" : "folder_hidden";
            }

            if ((directory.Attributes & FileAttributes.Encrypted) == FileAttributes.Encrypted)
            {
                iconKey = containsTrash ? "folder_lock_trash_files" : "folder_lock";
            }

            if ((directory.Attributes & FileAttributes.System) == FileAttributes.System)
            {
                iconKey = containsTrash ? "folder_lock_trash_files" : "folder_lock";
            }

            if (containsTrash && fileCount == 1)
            {
                treeNode.Text += " (contains " + fileCount + " empty file)";
            }
            else if (containsTrash)
            {
                treeNode.Text += " (contains " + fileCount + " empty files)";
            }
        }
        else if (statusType == DirectorySearchStatusTypes.Error)
        {
            iconKey = "folder_warning";

            if (optionalErrorMsg != "")
            {
                optionalErrorMsg = optionalErrorMsg.Replace("\r", "").Replace("\n", "");

                if (optionalErrorMsg.Length > 55)
                {
                    optionalErrorMsg = optionalErrorMsg.Substring(0, 55) + "...";
                }

                treeNode.Text += " (" + optionalErrorMsg + ")";
            }
        }
        else if (statusType == DirectorySearchStatusTypes.Ignore)
        {
            iconKey            = "protected_icon";
            treeNode.ForeColor = Color.Blue;
        }

        if (treeNode != this.rootNode)
        {
            treeNode.ImageKey         = iconKey;
            treeNode.SelectedImageKey = iconKey;
        }
    }

    private void ClearFastMode()
    {
        this.treeView.BackColor        = SystemColors.Window;
        this.fastModeInfoLabel.Visible = false;
    }

    private void CreateRootNode(DirectoryInfo directory, DirectoryIcons imageKey)
    {
        this.rootPath = directory.FullName.Trim('\\');

        this.rootNode                  = new TreeNode(directory.Name);
        this.rootNode.Tag              = directory;
        this.rootNode.ImageKey         = imageKey.ToString();
        this.rootNode.SelectedImageKey = imageKey.ToString();

        this.directoryToTreeNodeMapping = new Dictionary<string, TreeNode>();
        this.directoryToTreeNodeMapping.Add(directory.FullName, this.rootNode);

        if (!this.FastMode)


            // During fast mode the root node will be added after the search finished 
        {
            this.AddRootNode();
        }
    }


    // TODO: Find better code structure for the following two routines
    private TreeNode FindOrCreateDirectoryNodeByPath(string path)
    {
        if (path == null)
        {
            return null;
        }

        if (this.directoryToTreeNodeMapping.ContainsKey(path))
        {
            return this.directoryToTreeNodeMapping[path];
        }

        return this.AddOrUpdateDirectoryNode(path, DirectorySearchStatusTypes.NotEmpty, "");
    }

    private void ProtectNode(TreeNode node)
    {
        var directory = (DirectoryInfo)node.Tag;

        if (this.nodePropsBackup.ContainsKey(directory.FullName))
        {
            return;
        }

        if (this.OnProtectionStatusChanged != null)
        {
            this.OnProtectionStatusChanged(this, new ProtectionStatusChangedEventArgs(directory.FullName, true));
        }


        // Backup node props if the user changes his mind we can restore the node
        // TODO: I'm sure there is a better way to do this, maybe this info can be stored 
        // in the node.Tag or we simply recreate this info like it's a new node.
        this.nodePropsBackup.Add(directory.FullName, node.ImageKey + "|" + node.ForeColor.ToArgb());

        node.ImageKey         = "protected_icon";
        node.SelectedImageKey = "protected_icon";
        node.ForeColor        = Color.Blue;


        // Recursively protect directories
        if (node.Parent != this.rootNode)
        {
            this.ProtectNode(node.Parent);
        }
    }

    private void ResetTree()
    {
        this.rootNode                   = null;
        this.directoryToTreeNodeMapping = new Dictionary<string, TreeNode>();
        this.nodePropsBackup            = new Dictionary<string, object>();

        this.treeView.Nodes.Clear();
    }

    private void ScrollToNode(TreeNode node)
    {
        // Ignore when fast mode is enabled
        if (!this.FastMode)
        {
            node.EnsureVisible();
        }
    }

    private void ShowFastModeResults()
    {
        if (!this.FastMode)
        {
            return;
        }

        this.treeView.ResumeLayout();
        this.ClearFastMode();

        this.AddRootNode();


        // Scroll to root node and expand all dirs
        this.rootNode.EnsureVisible();
        this.treeView.ExpandAll();
    }

    private void SuspendTreeViewForFastMode()
    {
        this.treeView.SuspendLayout();

        this.treeView.BackColor        = SystemColors.Control;
        this.fastModeInfoLabel.Visible = true;
    }

    /// <summary>
    ///     Hack to selected the correct node
    /// </summary>
    private void tvFolders_MouseClick(object sender, MouseEventArgs e)
    {
        this.treeView.SelectedNode = this.treeView.GetNodeAt(e.X, e.Y);
    }

    private void UnprotectNode(TreeNode node)
    {
        if (node != null)
        {
            var directory = (DirectoryInfo)node.Tag;

            if (!this.nodePropsBackup.ContainsKey(directory.FullName))


                // TODO: What to do when this info is missing, show error?
            {
                return;
            }


            // Restore props from backup values
            var propList = ((string)this.nodePropsBackup[directory.FullName]).Split('|');

            this.nodePropsBackup.Remove(directory.FullName);

            node.ImageKey         = propList[0];
            node.SelectedImageKey = propList[0];
            node.ForeColor        = Color.FromArgb(int.Parse(propList[1]));

            if (this.OnProtectionStatusChanged != null)
            {
                this.OnProtectionStatusChanged(this, new ProtectionStatusChangedEventArgs(directory.FullName, false));
            }


            // Unprotect all subnodes
            foreach (TreeNode subNode in node.Nodes)
            {
                this.UnprotectNode(subNode);
            }
        }
    }

    private Dictionary<string, TreeNode> directoryToTreeNodeMapping;

    private readonly Label fastModeInfoLabel;

    /// <summary>
    ///     This dictionary holds the original properties of protected
    ///     nodes so that they can be restored if the user undoes the action
    /// </summary>
    private Dictionary<string, object> nodePropsBackup = new();

    private TreeNode rootNode;

    private string rootPath = "";

    private readonly TreeView treeView;

}