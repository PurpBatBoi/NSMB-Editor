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

using System.Drawing;
using NSMBe5.DSFileSystem;

namespace NSMBe5
{
    public class Tilemap
    {
        // ── Constants ─────────────────────────────────────────────────────────
        private const int TilePixelSize = 8;

        // ── Public state ──────────────────────────────────────────────────────
        public Tile[,]   tiles;
        public int       width;
        public int       height;
        public Palette[] palettes;
        public Image2D   tileset;
        public int       tileCount;
        public int       tileOffset;
        public int       paletteOffset;
        public Bitmap[]  buffers;
        public Bitmap    buffer;

        // ── Protected state ───────────────────────────────────────────────────
        protected File _file;

        // ── Private state ─────────────────────────────────────────────────────
        private Graphics _bufferGraphics;

        // ── Construction ──────────────────────────────────────────────────────

        public Tilemap(File file, int tileWidth, Image2D tileset, Palette[] palettes, int tileOffset, int paletteOffset)
        {
            _file              = file;
            width              = tileWidth;
            this.tileOffset    = tileOffset;
            this.paletteOffset = paletteOffset;
            this.tileset       = tileset;
            this.palettes      = palettes;
            tileCount          = tileset.getWidth() * tileset.getHeight() / 64;

            Load();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void BeginEdit() => _file.beginEdit(this);
        public void EndEdit()   => _file.endEdit(this);

        public Tile GetTileAtPos(int x, int y) => tiles[x, y];

        public int GetMap16TileCount() => tiles.Length / 4;

        public void ReloadResources(bool reloadImage, bool reloadPalettes, bool reloadLayout)
        {
            if (reloadImage && tileset != null)
                tileset.reload();

            if (reloadPalettes && palettes != null)
                ReloadPalettes();

            if (reloadLayout)
                Load();

            if (buffers == null || buffer == null)
                Render();
            else
                ReRenderAll();
        }

        public void UpdateBuffers()
        {
            buffers = new Bitmap[palettes.Length];
            for (int i = 0; i < palettes.Length; i++)
                buffers[i] = tileset.render(palettes[i]);
        }

        public Bitmap Render()
        {
            if (buffers == null)
                UpdateBuffers();

            if (buffer == null)
            {
                buffer          = new Bitmap(width * TilePixelSize, height * TilePixelSize,
                                      System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                _bufferGraphics = Graphics.FromImage(buffer);
                _bufferGraphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            }

            ReRenderAll();
            return buffer;
        }

        public Bitmap ReRenderAll()
        {
            UpdateBuffers();
            return ReRender(0, 0, width, height);
        }

        public Bitmap ReRender(int xMin, int yMin, int regionWidth, int regionHeight)
        {
            for (int x = xMin; x < xMin + regionWidth; x++)
            {
                for (int y = yMin; y < yMin + regionHeight; y++)
                {
                    if (x >= width || y >= height)
                        continue;

                    Tile t = tiles[x, y];

                    if (t.tileNum < 0 || t.tileNum >= tileCount)
                    {
                        _bufferGraphics.FillRectangle(Brushes.Transparent,
                            x * TilePixelSize, y * TilePixelSize, TilePixelSize, TilePixelSize);
                        continue;
                    }

                    if (t.palNum < 0 || t.palNum >= palettes.Length)
                        continue;

                    DrawTile(t, x, y);
                }
            }

            return buffer;
        }

        public virtual void Save()
        {
            var os = new ByteArrayOutputStream();

            for (int i = 0; i < _file.fileSize / 2; i++)
            {
                int x = i % width;
                int y = i / width;
                os.writeUShort(TileToShort(tiles[x, y]));
            }

            _file.replace(os.getArray(), this);
        }

        // ── Tile encoding / decoding ──────────────────────────────────────────

        public ushort TileToShort(Tile t)
        {
            ushort result = 0;

            if (t.tileNum != -1)
                result |= (ushort)((t.tileNum + tileOffset) & 0x3FF);

            result |= (ushort)((t.hflip ? 1 : 0) << 10);
            result |= (ushort)((t.vflip ? 1 : 0) << 11);
            result |= (ushort)(((t.palNum + paletteOffset) & 0x00F) << 12);

            return result;
        }

        public Tile ShortToTile(ushort value)
        {
            var t = new Tile();

            t.tileNum = (value & 0x3FF) - tileOffset;
            if (t.tileNum < 0)
                t.tileNum = -1;

            t.hflip  = ((value >> 10) & 1) == 1;
            t.vflip  = ((value >> 11) & 1) == 1;
            t.palNum = ((value >> 12) & 0xF) - paletteOffset;

            return t;
        }

        // ── Protected methods ─────────────────────────────────────────────────

        protected virtual void Load()
        {
            height = (_file.fileSize / 2 + width - 1) / width;
            tiles  = new Tile[width, height];

            var stream = new ByteArrayInputStream(_file.getContents());

            for (int i = 0; i < _file.fileSize / 2; i++)
            {
                int x = i % width;
                int y = i / width;
                tiles[x, y] = ShortToTile(stream.readUShort());
            }
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void ReloadPalettes()
        {
            foreach (Palette palette in palettes)
            {
                if (!(palette is FilePalette filePalette))
                    continue;

                filePalette.pal = FilePalette.arrayToPalette(filePalette.SourceFile.getContents());
                if (filePalette.pal.Length > 0)
                    filePalette.pal[0] = Color.Transparent;
            }
        }

        private void DrawTile(Tile t, int destX, int destY)
        {
            Rectangle srcRect = Image2D.getTileRectangle(buffers[t.palNum], TilePixelSize, t.tileNum);
            var tileRect      = new Rectangle(0, 0, TilePixelSize, TilePixelSize);
            var destRect      = new Rectangle(destX * TilePixelSize, destY * TilePixelSize, TilePixelSize, TilePixelSize);

            using (var tileSrc = new Bitmap(TilePixelSize, TilePixelSize, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
            {
                using (Graphics g = Graphics.FromImage(tileSrc))
                    g.DrawImage(buffers[t.palNum], tileRect, srcRect, GraphicsUnit.Pixel);

                // Clone before flipping — RotateFlip on the source bitmap causes GDI+ issues.
                using (var tile = (Bitmap)tileSrc.Clone())
                {
                    ApplyFlip(tile, t.hflip, t.vflip);
                    _bufferGraphics.DrawImage(tile, destRect, tileRect, GraphicsUnit.Pixel);
                }
            }
        }

        private static void ApplyFlip(Bitmap bitmap, bool hflip, bool vflip)
        {
            if      (hflip && vflip)  bitmap.RotateFlip(RotateFlipType.RotateNoneFlipXY);
            else if (hflip)           bitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            else if (vflip)           bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
            // No flip needed — leave bitmap unchanged.
        }

        // ── Tile struct ───────────────────────────────────────────────────────

        public struct Tile
        {
            public int  tileNum;
            public int  palNum;
            public bool hflip;
            public bool vflip;
        }
    }
}