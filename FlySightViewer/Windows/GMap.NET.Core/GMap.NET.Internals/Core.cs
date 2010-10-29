
namespace GMap.NET.Internals
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Threading;

    /// <summary>
    /// internal map control core
    /// </summary>
    internal class Core
    {
        public PointLatLng currentPosition;
        public Point currentPositionPixel;

        public Point renderOffset;
        public Point centerTileXYLocation;
        public Point centerTileXYLocationLast;
        public Point dragPoint;

        public Point mouseDown;
        public Point mouseCurrent;
        public Point mouseLastZoom;

        public MouseWheelZoomType MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;

        public PointLatLng? LastLocationInBounds = null;
        public bool VirtualSizeEnabled = false;

        public Size sizeOfMapArea;
        public Size minOfTiles;
        public Size maxOfTiles;

        public Rectangle tileRect;
        public Rectangle tileRectBearing;
        public Rectangle currentRegion;
        public float bearing = 0;
        public bool IsRotated = false;

        public readonly TileMatrix Matrix = new TileMatrix();

        public readonly List<Point> tileDrawingList = new List<Point>();
        public readonly FastReaderWriterLock tileDrawingListLock = new FastReaderWriterLock();

        //readonly ManualResetEvent waitForTileLoad = new ManualResetEvent(false);
        public readonly Queue<LoadTask> tileLoadQueue = new Queue<LoadTask>();

        public static readonly string googleCopyright = string.Format("©{0} Google - Map data ©{0} Tele Atlas, Imagery ©{0} TerraMetrics", DateTime.Today.Year);

        static readonly int GThreadPoolSize = 5;

        DateTime LastInvalidation = DateTime.Now;
        DateTime LastTileLoadStart = DateTime.Now;
        DateTime LastTileLoadEnd = DateTime.Now;
        internal bool IsStarted = false;
        int zoom;
        internal int maxZoom = 2;
        internal int minZoom = 2;
        internal int Width;
        internal int Height;

        internal int pxRes100m;  // 100 meters
        internal int pxRes1000m;  // 1km  
        internal int pxRes10km; // 10km
        internal int pxRes100km; // 100km
        internal int pxRes1000km; // 1000km
        internal int pxRes5000km; // 5000km

        /// <summary>
        /// current peojection
        /// </summary>
        public PureProjection Projection;

        /// <summary>
        /// is user dragging map
        /// </summary>
        public bool IsDragging = false;

        public Core()
        {
            MapType = MapType.None;
        }

        /// <summary>
        /// map zoom
        /// </summary>
        public int Zoom
        {
            get
            {
                return zoom;
            }
            set
            {
                if (zoom != value && !IsDragging)
                {
                    zoom = value;

                    minOfTiles = Projection.GetTileMatrixMinXY(value);
                    maxOfTiles = Projection.GetTileMatrixMaxXY(value);

                    CurrentPositionGPixel = Projection.FromLatLngToPixel(CurrentPosition, value);

                    if (IsStarted)
                    {
                        lock (tileLoadQueue)
                        {
                            tileLoadQueue.Clear();
                        }

                        Matrix.ClearLevelsBelove(zoom - LevelsKeepInMemmory);
                        Matrix.ClearLevelsAbove(zoom + LevelsKeepInMemmory);

                        lock (FailedLoads)
                        {
                            FailedLoads.Clear();
                            RaiseEmptyTileError = true;
                        }

                        GoToCurrentPositionOnZoom();
                        UpdateBounds();

                        if (OnMapZoomChanged != null)
                        {
                            OnMapZoomChanged();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// current marker position in pixel coordinates
        /// </summary>
        public Point CurrentPositionGPixel
        {
            get
            {
                return currentPositionPixel;
            }
            internal set
            {
                currentPositionPixel = value;
            }
        }

        /// <summary>
        /// current marker position
        /// </summary>
        public PointLatLng CurrentPosition
        {
            get
            {

                return currentPosition;
            }
            set
            {
                if (!IsDragging)
                {
                    currentPosition = value;
                    CurrentPositionGPixel = Projection.FromLatLngToPixel(value, Zoom);

                    if (IsStarted)
                    {
                        GoToCurrentPosition();
                    }
                }
                else
                {
                    currentPosition = value;
                    CurrentPositionGPixel = Projection.FromLatLngToPixel(value, Zoom);
                }

                if (IsStarted)
                {
                    if (OnCurrentPositionChanged != null)
                        OnCurrentPositionChanged(currentPosition);
                }
            }
        }

        internal bool zoomToArea = true;

        MapType mapType;
        public MapType MapType
        {
            get
            {
                return mapType;
            }
            set
            {
                if (value != MapType || value == MapType.None)
                {
                    mapType = value;

                    GMaps.Instance.AdjustProjection(mapType, ref Projection, out maxZoom);

                    tileRect = new Rectangle(new Point(0, 0), Projection.TileSize);
                    tileRectBearing = tileRect;
                    if (IsRotated)
                    {
                        tileRectBearing.Inflate(1, 1);
                    }

                    minOfTiles = Projection.GetTileMatrixMinXY(Zoom);
                    maxOfTiles = Projection.GetTileMatrixMaxXY(Zoom);
                    CurrentPositionGPixel = Projection.FromLatLngToPixel(CurrentPosition, Zoom);

                    if (IsStarted)
                    {
                        CancelAsyncTasks();
                        OnMapSizeChanged(Width, Height);
                        ReloadMap();

                        if (OnMapTypeChanged != null)
                        {
                            OnMapTypeChanged(value);
                        }

                        zoomToArea = true;
                    }
                }
            }
        }

        /// <summary>
        /// sets zoom to max to fit rect
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public bool SetZoomToFitRect(RectLatLng rect)
        {
            int mmaxZoom = GetMaxZoomToFitRect(rect);
            if (mmaxZoom > 0)
            {
                PointLatLng center = new PointLatLng(rect.Lat - (rect.HeightLat / 2), rect.Lng + (rect.WidthLng / 2));
                CurrentPosition = center;

                if (mmaxZoom > maxZoom)
                {
                    mmaxZoom = maxZoom;
                }

                if ((int)Zoom != mmaxZoom)
                {
                    Zoom = mmaxZoom;
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// is polygons enabled
        /// </summary>
        public bool PolygonsEnabled = true;

        /// <summary>
        /// is routes enabled
        /// </summary>
        public bool RoutesEnabled = true;

        /// <summary>
        /// is markers enabled
        /// </summary>
        public bool MarkersEnabled = true;

        /// <summary>
        /// can user drag map
        /// </summary>
        public bool CanDragMap = true;

        /// <summary>
        /// retry count to get tile 
        /// </summary>
        public int RetryLoadTile = 0;

        /// <summary>
        /// how many levels of tiles are staying decompresed in memory
        /// </summary>
        public int LevelsKeepInMemmory = 5;

        /// <summary>
        /// occurs when current position is changed
        /// </summary>
        public event CurrentPositionChanged OnCurrentPositionChanged;

        /// <summary>
        /// occurs when tile set load is complete
        /// </summary>
        public event TileLoadComplete OnTileLoadComplete;

        /// <summary>
        /// occurs when tile set is starting to load
        /// </summary>
        public event TileLoadStart OnTileLoadStart;

        /// <summary>
        /// occurs on empty tile displayed
        /// </summary>
        public event EmptyTileError OnEmptyTileError;

        /// <summary>
        /// occurs on tile loaded
        /// </summary>
        public event NeedInvalidation OnNeedInvalidation;

        /// <summary>
        /// occurs on map drag
        /// </summary>
        public event MapDrag OnMapDrag;

        /// <summary>
        /// occurs on map zoom changed
        /// </summary>
        public event MapZoomChanged OnMapZoomChanged;

        /// <summary>
        /// occurs on map type changed
        /// </summary>
        public event MapTypeChanged OnMapTypeChanged;

        readonly List<Thread> GThreadPool = new List<Thread>();

        // windows forms or wpf
        internal string SystemType;
        internal static readonly Guid SessionIdGuid = Guid.NewGuid();
        internal static readonly Guid CompanyIdGuid = new Guid("3E35F098-CE43-4F82-9E9D-05C8B1046A45");
        internal static readonly Guid ApplicationIdGuid = new Guid("FF328040-77B0-4546-ACF3-7C6EC0827BBB");
        internal static volatile bool AnalyticsStartDone = false;
        internal static volatile bool AnalyticsStopDone = false;

        /// <summary>
        /// starts core system
        /// </summary>
        public void StartSystem()
        {
            if (!IsStarted)
            {
                IsStarted = true;
                GoToCurrentPosition();
            }
        }

        internal void ApplicationExit()
        {
        }

        public void UpdateCenterTileXYLocation()
        {
            PointLatLng center = FromLocalToLatLng(Width / 2, Height / 2);
            Point centerPixel = Projection.FromLatLngToPixel(center, Zoom);
            centerTileXYLocation = Projection.FromPixelToTileXY(centerPixel);
        }

        public int vWidth = 800;
        public int vHeight = 400;

        public void OnMapSizeChanged(int width, int height)
        {
            this.Width = width;
            this.Height = height;

            if (IsRotated)
            {
                int diag = (int)Math.Round(Math.Sqrt(Width * Width + Height * Height) / Projection.TileSize.Width, MidpointRounding.AwayFromZero);
                sizeOfMapArea.Width = 1 + (diag / 2);
                sizeOfMapArea.Height = 1 + (diag / 2);
            }
            else
            {
                sizeOfMapArea.Width = 1 + (Width / Projection.TileSize.Width) / 2;
                sizeOfMapArea.Height = 1 + (Height / Projection.TileSize.Height) / 2;
            }

            Debug.WriteLine("OnMapSizeChanged, w: " + width + ", h: " + height + ", size: " + sizeOfMapArea);

            UpdateCenterTileXYLocation();

            if (IsStarted)
            {
                UpdateBounds();

                if (OnCurrentPositionChanged != null)
                    OnCurrentPositionChanged(currentPosition);
            }
        }

        public void OnMapClose()
        {
            CancelAsyncTasks();
            IsStarted = false;

            Matrix.ClearAllLevels();

            lock (FailedLoads)
            {
                FailedLoads.Clear();
                RaiseEmptyTileError = false;
            }
        }

        /// <summary>
        /// gets current map view top/left coordinate, width in Lng, height in Lat
        /// </summary>
        /// <returns></returns>
        public RectLatLng CurrentViewArea
        {
            get
            {
                if (Projection != null)
                {
                    PointLatLng p = Projection.FromPixelToLatLng(-renderOffset.X, -renderOffset.Y, Zoom);
                    double rlng = Projection.FromPixelToLatLng(-renderOffset.X + Width, -renderOffset.Y, Zoom).Lng;
                    double blat = Projection.FromPixelToLatLng(-renderOffset.X, -renderOffset.Y + Height, Zoom).Lat;

                    return RectLatLng.FromLTRB(p.Lng, p.Lat, rlng, blat);
                }
                return RectLatLng.Empty;
            }
        }

        /// <summary>
        /// gets lat/lng from local control coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public PointLatLng FromLocalToLatLng(int x, int y)
        {
            return Projection.FromPixelToLatLng(new Point(x - renderOffset.X, y - renderOffset.Y), Zoom);
        }

        /// <summary>
        /// return local coordinates from lat/lng
        /// </summary>
        /// <param name="latlng"></param>
        /// <returns></returns>
        public Point FromLatLngToLocal(PointLatLng latlng)
        {
            Point pLocal = Projection.FromLatLngToPixel(latlng, Zoom);
            pLocal.Offset(renderOffset);
            return pLocal;
        }

        /// <summary>
        /// gets max zoom level to fit rectangle
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public int GetMaxZoomToFitRect(RectLatLng rect)
        {
            int zoom = minZoom;

            for (int i = zoom; i <= maxZoom; i++)
            {
                Point p1 = Projection.FromLatLngToPixel(rect.LocationTopLeft, i);
                Point p2 = Projection.FromLatLngToPixel(rect.LocationRightBottom, i);

                if (((p2.X - p1.X) <= Width + 10) && (p2.Y - p1.Y) <= Height + 10)
                {
                    zoom = i;
                }
                else
                {
                    break;
                }
            }

            return zoom;
        }

        /// <summary>
        /// initiates map dragging
        /// </summary>
        /// <param name="pt"></param>
        public void BeginDrag(Point pt)
        {
            dragPoint.X = pt.X - renderOffset.X;
            dragPoint.Y = pt.Y - renderOffset.Y;
            IsDragging = true;
        }

        /// <summary>
        /// ends map dragging
        /// </summary>
        public void EndDrag()
        {
            IsDragging = false;
            if (OnNeedInvalidation != null)
            {
                OnNeedInvalidation();
            }
        }

        /// <summary>
        /// reloads map
        /// </summary>
        public void ReloadMap()
        {
            if (IsStarted)
            {
                Debug.WriteLine("------------------");

                lock (tileLoadQueue)
                {
                    tileLoadQueue.Clear();
                }

                Matrix.ClearAllLevels();

                lock (FailedLoads)
                {
                    FailedLoads.Clear();
                    RaiseEmptyTileError = true;
                }

                if (OnNeedInvalidation != null)
                {
                    OnNeedInvalidation();
                }

                UpdateBounds();
            }
            else
            {
                throw new Exception("Please, do not call ReloadMap before form is loaded, it's useless");
            }
        }

        /// <summary>
        /// moves current position into map center
        /// </summary>
        public void GoToCurrentPosition()
        {
            // reset stuff
            renderOffset = Point.Empty;
            centerTileXYLocationLast = Point.Empty;
            dragPoint = Point.Empty;

            // goto location
            this.Drag(new Point(-(CurrentPositionGPixel.X - Width / 2), -(CurrentPositionGPixel.Y - Height / 2)));
        }

        public bool MouseWheelZooming = false;

        /// <summary>
        /// moves current position into map center
        /// </summary>
        internal void GoToCurrentPositionOnZoom()
        {
            // reset stuff
            renderOffset = Point.Empty;
            centerTileXYLocationLast = Point.Empty;
            dragPoint = Point.Empty;

            // goto location and centering
            if (MouseWheelZooming)
            {
                if (MouseWheelZoomType != MouseWheelZoomType.MousePositionWithoutCenter)
                {
                    Point pt = new Point(-(CurrentPositionGPixel.X - Width / 2), -(CurrentPositionGPixel.Y - Height / 2));
                    renderOffset.X = pt.X - dragPoint.X;
                    renderOffset.Y = pt.Y - dragPoint.Y;
                }
                else // without centering
                {
                    renderOffset.X = -CurrentPositionGPixel.X - dragPoint.X;
                    renderOffset.Y = -CurrentPositionGPixel.Y - dragPoint.Y;
                    renderOffset.Offset(mouseLastZoom);
                }
            }
            else // use current map center
            {
                mouseLastZoom = Point.Empty;

                Point pt = new Point(-(CurrentPositionGPixel.X - Width / 2), -(CurrentPositionGPixel.Y - Height / 2));
                renderOffset.X = pt.X - dragPoint.X;
                renderOffset.Y = pt.Y - dragPoint.Y;
            }

            UpdateCenterTileXYLocation();
        }

        /// <summary>
        /// darg map by offset in pixels
        /// </summary>
        /// <param name="offset"></param>
        public void DragOffset(Point offset)
        {
            renderOffset.Offset(offset);

            UpdateCenterTileXYLocation();

            if (centerTileXYLocation != centerTileXYLocationLast)
            {
                centerTileXYLocationLast = centerTileXYLocation;
                UpdateBounds();
            }

            {
                LastLocationInBounds = CurrentPosition;
                CurrentPosition = FromLocalToLatLng((int)Width / 2, (int)Height / 2);
            }

            if (OnMapDrag != null)
            {
                OnMapDrag();
            }
        }

        /// <summary>
        /// drag map
        /// </summary>
        /// <param name="pt"></param>
        public void Drag(Point pt)
        {
            renderOffset.X = pt.X - dragPoint.X;
            renderOffset.Y = pt.Y - dragPoint.Y;

            UpdateCenterTileXYLocation();

            if (centerTileXYLocation != centerTileXYLocationLast)
            {
                centerTileXYLocationLast = centerTileXYLocation;
                UpdateBounds();
            }

            if (IsDragging)
            {
                LastLocationInBounds = CurrentPosition;
                CurrentPosition = FromLocalToLatLng((int)Width / 2, (int)Height / 2);

                if (OnMapDrag != null)
                {
                    OnMapDrag();
                }
            }
        }

        /// <summary>
        /// cancels tile loaders and bounds checker
        /// </summary>
        public void CancelAsyncTasks()
        {
            if (IsStarted)
            {
                lock (tileLoadQueue)
                {
                    tileLoadQueue.Clear();
                }
            }
        }

        bool RaiseEmptyTileError = false;
        internal readonly Dictionary<LoadTask, Exception> FailedLoads = new Dictionary<LoadTask, Exception>();

        internal static readonly int WaitForTileLoadThreadTimeout = 5 * 1000 * 60; // 5 min.

        long loadWaitCount = 0;
        readonly object LastInvalidationLock = new object();
        readonly object LastTileLoadStartEndLock = new object();

        void ProcessLoadTask()
        {
            bool invalidate = false;
            LoadTask? task = null;
            long lastTileLoadTimeMs;
            bool stop = false;

            Thread ct = Thread.CurrentThread;
            string ctid = "Thread[" + ct.ManagedThreadId + "]";

            while (!stop)
            {
                invalidate = false;
                task = null;

                lock (tileLoadQueue)
                {
                    while (tileLoadQueue.Count == 0)
                    {
                        Debug.WriteLine(ctid + " - Wait " + loadWaitCount + " - " + DateTime.Now.TimeOfDay);

                        if (++loadWaitCount >= GThreadPoolSize)
                        {
                            loadWaitCount = 0;

                            lock (LastInvalidationLock)
                            {
                                LastInvalidation = DateTime.Now;
                            }

                            if (OnNeedInvalidation != null)
                            {
                                OnNeedInvalidation();
                            }

                            lock (LastTileLoadStartEndLock)
                            {
                                LastTileLoadEnd = DateTime.Now;
                                lastTileLoadTimeMs = (long)(LastTileLoadEnd - LastTileLoadStart).TotalMilliseconds;
                            }

                            #region -- clear stuff--
                            {
                                GMaps.Instance.kiberCacheLock.AcquireWriterLock();
                                try
                                {
                                    GMaps.Instance.TilesInMemory.RemoveMemoryOverload();
                                }
                                finally
                                {
                                    GMaps.Instance.kiberCacheLock.ReleaseWriterLock();
                                }

                                tileDrawingListLock.AcquireReaderLock();
                                try
                                {
                                    Matrix.ClearLevelAndPointsNotIn(Zoom, tileDrawingList);
                                }
                                finally
                                {
                                    tileDrawingListLock.ReleaseReaderLock();
                                }
                            }
                            #endregion

                            UpdateGroundResolution();

                            Debug.WriteLine(ctid + " - OnTileLoadComplete: " + lastTileLoadTimeMs + "ms, MemoryCacheSize: " + GMaps.Instance.MemoryCacheSize + "MB");

                            if (OnTileLoadComplete != null)
                            {
                                OnTileLoadComplete(lastTileLoadTimeMs);
                            }
                        }

                        if (false == Monitor.Wait(tileLoadQueue, WaitForTileLoadThreadTimeout, false))
                        {
                            stop = true;
                            break;
                        }
                    }

                    if (!stop || tileLoadQueue.Count > 0)
                    {
                        task = tileLoadQueue.Dequeue();
                    }
                }

                if (task.HasValue)
                {
                    try
                    {
                        #region -- execute --
                        var m = Matrix.GetTileWithReadLock(task.Value.Zoom, task.Value.Pos);

                        if (m == null || m.Overlays.Count == 0)
                        {
                            Debug.WriteLine(ctid + " - Fill empty TileMatrix: " + task);

                            Tile t = new Tile(task.Value.Zoom, task.Value.Pos);
                            var layers = GMaps.Instance.GetAllLayersOfType(MapType);

                            foreach (MapType tl in layers)
                            {
                                int retry = 0;
                                do
                                {
                                    PureImage img;
                                    Exception ex;

                                    // tile number inversion(BottomLeft -> TopLeft) for pergo maps
                                    img = GMaps.Instance.GetImageFrom(tl, task.Value.Pos, task.Value.Zoom, out ex);

                                    if (img != null)
                                    {
                                        lock (t.Overlays)
                                        {
                                            t.Overlays.Add(img);
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        if (ex != null)
                                        {
                                            lock (FailedLoads)
                                            {
                                                if (!FailedLoads.ContainsKey(task.Value))
                                                {
                                                    FailedLoads.Add(task.Value, ex);

                                                    if (OnEmptyTileError != null)
                                                    {
                                                        if (!RaiseEmptyTileError)
                                                        {
                                                            RaiseEmptyTileError = true;
                                                            OnEmptyTileError(task.Value.Zoom, task.Value.Pos);
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        if (RetryLoadTile > 0)
                                        {
                                            Debug.WriteLine(ctid + " - ProcessLoadTask: " + task + " -> empty tile, retry " + retry);
                                            {
                                                Thread.Sleep(1111);
                                            }
                                        }
                                    }
                                }
                                while (++retry < RetryLoadTile);
                            }

                            if (t.Overlays.Count > 0)
                            {
                                Matrix.SetTile(t);
                            }
                            else
                            {
                                t.Clear();
                                t = null;
                            }

                            layers = null;
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ctid + " - ProcessLoadTask: " + ex.ToString());
                    }
                    finally
                    {
                        lock (LastInvalidationLock)
                        {
                            invalidate = ((DateTime.Now - LastInvalidation).TotalMilliseconds > 111);
                            if (invalidate)
                            {
                                LastInvalidation = DateTime.Now;
                            }
                        }

                        if (invalidate)
                        {
                            if (OnNeedInvalidation != null)
                            {
                                OnNeedInvalidation();
                            }
                        }
                    }
                }
            }

            lock (tileLoadQueue)
            {
                Debug.WriteLine("Quit - " + ct.Name);
                GThreadPool.Remove(ct);
            }
        }

        /// <summary>
        /// updates map bounds
        /// </summary>
        void UpdateBounds()
        {
            if (MapType == NET.MapType.None)
            {
                return;
            }

            lock (tileLoadQueue)
            {
                tileDrawingListLock.AcquireWriterLock();
                try
                {
                    #region -- find tiles around --
                    tileDrawingList.Clear();

                    for (int i = -sizeOfMapArea.Width; i <= sizeOfMapArea.Width; i++)
                    {
                        for (int j = -sizeOfMapArea.Height; j <= sizeOfMapArea.Height; j++)
                        {
                            Point p = centerTileXYLocation;
                            p.X += i;
                            p.Y += j;

#if ContinuesMap
               // ----------------------------
               if(p.X < minOfTiles.Width)
               {
                  p.X += (maxOfTiles.Width + 1);
               }

               if(p.X > maxOfTiles.Width)
               {
                  p.X -= (maxOfTiles.Width + 1);
               }
               // ----------------------------
#endif

                            if (p.X >= minOfTiles.Width && p.Y >= minOfTiles.Height && p.X <= maxOfTiles.Width && p.Y <= maxOfTiles.Height)
                            {
                                if (!tileDrawingList.Contains(p))
                                {
                                    tileDrawingList.Add(p);
                                }
                            }
                        }
                    }

                    if (GMaps.Instance.ShuffleTilesOnLoad)
                    {
                        Stuff.Shuffle<Point>(tileDrawingList);
                    }
                    #endregion

                    foreach (Point p in tileDrawingList)
                    {
                        LoadTask task = new LoadTask(p, Zoom);
                        {
                            if (!tileLoadQueue.Contains(task))
                            {
                                tileLoadQueue.Enqueue(task);
                            }
                        }
                    }
                }
                finally
                {
                    tileDrawingListLock.ReleaseWriterLock();
                }

                #region -- starts loader threads if needed --
#if !PocketPC
                while (GThreadPool.Count < GThreadPoolSize)
#else
            while(GThreadPool.Count < GThreadPoolSize)
#endif
                {
                    Thread t = new Thread(new ThreadStart(ProcessLoadTask));
                    {
                        t.Name = "GMap.NET TileLoader: " + GThreadPool.Count;
                        t.IsBackground = true;
                        t.Priority = ThreadPriority.BelowNormal;
                    }
                    GThreadPool.Add(t);

                    Debug.WriteLine("add " + t.Name + " to GThreadPool");

                    t.Start();
                }
                #endregion

                lock (LastTileLoadStartEndLock)
                {
                    LastTileLoadStart = DateTime.Now;
                    Debug.WriteLine("OnTileLoadStart - at zoom " + Zoom + ", time: " + LastTileLoadStart.TimeOfDay);
                }

                loadWaitCount = 0;

                Monitor.PulseAll(tileLoadQueue);
            }

            if (OnTileLoadStart != null)
            {
                OnTileLoadStart();
            }
        }

        /// <summary>
        /// updates ground resolution info
        /// </summary>
        void UpdateGroundResolution()
        {
            double rez = Projection.GetGroundResolution(Zoom, CurrentPosition.Lat);
            pxRes100m = (int)(100.0 / rez); // 100 meters
            pxRes1000m = (int)(1000.0 / rez); // 1km  
            pxRes10km = (int)(10000.0 / rez); // 10km
            pxRes100km = (int)(100000.0 / rez); // 100km
            pxRes1000km = (int)(1000000.0 / rez); // 1000km
            pxRes5000km = (int)(5000000.0 / rez); // 5000km
        }
    }
}
