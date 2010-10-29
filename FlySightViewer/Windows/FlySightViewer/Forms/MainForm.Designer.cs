using FlySightLog.Source;
namespace FlySightLog
{
   partial class MainForm
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
         if(disposing && (components != null))
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
          System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
          this.splitContainer1 = new System.Windows.Forms.SplitContainer();
          this.MainMap = new FlySightLog.Map();
          this.menuStrip1 = new System.Windows.Forms.MenuStrip();
          this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
          this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
          this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
          this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
          this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
          this.saveAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
          this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
          this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
          this.splitContainer2 = new System.Windows.Forms.SplitContainer();
          this.mJumpTree = new System.Windows.Forms.TreeView();
          this.tabControl1 = new System.Windows.Forms.TabControl();
          this.tabPage1 = new System.Windows.Forms.TabPage();
          this.mAltitudeGraph = new FlySightLog.Source.Graph();
          this.mGraphMode = new System.Windows.Forms.ComboBox();
          this.mGraph = new FlySightLog.Source.Graph();
          this.tabPage2 = new System.Windows.Forms.TabPage();
          this.tabPage3 = new System.Windows.Forms.TabPage();
          this.mRawData = new System.Windows.Forms.DataGridView();
          this.splitContainer1.Panel1.SuspendLayout();
          this.splitContainer1.Panel2.SuspendLayout();
          this.splitContainer1.SuspendLayout();
          this.menuStrip1.SuspendLayout();
          this.splitContainer2.Panel1.SuspendLayout();
          this.splitContainer2.Panel2.SuspendLayout();
          this.splitContainer2.SuspendLayout();
          this.tabControl1.SuspendLayout();
          this.tabPage1.SuspendLayout();
          this.tabPage3.SuspendLayout();
          ((System.ComponentModel.ISupportInitialize)(this.mRawData)).BeginInit();
          this.SuspendLayout();
          // 
          // splitContainer1
          // 
          this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
          this.splitContainer1.Location = new System.Drawing.Point(0, 0);
          this.splitContainer1.Name = "splitContainer1";
          this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
          // 
          // splitContainer1.Panel1
          // 
          this.splitContainer1.Panel1.Controls.Add(this.MainMap);
          this.splitContainer1.Panel1.Controls.Add(this.menuStrip1);
          // 
          // splitContainer1.Panel2
          // 
          this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
          this.splitContainer1.Size = new System.Drawing.Size(891, 665);
          this.splitContainer1.SplitterDistance = 332;
          this.splitContainer1.TabIndex = 1;
          // 
          // MainMap
          // 
          this.MainMap.Bearing = 0F;
          this.MainMap.CanDragMap = true;
          this.MainMap.Dock = System.Windows.Forms.DockStyle.Fill;
          this.MainMap.LevelsKeepInMemmory = 5;
          this.MainMap.Location = new System.Drawing.Point(0, 24);
          this.MainMap.LogEntry = null;
          this.MainMap.MarkersEnabled = true;
          this.MainMap.MaxZoom = 17;
          this.MainMap.MinZoom = 2;
          this.MainMap.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionWithoutCenter;
          this.MainMap.Name = "MainMap";
          this.MainMap.PolygonsEnabled = true;
          this.MainMap.Position = ((GMap.NET.PointLatLng)(resources.GetObject("MainMap.Position")));
          this.MainMap.RetryLoadTile = 0;
          this.MainMap.RoutesEnabled = true;
          this.MainMap.ShowTileGridLines = false;
          this.MainMap.Size = new System.Drawing.Size(891, 308);
          this.MainMap.TabIndex = 1;
          this.MainMap.Zoom = 0D;
          // 
          // menuStrip1
          // 
          this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
          this.menuStrip1.Location = new System.Drawing.Point(0, 0);
          this.menuStrip1.Name = "menuStrip1";
          this.menuStrip1.Size = new System.Drawing.Size(891, 24);
          this.menuStrip1.TabIndex = 2;
          this.menuStrip1.Text = "menuStrip1";
          // 
          // fileToolStripMenuItem
          // 
          this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newToolStripMenuItem,
            this.openToolStripMenuItem,
            this.toolStripMenuItem1,
            this.saveToolStripMenuItem,
            this.saveAsToolStripMenuItem,
            this.toolStripMenuItem2,
            this.exitToolStripMenuItem});
          this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
          this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
          this.fileToolStripMenuItem.Text = "&File";
          // 
          // newToolStripMenuItem
          // 
          this.newToolStripMenuItem.Name = "newToolStripMenuItem";
          this.newToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
          this.newToolStripMenuItem.Text = "New";
          this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
          // 
          // openToolStripMenuItem
          // 
          this.openToolStripMenuItem.Name = "openToolStripMenuItem";
          this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
          this.openToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
          this.openToolStripMenuItem.Text = "Open...";
          this.openToolStripMenuItem.Click += new System.EventHandler(this.OnOpenClick);
          // 
          // toolStripMenuItem1
          // 
          this.toolStripMenuItem1.Name = "toolStripMenuItem1";
          this.toolStripMenuItem1.Size = new System.Drawing.Size(190, 6);
          // 
          // saveToolStripMenuItem
          // 
          this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
          this.saveToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
          this.saveToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
          this.saveToolStripMenuItem.Text = "Save...";
          this.saveToolStripMenuItem.Click += new System.EventHandler(this.OnSaveClick);
          // 
          // saveAsToolStripMenuItem
          // 
          this.saveAsToolStripMenuItem.Name = "saveAsToolStripMenuItem";
          this.saveAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
                      | System.Windows.Forms.Keys.S)));
          this.saveAsToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
          this.saveAsToolStripMenuItem.Text = "Save as...";
          this.saveAsToolStripMenuItem.Click += new System.EventHandler(this.OnSaveAsClick);
          // 
          // toolStripMenuItem2
          // 
          this.toolStripMenuItem2.Name = "toolStripMenuItem2";
          this.toolStripMenuItem2.Size = new System.Drawing.Size(190, 6);
          // 
          // exitToolStripMenuItem
          // 
          this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
          this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
          this.exitToolStripMenuItem.Size = new System.Drawing.Size(193, 22);
          this.exitToolStripMenuItem.Text = "Exit";
          this.exitToolStripMenuItem.Click += new System.EventHandler(this.OnExit);
          // 
          // splitContainer2
          // 
          this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
          this.splitContainer2.Location = new System.Drawing.Point(0, 0);
          this.splitContainer2.Name = "splitContainer2";
          // 
          // splitContainer2.Panel1
          // 
          this.splitContainer2.Panel1.Controls.Add(this.mJumpTree);
          // 
          // splitContainer2.Panel2
          // 
          this.splitContainer2.Panel2.Controls.Add(this.tabControl1);
          this.splitContainer2.Size = new System.Drawing.Size(891, 329);
          this.splitContainer2.SplitterDistance = 224;
          this.splitContainer2.TabIndex = 0;
          // 
          // mJumpTree
          // 
          this.mJumpTree.AllowDrop = true;
          this.mJumpTree.Dock = System.Windows.Forms.DockStyle.Fill;
          this.mJumpTree.Location = new System.Drawing.Point(0, 0);
          this.mJumpTree.Name = "mJumpTree";
          this.mJumpTree.Size = new System.Drawing.Size(224, 329);
          this.mJumpTree.TabIndex = 0;
          this.mJumpTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.mJumpTree_AfterSelect);
          this.mJumpTree.DragDrop += new System.Windows.Forms.DragEventHandler(this.mJumpTree_DragDrop);
          this.mJumpTree.DragEnter += new System.Windows.Forms.DragEventHandler(this.mJumpTree_DragEnter);
          // 
          // tabControl1
          // 
          this.tabControl1.Controls.Add(this.tabPage1);
          this.tabControl1.Controls.Add(this.tabPage2);
          this.tabControl1.Controls.Add(this.tabPage3);
          this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
          this.tabControl1.Location = new System.Drawing.Point(0, 0);
          this.tabControl1.Name = "tabControl1";
          this.tabControl1.SelectedIndex = 0;
          this.tabControl1.Size = new System.Drawing.Size(663, 329);
          this.tabControl1.TabIndex = 0;
          // 
          // tabPage1
          // 
          this.tabPage1.Controls.Add(this.mAltitudeGraph);
          this.tabPage1.Controls.Add(this.mGraphMode);
          this.tabPage1.Controls.Add(this.mGraph);
          this.tabPage1.Location = new System.Drawing.Point(4, 22);
          this.tabPage1.Name = "tabPage1";
          this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
          this.tabPage1.Size = new System.Drawing.Size(655, 303);
          this.tabPage1.TabIndex = 0;
          this.tabPage1.Text = "Graphs";
          this.tabPage1.UseVisualStyleBackColor = true;
          // 
          // mAltitudeGraph
          // 
          this.mAltitudeGraph.AllowSelect = false;
          this.mAltitudeGraph.BackColor = System.Drawing.Color.Lavender;
          this.mAltitudeGraph.Dock = System.Windows.Forms.DockStyle.Bottom;
          this.mAltitudeGraph.Location = new System.Drawing.Point(3, 239);
          this.mAltitudeGraph.LogEntry = null;
          this.mAltitudeGraph.Mode = FlySightLog.Source.Graph.DisplayMode.Altitude;
          this.mAltitudeGraph.Name = "mAltitudeGraph";
          this.mAltitudeGraph.Size = new System.Drawing.Size(649, 61);
          this.mAltitudeGraph.TabIndex = 2;
          // 
          // mGraphMode
          // 
          this.mGraphMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
          this.mGraphMode.FormattingEnabled = true;
          this.mGraphMode.Location = new System.Drawing.Point(6, 6);
          this.mGraphMode.Name = "mGraphMode";
          this.mGraphMode.Size = new System.Drawing.Size(121, 21);
          this.mGraphMode.TabIndex = 1;
          this.mGraphMode.SelectedIndexChanged += new System.EventHandler(this.mGraphMode_SelectedIndexChanged);
          // 
          // mGraph
          // 
          this.mGraph.AllowSelect = false;
          this.mGraph.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                      | System.Windows.Forms.AnchorStyles.Left)
                      | System.Windows.Forms.AnchorStyles.Right)));
          this.mGraph.BackColor = System.Drawing.Color.Lavender;
          this.mGraph.Location = new System.Drawing.Point(3, 33);
          this.mGraph.LogEntry = null;
          this.mGraph.Mode = FlySightLog.Source.Graph.DisplayMode.Altitude;
          this.mGraph.Name = "mGraph";
          this.mGraph.Size = new System.Drawing.Size(649, 200);
          this.mGraph.TabIndex = 0;
          // 
          // tabPage2
          // 
          this.tabPage2.Location = new System.Drawing.Point(4, 22);
          this.tabPage2.Name = "tabPage2";
          this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
          this.tabPage2.Size = new System.Drawing.Size(655, 303);
          this.tabPage2.TabIndex = 1;
          this.tabPage2.Text = "Jump info";
          this.tabPage2.UseVisualStyleBackColor = true;
          // 
          // tabPage3
          // 
          this.tabPage3.Controls.Add(this.mRawData);
          this.tabPage3.Location = new System.Drawing.Point(4, 22);
          this.tabPage3.Name = "tabPage3";
          this.tabPage3.Size = new System.Drawing.Size(655, 303);
          this.tabPage3.TabIndex = 2;
          this.tabPage3.Text = "Raw data";
          this.tabPage3.UseVisualStyleBackColor = true;
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
          this.mRawData.Size = new System.Drawing.Size(655, 303);
          this.mRawData.TabIndex = 0;
          // 
          // MainForm
          // 
          this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
          this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
          this.BackColor = System.Drawing.Color.AliceBlue;
          this.ClientSize = new System.Drawing.Size(891, 665);
          this.Controls.Add(this.splitContainer1);
          this.KeyPreview = true;
          this.MainMenuStrip = this.menuStrip1;
          this.MinimumSize = new System.Drawing.Size(554, 107);
          this.Name = "MainForm";
          this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
          this.Text = "FlySight Viewer";
          this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
          this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing);
          this.splitContainer1.Panel1.ResumeLayout(false);
          this.splitContainer1.Panel1.PerformLayout();
          this.splitContainer1.Panel2.ResumeLayout(false);
          this.splitContainer1.ResumeLayout(false);
          this.menuStrip1.ResumeLayout(false);
          this.menuStrip1.PerformLayout();
          this.splitContainer2.Panel1.ResumeLayout(false);
          this.splitContainer2.Panel2.ResumeLayout(false);
          this.splitContainer2.ResumeLayout(false);
          this.tabControl1.ResumeLayout(false);
          this.tabPage1.ResumeLayout(false);
          this.tabPage3.ResumeLayout(false);
          ((System.ComponentModel.ISupportInitialize)(this.mRawData)).EndInit();
          this.ResumeLayout(false);

      }

      #endregion

      private System.Windows.Forms.SplitContainer splitContainer1;
      internal Map MainMap;
      private System.Windows.Forms.SplitContainer splitContainer2;
      private System.Windows.Forms.TreeView mJumpTree;
      private System.Windows.Forms.MenuStrip menuStrip1;
      private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
      private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
      private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
      private System.Windows.Forms.ToolStripMenuItem saveAsToolStripMenuItem;
      private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
      private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
      private System.Windows.Forms.TabControl tabControl1;
      private System.Windows.Forms.TabPage tabPage1;
      private System.Windows.Forms.TabPage tabPage2;
      private System.Windows.Forms.TabPage tabPage3;
      private System.Windows.Forms.DataGridView mRawData;
      private Graph mGraph;
      private System.Windows.Forms.ComboBox mGraphMode;
      private Graph mAltitudeGraph;

   }
}

