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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DSFile = NSMBe5.DSFileSystem.File;
using IOFile = System.IO.File;

namespace NSMBe5.TilemapEditor
{
    public partial class TilemapEditor : UserControl
    {
        // ── Instance fields ─────────────────────────────────────────────────────
        private Tilemap _tilemap;
        private ToolStripButton[] _toolButtons;

        private DSFile _backgroundGraphicsFile;
        private DSFile _backgroundPaletteFile;
        private DSFile _backgroundLayoutFile;

        private ToolStripButton _randomizationCanvasButton;
        private ToolStripButton _randomizationColorButton;
        private HashSet<int> _canvasRandomizationTiles = new HashSet<int>();
        private bool _isCanvasRandomizationVisible;
        private Color _randomizationOutlineColor = Color.FromArgb(220, 255, 200, 0);

        private GroupBox _backgroundFilesGroup;
        private Label _graphicsFileLabel;
        private Label _paletteFileLabel;
        private Label _layoutFileLabel;
        private Button _exportBackgroundFilesButton;
        private Button _importBackgroundFilesButton;

        // ── Construction / initialisation ──────────────────────────────────────
        public TilemapEditor()
        {
            InitializeComponent();
            LanguageManager.ApplyToContainer(this, "TilemapEditor");

            _toolButtons = new[]
            {
                drawToolButton,
                xFlipToolButton,
                yFlipToolButton,
                copyToolButton,
                pasteToolButton,
                changePalToolButton
            };

            tilemapEditorControl1.ZoomChanged += TilemapEditorControl1_ZoomChanged;

            InitializeBackgroundInfoPanel();
            UpdateZoomButtons();
        }

        // ── Public API ─────────────────────────────────────────────────────────
        public event Action<bool> CanvasRandomizationVisibilityChanged;
        public event Action<Color> RandomizationOutlineColorChanged;

        public void LoadTilemap(Tilemap tilemap)
        {
            _tilemap = tilemap;
            if (_tilemap.buffers == null)
                _tilemap.render();

            tilePicker1.init(_tilemap.buffers, 8);
            tilemapEditorControl1.picker = tilePicker1;
            tilemapEditorControl1.undobutton = undoButton;
            tilemapEditorControl1.redobutton = redoButton;
            tilemapEditorControl1.ed = this;
            tilemapEditorControl1.load(_tilemap);
            tilePicker1.SetZoom(tilemapEditorControl1.zoomLevel);

            RefreshCanvasPanelWidth();
            LayoutBackgroundInfoPanel();
            UpdateZoomButtons();
        }

        public void Reload()
        {
            tilePicker1.init(_tilemap.buffers, 8);
            tilePicker1.SetZoom(tilemapEditorControl1.zoomLevel);
            LayoutBackgroundInfoPanel();
        }

        public void SetMode(TilemapEditorControl.EditionMode mode)
        {
            _toolButtons[(int)mode].PerformClick();
        }

        public void ShowSaveButton()
        {
            saveButton.Visible = true;
            toolStripSeparator1.Visible = true;
        }

        public void SetBackgroundFiles(DSFile graphicsFile, DSFile paletteFile, DSFile layoutFile)
        {
            _backgroundGraphicsFile = graphicsFile;
            _backgroundPaletteFile = paletteFile;
            _backgroundLayoutFile = layoutFile;

            UpdateBackgroundFileInfo();
        }

        public void ConfigureRandomizationToggleButtons(bool canvasVisible, Color outlineColor, string canvasText, string colorText)
        {
            EnsureRandomizationButtons();

            _randomizationCanvasButton.Text = canvasText;
            _randomizationCanvasButton.Checked = canvasVisible;
            _randomizationColorButton.Text = colorText;

            _isCanvasRandomizationVisible = canvasVisible;
            _randomizationOutlineColor = outlineColor;

            tilemapEditorControl1.SetHighlightOutlineColor(_randomizationOutlineColor);
            tilePicker1.SetHighlightVisible(false);
        }

        public void SetCanvasRandomizationOverlay(IEnumerable<int> tileNumbers, bool visible)
        {
            _canvasRandomizationTiles = tileNumbers == null
                ? new HashSet<int>()
                : new HashSet<int>(tileNumbers);

            _isCanvasRandomizationVisible = visible;

            tilemapEditorControl1.SetHighlightTiles(_canvasRandomizationTiles);
            tilemapEditorControl1.SetHighlightVisible(_isCanvasRandomizationVisible);
            tilePicker1.SetHighlightVisible(false);

            if (_randomizationCanvasButton != null)
                _randomizationCanvasButton.Checked = _isCanvasRandomizationVisible;
        }

        // ── Tool selection helpers ─────────────────────────────────────────────
        private void ClearToolButtonSelection()
        {
            foreach (ToolStripButton button in _toolButtons)
                button.Checked = false;
        }

        private void SelectMode(TilemapEditorControl.EditionMode mode, ToolStripButton selectedButton)
        {
            tilemapEditorControl1.mode = mode;
            ClearToolButtonSelection();
            selectedButton.Checked = true;
        }

