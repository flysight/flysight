
namespace GMap.NET
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading;
    using GMap.NET.Internals;
    using GMap.NET.Projections;

    /// <summary>
    /// maps manager
    /// </summary>
    public class GMaps : Singleton<GMaps>
    {
        // Google version strings
        public string VersionGoogleMap = "m@132";
        public string VersionGoogleSatellite = "71";
        public string VersionGoogleLabels = "h@132";
        public string VersionGoogleTerrain = "t@125,r@132";
        public string SecGoogleWord = "Galileo";

        // Google (China) version strings
        public string VersionGoogleMapChina = "m@132";
        public string VersionGoogleSatelliteChina = "s@71";
        public string VersionGoogleLabelsChina = "h@132";
        public string VersionGoogleTerrainChina = "t@125,r@132";

        // Google (Korea) version strings
        public string VersionGoogleMapKorea = "kr1.12";
        public string VersionGoogleSatelliteKorea = "71";
        public string VersionGoogleLabelsKorea = "kr1t.12";

        /// <summary>
        /// Gets or sets the value of the User-agent HTTP header.
        /// </summary>
        public string UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.7) Gecko/20091221 Firefox/3.5.7";

        /// <summary>
        /// timeout for map connections
        /// </summary>
        public int Timeout = 30 * 1000;

        internal string LanguageStr;
        LanguageType language = LanguageType.English;

        /// <summary>
        /// map language
        /// </summary>
        public LanguageType Language
        {
            get
            {
                return language;
            }
            set
            {
                language = value;
                LanguageStr = Stuff.EnumToString(Language);
            }
        }

        /// <summary>
        /// is map ussing cache for routing
        /// </summary>
        public bool UseRouteCache = true;

        /// <summary>
        /// is map using cache for geocoder
        /// </summary>
        public bool UseGeocoderCache = true;

        /// <summary>
        /// is map using cache for placemarks
        /// </summary>
        public bool UsePlacemarkCache = true;

        /// <summary>
        /// max zoom for maps, 17 is max fo many maps
        /// </summary>
        public readonly int MaxZoom = 17;

        /// <summary>
        /// Radius of the Earth
        /// </summary>
        public double EarthRadiusKm = 6378.137; // WGS-84

        /// <summary>
        /// internal proxy for image managment
        /// </summary>
        public PureImageProxy ImageProxy;

        /// <summary>
        /// load tiles in random sequence
        /// </summary>
        public bool ShuffleTilesOnLoad = true;

        /// <summary>
        /// tiles in memmory
        /// </summary>
        internal readonly KiberTileCache TilesInMemory = new KiberTileCache();

        /// <summary>
        /// lock for TilesInMemory
        /// </summary>
        internal readonly FastReaderWriterLock kiberCacheLock = new FastReaderWriterLock();

        /// <summary>
        /// the amount of tiles in MB to keep in memmory, default: 22MB, if each ~100Kb it's ~222 tiles
        /// </summary>
        public int MemoryCacheCapacity
        {
            get
            {
                kiberCacheLock.AcquireReaderLock();
                try
                {
                    return TilesInMemory.MemoryCacheCapacity;
                }
                finally
                {
                    kiberCacheLock.ReleaseReaderLock();
                }
            }
            set
            {
                kiberCacheLock.AcquireWriterLock();
                try
                {
                    TilesInMemory.MemoryCacheCapacity = value;
                }
                finally
                {
                    kiberCacheLock.ReleaseWriterLock();
                }
            }
        }

        /// <summary>
        /// current memmory cache size in MB
        /// </summary>
        public double MemoryCacheSize
        {
            get
            {
                kiberCacheLock.AcquireReaderLock();
                try
                {
                    return TilesInMemory.MemoryCacheSize;
                }
                finally
                {
                    kiberCacheLock.ReleaseReaderLock();
                }
            }
        }

        bool? isRunningOnMono;

        /// <summary>
        /// return true if running on mono
        /// </summary>
        /// <returns></returns>
        public bool IsRunningOnMono
        {
            get
            {
                if (!isRunningOnMono.HasValue)
                {
                    try
                    {
                        isRunningOnMono = (Type.GetType("Mono.Runtime") != null);
                        return isRunningOnMono.Value;
                    }
                    catch
                    {
                    }
                }
                else
                {
                    return isRunningOnMono.Value;
                }
                return false;
            }
        }

        /// <summary>
        /// true if google versions was corrected
        /// </summary>
        bool IsCorrectedGoogleVersions = false;

        public GMaps()
        {
            #region singleton check
            if (Instance != null)
            {
                throw (new Exception("You have tried to create a new singleton class where you should have instanced it. Replace your \"new class()\" with \"class.Instance\""));
            }
            #endregion

            Language = LanguageType.English;
            ServicePointManager.DefaultConnectionLimit = 444;

            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object obj)
               {
                   TryCorrectGoogleVersions();
               }));
        }

        #region -- Stuff --

        MemoryStream GetTileFromMemoryCache(RawTile tile)
        {
            kiberCacheLock.AcquireReaderLock();
            try
            {
                MemoryStream ret = null;
                if (TilesInMemory.TryGetValue(tile, out ret))
                {
                    return ret;
                }
            }
            finally
            {
                kiberCacheLock.ReleaseReaderLock();
            }
            return null;
        }

        void AddTileToMemoryCache(RawTile tile, MemoryStream data)
        {
            kiberCacheLock.AcquireWriterLock();
            try
            {
                if (!TilesInMemory.ContainsKey(tile))
                {
                    TilesInMemory.Add(tile, Stuff.CopyStream(data, true));
                }
            }
            finally
            {
                kiberCacheLock.ReleaseWriterLock();
            }
        }

        /// <summary>
        /// gets all layers of map type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public MapType[] GetAllLayersOfType(MapType type)
        {
            MapType[] types = null;
            {
                switch (type)
                {
                    case MapType.GoogleHybrid:
                        {
                            types = new MapType[2];
                            types[0] = MapType.GoogleSatellite;
                            types[1] = MapType.GoogleLabels;
                        }
                        break;

                    default:
                        {
                            types = new MapType[1];
                            types[0] = type;
                        }
                        break;
                }
            }

            return types;
        }

        /// <summary>
        /// sets projection using specific map
        /// </summary>
        /// <param name="type"></param>
        /// <param name="Projection"></param>
        public void AdjustProjection(MapType type, ref PureProjection Projection, out int maxZoom)
        {
            maxZoom = MaxZoom;

            if (false == (Projection is MercatorProjection))
            {
                Projection = new MercatorProjection();
                maxZoom = GMaps.Instance.MaxZoom;
            }
        }

        /// <summary>
        /// distance (in km) between two points specified by latitude/longitude
        /// The Haversine formula, http://www.movable-type.co.uk/scripts/latlong.html
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public double GetDistance(PointLatLng p1, PointLatLng p2)
        {
            double dLat1InRad = p1.Lat * (Math.PI / 180);
            double dLong1InRad = p1.Lng * (Math.PI / 180);
            double dLat2InRad = p2.Lat * (Math.PI / 180);
            double dLong2InRad = p2.Lng * (Math.PI / 180);
            double dLongitude = dLong2InRad - dLong1InRad;
            double dLatitude = dLat2InRad - dLat1InRad;
            double a = Math.Pow(Math.Sin(dLatitude / 2), 2) + Math.Cos(dLat1InRad) * Math.Cos(dLat2InRad) * Math.Pow(Math.Sin(dLongitude / 2), 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double dDistance = EarthRadiusKm * c;
            return dDistance;
        }

        /// <summary>
        /// Accepts two coordinates in degrees.
        /// </summary>
        /// <returns>A double value in degrees. From 0 to 360.</returns>
        public double GetBearing(PointLatLng p1, PointLatLng p2)
        {
            var latitude1 = ToRadian(p1.Lat);
            var latitude2 = ToRadian(p2.Lat);
            var longitudeDifference = ToRadian(p2.Lng - p1.Lng);

            var y = Math.Sin(longitudeDifference) * Math.Cos(latitude2);
            var x = Math.Cos(latitude1) * Math.Sin(latitude2) - Math.Sin(latitude1) * Math.Cos(latitude2) * Math.Cos(longitudeDifference);

            return (ToDegree(Math.Atan2(y, x)) + 360) % 360;
        }

        /// <summary>
        /// Converts degrees to Radians.
        /// </summary>
        /// <returns>Returns a radian from degrees.</returns>
        public static Double ToRadian(Double degree)
        {
            return (degree * Math.PI / 180.0);
        }

        /// <summary>
        /// To degress from a radian value.
        /// </summary>
        /// <returns>Returns degrees from radians.</returns>
        public static Double ToDegree(Double radian)
        {
            return (radian / Math.PI * 180.0);
        }

        #endregion

        #region -- URL generation --

        /// <summary>
        /// makes url for image
        /// </summary>
        /// <param name="type"></param>
        /// <param name="pos"></param>
        /// <param name="zoom"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        internal string MakeImageUrl(MapType type, Point pos, int zoom, string language)
        {
            switch (type)
            {
                #region -- Google --
                case MapType.GoogleMap:
                    {
                        string server = "mt";
                        string request = "vt";
                        string sec1 = ""; // after &x=...
                        string sec2 = ""; // after &zoom=...
                        GetSecGoogleWords(pos, out sec1, out sec2);

                        // http://mt1.google.com/vt/lyrs=m@130&hl=lt&x=18683&s=&y=10413&z=15&s=Galile

                        return string.Format("http://{0}{1}.google.com/{2}/lyrs={3}&hl={4}&x={5}{6}&y={7}&z={8}&s={9}", server, GetServerNum(pos, 4), request, VersionGoogleMap, language, pos.X, sec1, pos.Y, zoom, sec2);
                    }

                case MapType.GoogleSatellite:
                    {
                        string server = "khm";
                        string request = "kh";
                        string sec1 = ""; // after &x=...
                        string sec2 = ""; // after &zoom=...
                        GetSecGoogleWords(pos, out sec1, out sec2);

                        return string.Format("http://{0}{1}.google.com/{2}/v={3}&hl={4}&x={5}{6}&y={7}&z={8}&s={9}", server, GetServerNum(pos, 4), request, VersionGoogleSatellite, language, pos.X, sec1, pos.Y, zoom, sec2);
                    }

                case MapType.GoogleLabels:
                    {
                        string server = "mt";
                        string request = "vt";
                        string sec1 = ""; // after &x=...
                        string sec2 = ""; // after &zoom=...
                        GetSecGoogleWords(pos, out sec1, out sec2);

                        // http://mt1.google.com/vt/lyrs=h@107&hl=lt&x=583&y=325&z=10&s=Ga
                        // http://mt0.google.com/vt/lyrs=h@130&hl=lt&x=1166&y=652&z=11&s=Galile

                        return string.Format("http://{0}{1}.google.com/{2}/lyrs={3}&hl={4}&x={5}{6}&y={7}&z={8}&s={9}", server, GetServerNum(pos, 4), request, VersionGoogleLabels, language, pos.X, sec1, pos.Y, zoom, sec2);
                    }

                case MapType.GoogleTerrain:
                    {
                        string server = "mt";
                        string request = "vt";
                        string sec1 = ""; // after &x=...
                        string sec2 = ""; // after &zoom=...
                        GetSecGoogleWords(pos, out sec1, out sec2);

                        return string.Format("http://{0}{1}.google.com/{2}/v={3}&hl={4}&x={5}{6}&y={7}&z={8}&s={9}", server, GetServerNum(pos, 4), request, VersionGoogleTerrain, language, pos.X, sec1, pos.Y, zoom, sec2);
                    }
                #endregion
            }

            return null;
        }

        Projections.MercatorProjection ProjectionForWMS = new Projections.MercatorProjection();

        /// <summary>
        /// gets secure google words based on position
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="sec1"></param>
        /// <param name="sec2"></param>
        internal void GetSecGoogleWords(Point pos, out string sec1, out string sec2)
        {
            sec1 = ""; // after &x=...
            sec2 = ""; // after &zoom=...
            int seclen = ((pos.X * 3) + pos.Y) % 8;
            sec2 = SecGoogleWord.Substring(0, seclen);
            if (pos.Y >= 10000 && pos.Y < 100000)
            {
                sec1 = "&s=";
            }
        }

        /// <summary>
        /// gets server num based on position
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        internal int GetServerNum(Point pos, int max)
        {
            return (pos.X + 2 * pos.Y) % max;
        }

        #endregion

        #region -- Content download --

        /// <summary>
        /// try to correct google versions
        /// </summary>    
        internal void TryCorrectGoogleVersions()
        {
            if (!IsCorrectedGoogleVersions)
            {
                string url = @"http://maps.google.com";
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Proxy = WebRequest.DefaultWebProxy;
                    request.UserAgent = UserAgent;
                    request.Timeout = Timeout;
                    request.ReadWriteTimeout = Timeout * 6;

                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            using (StreamReader read = new StreamReader(responseStream))
                            {
                                string html = read.ReadToEnd();

                                Regex reg = new Regex("\"*http://mt0.google.com/vt/lyrs=m@(\\d*)", RegexOptions.IgnoreCase);
                                Match mat = reg.Match(html);
                                if (mat.Success)
                                {
                                    GroupCollection gc = mat.Groups;
                                    int count = gc.Count;
                                    if (count > 0)
                                    {
                                        VersionGoogleMap = string.Format("m@{0}", gc[1].Value);
                                        VersionGoogleMapChina = VersionGoogleMap;
                                        Debug.WriteLine("TryCorrectGoogleVersions, VersionGoogleMap: " + VersionGoogleMap);
                                    }
                                }

                                reg = new Regex("\"*http://mt0.google.com/vt/lyrs=h@(\\d*)", RegexOptions.IgnoreCase);
                                mat = reg.Match(html);
                                if (mat.Success)
                                {
                                    GroupCollection gc = mat.Groups;
                                    int count = gc.Count;
                                    if (count > 0)
                                    {
                                        VersionGoogleLabels = string.Format("h@{0}", gc[1].Value);
                                        VersionGoogleLabelsChina = VersionGoogleLabels;
                                        Debug.WriteLine("TryCorrectGoogleVersions, VersionGoogleLabels: " + VersionGoogleLabels);
                                    }
                                }

                                reg = new Regex("\"*http://khm0.google.com/kh/v=(\\d*)", RegexOptions.IgnoreCase);
                                mat = reg.Match(html);
                                if (mat.Success)
                                {
                                    GroupCollection gc = mat.Groups;
                                    int count = gc.Count;
                                    if (count > 0)
                                    {
                                        VersionGoogleSatellite = gc[1].Value;
                                        VersionGoogleSatelliteKorea = VersionGoogleSatellite;
                                        VersionGoogleSatelliteChina = "s@" + VersionGoogleSatellite;
                                        Debug.WriteLine("TryCorrectGoogleVersions, VersionGoogleSatellite: " + VersionGoogleSatellite);
                                    }
                                }

                                reg = new Regex("\"*http://mt0.google.com/vt/lyrs=t@(\\d*),r@(\\d*)", RegexOptions.IgnoreCase);
                                mat = reg.Match(html);
                                if (mat.Success)
                                {
                                    GroupCollection gc = mat.Groups;
                                    int count = gc.Count;
                                    if (count > 1)
                                    {
                                        VersionGoogleTerrain = string.Format("t@{0},r@{1}", gc[1].Value, gc[2].Value);
                                        VersionGoogleTerrainChina = VersionGoogleTerrain;
                                        Debug.WriteLine("TryCorrectGoogleVersions, VersionGoogleTerrain: " + VersionGoogleTerrain);
                                    }
                                }
                            }
                        }
                    }
                    IsCorrectedGoogleVersions = true; // try it only once
                }
                catch (Exception ex)
                {
                    IsCorrectedGoogleVersions = false;
                    Debug.WriteLine("TryCorrectGoogleVersions failed: " + ex.ToString());
                }
            }
        }
        
        /// <summary>
        /// gets image from tile server
        /// </summary>
        /// <param name="type"></param>
        /// <param name="pos"></param>
        /// <param name="zoom"></param>
        /// <returns></returns>
        public PureImage GetImageFrom(MapType type, Point pos, int zoom, out Exception result)
        {
            PureImage ret = null;
            result = null;

            try
            {
                // let't check memmory first
                MemoryStream m = GetTileFromMemoryCache(new RawTile(type, pos, zoom));
                if (m != null)
                {
                    if (GMaps.Instance.ImageProxy != null)
                    {
                        ret = GMaps.Instance.ImageProxy.FromStream(m);
                        if (ret == null)
                        {
                            m.Dispose();
                        }
                    }
                }

                if (ret == null)
                {
                    string url = MakeImageUrl(type, pos, zoom, LanguageStr);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Proxy = WebRequest.DefaultWebProxy;
                    request.UserAgent = UserAgent;
                    request.Timeout = Timeout;
                    request.ReadWriteTimeout = Timeout * 6;
                    request.Accept = "*/*";
                    request.Referer = "http://maps.google.com/";

                    Debug.WriteLine("Starting GetResponse: " + pos);

                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        Debug.WriteLine("GetResponse OK: " + pos);

                        Debug.WriteLine("Starting GetResponseStream: " + pos);
                        MemoryStream responseStream = Stuff.CopyStream(response.GetResponseStream(), false);
                        {
                            Debug.WriteLine("GetResponseStream OK: " + pos);

                            if (GMaps.Instance.ImageProxy != null)
                            {
                                ret = GMaps.Instance.ImageProxy.FromStream(responseStream);

                                // Enqueue Cache
                                if (ret != null)
                                {
                                    AddTileToMemoryCache(new RawTile(type, pos, zoom), responseStream);
                                }
                            }
                        }
                        response.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                result = ex;
                ret = null;
                Debug.WriteLine("GetImageFrom: " + ex.ToString());
            }

            return ret;
        }

        #endregion
    }
}
