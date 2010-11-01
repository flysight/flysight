using System;
using System.Globalization;
using System.IO;
using GMap.NET;

namespace FlySightViewer
{
    static class FlySight
    {
        #region -- Static helpers ---------------------------------------------

        public static LogEntry Import(string aPath)
        {
            string[] lines = File.ReadAllLines(aPath);
            if (lines.Length > 2)
            {
                Record rec = ParseRecord(lines[2]);
                LogEntry entry = new LogEntry(rec.Time, lines.Length);
                entry.Records.Add(rec);

                for (int i = 3; i < lines.Length; i++)
                {
                    entry.Records.Add(ParseRecord(lines[i]));
                }

                return entry;
            }

            return null;
        }

        private static Record ParseRecord(string aLine)
        {
            NumberFormatInfo info = NumberFormatInfo.InvariantInfo;

            string[] fields = aLine.Split(',');

            Record result = new Record();
            result.Time = GetDateTime(fields[0]);
            result.Location = new PointLatLng(double.Parse(fields[1], info), double.Parse(fields[2], info));
            result.Altitude = float.Parse(fields[3], info);
            result.VelocityNorth = float.Parse(fields[4], info);
            result.VelocityEast = float.Parse(fields[5], info);
            result.VelocityDown = float.Parse(fields[6], info);
            return result;
        }

        private static DateTime GetDateTime(string aDate)
        {
            // 2010-10-16T20:48:10.60Z
            int year = int.Parse(aDate.Substring(0, 4));
            int month = int.Parse(aDate.Substring(5, 2));
            int day = int.Parse(aDate.Substring(8, 2));
            int hour = int.Parse(aDate.Substring(11, 2));
            int minute = int.Parse(aDate.Substring(14, 2));
            int second = int.Parse(aDate.Substring(17, 2));
            int ms = int.Parse(aDate.Substring(20, 2));
            return new DateTime(year, month, day, hour, minute, second, ms, DateTimeKind.Utc);
        }

        #endregion
    }
}
