using System;
using System.Collections;
using Brejc.Common;
using Brejc.Geometry;

namespace Brejc.GpsLibrary.Gpx
{
    /// <summary>
    /// Track point.
    /// </summary>
    [Serializable]
    public class TrackPoint
    {
        /// <summary>
        /// Creation/modification timestamp for element. Date and time in are in Univeral Coordinated Time (UTC), 
        /// not local time! Conforms to ISO 8601 specification for date/time representation. 
        /// Fractional seconds are allowed for millisecond timing in tracklogs.
        /// </summary>
        public DateTime? Time
        {
            get { return time; }
            set { time = value; }
        }

        /// <summary>
        /// Location of the point.
        /// </summary>
        /// <value>The location.</value>
        public Point2<double> Location
        {
            get { return location; }
            set { location = value; }
        }

        public double? Elevation
        {
            get { return elevation; }
            set { elevation = value; }
        }

        /// <summary>
        /// Magnetic variation (in degrees) at the point.
        /// </summary>
        public decimal? MagneticVariation
        {
            get { return magneticVariation; }
            set { magneticVariation = value; }
        }

        /// <summary>
        /// Height (in meters) of geoid (mean sea level) above WGS84 earth ellipsoid. As defined in NMEA GGA message.
        /// </summary>
        public decimal? GeoidHeight
        {
            get { return geoidHeight; }
            set { geoidHeight = value; }
        }

        /// <summary>
        /// The GPS name of the waypoint. This field will be transferred to and from the GPS. 
        /// GPX does not place restrictions on the length of this field or the characters contained in it. 
        /// It is up to the receiving application to validate the field before sending it to the GPS.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// GPS waypoint comment. Sent to GPS as comment.
        /// </summary>
        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }

        /// <summary>
        /// A text description of the element. Holds additional information about the element intended for the user, not the GPS.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
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
        /// Link to additional information about the waypoint.
        /// </summary>
        public Link Link
        {
            get { return link; }
            set { link = value; }
        }

        /// <summary>
        /// Text of GPS symbol name. For interchange with other programs, 
        /// use the exact spelling of the symbol as displayed on the GPS. 
        /// If the GPS abbreviates words, spell them out.
        /// </summary>
        public string Symbol
        {
            get { return symbol; }
            set { symbol = value; }
        }

        /// <summary>
        /// Type (classification) of the waypoint.
        /// </summary>
        public string PointType
        {
            get { return pointType; }
            set { pointType = value; }
        }

        /// <summary>
        /// Type of GPX fix.
        /// </summary>
        public string FixType
        {
            get { return fixType; }
            set { fixType = value; }
        }

        /// <summary>
        /// Number of satellites used to calculate the GPX fix.
        /// </summary>
        public int? NumberOfSatellites
        {
            get { return numberOfSatellites; }
            set { numberOfSatellites = value; }
        }

        /// <summary>
        /// Horizontal dilution of precision.
        /// </summary>
        public decimal? HorizontalDilutionOfPrecision
        {
            get { return horizontalDilutionOfPrecision; }
            set { horizontalDilutionOfPrecision = value; }
        }

        /// <summary>
        /// Vertical dilution of precision.
        /// </summary>
        public decimal? VerticalDilutionOfPrecision
        {
            get { return verticalDilutionOfPrecision; }
            set { verticalDilutionOfPrecision = value; }
        }

        /// <summary>
        /// Position dilution of precision.
        /// </summary>
        public decimal? PositionDilutionOfPrecision
        {
            get { return positionDilutionOfPrecision; }
            set { positionDilutionOfPrecision = value; }
        }

        /// <summary>
        /// Number of seconds since last DGPS update.
        /// </summary>
        public decimal? AgeOfGpsData
        {
            get { return ageOfGpsData; }
            set { ageOfGpsData = value; }
        }

        /// <summary>
        /// ID of DGPS station used in differential correction.
        /// </summary>
        public string DgpsId
        {
            get { return dgpsId; }
            set { dgpsId = value; }
        }

