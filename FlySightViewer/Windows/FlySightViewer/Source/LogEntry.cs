using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using GMap.NET;

namespace FlySightViewer
{
    public struct Record
    {
        private DateTime mTime;
        private PointLatLng mLocation;
        private float mAltitude;
        private float mVelocityNorth;
        private float mVelocityEast;
        private float mVelocityDown;

        [ReadOnly(true)]
        public DateTime Time
        {
            get { return mTime; }
            set { mTime = value; }
        }

        [ReadOnly(true)]
        public PointLatLng Location
        {
            get { return mLocation; }
            set { mLocation = value; }
        }

        [ReadOnly(true)]
        public float Altitude
        {
            get { return mAltitude; }
            set { mAltitude = value; }
        }

        [ReadOnly(true)]
        public float VelocityNorth
        {
            get { return mVelocityNorth; }
            set { mVelocityNorth = value; }
        }

        [ReadOnly(true)]
        public float VelocityEast
        {
            get { return mVelocityEast; }
            set { mVelocityEast = value; }
        }

        [ReadOnly(true)]
        public float VelocityDown
        {
            get { return mVelocityDown; }
            set { mVelocityDown = value; }
        }
    }

    public class LogEntry
    {
        private string mKey;
        private DateTime mDate;
        private List<Record> mRecords;

        public LogEntry(string aKey, DateTime aDate, int aCapacity)
        {
            mKey = aKey;
            mDate = aDate;
            mRecords = new List<Record>(aCapacity);
        }

        public string Key
        {
            get { return mKey; }
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

        public static LogEntry Read(string aKey, BinaryReader aReader)
        {
            int version = aReader.ReadByte();
            DateTime date = DateTime.FromBinary(aReader.ReadInt64());
            int count = aReader.ReadInt32();
            LogEntry result = new LogEntry(aKey, date, count);

            for (int i = 0; i < count; ++i)
            {
                Record rec = new Record();
                rec.Time = DateTime.FromBinary(aReader.ReadInt64());
                rec.Location = new PointLatLng(aReader.ReadDouble(), aReader.ReadDouble());
                rec.Altitude = aReader.ReadSingle();
                rec.VelocityNorth = aReader.ReadSingle();
                rec.VelocityEast = aReader.ReadSingle();
                rec.VelocityDown = aReader.ReadSingle();
                result.Records.Add(rec);
            }

            return result;
        }
    }
}
