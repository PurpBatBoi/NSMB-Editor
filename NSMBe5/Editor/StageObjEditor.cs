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
using System.Linq;

namespace NSMBe5
{
	public partial class StageObjEditor : UserControl
	{
		List<LevelItem> SelectedObjects = new List<LevelItem>();
		private LevelEditorControl EdControl;
		private byte[] stageObjBanks;
		private byte[] stageObjBanksOrder = new byte[16]{ 15, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
		private int[] stageObjectsInOv10 = new int[]
		{
			45,		// Invisible block
			57,		// Coin
			68,		// Vertical lift
			69,		// Horizontal lift
			71,		// Vertical stone block
			72,		// Horizontal stone block
			83,		// Active block
			84,		// Zoom controller
			95,		// Spin block
			98,		// Vertical camera scrolling
			100,	// Vertical camera offset
			148,	// Goomba
			149,	// Koopa Troopa
			150,	// Koopa Paratroopa
			169,	// Spin block (dupe 255)
			171,	// Spin block (dupe 256)
			186,	// Paragoomba
			189,	// Pipe cannon
			195,	// 0 stick to bottom length activator left
			196,	// 0 stick to bottom length activator right
			198,	// In air vertical scroll stop left
			199,	// In air vertical scroll stop right
			280,	// Horizontal camera
			218,	// Autoscroll controller
			222,	// Mini Goomba
			270,	// Broken pipe
			276,	// Scroll and mario stop sideways
			293,	// Touching ground vertical scroll stop left
			294,	// Touching ground vertical scroll stop right
			296,	// Horizontal lift (actor 163)
			297,	// Horizontal stone block (actor 167)
			310,	// Fog FG effect
			311,	// Snow FG effect 1
			313,	// Snow FG effect 2
			314,	// Snow FG effect 3
			315,	// Cloud FG effect
			316,	// Water FG effect 1
			317,	// Water FG effect 2
			318,	// Fire FG effect 1
			319,	// Fire FG effect 2
			320,	// Fire FG effect 3
			321,	// Light FG effect 1
			322,	// Light FG effect 2
			324,	// Grassland clouds FG effect
			325,	// Small grassland clouds FG effect
		};
		private int[] stageObjectsInOv54 = new int[]
		{
			22,		// Map hammer bro
			33,		// Trampoline
			34,		// Red coin ring
			66,		// P switch
			88,		// Brick P switch
			101,	// Event controller view enter
			106,	// Red coin
			107,	// ? switch
			108,	// ! switch
			110,	// Brick ? switch
			117,	// Map flying ? block
			131,	// Vertical checkpoint
			132,	// Checkpoint
			143,	// Spiked ? block (dupe 251)
			144,	// Spiked ? block
			145,	// Spiked ? block (dupe 253)
			152,	// Event trigger block
			155,	// Warp zone
			164,	// Event controller AND
			165,	// Event controller OR
			166,	// Event controller random
			167,	// Event controller chainer
			168,	// Event controller if
			192,	// Falling coin
			197,	// Tile creator/destroyer
			228,	// Roulette block
			231,	// Water
			234,	// Lava
			235,	// Star coin
			255,	// Jungle FG
			259,	// Poisoned water
			264,	// Mega mushroom drop controller
			286,	// Event controller multi-chainer
			290,	// Flying ? block
			291,	// Brick ! switch
			305,	// Final castle create loop
			306,	// Final castle wrong path
		};
		private bool updating = false;

		public int StageObjectCount = ROM.NativeStageObjCount;
		public List<string> StageObjectlist = new List<string>();
		public Dictionary<int, int> ActorToObjectMap = new Dictionary<int, int>();
		public Dictionary<int, int> StageObjToOverlayMap = new Dictionary<int, int>();
        public int[,] StageObjBankToOverlayTable;
        public List<int> CategoryObjectIDs = new List<int>();
		public List<int> FilteredObjectIDs = new List<int>();

		public StageObjEditor(LevelEditorControl EdControl)
		{
			InitializeComponent();
			this.EdControl = EdControl;

			stageObjBanks = ROM.GetInlineFile(ROM.Data.File_Modifiers);
			StageObjBankToOverlayTable = ROM.GetObjectBankOverlays();

			// Overlay 10 actors
			foreach (int objectID in stageObjectsInOv10)
			{
				StageObjToOverlayMap[objectID] = 10;
			}

			// Overlay 54 actors
			foreach (int objectID in stageObjectsInOv54)
			{
				StageObjToOverlayMap[objectID] = 54;
			}

			for (int i = 0; i < ROM.NativeStageObjCount; i++)
			{
				int objectID = ROM.GetObjIDFromTable(i);

				string objName = StageObjSettings.GetObjectName(objectID);
				if (string.IsNullOrEmpty(objName))
					objName = string.Format("Actor {0}", objectID);

				StageObjectlist.Add(i + ": " + objName);
				ActorToObjectMap[objectID] = i;

				int slot = stageObjBanks[i * 2];
				int bank = stageObjBanks[i * 2 + 1];

				if (slot < 0 || (slot > 9 && slot != 15) || bank == 0)
                    continue;

				int overlay = StageObjBankToOverlayTable[slot, bank - 1];
				if (overlay < 0)
                    continue;

				if (!StageObjToOverlayMap.ContainsKey(i))
					StageObjToOverlayMap[i] = overlay;

			}

			StageObjectCount = StageObjectlist.Count;

			FilteredObjectIDs.AddRange(Enumerable.Range(0, StageObjectCount));

			categoryList.Items.Add(LanguageManager.Get("SpriteEditor", "All"));
			categoryList.Items.Add(LanguageManager.Get("SpriteEditor", "InLevel"));
			categoryList.Items.Add(LanguageManager.Get("SpriteEditor", "InSpriteSets"));
			foreach (string cat in StageObjSettings.categoryNames)
				categoryList.Items.Add(cat);
			categoryList.SelectedIndex = 0;

			UpdateDataEditor();
			UpdateInfo();

			LanguageManager.ApplyToContainer(this, "SpriteEditor");
			spriteTypeUpDown.Maximum = StageObjectCount - 1;
        }

		private StageObjSettings.StageObjSettingsEditor sed;

		public void SelectObjects(List<LevelItem> objs)
		{
			SelectedObjects = objs;
			UpdateInfo();
			UpdateDataEditor();
		}

		public void UpdateDataEditor()
		{
			if (sed != null)
			{
				sed.saveData(null, null);
				sed.Parent = null;
			}
			sed = null;
			if (SelectedObjects == null) return;

			int type = getSpriteType();

			if (type != -1)
			{
				int objectID = ROM.GetObjIDFromTable(type);
				StageObjSettings settings = StageObjSettings.GetObject(objectID);
				if (settings != null)
				{
					sed = new StageObjSettings.StageObjSettingsEditor(SelectedObjects, settings, EdControl);
					spriteDataPanel.Controls.Add(sed);
					sed.Dock = DockStyle.Fill;
					//sed.Parent = spriteDataPanel;
					spriteDataPanel.Visible = true;
					return;
				}
			}
			spriteDataPanel.Visible = false;
		}

		public void RefreshDataEditor()
		{
			if (sed != null)
				sed.UpdateData();
		}

		public void UpdateInfo()
		{
			if (SelectedObjects == null || SelectedObjects.Count == 0)
			{
				tableLayoutPanel1.Visible = false;
				spriteDataPanel.Visible = false;
				return;
			}
			tableLayoutPanel1.Visible = true;
			spriteDataPanel.Visible = true;
			updating = true;
			int type = getSpriteType();
			spriteTypeUpDown.Value = type > -1 ? type : 0;
			byte[] SpriteData = (SelectedObjects[0] as NSMBStageObj).Data;
			spriteDataTextBox.Text = string.Format(
				"{0:X2} {1:X2} {2:X2} {3:X2} {4:X2} {5:X2}",
				SpriteData[0], SpriteData[1], SpriteData[2],
				SpriteData[3], SpriteData[4], SpriteData[5]);
			spriteDataTextBox.BackColor = SystemColors.Window;
			spriteListBox.SelectedIndex = FilteredObjectIDs.IndexOf(type);
			updating = false;
		}

		private void spriteTypeUpDown_ValueChanged(object sender, EventArgs e)
		{
			if (updating) return;
			EdControl.UndoManager.Do(new ChangeSpriteTypeAction(SelectedObjects, (int)spriteTypeUpDown.Value), true);
		}

		private void spriteListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (updating) return;
			if (spriteListBox.SelectedIndex > -1)
				spriteTypeUpDown.Value = FilteredObjectIDs[spriteListBox.SelectedIndex];
		}

