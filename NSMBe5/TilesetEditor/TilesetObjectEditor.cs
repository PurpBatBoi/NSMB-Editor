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
using System.Drawing.Imaging;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace NSMBe5
{
    public partial class TilesetObjectEditor : UserControl
    {
        private const int PreviewZoom = 2;
        private const int BaseTileSize = 16;
        private int objectEditorZoom = 3;
        private int map16TilesZoom = 1;
        private readonly Dictionary<int, Bitmap> scaledMap16TileCache = new Dictionary<int, Bitmap>();
        List<NSMBTileset.ObjectDefTile> selRow;
        NSMBTileset.ObjectDefTile selTile;
        NSMBTileset.ObjectDef obj;
        NSMBTileset tls;
        NSMBTile previewTile;
        public int tnum;
        public TextBox descBox;
        public Label descLbl;

        public delegate void mustRepaintObjectsD();
        public event mustRepaintObjectsD mustRepaintObjects;
        public delegate void changeDescription();
        public event changeDescription DescriptionChanged;

        public TilesetObjectEditor()
        {
            InitializeComponent();
            LanguageManager.ApplyToContainer(this, "TilesetObjectEditor");
            descBox = desc;
            descLbl = description;
        }

        public void setObject(int num)
        {
            if (tls.Objects[num] == null)
                return;
            previewTile.TileID = num;
            grpTileSettings.Visible = false;
            obj = tls.Objects[num];
            if (obj.tiles.Count == 0)
                obj.tiles.Add(new List<NSMBTileset.ObjectDefTile>());
            selTile = null;
            selRow = obj.tiles[0];
            DataUpdateFlag = true;
            objWidth.Value = obj.width;
            objHeight.Value = obj.height;
            DataUpdateFlag = false;
            grpTileSettings.Visible = selTile != null;
            repaint();
        }

        public void repaint()
        {
            try
            {
                previewTile.UpdateTileCache();
            }
            catch (Exception)
            {
            }
            editZone.Invalidate(true);
            previewBox.Invalidate(true);
            if (mustRepaintObjects != null)
                mustRepaintObjects();
        }

        public void load(NSMBGraphics g, int TilesetNumber)
        {
            this.tnum = TilesetNumber;
            this.tls = g.Tilesets[tnum];
            ClearScaledMap16TileCache();
            tilePicker1.init(new Bitmap[] { tls.map16.buffer }, 16);
            tilePicker1.SetZoom(map16TilesZoom);
            previewTile = new NSMBTile(0, tnum, 0, 0, 6, 6, g);
        }

        private void ClearScaledMap16TileCache()
        {
            foreach (Bitmap bmp in scaledMap16TileCache.Values)
                bmp.Dispose();
            scaledMap16TileCache.Clear();
        }

        private static Bitmap ExtractMap16Tile(Bitmap map16Buffer, int tileId)
        {
            Bitmap src = new Bitmap(BaseTileSize, BaseTileSize, PixelFormat.Format32bppArgb);
            Rectangle srcRect = Image2D.getTileRectangle(map16Buffer, BaseTileSize, tileId);
            using (Graphics g = Graphics.FromImage(src))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.DrawImage(map16Buffer, new Rectangle(0, 0, BaseTileSize, BaseTileSize), srcRect, GraphicsUnit.Pixel);
            }
            return src;
        }

        private static Bitmap ScaleBitmapNearest(Bitmap source, int scale)
        {
            if (scale <= 1)
                return (Bitmap)source.Clone();

            int dstWidth = source.Width * scale;
            int dstHeight = source.Height * scale;
            Bitmap dst = new Bitmap(dstWidth, dstHeight, PixelFormat.Format32bppArgb);

            Rectangle srcRect = new Rectangle(0, 0, source.Width, source.Height);
            Rectangle dstRect = new Rectangle(0, 0, dstWidth, dstHeight);
            Bitmap src32 = source.PixelFormat == PixelFormat.Format32bppArgb
                ? source
                : source.Clone(srcRect, PixelFormat.Format32bppArgb);
            bool disposeSrc32 = !ReferenceEquals(src32, source);

            BitmapData srcData = src32.LockBits(srcRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData dstData = dst.LockBits(dstRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                int srcStride = srcData.Stride;
                int dstStride = dstData.Stride;
                int srcBytesLen = srcStride * source.Height;
                int dstBytesLen = dstStride * dstHeight;
                byte[] srcBytes = new byte[srcBytesLen];
                byte[] dstBytes = new byte[dstBytesLen];
                Marshal.Copy(srcData.Scan0, srcBytes, 0, srcBytesLen);

                for (int y = 0; y < source.Height; y++)
                {
                    int srcRow = y * srcStride;
                    for (int sy = 0; sy < scale; sy++)
                    {
                        int dstRow = (y * scale + sy) * dstStride;
                        for (int x = 0; x < source.Width; x++)
                        {
                            int srcPx = srcRow + x * 4;
                            byte b = srcBytes[srcPx + 0];
                            byte g = srcBytes[srcPx + 1];
                            byte r = srcBytes[srcPx + 2];
                            byte a = srcBytes[srcPx + 3];

                            for (int sx = 0; sx < scale; sx++)
                            {
                                int dstPx = dstRow + (x * scale + sx) * 4;
                                dstBytes[dstPx + 0] = b;
                                dstBytes[dstPx + 1] = g;
                                dstBytes[dstPx + 2] = r;
                                dstBytes[dstPx + 3] = a;
                            }
                        }
                    }
                }
                Marshal.Copy(dstBytes, 0, dstData.Scan0, dstBytesLen);
            }
            finally
            {
                src32.UnlockBits(srcData);
                dst.UnlockBits(dstData);
                if (disposeSrc32)
                    src32.Dispose();
            }

            return dst;
        }

        private Bitmap GetScaledMap16Tile(int tileId, int editorTileSize)
        {
            int cacheKey = (editorTileSize << 16) | (tileId & 0xFFFF);
            Bitmap cached;
            if (scaledMap16TileCache.TryGetValue(cacheKey, out cached))
                return cached;

            using (Bitmap tile = ExtractMap16Tile(tls.Map16Buffer, tileId))
            {
                Bitmap scaled = ScaleBitmapNearest(tile, editorTileSize / BaseTileSize);
                scaledMap16TileCache[cacheKey] = scaled;
                return scaled;
            }
        }

        private void editZone_Paint(object sender, PaintEventArgs e)
        {
            if (tls == null) return;
            if (obj == null) return;

            Graphics g = e.Graphics;
            int editorTileSize = BaseTileSize * objectEditorZoom;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            g.FillRectangle(Brushes.LightSteelBlue, 0, 0, editZone.Width, editZone.Height);

            int x = editorTileSize;
            int y = 0;

            foreach (List<NSMBTileset.ObjectDefTile> row in obj.tiles)
            {
                foreach (NSMBTileset.ObjectDefTile t in row)
                {
                    if (t.controlTile)
                    {
                        g.FillRectangle(Brushes.White, x, y, editorTileSize - 1, editorTileSize - 1);
                        g.DrawRectangle(Pens.Black, x, y, editorTileSize - 1, editorTileSize - 1);
                        g.DrawString(String.Format("{0:X2}", t.controlByte), NSMBGraphics.InfoFont, Brushes.Black, x, y);
                    }
                    else if (!t.emptyTile)
                    {
                        Bitmap scaledTile = GetScaledMap16Tile(t.tileID, editorTileSize);
                        g.DrawImageUnscaled(scaledTile, x, y);
                        if ((t.controlByte & 1) != 0)
                            g.DrawRectangle(Pens.Red, x, y, editorTileSize - 1, editorTileSize - 1);
                        if ((t.controlByte & 2) != 0)
                            g.DrawRectangle(Pens.Blue, x + 1, y + 1, editorTileSize - 3, editorTileSize - 3);
                    }
                    if (t == selTile)
                        g.DrawRectangle(Pens.White, x, y, editorTileSize - 1, editorTileSize - 1);
                    x += editorTileSize;
                }
                g.DrawString((y / editorTileSize) + "", NSMBGraphics.InfoFont, Brushes.White, 0, y);
                if(selRow == row && selTile == null)
                    g.DrawRectangle(Pens.White, 0, y, editorTileSize - 1, editorTileSize - 1);

                x = editorTileSize;
                y += editorTileSize;
            }

        }

        bool DataUpdateFlag = false;

        private void insertTile(NSMBTileset.ObjectDefTile tile)
        {
            if (selRow == null)
                return;
            if (selTile == null)
                selRow.Insert(0, tile);
            else
                selRow.Insert(selRow.IndexOf(selTile) + 1, tile);

            selectTile(tile);
            repaint();
        }

        private void map16Picker1_TileSelected(int tile)
        {
            if (DataUpdateFlag)
                return;

            if (Control.ModifierKeys == Keys.Control)
            {
                NSMBTileset.ObjectDefTile nt = new NSMBTileset.ObjectDefTile(tls);
                nt.tileID = tile;
                insertTile(nt);
            }
            else if(selTile != null)
            {
                DataUpdateFlag = true;
                selTile.tileID = tile;
                map16Tile.Value = tile;
                DataUpdateFlag = false;
            }

            repaint();
        }

        private void editZone_MouseDown(object sender, MouseEventArgs e)
        {
            if (obj == null)
                return;

            int editorTileSize = BaseTileSize * objectEditorZoom;
            int tx = e.X / editorTileSize - 1;
            int ty = e.Y / editorTileSize;

            if(tx < -1) return;
            if(ty < 0) return;

            if (obj.tiles.Count > ty)
                if (obj.tiles[ty].Count > tx)
                {
                    selRow = obj.tiles[ty];
                    if (e.Button == MouseButtons.Right && tx >= 0)
                    {
                        selRow.RemoveAt(tx);
                        if (selRow.Count > 0)
                        {
                            int nextTile = Math.Min(tx, selRow.Count - 1);
                            selectTile(selRow[nextTile]);
                        }
                        else
                        {
                            selTile = null;
                        }

                        grpTileSettings.Visible = selTile != null;
                        repaint();
                        return;
                    }

                    if (tx == -1)
                    {
                        selTile = null;
                        if (Control.ModifierKeys == Keys.Control)
                            obj.tiles.Remove(selRow);
                    }
                    else
                    {
                        selectTile(selRow[tx]);
                        if (Control.ModifierKeys == Keys.Control)
                        {
                            selRow.Remove(selTile);
                            selTile = null;
                        }
                        else if (Control.ModifierKeys == Keys.Shift)
                            selTile.controlByte ^= 2;
                        else if (Control.ModifierKeys == Keys.Alt)
                            selTile.controlByte ^= 1;
                    }
                    grpTileSettings.Visible = selTile != null;
                    if (selTile != null) {
                        tilePicker1.selTileHeight = 1;
                        tilePicker1.selTileWidth = 1;
                        tilePicker1.selTileNum = selTile.tileID;
                        tilePicker1.Invalidate(true);
                    }
                    repaint();
                }
        }

        private void selectTile(NSMBTileset.ObjectDefTile tile)
        {
            selTile = tile;
            DataUpdateFlag = true;
            map16Tile.Value = selTile.tileID;
            controlByte.Value = selTile.controlByte;
//            map16Picker1.selectTile(selTile.tileID);
            grpTileSettings.Visible = selTile != null;

            DataUpdateFlag = false;
        }

        private void map16Tile_ValueChanged(object sender, EventArgs e)
        {
            if (DataUpdateFlag)
                return;
            if (selTile == null)
                return;

            selTile.tileID = (int)map16Tile.Value;
//            map16Picker1.selectTile(selTile.tileID);
            repaint();
        }

        private void controlByte_ValueChanged(object sender, EventArgs e)
        {
            if (DataUpdateFlag)
                return;
            if (selTile == null)
                return;

            selTile.controlByte = (byte)controlByte.Value;
            repaint();
        }

        private void objWidth_ValueChanged(object sender, EventArgs e)
        {
            if (DataUpdateFlag)
                return;
            if (obj == null)
                return;
            obj.width = (int)objWidth.Value;
        }

        private void objHeight_ValueChanged(object sender, EventArgs e)
        {
            if (DataUpdateFlag)
                return;
            if (obj == null)
                return;
            obj.height = (int)objHeight.Value;
        }

        private void previewBox_Paint(object sender, PaintEventArgs e)
        {
            if (previewTile == null)
                return;

            int nativeWidth = Math.Max(1, previewTile.Width * BaseTileSize);
            int nativeHeight = Math.Max(1, previewTile.Height * BaseTileSize);

            using (Bitmap native = new Bitmap(nativeWidth, nativeHeight, PixelFormat.Format32bppArgb))
            {
                using (Graphics g = Graphics.FromImage(native))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    g.FillRectangle(Brushes.LightSteelBlue, 0, 0, nativeWidth, nativeHeight);
                    previewTile.RenderPlain(g, 0, 0);
                }

                using (Bitmap scaled = ScaleBitmapNearest(native, PreviewZoom))
                {
                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    e.Graphics.FillRectangle(Brushes.LightSteelBlue, 0, 0, previewBox.Width, previewBox.Height);
                    e.Graphics.DrawImageUnscaled(scaled, 0, 0);
                }
            }
        }

        private void previewBox_MouseDown(object sender, MouseEventArgs e)
        {
            previewBox_MouseMove(sender, e);
        }

        private void previewBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int previewTileSize = BaseTileSize * PreviewZoom;
                previewTile.Width = e.X / previewTileSize + 1;
                previewTile.Height = e.Y / previewTileSize + 1;
                if (previewTile.Width < 1)
                    previewTile.Width = 1;
                if (previewTile.Height < 1)
                    previewTile.Height = 1;
                repaint();
            }
        }

        private void newLineButton_Click(object sender, EventArgs e)
        {
            newLine();
        }

        private void newLine()
        {
            if (selRow == null)
                return;

            obj.tiles.Insert(obj.tiles.IndexOf(selRow) + 1, new List<NSMBTileset.ObjectDefTile>());
            selRow = obj.tiles[obj.tiles.IndexOf(selRow) + 1];
            repaint();
        }

        public void redrawThings()
        {
            ClearScaledMap16TileCache();
            tilePicker1.init(new Bitmap[] { tls.map16.buffer }, 16);
            tilePicker1.SetZoom(map16TilesZoom);
        }

        private void emptyTileButton_Click(object sender, EventArgs e) {
            NSMBTileset.ObjectDefTile t = new NSMBTileset.ObjectDefTile(tls);
            t.tileID = -1;
            insertTile(t);
        }

        private void slopeControlButton_Click(object sender, EventArgs e) {
            NSMBTileset.ObjectDefTile tile = new NSMBTileset.ObjectDefTile(tls);
            tile.controlByte = 0x80;
            insertTile(tile);
        }

        private void desc_TextChanged(object sender, EventArgs e)
        {
            DescriptionChanged();
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            if (selRow == null)
                return;
            if (selTile == null)
            {
                if (obj.tiles.Count - 1 == obj.tiles.IndexOf(selRow))
                    if (obj.tiles.IndexOf(selRow) == 0)
                        return;
                    else
                    {
                        selRow = obj.tiles[obj.tiles.IndexOf(selRow) - 1];
                        obj.tiles.RemoveAt(obj.tiles.IndexOf(selRow) + 1);
                    }
                else
                {
                    selRow = obj.tiles[obj.tiles.IndexOf(selRow) + 1];
                    obj.tiles.RemoveAt(obj.tiles.IndexOf(selRow) - 1);
                }
            }
            else
            {
                if (selRow.Count - 1 == selRow.IndexOf(selTile))
                    if (selRow.IndexOf(selTile) == 0)
                    {
                        selRow.Remove(selTile);
                            selTile=null;
                    }
                    else
                    {
                        selTile = selRow[selRow.IndexOf(selTile) - 1];
                        selRow.RemoveAt(selRow.IndexOf(selTile) + 1);
                    }
                else
                {
                    selTile = selRow[selRow.IndexOf(selTile) + 1];
                    selRow.RemoveAt(selRow.IndexOf(selTile) - 1);
                }
            }
            repaint();
        }

        private void tilePicker1_TileSelected(int selTileNum, int selTilePal, int selTileWidth, int selTileHeight)
        {
            for (int y = 0; y < selTileHeight; y++)
            {
                if (y != 0)
                    newLine();
                for(int x = 0; x < selTileWidth; x++)
                {
                    NSMBTileset.ObjectDefTile tile = new NSMBTileset.ObjectDefTile(tls);
                    tile.tileID = selTileNum + x + y * tilePicker1.bufferWidth;
                    insertTile(tile);
                }
            }
        }

        private void objectEditorZoomUpDown_ValueChanged(object sender, EventArgs e)
        {
            objectEditorZoom = (int)objectEditorZoomUpDown.Value;
            ClearScaledMap16TileCache();
            editZone.Invalidate(true);
        }

        private void map16ZoomUpDown_ValueChanged(object sender, EventArgs e)
        {
            map16TilesZoom = (int)map16ZoomUpDown.Value;
            tilePicker1.SetZoom(map16TilesZoom);
        }

    }
}
