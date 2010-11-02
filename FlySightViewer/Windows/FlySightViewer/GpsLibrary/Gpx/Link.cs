using System;
using System.Collections;
using Brejc.Common;

namespace Brejc.GpsLibrary.Gpx
{
    /// <summary>
    /// A link to an external resource (Web page, digital photo, video clip, etc) with additional information.
    /// </summary>
    [Serializable]
    public class Link :  ICloneable
    {
        /// <summary>
        /// URL of hyperlink.
        /// </summary>
        public string Href
        {
            get { return href; }
            set { href = value; }
        }

        /// <summary>
        /// Text of hyperlink.
        /// </summary>
        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        /// <summary>
        /// Mime type of content.
        /// </summary>
        public string ContentType
        {
            get { return contentType; }
            set { contentType = value; }
        }

        /// <summary>
        /// Compares the current <see cref="Link"/> object to the specified object for equivalence.
        /// </summary>
        /// <param name="obj">The <see cref="Link"/> object to test for equivalence with the current object.</param>
        /// <returns>
        /// <c>true</c> if the two <see cref="Link"/> objects are equal; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals (object obj)
        {
            if (obj == null)
                return false;

            Link that = obj as Link;

            if (that == null)
                return false;

            return Comparer.Equals (href, that.href)
                && Comparer.Equals (text, that.text)
                && Comparer.Equals (contentType, that.contentType);
        }

        /// <summary>
        /// Returns the hash code for this <see cref="Link"/> object.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code.
        /// </returns>
        public override int GetHashCode ()
        {
            int hashCode = 0;
            hashCode = ObjectHelper.XorHashCode (hashCode, this.href);
            hashCode = ObjectHelper.XorHashCode (hashCode, this.text);
            hashCode = ObjectHelper.XorHashCode (hashCode, this.contentType);
            return hashCode;
        }
                
        public override string ToString ()
        {
            return ObjectHelper.ObjectToString (this);
        }

        #region ICloneable Members

        public object Clone ()
        {
            Link clone = new Link ();

            if (href != null)
                clone.href = (string)href.Clone ();
            if (text != null)
                clone.text = (string)text.Clone ();
            if (contentType != null)
                clone.contentType = (string)contentType.Clone ();

            return clone;
        }

        #endregion
                
        private string href;
        private string text;
        private string contentType;
    }
}
