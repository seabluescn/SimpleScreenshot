using SimpleScreenshot.Properties;
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
        private Bitmap RealScreenImage = null;
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

        Rectangle toolbarRect = new Rectangle();
        private List<Toolbar> Toolbars = new List<Toolbar>();
        

        #region BaseLogic       

        public FormMain()
        {
            InitializeComponent();
            
            Toolbars.Add(new Toolbar(Resources.edit, Resources.edit_a));
            Toolbars.Add(new Toolbar(Resources.Splitter, Resources.Splitter));
            Toolbars.Add(new Toolbar(Resources.save, Resources.save_a));
            Toolbars.Add(new Toolbar(Resources.cancel, Resources.cancel_a));
            Toolbars.Add(new Toolbar(Resources.ok, Resources.ok_a));
        }

        private void FormMain_Load(object sender, EventArgs e)
        { 
            HotKetID = HotKeyHelpper.GlobalAddAtom("SimpleScreenshot-HotKey");

            Debug.WriteLine($"FormMain_Load:HotKetID={HotKetID}");

            HotKeyHelpper.KeyModifiers keyModifiers 
                = (Settings.Default.HotKey_Ctrl ? HotKeyHelpper.KeyModifiers.Ctrl : HotKeyHelpper.KeyModifiers.None)
                | (Settings.Default.HotKey_Shift ? HotKeyHelpper.KeyModifiers.Shift : HotKeyHelpper.KeyModifiers.None)
                | (Settings.Default.HotKey_Alt ? HotKeyHelpper.KeyModifiers.Alt : HotKeyHelpper.KeyModifiers.None);
            
            var result = HotKeyHelpper.RegisterHotKey(this.Handle, HotKetID, keyModifiers, Settings.Default.HotKey_KeyValue);

            if (result == false)
            {
                MessageBox.Show("热键冲突,请修改热键。","Simple Screenshot");
            }
            else
            {
                new FormStartOK().ShowDialog();
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
            Debug.WriteLine($"ProcessHotkey:keyid={keyid},HotKetID={HotKetID}");
            if (keyid == HotKetID)
            {
                StartScreenshot();
            }
        }

        private void FormMain_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                StopScreeshot();
            }
        }

        private void StartScreenshot()
        {
            Debug.WriteLine($"DESKTOP:{PrimaryScreen.DESKTOP.Width},{PrimaryScreen.DESKTOP.Height}");
            Debug.WriteLine($"DPI:{PrimaryScreen.DpiX},{PrimaryScreen.DpiY}");
            Debug.WriteLine($"Scale:{PrimaryScreen.ScaleX},{PrimaryScreen.ScaleY}");

            Scale_X = PrimaryScreen.ScaleX;
            Scale_Y = PrimaryScreen.ScaleY;

            var size = PrimaryScreen.DESKTOP;
            var width = size.Width;
            var height = size.Height;

            RealScreenImage = new Bitmap(width, height);
            using (Graphics graphics = Graphics.FromImage(RealScreenImage))
            {
                graphics.CopyFromScreen(0, 0, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);

                if (Scale_X > 1 || Scale_Y > 1)
                {
                    Bitmap imageSRC = new Bitmap(this.Width, this.Height);
                    using (Graphics g = Graphics.FromImage(imageSRC))
                    {
                        Rectangle rectSrc = new Rectangle(0, 0, width, height);
                        Rectangle rectDes = new Rectangle(0, 0, this.Width, this.Height);
                        g.DrawImage(RealScreenImage, rectDes, rectSrc, GraphicsUnit.Pixel);

                        ScreenSrcImage?.Dispose();
                        ScreenSrcImage = imageSRC;
                    }
                }
                else
                {
                    ScreenSrcImage?.Dispose();
                    ScreenSrcImage = RealScreenImage;
                }
            }

            this.ScreenshotStatus = ScreenshotStatus.Screenshoting;
            this.Visible = true;
        }               

        private void StopScreeshot()
        {
            this.Visible = false;

            this.ScreenshotStatus = ScreenshotStatus.None;
            this.OperatorStatus = OperatorStatus.None;
            this.Cursor = Cursors.Default;
        }

        #endregion

        #region Paint             

        private void FormMain_MouseMove(object sender, MouseEventArgs e)
        {
            if (ScreenshotStatus != ScreenshotStatus.Screenshoting)
                return;

            //Debug.WriteLine($"1 Mouse move:{e.Button},{e.Location.X},{e.Location.Y}");

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

                        if (toolbarRect.Contains(e.Location))
                        {
                            this.Cursor = Cursors.Default;
                            this.Invalidate(toolbarRect);
                        }

                        return;
                    }

                    this.Cursor = Cursors.Default;

                    if (toolbarRect.Contains(e.Location))
                    {
                        this.Invalidate(toolbarRect);
                    }
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

        private void FormMain_MouseClick(object sender, MouseEventArgs e)
        {
            if (ScreenshotStatus != ScreenshotStatus.Screenshoting)
                return;

            if(OperatorStatus == OperatorStatus.FinishedSelect && MoveSelectRectState == MoveSelectRectState.None)
            {
                if (toolbarRect.Contains(e.Location))
                {
                    int toolbarHeight = 40;
                    int count = (e.Location.X - toolbarRect.Left) / toolbarHeight;
                    ToolbarType toolbarType = (ToolbarType)count;
                   
                    switch(toolbarType)
                    {
                        case ToolbarType.Save:
                            toolbarSave_Click();
                            break;

                        case ToolbarType.Cancel:
                            toolbarCancele_Click();
                            break;

                        case ToolbarType.OK:
                            toolbarOK_Click();
                            break;

                        case ToolbarType.Edit:
                            toolbarEdit_Click();
                            break;
                    }
                }
            }
        }

        #region toolbar_click

        private void toolbarSave_Click()
        {
            DateTime now = DateTime.Now;
            SaveFileDialog fileDialog = new SaveFileDialog
            {
                Filter = "PNG(*.png)|*.png|JPEG(*.jpg)|*.jpg|BMP(*.bmp)|*.bmp",
                FileName = $"Screenshot_{now.Year}{now.Month,0:D2}{now.Day,0:D2}{now.Hour,0:D2}{now.Minute,0:D2}{now.Second,0:D2}"
            };             

            if (fileDialog.ShowDialog()== DialogResult.OK)
            {   
                ImageFormat imageFormat = ImageFormat.Png;
                switch (fileDialog.FilterIndex)
                {
                    case 1: imageFormat = ImageFormat.Png; break;
                    case 2: imageFormat = ImageFormat.Jpeg; break;
                    case 3: imageFormat = ImageFormat.Bmp; break;                    
                }

                Bitmap bitmap = GetSelectedImage();               
                bitmap.Save(fileDialog.FileName, imageFormat);

                StopScreeshot();

                new FormSuccess().ShowDialog();               
            }           
        }

        private void toolbarCancele_Click()
        {
            StopScreeshot();
        }

        private void toolbarOK_Click()
        {
            Bitmap bitmap = GetSelectedImage();

            Clipboard.SetDataObject(bitmap);
            StopScreeshot();
        }

        private Bitmap GetSelectedImage()
        {
            Rectangle RealRect = SelectRect;
            if ((Scale_X - 1.0f) > 0.05f || (Scale_Y - 1.0f) > 0.05f)
            {
                RealRect = new Rectangle((int)(SelectRect.Left * Scale_X), (int)(SelectRect.Top * Scale_Y), (int)(SelectRect.Width * Scale_X), (int)(SelectRect.Height * Scale_Y));
            }

            Bitmap bitmap = new Bitmap(RealRect.Width, RealRect.Height);
            Graphics g = Graphics.FromImage(bitmap);
            g.DrawImage(this.RealScreenImage, 0, 0, RealRect, GraphicsUnit.Pixel);
            g.Dispose();
            return bitmap;
        }

        private void toolbarEdit_Click()
        {
            StopScreeshot();

            FormEdit formEdit = new FormEdit();
            formEdit.Show();
        }

        #endregion


        protected override void OnPaintBackground(PaintEventArgs e)
        {
            //Debug.WriteLine($"2 OnPaintBackground");

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
                //g.DrawImage(this.RealScreenImage, FullRect, 0, 0, this.Width, this.Height, GraphicsUnit.World);
            }
            else
            {
                g.DrawImage(this.ScreenSrcImage, 0, 0, this.Width, this.Height);
            }

            //base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {           
            //Debug.WriteLine($"3 OnPaint:CurrMouseLocation:{CurrMouseLocation}");

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
            int toolbarWidth = 200;
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

            toolbarRect = new Rectangle(toolbarLeft, toolbarTop, toolbarWidth, toolbarHeight);
            g.FillRectangle(Brushes.White, toolbarRect);

            int count = 0;
            foreach(var toolbar in Toolbars)
            {
                toolbar.Rectangle = new Rectangle(toolbarRect.Left + toolbarHeight * count, toolbarRect.Top, toolbarHeight, toolbarHeight);

                if (toolbar.Rectangle.Contains(CurrMouseLocation))
                {
                    g.DrawImage(toolbar.Icon_A, toolbar.Rectangle.Left + 4, toolbar.Rectangle.Top + 4);
                }
                else
                {
                    g.DrawImage(toolbar.Icon, toolbar.Rectangle.Left + 4, toolbar.Rectangle.Top + 4);
                }

                count++;
            }   
        }

        #endregion

        #region Menu Control

        private void screenShotAltSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StartScreenshot();
        }                 

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void seteupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormSetup frmSetup = new FormSetup();

            if(frmSetup.ShowDialog()== DialogResult.OK )
            {

            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
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

    public enum ToolbarType
    {
        Edit=0,       
        None=1,
        Save=2,
        Cancel=3,
        OK=4
    }
}
