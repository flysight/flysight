namespace FlySightViewer.Forms
{
    partial class GpsDownloadForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose (bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose ();
            }
            base.Dispose (disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent ()
        {
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonDownload = new System.Windows.Forms.Button();
            this.comboBoxInputPort = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBoxSourceType = new System.Windows.Forms.ComboBox();
            this.groupBoxGpsSource = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBoxGpsSource.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(356, 101);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(88, 23);
            this.buttonCancel.TabIndex = 2;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonDownload
            // 
            this.buttonDownload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDownload.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonDownload.Location = new System.Drawing.Point(262, 101);
            this.buttonDownload.Name = "buttonDownload";
            this.buttonDownload.Size = new System.Drawing.Size(88, 23);
            this.buttonDownload.TabIndex = 1;
            this.buttonDownload.Text = "Download";
            this.buttonDownload.UseVisualStyleBackColor = true;
            // 
            // comboBoxInputPort
            // 
            this.comboBoxInputPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxInputPort.FormattingEnabled = true;
            this.comboBoxInputPort.Location = new System.Drawing.Point(84, 50);
            this.comboBoxInputPort.Name = "comboBoxInputPort";
            this.comboBoxInputPort.Size = new System.Drawing.Size(127, 21);
            this.comboBoxInputPort.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(55, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Input port:";
            // 
            // comboBoxSourceType
            // 
            this.comboBoxSourceType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxSourceType.FormattingEnabled = true;
            this.comboBoxSourceType.Location = new System.Drawing.Point(84, 23);
            this.comboBoxSourceType.Name = "comboBoxSourceType";
            this.comboBoxSourceType.Size = new System.Drawing.Size(276, 21);
            this.comboBoxSourceType.TabIndex = 1;
            this.comboBoxSourceType.SelectionChangeCommitted += new System.EventHandler(this.comboBoxSourceType_SelectionChangeCommitted);
            // 
            // groupBoxGpsSource
            // 
            this.groupBoxGpsSource.Controls.Add(this.comboBoxInputPort);
            this.groupBoxGpsSource.Controls.Add(this.label2);
            this.groupBoxGpsSource.Controls.Add(this.comboBoxSourceType);
            this.groupBoxGpsSource.Controls.Add(this.label1);
            this.groupBoxGpsSource.Location = new System.Drawing.Point(12, 12);
            this.groupBoxGpsSource.Name = "groupBoxGpsSource";
            this.groupBoxGpsSource.Size = new System.Drawing.Size(432, 81);
            this.groupBoxGpsSource.TabIndex = 3;
            this.groupBoxGpsSource.TabStop = false;
            this.groupBoxGpsSource.Text = "GPS source";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Source type:";
            // 
            // GpsDownloadForm
            // 
            this.AcceptButton = this.buttonDownload;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(456, 136);
            this.Controls.Add(this.groupBoxGpsSource);
            this.Controls.Add(this.buttonDownload);
            this.Controls.Add(this.buttonCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "GpsDownloadForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Download GPS Data";
            this.groupBoxGpsSource.ResumeLayout(false);
            this.groupBoxGpsSource.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonDownload;
        private System.Windows.Forms.ComboBox comboBoxInputPort;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBoxSourceType;
        private System.Windows.Forms.GroupBox groupBoxGpsSource;
        private System.Windows.Forms.Label label1;
    }
}