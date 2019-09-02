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
        Rectangle SelectRect = new Rectangle();

        private Rectangle RectLeftTop = new Rectangle();
        private Rectangle RectLeftBottom = new Rectangle();
        private Rectangle RectRightTop = new Rectangle();
        private Rectangle RectRightBottom = new Rectangle();
        private Rectangle RectLeftMiddle = new Rectangle();
        private Rectangle RectTopMiddle = new Rectangle();
        private Rectangle RectRightMiddle = new Rectangle();
        private Rectangle RectBottomMiddle = new Rectangle();

        MoveSelectRectState MoveSelectRectState = MoveSelectRectState.None;
        private Point MoveStartLocation = new Point();     //移动选取鼠标开始位置
        private Point MoveStopLocation = new Point();      //移动选取鼠标结束位置
        

        #region BaseLogic       

        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        { 
            HotKetID = HotKeyHelpper.GlobalAddAtom("SimpleScreenshot-HotKey");
            var result = HotKeyHelpper.RegisterHotKey(this.Handle, HotKetID, HotKeyHelpper.KeyModifiers.Alt, (int)Keys.S);

            if (result == false)
            {
                MessageBox.Show("热键冲突");
                this.Close();
                return;
            }

            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true); // 禁止擦除背景.
            SetStyle(ControlStyles.DoubleBuffer, true);         // 双缓冲

            this.Visible = false;
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

                this.ScreenshotStatus = ScreenshotStatus.Screenshoting;
                this.Visible = true;
            }
        }

        private void FormMain_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {                
                this.Visible = false; 
              
                this.ScreenshotStatus = ScreenshotStatus.None;
                this.OperatorStatus = OperatorStatus.None;
                this.Cursor = Cursors.Default;
            }
        }

        #endregion

        #region Paint             

        private void FormMain_MouseMove(object sender, MouseEventArgs e)
        {
            if (ScreenshotStatus != ScreenshotStatus.Screenshoting)
                return;

            Debug.WriteLine($"1 Mouse move:{e.Button},{e.Location.X},{e.Location.Y}");

            CurrMouseLocation = e.Location;

            if (OperatorStatus == OperatorStatus.None)
            {                
                this.Invalidate();
                return;
            }

            if (OperatorStatus == OperatorStatus.StartSelect)
            {
                StopMouseLocation = e.Location;

                int left = StartMouseLocation.X <= StopMouseLocation.X ? StartMouseLocation.X : StopMouseLocation.X;
                int right = StartMouseLocation.X > StopMouseLocation.X ? StartMouseLocation.X : StopMouseLocation.X;
                int top = StartMouseLocation.Y <= StopMouseLocation.Y ? StartMouseLocation.Y : StopMouseLocation.Y;
                int bottom = StartMouseLocation.Y > StopMouseLocation.Y ? StartMouseLocation.Y : StopMouseLocation.Y;
                SelectRect = new Rectangle(left, top, right - left, bottom - top);
                CalacSelectedLocationRect();

                this.Invalidate();
                return;
            }

            //进入变换选区状态
            if (OperatorStatus == OperatorStatus.FinishedSelect)
            {
                if(e.Button== MouseButtons.Left)
                {
                    MoveStopLocation = e.Location;
                    int xOffset = MoveStopLocation.X - MoveStartLocation.X;
                    int yOffset = MoveStopLocation.Y - MoveStartLocation.Y;

                    switch (MoveSelectRectState)
                    {
                        case MoveSelectRectState.None:
                            break;

                        case MoveSelectRectState.MoveAll:
                            SelectRect.Offset(new Point(MoveStopLocation.X - MoveStartLocation.X, MoveStopLocation.Y - MoveStartLocation.Y));
                            if (SelectRect.X < 0) SelectRect.X = 0;
                            if (SelectRect.Right > this.Width) SelectRect.X = this.Width - SelectRect.Width;
                            if (SelectRect.Y < 0) SelectRect.Y = 0;
                            if (SelectRect.Bottom > this.Height) SelectRect.Y = this.Height - SelectRect.Height;
                            break;

                        case MoveSelectRectState.MoveLeftMiddle:
                            if (SelectRect.Width - xOffset > 0)
                            {
                                SelectRect.X += xOffset;
                                SelectRect.Width -= xOffset;
                            }
                            break;

                        case MoveSelectRectState.MoveRightMiddle:
                            if (SelectRect.Width + xOffset > 0)
                            {
                                SelectRect.Width += xOffset;
                            }
                            break;

                        case MoveSelectRectState.MoveTopMiddle:
                            if(SelectRect.Height- yOffset > 0)
                            {
                                SelectRect.Y += yOffset;
                                SelectRect.Height -= yOffset;
                            }
                            break;

                        case MoveSelectRectState.MoveBottomMiddle:
                            if(SelectRect.Height+yOffset>0)
                            {
                                SelectRect.Height += yOffset;
                            }
                            break;

                        case MoveSelectRectState.MoveLeftTop:
                            if (SelectRect.Width - xOffset > 0)
                            {
                                SelectRect.X += xOffset;
                                SelectRect.Width -= xOffset;
                            }
                            if (SelectRect.Height - yOffset > 0)
                            {
                                SelectRect.Y += yOffset;
                                SelectRect.Height -= yOffset;
                            }
                            break;

                        case MoveSelectRectState.MoveRightTop:
                            if (SelectRect.Width + xOffset > 0)
                            {
                                SelectRect.Width += xOffset;
                            }
                            if (SelectRect.Height - yOffset > 0)
                            {
                                SelectRect.Y += yOffset;
                                SelectRect.Height -= yOffset;
                            }
                            break;

                        case MoveSelectRectState.MoveLeftBottom:
                            if (SelectRect.Width - xOffset > 0)
                            {
                                SelectRect.X += xOffset;
                                SelectRect.Width -= xOffset;
                            }
                            if (SelectRect.Height + yOffset > 0)
                            {
                                SelectRect.Height += yOffset;
                            }
                            break;

                        case MoveSelectRectState.MoveRightBottom:
                            if (SelectRect.Width + xOffset > 0)
                            {
                                SelectRect.Width += xOffset;
                            }
                            if (SelectRect.Height + yOffset > 0)
                            {
                                SelectRect.Height += yOffset;
                            }
                            break;
                    }

                    CalacSelectedLocationRect();

                    MoveStartLocation = e.Location;
                    this.Invalidate();
                }
                else
                {
                    if (RectLeftTop.Contains(e.Location) || RectRightBottom.Contains(e.Location) )
                    {
                        this.Cursor = Cursors.SizeNWSE;
                        return;
                    }
                    
                    if (RectRightTop.Contains(e.Location) || RectLeftBottom.Contains(e.Location) )
                    {
                        this.Cursor = Cursors.SizeNESW;
                        return;
                    }

                    if (RectTopMiddle.Contains(e.Location) || RectBottomMiddle.Contains(e.Location))
                    {
                        this.Cursor = Cursors.SizeNS;
                        return;
                    }

                    if (RectLeftMiddle.Contains(e.Location) || RectRightMiddle.Contains(e.Location))
                    {
                        this.Cursor = Cursors.SizeWE;
                        return;
                    }

                    if (SelectRect.Contains(e.Location))
                    {
                        this.Cursor = Cursors.SizeAll;
                        return;
                    }

                    this.Cursor = Cursors.Default;
                }                
            }             
        }

        private void CalacSelectedLocationRect()
        {  
            RectLeftTop = new Rectangle(SelectRect.Left - 2, SelectRect.Top - 2, 5, 5);
            RectLeftBottom = new Rectangle(SelectRect.Left - 2, SelectRect.Bottom - 2, 5, 5);
            RectRightTop = new Rectangle(SelectRect.Right - 2, SelectRect.Top - 2, 5, 5);
            RectRightBottom = new Rectangle(SelectRect.Right - 2, SelectRect.Bottom - 2, 5, 5);
            RectLeftMiddle = new Rectangle(SelectRect.Left - 2, SelectRect.Height / 2 + SelectRect.Top - 2, 5, 5);
            RectTopMiddle = new Rectangle(SelectRect.Width / 2 + SelectRect.Left - 2, SelectRect.Top - 2, 5, 5);
            RectRightMiddle = new Rectangle(SelectRect.Right - 2, SelectRect.Height / 2 + SelectRect.Top - 2, 5, 5);
            RectBottomMiddle = new Rectangle(SelectRect.Width / 2 + SelectRect.Left - 2, SelectRect.Bottom - 2, 5, 5);
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

            //变换选区
            if(OperatorStatus == OperatorStatus.FinishedSelect)
            {                

                if (SelectRect.Contains(e.Location))
                {
                    MoveSelectRectState = MoveSelectRectState.MoveAll;
                }

                if (RectLeftTop.Contains(e.Location) )
                {
                    MoveSelectRectState = MoveSelectRectState.MoveLeftTop;
                }

                if (RectLeftBottom.Contains(e.Location))
                {
                    MoveSelectRectState = MoveSelectRectState.MoveLeftBottom;
                }

                if (RectRightTop.Contains(e.Location))
                {
                    MoveSelectRectState = MoveSelectRectState.MoveRightTop;
                }

                if ( RectRightBottom.Contains(e.Location))
                {
                    MoveSelectRectState = MoveSelectRectState.MoveRightBottom;
                }                    

                if (RectTopMiddle.Contains(e.Location) )
                {
                    MoveSelectRectState = MoveSelectRectState.MoveTopMiddle;
                }

                if ( RectBottomMiddle.Contains(e.Location))
                {
                    MoveSelectRectState = MoveSelectRectState.MoveBottomMiddle;
                }

                if (RectLeftMiddle.Contains(e.Location) )
                {
                    MoveSelectRectState = MoveSelectRectState.MoveLeftMiddle;
                }

                if ( RectRightMiddle.Contains(e.Location))
                {
                    MoveSelectRectState = MoveSelectRectState.MoveRightMiddle;
                }

                MoveStartLocation = e.Location;
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

            if (OperatorStatus == OperatorStatus.FinishedSelect)
            {
                MoveSelectRectState = MoveSelectRectState.None;
                this.Invalidate();
            }
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

            //base.OnPaintBackground(e);
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
                DrawSelectAare(g);
            }

            //绘制当前Point信息
            if (OperatorStatus != OperatorStatus.FinishedSelect  || (OperatorStatus == OperatorStatus.FinishedSelect && MoveSelectRectState!= MoveSelectRectState.None && MoveSelectRectState != MoveSelectRectState.MoveAll))
            {
                DrawPointInfo(g);
            }

            //绘制工具条
            if (OperatorStatus == OperatorStatus.FinishedSelect && MoveSelectRectState== MoveSelectRectState.None)
            {
                DrawToolbar(g);
            }

            //base.OnPaint(e);
        }


        private void DrawSelectAare(Graphics g)
        {
            g.DrawImage(this.ScreenSrcImage, SelectRect, SelectRect, GraphicsUnit.Pixel);
            g.DrawRectangle(new Pen(Brushes.Green, 1), SelectRect);

            //尺寸信息
            Font font = new Font("宋体", 12, FontStyle.Bold);
            var sizeinfo = $"{SelectRect.Width}✖{SelectRect.Height}";

            if (SelectRect.Top > 20)
            {
                g.DrawString(sizeinfo, font, Brushes.White, SelectRect.Left, SelectRect.Top - 20);
            }
            else
            {
                g.DrawString(sizeinfo, font, Brushes.Black, SelectRect.Left + 5, SelectRect.Top + 5);
            }

            //定位点
            g.FillRectangles(Brushes.Green, new Rectangle[] { RectLeftTop, RectLeftBottom, RectRightTop, RectRightBottom, RectLeftMiddle, RectTopMiddle, RectRightMiddle, RectBottomMiddle });
        }

        private void DrawPointInfo(Graphics g)
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

        private void DrawToolbar(Graphics g)
        {
            int offset = 4;
            int toolbarWidth = 400;
            int toolbarHeight = 40;

            int toolbarLeft = SelectRect.Right - toolbarWidth;
            if (toolbarLeft < SelectRect.Left)
                toolbarLeft = SelectRect.Left;

            int toolbarTop = 0;
            if (SelectRect.Bottom + offset + toolbarHeight <= this.Height)
            {
                toolbarTop = SelectRect.Bottom + offset;
            }
            else
            {
                if(SelectRect.Top> toolbarHeight+ offset)
                {
                    toolbarTop = SelectRect.Top - toolbarHeight - offset;
                }
                else
                {
                    toolbarTop = SelectRect.Bottom - toolbarHeight - offset;
                    toolbarLeft = SelectRect.Right - toolbarWidth - offset;
                    if (toolbarLeft < SelectRect.Left + 4)
                        toolbarLeft = SelectRect.Left + 4;
                }
            }

            Rectangle toolbarRect = new Rectangle(toolbarLeft, toolbarTop, toolbarWidth, toolbarHeight);
            g.FillRectangle(Brushes.White, toolbarRect);

            Rectangle toolSave = new Rectangle(toolbarRect.Left, toolbarRect.Top, toolbarHeight, toolbarHeight);
            Rectangle toolCancel = new Rectangle(toolbarRect.Left+ toolbarHeight, toolbarRect.Top, toolbarHeight, toolbarHeight);
            Rectangle toolOK = new Rectangle(toolbarRect.Left + toolbarHeight*2, toolbarRect.Top, toolbarHeight, toolbarHeight);

            g.DrawImage(Properties.Resources.save, toolSave.Left + 4, toolSave.Top + 4);
            g.DrawImage(Properties.Resources.cancel, toolCancel.Left + 4, toolCancel.Top + 4);
            g.DrawImage(Properties.Resources.ok, toolOK.Left + 4, toolOK.Top + 4);


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

    public enum MoveSelectRectState
    {
        None,
        MoveAll,
        MoveLeftTop,
        MoveRightTop,
        MoveLeftBottom,
        MoveRightBottom,
        MoveLeftMiddle,
        MoveRightMiddle,
        MoveTopMiddle,
        MoveBottomMiddle
    }
}
