namespace FlySightViewer.Forms
{
    partial class MapForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MapForm));
            this.mMap = new FlySightViewer.Controls.Map();
            this.SuspendLayout();
            // 
            // MainMap
            // 
            this.mMap.Bearing = 0F;
            this.mMap.CanDragMap = true;
            this.mMap.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mMap.LevelsKeepInMemmory = 5;
            this.mMap.Location = new System.Drawing.Point(0, 0);
            this.mMap.LogEntry = null;
            this.mMap.MaxZoom = 17;
            this.mMap.MinZoom = 2;
            this.mMap.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionWithoutCenter;
            this.mMap.Name = "MainMap";
            this.mMap.RetryLoadTile = 0;
            this.mMap.ShowTileGridLines = false;
            this.mMap.Size = new System.Drawing.Size(481, 383);
            this.mMap.TabIndex = 2;
            this.mMap.Zoom = 0D;
            // 
            // MapForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(481, 383);
            this.Controls.Add(this.mMap);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.HideOnClose = true;
            this.Name = "MapForm";
            this.Text = "Google Maps";
            this.ResumeLayout(false);

        }

        #endregion

        private FlySightViewer.Controls.Map mMap;
    }
}