using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using GMap.NET;
using GMap.NET.WindowsForms;
using FlySightLog;
using System.Text;
using FlySightLog.Source;

namespace FlySightLog
{
    public partial class MainForm : Form
    {
        private string mProjectName = string.Empty;
        private bool mProjectDirty = false;
        private Dictionary<string, LogEntry> mEntries = new Dictionary<string, LogEntry>();

        public MainForm()
        {
            InitializeComponent();

            if (!DesignMode)
            {
                // config map 
                MainMap.Position = new PointLatLng(54.6961334816182, 25.2985095977783);
                MainMap.MapType = MapType.GoogleHybrid;
                MainMap.MinZoom = 1;
                MainMap.MaxZoom = 17;
                MainMap.Zoom = 3;

                mGraphMode.Items.AddRange(Enum.GetNames(typeof(Graph.DisplayMode)));
                mGraphMode.SelectedIndex = 1;

                mGraph.AllowSelect = false;
                mAltitudeGraph.AllowSelect = true;
                mAltitudeGraph.SelectChanged += new EventHandler(mAltitudeGraph_SelectChanged);
                mAltitudeGraph.ShowUnits = false;

                UpdateTitleBar();
            }
        }

        void mAltitudeGraph_SelectChanged(object sender, EventArgs e)
        {
            if (mAltitudeGraph.SelectRange.Width > 10)
            {
                mGraph.DisplayRange = mAltitudeGraph.SelectRange;
                MainMap.DisplayRange = mAltitudeGraph.SelectRange;
            }
            else
            {
                mGraph.DisplayRange = Range.Invalid;
                MainMap.DisplayRange = Range.Invalid;
            }
        }

        private string ProjectName
        {
            get { return mProjectName; }
            set
            {
                if (mProjectName != value)
                {
                    mProjectName = value;
                    UpdateTitleBar();
                }
            }
        }

        private bool ProjectDirty
        {
            get { return mProjectDirty; }
            set
            {
                mProjectDirty = value;
                UpdateTitleBar();
            }
        }

        private void UpdateTitleBar()
        {
            string dirty = mProjectDirty ? "*" : "";
            if (string.IsNullOrEmpty(mProjectName))
            {
                Text = string.Format("FlySight Viewer - [noname.fly{0}]", dirty);
            }
            else
            {
                Text = string.Format("FlySight Viewer - [{0}{1}]", Path.GetFileName(mProjectName), dirty);
            }
        }

        private void mJumpTree_DragEnter(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void mJumpTree_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
            {
                ImportFiles(files);
            }
        }

