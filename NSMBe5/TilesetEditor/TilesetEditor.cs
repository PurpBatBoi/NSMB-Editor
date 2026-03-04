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
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NSMBe5
{
    public partial class TilesetEditor : Form
    {
        NSMBTileset t;
        NSMBGraphics g;
        int TilesetNumber;
        ushort TilesetID;
        List<string> descriptions;
        bool descExists;

        public TilesetEditor(ushort TilesetID, string tilesetName) {
            InitializeComponent();
            LanguageManager.ApplyToContainer(this, "TilesetEditor");
            savePNG.Filter = LanguageManager.Get("Filters", "png");
            openPNG.Filter = LanguageManager.Get("Filters", "png");
            saveTileset.Filter = LanguageManager.Get("Filters", "tileset");
            openTileset.Filter = LanguageManager.Get("Filters", "tileset");

            Text = string.Format(LanguageManager.Get("TilesetEditor", "_TITLE"), tilesetName);

            g = new NSMBGraphics();

            this.TilesetID = TilesetID;
            if (TilesetID == 65535)
            {
                // load Jyotyu
                g.LoadTilesets(0);
                TilesetNumber = 0;
            }
            else if (TilesetID == 65534)
            {
                // load Nohara_sub
                g.LoadTilesets(2);
                TilesetNumber = 2;
            }
            else
            {
                // load a normal tileset
                g.LoadTilesets(TilesetID);
                TilesetNumber = 1;
            }

            t = g.Tilesets[TilesetNumber];
            t.beginEdit();

            objectPickerControl1.Initialise(g);
            objectPickerControl1.CurrentTileset = TilesetNumber;

            tilesetObjectEditor1.load(g, TilesetNumber);
            tilemapEditor1.load(t.map16);
            tilemapEditor1.ConfigureRandomizationToggleButtons(
                Properties.Settings.Default.ShowMap16RandomizationHighlight,
                Properties.Settings.Default.ShowMap16RandomizationHighlightPicker,
                Color.FromArgb(Properties.Settings.Default.Map16RandomizationOutlineColorArgb),
                LanguageManager.Get("TilesetEditor", "showRandomizationTilesCanvas"),
                LanguageManager.Get("TilesetEditor", "showRandomizationTilesPicker"),
                LanguageManager.Get("TilesetEditor", "randomizationOutlineColor"));
            tilemapEditor1.CanvasRandomizationVisibilityChanged += tilemapEditor1_CanvasRandomizationVisibilityChanged;
            tilemapEditor1.PickerRandomizationVisibilityChanged += tilemapEditor1_PickerRandomizationVisibilityChanged;
            tilemapEditor1.RandomizationOutlineColorChanged += tilemapEditor1_RandomizationOutlineColorChanged;
            ApplyMap16RandomizationOverlay();

            imageManager1.addImage(t.graphics);

            for(int i = 0; i < t.palettes.Length; i++)
	            imageManager1.addPalette(t.palettes[i]);

            tileBehaviorPicker.init(new Bitmap[] { t.Map16Buffer }, 16);
            tileBehaviorPicker.SetZoom((int)behaviorZoomUpDown.Value);

            //FIXME
//            graphicsEditor1.SaveGraphics += new GraphicsEditor.SaveGraphicsHandler(graphicsEditor1_SaveGraphics);
            
            descExists = ROM.UserInfo.descriptions.ContainsKey(TilesetID); //Fild in there are descriptions for the tileset
            deleteDescriptions.Visible = descExists; //Make the appropriate button visible
            createDescriptions.Visible = !descExists;
            tilesetObjectEditor1.descBox.Visible = descExists; //Hide or show the description text box
            tilesetObjectEditor1.descLbl.Visible = descExists;
            if (descExists) {
                descriptions = ROM.UserInfo.descriptions[TilesetID]; //Get the descriptions
                tilesetObjectEditor1.descBox.Text = descriptions[0]; //Fill the description box with that of the first object
            }
            this.Icon = Properties.Resources.nsmbe;
			Program.ApplyFontToControls(Controls);
		}

        private void objectPickerControl1_ObjectSelected()
        {
            int sel = objectPickerControl1.SelectedObject;
            if (t.Objects.Length <= sel)
                return;

            for (int i = 0; i <= sel; i++)
                if (t.Objects[i] == null)
                    t.Objects[i] = new NSMBTileset.ObjectDef(t);

            tilesetObjectEditor1.setObject(objectPickerControl1.SelectedObject);
            if (tilesetObjectEditor1.descBox.Visible)
                tilesetObjectEditor1.descBox.Text = descriptions[objectPickerControl1.SelectedObject];
            objectPickerControl1.ReRenderAll(TilesetNumber);
        }


        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            // auto save graphics, I always end up forgetting..
            imageManager1.saveAll();

            t.save();
            ROM.UserInfo.SaveFile();
        }

        private void mustRepaintObjects()
        {
            t.map16.reRenderAll();
            tilemapEditor1.Invalidate(true);
            tilemapEditor1.reload();
        }

        private void TilesetEditor_FormClosed(object sender, FormClosedEventArgs e)
        {
            t.endEdit();
            g.close();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(LanguageManager.Get("TilesetEditor", "sureDelAll"), LanguageManager.Get("TilesetEditor", "titleSureDelAll"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                t.Objects = new NSMBTileset.ObjectDef[256];
                for(int x = 0; x < t.map16.width; x++)
                    for (int y = 0; y < t.map16.height; y++)
                    {
                        t.map16.tiles[x, y].tileNum = -1;
                        t.map16.tiles[x, y].hflip = false;
                        t.map16.tiles[x, y].vflip = false;
                        t.map16.tiles[x, y].palNum = 0;
                    }

                for (int i = 0; i < t.TileBehaviors.Length; i++)
                    t.TileBehaviors[i] = 0;

                mustRepaintObjects();
            }
        }
        
        private void createDescriptions_Click(object sender, EventArgs e)
        {
            ROM.UserInfo.createDescriptions(TilesetID);
            descriptions = ROM.UserInfo.descriptions[TilesetID];
            t.UseNotes = true;
            t.ObjNotes = descriptions.ToArray();
            descExists = true;
            createDescriptions.Visible = false;
            deleteDescriptions.Visible = true;
            tilesetObjectEditor1.descBox.Visible = true;
            tilesetObjectEditor1.descBox.Text = "";
            tilesetObjectEditor1.descLbl.Visible = true;
        }

        private void tilesetObjectEditor1_DescriptionChanged()
        {
            int num = objectPickerControl1.SelectedObject;
            descriptions[num] = num.ToString() + "=" + tilesetObjectEditor1.descBox.Text;
            t.ObjNotes[num] = tilesetObjectEditor1.descBox.Text;
            ROM.UserInfo.descriptions[TilesetID][num] = tilesetObjectEditor1.descBox.Text;
            objectPickerControl1.Invalidate(true);
        }

        private void deleteDescriptions_Click(object sender, EventArgs e)
        {
            if (!descExists) return;
            if (MessageBox.Show(LanguageManager.Get("TilesetEditor", "sureDelDescriptions"), LanguageManager.Get("TilesetEditor", "titleDelDescriptions"), MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                return;
            ROM.UserInfo.descriptions.Remove(TilesetID);
            createDescriptions.Visible = true;
            deleteDescriptions.Visible = false;
            tilesetObjectEditor1.descBox.Visible = false;
            tilesetObjectEditor1.descLbl.Visible = false;
            if (TilesetID != 65535) // The Jyotyu tileset
                t.UseNotes = false;
            else //Restore the original notes
                t.ObjNotes = NSMBGraphics.GetDescriptions(LanguageManager.GetList("ObjNotes"));
            descExists = false;
            objectPickerControl1.Invalidate(true);
        }

        private void copyPalettes_Click(object sender, EventArgs e)
        {
        	int repeat = 12;
        	
        	//Copy map16

        	for(int p = 1; p < t.palettes.Length; p++)
        		for(int x = 0; x < 32; x++)
        			for(int y = 0; y < repeat*2; y++)
        			{
        				t.map16.tiles[x, y+p*repeat*2] = t.map16.tiles[x, y];
        				t.map16.tiles[x, y+p*repeat*2].palNum = (t.map16.tiles[x, y+p*repeat*2].palNum+p)%t.palettes.Length;
        			}
        	
        	//Copy Tile behaviors
        	for(int p = 1; p < t.palettes.Length; p++)
        		for(int x = 0; x < 16*repeat; x++)
        			t.TileBehaviors[x+p*16*repeat] = t.TileBehaviors[x];
        	
        	//And now copy objects. Meh
        	int objCount = 0;
        	while(objCount < t.Objects.Length && t.Objects[objCount] != null)
        		objCount++;
        	
        	for(int p = 1; p < t.palettes.Length; p++)
        	{
        		for(int i = 0; i < objCount; i++)
        		{
        			if(i+p*objCount >= t.Objects.Length)
        				continue;
        			
		    		NSMBTileset.ObjectDef o = new NSMBTileset.ObjectDef(t);
		    		o.tiles.Clear();
		    		foreach(List<NSMBTileset.ObjectDefTile> row in t.Objects[i].tiles)
		    		{
		    			List<NSMBTileset.ObjectDefTile> row2 = new List<NSMBTileset.ObjectDefTile>();
			    		foreach(NSMBTileset.ObjectDefTile tile in row)
		    			{
		    				NSMBTileset.ObjectDefTile tile2 = new NSMBTileset.ObjectDefTile(t);
		    				tile2.tileID = (tile.tileID + repeat*16*p) % 768;
		    				tile2.controlByte = tile.controlByte;
		    				row2.Add(tile2);
		    			}
		    			o.tiles.Add(row2);
		    		}
		    		t.Objects[i+p*objCount] = o;
		    	}
        	}
        	//TODO merp

        	mustRepaintObjects();
        }
        
        private void setend_Click(object sender, EventArgs e)
        {
            int i = 0;
            for (; i <= objectPickerControl1.SelectedObject; i++)
            {
                if (t.Objects[i] == null)
                    t.Objects[i] = new NSMBTileset.ObjectDef(t);
            }
            for (; i <= 255; i++)
            {
                t.Objects[i] = null;
            }
            tilesetObjectEditor1.setObject(objectPickerControl1.SelectedObject);
        }

        private void TilesetEditor_Load(object sender, EventArgs e)
        {
            tilesetObjectEditor1.setObject(objectPickerControl1.SelectedObject);
        }

        byte[] tileBehavior = new byte[4];
        bool DataUpdateFlag = false;

        private void tileBehaviorPicker_TileSelected(int selTileNum, int selTilePal, int selTileWidth, int selTileHeight)
        {
            DataUpdateFlag = true;
			tileBehaviorEditor.setData(t.TileBehaviors[selTileNum]);
            DataUpdateFlag = false;
        }

        private void tileBehaviorEditor_DataChanged(uint val)
        {
            if (DataUpdateFlag) return;

            for(int x = 0; x < tileBehaviorPicker.selTileWidth; x++)
                for(int y = 0; y < tileBehaviorPicker.selTileHeight; y++)
                    t.TileBehaviors[tileBehaviorPicker.selTileNum + x + y*tileBehaviorPicker.bufferWidth] = val;
        }

        private void behaviorZoomUpDown_ValueChanged(object sender, EventArgs e)
        {
            tileBehaviorPicker.SetZoom((int)behaviorZoomUpDown.Value);
        }

        private void imageManager1_SomethingSaved()
        {
            mustRepaintObjects();
        }

        private void tilemapEditor1_CanvasRandomizationVisibilityChanged(bool visible)
        {
            Properties.Settings.Default.ShowMap16RandomizationHighlight = visible;
            Properties.Settings.Default.Save();
        }

        private void tilemapEditor1_PickerRandomizationVisibilityChanged(bool visible)
        {
            Properties.Settings.Default.ShowMap16RandomizationHighlightPicker = visible;
            Properties.Settings.Default.Save();
        }

        private void tilemapEditor1_RandomizationOutlineColorChanged(Color color)
        {
            Properties.Settings.Default.Map16RandomizationOutlineColorArgb = color.ToArgb();
            Properties.Settings.Default.Save();
        }

        private void ApplyMap16RandomizationOverlay()
        {
            int tileCount = t.map16.getMap16TileCount();
            HashSet<int> randomizationTiles = Map16RandomizationTable.GetRandomizedTiles(TilesetID, tileCount);
            HashSet<int> canvasTiles8x8 = ExpandMap16TilesTo8x8Layout(randomizationTiles);
            HashSet<int> pickerTiles = BuildPickerTileHighlights(randomizationTiles);

            tilemapEditor1.SetCanvasRandomizationOverlay(canvasTiles8x8, Properties.Settings.Default.ShowMap16RandomizationHighlight);
            tilemapEditor1.SetPickerRandomizationOverlay(pickerTiles, Properties.Settings.Default.ShowMap16RandomizationHighlightPicker);
        }

        private HashSet<int> ExpandMap16TilesTo8x8Layout(HashSet<int> map16Tiles)
        {
            HashSet<int> output = new HashSet<int>();
            if (map16Tiles == null || map16Tiles.Count == 0)
                return output;

            int width8x8 = t.map16.width;
            int map16Width = width8x8 / 2;
            if (map16Width <= 0)
                return output;

            foreach (int map16Tile in map16Tiles)
            {
                int map16X = map16Tile % map16Width;
                int map16Y = map16Tile / map16Width;
                int baseX = map16X * 2;
                int baseY = map16Y * 2;

                int topLeft = baseY * width8x8 + baseX;
                output.Add(topLeft);
                output.Add(topLeft + 1);
                output.Add(topLeft + width8x8);
                output.Add(topLeft + width8x8 + 1);
            }

            return output;
        }

        private HashSet<int> BuildPickerTileHighlights(HashSet<int> map16Tiles)
        {
            HashSet<int> pickerTiles = new HashSet<int>();
            if (map16Tiles == null || map16Tiles.Count == 0)
                return pickerTiles;

            int width8x8 = t.map16.width;
            int map16Width = width8x8 / 2;
            int height8x8 = t.map16.height;
            if (map16Width <= 0)
                return pickerTiles;

            foreach (int map16Tile in map16Tiles)
            {
                int map16X = map16Tile % map16Width;
                int map16Y = map16Tile / map16Width;
                int baseX = map16X * 2;
                int baseY = map16Y * 2;

                for (int dy = 0; dy < 2; dy++)
                {
                    for (int dx = 0; dx < 2; dx++)
                    {
                        int x = baseX + dx;
                        int y = baseY + dy;
                        if (x < 0 || x >= width8x8 || y < 0 || y >= height8x8)
                            continue;

                        int tileNum = t.map16.tiles[x, y].tileNum;
                        if (tileNum >= 0)
                            pickerTiles.Add(tileNum);
                    }
                }
            }

            return pickerTiles;
        }
    }
}
