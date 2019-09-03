using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleScreenshot
{
    public class HotKey
    {
        public bool Ctrl { get; set; } = false;
        public bool Shift { get; set; } = false;
        public bool Alt { get; set; } = false;

        public Keys KeyCode { get; set; }
        public int KeyValue { get; set; }

        public override string ToString()
        {
            string s = "";

            if (Ctrl) s += "Ctrl + ";
            if (Shift) s += "Shift + ";
            if (Alt) s += "Alt + ";

            s += KeyCode.ToString();

            return s;
        }
    }
}
