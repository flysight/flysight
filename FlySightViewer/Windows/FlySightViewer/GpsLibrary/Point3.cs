using System;
using System.Diagnostics.CodeAnalysis;

namespace Brejc.Geometry
{
    [Serializable]
    public struct Point3<T> : ICloneable
    {
        public T Y
        {
            get { return y; }
            set { y = value; }
        }

        public T X
        {
            get { return x; }
            set { x = value; }
        }

        public T Z
        {
          get { return z; }
          set { z = value; }
        }

        public Point3 (T x, T y, T z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        //public bool IsInsideTriangle (Point3 p0, Point3 p1, Point3 p2)
        //{
        //    double fAB = (Y - p0.Y) * (p1.X - p0.X) - (X - p0.X) * (p1.Y - p0.Y);
        //    double fBC = (Y - p1.Y) * (p2.X - p1.X) - (X - p1.X) * (p2.Y - p1.Y);
        //    double fCA = (Y - p2.Y) * (p0.X - p2.X) - (X - p2.X) * (p0.Y - p2.Y);

        //    return fAB * fBC > 0 && fBC * fCA > 0;
        //}

        #region ICloneable Members

        public object Clone ()
        {
            Point3<T> clone = new Point3<T> ();

            clone.x = x;
            clone.y = y;
            clone.z = z;

            return clone;
        }

        #endregion

        public Point2<T> ToPoint2()
        {
            return new Point2<T>(x, y);
        }

        public override string ToString ()
        {
            return String.Format (System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2}", x, y, z);
        }

        /// <summary>
        /// Compares the current <see cref="Point3"/> object to the specified object for equivalence.
        /// </summary>
        /// <param name="obj">The <see cref="Point3"/> object to test for equivalence with the current object.</param>
        /// <returns>
        /// <c>true</c> if the two <see cref="Point3"/> objects are equal; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals (object obj)
        {
            if (obj == null)
                return false;

            Point3<T> that = (Point3<T>)obj;

            return x.Equals (that.x) && y.Equals (that.y) && z.Equals (that.z);
        }

        /// <summary>
        /// Returns the hash code for this <see cref="Point3"/> object.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code.
        /// </returns>
        public override int GetHashCode ()
        {
            return x.GetHashCode () ^ y.GetHashCode () ^ z.GetHashCode ();
        }

        //static public Point3 operator * (Point3 point, float factor)
        //{
        //    return new Point3 (point.X * factor, point.Y * factor, point.Z * factor);
        //}

        //static public Point3 operator + (Point3 pointA, Point3 pointB)
        //{
        //    return new Point3 (pointA.X + pointB.X, pointA.Y + pointB.Y, pointA.Z + pointB.Z);
        //}

        //static public Point3 operator - (Point3 pointA, Point3 pointB)
        //{
        //    return new Point3 (pointA.X - pointB.X, pointA.Y - pointB.Y, pointA.Z - pointB.Z);
        //}

        //static public Point3 Multiply (Point3 point, float factor)
        //{
        //    return new Point3 (point.X * factor, point.Y * factor, point.Z * factor);
        //}

        //static public Point3 Add (Point3 pointA, Point3 pointB)
        //{
        //    return new Point3 (pointA.X + pointB.X, pointA.Y + pointB.Y, pointA.Z + pointB.Z);
        //}

        //static public Point3 Subtract (Point3 pointA, Point3 pointB)
        //{
        //    return new Point3 (pointA.X - pointB.X, pointA.Y - pointB.Y, pointA.Z - pointB.Z);
        //}

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="a">The first object.</param>
        /// <param name="b">The second object.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator == (Point3<T> a, Point3<T> b)
        {
            return a.Equals (b);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="a">The first object.</param>
        /// <param name="b">The second object.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator != (Point3<T> a, Point3<T> b)
        {
            return !a.Equals (b);
        }

        [SuppressMessage ("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible")]
        [SuppressMessage ("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes")]
        public static Point3<T> Empty = new Point3<T> ();

        private T x;
        private T y;
        private T z;
    }
}
