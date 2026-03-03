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
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace NSMBe5.TilemapEditor
{
    public partial class TilemapEditorWindow : Form
    {
        Tilemap t;

        public TilemapEditorWindow(Tilemap t)
            : this(t, null, null, null)
        {
        }

        public TilemapEditorWindow(Tilemap t, string graphicsFileName, string paletteFileName, string layoutFileName)
        {
            InitializeComponent();
            LanguageManager.ApplyToContainer(this, "TilemapEditor");
            this.t = t;
            t.beginEdit();
            tilemapEditor1.showSaveButton();
            tilemapEditor1.load(t);
            SetFilesStatus(graphicsFileName, paletteFileName, layoutFileName);
        }

        private void SetFilesStatus(string graphicsFileName, string paletteFileName, string layoutFileName)
        {
            if (string.IsNullOrEmpty(graphicsFileName) &&
                string.IsNullOrEmpty(paletteFileName) &&
                string.IsNullOrEmpty(layoutFileName))
            {
                statusStrip1.Visible = false;
                return;
            }

            statusStrip1.Visible = true;
            filesStatusLabel.Text = string.Format(
                "{0} {1} | {2} {3} | {4} {5}",
                LanguageManager.Get("TilemapEditor", "graphicsFileLabel"), graphicsFileName ?? "-",
                LanguageManager.Get("TilemapEditor", "paletteFileLabel"), paletteFileName ?? "-",
                LanguageManager.Get("TilemapEditor", "layoutFileLabel"), layoutFileName ?? "-");
        }

        private void TilemapEditorWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            t.endEdit();
        }
    }
}
