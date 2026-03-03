/*
*   This file is part of NSMB Editor 5.
*
*   NSMB Editor 5 is free software: you can redistribute it and/or modify
*   it under the terms of the GNU General Public License as published by
*   the Free Software Foundation, either version 3 of the License, or
*   (at your option) any later version.
*
*   NSMB Editor 5 is distributed in the hope that it will be useful,
*   but WITHOUT ANY WARRANTY; without even the implied warranty of
*   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*   GNU General Public License for more details.
*
*   You should have received a copy of the GNU General Public License
*   along with NSMB Editor 5.  If not, see <http://www.gnu.org/licenses/>.
*/

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.IO;
using System.Windows.Forms;
using DSFile = NSMBe5.DSFileSystem.File;
using IOFile = System.IO.File;

namespace NSMBe5.TilemapEditor
{
    public partial class TilemapEditor : UserControl
    {
        Tilemap t;

        ToolStripButton[] buttons;
        private DSFile backgroundGraphicsFile;
        private DSFile backgroundPaletteFile;
        private DSFile backgroundLayoutFile;
        private GroupBox backgroundFilesGroup;
        private Label graphicsFileLabel;
        private Label paletteFileLabel;
        private Label layoutFileLabel;
        private Button exportBackgroundFilesButton;
        private Button importBackgroundFilesButton;

        public TilemapEditor()
        {
            InitializeComponent();
            LanguageManager.ApplyToContainer(this, "TilemapEditor");
            buttons = new ToolStripButton[] { drawToolButton, xFlipToolButton, yFlipToolButton, copyToolButton, pasteToolButton, changePalToolButton };
            tilemapEditorControl1.ZoomChanged += tilemapEditorControl1_ZoomChanged;
            InitializeBackgroundInfoPanel();
            UpdateZoomButtons();
        }

        public void load(Tilemap t)
        {
            this.t = t;
            if (t.buffers == null) t.render();
            tilePicker1.init(t.buffers, 8);
            tilemapEditorControl1.picker = tilePicker1;
            tilemapEditorControl1.undobutton = undoButton;
            tilemapEditorControl1.redobutton = redoButton;
            tilemapEditorControl1.ed = this;
            tilemapEditorControl1.load(t);
            tilePicker1.SetZoom(tilemapEditorControl1.zoomLevel);
            RefreshCanvasPanelWidth();
            LayoutBackgroundInfoPanel();
            UpdateZoomButtons();
        }

        public void SetBackgroundFiles(DSFile graphicsFile, DSFile paletteFile, DSFile layoutFile)
        {
            backgroundGraphicsFile = graphicsFile;
            backgroundPaletteFile = paletteFile;
            backgroundLayoutFile = layoutFile;
            UpdateBackgroundFileInfo();
        }

        public void reload()
        {
            tilePicker1.init(t.buffers, 8);
            tilePicker1.SetZoom(tilemapEditorControl1.zoomLevel);
            LayoutBackgroundInfoPanel();
        }

        public void setMode(TilemapEditorControl.EditionMode mode)
        {
            buttons[(int)mode].PerformClick();
        }

        private void uncheckButtons()
        {
            foreach (ToolStripButton button in buttons)
                button.Checked = false;
        }

        private void drawToolButton_Click(object sender, EventArgs e)
        {
            tilemapEditorControl1.mode = TilemapEditorControl.EditionMode.DRAW;
            uncheckButtons();
            drawToolButton.Checked = true;
        }

        private void xFlipToolButton_Click(object sender, EventArgs e)
        {
            tilemapEditorControl1.mode = TilemapEditorControl.EditionMode.XFLIP;
            uncheckButtons();
            xFlipToolButton.Checked = true;
        }

        private void yFlipToolButton_Click(object sender, EventArgs e)
        {
            tilemapEditorControl1.mode = TilemapEditorControl.EditionMode.YFLIP;
            uncheckButtons();
            yFlipToolButton.Checked = true;
        }

        private void copyToolButton_Click(object sender, EventArgs e)
        {
            tilemapEditorControl1.mode = TilemapEditorControl.EditionMode.COPY;
            uncheckButtons();
            copyToolButton.Checked = true;
        }

        private void pasteToolButton_Click(object sender, EventArgs e)
        {
            tilemapEditorControl1.mode = TilemapEditorControl.EditionMode.PASTE;
            uncheckButtons();
            pasteToolButton.Checked = true;
        }
        private void changePalToolButton_Click(object sender, EventArgs e)
        {
            tilemapEditorControl1.mode = TilemapEditorControl.EditionMode.CHANGEPAL;
            uncheckButtons();
            changePalToolButton.Checked = true;
        }

