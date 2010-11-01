namespace FlySightViewer.Forms
{
    partial class DataForm
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
            this.mRawData = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.mRawData)).BeginInit();
            this.SuspendLayout();
            // 
            // mRawData
            // 
            this.mRawData.AllowUserToAddRows = false;
            this.mRawData.AllowUserToOrderColumns = true;
            this.mRawData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.mRawData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mRawData.Location = new System.Drawing.Point(0, 0);
            this.mRawData.Name = "mRawData";
            this.mRawData.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.mRawData.Size = new System.Drawing.Size(601, 423);
            this.mRawData.TabIndex = 1;
            // 
            // DataForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(601, 423);
            this.Controls.Add(this.mRawData);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HideOnClose = true;
            this.Name = "DataForm";
            this.Text = "GPS Data";
            ((System.ComponentModel.ISupportInitialize)(this.mRawData)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView mRawData;
    }
}