using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace FlySightViewer.Forms
{
    public partial class DownloadDialog : Form
    {
        public DownloadDialog()
        {
            InitializeComponent();
        }

        public string DownloadInstaller(Control aOwner, Version aVersion)
        {
            WebClient Client = new WebClient();
            Client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(Client_DownloadProgressChanged);
            Client.DownloadFileCompleted += new AsyncCompletedEventHandler(Client_DownloadFileCompleted);

            string destinationPath = Path.Combine(Path.GetTempPath(), string.Format("FlySightViewer-{0}-setup.exe", aVersion));
            Uri downloadUrl = new Uri(string.Format("http://tomvandijck.com/flysight/FlySightViewer-{0}-setup.exe", aVersion));

            Client.DownloadFileAsync(downloadUrl, destinationPath);

            if (ShowDialog(aOwner) == System.Windows.Forms.DialogResult.OK)
            {
                return destinationPath;
            }

            Client.CancelAsync();
            return null;
        }

        void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            mInstall.Enabled = true;
        }

        void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            mProgressbar.Value = e.ProgressPercentage;
            mProgressLabel.Text = string.Format("Progress: {0}/{1}", e.BytesReceived, e.TotalBytesToReceive);
        }
    }
}
