namespace FlySightViewer.Forms
{
    partial class SettingsDialog
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.mBabelPath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.mBrowse = new System.Windows.Forms.Button();
            this.mCancel = new System.Windows.Forms.Button();
            this.mOK = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.mBabelPath);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.mBrowse);
            this.groupBox1.Location = new System.Drawing.Point(13, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(423, 57);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "GPS Babel";
            // 
            // mBabelPath
            // 
            this.mBabelPath.Location = new System.Drawing.Point(60, 20);
            this.mBabelPath.Name = "mBabelPath";
            this.mBabelPath.Size = new System.Drawing.Size(325, 20);
            this.mBabelPath.TabIndex = 2;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Location";
            // 
            // mBrowse
            // 
            this.mBrowse.Location = new System.Drawing.Point(391, 18);
            this.mBrowse.Name = "mBrowse";
            this.mBrowse.Size = new System.Drawing.Size(26, 23);
            this.mBrowse.TabIndex = 0;
            this.mBrowse.Text = "...";
            this.mBrowse.UseVisualStyleBackColor = true;
            this.mBrowse.Click += new System.EventHandler(this.mBrowse_Click);
            // 
            // mCancel
            // 
            this.mCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.mCancel.Location = new System.Drawing.Point(361, 76);
            this.mCancel.Name = "mCancel";
            this.mCancel.Size = new System.Drawing.Size(75, 23);
            this.mCancel.TabIndex = 1;
            this.mCancel.Text = "Cancel";
            this.mCancel.UseVisualStyleBackColor = true;
            // 
            // mOK
            // 
            this.mOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.mOK.Location = new System.Drawing.Point(280, 76);
            this.mOK.Name = "mOK";
            this.mOK.Size = new System.Drawing.Size(75, 23);
            this.mOK.TabIndex = 2;
            this.mOK.Text = "OK";
            this.mOK.UseVisualStyleBackColor = true;
            this.mOK.Click += new System.EventHandler(this.mOK_Click);
            // 
            // SettingsDialog
            // 
            this.AcceptButton = this.mOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.mCancel;
            this.ClientSize = new System.Drawing.Size(448, 110);
            this.Controls.Add(this.mOK);
            this.Controls.Add(this.mCancel);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "SettingsDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Preferences";
            this.Load += new System.EventHandler(this.SettingsDialog_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox mBabelPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button mBrowse;
        private System.Windows.Forms.Button mCancel;
        private System.Windows.Forms.Button mOK;
    }
}