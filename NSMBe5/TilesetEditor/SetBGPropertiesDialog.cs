using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NSMBe5
{
    public partial class SetBGPropertiesDialog : Form
    {
        public bool Canceled = true;
        public string bgName;
        public int bgNCGID;
        public int bgNCLID;
        public int bgNSCID;
        public int bgBMPOffs;
        public int bgPALOffs;

        public SetBGPropertiesDialog(int ID, string Name, int NCGID, int NCLID, int NSCID, int BMPOffs, int PALOffs, bool IsLevelBG)
        {
            InitializeComponent();

            bmpoffs_UpDown.Enabled = false;
            paloffs_UpDown.Enabled = false;

            name_textBox.Text = Name;
            ncgid_UpDown.Value = NCGID;
            nclid_UpDown.Value = NCLID;
            nscid_UpDown.Value = NSCID;
            bmpoffs_UpDown.Value = BMPOffs;
            paloffs_UpDown.Value = PALOffs;

            ShowDialog();
        }

        private void Cancel_btn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Apply_btn_Click(object sender, EventArgs e)
        {
            int ncgId = (int)ncgid_UpDown.Value;
            int nclId = (int)nclid_UpDown.Value;
            int nscId = (int)nscid_UpDown.Value;
            int firstUsableFileId = ROM.GetOffset(ROM.Data.Number_FileOffset);
            bool hasZeroId = ncgId == 0 || nclId == 0 || nscId == 0;
            bool likelySystemFile =
                ncgId < firstUsableFileId ||
                nclId < firstUsableFileId ||
                nscId < firstUsableFileId;
            bool hasMissingFile = ROM.FS.getFileById(ncgId) == null || ROM.FS.getFileById(nclId) == null || ROM.FS.getFileById(nscId) == null;

            if (hasMissingFile || likelySystemFile || hasZeroId)
            {
                DialogResult warnResult = MessageBox.Show(
                    "One or more selected file IDs are missing or point to system/internal files. This may cause a black screen in-game. Continue anyway?",
                    "Background ID Warning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (warnResult != DialogResult.Yes)
                    return;
            }

            Canceled = false;

            bgName = name_textBox.Text;
            bgNCGID = ncgId;
            bgNCLID = nclId;
            bgNSCID = nscId;
            bgBMPOffs = (int)bmpoffs_UpDown.Value;
            bgPALOffs = (int)paloffs_UpDown.Value;

            Close();
        }
    }
}