        /// <summary>
        /// Compares the current <see cref="TrackPoint"/> object to the specified object for equivalence.
        /// </summary>
        /// <param name="obj">The <see cref="TrackPoint"/> object to test for equivalence with the current object.</param>
        /// <returns>
        /// <c>true</c> if the two <see cref="TrackPoint"/> objects are equal; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals (object obj)
        {
            if (obj == null)
                return false;

            TrackPoint that = obj as TrackPoint;

            if (that == null)
                return false;

            return 
                time.Equals (that.time)
                && location.Equals (that.location)
                && magneticVariation.Equals (that.magneticVariation)
                && geoidHeight.Equals (that.geoidHeight)
                && numberOfSatellites.Equals (that.numberOfSatellites)
                && horizontalDilutionOfPrecision.Equals (that.horizontalDilutionOfPrecision)
                && verticalDilutionOfPrecision.Equals (that.verticalDilutionOfPrecision)
                && positionDilutionOfPrecision.Equals (that.positionDilutionOfPrecision)
                && ageOfGpsData.Equals (that.ageOfGpsData)
                && dgpsId.Equals (that.dgpsId)
                && Comparer.Equals (name, that.name)
                && Comparer.Equals (comment, that.comment)
                && Comparer.Equals (description, that.description)
                && Comparer.Equals (sourceOfData, that.sourceOfData)
                && Comparer.Equals (link, that.link)
                && Comparer.Equals (symbol, that.symbol)
                && Comparer.Equals (fixType, that.fixType);
        }

        /// <summary>
        /// Returns the hash code for this <see cref="TrackPoint"/> object.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code.
        /// </returns>
        public override int GetHashCode ()
        {
            return this.Location.GetHashCode ();
        }

        public override string ToString ()
        {
            return ObjectHelper.ObjectToString (this);
        }

        #region ICloneable Members

        public virtual object Clone ()
        {
            TrackPoint clone = new TrackPoint ();

            CopyToClone (clone);

            return clone;
        }

        #endregion

        public virtual void CopyToClone (TrackPoint clone)
        {
            clone.time = time;
            clone.location = (Point2<double>)location.Clone ();
            clone.Elevation = Elevation;
            clone.magneticVariation = magneticVariation;
            clone.geoidHeight = geoidHeight;
            if (name != null)
                clone.name = (string)name.Clone ();
            if (comment != null)
                clone.comment = (string)comment.Clone ();
            if (description != null)
                clone.description = (string)description.Clone ();
            if (sourceOfData != null)
                clone.sourceOfData = (string)sourceOfData.Clone ();
            if (link != null)
                clone.link = (Link)link.Clone ();
            if (symbol != null)
                clone.symbol = (string)symbol.Clone ();
            if (pointType != null)
                clone.pointType = (string)pointType.Clone ();
            if (fixType != null)
                clone.fixType = (string)fixType.Clone ();
            clone.numberOfSatellites = numberOfSatellites;
            clone.horizontalDilutionOfPrecision = horizontalDilutionOfPrecision;
            clone.verticalDilutionOfPrecision = verticalDilutionOfPrecision;
            clone.positionDilutionOfPrecision = positionDilutionOfPrecision;
            clone.ageOfGpsData = ageOfGpsData;
            clone.dgpsId = dgpsId;
        }

        private DateTime? time;
        private Point2<double> location;
        private double? elevation;
        private decimal? magneticVariation;
        private decimal? geoidHeight;

        private string name;
        private string comment;
        private string description;
        private string sourceOfData;
        private Link link;
        private string symbol;
        private string pointType;

        private string fixType;
        private int? numberOfSatellites;
        private decimal? horizontalDilutionOfPrecision;
        private decimal? verticalDilutionOfPrecision;
        private decimal? positionDilutionOfPrecision;
        private decimal? ageOfGpsData;
        private string dgpsId;
    }
}
