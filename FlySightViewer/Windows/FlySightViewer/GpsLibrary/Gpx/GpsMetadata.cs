using System;
using Brejc.Geometry;

namespace Brejc.GpsLibrary.Gpx
{
    /// <summary>
    /// Information about the GPX file, author, and copyright restrictions goes in the metadata section. 
    /// Providing rich, meaningful information about your GPX files allows others to search for and use your GPS data. 
    /// </summary>
    [Serializable]
    public class GpsMetadata : ICloneable
    {
        /// <summary>
        /// The name of the GPX file.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// A description of the contents of the GPX file.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        /// <summary>
        /// The person or organization who created the GPX file.
        /// </summary>
        public GpsPersonMetadata Author
        {
            get { return author; }
            set { author = value; }
        }

        /// <summary>
        /// Copyright and license information governing use of the file.
        /// </summary>
        public GpsCopyrightMetadata Copyright
        {
            get { return copyright; }
            set { copyright = value; }
        }

        /// <summary>
        /// URLs associated with the location described in the file.
        /// </summary>
        public Link Link
        {
            get { return link; }
            set { link = value; }
        }

        /// <summary>
        /// The creation date of the file.
        /// </summary>
        public DateTime? Time
        {
            get { return time; }
            set { time = value; }
        }

        /// <summary>
        /// Keywords associated with the file. Search engines or databases can use this information to classify the data.
        /// </summary>
        public string Keywords
        {
            get { return keywords; }
            set { keywords = value; }
        }

        /// <summary>
        /// Minimum and maximum coordinates which describe the extent of the coordinates in the file.
        /// </summary>
        public Bounds3 Bounds
        {
            get { return bounds; }
            set { bounds = value; }
        }

        public object Clone ()
        {
            GpsMetadata clone = new GpsMetadata();

            if (name != null)
                clone.name = (string)name.Clone();
            if (description != null)
                clone.description = (string)description.Clone ();

            if (author != null)
                clone.author = (GpsPersonMetadata)author.Clone ();
            if (copyright != null)
                clone.copyright = (GpsCopyrightMetadata)copyright.Clone ();
            if (link != null)
                clone.link = (Link)link.Clone ();
            clone.time = time;
            if (keywords != null)
                clone.keywords = (string)keywords.Clone ();
            if (bounds != null)
                clone.bounds = (Bounds3)bounds.Clone ();

            return clone;
        }

        private string name;
        private string description;
        private GpsPersonMetadata author;
        private GpsCopyrightMetadata copyright;
        private Link link;
        private DateTime? time;
        private string keywords;
        private Bounds3 bounds;
    }
}