        // ── Zoom helpers ───────────────────────────────────────────────────────
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

        // ── Randomization helpers ──────────────────────────────────────────────
        private void EnsureRandomizationButtons()
        {
            if (_randomizationCanvasButton != null && _randomizationColorButton != null)
                return;

            _randomizationCanvasButton = new ToolStripButton
            {
                CheckOnClick = true,
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Name = "randomizationCanvasButton"
            };
            _randomizationCanvasButton.Click += RandomizationCanvasButtonClickHandler;

            _randomizationColorButton = new ToolStripButton
            {
                DisplayStyle = ToolStripItemDisplayStyle.Text,
                Name = "randomizationColorButton"
            };
            _randomizationColorButton.Click += RandomizationColorButtonClickHandler;

            toolStrip1.Items.Add(new ToolStripSeparator());
            toolStrip1.Items.Add(_randomizationCanvasButton);
            toolStrip1.Items.Add(_randomizationColorButton);
        }

        // ── Background file helpers ────────────────────────────────────────────
        private void InitializeBackgroundInfoPanel()
        {
            _backgroundFilesGroup = new GroupBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(8, 168),
                Size = new Size(Math.Max(220, panel2.Width - 16), 156),
                Text = LanguageManager.Get("TilemapEditor", "bgFilesGroup"),
                Visible = false
            };

            _graphicsFileLabel = CreateBackgroundFileLabel(24);
            _paletteFileLabel = CreateBackgroundFileLabel(47);
            _layoutFileLabel = CreateBackgroundFileLabel(70);

            _exportBackgroundFilesButton = CreateBackgroundFileButton(
                y: 95,
                textKey: "bgExportAllButton",
                clickHandler: ExportBackgroundFilesButtonClickHandler);

            _importBackgroundFilesButton = CreateBackgroundFileButton(
                y: 120,
                textKey: "bgImportAllButton",
                clickHandler: ImportBackgroundFilesButtonClickHandler);

            _backgroundFilesGroup.Controls.Add(_graphicsFileLabel);
            _backgroundFilesGroup.Controls.Add(_paletteFileLabel);
            _backgroundFilesGroup.Controls.Add(_layoutFileLabel);
            _backgroundFilesGroup.Controls.Add(_exportBackgroundFilesButton);
            _backgroundFilesGroup.Controls.Add(_importBackgroundFilesButton);

            panel2.Controls.Add(_backgroundFilesGroup);
            panel2.Resize += Panel2ResizeHandler;
        }

        private Label CreateBackgroundFileLabel(int y)
        {
            return new Label
            {
                AutoEllipsis = true,
                Location = new Point(10, y),
                Size = new Size(_backgroundFilesGroup.Width - 20, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
        }

        private Button CreateBackgroundFileButton(int y, string textKey, EventHandler clickHandler)
        {
            Button button = new Button
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Location = new Point(10, y),
                Size = new Size(_backgroundFilesGroup.Width - 20, 24),
                Text = LanguageManager.Get("TilemapEditor", textKey)
            };

            button.Click += clickHandler;
            return button;
        }

        private void LayoutBackgroundInfoPanel()
        {
            if (_backgroundFilesGroup == null)
                return;

            int y = tilePicker1.Bottom + 8;
            if (y < 8)
                y = 8;

            _backgroundFilesGroup.Location = new Point(8, y);
        }

        private void UpdateBackgroundFileInfo()
        {
            bool hasFiles = CanUseBackgroundFiles();
            _backgroundFilesGroup.Visible = hasFiles;
            if (!hasFiles)
                return;

            _graphicsFileLabel.Text = $"{LanguageManager.Get("TilemapEditor", "graphicsFileLabel")} {_backgroundGraphicsFile.name}";
            _paletteFileLabel.Text = $"{LanguageManager.Get("TilemapEditor", "paletteFileLabel")} {_backgroundPaletteFile.name}";
            _layoutFileLabel.Text = $"{LanguageManager.Get("TilemapEditor", "layoutFileLabel")} {_backgroundLayoutFile.name}";
        }

        private bool CanUseBackgroundFiles()
        {
            return _backgroundGraphicsFile != null
                && _backgroundPaletteFile != null
                && _backgroundLayoutFile != null;
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
                dialog.Title = $"{LanguageManager.Get("TilemapEditor", "bgImportAllButton")} ({suffix})";

                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return null;

                return dialog.FileName;
            }
        }

