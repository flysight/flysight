using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Brejc.GpsLibrary.Gpx
{
    [SuppressMessage ("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class TrackSegment : IEnumerable<TrackPoint>
    {
        public TrackSegment()
        {
        }

        public TrackSegment (IEnumerable<TrackPoint> points)
        {
            this.points.AddRange (points);
        }

        public IList<TrackPoint> Points
        {
            get { return points; }
        }

        public int PointsCount { get { return points.Count; } }

        public DateTime? StartTime
        {
            get
            {
                DateTime? startTime = null;
                foreach (TrackPoint tp in this.Points)
                {
                    if (tp.Time.HasValue)
                    {
                        if (startTime.HasValue == false || tp.Time < startTime)
                            startTime = tp.Time.Value;
                    }
                }

                return startTime;
            }
        }

        public DateTime? EndTime
        {
            get
            {
                if (points.Count > 0)
                    return points[points.Count - 1].Time;

                return null;
            }
        }

        public void AddPoint (TrackPoint point)
        {
            points.Add (point);
        }

        public IEnumerator<TrackPoint> GetEnumerator ()
        {
            return this.points.GetEnumerator ();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            return this.points.GetEnumerator ();
        }

        private List<TrackPoint> points = new List<TrackPoint> ();
    }
}