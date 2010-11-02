using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Xml;

namespace Brejc.GpsLibrary.Gpx
{
    /// <summary>
    /// GPX file.
    /// </summary>
    public class GpxFile
    {
        public string Creator
        {
            get { return creator; }
            set { creator = value; }
        }

        public GpsMetadata Metadata
        {
            get { return metadata; }
            set { metadata = value; }
        }

        public IList<TrackPoint> Waypoints
        {
            get { return waypoints; }
        }

        public IList<Track> Routes
        {
            get { return routes; }
        }

        public IList<Track> Tracks
        {
            get { return tracks; }
        }

        public void AddRoute (Track route)
        {
            Routes.Add (route);
        }

        public void AddTrack (Track track)
        {
            Tracks.Add (track);
        }

        public void AddWaypoint (TrackPoint waypoint)
        {
            Waypoints.Add (waypoint);
        }

        public void AddWaypoints (IList<TrackPoint> waypoints)
        {
            this.waypoints.AddRange(waypoints);
        }

        public GpsTrackStatistics CalculateStatistics ()
        {
            GpsTrackStatistics stat = new GpsTrackStatistics ();
            foreach (TrackPoint w in waypoints)
            {
                stat.Bounds.ExtendToCover (w.Location);
                if (w.Elevation.HasValue)
                {
                    stat.MinElevation = Math.Min (stat.MinElevation, w.Elevation.Value);
                    stat.MaxElevation = Math.Min (stat.MaxElevation, w.Elevation.Value);
                }

                if (w.Time.HasValue)
                {
                    stat.StartTime = stat.StartTime <= w.Time.Value ? stat.StartTime : w.Time.Value;
                    stat.EndTime = stat.EndTime >= w.Time.Value ? stat.EndTime : w.Time.Value;
                }
            }

            foreach (Track r in routes)
            {
                GpsTrackStatistics childStat = GpsUtilities.CalculateTrackStatistics (r);
                stat.Union (childStat);
            }

            foreach (Track t in tracks)
            {
                GpsTrackStatistics childStat = GpsUtilities.CalculateTrackStatistics (t);
                stat.Union (childStat);
            }

            return stat;
        }

        static public GpxFile Load (string filePath)
        {
            using (FileStream stream = File.Open (filePath, FileMode.Open))
                return Load (stream);
        }

        [SuppressMessage ("Microsoft.Naming", "CA1720:IdentifiersShouldNotContainTypeNames", MessageId = "string")]
        static public GpxFile LoadFromString (string gpxString)
        {
            using (MemoryStream stream = new MemoryStream (UTF8Encoding.Default.GetBytes (gpxString)))
                return Load (stream);
        }

        static public GpxFile Load (Stream stream)
        {
            XmlDocument xmlDoc = new XmlDocument ();
            xmlDoc.Load(stream);

            IGpxDataTranslator gpxDataTranslator = null;
            switch (xmlDoc.DocumentElement.NamespaceURI)
            {
                case "http://www.topografix.com/GPX/1/1":
                    gpxDataTranslator = new Gpx11DataTranslator();
                    break;
                case "http://www.topografix.com/GPX/1/0":
                    gpxDataTranslator = new Gpx10DataTranslator ();
                    break;
                    //throw new NotSupportedException ("GPX version 1.0 is currently not supported. Please use a 3rd party tool (like gpsbabel) to convert it to 1.1 format.");
                default:
                    throw new ArgumentException("This is not a GPX file.");
            }

            // move back to the start of the stream
            stream.Seek(0, SeekOrigin.Begin);
            GpxFile gpxFile = new GpxFile ();
            gpxDataTranslator.ReadGpxData(gpxFile, stream);
            return gpxFile;
        }

        public void Save (string fileName)
        {
            Gpx11DataTranslator translator = new Gpx11DataTranslator();
            translator.WriteGpxData(this, fileName);
        }

        private string creator = "";

        private GpsMetadata metadata;
        private List<TrackPoint> waypoints = new List<TrackPoint> ();
        private List<Track> routes = new List<Track> ();
        private List<Track> tracks = new List<Track> ();
    }
}