		private void spriteListBox_DrawItem(object sender, DrawItemEventArgs e)
		{
			e.DrawBackground();
			Color TextColor, BackColor = e.BackColor;
			if (spriteListBox.Items.Count > 0 && e.Index > -1)
			{
				int objID = FilteredObjectIDs[e.Index];

				if (EdControl.Level.ValidSprites[objID]) {
					TextColor = e.ForeColor;
				} else {
					TextColor = Color.DarkRed;
					if ((e.State & DrawItemState.Selected) != DrawItemState.None) {
						TextColor = Color.White;
						BackColor = Color.DarkRed;
						SolidBrush b = new SolidBrush(BackColor);
						e.Graphics.FillRectangle(b, e.Bounds);
						b.Dispose();
					}
				}

				int offsetX = 0;
				bool renderObjBank = true;
				bool renderOverlay = true;
				bool renderActorID = true;

				if (objID < ROM.NativeStageObjCount)
				{
					if (renderActorID)
					{
						string txt = ROM.GetObjIDFromTable(objID).ToString();

						offsetX -= 26;
						TextRenderer.DrawText(e.Graphics, txt, spriteListBox.Font, new Rectangle(e.Bounds.X + e.Bounds.Width + offsetX, e.Bounds.Y, 26, e.Bounds.Height), TextColor, BackColor, TextFormatFlags.Right);

						e.Graphics.DrawLine(
							Pens.LightGray,
							new Point(e.Bounds.X + e.Bounds.Width + offsetX, e.Bounds.Y),
							new Point(e.Bounds.X + e.Bounds.Width + offsetX, e.Bounds.Y + e.Bounds.Height)
						);
					}

					if (renderOverlay)
					{
						string txt = StageObjToOverlayMap.ContainsKey(objID) ? $"ov{StageObjToOverlayMap[objID]}" : "-";

						offsetX -= 40;
						TextRenderer.DrawText(e.Graphics, txt, spriteListBox.Font, new Rectangle(e.Bounds.X + e.Bounds.Width + offsetX, e.Bounds.Y, 40, e.Bounds.Height), TextColor, BackColor, TextFormatFlags.Right);

						e.Graphics.DrawLine(
							Pens.LightGray,
							new Point(e.Bounds.X + e.Bounds.Width + offsetX, e.Bounds.Y),
							new Point(e.Bounds.X + e.Bounds.Width + offsetX, e.Bounds.Y + e.Bounds.Height)
						);
					}

					if (renderObjBank)
					{
						int set = stageObjBanks[objID * 2];
						int subset = stageObjBanks[objID * 2 + 1];
						string txt = (set + 1) + "-" + subset;
						if (subset == 0)
							txt = "-";

						offsetX -= 30;
						TextRenderer.DrawText(e.Graphics, txt, spriteListBox.Font, new Rectangle(e.Bounds.X + e.Bounds.Width + offsetX, e.Bounds.Y, 30, e.Bounds.Height), TextColor, BackColor, TextFormatFlags.Right);

						e.Graphics.DrawLine(
							Pens.LightGray,
							new Point(e.Bounds.X + e.Bounds.Width + offsetX, e.Bounds.Y),
							new Point(e.Bounds.X + e.Bounds.Width + offsetX, e.Bounds.Y + e.Bounds.Height)
						);
					}
				}

				TextRenderer.DrawText(
					e.Graphics,
					(string)spriteListBox.Items[e.Index],
					spriteListBox.Font,
					new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width + offsetX, e.Bounds.Height),
					TextColor,
					BackColor,
					TextFormatFlags.Left
				);

			}
		}

