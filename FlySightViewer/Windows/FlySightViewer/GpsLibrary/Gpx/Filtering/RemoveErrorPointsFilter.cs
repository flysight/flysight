using System.Collections.Generic;

namespace Brejc.GpsLibrary.Gpx.Filtering
{
    public class RemoveErrorPointsFilter : IFileFilter, IPointFilter
    {
        public IList<TrackPoint> ApplyFilter (IEnumerable<TrackPoint> points)
        {
            List<TrackPoint> filteredPoints = new List<TrackPoint>();

            foreach (TrackPoint point in points)
            {
                if (point.Location.X < -180 
                    || point.Location.X > 180
                    || point.Location.Y < -90
                    || point.Location.Y > 90)
                    continue;

                filteredPoints.Add(point);
            }

            return filteredPoints;
        }

        public GpxFile ApplyFilter(GpxFile input)
        {
            GpxFile output = CreateGpxFileCopy(input);

            foreach (Track track in input.Tracks)
            {
                Track outputTrack = CreateTrackCopy(track);
                foreach (TrackSegment segment in track.Segments)
                {
                    IList<TrackPoint> filteredPoints = ApplyFilter(segment);
                    TrackSegment outputSegment = new TrackSegment(filteredPoints);
                    outputTrack.AddSegment(outputSegment);
                }

                output.AddTrack(outputTrack);
            }

            foreach (Track track in input.Routes)
            {
                Track outputTrack = CreateTrackCopy (track);
                foreach (TrackSegment segment in track.Segments)
                {
                    IList<TrackPoint> filteredPoints = ApplyFilter (segment);
                    TrackSegment outputSegment = new TrackSegment (filteredPoints);
                    outputTrack.AddSegment (outputSegment);
                }

                output.AddRoute(outputTrack);
            }

            IList<TrackPoint> filteredWaypoints = ApplyFilter (input.Waypoints);
            output.AddWaypoints(filteredWaypoints);

            return output;
        }

        protected static GpxFile CreateGpxFileCopy (GpxFile file)
        {
            GpxFile copy = new GpxFile ();
            copy.Creator = file.Creator;
            copy.Metadata = file.Metadata;
            return copy;
        }

        protected static Track CreateTrackCopy(Track track)
        {
            Track copy = new Track();
            copy.Comment = track.Comment;
            copy.Description = track.Description;
            foreach (Link link in track.Links)
                copy.Links.Add(link);
            copy.Name = track.Name;
            copy.SourceOfData = track.SourceOfData;
            copy.TrackNumber = track.TrackNumber;
            copy.TrackType = track.TrackType;
            copy.TrackTypeString = track.TrackTypeString;

            return copy;
        }
    }
}