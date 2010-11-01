using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using FlySightViewer.WinFormsUI.Docking;
using System.Net;

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
                            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                            Version onlineVersion = new Version(read.ReadToEnd());
                            Debug.WriteLine(string.Format("onlineVersion: {0} ", onlineVersion));

                            if (onlineVersion > currentVersion)
                            {
                                if (MessageBox.Show(this, "A new version is available online, do you wish to download it?", "Perfect", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                                {
                                    string downloadUrl = string.Format("http://tomvandijck.com/flysight/FlySightViewer-{0}-setup.exe", onlineVersion);
                                    Process.Start(downloadUrl);
                                    Close();
                                }
                            }
                            else
                            {
                                MessageBox.Show(this, "You are running the latest version.", "Perfect");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Unable to check version", "Error");
            }
        }
    }
}
