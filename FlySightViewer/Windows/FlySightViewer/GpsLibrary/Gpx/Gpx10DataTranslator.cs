using System.IO;
using System.Xml.Serialization;
using Brejc.Geometry;
using Brejc.GpsLibrary.Gpx.Schema10;

namespace Brejc.GpsLibrary.Gpx
{
    public class Gpx10DataTranslator : IGpxDataTranslator
    {
        public void ReadGpxData(GpxFile gpxFile, Stream stream)
        {
            XmlSerializer serializer = new XmlSerializer (typeof (gpx));
            gpx gpx = (gpx)serializer.Deserialize (stream);

            if (gpx.trk != null)
            {
                foreach (gpxTrk trkType in gpx.trk)
                {
                    Track track = new Track ();

                    foreach (gpxTrkTrkseg trkseg in trkType.trkseg)
                    {
                        if (trkseg.trkpt.Length > 0)
                        {
                            TrackSegment segment = new TrackSegment ();
                            foreach (gpxTrkTrksegTrkpt trkpt in trkseg.trkpt)
                            {
                                TrackPoint waypoint = ReadTrackPoint (trkpt);
                                segment.AddPoint (waypoint);
                            }

                            track.AddSegment (segment);
                        }
                    }

                    gpxFile.AddTrack (track);
                }
            }

            if (gpx.wpt != null)
            {
                foreach (gpxWpt wptType in gpx.wpt)
                {
                    TrackPoint waypoint = ReadWayoint(wptType);
                    gpxFile.AddWaypoint (waypoint);
                }
            }

            if (gpx.rte != null)
            {
                foreach (gpxRte rteType in gpx.rte)
                {
                    Track route = new Track ();

                    if (rteType.rtept != null)
                    {
                        TrackSegment segment = new TrackSegment ();
                        foreach (gpxRteRtept wptType in rteType.rtept)
                        {
                            TrackPoint waypoint = ReadRoutePoint (wptType);
                            segment.AddPoint (waypoint);
                        }
                    }

                    gpxFile.AddRoute (route);
                }
            }
        }

        public void WriteGpxData(GpxFile gpxFile, string fileName)
        {
            throw new System.NotImplementedException();
        }

        static private TrackPoint ReadTrackPoint (gpxTrkTrksegTrkpt wptType)
        {
            TrackPoint waypoint = new TrackPoint ();

            if (wptType.ageofdgpsdataSpecified)
                waypoint.AgeOfGpsData = wptType.ageofdgpsdata;
            waypoint.Comment = wptType.cmt;
            waypoint.Description = wptType.desc;
            waypoint.DgpsId = wptType.dgpsid;
            if (wptType.eleSpecified)
                waypoint.Elevation = (double)wptType.ele;
            if (wptType.fixSpecified)
                waypoint.FixType = wptType.fix.ToString ();
            if (wptType.geoidheightSpecified)
                waypoint.GeoidHeight = wptType.geoidheight;
            if (wptType.hdopSpecified)
                waypoint.HorizontalDilutionOfPrecision = wptType.hdop;
            waypoint.Location = new Point2<double> ((double)wptType.lon, (double)wptType.lat);
            if (wptType.magvarSpecified)
                waypoint.MagneticVariation = wptType.magvar;
            waypoint.Name = wptType.name;
            if (wptType.pdopSpecified)
                waypoint.PositionDilutionOfPrecision = wptType.pdop;
            //wptType.sat;
            waypoint.SourceOfData = wptType.src;
            waypoint.Symbol = wptType.sym;
            if (wptType.timeSpecified)
                waypoint.Time = wptType.time;
            //wptType.type;
            if (wptType.vdopSpecified)
                waypoint.VerticalDilutionOfPrecision = wptType.vdop;
            if (wptType.timeSpecified)
                waypoint.Time = wptType.time;

            return waypoint;
        }

        static private TrackPoint ReadWayoint (gpxWpt wptType)
        {
            TrackPoint waypoint = new TrackPoint ();

            if (wptType.ageofdgpsdataSpecified)
                waypoint.AgeOfGpsData = wptType.ageofdgpsdata;
            waypoint.Comment = wptType.cmt;
            waypoint.Description = wptType.desc;
            waypoint.DgpsId = wptType.dgpsid;
            if (wptType.eleSpecified)
                waypoint.Elevation = (double)wptType.ele;
            if (wptType.fixSpecified)
                waypoint.FixType = wptType.fix.ToString ();
            if (wptType.geoidheightSpecified)
                waypoint.GeoidHeight = wptType.geoidheight;
            if (wptType.hdopSpecified)
                waypoint.HorizontalDilutionOfPrecision = wptType.hdop;
            waypoint.Location = new Point2<double> ((double)wptType.lon, (double)wptType.lat);
            if (wptType.magvarSpecified)
                waypoint.MagneticVariation = wptType.magvar;
            waypoint.Name = wptType.name;
            if (wptType.pdopSpecified)
                waypoint.PositionDilutionOfPrecision = wptType.pdop;
            //wptType.sat;
            waypoint.SourceOfData = wptType.src;
            waypoint.Symbol = wptType.sym;
            if (wptType.timeSpecified)
                waypoint.Time = wptType.time;
            //wptType.type;
            if (wptType.vdopSpecified)
                waypoint.VerticalDilutionOfPrecision = wptType.vdop;
            if (wptType.timeSpecified)
                waypoint.Time = wptType.time;

            return waypoint;
        }

        static private TrackPoint ReadRoutePoint (gpxRteRtept wptType)
        {
            TrackPoint waypoint = new TrackPoint ();

            if (wptType.ageofdgpsdataSpecified)
                waypoint.AgeOfGpsData = wptType.ageofdgpsdata;
            waypoint.Comment = wptType.cmt;
            waypoint.Description = wptType.desc;
            waypoint.DgpsId = wptType.dgpsid;
            if (wptType.eleSpecified)
                waypoint.Elevation = (double)wptType.ele;
            if (wptType.fixSpecified)
                waypoint.FixType = wptType.fix.ToString ();
            if (wptType.geoidheightSpecified)
                waypoint.GeoidHeight = wptType.geoidheight;
            if (wptType.hdopSpecified)
                waypoint.HorizontalDilutionOfPrecision = wptType.hdop;
            waypoint.Location = new Point2<double> ((double)wptType.lon, (double)wptType.lat);
            if (wptType.magvarSpecified)
                waypoint.MagneticVariation = wptType.magvar;
            waypoint.Name = wptType.name;
            if (wptType.pdopSpecified)
                waypoint.PositionDilutionOfPrecision = wptType.pdop;
            //wptType.sat;
            waypoint.SourceOfData = wptType.src;
            waypoint.Symbol = wptType.sym;
            if (wptType.timeSpecified)
                waypoint.Time = wptType.time;
            //wptType.type;
            if (wptType.vdopSpecified)
                waypoint.VerticalDilutionOfPrecision = wptType.vdop;
            if (wptType.timeSpecified)
                waypoint.Time = wptType.time;

            return waypoint;
        }
    }
}