		private void spriteDataTextBox_TextChanged(object sender, EventArgs e)
		{
			if (updating || !spriteDataTextBox.Visible)
				return;

			// validate
			if (System.Text.RegularExpressions.Regex.IsMatch(
				spriteDataTextBox.Text,
				"^[0-9a-f] *[0-9a-f] *[0-9a-f] *[0-9a-f] *[0-9a-f] *[0-9a-f] *[0-9a-f] *[0-9a-f] *[0-9a-f] *[0-9a-f] *[0-9a-f] *[0-9a-f] *$", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
			{
				string parseit = spriteDataTextBox.Text.Replace(" ", "");
				byte[] data = new byte[6];
				for (int hexidx = 0; hexidx < 6; hexidx++)
					data[hexidx] = byte.Parse(parseit.Substring(hexidx*2, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
				if (!updating)
					EdControl.UndoManager.Do(new ChangeStageObjSettingsAction(SelectedObjects, data));
				spriteDataTextBox.BackColor = SystemColors.Window;
			}
			else
				spriteDataTextBox.BackColor = Color.Coral;
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			FilteredObjectIDs.Clear();
			for (int l = 0; l < CategoryObjectIDs.Count; l++) {

				int objectID = CategoryObjectIDs[l];

				if (objectID >= StageObjectlist.Count)
					continue;

				if (StageObjectlist[objectID].ToLowerInvariant().Contains(searchBox.Text.ToLowerInvariant()))
					FilteredObjectIDs.Add(CategoryObjectIDs[l]);

			}
			spriteListBox.Items.Clear();
			List<string> items = new List<string>();
			for (int l = 0; l < FilteredObjectIDs.Count; l++)
				items.Add(StageObjectlist[FilteredObjectIDs[l]]);
			spriteListBox.Items.AddRange(items.ToArray());
			spriteListBox.SelectedIndex = FilteredObjectIDs.IndexOf(getSpriteType());
			if (FilteredObjectIDs.Count > 0)
				searchBox.BackColor = SystemColors.Window;
			else
				searchBox.BackColor = Color.Coral;
		}

		private int getSpriteType()
		{
			int type = -1;
			foreach (LevelItem obj in SelectedObjects)
			{
				if (obj is NSMBStageObj)
				{
					NSMBStageObj s = obj as NSMBStageObj;
					if (type == -1) type = s.Type;
					if (type != s.Type) return -1;
				}
			}
			return type;
		}

		private void clearSearch_Click(object sender, EventArgs e)
		{
			searchBox.Text = "";
		}

		private void clearSpriteData_Click(object sender, EventArgs e)
		{
			byte[] emptyData = new byte[6];
			EdControl.UndoManager.Do(new ChangeStageObjSettingsAction(SelectedObjects, emptyData));
		}

		public int getSelectedType()
		{
			if (spriteListBox.SelectedIndex == -1)
			{
				// Fix for this http://nsmbhd.net/post/39505/
				if (SelectedObjects.Count > 0 && SelectedObjects[0] is NSMBStageObj)
				{
					return (SelectedObjects[0] as NSMBStageObj).Type;
				}
				return -1;
			}
			return FilteredObjectIDs[spriteListBox.SelectedIndex];
		}

		private void categoryList_SelectedIndexChanged(object sender, EventArgs e)
		{
			CategoryObjectIDs.Clear();
			switch (categoryList.SelectedIndex)
			{
			case 0:
				for (int i = 0; i < StageObjectCount; i++)
					CategoryObjectIDs.Add(i);
				break;
			case 1:
				foreach (NSMBStageObj s in EdControl.Level.Sprites)
				{
					if (!CategoryObjectIDs.Contains(s.Type))
						CategoryObjectIDs.Add(s.Type);
				}
				CategoryObjectIDs.Sort();
				break;
			case 2:
				for (int i = 0; i < StageObjectCount; i++)
				{
					if (EdControl.Level.ValidSprites[i])
						CategoryObjectIDs.Add(i);
				}
				break;
			default:
				foreach (int stageObjID in StageObjSettings.categoryObjs[StageObjSettings.categoryIDs[categoryList.SelectedIndex - 3]])
				{
					if (ActorToObjectMap.ContainsKey(stageObjID))
						CategoryObjectIDs.Add(ActorToObjectMap[stageObjID]);
				}
				break;
			}
			textBox1_TextChanged(sender, e);
		}
	}
}
