using System;
using Brejc.GpsLibrary.Gpx;
using GMap.NET;
using Brejc.GpsLibrary.Gpx.Filtering;

namespace FlySightViewer
{
    delegate void AddEntryCallback(string aName, LogEntry aEntry);

    static class GpxImporter
    {
        public static void Import(string aKey, string aPath, AddEntryCallback aCallback)
        {
            GpxFile file = GpxFile.Load(aPath);
            if (file != null)
            {
                Import(aKey, file, aCallback);
            }
        }

        public static void Import(string aKey, GpxFile aFile, AddEntryCallback aCallback)
        {
            if (aFile != null)
            {
                // apply some basic filtering of loaded GPX data
                FileFilterChain filterChain = new FileFilterChain();
                filterChain.Filters.Add(new RemoveErrorPointsFilter());
                aFile = filterChain.ApplyFilters(aFile);

                // convert to LogEntry.
                int idx = 0;
                foreach (Track track in aFile.Tracks)
                {
                    if (track.Segments.Count > 0)
                    {
                        TrackSegment firstSeg = track.Segments[0];

                        string key = string.Format("{0}/{1}[{2}]", aKey, track.Name, idx);

                        DateTime time = (DateTime)firstSeg.StartTime;
                        LogEntry entry = new LogEntry(key, time, firstSeg.PointsCount);

                        foreach (TrackSegment seg in track.Segments)
                        {
                            foreach (TrackPoint pnt in seg.Points)
                            {
                                Record rec = new Record();
                                rec.Location = new PointLatLng(pnt.Location.Y, pnt.Location.X);

                                if (pnt.Time != null)
                                {
                                    rec.Time = (DateTime)pnt.Time;
                                }

                                if (pnt.Elevation != null)
                                {
                                    rec.Altitude = (float)pnt.Elevation;
                                }

                                entry.Records.Add(rec);
                            }
                        }

                        aCallback(key, entry);
                        idx++;
                    }
                }
            }
        }
    }
}
