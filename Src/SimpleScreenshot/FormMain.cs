using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleScreenshot
{
    public partial class FormMain : Form
    {
        private short HotKetID;
        private Bitmap ScreenSrcImage = null;
        private float Scale_X = 1, Scale_Y = 1;


        private ScreenshotStatus ScreenshotStatus = ScreenshotStatus.None;
        private OperatorStatus OperatorStatus = OperatorStatus.None;

        private Point CurrMouseLocation = new Point();      //当前鼠标位置
        private Point StartMouseLocation = new Point();     //开始选择鼠标位置
        private Point StopMouseLocation = new Point();      //结束选择鼠标位置

        public FormMain()
        {
            InitializeComponent();
        }

        #region BaseLogic       

        private void FormMain_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;

            HotKetID = HotKeyHelpper.GlobalAddAtom("SimpleScreenshot-HotKey");
            var result = HotKeyHelpper.RegisterHotKey(this.Handle, HotKetID, HotKeyHelpper.KeyModifiers.Alt, (int)Keys.S);

            if (result == false)
            {
                MessageBox.Show("热键冲突");
            }

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true); // 禁止擦除背景.
            SetStyle(ControlStyles.DoubleBuffer, true);         // 双缓冲
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            HotKeyHelpper.UnregisterHotKey(this.Handle, HotKetID);
            HotKeyHelpper.GlobalDeleteAtom(HotKetID);
        }

        protected override void WndProc(ref Message m)// 监视Windows消息
        {
            switch (m.Msg)
            {
                case HotKeyHelpper.WM_HOTKEY:
                    ProcessHotkey(m);
                    break;
            }

            base.WndProc(ref m);
        }

        private void ProcessHotkey(Message m)
        {
            var keyid = m.WParam.ToInt32();
            if (keyid == HotKetID)
            {
                Debug.WriteLine($"DESKTOP:{PrimaryScreen.DESKTOP.Width},{PrimaryScreen.DESKTOP.Height}");
                Debug.WriteLine($"DPI:{PrimaryScreen.DpiX},{PrimaryScreen.DpiY}");
                Debug.WriteLine($"Scale:{PrimaryScreen.ScaleX},{PrimaryScreen.ScaleY}");

                Scale_X = PrimaryScreen.ScaleX;
                Scale_Y = PrimaryScreen.ScaleY;

                var size = PrimaryScreen.DESKTOP;
                var width = size.Width;
                var height = size.Height;

                Bitmap imageSRC = new Bitmap(width, height);
                using (Graphics graphics = Graphics.FromImage(imageSRC))
                {
                    graphics.CopyFromScreen(0, 0, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

                    ScreenSrcImage?.Dispose();
                    ScreenSrcImage = imageSRC;
                }

                this.WindowState = FormWindowState.Maximized;   // trigger invalidate(full screen) 
                this.ScreenshotStatus = ScreenshotStatus.Screenshoting;               
            }
        }

        private void FormMain_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.WindowState = FormWindowState.Minimized;
                this.ScreenshotStatus = ScreenshotStatus.None;
            }
        }

        #endregion

        #region Paint
       

        private void FormMain_MouseMove(object sender, MouseEventArgs e)
        {
            if (ScreenshotStatus != ScreenshotStatus.Screenshoting)
                return;

            Debug.WriteLine($"1 Mouse move:{e.Button},{e.Location.X},{e.Location.Y}");

            if (OperatorStatus == OperatorStatus.FinishedSelect)
            {

            }
            else
            {               
                CurrMouseLocation = e.Location;

                if (OperatorStatus == OperatorStatus.StartSelect)
                {
                    StopMouseLocation = e.Location;
                }

                this.Invalidate();
            }
           
        }

        private void FormMain_MouseDown(object sender, MouseEventArgs e)
        {
            if (ScreenshotStatus != ScreenshotStatus.Screenshoting)
                return;

            if (OperatorStatus == OperatorStatus.None)
            {
                if (e.Button == MouseButtons.Left)
                {
                    OperatorStatus = OperatorStatus.StartSelect;
                    StartMouseLocation = e.Location;
                    StopMouseLocation = e.Location;
                }
            }
        }

        private void FormMain_MouseUp(object sender, MouseEventArgs e)
        {
            if (ScreenshotStatus != ScreenshotStatus.Screenshoting)
                return;

            if (OperatorStatus == OperatorStatus.StartSelect)
            {
                if (e.Button == MouseButtons.Left)
                {
                    OperatorStatus = OperatorStatus.FinishedSelect;
                    this.Invalidate();
                }
            }
        }

        private void FormMain_MouseClick(object sender, MouseEventArgs e)
        {

        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Debug.WriteLine($"2 OnPaintBackground");

            if (ScreenshotStatus != ScreenshotStatus.Screenshoting)
                return;

            if (this.ScreenSrcImage == null)
                return;

            Graphics g = e.Graphics;
            g.CompositingQuality = CompositingQuality.HighQuality;

            if (OperatorStatus != OperatorStatus.None)
            {
                float opacity = 0.3f;

                float[][] nArray ={
                    new float[] {1, 0, 0, 0, 0},
                    new float[] {0, 1, 0, 0, 0},
                    new float[] {0, 0, 1, 0, 0},
                    new float[] {0, 0, 0, opacity, 0},
                    new float[] {0, 0, 0, 0, 1}};

                ColorMatrix matrix = new ColorMatrix(nArray);
                ImageAttributes imageAttributes = new ImageAttributes();
                imageAttributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                Rectangle FullRect = new Rectangle(0, 0, this.Width, this.Height);
                g.DrawImage(this.ScreenSrcImage, FullRect, 0, 0, this.Width, this.Height, GraphicsUnit.Pixel, imageAttributes);
            }
            else
            {
                g.DrawImage(this.ScreenSrcImage, 0, 0, this.Width, this.Height);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {           
            Debug.WriteLine($"3 OnPaint:CurrMouseLocation:{CurrMouseLocation}");

            if (ScreenshotStatus != ScreenshotStatus.Screenshoting)
                return;

            if (this.ScreenSrcImage == null)
                return;

            Graphics g = e.Graphics;
            g.CompositingQuality = CompositingQuality.HighQuality;

            //绘制选择区域
            if (OperatorStatus != OperatorStatus.None)
            { 
                int left = StartMouseLocation.X <= StopMouseLocation.X ? StartMouseLocation.X : StopMouseLocation.X;
                int right = StartMouseLocation.X > StopMouseLocation.X ? StartMouseLocation.X : StopMouseLocation.X;
                int top = StartMouseLocation.Y <= StopMouseLocation.Y ? StartMouseLocation.Y : StopMouseLocation.Y;
                int bottom = StartMouseLocation.Y > StopMouseLocation.Y ? StartMouseLocation.Y : StopMouseLocation.Y;

                Rectangle SelectRect = new Rectangle(left, top, right - left, bottom - top);

                g.DrawImage(this.ScreenSrcImage, SelectRect, SelectRect,GraphicsUnit.Pixel);

                g.DrawRectangle(new Pen(Brushes.Green, 1), SelectRect);

                g.FillRectangle(Brushes.Green, left - 2, top - 2, 5, 5);
                g.FillRectangle(Brushes.Green, right - 2, top - 2, 5, 5);
                g.FillRectangle(Brushes.Green, left - 2, bottom - 2, 5, 5);
                g.FillRectangle(Brushes.Green, right - 2, bottom - 2, 5, 5);               
            }

            if (OperatorStatus != OperatorStatus.FinishedSelect)            
            {
                //计算Zoom区域位置
                int ShiftWidth = 20;
                int ZoomRectWeight = 140;
                int PosRectHeight = 50;

                Rectangle ZoomRect = new Rectangle();
                Rectangle PosRect = new Rectangle();
                if (CurrMouseLocation.X < (this.Width - ZoomRectWeight - ShiftWidth))
                {
                    if (CurrMouseLocation.Y < (this.Height - ZoomRectWeight - PosRectHeight - ShiftWidth))
                    {
                        ZoomRect = new Rectangle(CurrMouseLocation.X + ShiftWidth, CurrMouseLocation.Y + ShiftWidth, ZoomRectWeight, ZoomRectWeight);
                        PosRect = new Rectangle(CurrMouseLocation.X + ShiftWidth, CurrMouseLocation.Y + ShiftWidth + ZoomRectWeight + 1, ZoomRectWeight + 1, PosRectHeight);
                    }
                    else
                    {
                        ZoomRect = new Rectangle(CurrMouseLocation.X + ShiftWidth, CurrMouseLocation.Y - ShiftWidth - ZoomRectWeight - PosRectHeight, ZoomRectWeight, ZoomRectWeight);
                        PosRect = new Rectangle(CurrMouseLocation.X + ShiftWidth, CurrMouseLocation.Y - ShiftWidth - PosRectHeight + 1, ZoomRectWeight + 1, PosRectHeight);
                    }
                }
                else
                {
                    if (CurrMouseLocation.Y < (this.Height - ZoomRectWeight - PosRectHeight - ShiftWidth))
                    {
                        ZoomRect = new Rectangle(CurrMouseLocation.X - ShiftWidth - ZoomRectWeight, CurrMouseLocation.Y + ShiftWidth, ZoomRectWeight, ZoomRectWeight);
                        PosRect = new Rectangle(CurrMouseLocation.X - ShiftWidth - ZoomRectWeight, CurrMouseLocation.Y + ShiftWidth + ZoomRectWeight + 1, ZoomRectWeight + 1, PosRectHeight);
                    }
                    else
                    {
                        ZoomRect = new Rectangle(CurrMouseLocation.X - ShiftWidth - ZoomRectWeight, CurrMouseLocation.Y - ShiftWidth - ZoomRectWeight - PosRectHeight, ZoomRectWeight, ZoomRectWeight);
                        PosRect = new Rectangle(CurrMouseLocation.X - ShiftWidth - ZoomRectWeight, CurrMouseLocation.Y - ShiftWidth - PosRectHeight + 1, ZoomRectWeight + 1, PosRectHeight);
                    }
                }

                //绘制放大图片          
                int ZoomImageWidth = ZoomRectWeight / 2;
                Rectangle ZoomImageRect = new Rectangle(CurrMouseLocation.X - ZoomImageWidth / 2, CurrMouseLocation.Y - ZoomImageWidth / 2, ZoomImageWidth, ZoomImageWidth);
                g.FillRectangle(Brushes.DarkGray, ZoomRect);
                g.DrawImage(ScreenSrcImage, ZoomRect, ZoomImageRect, GraphicsUnit.Pixel);

                g.DrawRectangle(new Pen(Brushes.Green, 1), ZoomRect);
                g.DrawLine(new Pen(Brushes.Green, 2), new Point(ZoomRect.Left, ZoomRect.Top + ZoomRect.Height / 2), new Point(ZoomRect.Right, ZoomRect.Top + ZoomRect.Height / 2));
                g.DrawLine(new Pen(Brushes.Green, 2), new Point(ZoomRect.Left + ZoomRect.Width / 2, ZoomRect.Top), new Point(ZoomRect.Left + ZoomRect.Width / 2, ZoomRect.Bottom));

                //绘制Point位置信息
                g.FillRectangle(Brushes.Black, PosRect);
                Font font = new Font("宋体", 10, FontStyle.Bold);
                g.DrawString($"POS:({CurrMouseLocation.X},{CurrMouseLocation.Y})", font, Brushes.White, PosRect.Left + 10, PosRect.Top + 10);

                Color pointColor = ScreenSrcImage.GetPixel(CurrMouseLocation.X, CurrMouseLocation.Y);
                g.DrawString($"RGB:({pointColor.R},{pointColor.G},{pointColor.B})", font, Brushes.White, PosRect.Left + 10, PosRect.Top + 30);
            }           
        }

        #endregion

        #region Menu Control

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void seteupToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void screenShotAltSToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

       

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        #endregion

    }

    public enum ScreenshotStatus
    {
        None,
        Screenshoting
    }

    public enum OperatorStatus
    {
        None,
        StartSelect,
        FinishedSelect
    }
}