        public void showSaveButton()
        {
            saveButton.Visible = true;
            toolStripSeparator1.Visible = true;
        }
        private void saveButton_Click(object sender, EventArgs e)
        {
            t.save();
        }

        private void tilemapEditorControl1_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void gridButton_CheckStateChanged(object sender, EventArgs e)
        {
            tilemapEditorControl1.showGrid = gridButton.Checked;
            tilemapEditorControl1.Invalidate(true);
        }

        private void zoomInButton_Click(object sender, EventArgs e)
        {
            tilemapEditorControl1.ZoomIn();
            UpdateZoomButtons();
        }

        private void zoomActualSizeButton_Click(object sender, EventArgs e)
        {
            tilemapEditorControl1.ZoomActualSize();
            UpdateZoomButtons();
        }

        private void zoomOutButton_Click(object sender, EventArgs e)
        {
            tilemapEditorControl1.ZoomOut();
            UpdateZoomButtons();
        }

        private void tilemapEditorControl1_ZoomChanged(object sender, EventArgs e)
        {
            tilePicker1.SetZoom(tilemapEditorControl1.zoomLevel);
            RefreshCanvasPanelWidth();
            LayoutBackgroundInfoPanel();
            UpdateZoomButtons();
        }

        private void UpdateZoomButtons()
        {
            zoomInButton.Enabled = tilemapEditorControl1.CanZoomIn;
            zoomOutButton.Enabled = tilemapEditorControl1.CanZoomOut;
            zoomActualSizeButton.Enabled = !tilemapEditorControl1.IsActualZoom;
        }

        private void RefreshCanvasPanelWidth()
        {
            panel1.Width = tilemapEditorControl1.Width + 30;
        }

        private void InitializeBackgroundInfoPanel()
        {
            backgroundFilesGroup = new GroupBox();
            backgroundFilesGroup.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            backgroundFilesGroup.Location = new Point(8, 168);
            backgroundFilesGroup.Size = new Size(Math.Max(220, panel2.Width - 16), 156);
            backgroundFilesGroup.Text = LanguageManager.Get("TilemapEditor", "bgFilesGroup");
            backgroundFilesGroup.Visible = false;

            graphicsFileLabel = new Label();
            graphicsFileLabel.AutoEllipsis = true;
            graphicsFileLabel.Location = new Point(10, 24);
            graphicsFileLabel.Size = new Size(backgroundFilesGroup.Width - 20, 20);
            graphicsFileLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            paletteFileLabel = new Label();
            paletteFileLabel.AutoEllipsis = true;
            paletteFileLabel.Location = new Point(10, 47);
            paletteFileLabel.Size = new Size(backgroundFilesGroup.Width - 20, 20);
            paletteFileLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            layoutFileLabel = new Label();
            layoutFileLabel.AutoEllipsis = true;
            layoutFileLabel.Location = new Point(10, 70);
            layoutFileLabel.Size = new Size(backgroundFilesGroup.Width - 20, 20);
            layoutFileLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            exportBackgroundFilesButton = new Button();
            exportBackgroundFilesButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            exportBackgroundFilesButton.Location = new Point(10, 95);
            exportBackgroundFilesButton.Size = new Size(backgroundFilesGroup.Width - 20, 24);
            exportBackgroundFilesButton.Text = LanguageManager.Get("TilemapEditor", "bgExportAllButton");
            exportBackgroundFilesButton.Click += exportBackgroundFilesButton_Click;

            importBackgroundFilesButton = new Button();
            importBackgroundFilesButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            importBackgroundFilesButton.Location = new Point(10, 120);
            importBackgroundFilesButton.Size = new Size(backgroundFilesGroup.Width - 20, 24);
            importBackgroundFilesButton.Text = LanguageManager.Get("TilemapEditor", "bgImportAllButton");
            importBackgroundFilesButton.Click += importBackgroundFilesButton_Click;

            backgroundFilesGroup.Controls.Add(graphicsFileLabel);
            backgroundFilesGroup.Controls.Add(paletteFileLabel);
            backgroundFilesGroup.Controls.Add(layoutFileLabel);
            backgroundFilesGroup.Controls.Add(exportBackgroundFilesButton);
            backgroundFilesGroup.Controls.Add(importBackgroundFilesButton);
            panel2.Controls.Add(backgroundFilesGroup);
            panel2.Resize += panel2_Resize;
        }

