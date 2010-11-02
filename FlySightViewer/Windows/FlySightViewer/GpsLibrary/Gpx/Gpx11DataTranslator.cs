using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Brejc.Geometry;
using Brejc.GpsLibrary.Gpx.Schema11;

namespace Brejc.GpsLibrary.Gpx
{
    public class Gpx11DataTranslator : IGpxDataTranslator
    {
        public void ReadGpxData(GpxFile gpxFile, Stream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(gpxType));
            gpxType gpx = (gpxType)serializer.Deserialize(stream);

            if (gpx.trk != null)
            {
                foreach (trkType trkType in gpx.trk)
                {
                    Track track = new Track();

                    if (trkType.trkseg != null)
                    {
                        foreach (trksegType trksegType in trkType.trkseg)
                        {
                            if (trksegType.trkpt != null)
                            {
                                TrackSegment segment = new TrackSegment();
                                foreach (wptType wptType in trksegType.trkpt)
                                {
                                    TrackPoint waypoint = ReadTrackPoint(wptType);
                                    segment.AddPoint(waypoint);
                                }

                                track.AddSegment(segment);
                            }
                        }
                    }

                    gpxFile.AddTrack(track);
                }
            }

            if (gpx.wpt != null)
            {
                foreach (wptType wptType in gpx.wpt)
                {
                    TrackPoint waypoint = ReadTrackPoint(wptType);
                    gpxFile.AddWaypoint(waypoint);
                }
            }

            if (gpx.rte != null)
            {
                foreach (rteType rteType in gpx.rte)
                {
                    Track route = new Track();

                    if (rteType.rtept != null)
                    {
                        TrackSegment segment = new TrackSegment();
                        foreach (wptType wptType in rteType.rtept)
                        {
                            TrackPoint waypoint = ReadTrackPoint(wptType);
                            segment.AddPoint(waypoint);
                        }

                        route.AddSegment(segment);
                    }

                    gpxFile.AddRoute(route);
                }
            }
        }

        public void WriteGpxData(GpxFile gpxFile, string fileName)
        {
            gpxType gpx = new gpxType();

            List<trkType> destTracks = new List<trkType>();

            // tracks
            foreach (Track track in gpxFile.Tracks)
            {
                List<trksegType> destSegments = new List<trksegType>();

                foreach (TrackSegment segment in track.Segments)
                {
                    trksegType destSegment = new trksegType();
                    List<wptType> destWaypoints = new List<wptType>();

                    foreach (TrackPoint point in segment.Points)
                        destWaypoints.Add(SaveTrackPoint(point));

                    destSegment.trkpt = destWaypoints.ToArray();
                    destSegments.Add(destSegment);
                }

                if (destSegments.Count > 0)
                {
                    trkType destTrack = new trkType();
                    destTrack.trkseg = destSegments.ToArray();
                    destTracks.Add(destTrack);
                }
            }

            if (destTracks.Count > 0)
                gpx.trk = destTracks.ToArray();

            // waypoints 
            List<wptType> destWaypoints2 = new List<wptType>();

            foreach (TrackPoint waypoint in gpxFile.Waypoints)
                destWaypoints2.Add(SaveTrackPoint(waypoint));

            if (destWaypoints2.Count > 0)
                gpx.wpt = destWaypoints2.ToArray();

            // routes
            List<rteType> destRoutes = new List<rteType>();

            foreach (Track route in gpxFile.Routes)
            {
                List<wptType> destRoutePoints = new List<wptType>();

                foreach (TrackSegment segment in route.Segments)
                {
                    foreach (TrackPoint routePoint in segment.Points)
                        destRoutePoints.Add(SaveTrackPoint(routePoint));
                }

                if (destRoutePoints.Count > 0)
                {
                    rteType destRoute = new rteType();
                    destRoute.rtept = destRoutePoints.ToArray();
                    destRoutes.Add(destRoute);
                }
            }

            if (destRoutes.Count > 0)
                gpx.rte = destRoutes.ToArray();

            using (FileStream stream = File.Open(fileName, FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(gpxType));
                serializer.Serialize(stream, gpx);
            }
        }

        static private TrackPoint ReadTrackPoint(wptType wptType)
        {
            TrackPoint waypoint = new TrackPoint();

            if (wptType.ageofdgpsdataSpecified)
                waypoint.AgeOfGpsData = wptType.ageofdgpsdata;
            waypoint.Comment = wptType.cmt;
            waypoint.Description = wptType.desc;
            waypoint.DgpsId = wptType.dgpsid;
            if (wptType.eleSpecified)
                waypoint.Elevation = (double)wptType.ele;
            if (wptType.fixSpecified)
                waypoint.FixType = wptType.fix.ToString();
            if (wptType.geoidheightSpecified)
                waypoint.GeoidHeight = wptType.geoidheight;
            if (wptType.hdopSpecified)
                waypoint.HorizontalDilutionOfPrecision = wptType.hdop;
            waypoint.Location = new Point2<double>((double)wptType.lon, (double)wptType.lat);
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

        private static wptType SaveTrackPoint(TrackPoint point)
        {
            wptType wp = new wptType();

            if (point.AgeOfGpsData.HasValue)
            {
                wp.ageofdgpsdataSpecified = true;
                wp.ageofdgpsdata = point.AgeOfGpsData.Value;
            }
            wp.cmt = point.Comment;
            wp.desc = point.Description;
            wp.dgpsid = point.DgpsId;
            if (point.Elevation.HasValue)
            {
                wp.eleSpecified = true;
                wp.ele = (decimal)point.Elevation.Value;
            }
            if (false == String.IsNullOrEmpty(point.FixType))
            {
                wp.fixSpecified = true;
                wp.fix = (fixType)Enum.Parse(typeof(fixType), point.FixType);
            }
            if (point.GeoidHeight.HasValue)
            {
                wp.geoidheightSpecified = true;
                wp.geoidheight = point.GeoidHeight.Value;
            }
            if (point.HorizontalDilutionOfPrecision.HasValue)
            {
                wp.hdopSpecified = true;
                wp.hdop = point.HorizontalDilutionOfPrecision.Value;
            }
            wp.lon = (decimal)point.Location.X;
            wp.lat = (decimal)point.Location.Y;
            wp.name = point.Name;
            if (point.MagneticVariation.HasValue)
            {
                wp.magvarSpecified = true;
                wp.magvar = point.MagneticVariation.Value;
            }
            if (point.PositionDilutionOfPrecision.HasValue)
            {
                wp.pdopSpecified = true;
                wp.pdop = point.PositionDilutionOfPrecision.Value;
            }
            wp.src = point.SourceOfData;
            wp.sym = point.Symbol;
            if (point.Time.HasValue)
            {
                wp.timeSpecified = true;
                wp.time = point.Time.Value;
            }
            if (point.VerticalDilutionOfPrecision.HasValue)
            {
                wp.vdopSpecified = true;
                wp.vdop = point.VerticalDilutionOfPrecision.Value;
            }

            return wp;
        }
    }
}