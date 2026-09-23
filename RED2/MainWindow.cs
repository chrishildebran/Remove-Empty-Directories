namespace RED2;

using Properties;
using ErrorEventArgs = Lib.ErrorEventArgs;

public partial class MainWindow : Form
{

    /// <summary>Constructor</summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>Check if we were started with admin rights</summary>
    private void AdminCheck()
    {
        var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());

        if (principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            var isIntegrated = SystemFunctions.IsRegKeyIntegratedIntoWindowsExplorer();

            btnExplorerIntegrate.Enabled = !isIntegrated;
            btnExplorerRemove.Enabled    = isIntegrated;

            Text += " (Admin mode)";

            lblReqAdmin.ForeColor = Color.DarkGray;
        }
        else
        {
            groupBoxExplorerIntegration.Enabled = false;


            // Highlight admin info text bold 
            // Note: Changed it from red to bold because red looked like an error
            // but actually it's just an info message
            lblReqAdmin.Font = new Font(DefaultFont, FontStyle.Bold);


            // this.btnExplorerIntegrate.Enabled = false;
            // this.btnExplorerRemove.Enabled = false;
        }
    }

    /// <summary>Bind config settings to UI controls</summary>
    private void BindConfigToControls()
    {
        tbFolder.DataBindings.Add("Text", Settings.Default, "last_used_directory");
        cbFastSearchMode.DataBindings.Add("Checked", Settings.Default, "fast_search_mode");

        cbIgnoreHiddenFolders.DataBindings.Add("Checked", Settings.Default, "dont_scan_hidden_folders");

        cbIgnore0kbFiles.DataBindings.Add("Checked", Settings.Default, "ignore_0kb_files");
        cbKeepSystemFolders.DataBindings.Add("Checked", Settings.Default, "keep_system_folders");
        cbClipboardDetection.DataBindings.Add("Checked", Settings.Default, "clipboard_detection");
        cbHideScanErrors.DataBindings.Add("Checked", Settings.Default, "hide_scan_errors");

        tbIgnoreFiles.DataBindings.Add("Text", Settings.Default, "ignore_files");
        tbIgnoreFolders.DataBindings.Add("Text", Settings.Default, "ignore_directories");

        nuMaxDepth.DataBindings.Add("Value", Settings.Default, "max_depth");

        nuInfiniteLoopDetectionCount.DataBindings.Add("Value", Settings.Default, "infinite_loop_detection_count");

        nuPause.DataBindings.Add("Value", Settings.Default, "pause_between");
        cbIgnoreErrors.DataBindings.Add("Checked", Settings.Default, "ignore_deletion_errors");
        nuFolderAge.DataBindings.Add("Value", Settings.Default, "min_folder_age_hours");


        // Populate delete mode item list
        foreach (var d in DeleteModeItem.GetList())
        {
            cbDeleteMode.Items.Add(new DeleteModeItem(d));
        }

        cbDeleteMode.DataBindings.Add("SelectedIndex", Settings.Default, "delete_mode");
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        core.CancelCurrentProcess();
    }

    /// <summary>Let the user select a folder</summary>
    private void btnChooseFolder_Click(object sender, EventArgs e)
    {
        Settings.Default.last_used_directory = SystemFunctions.ChooseDirectoryDialog(Settings.Default.last_used_directory);
    }

    private void btnCopyDebugInfo_Click(object sender, EventArgs e)
    {
        var info = new StringBuilder();

        info.AppendLine("System info");
        info.Append("- RED Version: ");

        try
        {
            info.AppendLine($"{Assembly.GetExecutingAssembly().GetName().Version}");
        }
        catch (Exception ex)
        {
            info.AppendLine("Failed (" + ex.Message + ")");
        }

        info.Append("- Operating System: ");

        try
        {
            info.AppendLine(Environment.OSVersion.ToString());
        }
        catch (Exception ex)
        {
            info.AppendLine("Failed (" + ex.Message + ")");
        }

        info.Append("- Processor architecture: ");

        try
        {
            info.AppendLine(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"));
        }
        catch (Exception ex)
        {
            info.AppendLine("Failed (" + ex.Message + ")");
        }

        info.Append("- Is Administrator: ");

        try
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            info.AppendLine(principal.IsInRole(WindowsBuiltInRole.Administrator) ? "Yes" : "No");
        }
        catch (Exception ex)
        {
            info.AppendLine("Failed (" + ex.Message + ")");
        }

        info.AppendLine("");
        info.AppendLine("RED Config settings: ");

        try
        {
            foreach (SettingsProperty setting in Settings.Default.Properties)
            {
                var value = Settings.Default.PropertyValues[setting.Name].PropertyValue.ToString();

                if (setting.Name == "ignore_files" || setting.Name == "ignore_directories")
                {
                    value = value.Replace("\r", "").Replace("\n", "\\n");
                }

                info.AppendLine("- " + setting.Name + ": " + value);
            }
        }
        catch (Exception ex)
        {
            info.AppendLine("Failed (" + ex.Message + ")");
        }

        try
        {
            Clipboard.SetText(info.ToString(), TextDataFormat.Text);

            MessageBox.Show("Copied this text to your clipboard:" + Environment.NewLine + Environment.NewLine + info);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Sorry, could not copy the debug info into your clipboard because of this error: " + Environment.NewLine + ex.Message);
        }
    }

    private void btnDelete_Click(object sender, EventArgs e)
    {
        data.AddLogSpacer();
        SetStatusAndLogMessage(Resources.started_deletion_process);

        btnScan.Enabled = false;
        UpdateContextMenu(cmStrip, false);
        btnDelete.Enabled = false;

        SetProcessActiveLock(true);

        UpdateRuntimeDataObject();

        tree.OnDeletionProcessStart();

        runtimeWatch.Reset();
        runtimeWatch.Start();

        core.StartDeleteProcess();
    }

    private void btnExit_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void btnExplorerIntegrate_Click(object sender, EventArgs e)
    {
        SystemFunctions.AddOrRemoveRegKey(true);
        btnExplorerRemove.Enabled    = true;
        btnExplorerIntegrate.Enabled = false;
    }

    private void btnExplorerRemove_Click(object sender, EventArgs e)
    {
        SystemFunctions.AddOrRemoveRegKey(false);
        btnExplorerRemove.Enabled    = false;
        btnExplorerIntegrate.Enabled = true;
    }

    private void btnResetConfig_Click(object sender, EventArgs e)
    {
        if (MessageBox.Show(this, "Do you really want to reset all settings to the default values?", "Restore default settings", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
        {
            Settings.Default.Reset();

            tree.SetFastMode(Settings.Default.fast_search_mode);
        }
    }

    /// <summary>Starts the Scan-Progress</summary>
    private void btnScan_Click(object sender, EventArgs e)
    {
        // Check given folder
        DirectoryInfo selectedDirectory = null;

        try
        {
            selectedDirectory = new DirectoryInfo(tbFolder.Text);

            if (!selectedDirectory.Exists)
            {
                MessageBox.Show(this, Resources.error_dir_does_not_exist);

                return;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "The given directory caused a problem:" + Environment.NewLine + ex.Message, "RED error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            return;
        }

        data.StartFolder = selectedDirectory;
        UpdateRuntimeDataObject();

        pbProgressStatus.Style = ProgressBarStyle.Marquee;

        SetProcessActiveLock(true);

        tree.OnSearchStart(data.StartFolder);

        UpdateContextMenu(cmStrip, false);

        data.AddLogSpacer();
        SetStatusAndLogMessage(Resources.searching_empty_folders);

        runtimeWatch.Reset();
        runtimeWatch.Start();

        core.SearchingForEmptyDirectories();
    }

    private void btnShowConfig_Click(object sender, EventArgs e)
    {
        SystemFunctions.OpenDirectoryWithExplorer(Application.StartupPath);
    }

    private void btnShowLog_Click(object sender, EventArgs e)
    {
        var logWindow = new LogWindow();
        logWindow.SetLog(core.GetLogMessages());
        logWindow.ShowDialog();
        logWindow.Dispose();
    }

    private void cmStrip_Opening(object sender, CancelEventArgs e)
    {
        openFolderToolStripMenuItem.Enabled = tvFolders.SelectedNode != null;
    }

    private void core_OnAborted(object sender, EventArgs e)
    {
        pbProgressStatus.Style = ProgressBarStyle.Blocks;

        if (core.CurrentProcessStep == WorkflowSteps.DeleteProcessRunning)
        {
            SetStatusAndLogMessage(Resources.deletion_aborted);
        }
        else
        {
            SetStatusAndLogMessage(Resources.process_aborted);
        }

        btnScan.Enabled   = true;
        btnDelete.Enabled = false;

        SetProcessActiveLock(false);
        tree.OnProcessCancelled();
    }

    private void core_OnCancelled(object sender, EventArgs e)
    {
        pbProgressStatus.Style = ProgressBarStyle.Blocks;

        if (core.CurrentProcessStep == WorkflowSteps.DeleteProcessRunning)
        {
            SetStatusAndLogMessage(Resources.deletion_aborted);
        }
        else
        {
            SetStatusAndLogMessage(Resources.process_cancelled);
        }

        btnScan.Enabled   = true;
        btnDelete.Enabled = false;

        SetProcessActiveLock(false);
        tree.OnProcessCancelled();
    }

    private void core_OnDeleteError(object sender, DeletionErrorEventArgs e)
    {
        var errorDialog = new DeletionError();

        errorDialog.SetPath(e.Path);
        errorDialog.SetErrorMessage(e.ErrorMessage);

        var dialogResult = errorDialog.ShowDialog();

        errorDialog.Dispose();

        if (dialogResult == DialogResult.Abort)
        {
            core.AbortDeletion();
        }
        else
        {
            // Hack: retry means -> ignore all errors
            if (dialogResult == DialogResult.Retry)
            {
                data.IgnoreAllErrors = true;
            }

            core.ContinueDeleteProcess();
        }
    }

    private void core_OnDeleteProcessChanged(object sender, DeleteProcessUpdateEventArgs e)
    {
        switch (e.Status)
        {
            case DirectoryDeletionStatusTypes.Deleted:

                lbStatus.Text = string.Format(Resources.removing_empty_folders, e.ProgressStatus + 1, e.FolderCount);

                tree.UpdateItemIcon(e.Path, DirectoryIcons.Deleted);

                break;

            case DirectoryDeletionStatusTypes.Protected:

                tree.UpdateItemIcon(e.Path, DirectoryIcons.ProtectedIcon);

                break;

            default:

                tree.UpdateItemIcon(e.Path, DirectoryIcons.FolderWarning);

                break;
        }

        pbProgressStatus.Value = e.ProgressStatus;
    }

    private void core_OnDeleteProcessFinished(object sender, DeleteProcessFinishedEventArgs e)
    {
        runtimeWatch.Stop();

        SetStatusAndLogMessage(string.Format(Resources.delete_process_finished, e.DeletedFolderCount, e.FailedFolderCount, e.ProtectedCount, runtimeWatch.Elapsed.Minutes, runtimeWatch.Elapsed.Seconds));

        pbProgressStatus.Value = pbProgressStatus.Maximum;

        btnDelete.Enabled = false;
        btnScan.Enabled   = true;

        SetProcessActiveLock(false);


        // Increase deletion statistics (shown in about tab)
        Settings.Default.delete_stats += e.DeletedFolderCount;

        lblRedStats.Text = string.Format(Resources.red_deleted, Settings.Default.delete_stats);

        tree.OnDeletionProcessFinished();
    }

    private void core_OnError(object sender, ErrorEventArgs e)
    {
        pbProgressStatus.Style = ProgressBarStyle.Blocks;

        MessageBox.Show(this, "Error: " + e.Message, "RED error message");
    }

    private void core_OnFoundEmptyDir(object sender, FoundEmptyDirInfoEventArgs e)
    {
        tree.AddOrUpdateDirectoryNode(e.Directory, e.Type, e.ErrorMessage);
    }

    private void core_OnFoundFinishedScanForEmptyDirs(object sender, FinishedScanForEmptyDirsEventArgs e)
    {
        // Search finished

        runtimeWatch.Stop();

        SetStatusAndLogMessage(string.Format(Resources.found_x_empty_folders, e.EmptyFolderCount, e.FolderCount, runtimeWatch.Elapsed.Minutes, runtimeWatch.Elapsed.Seconds));

        btnDelete.Enabled        = e.EmptyFolderCount > 0;
        pbProgressStatus.Style   = ProgressBarStyle.Blocks;
        pbProgressStatus.Maximum = e.EmptyFolderCount;
        pbProgressStatus.Minimum = 0;
        pbProgressStatus.Value   = pbProgressStatus.Maximum;
        pbProgressStatus.Step    = 5;

        SetProcessActiveLock(false);

        btnScan.Enabled = true;

        UpdateContextMenu(cmStrip, true);

        tree.OnSearchFinished();

        btnScan.Text = Resources.btn_scan_again;
    }

    private void core_OnProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        lbStatus.Text = (string)e.UserState;
    }

    private void Default_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // Save settings when any of them was changed
        Settings.Default.Save();
    }

    private void Default_SettingChanging(object sender, SettingChangingEventArgs e)
    {
        if (e.SettingName == "keep_system_folders" && !(bool)e.NewValue)
        {
            if (MessageBox.Show(this, SystemFunctions.ConvertLineBreaks(Resources.warning_really_delete), Resources.warning, MessageBoxButtons.OKCancel, MessageBoxIcon.Asterisk) == DialogResult.Cancel)
            {
                e.Cancel = true;
            }
        }
        else if (e.SettingName == "fast_search_mode")
        {
            tree.SetFastMode((bool)e.NewValue);
        }
    }

    private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
    {
        tree.DeleteSelectedDirectory();
    }

    private void DrawDirectoryIcons()
    {
        #region Set and display folder status icons

        var icons = new Dictionary<string, string>();

        icons.Add("home",               Resources.icon_root);
        icons.Add("folder",             Resources.icon_default);
        icons.Add("folder_trash_files", Resources.icon_contains_trash);
        icons.Add("folder_hidden",      Resources.icon_hidden_folder);
        icons.Add("folder_lock",        Resources.icon_locked_folder);
        icons.Add("folder_warning",     Resources.icon_warning);
        icons.Add("protected_icon",     Resources.icon_protected_folder);
        icons.Add("deleted",            Resources.icon_deleted_folder);

        var xpos = 6;
        var ypos = 30;

        foreach (var key in icons.Keys)
        {
            var icon = ilFolderIcons.Images[key];

            var picIcon = new PictureBox();
            picIcon.Image    = icon;
            picIcon.Location = new Point(xpos, ypos);
            picIcon.Name     = "picIcon";
            picIcon.Size     = new Size(icon.Width, icon.Height);

            var picLabel = new Label();
            picLabel.Text     = icons[key];
            picLabel.Location = new Point(xpos + icon.Width + 2, ypos + 2);
            picLabel.Name     = "picLabel";

            pnlIcons.Controls.Add(picIcon);
            pnlIcons.Controls.Add(picLabel);

            ypos += icon.Height + 6;
        }

        #endregion
    }

    private void fMain_Activated(object sender, EventArgs e)
    {
        // Detect paths in the clipboard

        if (cbClipboardDetection.Checked && Clipboard.ContainsText(TextDataFormat.Text))
        {
            var clipValue = Clipboard.GetText(TextDataFormat.Text);

            if (clipValue.Contains(":\\") && !clipValue.Contains("\n"))
            {
                // add ending backslash
                if (!clipValue.EndsWith("\\"))
                {
                    clipValue += "\\";
                }

                Settings.Default.last_used_directory = clipValue;
            }
        }
    }

    /// <summary>Part of the drag & drop functions (you can drag a folder into RED)</summary>
    private void fMain_DragDrop(object sender, DragEventArgs e)
    {
        var s = (string[])e.Data.GetData(DataFormats.FileDrop, false);

        if (s.Length == 1)
        {
            Settings.Default.last_used_directory = s[0].Trim();
        }
        else
        {
            MessageBox.Show(this, Resources.error_only_one_folder);
        }
    }

    /// <summary>Part of the drag & drop functions (you can drag a folder into RED)</summary>
    private void fMain_DragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effect = DragDropEffects.None;
        }
        else
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    /// <summary>On load</summary>
    private void fMain_Load(object sender, EventArgs e)
    {
        #region Init RED core

        core = new RedCore(this, data);


        // Attach events
        core.OnError     += core_OnError;
        core.OnCancelled += core_OnCancelled;
        core.OnAborted   += core_OnAborted;

        core.OnProgressChanged     += core_OnProgressChanged;
        core.OnFoundEmptyDirectory += core_OnFoundEmptyDir;

        core.OnFinishedScanForEmptyDirs += core_OnFoundFinishedScanForEmptyDirs;

        core.OnDeleteProcessChanged += core_OnDeleteProcessChanged;

        core.OnDeleteProcessFinished += core_OnDeleteProcessFinished;

        core.OnDeleteError += core_OnDeleteError;

        #endregion


        // Subscribe to settings events
        Settings.Default.PropertyChanged += Default_PropertyChanged;
        Settings.Default.SettingChanging += Default_SettingChanging;


        // Init tree manager / helper
        tree = new TreeManager(tvFolders, lbFastModeInfo);
        tree.SetFastMode(Settings.Default.fast_search_mode);

        tree.OnProtectionStatusChanged += tree_OnProtectionStatusChanged;

        tree.OnDeleteRequest += tree_OnDeleteRequest;

        BindConfigToControls();


        // Update labels
        lblRedStats.Text = string.Format(Resources.red_deleted, Settings.Default.delete_stats);

        lbAppTitle.Text += $"{Assembly.GetExecutingAssembly().GetName().Version}";
        lbStatus.Text   =  "";

        AdminCheck();

        UpdateContextMenu(cmStrip, false);

        pbProgressStatus.Maximum = 100;
        pbProgressStatus.Minimum = 0;
        pbProgressStatus.Step    = 5;

        DrawDirectoryIcons();

        ProcessCommandLineArgs();
    }

    private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start($"https://www.jonasjohn.de/lab/check_update.php?p=red&version={Assembly.GetExecutingAssembly().GetName().Version}");
    }

    private void linkLabel2_LinkClicked_1(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start("https://www.jonasjohn.de/lab/red_feedback.htm");
    }

    private void llGithub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start("https://github.com/hxseven/Remove-Empty-Directories");
    }

    private void llWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        Process.Start("https://www.jonasjohn.de/lab/red.htm");
    }

    private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
    {
        SystemFunctions.OpenDirectoryWithExplorer(tree.GetSelectedFolderPath());
    }

    /// <summary>Read and apply command line arguments</summary>
    private void ProcessCommandLineArgs()
    {
        var args = Environment.GetCommandLineArgs();

        if (args.Length > 1)
        {
            args[0] = "";
            var path = string.Join("", args).Replace("\"", "").Trim();


            // add ending backslash
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }

            Settings.Default.last_used_directory = path;
        }
    }

    private void protectFolderFromBeingDeletedToolStripMenuItem_Click(object sender, EventArgs e)
    {
        tree.ProtectSelected();
    }

    private void proToolStripMenuItem_Click(object sender, EventArgs e)
    {
        if (tvFolders.SelectedNode == null)
        {
            return;
        }

        Settings.Default.ignore_directories += "\r\n" + ((DirectoryInfo)tvFolders.SelectedNode.Tag).FullName;


        // Focus third tab (Ignore list)
        tcMain.SelectedIndex = 2;


        // TODO: Update the results + tree to reflect the newly ignored item
        // Current solution: The user has to do a complete rescan
        btnDelete.Enabled = false;
    }

    private void scanOnlyThisDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
    {
        Settings.Default.last_used_directory = tree.GetSelectedFolderPath();
        btnScan.PerformClick();
    }

    /// <summary>Locks various GUI elements when search or deletion is active</summary>
    /// <param name = "isActive"></param>
    private void SetProcessActiveLock(bool isActive)
    {
        btnCancel.Enabled  = isActive;
        btnShowLog.Enabled = !isActive;

        gbOptions.Enabled       = !isActive;
        gbDeleteMode.Enabled    = !isActive;
        tbIgnoreFolders.Enabled = !isActive;

        gbAdvancedSettings.Enabled = !isActive;
        gbIgnoreFilenames.Enabled  = !isActive;

        btnResetConfig.Enabled = !isActive;
    }

    private void SetStatusAndLogMessage(string msg)
    {
        lbStatus.Text = msg;
        data.AddLogMessage(msg);
    }

    private void tbFolder_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        tbFolder.SelectAll();
    }

    private void toolStripCollapseAll_Click(object sender, EventArgs e)
    {
        tvFolders.CollapseAll();
    }

    private void toolStripExpandAll_Click(object sender, EventArgs e)
    {
        tvFolders.ExpandAll();
    }

    private void tree_OnDeleteRequest(object sender, DeleteRequestFromTreeEventArgs e)
    {
        try
        {
            var deletePath = e.Directory;


            // To simplify the code here there is only the RecycleBinWithQuestion or simulate possible here
            // (all others will be ignored)
            SystemFunctions.ManuallyDeleteDirectory(deletePath, (DeleteModes)Settings.Default.delete_mode);


            // Remove root node
            tree.RemoveNode(deletePath);

            data.AddLogMessage("Manually deleted: \"" + deletePath + "\" including all subdirectories");


            // Disable the delete button because the user has to re-scan after he manually deleted a directory
            btnDelete.Enabled = false;
        }
        catch (OperationCanceledException)
        {
            // The user canceled the deletion 
        }
        catch (Exception ex)
        {
            data.AddLogMessage("Could not manually delete \"" + e.Directory + "\" because of the following error: " + ex.Message);

            MessageBox.Show(this, "The directory was not deleted, because of the following error:" + Environment.NewLine + ex.Message);
        }
    }

    private void tree_OnProtectionStatusChanged(object sender, ProtectionStatusChangedEventArgs e)
    {
        if (e.Protected)
        {
            core.AddProtectedFolder(e.Path);
        }
        else
        {
            core.RemoveProtected(e.Path);
        }
    }

    /// <summary>User clicks twice on a folder</summary>
    private void tvFolders_DoubleClick(object sender, EventArgs e)
    {
        SystemFunctions.OpenDirectoryWithExplorer(tree.GetSelectedFolderPath());
    }

    private void unprotectFolderToolStripMenuItem_Click(object sender, EventArgs e)
    {
        tree.UnprotectSelected();
    }

    /// <summary>Enables/disables all items in the context menu</summary>
    /// <param name = "contextMenuStrip"></param>
    /// <param name = "enable"></param>
    private void UpdateContextMenu(ContextMenuStrip contextMenuStrip, bool enable)
    {
        foreach (ToolStripItem item in contextMenuStrip.Items)
        {
            item.Enabled = enable;
        }
    }

    private void UpdateRuntimeDataObject()
    {
        data.IgnoreAllErrors            = Settings.Default.ignore_deletion_errors;
        data.IgnoreFiles                = Settings.Default.ignore_files;
        data.IgnoreDirectoriesList      = Settings.Default.ignore_directories;
        data.IgnoreEmptyFiles           = Settings.Default.ignore_0kb_files;
        data.IgnoreHiddenFolders        = Settings.Default.dont_scan_hidden_folders;
        data.KeepSystemFolders          = Settings.Default.keep_system_folders;
        data.HideScanErrors             = Settings.Default.hide_scan_errors;
        data.MinFolderAgeHours          = Settings.Default.min_folder_age_hours;
        data.MaxDepth                   = (int)Settings.Default.max_depth;
        data.InfiniteLoopDetectionCount = (int)Settings.Default.infinite_loop_detection_count;
        data.DeleteMode                 = (DeleteModes)Settings.Default.delete_mode;
        data.PauseTime                  = (int)Settings.Default.pause_between;
    }

    private RedCore core;

    private readonly RuntimeData data = new();

    private readonly Stopwatch runtimeWatch = new();

    private TreeManager tree;

}