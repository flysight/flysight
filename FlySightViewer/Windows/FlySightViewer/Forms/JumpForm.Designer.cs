namespace FlySightViewer.Forms
{
    partial class JumpForm
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
            this.mJumpTree = new System.Windows.Forms.TreeView();
            this.SuspendLayout();
            // 
            // mJumpTree
            // 
            this.mJumpTree.AllowDrop = true;
            this.mJumpTree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mJumpTree.HideSelection = false;
            this.mJumpTree.Location = new System.Drawing.Point(0, 0);
            this.mJumpTree.Name = "mJumpTree";
            this.mJumpTree.Size = new System.Drawing.Size(284, 581);
            this.mJumpTree.TabIndex = 0;
            this.mJumpTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.mJumpTree_AfterSelect);
            this.mJumpTree.DragDrop += new System.Windows.Forms.DragEventHandler(this.mJumpTree_DragDrop);
            this.mJumpTree.DragEnter += new System.Windows.Forms.DragEventHandler(this.mJumpTree_DragEnter);
            this.mJumpTree.MouseClick += new System.Windows.Forms.MouseEventHandler(this.mJumpTree_MouseClick);
            // 
            // JumpForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 581);
            this.Controls.Add(this.mJumpTree);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.HideOnClose = true;
            this.Name = "JumpForm";
            this.Text = "Jump Explorer";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView mJumpTree;
    }
}