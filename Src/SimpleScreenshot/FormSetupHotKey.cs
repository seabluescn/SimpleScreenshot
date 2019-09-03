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
    public partial class FormSetupHotKey : Form
    {
        public FormSetupHotKey()
        {
            InitializeComponent();
        }

        public HotKey Hotkey { get; set; } 


        private void FormSetupHotKey_Load(object sender, EventArgs e)
        {
            this.lblShow.Text = Hotkey.ToString();
        }

        private void FormSetupHotKey_KeyDown(object sender, KeyEventArgs e)
        {
            Hotkey.Ctrl = e.Control;
            Hotkey.Shift = e.Shift;
            Hotkey.Alt = e.Alt;
            Hotkey.KeyCode = e.KeyCode;
            Hotkey.KeyValue = e.KeyValue;

            this.lblShow.Text = Hotkey.ToString();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }        
    }
}
