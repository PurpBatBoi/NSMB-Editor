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

using NSMBe5.DSFileSystem;

namespace NSMBe5
{
    /// <summary>
    /// A tilemap whose tiles are stored as 2×2 Map16 blocks rather than
    /// individual 8px tiles. Each Map16 entry occupies 4 consecutive ushorts
    /// in the file (top-left, top-right, bottom-left, bottom-right).
    /// </summary>
    public class Map16Tilemap : Tilemap
    {
        // ── Construction ──────────────────────────────────────────────────────

        public Map16Tilemap(File file, int tileWidth, Image2D tileset, Palette[] palettes, int tileOffset, int paletteOffset)
            : base(file, tileWidth, tileset, palettes, tileOffset, paletteOffset)
        {
        }

        // ── Overrides ─────────────────────────────────────────────────────────

        protected override void Load()
        {
            height = (_file.fileSize / 2 + width - 1) / width;
            tiles  = new Tile[width, height];

            var stream    = new ByteArrayInputStream(_file.getContents());
            int blockCols = width / 2;

            // Each iteration reads one 2×2 Map16 block (4 ushorts).
            for (int i = 0; i < _file.fileSize / 8; i++)
            {
                int x = (i % blockCols) * 2;
                int y = (i / blockCols) * 2;

                tiles[x,     y    ] = ShortToTile(stream.readUShort());
                tiles[x + 1, y    ] = ShortToTile(stream.readUShort());
                tiles[x,     y + 1] = ShortToTile(stream.readUShort());
                tiles[x + 1, y + 1] = ShortToTile(stream.readUShort());
            }
        }

        public override void Save()
        {
            var os        = new ByteArrayOutputStream();
            int blockCols = width / 2;

            for (int i = 0; i < _file.fileSize / 8; i++)
            {
                int x = (i % blockCols) * 2;
                int y = (i / blockCols) * 2;

                os.writeUShort(TileToShort(tiles[x,     y    ]));
                os.writeUShort(TileToShort(tiles[x + 1, y    ]));
                os.writeUShort(TileToShort(tiles[x,     y + 1]));
                os.writeUShort(TileToShort(tiles[x + 1, y + 1]));
            }

            _file.replace(os.getArray(), this);
        }
    }
}