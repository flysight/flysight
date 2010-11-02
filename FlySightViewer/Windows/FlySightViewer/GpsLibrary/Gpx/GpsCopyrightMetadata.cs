using System;

namespace Brejc.GpsLibrary.Gpx
{
    /// <summary>
    /// Information about the copyright holder and any license governing use of this file. 
    /// By linking to an appropriate license, you may place your data into the public domain or grant additional usage rights. 
    /// </summary>
    [Serializable]
    public class GpsCopyrightMetadata : ICloneable
    {
        /// <summary>
        /// Copyright holder.
        /// </summary>
        public string Author
        {
            get { return author; }
            set { author = value; }
        }

        /// <summary>
        /// Year of copyright.
        /// </summary>
        public int? Year
        {
            get { return year; }
            set { year = value; }
        }

        /// <summary>
        /// Link to external file containing license text.
        /// </summary>
        public Uri License
        {
            get { return license; }
            set { license = value; }
        }

        public object Clone ()
        {
            GpsCopyrightMetadata clone = new GpsCopyrightMetadata ();
            if (author != null)
                clone.author = (string)author;
            clone.year = year;
            if (license != null)
                clone.license = new Uri ((string)license.ToString ());

            return clone;
        }

        private string author;
        private int? year;
        private Uri license;
    }
}
