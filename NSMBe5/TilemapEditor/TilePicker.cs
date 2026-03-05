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
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace NSMBe5
{
    public partial class TilePicker : UserControl
    {
        // ── Zoom ──────────────────────────────────────────────────────────────
        private const int MinZoom = 1;
        private const int MaxZoom = 8;

        // ── Highlight overlay ─────────────────────────────────────────────────
        private const float HighlightOutlineWidth = 2.0f;
        private const int   HighlightFillAlpha    = 96;

        // ── Behavior-shape overlay ────────────────────────────────────────────
        private const int BehaviorOverlaySpriteSize = 16;

        // Sprite-sheet column indices — adjust to match your sheet.
        private const int SpriteIdxCoin                = 25;
        private const int SpriteIdxQuestion            = 23;
        private const int SpriteIdxExplodable          = 18;
        private const int SpriteIdxBrick               = 19;
        private const int SpriteIdxWater               = 20;
        private const int SpriteIdxClimbable           = 24;
        private const int SpriteIdxSpikes              = 16;
        private const int SpriteIdxHarmfulAllSides      = 21;
        private const int SpriteIdxInvisible           = 26;
        private const int SpriteIdxInvisibleSlope1x    = 27;
        private const int SpriteIdxInvisibleSlope2x    = 28;
        private const int SpriteIdxInvisibleSlope4x    = 30;

        // ── Shared sprite-sheet state ─────────────────────────────────────────
        private static Bitmap _overlaySprites;
        private static bool   _triedLoadingOverlaySprites;
        private static readonly Dictionary<string, Bitmap[]> _scaledSpriteCache =
            new Dictionary<string, Bitmap[]>();

        // ── Public state ──────────────────────────────────────────────────────
        public delegate void TileSelectedd(
            int tileNum, int tilePal, int tileWidth, int tileHeight);

        public event TileSelectedd TileSelected;

        public Bitmap[] Buffers      { get; private set; }
        public int      BufferWidth  { get; private set; }
        public int      BufferHeight { get; private set; }
        public int      BufferCount  { get; private set; }
        public int      TileSize     { get; private set; }

        public int  SelTileNum    = 0;
        public int  SelTilePal    = 0;
        public int  SelTileWidth  = 1;
        public int  SelTileHeight = 1;

        public bool AllowsRectangle = true;
        public bool AllowNoTile     = true;

        // Legacy API compatibility for existing callers.
        public int selTileNum
        {
            get { return SelTileNum; }
            set { SelTileNum = value; }
        }

        public int selTilePal
        {
            get { return SelTilePal; }
            set { SelTilePal = value; }
        }

        public int selTileWidth
        {
            get { return SelTileWidth; }
            set { SelTileWidth = value; }
        }

        public int selTileHeight
        {
            get { return SelTileHeight; }
            set { SelTileHeight = value; }
        }

        public int bufferWidth  { get { return BufferWidth; } }
        public int bufferHeight { get { return BufferHeight; } }
        public int bufferCount  { get { return BufferCount; } }
        public int tileSize     { get { return TileSize; } }
        public Bitmap[] buffers { get { return Buffers; } }

        public bool allowsRectangle
        {
            get { return AllowsRectangle; }
            set { AllowsRectangle = value; }
        }

        public bool allowNoTile
        {
            get { return AllowNoTile; }
            set { AllowNoTile = value; }
        }

        // ── Private state ─────────────────────────────────────────────────────
        private int  _baseTileSize;
        private int  _zoomLevel = 1;

        private int  _hoverTileX = -1;
        private int  _hoverTileY = -1;
        private int  _downTileNum;
        private bool _isMouseDown;

        private readonly HashSet<int> _highlightedTiles = new HashSet<int>();
        private bool  _showHighlightedTiles;
        private Color _highlightOutlineColor = Color.FromArgb(220, 255, 200, 0);

        private uint[] _behaviorOverlayData;
        private bool   _showBehaviorShapeOverlay;

        // ─────────────────────────────────────────────────────────────────────
        // Construction / initialisation
        // ─────────────────────────────────────────────────────────────────────

        public TilePicker()
        {
            InitializeComponent();
            SetStyle(ControlStyles.Selectable, true);
        }

        public void Init(Bitmap[] buffers, int tileSize)
        {
            Buffers      = buffers;
            BufferCount  = buffers.Length;
            _baseTileSize = tileSize;
            BufferHeight = buffers[0].Height / _baseTileSize;
            BufferWidth  = buffers[0].Width  / _baseTileSize;
            ApplyZoom();
        }

        public void init(Bitmap[] buffers, int tileSize)
        {
            Init(buffers, tileSize);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Zoom
        // ─────────────────────────────────────────────────────────────────────

        public void SetZoom(int zoom)
        {
            int clamped = Math.Max(MinZoom, Math.Min(MaxZoom, zoom));
            if (clamped == _zoomLevel)
                return;

            _zoomLevel = clamped;
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            TileSize = _baseTileSize * _zoomLevel;
            if (Buffers == null)
                return;

            pictureBox1.Size = new Size(BufferWidth * TileSize, BufferHeight * BufferCount * TileSize);
            pictureBox1.Invalidate(true);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Highlight overlay API
        // ─────────────────────────────────────────────────────────────────────

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

        // ─────────────────────────────────────────────────────────────────────
        // Behavior-shape overlay API
        // ─────────────────────────────────────────────────────────────────────

        public void SetBehaviorOverlayData(uint[] behaviorData)
        {
            _behaviorOverlayData = behaviorData;
            pictureBox1.Invalidate(true);
        }

        public void SetBehaviorOverlayVisible(bool visible)
        {
            if (_showBehaviorShapeOverlay == visible)
                return;

            _showBehaviorShapeOverlay = visible;
            pictureBox1.Invalidate(true);
        }

        public void SetBehaviorShapeOverlayData(uint[] behaviorData)
        {
            SetBehaviorOverlayData(behaviorData);
        }

        public void SetBehaviorShapeOverlayVisible(bool visible)
        {
            SetBehaviorOverlayVisible(visible);
        }

        public void RefreshView() => pictureBox1.Invalidate(true);

        // ─────────────────────────────────────────────────────────────────────
        // Painting
        // ─────────────────────────────────────────────────────────────────────

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (Buffers == null)
                return;

            Graphics g = e.Graphics;

            g.FillRectangle(Brushes.DarkSlateGray,
                0, 0, BufferWidth * TileSize, BufferHeight * TileSize * BufferCount);

            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode   = PixelOffsetMode.Half;

            for (int i = 0; i < BufferCount; i++)
                g.DrawImage(Buffers[i],
                    0, i * BufferHeight * TileSize,
                    BufferWidth * TileSize, BufferHeight * TileSize);

            DrawHighlightOverlay(g);
            DrawBehaviorShapeOverlay(g);
            DrawSelectionRect(g);
            DrawHoverRect(g);
        }

        private void DrawHighlightOverlay(Graphics g)
        {
            if (!_showHighlightedTiles || _highlightedTiles.Count == 0)
                return;

            using (var pen   = new Pen(_highlightOutlineColor, HighlightOutlineWidth))
            using (var brush = new SolidBrush(
                Color.FromArgb(HighlightFillAlpha,
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

                    for (int pal = 0; pal < BufferCount; pal++)
                    {
                        var rect = new Rectangle(
                            tx * TileSize,
                            (ty + pal * BufferHeight) * TileSize,
                            TileSize * 2,
                            TileSize * 2);

                        g.FillRectangle(brush, rect);
                        g.DrawRectangle(pen, rect.X, rect.Y, rect.Width - 1, rect.Height - 1);
                    }
                }
            }
        }

        private void DrawSelectionRect(Graphics g)
        {
            g.DrawRectangle(Pens.White,
                (SelTileNum % BufferWidth) * TileSize,
                (SelTileNum / BufferWidth + SelTilePal * BufferHeight) * TileSize,
                SelTileWidth  * TileSize,
                SelTileHeight * TileSize);
        }

        private void DrawHoverRect(Graphics g)
        {
            if (!_isMouseDown && _hoverTileX != -1)
                g.DrawRectangle(Pens.White,
                    _hoverTileX * TileSize,
                    _hoverTileY * TileSize,
                    TileSize,
                    TileSize);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Behavior-shape overlay — drawing
        // ─────────────────────────────────────────────────────────────────────

        private void DrawBehaviorShapeOverlay(Graphics g)
        {
            if (!_showBehaviorShapeOverlay || _behaviorOverlayData == null || _behaviorOverlayData.Length == 0)
                return;

            EnsureOverlaySpritesLoaded();

            g.SmoothingMode = SmoothingMode.AntiAlias;

            float borderWidth    = Math.Max(1f, TileSize / 12f);
            float slopeWidth     = Math.Max(1f, TileSize / 8f);
            float slopeEdgeWidth = Math.Max(2f, TileSize / 4f);

            using (var slopeOutlinePen     = new Pen(Color.FromArgb(220, 255, 255, 255), slopeWidth + 1f))
            using (var slopeEdgeOutlinePen = new Pen(Color.FromArgb(235, 255, 255, 255), slopeEdgeWidth + 1f))
            {
                for (int ty = 0; ty < BufferHeight; ty++)
                {
                    for (int tx = 0; tx < BufferWidth; tx++)
                    {
                        int tileNum = ty * BufferWidth + tx;
                        if (tileNum >= _behaviorOverlayData.Length)
                            continue;

                        uint behavior    = _behaviorOverlayData[tileNum];
                        uint flags       = GetFlags(behavior);
                        int  subType     = GetSubType(behavior);
                        int  behaviorParam = (int)(behavior & 0xFF);
                        int  slopeParam  = GetSlopeParam(behavior);
                        bool flippedSlope = (flags & 0x40u) != 0;
                        bool solidOnTop   = (flags & 0x8000u) != 0;
                        Color baseColor   = GetBehaviorOverlayColor(flags, subType, slopeParam, behaviorParam);

                        if (baseColor.A == 0 && slopeParam < 0 && !solidOnTop)
                            continue;

                        float px = tx * TileSize;

                        for (int pal = 0; pal < BufferCount; pal++)
                        {
                            float py   = (ty + pal * BufferHeight) * TileSize;
                            var   rect = new RectangleF(px, py, TileSize, TileSize);

                            if (TryDrawOverlaySprite(g, rect, flags, slopeParam, behaviorParam, solidOnTop, baseColor))
                                continue;

                            if (slopeParam >= 0)
                            {
                                using (var fill  = new SolidBrush(Color.FromArgb(135, baseColor.R, baseColor.G, baseColor.B)))
                                using (var slope = new Pen(Color.FromArgb(240, baseColor.R, baseColor.G, baseColor.B), slopeWidth))
                                using (var edge  = new Pen(Color.FromArgb(240, baseColor.R, baseColor.G, baseColor.B), slopeEdgeWidth))
                                {
                                    DrawSlopeShape(g, rect, slopeParam, flippedSlope,
                                        fill, slope, slopeOutlinePen, edge, slopeEdgeOutlinePen);
                                }
                            }
                            else if (solidOnTop)
                            {
                                DrawSolidOnTopLine(g, rect);
                            }
                            else if (baseColor.A != 0)
                            {
                                using (var fill   = new SolidBrush(Color.FromArgb(140, baseColor.R, baseColor.G, baseColor.B)))
                                using (var border = new Pen(Color.FromArgb(220, baseColor.R, baseColor.G, baseColor.B), borderWidth))
                                {
                                    g.FillRectangle(fill, rect);
                                    g.DrawRectangle(border, rect.X, rect.Y, rect.Width - 1f, rect.Height - 1f);
                                }
                            }
                        }
                    }
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Behavior-shape overlay — sprite loading & drawing
        // ─────────────────────────────────────────────────────────────────────

        private static void EnsureOverlaySpritesLoaded()
        {
            if (_triedLoadingOverlaySprites)
                return;

            _triedLoadingOverlaySprites = true;
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidatePaths =
            {
                Path.Combine(baseDir, "Data", "behavior_overlay.png"),
                Path.Combine(baseDir, "behavior_overlay.png"),
            };

            foreach (string path in candidatePaths)
            {
                if (!File.Exists(path))
                    continue;

                try
                {
                    using (var loaded = new Bitmap(path))
                        _overlaySprites = new Bitmap(loaded);

                    _scaledSpriteCache.Clear();
                    return;
                }
                catch
                {
                    _overlaySprites = null;
                }
            }
        }

        private static bool TryDrawOverlaySprite(
            Graphics g, RectangleF rect, uint flags, int slopeParam,
            int behaviorParam, bool solidOnTop, Color tintColor)
        {
            if (_overlaySprites == null)
                return false;

            if (!TryGetOverlaySpriteIndex(flags, slopeParam, behaviorParam, solidOnTop,
                out int spriteIndex, out bool flipX, out bool flipY, out int rotateTurns))
                return false;

            int cols = _overlaySprites.Width / BehaviorOverlaySpriteSize;
            if (cols <= 0)
                return false;

            int maxSprites = cols * (_overlaySprites.Height / BehaviorOverlaySpriteSize);
            if (spriteIndex < 0 || spriteIndex >= maxSprites)
                return false;

            int zoom = Math.Max(1, (int)Math.Round(rect.Width / BehaviorOverlaySpriteSize));
            Bitmap bitmap = GetScaledSpriteBitmap(spriteIndex, zoom, flipX, flipY, rotateTurns, tintColor);
            if (bitmap == null)
                return false;

            g.DrawImageUnscaled(bitmap, (int)rect.X, (int)rect.Y);
            return true;
        }

        private static bool TryGetOverlaySpriteIndex(
            uint flags, int slopeParam, int behaviorParam, bool solidOnTop,
            out int spriteIndex, out bool flipX, out bool flipY, out int rotateTurns)
        {
            spriteIndex  = -1;
            flipX        = false;
            flipY        = false;
            rotateTurns  = 0;
            bool flippedSlope = (flags & 0x40u) != 0;

            // Harmful / spike tiles
            if ((flags & 0x1000u) != 0)
            {
                switch (behaviorParam)
                {
                    case 0: spriteIndex = SpriteIdxSpikes; rotateTurns = 3; return true; // left
                    case 1: spriteIndex = SpriteIdxSpikes; rotateTurns = 1; return true; // right
                    case 2: spriteIndex = SpriteIdxSpikes;                  return true; // up
                    case 3: spriteIndex = SpriteIdxSpikes; rotateTurns = 2; return true; // down
                    case 4: spriteIndex = SpriteIdxWater;                   return true; // lava
                    case 5: spriteIndex = 0;                                return true; // instant death
                    case 6:
                    case 7: spriteIndex = SpriteIdxHarmfulAllSides;         return true;
                }
            }

            // Invisible tiles
            if ((flags & 0x2000u) != 0)
            {
                if (slopeParam >= 0)
                {
                    if (!TryDecodeSlopePart(slopeParam,
                        out int partCount, out int partIndex, out bool isDownRight))
                    {
                        spriteIndex = SpriteIdxInvisible;
                        return true;
                    }

                    if (isDownRight && partCount > 1)
                        partIndex = partCount - 1 - partIndex;

                    int startIndex;
                    if (partCount == 1)
                        startIndex = SpriteIdxInvisibleSlope1x;
                    else if (partCount == 2)
                        startIndex = SpriteIdxInvisibleSlope2x;
                    else
                        startIndex = SpriteIdxInvisibleSlope4x;

                    spriteIndex = startIndex + partIndex;
                    flipX = isDownRight;
                    flipY = flippedSlope;
                    return true;
                }

                spriteIndex = SpriteIdxInvisible;
                return true;
            }

            // Simple flag-mapped sprites
            if ((flags & 0x2u)   != 0) { spriteIndex = SpriteIdxCoin;       return true; }
            if ((flags & 0x4u)   != 0) { spriteIndex = SpriteIdxQuestion;   return true; }
            if ((flags & 0x8u)   != 0) { spriteIndex = SpriteIdxExplodable; return true; }
            if ((flags & 0x10u)  != 0) { spriteIndex = SpriteIdxBrick;      return true; }
            if ((flags & 0x200u) != 0) { spriteIndex = SpriteIdxWater;      return true; }
            if ((flags & 0x400u) != 0) { spriteIndex = SpriteIdxClimbable;  return true; }

            // Slope-edge exception (param 10 = horizontal-line graphic)
            if (slopeParam == 10)
            {
                spriteIndex = 0;
                flipY = flippedSlope;
                return true;
            }

            // Regular slope
            if (slopeParam >= 0)
            {
                if (!TryDecodeSlopePart(slopeParam,
                    out int partCount, out int partIndex, out bool isDownRight))
                    return false;

                if (isDownRight && partCount > 1)
                    partIndex = partCount - 1 - partIndex;

                int startIndex = solidOnTop
                    ? (partCount == 1 ? 9 : partCount == 2 ? 10 : 12)
                    : (partCount == 1 ? 1 : partCount == 2 ?  2 :  4);

                spriteIndex = startIndex + partIndex;
                flipX = isDownRight;
                flipY = flippedSlope;
                return true;
            }

            // Solid-on-top flat tile
            if (solidOnTop)
            {
                spriteIndex = 8;
                flipY = flippedSlope;
                return true;
            }

            spriteIndex = 0;
            return true;
        }

        private static bool TryDecodeSlopePart(
            int slopeParam, out int partCount, out int partIndex, out bool isDownRight)
        {
            partCount   = 0;
            partIndex   = 0;
            isDownRight = false;

            switch (slopeParam)
            {
                case  0: partCount = 1; partIndex = 0; isDownRight = false; return true;
                case  1: partCount = 1; partIndex = 0; isDownRight = true;  return true;
                case  2: partCount = 2; partIndex = 0; isDownRight = false; return true;
                case  3: partCount = 2; partIndex = 1; isDownRight = false; return true;
                case  4: partCount = 2; partIndex = 0; isDownRight = true;  return true;
                case  5: partCount = 2; partIndex = 1; isDownRight = true;  return true;
                case 11: partCount = 4; partIndex = 0; isDownRight = false; return true;
                case 12: partCount = 4; partIndex = 1; isDownRight = false; return true;
                case 13: partCount = 4; partIndex = 2; isDownRight = false; return true;
                case 14: partCount = 4; partIndex = 3; isDownRight = false; return true;
                case 15: partCount = 4; partIndex = 0; isDownRight = true;  return true;
                case 16: partCount = 4; partIndex = 1; isDownRight = true;  return true;
                case 17: partCount = 4; partIndex = 2; isDownRight = true;  return true;
                case 18: partCount = 4; partIndex = 3; isDownRight = true;  return true;
                default: return false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Sprite bitmap helpers
        // ─────────────────────────────────────────────────────────────────────

        private static Bitmap GetScaledSpriteBitmap(
            int spriteIndex, int zoom, bool flipX, bool flipY, int rotateTurns, Color tintColor)
        {
            if (_overlaySprites == null || zoom <= 0)
                return null;

            zoom = Math.Min(8, zoom);
            string cacheKey = $"{spriteIndex}:{(flipX ? 1 : 0)}:{(flipY ? 1 : 0)}:{rotateTurns & 3}:{tintColor.ToArgb() & 0x00FFFFFF}";

            if (!_scaledSpriteCache.TryGetValue(cacheKey, out Bitmap[] zoomCache))
            {
                zoomCache = new Bitmap[8];
                _scaledSpriteCache[cacheKey] = zoomCache;
            }

            Bitmap cached = zoomCache[zoom - 1];
            if (cached != null)
                return cached;

            int cols = _overlaySprites.Width / BehaviorOverlaySpriteSize;
            if (cols <= 0)
                return null;

            var srcRect = new Rectangle(
                (spriteIndex % cols) * BehaviorOverlaySpriteSize,
                (spriteIndex / cols) * BehaviorOverlaySpriteSize,
                BehaviorOverlaySpriteSize,
                BehaviorOverlaySpriteSize);

            Bitmap src = _overlaySprites.Clone(srcRect, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            switch (rotateTurns & 3)
            {
                case 1: src.RotateFlip(RotateFlipType.Rotate90FlipNone);  break;
                case 2: src.RotateFlip(RotateFlipType.Rotate180FlipNone); break;
                case 3: src.RotateFlip(RotateFlipType.Rotate270FlipNone); break;
            }

            if      (flipX && flipY) src.RotateFlip(RotateFlipType.RotateNoneFlipXY);
            else if (flipX)          src.RotateFlip(RotateFlipType.RotateNoneFlipX);
            else if (flipY)          src.RotateFlip(RotateFlipType.RotateNoneFlipY);

            Bitmap tinted = TintRedChannel(src, tintColor);
            src.Dispose();

            // Nearest-neighbor scale using SetPixel (keeps pixel-art look).
            int scaledSize = BehaviorOverlaySpriteSize * zoom;
            var scaled = new Bitmap(scaledSize, scaledSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < BehaviorOverlaySpriteSize; y++)
            {
                for (int x = 0; x < BehaviorOverlaySpriteSize; x++)
                {
                    Color c   = tinted.GetPixel(x, y);
                    int   dstX = x * zoom;
                    int   dstY = y * zoom;
                    for (int yy = 0; yy < zoom; yy++)
                        for (int xx = 0; xx < zoom; xx++)
                            scaled.SetPixel(dstX + xx, dstY + yy, c);
                }
            }

            tinted.Dispose();
            zoomCache[zoom - 1] = scaled;
            return scaled;
        }

        /// <summary>
        /// Recolours a sprite whose intensity is encoded in the red channel,
        /// replacing red with <paramref name="tintColor"/> while preserving alpha.
        /// </summary>
        private static Bitmap TintRedChannel(Bitmap src, Color tintColor)
        {
            var tinted = new Bitmap(src.Width, src.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < src.Height; y++)
            {
                for (int x = 0; x < src.Width; x++)
                {
                    Color s = src.GetPixel(x, y);
                    if (s.A == 0)
                    {
                        tinted.SetPixel(x, y, Color.Transparent);
                        continue;
                    }

                    float intensity = s.R / 255f;
                    byte  r = (byte)Math.Max(0, Math.Min(255, tintColor.R * intensity));
                    byte  g = (byte)Math.Max(0, Math.Min(255, tintColor.G * intensity));
                    byte  b = (byte)Math.Max(0, Math.Min(255, tintColor.B * intensity));
                    tinted.SetPixel(x, y, Color.FromArgb(s.A, r, g, b));
                }
            }

            return tinted;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Behavior data decoding helpers
        // ─────────────────────────────────────────────────────────────────────

        private static uint GetFlags(uint data)
            => ((data & 0xFFFF0000u) >> 16) | ((data & 0xF00u) << 8);

        private static int GetSubType(uint data)
            => (int)((data & 0xF000u) >> 12);

        private static int GetSlopeParam(uint data)
        {
            uint flags   = GetFlags(data);
            bool isSlope = (flags & (0x20u | 0x40u)) != 0;
            return isSlope ? (int)(data & 0xFF) : -1;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Behavior overlay colour helpers
        // ─────────────────────────────────────────────────────────────────────

        private static Color GetBehaviorOverlayColor(uint flags, int subType, int slopeParam, int behaviorParam)
        {
            if (slopeParam == 10)         return Color.FromArgb(255, 85,  105, 200); // slope-edge exception
            if ((flags & 0x1Eu) != 0)     return Color.FromArgb(255, 235, 210, 80);  // coin/question/explodable/brick
            if ((flags & 0x200u) != 0)    return Color.FromArgb(255, 80,  215, 255); // water
            if ((flags & 0x400u) != 0)    return Color.FromArgb(255, 85,  220, 110); // climbable
            if ((flags & 0x1000u) != 0)
                return behaviorParam == 4
                    ? Color.FromArgb(255, 235, 105, 65)   // lava
                    : Color.FromArgb(255, 230, 60,  60);  // spikes / instant-death

            Color subTypeColor = GetSubtypeOverlayColor(subType);
            if (subTypeColor.A != 0)
                return subTypeColor;

            if ((flags & 0x2000u) != 0) return Color.FromArgb(255, 170, 115, 235); // invisible
            if ((flags & 0x800u)  != 0) return Color.FromArgb(255, 185, 95,  255); // partial solid
            if ((flags & 0x8000u) != 0) return Color.FromArgb(255, 110, 110, 110); // solid-on-top
            if (slopeParam >= 0)        return Color.FromArgb(255, 110, 110, 110); // slope
            if ((flags & 1u) != 0)      return Color.FromArgb(255, 110, 110, 110); // solid

            return Color.FromArgb(0, 0, 0, 0); // no overlay
        }

        private static Color GetSubtypeOverlayColor(int subType)
        {
            switch (subType)
            {
                case  1: // Ice
                case  2: return Color.FromArgb(255, 70,  170, 255); // Snow
                case  3: // Quicksand
                case 14: return Color.FromArgb(255, 225, 180, 70);  // Sandy
                case  4: // Conveyor right
                case  5: return Color.FromArgb(255, 255, 160, 40);  // Conveyor left
                case  6: return Color.FromArgb(255, 150, 210, 120); // Horizontal rope
                case 10: return Color.FromArgb(255, 85,  220, 110); // Vertical pole
                case 13: return Color.FromArgb(255, 210, 140, 70);  // Slows down
                default: return Color.FromArgb(0, 0, 0, 0);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // Shape-drawing helpers
        // ─────────────────────────────────────────────────────────────────────

        private static void DrawSlopeShape(
            Graphics g, RectangleF rect, int slopeParam, bool flippedSlope,
            SolidBrush fillBrush, Pen slopePen, Pen slopeOutlinePen,
            Pen edgePen, Pen edgeOutlinePen)
        {
            // Special case: horizontal edge line
            if (slopeParam == 10)
            {
                float y     = flippedSlope ? rect.Top + rect.Height * 0.15f : rect.Bottom - rect.Height * 0.15f;
                float width = Math.Max(2f, rect.Width / 4f);
                using (var outlinePen = new Pen(Color.FromArgb(235, 255, 255, 255), width + 1f))
                using (var linePen   = new Pen(Color.FromArgb(245, 220, 45,  45),   width))
                {
                    g.DrawLine(outlinePen, rect.Left, y, rect.Right, y);
                    g.DrawLine(linePen,    rect.Left, y, rect.Right, y);
                }
                return;
            }

            if (!TryGetSlopeSegment(slopeParam, out float x1, out float y1, out float x2, out float y2))
                return;

            if (flippedSlope)
            {
                y1 = 1f - y1;
                y2 = 1f - y2;
            }

            var p1 = new PointF(rect.Left + x1 * rect.Width, rect.Top + y1 * rect.Height);
            var p2 = new PointF(rect.Left + x2 * rect.Width, rect.Top + y2 * rect.Height);

            g.DrawLine(slopeOutlinePen, p1, p2);
            g.DrawLine(slopePen,        p1, p2);

            // Fill the solid area beneath (or above) the slope line.
            PointF[] solidArea = flippedSlope
                ? new[] { p1, p2, new PointF(rect.Right, rect.Top),  new PointF(rect.Left, rect.Top) }
                : new[] { p1, p2, new PointF(rect.Right, rect.Bottom), new PointF(rect.Left, rect.Bottom) };

            g.FillPolygon(fillBrush, solidArea);
        }

        private static void DrawSolidOnTopLine(Graphics g, RectangleF rect)
        {
            float y     = rect.Top + rect.Height * 0.14f;
            float width = Math.Max(2f, rect.Width / 7f);
            using (var outlinePen = new Pen(Color.FromArgb(235, 255, 255, 255), width + 1f))
            using (var linePen   = new Pen(Color.FromArgb(240, 110, 110, 110), width))
            {
                g.DrawLine(outlinePen, rect.Left, y, rect.Right, y);
                g.DrawLine(linePen,   rect.Left, y, rect.Right, y);
            }
        }

        private static bool TryGetSlopeSegment(
            int slopeParam, out float x1, out float y1, out float x2, out float y2)
        {
            x1 = y1 = x2 = y2 = 0f;

            switch (slopeParam)
            {
                case 0: x1=0f; y1=1f; x2=1f;  y2=0f;  return true;
                case 1: x1=0f; y1=0f; x2=1f;  y2=1f;  return true;
                case 6: x1=0f; y1=1f; x2=0.5f;y2=0f;  return true;
                case 7: x1=0.5f;y1=1f;x2=1f;  y2=0f;  return true;
                case 8: x1=0.5f;y1=0f;x2=1f;  y2=1f;  return true;
                case 9: x1=0f; y1=0f; x2=0.5f;y2=1f;  return true;

                case  2: return TryGetNx1Segment(2, 0, upRight: true,  out x1, out y1, out x2, out y2);
                case  3: return TryGetNx1Segment(2, 1, upRight: true,  out x1, out y1, out x2, out y2);
                case  4: return TryGetNx1Segment(2, 0, upRight: false, out x1, out y1, out x2, out y2);
                case  5: return TryGetNx1Segment(2, 1, upRight: false, out x1, out y1, out x2, out y2);
                case 11: return TryGetNx1Segment(4, 0, upRight: true,  out x1, out y1, out x2, out y2);
                case 12: return TryGetNx1Segment(4, 1, upRight: true,  out x1, out y1, out x2, out y2);
                case 13: return TryGetNx1Segment(4, 2, upRight: true,  out x1, out y1, out x2, out y2);
                case 14: return TryGetNx1Segment(4, 3, upRight: true,  out x1, out y1, out x2, out y2);
                case 15: return TryGetNx1Segment(4, 0, upRight: false, out x1, out y1, out x2, out y2);
                case 16: return TryGetNx1Segment(4, 1, upRight: false, out x1, out y1, out x2, out y2);
                case 17: return TryGetNx1Segment(4, 2, upRight: false, out x1, out y1, out x2, out y2);
                case 18: return TryGetNx1Segment(4, 3, upRight: false, out x1, out y1, out x2, out y2);

                default: return false;
            }
        }

        private static bool TryGetNx1Segment(
            int width, int part, bool upRight,
            out float x1, out float y1, out float x2, out float y2)
        {
            x1 = y1 = x2 = y2 = 0f;
            if (width <= 0 || part < 0 || part >= width)
                return false;

            float gx1 = part;
            float gx2 = part + 1;
            float gy1 = upRight ? 1f - gx1 / width : gx1 / width;
            float gy2 = upRight ? 1f - gx2 / width : gx2 / width;

            x1 = 0f; x2 = 1f;
            y1 = gy1; y2 = gy2;
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Mouse event handlers
        // ─────────────────────────────────────────────────────────────────────

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            _isMouseDown = true;

            int tx = Clamp(e.X / TileSize, 0, BufferWidth - 1);
            int ty = Clamp(e.Y / TileSize, 0, BufferHeight * BufferCount - 1);

            SelTilePal = ty / BufferHeight;
            ty        %= BufferHeight;

            _downTileNum = ty * BufferWidth + tx;

            pictureBox1_MouseMove(sender, e);
            Focus();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                int dx = _downTileNum % BufferWidth;
                int dy = _downTileNum / BufferWidth;

                int tx = Clamp(e.X / TileSize, 0, BufferWidth - 1);
                int ty = Clamp(e.Y / TileSize - BufferHeight * SelTilePal, 0, BufferHeight - 1);

                int xMin = Math.Min(dx, tx);
                int yMin = Math.Min(dy, ty);
                int xMax = Math.Max(dx, tx);
                int yMax = Math.Max(dy, ty);

                SelTileNum    = xMin + yMin * BufferWidth;
                SelTileWidth  = xMax - xMin + 1;
                SelTileHeight = yMax - yMin + 1;
            }
            else
            {
                _hoverTileX = Clamp(e.X / TileSize, 0, BufferWidth - 1);
                _hoverTileY = Clamp(e.Y / TileSize, 0, BufferHeight * BufferCount - 1);
            }

            pictureBox1.Invalidate(true);
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (_isMouseDown)
                TileSelected?.Invoke(SelTileNum, SelTilePal, SelTileWidth, SelTileHeight);

            _isMouseDown = false;
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            _hoverTileX = -1;
            _hoverTileY = -1;
            pictureBox1.Invalidate(true);
        }

        private void TilePicker_KeyPress(object sender, KeyPressEventArgs e)
        {
        }

        // ─────────────────────────────────────────────────────────────────────
        // Utility
        // ─────────────────────────────────────────────────────────────────────

        private static int Clamp(int value, int min, int max)
            => Math.Max(min, Math.Min(max, value));
    }
}
