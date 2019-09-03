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
    public partial class FormSuccess : Form
    {
        private int count = 0;

        public FormSuccess()
        {
            InitializeComponent();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            count++;

            if(count>8)
            {
                this.Close();
            }
        }
    }
}
