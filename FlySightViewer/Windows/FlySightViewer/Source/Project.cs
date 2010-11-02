using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Brejc.GpsLibrary.Gpx;

namespace FlySightViewer
{
    public static class Project
    {
        private static string mProjectName = string.Empty;
        private static bool mProjectDirty = false;
        private static Dictionary<string, LogEntry> mEntries = new Dictionary<string, LogEntry>();

        public static event EventHandler ProjectNameChanged;
        public static event EventHandler ProjectDirtyChanged;
        public static event EventHandler ProjectEntriesChanged;

        #region -- Properties -------------------------------------------------

        public static string Name
        {
            get { return mProjectName; }
            private set
            {
                if (mProjectName != value)
                {
                    mProjectName = value;
                    FireNameChanged();
                }
            }
        }

        public static bool Dirty
        {
            get { return mProjectDirty; }
            private set
            {
                if (mProjectDirty != value)
                {
                    mProjectDirty = value;
                    FireDirtyChanged();
                }
            }
        }

        public static IEnumerable<LogEntry> Entries
        {
            get { return mEntries.Values; }
        }

        #endregion

        #region -- Event handlers ---------------------------------------------

        private static void FireNameChanged()
        {
            if (ProjectNameChanged != null)
            {
                ProjectNameChanged(null, EventArgs.Empty);
            }
        }

        private static void FireDirtyChanged()
        {
            if (ProjectDirtyChanged != null)
            {
                ProjectDirtyChanged(null, EventArgs.Empty);
            }
        }

        private static void FireEntriesChanged()
        {
            if (ProjectEntriesChanged != null)
            {
                ProjectEntriesChanged(null, EventArgs.Empty);
            }
        }

        #endregion

        #region -- Import methods ---------------------------------------------

        public static void ImportFiles(params string[] aPaths)
        {
            try
            {
                bool changed = false;
                foreach (string file in aPaths)
                {
                    if (File.Exists(file))
                    {
                        changed |= ImportFile(file);
                    }
                    else if (Directory.Exists(file))
                    {
                        changed |= ImportFolder(file);
                    }
                }
                if (changed)
                {
                    FireEntriesChanged();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Program.Form, ex.Message, "Error importing.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static bool ImportFolder(string aPath)
        {
            bool changed = false;
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles(aPath, "*.csv"));
            files.AddRange(Directory.GetFiles(aPath, "*.gpx"));
            foreach (string file in files)
            {
                changed |= ImportFile(file);
            }
            return changed;
        }

        private static bool ImportFile(string aPath)
        {
            string name = Path.GetFileNameWithoutExtension(aPath).ToLower();
            if (!mEntries.ContainsKey(name))
            {
                LogEntry entry;
                string ext = Path.GetExtension(aPath).ToLower();
                switch (ext)
                {
                    case ".csv":
                        entry = FlySight.Import(name, aPath);
                        AddEntry(name, entry);
                        return true;
                    case ".gpx":
                        GpxImporter.Import(name, aPath, AddEntry);
                        return true;
                    default:
                        MessageBox.Show(Program.Form, "Unsupported fileformat");
                        return false;
                }
            }
            return false;
        }

        private static void AddEntry(string aName, LogEntry aEntry)
        {
            if (aEntry != null)
            {
                Dirty = true;
                mEntries.Add(aName, aEntry);
            }
        }

        public static void DeleteEntry(LogEntry aEntry)
        {
            if (aEntry != null)
            {
                if (mEntries.Remove(aEntry.Key))
                {
                    Dirty = true;
                    FireEntriesChanged();
                }
            }
        }

        #endregion

        #region -- Save/Load/Reset --------------------------------------------

        public static void Reset()
        {
            mEntries.Clear();
            Name = string.Empty;
            Dirty = false;
            FireEntriesChanged();
        }

        public static void SaveProject(string aPath)
        {
            using (FileStream file = new FileStream(aPath, FileMode.Create, FileAccess.Write))
            {
                BinaryWriter writer = new BinaryWriter(file);

                byte[] id = Encoding.ASCII.GetBytes("FlySight");
                writer.Write(id, 0, 8);
                writer.Write((byte)2);

                writer.Write(mEntries.Count);
                foreach (KeyValuePair<string, LogEntry> entry in mEntries)
                {
                    writer.Write(entry.Key);
                    entry.Value.Write(writer);
                }

                Name = aPath;
                Dirty = false;
            }
        }

        public static void LoadProject(string aPath)
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
                    if (version < 2)
                    {
                        reader.ReadInt32();
                    }
                    mEntries.Add(name, LogEntry.Read(name, reader));
                }

                Name = aPath;
                Dirty = false;
                FireEntriesChanged();
            }
        }

        public static bool IsItSaveToDestroyProject()
        {
            if (mProjectDirty)
            {
                switch (MessageBox.Show(Program.Form, "Log has been modified but not saved yet.\nDo you want to save first?",
                    "Log not saved", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question))
                {
                    case DialogResult.Cancel:
                        return false;

                    case DialogResult.Yes:
                        OnSaveClick();
                        return !Dirty;

                    case DialogResult.No:
                        return true;
                }
            }
            return true;
        }

        #endregion

        #region -- Event helpers ----------------------------------------------

        public static void OnNewClick()
        {
            if (IsItSaveToDestroyProject())
            {
                Reset();
            }
        }

        public static void OnSaveClick()
        {
            if (string.IsNullOrEmpty(Name))
            {
                OnSaveAsClick();
            }
            else
            {
                SaveProject(Name);
            }
        }

        public static void OnSaveAsClick()
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Save log";
            dialog.Filter = "FlySight files (*.fly)|*.fly";
            dialog.CheckPathExists = true;
            dialog.AddExtension = true;
            dialog.ShowHelp = true;

            if (dialog.ShowDialog(Program.Form) == DialogResult.OK)
            {
                SaveProject(dialog.FileName);
            }
        }

        public static void OnOpenClick()
        {
            if (IsItSaveToDestroyProject())
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Title = "Open log";
                dialog.Filter = "FlySight files (*.fly)|*.fly";
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                dialog.ShowHelp = true;

                if (dialog.ShowDialog(Program.Form) == DialogResult.OK)
                {
                    try
                    {
                        LoadProject(dialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        Reset();
                        MessageBox.Show(Program.Form, ex.Message, "Error loading log", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        #endregion
    }
}
