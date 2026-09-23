namespace RED2.Lib;

/// <summary>Handles tree related things TODO: Handle null references within tree nodes</summary>
public class TreeManager
{

    public TreeManager(TreeView dirTree, Label fastModeInfoLabel)
    {
        treeView            =  dirTree;
        treeView.MouseClick += tvFolders_MouseClick;

        this.fastModeInfoLabel = fastModeInfoLabel;

        ResetTree();

        rootPath = "";
    }

    public event EventHandler<DeleteRequestFromTreeEventArgs> OnDeleteRequest;

    public event EventHandler<ProtectionStatusChangedEventArgs> OnProtectionStatusChanged;

    private bool FastMode{get; set;} = true;

    /// <summary>Add or update directory tree node</summary>
    /// <param name = "path">Directory path</param>
    /// <param name = "statusType">Result status</param>
    /// <param name = "optionalErrorMsg">Error message (optional)</param>
    /// <returns></returns>
    public TreeNode AddOrUpdateDirectoryNode(string path, DirectorySearchStatusTypes statusType, string optionalErrorMsg)
    {
        if (directoryToTreeNodeMapping.ContainsKey(path))
        {
            // Just update the style if the node already exists
            var node = directoryToTreeNodeMapping[path];
            ApplyNodeStyle(node, path, statusType, optionalErrorMsg);

            return node;
        }

        var directory = new DirectoryInfo(path);


        // Create new tree node
        var newTreeNode = new TreeNode(directory.Name);

        ApplyNodeStyle(newTreeNode, path, statusType, optionalErrorMsg);

        newTreeNode.Tag = directory;

        if (directory.Parent.FullName.Trim('\\').Equals(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            rootNode.Nodes.Add(newTreeNode);
        }
        else
        {
            var parentNode = FindOrCreateDirectoryNodeByPath(directory.Parent.FullName);
            parentNode.Nodes.Add(newTreeNode);
        }

        directoryToTreeNodeMapping.Add(path, newTreeNode);

        ScrollToNode(newTreeNode);

        return newTreeNode;
    }

    /// <summary>Returns the selected folder path</summary>
    public string GetSelectedFolderPath()
    {
        if (treeView.SelectedNode != null && treeView.SelectedNode.Tag != null && treeView.SelectedNode.Tag is DirectoryInfo)
        {
            return ((DirectoryInfo)treeView.SelectedNode.Tag).FullName;
        }

        return "";
    }

    public void OnDeletionProcessFinished()
    {
        ShowFastModeResults();
    }

    public void OnDeletionProcessStart()
    {
        if (FastMode)
        {
            treeView.Nodes.Clear();
            SuspendTreeViewForFastMode();
        }
    }

    public void OnProcessCancelled()
    {
        ShowFastModeResults();
    }

    public void OnSearchFinished()
    {
        ShowFastModeResults();
    }

    public void OnSearchStart(DirectoryInfo directory)
    {
        ResetTree();


        // Disable UI updates when fast mode is enabled
        if (FastMode)
        {
            SuspendTreeViewForFastMode();
        }

        CreateRootNode(directory, DirectoryIcons.Home);
    }

    public void SetFastMode(bool fastModeActive)
    {
        FastMode = fastModeActive;

        if (FastMode)
        {
            treeView.SuspendLayout();
        }
        else
        {
            ClearFastMode();
            treeView.ResumeLayout();
        }
    }

    internal void DeleteSelectedDirectory()
    {
        if (treeView.SelectedNode != null && treeView.SelectedNode.Tag != null && treeView.SelectedNode.Tag is DirectoryInfo)
        {
            var folder = (DirectoryInfo)treeView.SelectedNode.Tag;

            if (OnDeleteRequest != null)
            {
                OnDeleteRequest(this, new DeleteRequestFromTreeEventArgs(folder.FullName));
            }
        }
    }

    internal void ProtectSelected()
    {
        if (treeView.SelectedNode != null)
        {
            ProtectNode(treeView.SelectedNode);
        }
    }

    internal void RemoveNode(string path)
    {
        if (nodePropsBackup.ContainsKey(path))
        {
            nodePropsBackup.Remove(path);
        }

        if (directoryToTreeNodeMapping.ContainsKey(path))
        {
            directoryToTreeNodeMapping[path].Remove();
            directoryToTreeNodeMapping.Remove(path);
        }
    }

    internal void UnprotectSelected()
    {
        UnprotectNode(treeView.SelectedNode);
    }

    /// <summary>Marks a folder with the warning or deleted icon</summary>
    /// <param name = "path">Dir path</param>
    /// <param name = "iconKey">Icon</param>
    internal void UpdateItemIcon(string path, DirectoryIcons iconKey)
    {
        var treeNode = FindOrCreateDirectoryNodeByPath(path);

        treeNode.ImageKey         = iconKey.ToString();
        treeNode.SelectedImageKey = iconKey.ToString();

        ScrollToNode(treeNode);
    }

    private void AddRootNode()
    {
        if (rootNode == null || (treeView.Nodes.Count == 1 && treeView.Nodes[0] == rootNode))
        {
            return;
        }

        treeView.Nodes.Clear();
        treeView.Nodes.Add(rootNode);
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

        if (treeNode != rootNode)
        {
            treeNode.ImageKey         = iconKey;
            treeNode.SelectedImageKey = iconKey;
        }
    }

    private void ClearFastMode()
    {
        treeView.BackColor        = SystemColors.Window;
        fastModeInfoLabel.Visible = false;
    }

    private void CreateRootNode(DirectoryInfo directory, DirectoryIcons imageKey)
    {
        rootPath = directory.FullName.Trim('\\');

        rootNode                  = new TreeNode(directory.Name);
        rootNode.Tag              = directory;
        rootNode.ImageKey         = imageKey.ToString();
        rootNode.SelectedImageKey = imageKey.ToString();

        directoryToTreeNodeMapping = new Dictionary<string, TreeNode>();
        directoryToTreeNodeMapping.Add(directory.FullName, rootNode);

        if (!FastMode)


            // During fast mode the root node will be added after the search finished 
        {
            AddRootNode();
        }
    }


    // TODO: Find better code structure for the following two routines
    private TreeNode FindOrCreateDirectoryNodeByPath(string path)
    {
        if (path == null)
        {
            return null;
        }

        if (directoryToTreeNodeMapping.ContainsKey(path))
        {
            return directoryToTreeNodeMapping[path];
        }

        return AddOrUpdateDirectoryNode(path, DirectorySearchStatusTypes.NotEmpty, "");
    }

    private void ProtectNode(TreeNode node)
    {
        var directory = (DirectoryInfo)node.Tag;

        if (nodePropsBackup.ContainsKey(directory.FullName))
        {
            return;
        }

        if (OnProtectionStatusChanged != null)
        {
            OnProtectionStatusChanged(this, new ProtectionStatusChangedEventArgs(directory.FullName, true));
        }


        // Backup node props if the user changes his mind we can restore the node
        // TODO: I'm sure there is a better way to do this, maybe this info can be stored 
        // in the node.Tag or we simply recreate this info like it's a new node.
        nodePropsBackup.Add(directory.FullName, node.ImageKey + "|" + node.ForeColor.ToArgb());

        node.ImageKey         = "protected_icon";
        node.SelectedImageKey = "protected_icon";
        node.ForeColor        = Color.Blue;


        // Recursively protect directories
        if (node.Parent != rootNode)
        {
            ProtectNode(node.Parent);
        }
    }

    private void ResetTree()
    {
        rootNode                   = null;
        directoryToTreeNodeMapping = new Dictionary<string, TreeNode>();
        nodePropsBackup            = new Dictionary<string, object>();

        treeView.Nodes.Clear();
    }

    private void ScrollToNode(TreeNode node)
    {
        // Ignore when fast mode is enabled
        if (!FastMode)
        {
            node.EnsureVisible();
        }
    }

    private void ShowFastModeResults()
    {
        if (!FastMode)
        {
            return;
        }

        treeView.ResumeLayout();
        ClearFastMode();

        AddRootNode();


        // Scroll to root node and expand all dirs
        rootNode.EnsureVisible();
        treeView.ExpandAll();
    }

    private void SuspendTreeViewForFastMode()
    {
        treeView.SuspendLayout();

        treeView.BackColor        = SystemColors.Control;
        fastModeInfoLabel.Visible = true;
    }

    /// <summary>Hack to selected the correct node</summary>
    private void tvFolders_MouseClick(object sender, MouseEventArgs e)
    {
        treeView.SelectedNode = treeView.GetNodeAt(e.X, e.Y);
    }

    private void UnprotectNode(TreeNode node)
    {
        if (node != null)
        {
            var directory = (DirectoryInfo)node.Tag;

            if (!nodePropsBackup.ContainsKey(directory.FullName))


                // TODO: What to do when this info is missing, show error?
            {
                return;
            }


            // Restore props from backup values
            var propList = ((string)nodePropsBackup[directory.FullName]).Split('|');

            nodePropsBackup.Remove(directory.FullName);

            node.ImageKey         = propList[0];
            node.SelectedImageKey = propList[0];
            node.ForeColor        = Color.FromArgb(int.Parse(propList[1]));

            if (OnProtectionStatusChanged != null)
            {
                OnProtectionStatusChanged(this, new ProtectionStatusChangedEventArgs(directory.FullName, false));
            }


            // Unprotect all subnodes
            foreach (TreeNode subNode in node.Nodes)
            {
                UnprotectNode(subNode);
            }
        }
    }

    private Dictionary<string, TreeNode> directoryToTreeNodeMapping;

    private readonly Label fastModeInfoLabel;

    /// <summary>This dictionary holds the original properties of protected nodes so that they can be restored if the user undoes the action</summary>
    private Dictionary<string, object> nodePropsBackup = new();

    private TreeNode rootNode;

    private string rootPath = "";

    private readonly TreeView treeView;

}