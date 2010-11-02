using System;

namespace Brejc.GpsLibrary.Gpx
{
    /// <summary>
    /// A person or organization.
    /// </summary>
    [Serializable]
    public class GpsPersonMetadata : ICloneable
    {
        /// <summary>
        /// Name of person or organization.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Email address.
        /// </summary>
        public Uri Email
        {
            get { return email; }
            set { email = value; }
        }

        /// <summary>
        /// Link to Web site or other external information about person.
        /// </summary>
        public Link Link
        {
            get { return link; }
            set { link = value; }
        }

        public object Clone ()
        {
            GpsPersonMetadata clone = new GpsPersonMetadata();
            if (name != null)
                clone.name = (string) name;
            if (email != null)
                clone.email = new Uri((string) email.ToString());
            if (link != null)
                clone.link = (Link) link.Clone();

            return clone;
        }

        private string name;
        private Uri email;
        private Link link;
    }

}
