using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Forms;
using FlySightViewer.WinFormsUI.Docking;
using Brejc.GpsLibrary;
using Brejc.GpsLibrary.Gpx;

namespace FlySightViewer.Forms
{
    public partial class MainForm : Form
    {
        private bool mSaveLayout = true;
        private DeserializeDockContent mDeserializeDockContent;
        private MapForm mMapForm = new MapForm();
        private GraphForm mGraphForm = new GraphForm();
        private JumpForm mJumpForm = new JumpForm();
        private DataForm mDataForm = new DataForm();
        private Version mOnlineVersion;
        private object mOnlineVersionLock = new object();

        public MainForm()
        {
            InitializeComponent();
            mDeserializeDockContent = new DeserializeDockContent(GetContentFromPersistString);

            // monitor name and dirty flag.
            Project.ProjectDirtyChanged += new EventHandler(UpdateTitleBar);
            Project.ProjectNameChanged += new EventHandler(UpdateTitleBar);

            // monitor range selection.
            mGraphForm.DisplayRangeChanged += new EventHandler(mGraphForm_DisplayRangeChanged);

            mJumpForm.SelectedEntryChanged += new EventHandler(mJumpForm_SelectedEntryChanged);

            // update once.
            UpdateTitleBar(null, EventArgs.Empty);

            // load settings.
            Settings.Instance.Load();
        }

        void mJumpForm_SelectedEntryChanged(object sender, EventArgs e)
        {
            mDataForm.SelectedEntry = mJumpForm.SelectedEntry;
            mGraphForm.SelectedEntry = mJumpForm.SelectedEntry;
            mMapForm.SelectedEntry = mJumpForm.SelectedEntry;
        }

        private void mGraphForm_DisplayRangeChanged(object sender, EventArgs e)
        {
            mMapForm.DisplayRange = mGraphForm.DisplayRange;
        }

        private void UpdateTitleBar(object sender, EventArgs e)
        {
            string dirty = Project.Dirty ? "*" : "";
            if (string.IsNullOrEmpty(Project.Name))
            {
                Text = string.Format("FlySight Viewer {0} - [noname.fly{1}]", Program.Version, dirty);
            }
            else
            {
                Text = string.Format("FlySight Viewer {0} - [{1}{2}]", Program.Version, Path.GetFileName(Project.Name), dirty);
            }
        }

        private IDockContent GetContentFromPersistString(string persistString)
        {
            if (persistString == typeof(MapForm).ToString())
                return mMapForm;
            if (persistString == typeof(GraphForm).ToString())
                return mGraphForm;
            if (persistString == typeof(JumpForm).ToString())
                return mJumpForm;
            if (persistString == typeof(DataForm).ToString())
                return mDataForm;
            return null;
        }

        protected override void OnLoad(EventArgs e)
        {
            string configFile = Path.Combine(Application.UserAppDataPath, "dock.config");
            if (File.Exists(configFile))
            {
                dockPanel.LoadFromXml(configFile, mDeserializeDockContent);
            }
            else
            {
                dockPanel.DockLeftPortion = 0.25f;
                dockPanel.DockBottomPortion = 0.5f;
                mMapForm.Show(dockPanel);
                mDataForm.Show(dockPanel, DockState.DockBottom);
                mGraphForm.Show(mMapForm.Pane, DockAlignment.Left, 0.5f);
                mJumpForm.Show(dockPanel, DockState.DockLeft);
            }

            // do our online version check.
            mWorker.RunWorkerAsync();
            base.OnLoad(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!Project.IsItSaveToDestroyProject())
            {
                e.Cancel = true;
            }
            else
            {
                Settings.Instance.Save();
                Directory.CreateDirectory(Application.UserAppDataPath);
                string configFile = Path.Combine(Application.UserAppDataPath, "dock.config");
                if (mSaveLayout)
                {
                    dockPanel.SaveAsXml(configFile);
                }
                else if (File.Exists(configFile))
                {
                    File.Delete(configFile);
                }
            }
            base.OnClosing(e);
        }

        private void jumpExplorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mJumpForm.Show(dockPanel);
        }