        private void ImportFiles(string[] aPaths)
        {
            mJumpTree.BeginUpdate();

            try
            {
                foreach (string file in aPaths)
                {
                    ImportFile(file);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error importing.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            mJumpTree.Sort();
            mJumpTree.EndUpdate();
            mJumpTree.ExpandAll();
        }

        private void ImportFile(string aPath)
        {
            string name = Path.GetFileNameWithoutExtension(aPath).ToLower();
            if (!mEntries.ContainsKey(name))
            {
                string ext = Path.GetExtension(aPath).ToLower();
                switch (ext)
                {
                    case ".csv":
                        LogEntry entry = FlySight.Import(aPath);
                        AddEntry(name, entry);
                        break;
                    default:
                        MessageBox.Show("Unsupported fileformat");
                        break;
                }
            }
        }

        private void AddEntry(string aName, LogEntry aEntry)
        {
            ProjectDirty = true;
            mEntries.Add(aName, aEntry);
            AddEntryToTree(aEntry);
        }

        private void AddEntryToTree(LogEntry aEntry)
        {
            DateTime local = aEntry.DateTime.ToLocalTime();
            TreeNode node = AddDate(local);
            TreeNode entry = GetOrAdd(node.Nodes, local.ToString("H:mm.ss"));
            entry.Tag = aEntry;
        }

        private TreeNode AddDate(DateTime aDate)
        {
            TreeNode year = GetOrAdd(mJumpTree.Nodes, aDate.ToString("yyyy"));
            return GetOrAdd(year.Nodes, aDate.ToString("MMMM, d"));
        }

        private TreeNode GetOrAdd(TreeNodeCollection aNodes, string aValue)
        {
            int idx = aNodes.IndexOfKey(aValue);
            if (idx >= 0)
            {
                return aNodes[idx];
            }
            else
            {
                return aNodes.Add(aValue, aValue);
            }
        }

        private void mJumpTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            LogEntry entry = e.Node.Tag as LogEntry;
            MainMap.LogEntry = entry;
            mGraph.LogEntry = entry;
            mAltitudeGraph.LogEntry = entry;

            if (entry != null)
            {
                mRawData.DataSource = entry.Records;
            }
            else
            {
                mRawData.DataSource = null;
            }
        }

        private void ResetProject()
        {
            mJumpTree.Nodes.Clear();
            mEntries.Clear();
            ProjectName = string.Empty;
            ProjectDirty = false;
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (IsItSaveToDestroyProject())
            {
                ResetProject();
            }
        }

        private void OnSaveClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ProjectName))
            {
                OnSaveAsClick(sender, e);
            }
            else
            {
                SaveProject(ProjectName);
            }
        }

        private void OnSaveAsClick(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Save log";
            dialog.Filter = "FlySight files (*.fly)|*.fly";
            dialog.CheckPathExists = true;
            dialog.AddExtension = true;
            dialog.ShowHelp = true;

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                ProjectName = dialog.FileName;
                SaveProject(ProjectName);
            }
        }

        private bool IsItSaveToDestroyProject()
        {
            if (mProjectDirty)
            {
                switch (MessageBox.Show(this, "Log has been modified but not saved yet.\nDo you want to save first?",
                    "Log not saved", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Cancel:
                        return false;

                    case DialogResult.Yes:
                        OnSaveClick(this, EventArgs.Empty);
                        return !mProjectDirty;

                    case DialogResult.No:
                        return true;
                }
            }
            return true;
        }

        private void OnExit(object sender, EventArgs e)
        {
            Close();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!IsItSaveToDestroyProject())
            {
                e.Cancel = true;
            }
        }

        private void OnOpenClick(object sender, EventArgs e)
        {
            if (IsItSaveToDestroyProject())
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Title = "Open log";
                dialog.Filter = "FlySight files (*.fly)|*.fly";
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.ShowHelp = true;

                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        mJumpTree.Nodes.Clear();
                        LoadProject(dialog.FileName);
                        ProjectName = dialog.FileName;
                    }
                    catch (Exception ex)
                    {
                        ResetProject();
                        MessageBox.Show(this, ex.Message, "Error loading log", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SaveProject(string aPath)
        {
            using (FileStream file = new FileStream(aPath, FileMode.Create, FileAccess.Write))
            {
                BinaryWriter writer = new BinaryWriter(file);

                byte[] id = Encoding.ASCII.GetBytes("FlySight");
                writer.Write(id, 0, 8);
                writer.Write((byte)1);

                writer.Write(mEntries.Count);
                foreach (KeyValuePair<string, LogEntry> entry in mEntries)
                {
                    writer.Write(entry.Key);
                    writer.Write(entry.Value.ID);
                    entry.Value.Write(writer);
                }

                ProjectDirty = false;
            }
        }

        private void LoadProject(string aPath)
        {
            mEntries.Clear();
            using (FileStream file = new FileStream(aPath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader reader = new BinaryReader(file);

                byte[] id = reader.ReadBytes(8);
                int version = reader.ReadByte();

                int count = reader.ReadInt32();
                for (int i = 0; i < count; ++i)
                {
                    string name = reader.ReadString();
                    int type = reader.ReadInt32();

                    if (type == FlySight.kIID)
                    {
                        mEntries.Add(name, FlySight.Read(reader));
                    }
                    else
                    {
                        throw new Exception("unexpected data");
                    }
                }
            }

            ProjectDirty = false;

            mJumpTree.BeginUpdate();
            foreach (LogEntry entry in mEntries.Values)
            {
                AddEntryToTree(entry);
            }
            mJumpTree.Sort();
            mJumpTree.EndUpdate();
            mJumpTree.ExpandAll();
        }

        private void mGraphMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = mGraphMode.SelectedIndex;
            Graph.DisplayMode[] values = (Graph.DisplayMode[])Enum.GetValues(typeof(Graph.DisplayMode));
            mGraph.Mode = values[idx];
            UpdateGraphMode();
        }

        private void UpdateGraphMode()
        {
            if (mGraph.Mode == Graph.DisplayMode.GlideRatio)
            {
                mImperial.Hide();
                mMetric.Hide();
            }
            else
            {
                mImperial.Show();
                mMetric.Show();
                switch (mGraph.Mode)
                {
                    case Graph.DisplayMode.HorizontalVelocity:
                    case Graph.DisplayMode.VerticalVelocity:
                        mImperial.Text = "MPH";
                        mMetric.Text = "KMPH";
                        break;
                    case Graph.DisplayMode.Altitude:
                        mImperial.Text = "ft (x1000)";
                        mMetric.Text = "KM";
                        break;
                }
            }
        }

        private void OnUnitCheckedChanged(object sender, EventArgs e)
        {
            if (mImperial.Checked)
            {
                mGraph.Unit = Graph.Units.Imperial;
            }
            else
            {
                mGraph.Unit = Graph.Units.Metric;
            }
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
                ImportFiles(dialog.FileNames);
            }
        }
    }
}
