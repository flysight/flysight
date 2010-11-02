using System;
using System.Diagnostics;

namespace Brejc.Geometry
{
    [Serializable]
    public struct Point2<T> : ICloneable
    {
        public T Y
        {
            [DebuggerStepThrough]
            get { return y; }
            [DebuggerStepThrough]
            set { y = value; }
        }

        public T X
        {
            [DebuggerStepThrough]
            get { return x; }
            [DebuggerStepThrough]
            set { x = value; }
        }

        public Point2 (T x, T y)
        {
            this.x = x;
            this.y = y;
        }

        #region ICloneable Members

        public object Clone ()
        {
            Point2<T> clone = new Point2<T> ();

            clone.x = x;
            clone.y = y;

            return clone;
        }

        #endregion

        public override string ToString ()
        {
            return String.Format (System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1}", x, y);
        }

        //// IsLeft(): tests if a point is Left|On|Right of an infinite line.
        ////    Input:  three points P0, P1, and P2
        ////    Return: >0 for P2 left of the line through P0 and P1
        ////            =0 for P2 on the line
        ////            <0 for P2 right of the line
        //static public T IsLeft (Point2<T> point0, Point2<T> point1, Point2<T> point2)
        //{
        //    return ((point1.X - point0.X) * (point2.Y - point0.Y)
        //            - (point2.X - point0.X) * (point1.Y - point0.Y));
        //}

        /// <summary>
        /// Compares the current <see cref="Point2"/> object to the specified object for equivalence.
        /// </summary>
        /// <param name="obj">The <see cref="Point2"/> object to test for equivalence with the current object.</param>
        /// <returns>
        /// <c>true</c> if the two <see cref="Point2"/> objects are equal; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals (object obj)
        {
            if (obj == null)
                return false;

            Point2<T> that = (Point2<T>)obj;

            return x.Equals (that.x) && y.Equals (that.y);
        }

        /// <summary>
        /// Returns the hash code for this <see cref="Point2"/> object.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code.
        /// </returns>
        public override int GetHashCode ()
        {
            return x.GetHashCode () ^ y.GetHashCode ();
        }

        //static public Point2<T> operator + (Point2<T> pointA, Point2<T> pointB)
        //{
        //    return new Point2<T> (pointA.X + pointB.X, pointA.Y + pointB.Y);
        //}

        //static public Point2<T> operator - (Point2<T> pointA, Point2<T> pointB)
        //{
        //    return new Point2<T> (pointA.X - pointB.X, pointA.Y - pointB.Y);
        //}

        //static public Point2<T> Add (Point2<T> pointA, Point2<T> pointB)
        //{
        //    return new Point2<T> (pointA.X + pointB.X, pointA.Y + pointB.Y);
        //}

        //static public Point2<T> Subtract (Point2<T> pointA, Point2<T> pointB)
        //{
        //    return new Point2<T> (pointA.X - pointB.X, pointA.Y - pointB.Y);
        //}

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="a">The first object.</param>
        /// <param name="b">The second object.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator == (Point2<T> a, Point2<T> b)
        {
            return a.Equals (b);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="a">The first object.</param>
        /// <param name="b">The second object.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator != (Point2<T> a, Point2<T> b)
        {
            return !a.Equals (b);
        }

        private T x, y;
    }
}
