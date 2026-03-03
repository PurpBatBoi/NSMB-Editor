namespace NSMBe5.TilemapEditor
{
    partial class TilemapEditorWindow
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
            this.tilemapEditor1 = new NSMBe5.TilemapEditor.TilemapEditor();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.filesStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tilemapEditor1
            // 
            this.tilemapEditor1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tilemapEditor1.Location = new System.Drawing.Point(0, 0);
            this.tilemapEditor1.Name = "tilemapEditor1";
            this.tilemapEditor1.Size = new System.Drawing.Size(921, 589);
            this.tilemapEditor1.TabIndex = 0;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.filesStatusLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 567);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(921, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            this.statusStrip1.Visible = false;
            // 
            // filesStatusLabel
            // 
            this.filesStatusLabel.Name = "filesStatusLabel";
            this.filesStatusLabel.Size = new System.Drawing.Size(39, 17);
            this.filesStatusLabel.Text = "<Files>";
            // 
            // TilemapEditorWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(921, 589);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.tilemapEditor1);
            this.Name = "TilemapEditorWindow";
            this.Text = "<Tilemap Editor>";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TilemapEditorWindow_FormClosing);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TilemapEditor tilemapEditor1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel filesStatusLabel;
    }
}
