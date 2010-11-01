using FlySightViewer.WinFormsUI.Docking;
using System.Windows.Forms;
using System;
using System.Collections.Generic;

namespace FlySightViewer.Forms
{
    public partial class JumpForm : DockContent
    {
        private LogEntry mSelectedEntry;

        public event EventHandler SelectedEntryChanged;

        public JumpForm()
        {
            InitializeComponent();
            Project.ProjectEntriesChanged += new EventHandler(Project_ProjectEntriesChanged);
        }

        public LogEntry SelectedEntry
        {
            get { return mSelectedEntry; }
            set
            {
                if (!object.ReferenceEquals(mSelectedEntry, value))
                {
                    mSelectedEntry = value;
                    if (SelectedEntryChanged != null)
                    {
                        SelectedEntryChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        private void Project_ProjectEntriesChanged(object sender, EventArgs e)
        {
            HashSet<LogEntry> entries = new HashSet<LogEntry>();
            mJumpTree.BeginUpdate();

            // add 'new' nodes.
            foreach (LogEntry entry in Project.Entries)
            {
                entries.Add(entry);
                AddEntryToTree(entry);
            }

            // remove 'old' nodes.
            RemoveOld(mJumpTree.Nodes, entries);

            // sort.
            mJumpTree.Sort();
            mJumpTree.EndUpdate();

            // update selection.
            if (!entries.Contains(SelectedEntry))
            {
                SelectedEntry = null;
            }
        }


        private void RemoveOld(TreeNodeCollection aNodes, HashSet<LogEntry> aEntries)
        {
            List<TreeNode> deleteList = new List<TreeNode>();
            foreach (TreeNode node in aNodes)
            {
                LogEntry entry = node.Tag as LogEntry;
                if (entry != null && !aEntries.Contains(entry))
                {
                    deleteList.Add(node);
                }
                else if (entry == null)
                {
                    RemoveOld(node.Nodes, aEntries);
                    if (node.Nodes.Count == 0)
                    {
                        deleteList.Add(node);
                    }
                }
            }
            foreach (TreeNode node in deleteList)
            {
                aNodes.Remove(node);
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
                Project.ImportFiles(files);
            }
        }

        private void mJumpTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            SelectedEntry = e.Node.Tag as LogEntry;
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
                TreeNode node =  aNodes.Add(aValue, aValue);
                node.ExpandAll();
                return node;
            }
        }
    }
}
