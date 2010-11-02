namespace FlySightViewer.Forms
{
    partial class DownloadDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mProgressbar = new System.Windows.Forms.ProgressBar();
            this.mInstall = new System.Windows.Forms.Button();
            this.mCancel = new System.Windows.Forms.Button();
            this.mProgressLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // mProgressbar
            // 
            this.mProgressbar.Location = new System.Drawing.Point(12, 12);
            this.mProgressbar.Name = "mProgressbar";
            this.mProgressbar.Size = new System.Drawing.Size(362, 23);
            this.mProgressbar.TabIndex = 0;
            // 
            // mInstall
            // 
            this.mInstall.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.mInstall.Enabled = false;
            this.mInstall.Location = new System.Drawing.Point(299, 41);
            this.mInstall.Name = "mInstall";
            this.mInstall.Size = new System.Drawing.Size(75, 23);
            this.mInstall.TabIndex = 1;
            this.mInstall.Text = "Install";
            this.mInstall.UseVisualStyleBackColor = true;
            // 
            // mCancel
            // 
            this.mCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.mCancel.Location = new System.Drawing.Point(218, 41);
            this.mCancel.Name = "mCancel";
            this.mCancel.Size = new System.Drawing.Size(75, 23);
            this.mCancel.TabIndex = 2;
            this.mCancel.Text = "Cancel";
            this.mCancel.UseVisualStyleBackColor = true;
            // 
            // mProgressLabel
            // 
            this.mProgressLabel.AutoSize = true;
            this.mProgressLabel.Location = new System.Drawing.Point(12, 46);
            this.mProgressLabel.Name = "mProgressLabel";
            this.mProgressLabel.Size = new System.Drawing.Size(51, 13);
            this.mProgressLabel.TabIndex = 3;
            this.mProgressLabel.Text = "Progress:";
            // 
            // DownloadDialog
            // 
            this.AcceptButton = this.mInstall;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.mCancel;
            this.ClientSize = new System.Drawing.Size(386, 76);
            this.Controls.Add(this.mProgressLabel);
            this.Controls.Add(this.mCancel);
            this.Controls.Add(this.mInstall);
            this.Controls.Add(this.mProgressbar);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "DownloadDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Downloading";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar mProgressbar;
        private System.Windows.Forms.Button mInstall;
        private System.Windows.Forms.Button mCancel;
        private System.Windows.Forms.Label mProgressLabel;
    }
}