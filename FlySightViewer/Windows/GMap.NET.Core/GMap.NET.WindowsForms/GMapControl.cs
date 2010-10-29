
namespace GMap.NET.WindowsForms
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using System.Drawing.Text;
    using System.IO;
    using System.Threading;
    using System.Windows.Forms;
    using GMap.NET;
    using GMap.NET.Internals;

    /// <summary>
    /// GMap.NET control for Windows Forms
    /// </summary>   
    public partial class GMapControl : UserControl, IGControl
    {
        /// <summary>
        /// max zoom
        /// </summary>         
        [Category("GMap.NET")]
        [Description("maximum zoom level of map")]
        public int MaxZoom
        {
            get
            {
                return Core.maxZoom;
            }
            set
            {
                Core.maxZoom = value;
            }
        }

        /// <summary>
        /// min zoom
        /// </summary>      
        [Category("GMap.NET")]
        [Description("minimum zoom level of map")]
        public int MinZoom
        {
            get
            {
                return Core.minZoom;
            }
            set
            {
                Core.minZoom = value;
            }
        }

        /// <summary>
        /// map zooming type for mouse wheel
        /// </summary>
        [Category("GMap.NET")]
        [Description("map zooming type for mouse wheel")]
        public MouseWheelZoomType MouseWheelZoomType
        {
            get
            {
                return Core.MouseWheelZoomType;
            }
            set
            {
                Core.MouseWheelZoomType = value;
            }
        }

        /// <summary>
        /// text on empty tiles
        /// </summary>
        public string EmptyTileText = "We are sorry, but we don't\nhave imagery at this zoom\nlevel for this region.";

        /// <summary>
        /// pen for empty tile borders
        /// </summary>
        public Pen EmptyTileBorders = new Pen(Brushes.White, 1);


        /// <summary>
        /// pen for scale info
        /// </summary>
        public Pen ScalePen = new Pen(Brushes.Blue, 1);

        /// <summary>
        /// area selection pen
        /// </summary>
        public Pen SelectionPen = new Pen(Brushes.Blue, 2);

        /// <summary>
        /// background of selected area
        /// </summary>
        public Brush SelectedAreaFill = new SolidBrush(Color.FromArgb(33, Color.RoyalBlue));

        /// <summary>
        /// pen for empty tile background
        /// </summary>
        public Brush EmptytileBrush = Brushes.Navy;

        /// <summary>
        /// show map scale info
        /// </summary>
        public bool MapScaleInfoEnabled = false;

        /// <summary>
        /// retry count to get tile 
        /// </summary>
        [Browsable(false)]
        public int RetryLoadTile
        {
            get
            {
                return Core.RetryLoadTile;
            }
            set
            {
                Core.RetryLoadTile = value;
            }
        }

        /// <summary>
        /// how many levels of tiles are staying decompresed in memory
        /// </summary>
        [Browsable(false)]
        public int LevelsKeepInMemmory
        {
            get
            {
                return Core.LevelsKeepInMemmory;
            }

            set
            {
                Core.LevelsKeepInMemmory = value;
            }
        }

        /// <summary>
        /// map dragg button
        /// </summary>
        [Category("GMap.NET")]
        public MouseButtons DragButton = MouseButtons.Right;

        private bool showTileGridLines = false;

        /// <summary>
        /// shows tile gridlines
        /// </summary>
        [Category("GMap.NET")]
        [Description("shows tile gridlines")]
        public bool ShowTileGridLines
        {
            get
            {
                return showTileGridLines;
            }
            set
            {
                showTileGridLines = value;
                Invalidate();
            }
        }

        /// <summary>
        /// current selected area in map
        /// </summary>
        private RectLatLng selectedArea;

        [Browsable(false)]
        public RectLatLng SelectedArea
        {
            get
            {
                return selectedArea;
            }
            set
            {
                selectedArea = value;

                if (Core.IsStarted)
                {
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// map boundaries
        /// </summary>
        public RectLatLng? BoundsOfMap = null;

        /// <summary>
        /// enables integrated DoubleBuffer for best performance
        /// if using a lot objets on map or running on windows mobile
        /// </summary>
        public bool ForceDoubleBuffer = false;

        /// <summary>
        /// stops immediate marker/route/polygon invalidations;
        /// call Refresh to perform single refresh and reset invalidation state
        /// </summary>
        public bool HoldInvalidation = false;

        /// <summary>
        /// call this to stop HoldInvalidation and perform single refresh 
        /// </summary>
        public override void Refresh()
        {
            if (HoldInvalidation)
            {
                HoldInvalidation = false;
            }
            base.Refresh();
        }

        // internal stuff
        internal readonly Core Core = new Core();
        internal readonly Font CopyrightFont = new Font(FontFamily.GenericSansSerif, 7, FontStyle.Regular);
        internal readonly Font MissingDataFont = new Font(FontFamily.GenericSansSerif, 11, FontStyle.Bold);

        Font ScaleFont = new Font(FontFamily.GenericSansSerif, 5, FontStyle.Italic);
        internal readonly StringFormat CenterFormat = new StringFormat();
        internal readonly StringFormat BottomFormat = new StringFormat();
        readonly ImageAttributes TileFlipXYAttributes = new ImageAttributes();

        double zoomReal;
        Bitmap backBuffer;
        Graphics gxOff;

#if !DESIGN
        /// <summary>
        /// construct
        /// </summary>
        public GMapControl()
        {
            if (!DesignModeInConstruct && !IsDesignerHosted)
            {
                WindowsFormsImageProxy wimg = new WindowsFormsImageProxy();

                this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
                this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
                this.SetStyle(ControlStyles.UserPaint, true);
                this.SetStyle(ControlStyles.Opaque, true);
                ResizeRedraw = true;

                TileFlipXYAttributes.SetWrapMode(WrapMode.TileFlipXY);
                GMaps.Instance.ImageProxy = wimg;

                // to know when to invalidate
                Core.OnNeedInvalidation += new NeedInvalidation(Core_OnNeedInvalidation);
                Core.SystemType = "WindowsForms";

                Core.currentRegion = new Rectangle(-50, -50, Size.Width + 100, Size.Height + 100);

                CenterFormat.Alignment = StringAlignment.Center;
                CenterFormat.LineAlignment = StringAlignment.Center;

                BottomFormat.Alignment = StringAlignment.Center;

                BottomFormat.LineAlignment = StringAlignment.Far;
                if (GMaps.Instance.IsRunningOnMono)
                {
                    // no imports to move pointer
                    MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionWithoutCenter;
                }
            }
        }
#endif

        /// <summary>
        /// thread safe invalidation
        /// </summary>
        internal void Core_OnNeedInvalidation()
        {
            if (this.InvokeRequired)
            {
                MethodInvoker m = delegate
                {
                    Invalidate(false);
                };
                try
                {
                    this.BeginInvoke(m);
                }
                catch
                {
                }
            }
            else
            {
                Invalidate(false);
            }
        }

        /// <summary>
        /// render map in GDI+
        /// </summary>
        /// <param name="g"></param>
        void DrawMapGDIplus(Graphics g)
        {
            if (MapType == NET.MapType.None)
            {
                return;
            }

            Core.Matrix.EnterReadLock();
            Core.tileDrawingListLock.AcquireReaderLock();
            try
            {
                foreach (var tilePoint in Core.tileDrawingList)
                {
                    {
                        Core.tileRect.X = tilePoint.X * Core.tileRect.Width;
                        Core.tileRect.Y = tilePoint.Y * Core.tileRect.Height;
                        Core.tileRect.Offset(Core.renderOffset);

                        if (Core.currentRegion.IntersectsWith(Core.tileRect) || IsRotated)
                        {
                            bool found = false;
                            Tile t = Core.Matrix.GetTileWithNoLock(Core.Zoom, tilePoint);
                            if (t != null)
                            {
                                // render tile
                                lock (t.Overlays)
                                {
                                    foreach (WindowsFormsImage img in t.Overlays)
                                    {
                                        if (img != null && img.Img != null)
                                        {
                                            if (!found)
                                                found = true;

                                            g.DrawImage(img.Img, Core.tileRect.X, Core.tileRect.Y, Core.tileRectBearing.Width, Core.tileRectBearing.Height);
                                        }
                                    }
                                }
                            }
                            else // testing smooth zooming
                            {
                                int ZoomOffset = 0;
                                Tile ParentTile = null;
                                int Ix = 0;

                                while (ParentTile == null && (Core.Zoom - ZoomOffset) >= 1 && ZoomOffset <= LevelsKeepInMemmory)
                                {
                                    Ix = (int)Math.Pow(2, ++ZoomOffset);
                                    ParentTile = Core.Matrix.GetTileWithNoLock(Core.Zoom - ZoomOffset, new Point((int)(tilePoint.X / Ix), (int)(tilePoint.Y / Ix)));
                                }

                                if (ParentTile != null)
                                {
                                    int Xoff = Math.Abs(tilePoint.X - (ParentTile.Pos.X * Ix));
                                    int Yoff = Math.Abs(tilePoint.Y - (ParentTile.Pos.Y * Ix));

                                    // render tile 
                                    lock (ParentTile.Overlays)
                                    {
                                        foreach (WindowsFormsImage img in ParentTile.Overlays)
                                        {
                                            if (img != null && img.Img != null)
                                            {
                                                if (!found)
                                                    found = true;

                                                System.Drawing.RectangleF srcRect = new System.Drawing.RectangleF((float)(Xoff * (img.Img.Width / Ix)), (float)(Yoff * (img.Img.Height / Ix)), (img.Img.Width / Ix), (img.Img.Height / Ix));
                                                System.Drawing.Rectangle dst = new System.Drawing.Rectangle(Core.tileRect.X, Core.tileRect.Y, Core.tileRect.Width, Core.tileRect.Height);

                                                g.DrawImage(img.Img, dst, srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, GraphicsUnit.Pixel, TileFlipXYAttributes);
                                                g.FillRectangle(SelectedAreaFill, dst);
                                            }
                                        }
                                    }
                                }
                            }

                            // add text if tile is missing
                            if (!found)
                            {
                                lock (Core.FailedLoads)
                                {
                                    var lt = new LoadTask(tilePoint, Core.Zoom);
                                    if (Core.FailedLoads.ContainsKey(lt))
                                    {
                                        var ex = Core.FailedLoads[lt];
                                        g.FillRectangle(EmptytileBrush, new RectangleF(Core.tileRect.X, Core.tileRect.Y, Core.tileRect.Width, Core.tileRect.Height));

                                        g.DrawString("Exception: " + ex.Message, MissingDataFont, Brushes.Red, new RectangleF(Core.tileRect.X + 11, Core.tileRect.Y + 11, Core.tileRect.Width - 11, Core.tileRect.Height - 11));

                                        g.DrawString(EmptyTileText, MissingDataFont, Brushes.Blue, new RectangleF(Core.tileRect.X, Core.tileRect.Y, Core.tileRect.Width, Core.tileRect.Height), CenterFormat);

                                        g.DrawRectangle(EmptyTileBorders, Core.tileRect.X, Core.tileRect.Y, Core.tileRect.Width, Core.tileRect.Height);
                                    }
                                }
                            }

                            if (ShowTileGridLines)
                            {
                                g.DrawRectangle(EmptyTileBorders, Core.tileRect.X, Core.tileRect.Y, Core.tileRect.Width, Core.tileRect.Height);
                                {
                                    g.DrawString((tilePoint == Core.centerTileXYLocation ? "CENTER: " : "TILE: ") + tilePoint, MissingDataFont, Brushes.Red, new RectangleF(Core.tileRect.X, Core.tileRect.Y, Core.tileRect.Width, Core.tileRect.Height), CenterFormat);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                Core.tileDrawingListLock.ReleaseReaderLock();
                Core.Matrix.LeaveReadLock();
            }
        }

        /// <summary>
        /// sets zoom to max to fit rect
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public bool SetZoomToFitRect(RectLatLng rect)
        {
            int maxZoom = Core.GetMaxZoomToFitRect(rect);
            if (maxZoom > 0)
            {
                PointLatLng center = new PointLatLng(rect.Lat - (rect.HeightLat / 2), rect.Lng + (rect.WidthLng / 2));
                Position = center;

                if (maxZoom > MaxZoom)
                {
                    maxZoom = MaxZoom;
                }

                if ((int)Zoom != maxZoom)
                {
                    Zoom = maxZoom;
                }

                return true;
            }
            return false;
        }


        /// <summary>
        /// gets image of the current view
        /// </summary>
        /// <returns></returns>
        public Image ToImage()
        {
            Image ret = null;
            try
            {
                using (Bitmap bitmap = new Bitmap(Width, Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        using (Graphics gg = this.CreateGraphics())
                        {
                            g.CopyFromScreen(PointToScreen(new System.Drawing.Point()).X, PointToScreen(new System.Drawing.Point()).Y, 0, 0, new System.Drawing.Size(Width, Height));
                        }
                    }

                    // Convert the Image to a png
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Save(ms, ImageFormat.Png);
                        ret = Image.FromStream(ms);
                    }
                }
            }
            catch
            {
                ret = null;
            }
            return ret;
        }


        /// <summary>
        /// offset position in pixels
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Offset(int x, int y)
        {
            if (IsHandleCreated)
            {
                // need to fix in rotated mode usinf rotationMatrix
                // ...
                Core.DragOffset(new Point(x, y));
            }
        }

        #region UserControl Events

        protected bool DesignModeInConstruct
        {
            get
            {
                return (LicenseManager.UsageMode == LicenseUsageMode.Designtime);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsDesignerHosted
        {
            get
            {
                return IsControlDesignerHosted(this);
            }
        }

        public bool IsControlDesignerHosted(Control ctrl)
        {
            if (ctrl != null)
            {
                if (ctrl.Site != null)
                {

                    if (ctrl.Site.DesignMode == true)
                        return true;

                    else
                    {
                        if (IsControlDesignerHosted(ctrl.Parent))
                            return true;

                        else
                            return false;
                    }
                }
                else
                {
                    if (IsControlDesignerHosted(ctrl.Parent))
                        return true;
                    else
                        return false;
                }
            }
            else
                return false;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!IsDesignerHosted)
            {
                MethodInvoker m = delegate
                {
                    Thread.Sleep(222);
                    Core.StartSystem();
                };
                this.BeginInvoke(m);
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            Core.OnMapClose();
            Core.ApplicationExit();

            base.OnHandleDestroyed(e);
        }

        PointLatLng selectionStart;
        PointLatLng selectionEnd;

#if !PocketPC
        float? MapRenderTransform = null;
#endif

        public Color EmptyMapBackground = Color.WhiteSmoke;

        protected override void OnPaint(PaintEventArgs e)
        {
            if (ForceDoubleBuffer)
            {
                if (gxOff != null && backBuffer != null)
                {
                    // render white background
                    gxOff.Clear(EmptyMapBackground);

#if !PocketPC
                    if (MapRenderTransform.HasValue)
                    {
                        gxOff.ScaleTransform(MapRenderTransform.Value, MapRenderTransform.Value);
                        {
                            DrawMapGDIplus(gxOff);
                        }
                        gxOff.ResetTransform();
                    }
                    else
#endif
                    {
                        DrawMapGDIplus(gxOff);
                    }

                    OnPaintEtc(gxOff);

                    e.Graphics.DrawImage(backBuffer, 0, 0);
                }
            }
            else
            {
                e.Graphics.Clear(EmptyMapBackground);

#if !PocketPC
                if (MapRenderTransform.HasValue)
                {
                    e.Graphics.ScaleTransform(MapRenderTransform.Value, MapRenderTransform.Value);
                    {
                        DrawMapGDIplus(e.Graphics);
                    }
                    e.Graphics.ResetTransform();
                }
                else
#endif
                {
                    if (VirtualSizeEnabled)
                    {
                        e.Graphics.TranslateTransform((Width - Core.vWidth) / 2, (Height - Core.vHeight) / 2);
                    }

                    // test rotation
                    if (IsRotated)
                    {
                        e.Graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                        e.Graphics.TranslateTransform((float)(Core.Width / 2.0), (float)(Core.Height / 2.0));
                        e.Graphics.RotateTransform(-Bearing);
                        e.Graphics.TranslateTransform((float)(-Core.Width / 2.0), (float)(-Core.Height / 2.0));

                        DrawMapGDIplus(e.Graphics);

                        e.Graphics.ResetTransform();

                        OnPaintEtc(e.Graphics);
                    }
                    else
                    {
                        DrawMapGDIplus(e.Graphics);
                        OnPaintEtc(e.Graphics);
                    }
                }

                if (VirtualSizeEnabled)
                {
                    e.Graphics.ResetTransform();
                    e.Graphics.DrawRectangle(SelectionPen, (Width - Core.vWidth) / 2, (Height - Core.vHeight) / 2, Core.vWidth, Core.vHeight);
                }
            }

            base.OnPaint(e);
        }

        readonly Matrix rotationMatrix = new Matrix();
        readonly Matrix rotationMatrixInvert = new Matrix();

        /// <summary>
        /// updates rotation matrix
        /// </summary>
        void UpdateRotationMatrix()
        {
            PointF center = new PointF(Core.Width / 2, Core.Height / 2);

            rotationMatrix.Reset();
            rotationMatrix.RotateAt(-Bearing, center);

            rotationMatrixInvert.Reset();
            rotationMatrixInvert.RotateAt(-Bearing, center);
            rotationMatrixInvert.Invert();
        }

        /// <summary>
        /// returs true if map bearing is not zero
        /// </summary>    
        [Browsable(false)]
        public bool IsRotated
        {
            get
            {
                return Core.IsRotated;
            }
        }

        /// <summary>
        /// bearing for rotation of the map
        /// </summary>
        [Category("GMap.NET")]
        public float Bearing
        {
            get
            {
                return Core.bearing;
            }
            set
            {
                if (Core.bearing != value)
                {
                    bool resize = Core.bearing == 0;
                    Core.bearing = value;

                    //if(VirtualSizeEnabled)
                    //{
                    //   c.X += (Width - Core.vWidth) / 2;
                    //   c.Y += (Height - Core.vHeight) / 2;
                    //}

                    UpdateRotationMatrix();

                    if (value != 0 && value % 360 != 0)
                    {
                        Core.IsRotated = true;

                        if (Core.tileRectBearing.Size == Core.tileRect.Size)
                        {
                            Core.tileRectBearing = Core.tileRect;
                            Core.tileRectBearing.Inflate(1, 1);
                        }
                    }
                    else
                    {
                        Core.IsRotated = false;
                        Core.tileRectBearing = Core.tileRect;
                    }

                    if (resize)
                    {
                        Core.OnMapSizeChanged(Width, Height);
                    }
                }
            }
        }

        /// <summary>
        /// override, to render something more
        /// </summary>
        /// <param name="g"></param>
        protected virtual void OnPaintEtc(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.HighQuality;

            if (!SelectedArea.IsEmpty)
            {
                Point p1 = FromLatLngToLocal(SelectedArea.LocationTopLeft);
                Point p2 = FromLatLngToLocal(SelectedArea.LocationRightBottom);

                int x1 = p1.X;
                int y1 = p1.Y;
                int x2 = p2.X;
                int y2 = p2.Y;

                g.DrawRectangle(SelectionPen, x1, y1, x2 - x1, y2 - y1);
                g.FillRectangle(SelectedAreaFill, x1, y1, x2 - x1, y2 - y1);
            }

            #region -- copyright --

            g.DrawString(Core.googleCopyright, CopyrightFont, Brushes.Navy, 3, Height - CopyrightFont.Height - 5);

            #endregion

            #region -- draw scale --
#if !PocketPC
            if (MapScaleInfoEnabled)
            {
                if (Width > Core.pxRes5000km)
                {
                    g.DrawRectangle(ScalePen, 10, 10, Core.pxRes5000km, 10);
                    g.DrawString("5000Km", ScaleFont, Brushes.Blue, Core.pxRes5000km + 10, 11);
                }
                if (Width > Core.pxRes1000km)
                {
                    g.DrawRectangle(ScalePen, 10, 10, Core.pxRes1000km, 10);
                    g.DrawString("1000Km", ScaleFont, Brushes.Blue, Core.pxRes1000km + 10, 11);
                }
                if (Width > Core.pxRes100km && Zoom > 2)
                {
                    g.DrawRectangle(ScalePen, 10, 10, Core.pxRes100km, 10);
                    g.DrawString("100Km", ScaleFont, Brushes.Blue, Core.pxRes100km + 10, 11);
                }
                if (Width > Core.pxRes10km && Zoom > 5)
                {
                    g.DrawRectangle(ScalePen, 10, 10, Core.pxRes10km, 10);
                    g.DrawString("10Km", ScaleFont, Brushes.Blue, Core.pxRes10km + 10, 11);
                }
                if (Width > Core.pxRes1000m && Zoom >= 10)
                {
                    g.DrawRectangle(ScalePen, 10, 10, Core.pxRes1000m, 10);
                    g.DrawString("1000m", ScaleFont, Brushes.Blue, Core.pxRes1000m + 10, 11);
                }
                if (Width > Core.pxRes100m && Zoom > 11)
                {
                    g.DrawRectangle(ScalePen, 10, 10, Core.pxRes100m, 10);
                    g.DrawString("100m", ScaleFont, Brushes.Blue, Core.pxRes100m + 9, 11);
                }
            }
#endif
            #endregion
        }

        /// <summary>
        /// shrinks map area, useful just for testing
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool VirtualSizeEnabled
        {
            get
            {
                return Core.VirtualSizeEnabled;
            }
            set
            {
                Core.VirtualSizeEnabled = value;
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (!IsDesignerHosted && !DesignModeInConstruct)
            {
                if (ForceDoubleBuffer)
                {
                    if (backBuffer != null)
                    {
                        backBuffer.Dispose();
                        backBuffer = null;
                    }
                    if (gxOff != null)
                    {
                        gxOff.Dispose();
                        gxOff = null;
                    }

                    backBuffer = new Bitmap(Width, Height);
                    gxOff = Graphics.FromImage(backBuffer);
                }

                if (!VirtualSizeEnabled)
                {
                    Core.OnMapSizeChanged(Width, Height);
                    Core.currentRegion = new Rectangle(-50, -50, Core.Width + 50, Core.Height + 50);
                }
                else
                {
                    Core.OnMapSizeChanged(Core.vWidth, Core.vHeight);
                    Core.currentRegion = new Rectangle(-50, -50, Core.Width + 50, Core.Height + 50);
                }

                if (Visible && IsHandleCreated)
                {
                    // keep center on same position
                    Core.GoToCurrentPosition();

                    if (IsRotated)
                    {
                        UpdateRotationMatrix();
                    }
                }
            }
        }

        bool isSelected = false;
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (!IsMouseOverMarker)
            {
#if !PocketPC
                if (e.Button == DragButton && CanDragMap)
#else
            if(CanDragMap)
#endif
                {
                    Core.mouseDown = ApplyRotationInversion(e.X, e.Y);

#if !PocketPC
                    this.Cursor = System.Windows.Forms.Cursors.SizeAll;
#endif
                    Core.BeginDrag(Core.mouseDown);

#if !PocketPC
                    this.Invalidate(false);
#else
               this.Invalidate();
#endif
                }
                else if (!isSelected)
                {
                    isSelected = true;
                    SelectedArea = RectLatLng.Empty;
                    selectionEnd = PointLatLng.Empty;
                    selectionStart = FromLocalToLatLng(e.X, e.Y);
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (isSelected)
            {
                isSelected = false;
            }

            if (Core.IsDragging)
            {
                if (isDragging)
                {
                    isDragging = false;
                    Debug.WriteLine("IsDragging = " + isDragging);
                }
                Core.EndDrag();

#if !PocketPC
                this.Cursor = System.Windows.Forms.Cursors.Default;
#endif
                if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
                {
                    if (Core.LastLocationInBounds.HasValue)
                    {
                        Position = Core.LastLocationInBounds.Value;
                    }
                }
            }
            else
            {
#if !PocketPC
                if (!selectionEnd.IsEmpty && !selectionStart.IsEmpty)
                {
                    if (!SelectedArea.IsEmpty && Form.ModifierKeys == Keys.Shift)
                    {
                        SetZoomToFitRect(SelectedArea);
                    }
                }
#endif
            }
        }

        /// <summary>
        /// apply transformation if in rotation mode
        /// </summary>
        Point ApplyRotationInversion(int x, int y)
        {
            Point ret = new Point(x, y);

            if (IsRotated)
            {
                System.Drawing.Point[] tt = new System.Drawing.Point[] { new System.Drawing.Point(x, y) };
                rotationMatrixInvert.TransformPoints(tt);
                var f = tt[0];

                if (VirtualSizeEnabled)
                {
                    f.X += (Width - Core.vWidth) / 2;
                    f.Y += (Height - Core.vHeight) / 2;
                }

                ret.X = f.X;
                ret.Y = f.Y;
            }

            return ret;
        }

        /// <summary>
        /// apply transformation if in rotation mode
        /// </summary>
        Point ApplyRotation(int x, int y)
        {
            Point ret = new Point(x, y);

            if (IsRotated)
            {
                Point[] tt = new Point[] { new Point(x, y) };
                rotationMatrix.TransformPoints(tt);
                var f = tt[0];

                if (VirtualSizeEnabled)
                {
                    f.X += (Width - Core.vWidth) / 2;
                    f.Y += (Height - Core.vHeight) / 2;
                }

                ret.X = f.X;
                ret.Y = f.Y;
            }

            return ret;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (Core.IsDragging)
            {
                if (!isDragging)
                {
                    isDragging = true;
                    Debug.WriteLine("IsDragging = " + isDragging);
                }

                if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
                {
                    // ...
                }
                else
                {
                    Core.mouseCurrent = ApplyRotationInversion(e.X, e.Y);
                    Core.Drag(Core.mouseCurrent);
                    Refresh();
                }
            }
            else
            {
                if (isSelected && !selectionStart.IsEmpty && (Form.ModifierKeys == Keys.Alt || Form.ModifierKeys == Keys.Shift))
                {
                    selectionEnd = FromLocalToLatLng(e.X, e.Y);
                    {
                        GMap.NET.PointLatLng p1 = selectionStart;
                        GMap.NET.PointLatLng p2 = selectionEnd;

                        double x1 = Math.Min(p1.Lng, p2.Lng);
                        double y1 = Math.Max(p1.Lat, p2.Lat);
                        double x2 = Math.Max(p1.Lng, p2.Lng);
                        double y2 = Math.Min(p1.Lat, p2.Lat);

                        SelectedArea = new RectLatLng(y1, x1, x2 - x1, y1 - y2);
                    }
                }
            }

            base.OnMouseMove(e);
        }

#if !PocketPC

        /// <summary>
        /// reverses MouseWheel zooming direction
        /// </summary>
        public bool InvertedMouseWheelZooming = false;

        /// <summary>
        /// lets you zoom by MouseWheel even when pointer is in area of marker
        /// </summary>
        public bool IgnoreMarkerOnMouseWheel = false;

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if ((!IsMouseOverMarker || IgnoreMarkerOnMouseWheel) && !Core.IsDragging)
            {
                if (Core.mouseLastZoom.X != e.X && Core.mouseLastZoom.Y != e.Y)
                {
                    if (MouseWheelZoomType == MouseWheelZoomType.MousePositionAndCenter)
                    {
                        Core.currentPosition = FromLocalToLatLng(e.X, e.Y);
                    }
                    else if (MouseWheelZoomType == MouseWheelZoomType.ViewCenter)
                    {
                        Core.currentPosition = FromLocalToLatLng((int)Width / 2, (int)Height / 2);
                    }
                    else if (MouseWheelZoomType == MouseWheelZoomType.MousePositionWithoutCenter)
                    {
                        Core.currentPosition = FromLocalToLatLng(e.X, e.Y);
                    }

                    Core.mouseLastZoom.X = e.X;
                    Core.mouseLastZoom.Y = e.Y;
                }

                // set mouse position to map center
                if (MouseWheelZoomType != MouseWheelZoomType.MousePositionWithoutCenter)
                {
                    if (!GMaps.Instance.IsRunningOnMono)
                    {
                        System.Drawing.Point p = PointToScreen(new System.Drawing.Point(Width / 2, Height / 2));
                        Stuff.SetCursorPos((int)p.X, (int)p.Y);
                    }
                }

                Core.MouseWheelZooming = true;

                if (e.Delta > 0)
                {
                    if (!InvertedMouseWheelZooming)
                    {
                        Zoom++;
                    }
                    else
                    {
                        Zoom--;
                    }
                }
                else if (e.Delta < 0)
                {
                    if (!InvertedMouseWheelZooming)
                    {
                        Zoom--;
                    }
                    else
                    {
                        Zoom++;
                    }
                }

                Core.MouseWheelZooming = false;
            }
        }
#endif
        #endregion

        #region IGControl Members

        /// <summary>
        /// Call it to empty tile cache & reload tiles
        /// </summary>
        public void ReloadMap()
        {
            Core.ReloadMap();
        }

        /// <summary>
        /// gets world coordinate from local control coordinate 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public PointLatLng FromLocalToLatLng(int x, int y)
        {
#if !PocketPC
            if (MapRenderTransform.HasValue)
            {
                // var tp = MapRenderTransform.Inverse.Transform(new System.Windows.Point(x, y));
                //x = (int) tp.X;
                //y = (int) tp.Y;
                x = (int)(x * MapRenderTransform.Value);
                y = (int)(y * MapRenderTransform.Value);
            }

            if (IsRotated)
            {
                System.Drawing.Point[] tt = new System.Drawing.Point[] { new System.Drawing.Point(x, y) };
                rotationMatrixInvert.TransformPoints(tt);
                var f = tt[0];

                if (VirtualSizeEnabled)
                {
                    f.X += (Width - Core.vWidth) / 2;
                    f.Y += (Height - Core.vHeight) / 2;
                }

                x = f.X;
                y = f.Y;
            }
#endif
            return Core.FromLocalToLatLng(x, y);
        }

        /// <summary>
        /// gets local coordinate from world coordinate
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Point FromLatLngToLocal(PointLatLng point)
        {
            Point ret = Core.FromLatLngToLocal(point);

            if (MapRenderTransform.HasValue)
            {
                //var tp = MapRenderTransform.Transform(new System.Windows.Point(ret.X, ret.Y));
                ret.X = (int)(ret.X / MapRenderTransform.Value);
                ret.Y = (int)(ret.X / MapRenderTransform.Value);
            }

            if (IsRotated)
            {
                System.Drawing.Point[] tt = new System.Drawing.Point[] { new System.Drawing.Point(ret.X, ret.Y) };
                rotationMatrix.TransformPoints(tt);
                var f = tt[0];

                if (VirtualSizeEnabled)
                {
                    f.X += (Width - Core.vWidth) / 2;
                    f.Y += (Height - Core.vHeight) / 2;
                }

                ret.X = f.X;
                ret.Y = f.Y;
            }

            return ret;
        }

        [Category("GMap.NET"), DefaultValue(0)]
        public double Zoom
        {
            get
            {
                return zoomReal;
            }
            set
            {
                if (zoomReal != value)
                {
                    Debug.WriteLine("ZoomPropertyChanged: " + zoomReal + " -> " + value);

                    if (value > MaxZoom)
                    {
                        zoomReal = MaxZoom;
                    }
                    else if (value < MinZoom)
                    {
                        zoomReal = MinZoom;
                    }
                    else
                    {
                        zoomReal = value;
                    }

                    float remainder = (float)System.Decimal.Remainder((Decimal)value, (Decimal)1);
                    if (remainder != 0)
                    {
                        float scaleValue = remainder + 1;
                        {
                            MapRenderTransform = scaleValue;
                        }

                        ZoomStep = Convert.ToInt32(value - remainder);
                    }
                    else
                    {
                        MapRenderTransform = null;
                        ZoomStep = Convert.ToInt32(value);
                        zoomReal = ZoomStep;
                    }
                }
            }
        }

        /// <summary>
        /// map zoom level
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        internal int ZoomStep
        {
            get
            {
                return Core.Zoom;
            }
            set
            {
                if (value > MaxZoom)
                {
                    Core.Zoom = MaxZoom;
                }
                else if (value < MinZoom)
                {
                    Core.Zoom = MinZoom;
                }
                else
                {
                    Core.Zoom = value;
                }
            }
        }

        /// <summary>
        /// current map center position
        /// </summary>
        [Browsable(false)]
        public PointLatLng Position
        {
            get
            {
                return Core.CurrentPosition;
            }
            set
            {
                Core.CurrentPosition = value;
                Refresh();
            }
        }

        /// <summary>
        /// current marker position in pixel coordinates
        /// </summary>
        [Browsable(false)]
        public Point CurrentPositionGPixel
        {
            get
            {
                return Core.CurrentPositionGPixel;
            }
        }

        bool isDragging = false;

        /// <summary>
        /// is user dragging map
        /// </summary>
        [Browsable(false)]
        public bool IsDragging
        {
            get
            {
                return isDragging;
            }
        }

        bool isMouseOverMarker;

        /// <summary>
        /// is mouse over marker
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool IsMouseOverMarker
        {
            get
            {
                return isMouseOverMarker;
            }
            internal set
            {
                isMouseOverMarker = value;
            }
        }

        /// <summary>
        /// gets current map view top/left coordinate, width in Lng, height in Lat
        /// </summary>
        [Browsable(false)]
        public RectLatLng CurrentViewArea
        {
            get
            {
                return Core.CurrentViewArea;
            }
        }

        /// <summary>
        /// type of map
        /// </summary>
        [Category("GMap.NET"), DefaultValue(MapType.None)]
        public MapType MapType
        {
            get
            {
                return Core.MapType;
            }
            set
            {
                if (Core.MapType != value)
                {
                    Debug.WriteLine("MapType: " + Core.MapType + " -> " + value);

                    RectLatLng viewarea = SelectedArea;
                    if (viewarea != RectLatLng.Empty)
                    {
                        Position = new PointLatLng(viewarea.Lat - viewarea.HeightLat / 2, viewarea.Lng + viewarea.WidthLng / 2);
                    }
                    else
                    {
                        viewarea = CurrentViewArea;
                    }

                    Core.MapType = value;

                    if (Core.IsStarted)
                    {
                        if (Core.zoomToArea)
                        {
                            // restore zoomrect as close as possible
                            if (viewarea != RectLatLng.Empty && viewarea != CurrentViewArea)
                            {
                                int bestZoom = Core.GetMaxZoomToFitRect(viewarea);
                                if (bestZoom > 0 && Zoom != bestZoom)
                                {
                                    Zoom = bestZoom;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// map projection
        /// </summary>
        [Browsable(false)]
        public PureProjection Projection
        {
            get
            {
                return Core.Projection;
            }
        }

        /// <summary>
        /// is routes enabled
        /// </summary>
        [Category("GMap.NET")]
        public bool RoutesEnabled
        {
            get
            {
                return Core.RoutesEnabled;
            }
            set
            {
                Core.RoutesEnabled = value;
            }
        }

        /// <summary>
        /// is polygons enabled
        /// </summary>
        [Category("GMap.NET")]
        public bool PolygonsEnabled
        {
            get
            {
                return Core.PolygonsEnabled;
            }
            set
            {
                Core.PolygonsEnabled = value;
            }
        }

        /// <summary>
        /// is markers enabled
        /// </summary>
        [Category("GMap.NET")]
        public bool MarkersEnabled
        {
            get
            {
                return Core.MarkersEnabled;
            }
            set
            {
                Core.MarkersEnabled = value;
            }
        }

        /// <summary>
        /// can user drag map
        /// </summary>
        [Category("GMap.NET")]
        public bool CanDragMap
        {
            get
            {
                return Core.CanDragMap;
            }
            set
            {
                Core.CanDragMap = value;
            }
        }

        /// <summary>
        /// gets map manager
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public GMaps Manager
        {
            get
            {
                return GMaps.Instance;
            }
        }

        #endregion

        #region IGControl event Members

        /// <summary>
        /// occurs when current position is changed
        /// </summary>
        public event CurrentPositionChanged OnCurrentPositionChanged
        {
            add
            {
                Core.OnCurrentPositionChanged += value;
            }
            remove
            {
                Core.OnCurrentPositionChanged -= value;
            }
        }

        /// <summary>
        /// occurs when tile set load is complete
        /// </summary>
        public event TileLoadComplete OnTileLoadComplete
        {
            add
            {
                Core.OnTileLoadComplete += value;
            }
            remove
            {
                Core.OnTileLoadComplete -= value;
            }
        }

        /// <summary>
        /// occurs when tile set is starting to load
        /// </summary>
        public event TileLoadStart OnTileLoadStart
        {
            add
            {
                Core.OnTileLoadStart += value;
            }
            remove
            {
                Core.OnTileLoadStart -= value;
            }
        }

        /// <summary>
        /// occurs on map drag
        /// </summary>
        public event MapDrag OnMapDrag
        {
            add
            {
                Core.OnMapDrag += value;
            }
            remove
            {
                Core.OnMapDrag -= value;
            }
        }

        /// <summary>
        /// occurs on map zoom changed
        /// </summary>
        public event MapZoomChanged OnMapZoomChanged
        {
            add
            {
                Core.OnMapZoomChanged += value;
            }
            remove
            {
                Core.OnMapZoomChanged -= value;
            }
        }

        /// <summary>
        /// occures on map type changed
        /// </summary>
        public event MapTypeChanged OnMapTypeChanged
        {
            add
            {
                Core.OnMapTypeChanged += value;
            }
            remove
            {
                Core.OnMapTypeChanged -= value;
            }
        }

        /// <summary>
        /// occurs on empty tile displayed
        /// </summary>
        public event EmptyTileError OnEmptyTileError
        {
            add
            {
                Core.OnEmptyTileError += value;
            }
            remove
            {
                Core.OnEmptyTileError -= value;
            }
        }

        #endregion
    }
}
