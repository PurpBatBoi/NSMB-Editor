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
using NSMBe5.DSFileSystem;

namespace NSMBe5.TilemapEditor
{
    public partial class TilemapEditorWindow : Form
    {
        Tilemap t;

        public TilemapEditorWindow(Tilemap t)
            : this(t, null, null, null)
        {
        }

        public TilemapEditorWindow(Tilemap t, File graphicsFile, File paletteFile, File layoutFile)
        {
            InitializeComponent();
            LanguageManager.ApplyToContainer(this, "TilemapEditor");
            this.t = t;
            t.beginEdit();
            tilemapEditor1.ShowSaveButton();
            tilemapEditor1.LoadTilemap(t);
            tilemapEditor1.SetBackgroundFiles(graphicsFile, paletteFile, layoutFile);
        }

        private void TilemapEditorWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            t.endEdit();
        }
    }
}
