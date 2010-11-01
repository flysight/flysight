using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace FlySightViewer
{
    static class GpxFile
    {
        private static XmlSerializer mGpx11Serializer = new XmlSerializer(typeof(gpx11.gpxType));

        public static LogEntry Import(string aPath)
        {
            string text = File.ReadAllText(aPath);

            try
            {
                return ImportGPX11(text);
            }
            catch (Exception)
            {
                MessageBox.Show("Not a recognised GPX file.");
            }

            return null;
        }

        public static LogEntry ImportGPX11(string aText)
        {
            using (StringReader reader = new StringReader(aText))
            {
                gpx11.gpxType response = (gpx11.gpxType)mGpx11Serializer.Deserialize(reader);
                if (response != null)
                {
                    DateTime date = DateTime.Now;

                    // read the date.
                    gpx11.metadataType md = response.metadata;
                    if (md != null)
                    {
                        date = md.time;
                    }

                    if (response.trk == null)
                    {
                        MessageBox.Show("There is no track information in the GPX file");
                        return null;
                    }

                    LogEntry entry = new LogEntry(date, 1000);
                    foreach (var track in response.trk)
                    {
                        foreach (var seg in track.trkseg)
                        {
                            foreach (var point in seg.trkpt)
                            {
                                Record rec = new Record();
                                rec.Location = new GMap.NET.PointLatLng((double)point.lat, (double)point.lon);
                                rec.Altitude = (float)point.geoidheight;
                                rec.Time = point.time;
                                entry.Records.Add(rec);
                            }
                        }
                    }
                    return entry;
                }
            }
            return null;
        }
    }
}
