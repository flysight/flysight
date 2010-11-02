using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Brejc.Common;

namespace Brejc.GpsLibrary.Gpx
{
    public enum TrackType
    {
        RecordedTrack,
        PlannedRoute,
    }

    /// <summary>
    /// GPS track.
    /// </summary>
    [Serializable]
    [SuppressMessage ("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix")]
    public class Track 
    {
        /// <summary>
        /// Track name.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// GPS comment for the track.
        /// </summary>
        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }

        /// <summary>
        /// User description for the track.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        public IList<TrackSegment> Segments
        {
            get { return segments; }
        }

        /// <summary>
        /// Source of data. Included to give user some idea of reliability and accuracy of data. "Garmin eTrex", "USGS quad Boston North", e.g.
        /// </summary>
        public string SourceOfData
        {
            get { return sourceOfData; }
            set { sourceOfData = value; }
        }

        /// <summary>
        /// Number of the track.
        /// </summary>
        public int? TrackNumber
        {
            get { return trackNumber; }
            set { trackNumber = value; }
        }

        /// <summary>
        /// Type (classification) of track.
        /// </summary>
        public string TrackTypeString
        {
            get { return trackTypeString; }
            set { trackTypeString = value; }
        }

        public TrackType TrackType
        {
            get { return trackType; }
            set { trackType = value; }
        }

        /// <summary>
        /// Links to external information about track.
        /// </summary>
        public IList<Link> Links
        {
            get { return links; }
        }

        public int SegmentsCount 
        { 
            get 
            {
                return segments.Count;
            } 
        }

        /// <summary>
        /// Gets a value indicating whether the track is segmented, i.e. it has more than one segment.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the track is segmented; otherwise, <c>false</c>.
        /// </value>
        public bool IsSegmented { get { return SegmentsCount > 1; } }

        public void AddSegment (TrackSegment segment)
        {
            segments.Add(segment);
        }

        /// <summary>
        /// Compares the current <see cref="Track"/> object to the specified object for equivalence.
        /// </summary>
        /// <param name="obj">The <see cref="Track"/> object to test for equivalence with the current object.</param>
        /// <returns>
        /// <c>true</c> if the two <see cref="Track"/> objects are equal; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals (object obj)
        {
            if (obj == null)
                return false;

            Track that = obj as Track;

            if (that == null)
                return false;

            return String.Equals (name, that.name)
                && String.Equals (comment, that.comment)
                && String.Equals (description, that.description)
                && String.Equals (sourceOfData, that.sourceOfData)
                && Comparer.Equals (trackNumber, that.trackNumber)
                && String.Equals (trackTypeString, that.trackTypeString)
                && String.Equals (trackType, that.trackType)
                && ObjectHelper.AreCollectionsEqual (links, that.links)
                && ObjectHelper.AreCollectionsEqual (segments, that.segments);
        }

        /// <summary>
        /// Returns the hash code for this <see cref="Track"/> object.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code.
        /// </returns>
        public override int GetHashCode ()
        {
            if (name != null)
                return name.GetHashCode();
            return base.GetHashCode();
        }
                
        /// <summary>
        /// Returns a <see cref="String"/> that represents the current object.
        /// </summary>
        /// <returns>
        /// A <see cref="String"/> that represents the current object.
        /// </returns>
        public override string ToString ()
        {
            return ObjectHelper.ObjectToString (this);
        }

        private string name;
        private string comment;
        private string description;
        private string sourceOfData;
        private int? trackNumber;
        private string trackTypeString;
        private TrackType trackType;
        private List<Link> links = new List<Link> ();
        private List<TrackSegment> segments = new List<TrackSegment> ();
    }
}