        private void ReplaceBackgroundFile(DSFile targetFile, byte[] newContents)
        {
            if (targetFile.beingEditedBy(_tilemap))
            {
                targetFile.replace(newContents, _tilemap);
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
            _tilemap.ReloadResources(true, true, true);
            tilemapEditorControl1.ReloadFromTilemap();
            tilePicker1.init(_tilemap.buffers, 8);
            tilePicker1.SetZoom(tilemapEditorControl1.zoomLevel);

            RefreshCanvasPanelWidth();

            tilemapEditorControl1.Invalidate(true);
            tilePicker1.Invalidate(true);
        }

        // ── Event handlers ─────────────────────────────────────────────────────
        private void DrawToolButtonClickHandler(object sender, EventArgs e)
        {
            SelectMode(TilemapEditorControl.EditionMode.DRAW, drawToolButton);
        }

        private void XFlipToolButtonClickHandler(object sender, EventArgs e)
        {
            SelectMode(TilemapEditorControl.EditionMode.XFLIP, xFlipToolButton);
        }

        private void YFlipToolButtonClickHandler(object sender, EventArgs e)
        {
            SelectMode(TilemapEditorControl.EditionMode.YFLIP, yFlipToolButton);
        }

        private void CopyToolButtonClickHandler(object sender, EventArgs e)
        {
            SelectMode(TilemapEditorControl.EditionMode.COPY, copyToolButton);
        }

        private void PasteToolButtonClickHandler(object sender, EventArgs e)
        {
            SelectMode(TilemapEditorControl.EditionMode.PASTE, pasteToolButton);
        }

        private void ChangePalToolButtonClickHandler(object sender, EventArgs e)
        {
            SelectMode(TilemapEditorControl.EditionMode.CHANGEPAL, changePalToolButton);
        }

        private void SaveButtonClickHandler(object sender, EventArgs e)
        {
            _tilemap.save();
        }

        private void GridButtonCheckStateChangedHandler(object sender, EventArgs e)
        {
            tilemapEditorControl1.showGrid = gridButton.Checked;
            tilemapEditorControl1.Invalidate(true);
        }

        private void ZoomInButtonClickHandler(object sender, EventArgs e)
        {
            tilemapEditorControl1.ZoomIn();
            UpdateZoomButtons();
        }

        private void ZoomActualSizeButtonClickHandler(object sender, EventArgs e)
        {
            tilemapEditorControl1.ZoomActualSize();
            UpdateZoomButtons();
        }

        private void ZoomOutButtonClickHandler(object sender, EventArgs e)
        {
            tilemapEditorControl1.ZoomOut();
            UpdateZoomButtons();
        }

        private void TilemapEditorControl1_ZoomChanged(object sender, EventArgs e)
        {
            tilePicker1.SetZoom(tilemapEditorControl1.zoomLevel);
            RefreshCanvasPanelWidth();
            LayoutBackgroundInfoPanel();
            UpdateZoomButtons();
        }

        private void RandomizationCanvasButtonClickHandler(object sender, EventArgs e)
        {
            _isCanvasRandomizationVisible = _randomizationCanvasButton.Checked;
            tilemapEditorControl1.SetHighlightVisible(_isCanvasRandomizationVisible);
            CanvasRandomizationVisibilityChanged?.Invoke(_isCanvasRandomizationVisible);
        }

        private void RandomizationColorButtonClickHandler(object sender, EventArgs e)
        {
            using (ColorDialog dialog = new ColorDialog())
            {
                dialog.AllowFullOpen = true;
                dialog.FullOpen = true;
                dialog.Color = _randomizationOutlineColor;

                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                _randomizationOutlineColor = dialog.Color;
                tilemapEditorControl1.SetHighlightOutlineColor(_randomizationOutlineColor);
                RandomizationOutlineColorChanged?.Invoke(_randomizationOutlineColor);
            }
        }

        private void Panel2ResizeHandler(object sender, EventArgs e)
        {
            if (_backgroundFilesGroup == null)
                return;

            _backgroundFilesGroup.Width = Math.Max(220, panel2.Width - 16);

            int contentWidth = _backgroundFilesGroup.Width - 20;
            _graphicsFileLabel.Width = contentWidth;
            _paletteFileLabel.Width = contentWidth;
            _layoutFileLabel.Width = contentWidth;
            _exportBackgroundFilesButton.Width = contentWidth;
            _importBackgroundFilesButton.Width = contentWidth;

            LayoutBackgroundInfoPanel();
        }

        private void ExportBackgroundFilesButtonClickHandler(object sender, EventArgs e)
        {
            if (!CanUseBackgroundFiles())
                return;

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                WriteFileToDirectory(_backgroundGraphicsFile, dialog.SelectedPath);
                WriteFileToDirectory(_backgroundPaletteFile, dialog.SelectedPath);
                WriteFileToDirectory(_backgroundLayoutFile, dialog.SelectedPath);
            }
        }

        private void ImportBackgroundFilesButtonClickHandler(object sender, EventArgs e)
        {
            if (!CanUseBackgroundFiles())
                return;

            string ncgPath = PromptBackgroundImportFile("_ncg");
            if (ncgPath == null)
                return;

            string nclPath = PromptBackgroundImportFile("_ncl");
            if (nclPath == null)
                return;

            string nscPath = PromptBackgroundImportFile("_nsc");
            if (nscPath == null)
                return;

            try
            {
                ReplaceBackgroundFile(_backgroundGraphicsFile, IOFile.ReadAllBytes(ncgPath));
                ReplaceBackgroundFile(_backgroundPaletteFile, IOFile.ReadAllBytes(nclPath));
                ReplaceBackgroundFile(_backgroundLayoutFile, IOFile.ReadAllBytes(nscPath));
                RefreshAfterBackgroundImport();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, LanguageManager.Get("Errors", "errortitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
