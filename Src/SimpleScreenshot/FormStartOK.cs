using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleScreenshot
{
    public partial class FormStartOK : Form
    {
        private int count = 0;

        public FormStartOK()
        {
            InitializeComponent();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            count++;

            if (count > 12)
            {
                this.Close();
            }
        }

        private void FormStartOK_Load(object sender, EventArgs e)
        {
            HotKey Hotkey = new HotKey();
            Hotkey.Ctrl = Properties.Settings.Default.HotKey_Ctrl;
            Hotkey.Shift = Properties.Settings.Default.HotKey_Shift;
            Hotkey.Alt = Properties.Settings.Default.HotKey_Alt;
            Hotkey.KeyCode = Properties.Settings.Default.HotKey_KeyCode;
            Hotkey.KeyValue = Properties.Settings.Default.HotKey_KeyValue;

            this.labelInfo.Text = "快捷键: " + Hotkey.ToString();
        }
    }
}
