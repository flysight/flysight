using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlySightViewer.Forms
{
    public partial class SettingsDialog : Form
    {
        public SettingsDialog()
        {
            InitializeComponent();
        }

        private void SettingsDialog_Load(object sender, EventArgs e)
        {
            mBabelPath.Text = Settings.Instance.GPSBabelPath;
        }

        private void mOK_Click(object sender, EventArgs e)
        {
            Settings.Instance.GPSBabelPath = mBabelPath.Text;
            Settings.Instance.Save();
        }

        private void mBrowse_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.ShowNewFolderButton = false;
            dlg.SelectedPath = mBabelPath.Text;
            dlg.Description = "Select the path where GPS Babel is installed. This is usually somewhere in \"C:\\Program Files\\GPS Babel\"";
            if (dlg.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                mBabelPath.Text = dlg.SelectedPath;
            }
        }
    }
}