        private void panel2_Resize(object sender, EventArgs e)
        {
            if (backgroundFilesGroup == null)
                return;

            backgroundFilesGroup.Width = Math.Max(220, panel2.Width - 16);
            int contentWidth = backgroundFilesGroup.Width - 20;
            graphicsFileLabel.Width = contentWidth;
            paletteFileLabel.Width = contentWidth;
            layoutFileLabel.Width = contentWidth;
            exportBackgroundFilesButton.Width = contentWidth;
            importBackgroundFilesButton.Width = contentWidth;
            LayoutBackgroundInfoPanel();
        }

        private void LayoutBackgroundInfoPanel()
        {
            if (backgroundFilesGroup == null)
                return;

            int y = tilePicker1.Bottom + 8;
            if (y < 8)
                y = 8;
            backgroundFilesGroup.Location = new Point(8, y);
        }

        private void UpdateBackgroundFileInfo()
        {
            bool hasFiles = backgroundGraphicsFile != null && backgroundPaletteFile != null && backgroundLayoutFile != null;
            backgroundFilesGroup.Visible = hasFiles;
            if (!hasFiles)
                return;

            graphicsFileLabel.Text = string.Format("{0} {1}", LanguageManager.Get("TilemapEditor", "graphicsFileLabel"), backgroundGraphicsFile.name);
            paletteFileLabel.Text = string.Format("{0} {1}", LanguageManager.Get("TilemapEditor", "paletteFileLabel"), backgroundPaletteFile.name);
            layoutFileLabel.Text = string.Format("{0} {1}", LanguageManager.Get("TilemapEditor", "layoutFileLabel"), backgroundLayoutFile.name);
        }

        private void exportBackgroundFilesButton_Click(object sender, EventArgs e)
        {
            if (!CanUseBackgroundFiles())
                return;

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                WriteFileToDirectory(backgroundGraphicsFile, dialog.SelectedPath);
                WriteFileToDirectory(backgroundPaletteFile, dialog.SelectedPath);
                WriteFileToDirectory(backgroundLayoutFile, dialog.SelectedPath);
            }
        }

        private void importBackgroundFilesButton_Click(object sender, EventArgs e)
        {
            if (!CanUseBackgroundFiles())
                return;

            string ncgPath = PromptBackgroundImportFile("_ncg");
            if (ncgPath == null) return;
            string nclPath = PromptBackgroundImportFile("_ncl");
            if (nclPath == null) return;
            string nscPath = PromptBackgroundImportFile("_nsc");
            if (nscPath == null) return;

            try
            {
                ReplaceBackgroundFile(backgroundGraphicsFile, IOFile.ReadAllBytes(ncgPath));
                ReplaceBackgroundFile(backgroundPaletteFile, IOFile.ReadAllBytes(nclPath));
                ReplaceBackgroundFile(backgroundLayoutFile, IOFile.ReadAllBytes(nscPath));
                RefreshAfterBackgroundImport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, LanguageManager.Get("Errors", "errortitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool CanUseBackgroundFiles()
        {
            return backgroundGraphicsFile != null && backgroundPaletteFile != null && backgroundLayoutFile != null;
        }

        private static void WriteFileToDirectory(DSFile file, string directoryPath)
        {
            string outputPath = Path.Combine(directoryPath, file.name);
            IOFile.WriteAllBytes(outputPath, file.getContents());
        }

        private string PromptBackgroundImportFile(string suffix)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.CheckFileExists = true;
                dialog.Filter = LanguageManager.Get("Filters", "all");
                dialog.Title = string.Format("{0} ({1})", LanguageManager.Get("TilemapEditor", "bgImportAllButton"), suffix);
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return null;

                return dialog.FileName;
            }
        }

        private void ReplaceBackgroundFile(DSFile targetFile, byte[] newContents)
        {
            if (targetFile.beingEditedBy(t))
            {
                targetFile.replace(newContents, t);
                return;
            }

            targetFile.beginEdit(this);
            try
            {
                targetFile.replace(newContents, this);
            }
            finally
            {
                if (targetFile.beingEditedBy(this))
                    targetFile.endEdit(this);
            }
        }

        private void RefreshAfterBackgroundImport()
        {
            t.ReloadResources(true, true, true);
            tilemapEditorControl1.ReloadFromTilemap();
            tilePicker1.init(t.buffers, 8);
            tilePicker1.SetZoom(tilemapEditorControl1.zoomLevel);
            RefreshCanvasPanelWidth();
            tilemapEditorControl1.Invalidate(true);
            tilePicker1.Invalidate(true);
        }
    }
}
