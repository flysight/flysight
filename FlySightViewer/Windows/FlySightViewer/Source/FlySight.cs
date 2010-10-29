using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using GMap.NET;

namespace FlySightLog
{
    class FlySight : LogEntry
    {
        public const int kIID = 0x0B716E1B;

        private DateTime mDate;
        private List<Record> mRecords;

        public FlySight(DateTime aDate, int aCapacity)
        {
            mDate = aDate;
            mRecords = new List<Record>(aCapacity);
        }

        public int ID
        {
            get { return kIID; }
        }

        public DateTime DateTime
        {
            get { return mDate; }
        }

        public IList<Record> Records
        {
            get { return mRecords; }
        }

        public void Write(BinaryWriter aWriter)
        {
            aWriter.Write((byte)1);
            aWriter.Write(mDate.ToBinary());
            aWriter.Write(mRecords.Count);
            foreach (Record rec in mRecords)
            {
                aWriter.Write(rec.Time.ToBinary());
                aWriter.Write(rec.Location.Lat);
                aWriter.Write(rec.Location.Lng);
                aWriter.Write(rec.Altitude);
                aWriter.Write(rec.VelocityNorth);
                aWriter.Write(rec.VelocityEast);
                aWriter.Write(rec.VelocityDown);
            }
        }

        public static FlySight Read(BinaryReader aReader)
        {
            int version = aReader.ReadByte();
            DateTime date = DateTime.FromBinary(aReader.ReadInt64());
            int count = aReader.ReadInt32();
            FlySight result = new FlySight(date, count);

            for (int i = 0; i < count; ++i)
            {
                Record rec = new Record();
                rec.Time = DateTime.FromBinary(aReader.ReadInt64());
                rec.Location = new PointLatLng(aReader.ReadDouble(), aReader.ReadDouble());
                rec.Altitude = aReader.ReadSingle();
                rec.VelocityNorth = aReader.ReadSingle();
                rec.VelocityEast = aReader.ReadSingle();
                rec.VelocityDown = aReader.ReadSingle();
                result.mRecords.Add(rec);
            }

            return result;
        }

        #region -- Static helpers ---------------------------------------------

        public static FlySight Import(string aPath)
        {
            string[] lines = File.ReadAllLines(aPath);
            if (lines.Length > 2)
            {
                Record rec = ParseRecord(lines[2]);
                FlySight entry = new FlySight(rec.Time, lines.Length);
                entry.mRecords.Add(rec);

                for (int i = 3; i < lines.Length; i++)
                {
                    entry.mRecords.Add(ParseRecord(lines[i]));
                }

                return entry;
            }

            return null;
        }

        private static Record ParseRecord(string aLine)
        {
            string[] fields = aLine.Split(',');

            Record result = new Record();
            result.Time = GetDateTime(fields[0]);
            result.Location = new PointLatLng(double.Parse(fields[1]), double.Parse(fields[2]));
            result.Altitude = float.Parse(fields[3]);
            result.VelocityNorth = float.Parse(fields[4]);
            result.VelocityEast = float.Parse(fields[5]);
            result.VelocityDown = float.Parse(fields[6]);
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