        private void graphToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mGraphForm.Show(dockPanel);
        }

        private void googleMapsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mMapForm.Show(dockPanel);
        }

        private void dataViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mDataForm.Show(dockPanel);
        }

        private void OnNewClick(object sender, EventArgs e)
        {
            Project.OnNewClick();
        }

        private void OnOpenClick(object sender, EventArgs e)
        {
            Project.OnOpenClick();
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            Project.OnSaveClick();
        }

        private void OnSaveAsClick(object sender, EventArgs e)
        {
            Project.OnSaveAsClick();
        }

        private void OnImportCSVClick(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Import file";
            dialog.Filter = "FlySight csv file (*.csv)|*.csv";
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;
            dialog.Multiselect = true;
            dialog.ShowHelp = true;

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                Project.ImportFiles(dialog.FileNames);
            }
        }
        
        private void OnImportGPXClick(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Import file";
            dialog.Filter = "GPX file (*.gpx)|*.gpx";
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;
            dialog.Multiselect = true;
            dialog.ShowHelp = true;

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                Project.ImportFiles(dialog.FileNames);
            }
        }

        private void OnDownloadClick(object sender, EventArgs e)
        {
            GpsDownloadForm dlg = new GpsDownloadForm();
            if (dlg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                GpsBabelCommunicator gpsCommunicator = new GpsBabelCommunicator();
                gpsCommunicator.GpsBabelWrapper.GpsBabelPath = Path.Combine(Settings.Instance.GPSBabelPath, "gpsbabel.exe");
                gpsCommunicator.GpsBabelWrapper.InputPort = dlg.InputPort;
                gpsCommunicator.GpsBabelWrapper.SourceType = dlg.SourceType;

                try
                {
                    Project.ImportFiles(gpsCommunicator.DownloadGpsData());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error downloading.", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnExitClick(object sender, EventArgs e)
        {
            Close();
        }

        private void OnAboutClick(object sender, EventArgs e)
        {
            AboutBox box = new AboutBox();
            box.ShowDialog(this);
        }

        private void OnCheckForUpdates(object sender, EventArgs e)
        {
            mWorker_DoWork(null, new DoWorkEventArgs(null));
            mWorker_RunWorkerCompleted(null, new RunWorkerCompletedEventArgs(true, null, false));
        }

        private void mWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string url = @"http://tomvandijck.com/flysight/version.txt";
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Proxy = WebRequest.DefaultWebProxy;
                request.UserAgent = this.Text;
                request.Timeout = 5000;
                request.ReadWriteTimeout = 30000;

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        using (StreamReader read = new StreamReader(responseStream))
                        {
                            lock (mOnlineVersionLock)
                            {
                                mOnlineVersion = new Version(read.ReadToEnd());
                                Debug.WriteLine(string.Format("onlineVersion: {0} ", mOnlineVersion));
                            }
                        }
                    }
                }

                // make sure we're silent.
                e.Result = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        private void mWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lock (mOnlineVersionLock)
            {
                Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                if (mOnlineVersion > currentVersion)
                {
                    if (MessageBox.Show(this, "A new version is available online, do you wish to download it?",
                        "New version available", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                    {
                        DownloadDialog dlg = new DownloadDialog();
                        string installPath = dlg.DownloadInstaller(this, mOnlineVersion);
                        if (!string.IsNullOrEmpty(installPath))
                        {
                            Process.Start(installPath);
                            Close();
                        }
                    }
                }
                else if ((bool)e.Result)
                {
                    MessageBox.Show(this, "You are running the latest version.", "Perfect");
                }
            }
        }

        private void OnGotoWebsite(object sender, EventArgs e)
        {
            Process.Start("http://www.tomvandijck.com/flysight/");
        }

        private void OnGotoFlysight(object sender, EventArgs e)
        {
            Process.Start("http://www.flysight.ca/");
        }

        private void toolStripMenuItem7_Click(object sender, EventArgs e)
        {
            SettingsDialog dlg = new SettingsDialog();
            dlg.ShowDialog(this);
        }
    }
}
