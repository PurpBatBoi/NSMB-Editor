/*
 * This file is part of NSMB Editor 5.
 *
 * NSMB Editor 5 is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * NSMB Editor 5 is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with NSMB Editor 5. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NSMBe5
{
    public partial class TilemapEditorControl : UserControl
    {
        // ── Constants ─────────────────────────────────────────────────────────
        private const int BaseTileSize      = 8;
        private const int MinZoom           = 1;
        private const int MaxZoom           = 8;
        private const float HighlightOutlineWidth = 2.0f;
        private const int   HighlightFillAlpha    = 96;

        // ── Public state ──────────────────────────────────────────────────────
        public event EventHandler ZoomChanged;

        public Tilemap                    Tilemap;
        public TilePicker                 Picker;
        public TilemapEditor.TilemapEditor Editor;

        public ToolStripButton UndoButton;
        public ToolStripButton RedoButton;

        public int BufferWidth;
        public int BufferHeight;
        public int TileSize;
        public int ZoomLevel = 1;

        public int SelTileX;
        public int SelTileY;
        public int SelTileWidth;
        public int SelTileHeight;

        public bool ShowGrid;
        public EditionMode Mode;

        public readonly Stack<TilemapUndoEntry> UndoActions = new Stack<TilemapUndoEntry>();
        public readonly Stack<TilemapUndoEntry> RedoActions = new Stack<TilemapUndoEntry>();

        // ── Private state ─────────────────────────────────────────────────────
        private int  _hoverTileX = -1;
        private int  _hoverTileY = -1;
        private int  _downTileX;
        private int  _downTileY;
        private bool _isMouseDown;

        private int _defaultWidth  = 1;
        private int _defaultHeight = 1;

        private Tilemap.Tile[,] _clipboard;
        private int _clipboardWidth;
        private int _clipboardHeight;

        private readonly HashSet<int> _highlightedTiles    = new HashSet<int>();
        private bool  _showHighlightedTiles;
        private Color _highlightOutlineColor = Color.FromArgb(220, 255, 200, 0);

        // ── Enums ─────────────────────────────────────────────────────────────
        public enum EditionMode
        {
            Draw      = 0,
            XFlip     = 1,
            YFlip     = 2,
            Copy      = 3,
            Paste     = 4,
            ChangePal = 5,
        }

        // ── Construction / initialisation ─────────────────────────────────────
        public TilemapEditorControl()
        {
            InitializeComponent();
            SetStyle(ControlStyles.Selectable, true);
        }

        public void LoadTilemap(Tilemap tilemap)
        {
            Tilemap = tilemap;
            tilemap.render();

            ZoomLevel    = 1;
            TileSize     = BaseTileSize;
            BufferHeight = tilemap.height;
            BufferWidth  = tilemap.width;
            Size         = new Size(BufferWidth * TileSize, BufferHeight * TileSize);

            UndoButton.Click += Undo;
            RedoButton.Click += Redo;
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void ReloadFromTilemap()
        {
            if (Tilemap == null)
                return;

            BufferHeight = Tilemap.height;
            BufferWidth  = Tilemap.width;
            Size         = new Size(BufferWidth * TileSize, BufferHeight * TileSize);
            pictureBox1.Invalidate(true);
        }

        public bool CanZoomIn    => ZoomLevel < MaxZoom;
        public bool CanZoomOut   => ZoomLevel > MinZoom;
        public bool IsActualZoom => ZoomLevel == 1;

        public void ZoomIn()         => SetZoom(ZoomLevel + 1);
        public void ZoomOut()        => SetZoom(ZoomLevel - 1);
        public void ZoomActualSize() => SetZoom(1);

        public void SetZoom(int newZoom)
        {
            int clamped = Clamp(newZoom, MinZoom, MaxZoom);
            if (clamped == ZoomLevel)
                return;

            ZoomLevel = clamped;
            TileSize  = BaseTileSize * ZoomLevel;
            Size      = new Size(BufferWidth * TileSize, BufferHeight * TileSize);
            pictureBox1.Invalidate(true);

            ZoomChanged?.Invoke(this, EventArgs.Empty);
        }

        // ── Highlight overlay API ─────────────────────────────────────────────

        public void SetHighlightTiles(IEnumerable<int> tileNumbers)
        {
            _highlightedTiles.Clear();
            if (tileNumbers != null)
                foreach (int n in tileNumbers)
                    if (n >= 0)
                        _highlightedTiles.Add(n);

            pictureBox1.Invalidate(true);
        }

        public void SetHighlightVisible(bool visible)
        {
            if (_showHighlightedTiles == visible)
                return;

            _showHighlightedTiles = visible;
            pictureBox1.Invalidate(true);
        }

        public void SetHighlightOutlineColor(Color color)
        {
            _highlightOutlineColor = color;
            pictureBox1.Invalidate(true);
        }

        // ── Undo / Redo ───────────────────────────────────────────────────────

        public void Undo(object sender, EventArgs e)
        {
            if (UndoActions.Count == 0)
                return;

            TilemapUndoEntry item = UndoActions.Pop();
            item.Undo(Tilemap.tiles);
            item.ReRender(Tilemap);
            RedoActions.Push(item);

            UndoButton.Enabled = UndoActions.Count > 0;
            RedoButton.Enabled = true;
            pictureBox1.Invalidate(true);
        }

        public void Redo(object sender, EventArgs e)
        {
            if (RedoActions.Count == 0)
                return;

            TilemapUndoEntry item = RedoActions.Pop();
            item.Redo(Tilemap.tiles);
            item.ReRender(Tilemap);
            UndoActions.Push(item);

            RedoButton.Enabled = RedoActions.Count > 0;
            UndoButton.Enabled = true;
            pictureBox1.Invalidate(true);
        }

        // ── Painting ──────────────────────────────────────────────────────────

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (Tilemap == null)
                return;

            Graphics g = e.Graphics;

            g.FillRectangle(Brushes.DarkSlateGray,
                0, 0, BufferWidth * TileSize, BufferHeight * TileSize);

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode   = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.DrawImage(Tilemap.buffer, 0, 0, BufferWidth * TileSize, BufferHeight * TileSize);

            DrawHighlightOverlay(g);
            DrawGrid(g);
            DrawSelectionRect(g);
            DrawHoverRect(g);
        }

        private void DrawHighlightOverlay(Graphics g)
        {
            if (!_showHighlightedTiles || _highlightedTiles.Count == 0)
                return;

            using (Pen pen = new Pen(_highlightOutlineColor, HighlightOutlineWidth))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(
                HighlightFillAlpha,
                _highlightOutlineColor.R,
                _highlightOutlineColor.G,
                _highlightOutlineColor.B)))
            {
                foreach (int tileNumber in _highlightedTiles)
                {
                    int tx = tileNumber % BufferWidth;
                    int ty = tileNumber / BufferWidth;

                    if (tx < 0 || tx >= BufferWidth || ty < 0 || ty >= BufferHeight)
                        continue;

                    // Only draw at even-aligned 2×2 groups.
                    if ((tx & 1) != 0 || (ty & 1) != 0)
                        continue;

                    int right       = tileNumber + 1;
                    int bottom      = tileNumber + BufferWidth;
                    int bottomRight = bottom + 1;

                    if (!_highlightedTiles.Contains(right)       ||
                        !_highlightedTiles.Contains(bottom)      ||
                        !_highlightedTiles.Contains(bottomRight))
                        continue;

                    var rect = new Rectangle(tx * TileSize, ty * TileSize, TileSize * 2, TileSize * 2);
                    g.FillRectangle(brush, rect);
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
                }
            }
        }

        private void DrawGrid(Graphics g)
        {
            if (!ShowGrid)
                return;

            // Grid lines every 2 tiles (one Map16 block).
            int step = TileSize * 2;
            for (int x = step; x < Width;  x += step) g.DrawLine(Pens.Gray, x, 0,      x,      Height);
            for (int y = step; y < Height; y += step) g.DrawLine(Pens.Gray, 0, y, Width, y);
        }

        private void DrawSelectionRect(Graphics g)
        {
            if (!_isMouseDown)
                return;

            // While dragging a single-tile selection, preview the brush size instead.
            int drawWidth  = (SelTileWidth  == 1 && SelTileHeight == 1) ? _defaultWidth  : SelTileWidth;
            int drawHeight = (SelTileWidth  == 1 && SelTileHeight == 1) ? _defaultHeight : SelTileHeight;

            g.DrawRectangle(Pens.White,
                SelTileX * TileSize,
                SelTileY * TileSize,
                drawWidth  * TileSize,
                drawHeight * TileSize);
        }

        private void DrawHoverRect(Graphics g)
        {
            if (_isMouseDown || _hoverTileX == -1)
                return;

            g.DrawRectangle(Pens.White,
                _hoverTileX * TileSize,
                _hoverTileY * TileSize,
                _defaultWidth  * TileSize,
                _defaultHeight * TileSize);
        }

        // ── Edit operations ───────────────────────────────────────────────────

        private void RefreshDefaultSize()
        {
            _defaultWidth  = 1;
            _defaultHeight = 1;

            if (Mode == EditionMode.Draw)
            {
                _defaultWidth  = Picker.selTileWidth;
                _defaultHeight = Picker.selTileHeight;
            }
            else if (Mode == EditionMode.Paste && _clipboard != null)
            {
                _defaultWidth  = _clipboardWidth;
                _defaultHeight = _clipboardHeight;
            }
        }

        private void ApplyEdit()
        {
            // Finalise a 1×1 drag into the current brush/clipboard size.
            if (SelTileWidth == 1 && SelTileHeight == 1)
            {
                RefreshDefaultSize();
                SelTileWidth  = _defaultWidth;
                SelTileHeight = _defaultHeight;
            }

            // Record undo state before making changes (skip for Copy — nothing changes).
            TilemapUndoEntry undoEntry = null;
            if (Mode != EditionMode.Copy)
            {
                UndoButton.Enabled = true;
                RedoButton.Enabled = false;
                RedoActions.Clear();
                undoEntry = new TilemapUndoEntry(Tilemap.tiles, SelTileX, SelTileY, SelTileWidth, SelTileHeight);
                UndoActions.Push(undoEntry);
            }

            switch (Mode)
            {
                case EditionMode.Draw:      ApplyDraw();      break;
                case EditionMode.XFlip:     ApplyXFlip();     break;
                case EditionMode.YFlip:     ApplyYFlip();     break;
                case EditionMode.Copy:      ApplyCopy();      break;
                case EditionMode.Paste:     ApplyPaste();     break;
                case EditionMode.ChangePal: ApplyChangePal(); break;
            }

            Tilemap.reRender(SelTileX, SelTileY, SelTileWidth, SelTileHeight);
            pictureBox1.Invalidate(true);

            // Save the post-edit tile state into the undo entry.
            if (Mode != EditionMode.Copy)
                undoEntry.LoadNewTiles(Tilemap.tiles);
        }

        private void ApplyDraw()
        {
            for (int x = 0; x < SelTileWidth; x++)
            {
                for (int y = 0; y < SelTileHeight; y++)
                {
                    if (x + SelTileX >= Tilemap.width  ||
                        y + SelTileY >= Tilemap.height)
                        continue;

                    // Tile number wraps within the picker's selected rectangle.
                    int tileNum = Picker.selTileNum
                        + (x % Picker.selTileWidth)
                        + (y % Picker.selTileHeight) * Picker.bufferWidth;

                    Tilemap.Tile tile = Tilemap.tiles[x + SelTileX, y + SelTileY];
                    tile.tileNum = tileNum;
                    tile.palNum  = Picker.selTilePal;
                    tile.hflip   = false;
                    tile.vflip   = false;
                }
            }
        }

        private void ApplyXFlip()
        {
            // Swap columns around the horizontal centre.
            for (int x = 0; x < SelTileWidth / 2; x++)
            {
                for (int y = 0; y < SelTileHeight; y++)
                {
                    int x1 = x + SelTileX;
                    int x2 = SelTileX + SelTileWidth - x - 1;
                    int yy = y + SelTileY;

                    Tilemap.Tile tmp  = Tilemap.tiles[x1, yy];
                    Tilemap.tiles[x1, yy] = Tilemap.tiles[x2, yy];
                    Tilemap.tiles[x2, yy] = tmp;
                }
            }

            // Flip the horizontal flag on every tile in the region.
            for (int x = 0; x < SelTileWidth; x++)
                for (int y = 0; y < SelTileHeight; y++)
                    Tilemap.tiles[x + SelTileX, y + SelTileY].hflip
                        = !Tilemap.tiles[x + SelTileX, y + SelTileY].hflip;
        }

        private void ApplyYFlip()
        {
            // Swap rows around the vertical centre.
            for (int x = 0; x < SelTileWidth; x++)
            {
                for (int y = 0; y < SelTileHeight / 2; y++)
                {
                    int y1 = y + SelTileY;
                    int y2 = SelTileY + SelTileHeight - y - 1;
                    int xx = x + SelTileX;

                    Tilemap.Tile tmp  = Tilemap.tiles[xx, y1];
                    Tilemap.tiles[xx, y1] = Tilemap.tiles[xx, y2];
                    Tilemap.tiles[xx, y2] = tmp;
                }
            }

            // Flip the vertical flag on every tile in the region.
            for (int x = 0; x < SelTileWidth; x++)
                for (int y = 0; y < SelTileHeight; y++)
                    Tilemap.tiles[x + SelTileX, y + SelTileY].vflip
                        = !Tilemap.tiles[x + SelTileX, y + SelTileY].vflip;
        }

        private void ApplyCopy()
        {
            _clipboardWidth  = SelTileWidth;
            _clipboardHeight = SelTileHeight;
            _clipboard       = new Tilemap.Tile[_clipboardWidth, _clipboardHeight];

            for (int x = 0; x < SelTileWidth; x++)
                for (int y = 0; y < SelTileHeight; y++)
                    _clipboard[x, y] = Tilemap.tiles[x + SelTileX, y + SelTileY];
        }

        private void ApplyPaste()
        {
            if (_clipboard == null)
                return;

            for (int x = 0; x < SelTileWidth; x++)
                for (int y = 0; y < SelTileHeight; y++)
                    Tilemap.tiles[x + SelTileX, y + SelTileY]
                        = _clipboard[x % _clipboardWidth, y % _clipboardHeight];
        }

        private void ApplyChangePal()
        {
            for (int x = 0; x < SelTileWidth; x++)
            {
                for (int y = 0; y < SelTileHeight; y++)
                {
                    Tilemap.Tile tile = Tilemap.tiles[x + SelTileX, y + SelTileY];
                    tile.palNum = (tile.palNum + 1) % Tilemap.palettes.Length;
                }
            }
        }

        // ── Mouse event handlers ──────────────────────────────────────────────

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            _isMouseDown = true;
            _downTileX   = Clamp(e.X / TileSize, 0, BufferWidth  - 1);
            _downTileY   = Clamp(e.Y / TileSize, 0, BufferHeight - 1);

            pictureBox1_MouseMove(sender, e);
            FocusPreservingParentScroll();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                int tx = Clamp(e.X / TileSize, 0, BufferWidth  - 1);
                int ty = Clamp(e.Y / TileSize, 0, BufferHeight - 1);

                int xMin = Math.Min(_downTileX, tx);
                int yMin = Math.Min(_downTileY, ty);
                int xMax = Math.Max(_downTileX, tx);
                int yMax = Math.Max(_downTileY, ty);

                SelTileX      = xMin;
                SelTileY      = yMin;
                SelTileWidth  = xMax - xMin + 1;
                SelTileHeight = yMax - yMin + 1;
            }
            else
            {
                _hoverTileX = Clamp(e.X / TileSize, 0, BufferWidth  - 1);
                _hoverTileY = Clamp(e.Y / TileSize, 0, BufferHeight - 1);
            }

            RefreshDefaultSize();
            pictureBox1.Invalidate(true);
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isMouseDown)
                ApplyEdit();

            _isMouseDown = false;
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            _hoverTileX = -1;
            _hoverTileY = -1;
            pictureBox1.Invalidate(true);
        }

        private void pictureBox1_MouseWheel(object sender, MouseEventArgs e)
            => HandleZoomMouseWheel(e);

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            HandleZoomMouseWheel(e);
            base.OnMouseWheel(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Mode hotkeys — index matches EditionMode enum values.
            switch (keyData)
            {
                case Keys.D: Editor.SetMode(EditionMode.Draw);      return true;
                case Keys.X: Editor.SetMode(EditionMode.XFlip);     return true;
                case Keys.Y: Editor.SetMode(EditionMode.YFlip);     return true;
                case Keys.C: Editor.SetMode(EditionMode.Copy);      return true;
                case Keys.V: Editor.SetMode(EditionMode.Paste);     return true;
                case Keys.P: Editor.SetMode(EditionMode.ChangePal); return true;
            }

            if (keyData == (Keys.Control | Keys.Z)) { Undo(null, null); return true; }
            if (keyData == (Keys.Control | Keys.Y)) { Redo(null, null); return true; }
            if (keyData == (Keys.Control | Keys.S)) { Tilemap.save();   return true; }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void HandleZoomMouseWheel(MouseEventArgs e)
        {
            if ((ModifierKeys & Keys.Control) != Keys.Control)
                return;

            if      (e.Delta > 0) ZoomIn();
            else if (e.Delta < 0) ZoomOut();
        }

        /// <summary>
        /// Focuses this control without disturbing the parent scroll container's
        /// current scroll position (WinForms auto-scrolls to the focused child).
        /// </summary>
        private void FocusPreservingParentScroll()
        {
            ScrollableControl scrollParent = Parent as ScrollableControl;
            Point savedScroll = Point.Empty;
            bool  shouldRestore = scrollParent != null && scrollParent.AutoScroll;

            if (shouldRestore)
            {
                Point pos = scrollParent.AutoScrollPosition;
                // AutoScrollPosition is returned as negative; store it positive for re-assignment.
                savedScroll = new Point(-pos.X, -pos.Y);
            }

            Focus();

            if (shouldRestore)
                scrollParent.AutoScrollPosition = savedScroll;
        }

        private static int Clamp(int value, int min, int max)
            => Math.Max(min, Math.Min(max, value));
    }


    // ── TilemapUndoEntry ──────────────────────────────────────────────────────

    public class TilemapUndoEntry
    {
        // ── Public state ──────────────────────────────────────────────────────
        public Tilemap.Tile[,] OldTiles { get; private set; }
        public Tilemap.Tile[,] NewTiles { get; private set; }

        public int X      { get; }
        public int Y      { get; }
        public int Width  { get; }
        public int Height { get; }

        // ── Construction ──────────────────────────────────────────────────────
        public TilemapUndoEntry(Tilemap.Tile[,] allTiles, int x, int y, int width, int height)
        {
            X      = x;
            Y      = y;
            Width  = width;
            Height = height;
            OldTiles = CopyRegion(allTiles);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Called after the edit is applied to snapshot the post-edit state.</summary>
        public void LoadNewTiles(Tilemap.Tile[,] allTiles)
            => NewTiles = CopyRegion(allTiles);

        public void Undo(Tilemap.Tile[,] allTiles) => PasteRegion(OldTiles, allTiles);
        public void Redo(Tilemap.Tile[,] allTiles) => PasteRegion(NewTiles, allTiles);

        public void ReRender(Tilemap tilemap) => tilemap.reRender(X, Y, Width, Height);

        // ── Private helpers ───────────────────────────────────────────────────

        private Tilemap.Tile[,] CopyRegion(Tilemap.Tile[,] allTiles)
        {
            var region = new Tilemap.Tile[Width, Height];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    region[x, y] = allTiles[x + X, y + Y];
            return region;
        }

        private void PasteRegion(Tilemap.Tile[,] region, Tilemap.Tile[,] allTiles)
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    allTiles[x + X, y + Y] = region[x, y];
        }
    }
}
