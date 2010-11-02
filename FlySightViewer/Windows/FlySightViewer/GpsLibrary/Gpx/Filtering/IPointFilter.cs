using System.Collections.Generic;

namespace Brejc.GpsLibrary.Gpx.Filtering
{
    public interface IPointFilter
    {
        IList<TrackPoint> ApplyFilter (IEnumerable<TrackPoint> points);
    }
}