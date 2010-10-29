using System;
using System.Collections.Generic;
using System.Text;
using GMap.NET;
using System.IO;
using System.ComponentModel;

namespace FlySightLog
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

    public interface LogEntry
    {
        int ID { get; }
        DateTime DateTime { get; }
        IList<Record> Records { get; }

        void Write(BinaryWriter aWriter);
    }
}
