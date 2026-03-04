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
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace NSMBe5
{
    public partial class TilePicker : UserControl
    {
        private const int MinZoom = 1;
        private const int MaxZoom = 8;

        public delegate void TileSelectedd(int selTileNum, int selTilePal, int selTileWidth, int selTileHeight);
        public event TileSelectedd TileSelected;


        int hovertx = -1;
        int hoverty = -1;


        int downTileNum;

        public int selTileNum = 0;
        public int selTilePal = 0;
        public int selTileWidth = 1;
        public int selTileHeight = 1;

        public Bitmap[] buffers;
        public int bufferWidth, bufferHeight;
        public int bufferCount;
        public int tileSize;
        private int baseTileSize;
        private int zoomLevel = 1;

        public bool allowsRectangle = true;
        public bool allowNoTile = true;
        private readonly HashSet<int> highlightedTiles = new HashSet<int>();
        private bool showHighlightedTiles = false;
        private Color highlightOutlineColor = Color.FromArgb(220, 255, 200, 0);
        private const float HighlightOutlineWidth = 2.0f;
        private const int HighlightFillAlpha = 96;

        public TilePicker()
        {
            InitializeComponent();
            this.SetStyle(ControlStyles.Selectable, true);
        }

        public void init(Bitmap[] buffers, int tileSize)
        {
            this.buffers = buffers;
            this.bufferCount = buffers.Length;
            this.baseTileSize = tileSize;

            this.bufferHeight = buffers[0].Height / baseTileSize;
            this.bufferWidth = buffers[0].Width / baseTileSize;
            ApplyZoom();
        }

        public void SetZoom(int zoom)
        {
            int clamped = Math.Max(MinZoom, Math.Min(MaxZoom, zoom));
            if (clamped == zoomLevel)
                return;

            zoomLevel = clamped;
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            tileSize = baseTileSize * zoomLevel;
            if (buffers == null)
                return;

            pictureBox1.Size = new Size(bufferWidth * tileSize, bufferHeight * bufferCount * tileSize);
            pictureBox1.Invalidate(true);
        }

        public void SetTileset(NSMBTileset t)
        {
        }

        public void SetHighlightTiles(IEnumerable<int> tileNumbers)
        {
            highlightedTiles.Clear();
            if (tileNumbers != null)
            {
                foreach (int tileNumber in tileNumbers)
                {
                    if (tileNumber >= 0)
                        highlightedTiles.Add(tileNumber);
                }
            }

            pictureBox1.Invalidate(true);
        }

        public void SetHighlightVisible(bool visible)
        {
            if (showHighlightedTiles == visible)
                return;

            showHighlightedTiles = visible;
            pictureBox1.Invalidate(true);
        }

        public void SetHighlightOutlineColor(Color color)
        {
            highlightOutlineColor = color;
            pictureBox1.Invalidate(true);
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (buffers == null)
                return;

            e.Graphics.FillRectangle(Brushes.DarkSlateGray,
                0, 0, bufferWidth * tileSize, bufferHeight * tileSize * bufferCount);

            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            for (int i = 0; i < bufferCount; i++)
                e.Graphics.DrawImage(buffers[i], 0, i * bufferHeight * tileSize, bufferWidth * tileSize, bufferHeight * tileSize);

            if (showHighlightedTiles && highlightedTiles.Count > 0)
            {
                using (Pen highlightPen = new Pen(highlightOutlineColor, HighlightOutlineWidth))
                using (SolidBrush highlightBrush = new SolidBrush(Color.FromArgb(HighlightFillAlpha, highlightOutlineColor.R, highlightOutlineColor.G, highlightOutlineColor.B)))
                {
                    foreach (int tileNumber in highlightedTiles)
                    {
                        int tx = tileNumber % bufferWidth;
                        int ty = tileNumber / bufferWidth;
                        if (tx < 0 || tx >= bufferWidth || ty < 0 || ty >= bufferHeight)
                            continue;

                        if ((tx & 1) == 0 && (ty & 1) == 0)
                        {
                            int right = tileNumber + 1;
                            int bottom = tileNumber + bufferWidth;
                            int bottomRight = bottom + 1;
                            if (highlightedTiles.Contains(right) && highlightedTiles.Contains(bottom) && highlightedTiles.Contains(bottomRight))
                            {
                                for (int pal = 0; pal < bufferCount; pal++)
                                {
                                    int drawX = tx * tileSize;
                                    int drawY = (ty + pal * bufferHeight) * tileSize;
                                    Rectangle map16Rect = new Rectangle(drawX, drawY, tileSize * 2, tileSize * 2);
                                    e.Graphics.FillRectangle(highlightBrush, map16Rect);
                                    e.Graphics.DrawRectangle(highlightPen, map16Rect.X, map16Rect.Y, map16Rect.Width - 1, map16Rect.Height - 1);
                                }
                            }
                        }
                    }
                }
            }

            e.Graphics.DrawRectangle(Pens.White,
                (selTileNum % bufferWidth) * tileSize, (selTileNum / bufferWidth + selTilePal * bufferHeight) * tileSize,
                selTileWidth * tileSize, selTileHeight * tileSize);

            if(!down && hovertx != -1)
                e.Graphics.DrawRectangle(Pens.White,
                    hovertx * tileSize, hoverty*tileSize,
                    tileSize, tileSize);
        }

        bool down = false;

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            down = true;

            int tx = e.X / tileSize;
            if (tx >= bufferWidth) tx = bufferWidth-1;
            if (tx < 0) tx = 0;
            int ty = e.Y / tileSize;
            if (ty >= bufferHeight * bufferCount) ty = bufferHeight * bufferCount - 1;
            if (ty < 0) ty = 0;

            selTilePal = ty / bufferHeight;
            ty %= bufferHeight;

            downTileNum = ty * bufferWidth + tx;

            pictureBox1_MouseMove(sender, e);

            this.Focus();
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (down)
            {
                int dx = downTileNum % bufferWidth;
                int dy = downTileNum / bufferWidth;

                int tx = e.X / tileSize;
                if (tx >= bufferWidth) tx = bufferWidth - 1;
                if (tx < 0) tx = 0;

                int ty = e.Y / tileSize;
                ty -= bufferHeight * selTilePal;
                if (ty >= bufferHeight) ty = bufferHeight - 1;
                if (ty < 0) ty = 0;

                int xmin = Math.Min(dx, tx);
                int ymin = Math.Min(dy, ty);
                int xmax = Math.Max(dx, tx);
                int ymax = Math.Max(dy, ty);

                selTileNum = xmin + ymin * bufferWidth;
                selTileWidth = xmax - xmin + 1;
                selTileHeight = ymax - ymin + 1;
            }
            else
            {
                int tx = e.X / tileSize;
                if (tx >= bufferWidth) tx = bufferWidth - 1;
                if (tx < 0) tx = 0;
                int ty = e.Y / tileSize;
                if (ty >= bufferHeight * bufferCount) ty = bufferHeight * bufferCount - 1;
                if (ty < 0) ty = 0;

                hovertx = tx;
                hoverty = ty;
            }
            pictureBox1.Invalidate(true);
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (down)
            {
                if (TileSelected != null)
                    TileSelected(selTileNum, selTilePal, selTileWidth, selTileHeight);
            }
            down = false;
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            hovertx = -1;
            hoverty = -1;
            pictureBox1.Invalidate(true);
        }


        private void TilePicker_KeyPress(object sender, KeyPressEventArgs e)
        {

        }

    }
}
