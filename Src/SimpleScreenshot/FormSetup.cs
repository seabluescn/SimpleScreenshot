using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleScreenshot
{
    public partial class FormSetup : Form
    {
        private HotKey Hotkey;

        public FormSetup()
        {
            InitializeComponent();
        }       

        private void FormSetup_Load(object sender, EventArgs e)
        {
            Hotkey = new HotKey();
            Hotkey.Ctrl = Properties.Settings.Default.HotKey_Ctrl;
            Hotkey.Shift = Properties.Settings.Default.HotKey_Shift;
            Hotkey.Alt = Properties.Settings.Default.HotKey_Alt;
            Hotkey.KeyCode = Properties.Settings.Default.HotKey_KeyCode;
            Hotkey.KeyValue = Properties.Settings.Default.HotKey_KeyValue;

            this.btnSetupHotKey.Text = Hotkey.ToString();
        }

        private void btnSetupHotKey_Click(object sender, EventArgs e)
        {
            FormSetupHotKey frm = new FormSetupHotKey
            {
                Hotkey = this.Hotkey
            };

            if (frm.ShowDialog() == DialogResult.OK)
            {
                HotKey Hotkey = frm.Hotkey;

                this.btnSetupHotKey.Text = Hotkey.ToString();

                Properties.Settings.Default.HotKey_Ctrl = Hotkey.Ctrl;
                Properties.Settings.Default.HotKey_Shift = Hotkey.Shift;
                Properties.Settings.Default.HotKey_Alt = Hotkey.Alt;
                Properties.Settings.Default.HotKey_KeyCode = Hotkey.KeyCode;
                Properties.Settings.Default.HotKey_KeyValue = Hotkey.KeyValue;

                Properties.Settings.Default.Save();

                this.lblReset.Visible = true;
            }
        }
    }
}
