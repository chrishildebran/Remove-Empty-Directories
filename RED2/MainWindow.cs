namespace RED2
{

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Configuration;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Reflection;
    using System.Security.Principal;
    using System.Text;
    using System.Windows.Forms;
    using Lib;
    using Properties;
    using ErrorEventArgs = Lib.ErrorEventArgs;

    public partial class MainWindow : Form
    {

        /// <summary>
        ///     Constructor
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
        }

        /// <summary>
        ///     Check if we were started with admin rights
        /// </summary>
        private void AdminCheck()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());

            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                var isIntegrated = SystemFunctions.IsRegKeyIntegratedIntoWindowsExplorer();

                this.btnExplorerIntegrate.Enabled = !isIntegrated;
                this.btnExplorerRemove.Enabled    = isIntegrated;

                this.Text += " (Admin mode)";

                this.lblReqAdmin.ForeColor = Color.DarkGray;
            }
            else
            {
                this.groupBoxExplorerIntegration.Enabled = false;


                // Highlight admin info text bold 
                // Note: Changed it from red to bold because red looked like an error
                // but actually it's just an info message
                this.lblReqAdmin.Font = new Font(DefaultFont, FontStyle.Bold);


                // this.btnExplorerIntegrate.Enabled = false;
                // this.btnExplorerRemove.Enabled = false;
            }
        }

        /// <summary>
        ///     Bind config settings to UI controls
        /// </summary>
        private void BindConfigToControls()
        {
            this.tbFolder.DataBindings.Add("Text", Settings.Default, "last_used_directory");
            this.cbFastSearchMode.DataBindings.Add("Checked", Settings.Default, "fast_search_mode");

            this.cbIgnoreHiddenFolders.DataBindings.Add("Checked", Settings.Default, "dont_scan_hidden_folders");

            this.cbIgnore0kbFiles.DataBindings.Add("Checked", Settings.Default, "ignore_0kb_files");
            this.cbKeepSystemFolders.DataBindings.Add("Checked", Settings.Default, "keep_system_folders");
            this.cbClipboardDetection.DataBindings.Add("Checked", Settings.Default, "clipboard_detection");
            this.cbHideScanErrors.DataBindings.Add("Checked", Settings.Default, "hide_scan_errors");

            this.tbIgnoreFiles.DataBindings.Add("Text", Settings.Default, "ignore_files");
            this.tbIgnoreFolders.DataBindings.Add("Text", Settings.Default, "ignore_directories");

            this.nuMaxDepth.DataBindings.Add("Value", Settings.Default, "max_depth");

            this.nuInfiniteLoopDetectionCount.DataBindings.Add("Value", Settings.Default, "infinite_loop_detection_count");

            this.nuPause.DataBindings.Add("Value", Settings.Default, "pause_between");
            this.cbIgnoreErrors.DataBindings.Add("Checked", Settings.Default, "ignore_deletion_errors");
            this.nuFolderAge.DataBindings.Add("Value", Settings.Default, "min_folder_age_hours");


            // Populate delete mode item list
            foreach (var d in DeleteModeItem.GetList())
            {
                this.cbDeleteMode.Items.Add(new DeleteModeItem(d));
            }

            this.cbDeleteMode.DataBindings.Add("SelectedIndex", Settings.Default, "delete_mode");
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.core.CancelCurrentProcess();
        }

        /// <summary>
        ///     Let the user select a folder
        /// </summary>
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
            this.data.AddLogSpacer();
            this.SetStatusAndLogMessage(Resources.started_deletion_process);

            this.btnScan.Enabled = false;
            this.UpdateContextMenu(this.cmStrip, false);
            this.btnDelete.Enabled = false;

            this.SetProcessActiveLock(true);

            this.UpdateRuntimeDataObject();

            this.tree.OnDeletionProcessStart();

            this.runtimeWatch.Reset();
            this.runtimeWatch.Start();

            this.core.StartDeleteProcess();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnExplorerIntegrate_Click(object sender, EventArgs e)
        {
            SystemFunctions.AddOrRemoveRegKey(true);
            this.btnExplorerRemove.Enabled    = true;
            this.btnExplorerIntegrate.Enabled = false;
        }

        private void btnExplorerRemove_Click(object sender, EventArgs e)
        {
            SystemFunctions.AddOrRemoveRegKey(false);
            this.btnExplorerRemove.Enabled    = false;
            this.btnExplorerIntegrate.Enabled = true;
        }

        private void btnResetConfig_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, "Do you really want to reset all settings to the default values?", "Restore default settings", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                Settings.Default.Reset();

                this.tree.SetFastMode(Settings.Default.fast_search_mode);
            }
        }

        /// <summary>
        ///     Starts the Scan-Progress
        /// </summary>
        private void btnScan_Click(object sender, EventArgs e)
        {
            // Check given folder
            DirectoryInfo selectedDirectory = null;

            try
            {
                selectedDirectory = new DirectoryInfo(this.tbFolder.Text);

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

            this.data.StartFolder = selectedDirectory;
            this.UpdateRuntimeDataObject();

            this.pbProgressStatus.Style = ProgressBarStyle.Marquee;

            this.SetProcessActiveLock(true);

            this.tree.OnSearchStart(this.data.StartFolder);

            this.UpdateContextMenu(this.cmStrip, false);

            this.data.AddLogSpacer();
            this.SetStatusAndLogMessage(Resources.searching_empty_folders);

            this.runtimeWatch.Reset();
            this.runtimeWatch.Start();

            this.core.SearchingForEmptyDirectories();
        }

        private void btnShowConfig_Click(object sender, EventArgs e)
        {
            SystemFunctions.OpenDirectoryWithExplorer(Application.StartupPath);
        }

        private void btnShowLog_Click(object sender, EventArgs e)
        {
            var logWindow = new LogWindow();
            logWindow.SetLog(this.core.GetLogMessages());
            logWindow.ShowDialog();
            logWindow.Dispose();
        }

        private void cmStrip_Opening(object sender, CancelEventArgs e)
        {
            this.openFolderToolStripMenuItem.Enabled = this.tvFolders.SelectedNode != null;
        }

        private void core_OnAborted(object sender, EventArgs e)
        {
            this.pbProgressStatus.Style = ProgressBarStyle.Blocks;

            if (this.core.CurrentProcessStep == WorkflowSteps.DeleteProcessRunning)
            {
                this.SetStatusAndLogMessage(Resources.deletion_aborted);
            }
            else
            {
                this.SetStatusAndLogMessage(Resources.process_aborted);
            }

            this.btnScan.Enabled   = true;
            this.btnDelete.Enabled = false;

            this.SetProcessActiveLock(false);
            this.tree.OnProcessCancelled();
        }

        private void core_OnCancelled(object sender, EventArgs e)
        {
            this.pbProgressStatus.Style = ProgressBarStyle.Blocks;

            if (this.core.CurrentProcessStep == WorkflowSteps.DeleteProcessRunning)
            {
                this.SetStatusAndLogMessage(Resources.deletion_aborted);
            }
            else
            {
                this.SetStatusAndLogMessage(Resources.process_cancelled);
            }

            this.btnScan.Enabled   = true;
            this.btnDelete.Enabled = false;

            this.SetProcessActiveLock(false);
            this.tree.OnProcessCancelled();
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
                this.core.AbortDeletion();
            }
            else
            {
                // Hack: retry means -> ignore all errors
                if (dialogResult == DialogResult.Retry)
                {
                    this.data.IgnoreAllErrors = true;
                }

                this.core.ContinueDeleteProcess();
            }
        }

        private void core_OnDeleteProcessChanged(object sender, DeleteProcessUpdateEventArgs e)
        {
            switch (e.Status)
            {
                case DirectoryDeletionStatusTypes.Deleted:

                    this.lbStatus.Text = string.Format(Resources.removing_empty_folders, e.ProgressStatus + 1, e.FolderCount);

                    this.tree.UpdateItemIcon(e.Path, DirectoryIcons.Deleted);

                    break;

                case DirectoryDeletionStatusTypes.Protected:

                    this.tree.UpdateItemIcon(e.Path, DirectoryIcons.ProtectedIcon);

                    break;

                default:

                    this.tree.UpdateItemIcon(e.Path, DirectoryIcons.FolderWarning);

                    break;
            }

            this.pbProgressStatus.Value = e.ProgressStatus;
        }

        private void core_OnDeleteProcessFinished(object sender, DeleteProcessFinishedEventArgs e)
        {
            this.runtimeWatch.Stop();

            this.SetStatusAndLogMessage(string.Format(Resources.delete_process_finished, e.DeletedFolderCount, e.FailedFolderCount, e.ProtectedCount, this.runtimeWatch.Elapsed.Minutes, this.runtimeWatch.Elapsed.Seconds));

            this.pbProgressStatus.Value = this.pbProgressStatus.Maximum;

            this.btnDelete.Enabled = false;
            this.btnScan.Enabled   = true;

            this.SetProcessActiveLock(false);


            // Increase deletion statistics (shown in about tab)
            Settings.Default.delete_stats += e.DeletedFolderCount;

            this.lblRedStats.Text = string.Format(Resources.red_deleted, Settings.Default.delete_stats);

            this.tree.OnDeletionProcessFinished();
        }

        private void core_OnError(object sender, ErrorEventArgs e)
        {
            this.pbProgressStatus.Style = ProgressBarStyle.Blocks;

            MessageBox.Show(this, "Error: " + e.Message, "RED error message");
        }

        private void core_OnFoundEmptyDir(object sender, FoundEmptyDirInfoEventArgs e)
        {
            this.tree.AddOrUpdateDirectoryNode(e.Directory, e.Type, e.ErrorMessage);
        }

        private void core_OnFoundFinishedScanForEmptyDirs(object sender, FinishedScanForEmptyDirsEventArgs e)
        {
            // Search finished

            this.runtimeWatch.Stop();

            this.SetStatusAndLogMessage(string.Format(Resources.found_x_empty_folders, e.EmptyFolderCount, e.FolderCount, this.runtimeWatch.Elapsed.Minutes, this.runtimeWatch.Elapsed.Seconds));

            this.btnDelete.Enabled        = e.EmptyFolderCount > 0;
            this.pbProgressStatus.Style   = ProgressBarStyle.Blocks;
            this.pbProgressStatus.Maximum = e.EmptyFolderCount;
            this.pbProgressStatus.Minimum = 0;
            this.pbProgressStatus.Value   = this.pbProgressStatus.Maximum;
            this.pbProgressStatus.Step    = 5;

            this.SetProcessActiveLock(false);

            this.btnScan.Enabled = true;

            this.UpdateContextMenu(this.cmStrip, true);

            this.tree.OnSearchFinished();

            this.btnScan.Text = Resources.btn_scan_again;
        }

        private void core_OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.lbStatus.Text = (string)e.UserState;
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
                this.tree.SetFastMode((bool)e.NewValue);
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.tree.DeleteSelectedDirectory();
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
                var icon = this.ilFolderIcons.Images[key];

                var picIcon = new PictureBox();
                picIcon.Image    = icon;
                picIcon.Location = new Point(xpos, ypos);
                picIcon.Name     = "picIcon";
                picIcon.Size     = new Size(icon.Width, icon.Height);

                var picLabel = new Label();
                picLabel.Text     = icons[key];
                picLabel.Location = new Point(xpos + icon.Width + 2, ypos + 2);
                picLabel.Name     = "picLabel";

                this.pnlIcons.Controls.Add(picIcon);
                this.pnlIcons.Controls.Add(picLabel);

                ypos += icon.Height + 6;
            }

            #endregion
        }

        private void fMain_Activated(object sender, EventArgs e)
        {
            // Detect paths in the clipboard

            if (this.cbClipboardDetection.Checked && Clipboard.ContainsText(TextDataFormat.Text))
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

        /// <summary>
        ///     Part of the drag & drop functions
        ///     (you can drag a folder into RED)
        /// </summary>
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

        /// <summary>
        ///     Part of the drag & drop functions
        ///     (you can drag a folder into RED)
        /// </summary>
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

        /// <summary>
        ///     On load
        /// </summary>
        private void fMain_Load(object sender, EventArgs e)
        {
            #region Init RED core

            this.core = new RedCore(this, this.data);


            // Attach events
            this.core.OnError     += this.core_OnError;
            this.core.OnCancelled += this.core_OnCancelled;
            this.core.OnAborted   += this.core_OnAborted;

            this.core.OnProgressChanged     += this.core_OnProgressChanged;
            this.core.OnFoundEmptyDirectory += this.core_OnFoundEmptyDir;

            this.core.OnFinishedScanForEmptyDirs += this.core_OnFoundFinishedScanForEmptyDirs;

            this.core.OnDeleteProcessChanged += this.core_OnDeleteProcessChanged;

            this.core.OnDeleteProcessFinished += this.core_OnDeleteProcessFinished;

            this.core.OnDeleteError += this.core_OnDeleteError;

            #endregion


            // Subscribe to settings events
            Settings.Default.PropertyChanged += this.Default_PropertyChanged;
            Settings.Default.SettingChanging += this.Default_SettingChanging;


            // Init tree manager / helper
            this.tree = new TreeManager(this.tvFolders, this.lbFastModeInfo);
            this.tree.SetFastMode(Settings.Default.fast_search_mode);

            this.tree.OnProtectionStatusChanged += this.tree_OnProtectionStatusChanged;

            this.tree.OnDeleteRequest += this.tree_OnDeleteRequest;

            this.BindConfigToControls();


            // Update labels
            this.lblRedStats.Text = string.Format(Resources.red_deleted, Settings.Default.delete_stats);

            this.lbAppTitle.Text += $"{Assembly.GetExecutingAssembly().GetName().Version}";
            this.lbStatus.Text   =  "";

            this.AdminCheck();

            this.UpdateContextMenu(this.cmStrip, false);

            this.pbProgressStatus.Maximum = 100;
            this.pbProgressStatus.Minimum = 0;
            this.pbProgressStatus.Step    = 5;

            this.DrawDirectoryIcons();

            this.ProcessCommandLineArgs();
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
            SystemFunctions.OpenDirectoryWithExplorer(this.tree.GetSelectedFolderPath());
        }

        /// <summary>
        ///     Read and apply command line arguments
        /// </summary>
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
            this.tree.ProtectSelected();
        }

        private void proToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.tvFolders.SelectedNode == null)
            {
                return;
            }

            Settings.Default.ignore_directories += "\r\n" + ((DirectoryInfo)this.tvFolders.SelectedNode.Tag).FullName;


            // Focus third tab (Ignore list)
            this.tcMain.SelectedIndex = 2;


            // TODO: Update the results + tree to reflect the newly ignored item
            // Current solution: The user has to do a complete rescan
            this.btnDelete.Enabled = false;
        }

        private void scanOnlyThisDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Settings.Default.last_used_directory = this.tree.GetSelectedFolderPath();
            this.btnScan.PerformClick();
        }

        /// <summary>
        ///     Locks various GUI elements when search or deletion is active
        /// </summary>
        /// <param
        ///     name="isActive">
        /// </param>
        private void SetProcessActiveLock(bool isActive)
        {
            this.btnCancel.Enabled  = isActive;
            this.btnShowLog.Enabled = !isActive;

            this.gbOptions.Enabled       = !isActive;
            this.gbDeleteMode.Enabled    = !isActive;
            this.tbIgnoreFolders.Enabled = !isActive;

            this.gbAdvancedSettings.Enabled = !isActive;
            this.gbIgnoreFilenames.Enabled  = !isActive;

            this.btnResetConfig.Enabled = !isActive;
        }

        private void SetStatusAndLogMessage(string msg)
        {
            this.lbStatus.Text = msg;
            this.data.AddLogMessage(msg);
        }

        private void tbFolder_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.tbFolder.SelectAll();
        }

        private void toolStripCollapseAll_Click(object sender, EventArgs e)
        {
            this.tvFolders.CollapseAll();
        }

        private void toolStripExpandAll_Click(object sender, EventArgs e)
        {
            this.tvFolders.ExpandAll();
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
                this.tree.RemoveNode(deletePath);

                this.data.AddLogMessage("Manually deleted: \"" + deletePath + "\" including all subdirectories");


                // Disable the delete button because the user has to re-scan after he manually deleted a directory
                this.btnDelete.Enabled = false;
            }
            catch (OperationCanceledException)
            {
                // The user canceled the deletion 
            }
            catch (Exception ex)
            {
                this.data.AddLogMessage("Could not manually delete \"" + e.Directory + "\" because of the following error: " + ex.Message);

                MessageBox.Show(this, "The directory was not deleted, because of the following error:" + Environment.NewLine + ex.Message);
            }
        }

        private void tree_OnProtectionStatusChanged(object sender, ProtectionStatusChangedEventArgs e)
        {
            if (e.Protected)
            {
                this.core.AddProtectedFolder(e.Path);
            }
            else
            {
                this.core.RemoveProtected(e.Path);
            }
        }

        /// <summary>
        ///     User clicks twice on a folder
        /// </summary>
        private void tvFolders_DoubleClick(object sender, EventArgs e)
        {
            SystemFunctions.OpenDirectoryWithExplorer(this.tree.GetSelectedFolderPath());
        }

        private void unprotectFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.tree.UnprotectSelected();
        }

        /// <summary>
        ///     Enables/disables all items in the context menu
        /// </summary>
        /// <param
        ///     name="contextMenuStrip">
        /// </param>
        /// <param
        ///     name="enable">
        /// </param>
        private void UpdateContextMenu(ContextMenuStrip contextMenuStrip, bool enable)
        {
            foreach (ToolStripItem item in contextMenuStrip.Items)
            {
                item.Enabled = enable;
            }
        }

        private void UpdateRuntimeDataObject()
        {
            this.data.IgnoreAllErrors            = Settings.Default.ignore_deletion_errors;
            this.data.IgnoreFiles                = Settings.Default.ignore_files;
            this.data.IgnoreDirectoriesList      = Settings.Default.ignore_directories;
            this.data.IgnoreEmptyFiles           = Settings.Default.ignore_0kb_files;
            this.data.IgnoreHiddenFolders        = Settings.Default.dont_scan_hidden_folders;
            this.data.KeepSystemFolders          = Settings.Default.keep_system_folders;
            this.data.HideScanErrors             = Settings.Default.hide_scan_errors;
            this.data.MinFolderAgeHours          = Settings.Default.min_folder_age_hours;
            this.data.MaxDepth                   = (int)Settings.Default.max_depth;
            this.data.InfiniteLoopDetectionCount = (int)Settings.Default.infinite_loop_detection_count;
            this.data.DeleteMode                 = (DeleteModes)Settings.Default.delete_mode;
            this.data.PauseTime                  = (int)Settings.Default.pause_between;
        }

        private RedCore core;

        private readonly RuntimeData data = new RuntimeData();

        private readonly Stopwatch runtimeWatch = new Stopwatch();

        private TreeManager tree;

    }

}