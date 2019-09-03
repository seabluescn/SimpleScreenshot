using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleScreenshot
{
    internal class Toolbar
    {
        public Bitmap Icon { get; set; }
        public Bitmap Icon_A { get; set; }
        public Rectangle Rectangle = new Rectangle();

        public Toolbar(Bitmap icon , Bitmap icona)
        {
            Icon = icon;
            Icon_A = icona;
        }
    }
}
