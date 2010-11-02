using System;
using Brejc.Geometry;

namespace Brejc.GpsLibrary.Gpx
{
    [Serializable]
    public class GpsTrackStatistics
    {
        public double MaxElevation
        {
            get { return maxElevation; }
            set { maxElevation = value; }
        }

        public double MinElevation
        {
            get { return minElevation; }
            set { minElevation = value; }
        }

        public double ElevationDifference
        {
            get { return MaxElevation - MinElevation; }
        }


        public double TotalDescent
        {
            get { return totalDescent; }
            set { totalDescent = value; }
        }

        public double TotalAscent
        {
            get { return totalAscent; }
            set { totalAscent = value; }
        }

        public Bounds2 Bounds
        {
            get { return bounds; }
            set { bounds = value; }
        }

        public DateTime? EndTime
        {
            get { return endTime; }
            set { endTime = value; }
        }

        public DateTime? StartTime
        {
            get { return startTime; }
            set { startTime = value; }
        }

        public TimeSpan? Duration 
        { 
            get 
            { 
                if (StartTime.HasValue && EndTime.HasValue)
                    return EndTime - StartTime;
                return null;
            } 
        }

        public double SlopeDistance
        {
            get { return slopeDistance; }
            set { slopeDistance = value; }
        }

        public double HorizontalDistance
        {
            get { return horizontalDistance; }
            set { horizontalDistance = value; }
        }

        public void Union (GpsTrackStatistics tsStat)
        {
            if (tsStat == null)
                throw new ArgumentNullException ("tsStat");

            MinElevation = Math.Min (this.MinElevation, tsStat.MinElevation);
            MaxElevation = Math.Max (this.MaxElevation, tsStat.MaxElevation);
            Bounds = Bounds.Union (tsStat.Bounds);
            TotalAscent += tsStat.TotalAscent;
            TotalDescent += tsStat.TotalDescent;
            
            if (startTime.HasValue && tsStat.startTime.HasValue)
                StartTime = StartTime <= tsStat.StartTime ? StartTime : tsStat.StartTime;
            else if (startTime.HasValue)
                StartTime = startTime;
            else if (tsStat.startTime.HasValue)
                StartTime = tsStat.StartTime;

            if (endTime.HasValue && tsStat.endTime.HasValue)
                EndTime = EndTime >= tsStat.EndTime ? EndTime : tsStat.EndTime;
            else if (endTime.HasValue)
                EndTime = endTime;
            else if (tsStat.endTime.HasValue)
                EndTime = tsStat.EndTime;

            HorizontalDistance = HorizontalDistance + tsStat.HorizontalDistance;
            SlopeDistance = SlopeDistance + tsStat.SlopeDistance;
        }

        private Bounds2 bounds = new Bounds2();
        private double minElevation = double.MaxValue, maxElevation = double.MinValue;
        private double totalAscent, totalDescent;
        private DateTime? startTime, endTime;
        private double horizontalDistance, slopeDistance;
    }
}
