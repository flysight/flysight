using System;
using Brejc.GpsLibrary.Gpx;

namespace Brejc.GpsLibrary
{
    public sealed class GpsUtilities
    {
        // TODO: take into account the elevation of points
        /// <summary>
        /// Calculates the horizontal distance between two points.
        /// </summary>
        /// <param name="p1">First point.</param>
        /// <param name="p2">Second point.</param>
        /// <returns>Horizontal distance (in meters).</returns>
        static public double CalculateHorizontalDistance (TrackPoint point1, TrackPoint point2)
        {
            double R = 6371000; // earth's mean radius in km
            double dLat = (point2.Location.Y - point1.Location.Y) * Math.PI / 180d;
            double dLong = (point2.Location.X - point1.Location.X) * Math.PI / 180d;

            double a = Math.Sin (dLat / 2) * Math.Sin (dLat / 2)
                    + Math.Cos (point1.Location.Y * Math.PI / 180d) * Math.Cos (point2.Location.Y * Math.PI / 180d)
                    * Math.Sin (dLong / 2) * Math.Sin (dLong / 2);
            double c = 2 * Math.Atan2 (Math.Sqrt (a), Math.Sqrt (1 - a));
            double d = R * c;

            return d;
        }

        /// <summary>
        /// Calculates statistics for a <see cref="IGpsTrack"/> object.
        /// </summary>
        /// <param name="gpsTrack"><see cref="IGpsTrack"/> object to calculate statistics for.</param>
        /// <returns>GpsTrackStatistics object containing the statistics data.</returns>
        static public GpsTrackStatistics CalculateTrackStatistics (Track track)
        {
            GpsTrackStatistics stat = new GpsTrackStatistics ();

            TrackPoint lastPoint = null;
            TrackPoint lastPointWithElevation = null;

            foreach (TrackSegment segment in track.Segments)
            {
                foreach (TrackPoint point in segment.Points)
                {
                    stat.Bounds.ExtendToCover(point.Location);
                    if (point.Elevation.HasValue)
                    {
                        stat.MinElevation = Math.Min(stat.MinElevation, point.Elevation.Value);
                        stat.MaxElevation = Math.Max(stat.MaxElevation, point.Elevation.Value);
                    }

                    if (point.Time.HasValue)
                    {
                        if (false == stat.StartTime.HasValue || point.Time < stat.StartTime)
                            stat.StartTime = point.Time.Value;
                        if (false == stat.EndTime.HasValue || point.Time > stat.EndTime)
                            stat.EndTime = point.Time.Value;
                    }

                    if (lastPoint != null)
                    {
                        double horizontalDistance = GpsUtilities.CalculateHorizontalDistance(lastPoint, point);
                        stat.HorizontalDistance += horizontalDistance;
                    }

                    if (lastPointWithElevation != null)
                    {
                        // calculate ascent/descent
                        if (point.Elevation.HasValue)
                        {
                            double elevationDifference = point.Elevation.Value - lastPointWithElevation.Elevation.Value;
                            if (elevationDifference < 0)
                                stat.TotalDescent -= elevationDifference;
                            else
                                stat.TotalAscent += elevationDifference;
                        }
                    }

                    lastPoint = point;

                    if (point.Elevation.HasValue)
                        lastPointWithElevation = point;
                }
            }

            return stat;
        }

        private GpsUtilities() {}
    }
}
