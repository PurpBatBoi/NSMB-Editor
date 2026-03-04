// Decompiled with JetBrains decompiler
// Type: NSMBe4.ExtDataSearch
// Assembly: NSMBe5, Version=5.1.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 375C0264-8422-4B10-96C9-D574BA1AC306
// Assembly location: C:\Users\tiago\Desktop\nsmbeBent\NSMBe5.exe

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NSMBe5
{
  public class ExtraDataSearch : Form
  {
    public int selItem = -1;
    private Dictionary<string, int> dict;
    private IContainer components = new Container();
    private ListBox list;
    private TextBox searchBox;

    public ExtraDataSearch(Dictionary<string, int> dict, string caption = "")
    {
      this.dict = dict;
      this.InitializeComponent();
      if (caption != "")
        this.Text = this.Text + ": " + caption;
      this.CenterToParent();
      this.search("");
    }

    private void searchBox_TextChanged(object sender, EventArgs e)
    {
      this.search(this.searchBox.Text);
    }

    private void search(string s)
    {
      this.list.Items.Clear();
      this.list.SelectedIndex = -1;
      string[] array = new string[this.dict.Count];
      this.dict.Keys.CopyTo(array, 0);
      for (int index = 0; index < array.Length; ++index)
      {
        if (array[index].ToLower().Contains(s.ToLower()))
          this.list.Items.Add((object) array[index]);
      }
    }

    private void list_DoubleClick(object sender, MouseEventArgs e)
    {
      int index = this.list.IndexFromPoint(e.Location);
      if (index == -1)
        return;
      this.selItem = this.dict[this.list.Items[index].ToString()];
      this.DialogResult = DialogResult.OK;
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing && this.components != null)
        this.components.Dispose();
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      this.list = new ListBox();
      this.searchBox = new TextBox();
      this.SuspendLayout();
      this.list.FormattingEnabled = true;
      this.list.Location = new Point(12, 12);
      this.list.Name = "list";
      this.list.Size = new Size(260, 303);
      this.list.TabIndex = 1;
      this.list.MouseDoubleClick += new MouseEventHandler(this.list_DoubleClick);
      this.searchBox.Location = new Point(12, 325);
      this.searchBox.Name = "searchBox";
      this.searchBox.Size = new Size(260, 20);
      this.searchBox.TabIndex = 2;
      this.searchBox.TextChanged += new EventHandler(this.searchBox_TextChanged);
      this.AutoScaleDimensions = new SizeF(6f, 13f);
      this.AutoScaleMode = AutoScaleMode.Font;
      this.ClientSize = new Size(284, 357);
      this.Controls.Add((Control) this.searchBox);
      this.Controls.Add((Control) this.list);
      this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
      this.Name = "ExtDataSearch";
      this.Text = "Extra Data - Search";
      this.ResumeLayout(false);
      this.PerformLayout();
    }
  }
}
