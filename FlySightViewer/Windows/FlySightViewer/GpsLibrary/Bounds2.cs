using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Brejc.Geometry
{
    [Serializable]
    public class Bounds2 : ICloneable
    {
        public double MaxY
        {
            [DebuggerStepThrough]
            get { return maxY; }
            [DebuggerStepThrough]
            set { maxY = value; }
        }

        public double MinY
        {
            [DebuggerStepThrough]
            get { return minY; }
            [DebuggerStepThrough]
            set { minY = value; }
        }

        public double MaxX
        {
            [DebuggerStepThrough]
            get { return maxX; }
            [DebuggerStepThrough]
            set { maxX = value; }
        }

        public double MinX
        {
            [DebuggerStepThrough]
            get { return minX; }
            [DebuggerStepThrough]
            set { minX = value; }
        }

        public Point2<double> Center
        {
            [DebuggerStepThrough]
            get
            {
                return new Point2<double>((minX + maxX) / 2, (minY + maxY) / 2);
            }
        }

        public double DeltaX
        {
            [DebuggerStepThrough]
            get { return maxX - minX; }
            [DebuggerStepThrough]
            set { maxX = minX + value; }
        }

        public double DeltaY
        {
            [DebuggerStepThrough]
            get { return maxY - minY; }
            [DebuggerStepThrough]
            set { maxY = minY + value; }
        }

        public Point2<double> MinPoint
        {
            [DebuggerStepThrough]
            get { return new Point2<double> (minX, minY); }
        }

        public Point2<double> MaxPoint
        {
            [DebuggerStepThrough]
            get { return new Point2<double> (maxX, maxY); }
        }

        [DebuggerStepThrough]
        public Bounds2 () { }

        [DebuggerStepThrough]
        public Bounds2 (double minX, double minY, double maxX, double maxY)
        {
            this.minX = minX;
            this.minY = minY;
            this.maxX = maxX;
            this.maxY = maxY;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bounds2"/> class using
        /// an array of points which all have to be included in the boundaries.
        /// </summary>
        /// <param name="pointsToCover">Points which this instance should cover.</param>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public Bounds2(IEnumerable<Point2<double>> pointsToCover)
        {
            foreach (Point2<double> point in pointsToCover)
                ExtendToCover (point);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bounds2"/> class so that it
        /// has a specified point in its center and has width and height equal to the speficied <c>size</c> parameter.
        /// </summary>
        /// <param name="centerPoint">The center point of the new boundary.</param>
        /// <param name="size">The width and the height of the boundary.</param>
        public Bounds2 (Point2<double> centerPoint, double size)
        {
            double halfSize = size / 2;
            minX = centerPoint.X - halfSize;
            minY = centerPoint.Y - halfSize;
            maxX = centerPoint.X + halfSize;
            maxY = centerPoint.Y + halfSize;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bounds2"/> class with the two
        /// points specifying its extent.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        public Bounds2 (Point2<double> point1, Point2<double> point2)
        {
            ExtendToCover (point1);
            ExtendToCover (point2);
        }

        public Bounds2(Bounds2 original)
        {
            this.minX = original.minX;
            this.minY = original.minY;
            this.maxX = original.maxX;
            this.maxY = original.maxY;
        }

        public void Normalize ()
        {
            double swap;

            if (minX > maxX)
            {
                swap = minX;
                minX = maxX;
                maxX = swap;
            }

            if (minY > maxY)
            {
                swap = minY;
                minY = maxY;
                maxY = swap;
            }
        }

        public void ExtendToCover (Point2<double> point)
        {
            ExtendToCover (point.X, point.Y);
        }

        public void ExtendToCover (double x, double y)
        {
            if (x.CompareTo (minX) < 0)
                minX = x;
            if (x.CompareTo (maxX) > 0)
                maxX = x;
            if (y.CompareTo (minY) < 0)
                minY = y;
            if (y.CompareTo (maxY) > 0)
                maxY = y;
        }

        /// <summary>
        /// Returns the intersection of two <see cref="Bounds2"/> objects.
        /// </summary>
        /// <param name="with"><see cref="Bounds2"/> object to intersect with.</param>
        /// <returns>Intersection of two <see cref="Bounds2"/> objects.</returns>
        public Bounds2 Intersect (Bounds2 with)
        {
            if (with == null)
                throw new ArgumentNullException ("with");

            return new Bounds2 (Math.Max (MinX, with.MinX), Math.Max (MinY, with.MinY),
                Math.Min (MaxX, with.MaxX), Math.Min (MaxY, with.MaxY));
        }

        /// <summary>
        /// Checks if the object intersects with specified boundaries.
        /// </summary>
        /// <returns><c>True</c> if it intersects, <c>false</c> otherwise.</returns>
        public bool IntersectsWith (double minX, double minY, double maxX, double maxY)
        {
            return !(MinY > maxY || MaxY < minY || MaxX < minX || MinX > maxX);
        }

        /// <summary>
        /// Checks if the object intersects with specified boundaries.
        /// </summary>
        /// <param name="other">The other boundaries.</param>
        /// <returns>
        /// 	<c>True</c> if it intersects, <c>false</c> otherwise.
        /// </returns>
        public bool IntersectsWith (Bounds2 other)
        {
            return IntersectsWith (other.minX, other.minY, other.maxX, other.maxY);
        }

        /// <summary>
        /// Checks if the specified geographic point is inside bounds.
        /// </summary>
        /// <param name="lon">Longitude of the point.</param>
        /// <param name="lat">Latitude of the point.</param>
        /// <returns><c>True</c> if the point is inside, <c>false</c> otherwise.</returns>
        public bool IsInside (double x, double y)
        {
            return MinX <= x && x <= MaxX && MinY <= y && y <= MaxY;
        }

        /// <summary>
        /// Checks if the specified geographic point is inside bounds.
        /// </summary>
        /// <param name="geoPosition">Geographic position of the point.</param>
        /// <returns><c>True</c> if the point is inside, <c>false</c> otherwise.</returns>
        public bool IsInside (Point2<double> point)
        {
            return MinX <= point.X && point.X <= MaxX
                && MinY <= point.Y && point.Y <= MaxY;
        }

        /// <summary>
        /// Returns the union of two <see cref="Bounds2"/> objects.
        /// </summary>
        /// <param name="with"><see cref="Bounds2"/> object to unite with.</param>
        /// <returns>Union of two <see cref="Bounds2"/> objects.</returns>
        public Bounds2 Union (Bounds2 with)
        {
            if (with == null)
                throw new ArgumentNullException ("with");

            return new Bounds2 (Math.Min (MinX, with.MinX), Math.Min (MinY, with.MinY),
                Math.Max (MaxX, with.MaxX), Math.Max (MaxY, with.MaxY));
        }

        /// <summary>
        /// Inflates the bounds by a factor.
        /// </summary>
        /// <param name="factor"><see cref="Double"/> value of inflation.
        /// <list type="table">
        ///     <item>
        ///         <term>factor > 1</term>
        ///         <description>bounds are inflated</description>
        ///     </item>
        ///     <item>
        ///         <term>0 <= factor < 1</term>
        ///         <description>bounds are deflated</description>
        ///     </item>
        ///     <item>
        ///         <term>factor == 1</term>
        ///         <description>bounds remain the same size</description>
        ///     </item>
        ///     <item>
        ///         <term>factor < 0</term>
        ///         <description><see cref="ArgumentOutOfRangeException"/> is thrown.</description>
        ///     </item>
        /// </list>
        /// </param>
        /// <returns>Inflated <see cref="Bounds2"/> object.</returns>
        public Bounds2 InflateBy (double factor)
        {
            if (factor < 0)
                throw new ArgumentOutOfRangeException ("factor", factor, "Factor cannot be a negative value.");

            double yFactor = DeltaY * (factor - 1) / 2;
            double xFactor = DeltaX * (factor - 1) / 2;

            return new Bounds2 (MinX - xFactor, MinY - yFactor,
                MaxX + xFactor, MaxY + yFactor);
        }

        public Bounds2 Inflate (double dx, double dy)
        {
            return new Bounds2 (MinX - dx, MinY - dy,
                MaxX + dx, MaxY + dy);
        }

        public object Clone ()
        {
            return new Bounds2 (minX, minY, maxX, maxY);
        }

        public BoundsLineCrossing CrossLine (Point2<double> point1, Point2<double> point2)
        {
            BoundsLineCrossing crossing = new BoundsLineCrossing (point1, point2);

            CohenSutherland2DClipper.Clip (crossing, this);

            return crossing;
        }

        /// <summary>
        /// Compares the current <see cref="Bounds2"/> object to the specified object for equivalence.
        /// </summary>
        /// <param name="obj">The <see cref="Bounds2"/> object to test for equivalence with the current object.</param>
        /// <returns>
        /// <c>true</c> if the two <see cref="Bounds2"/> objects are equal; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals (object obj)
        {
            if (obj == null)
                return false;

            Bounds2 that = obj as Bounds2;

            if (that == null)
                return false;

            return minX.Equals (that.minX) && minY.Equals (that.minY) && maxX.Equals (that.maxX) && maxY.Equals (that.MaxY);
        }

        /// <summary>
        /// Returns the hash code for this <see cref="Bounds2"/> object.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code.
        /// </returns>
        public override int GetHashCode ()
        {
            return minX.GetHashCode () ^ minY.GetHashCode () ^ maxX.GetHashCode () ^ maxY.GetHashCode ();
        }

        public static Bounds2 Parse (string value)
        {
            if (value == null)
                throw new ArgumentNullException ("value");

            string[] splits = value.Split (',');
            if (splits.Length != 4)
                throw new ArgumentException ("Too many or too few parameters.");

            return new Bounds2 (
                double.Parse (splits[0], System.Globalization.CultureInfo.InvariantCulture),
                double.Parse (splits[1], System.Globalization.CultureInfo.InvariantCulture),
                double.Parse (splits[2], System.Globalization.CultureInfo.InvariantCulture),
                double.Parse (splits[3], System.Globalization.CultureInfo.InvariantCulture)
                );
        }

        public override string ToString ()
        {
            return String.Format (System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2},{3}", minX, minY, maxX, maxY);
        }

        private double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
    }

    public enum BoundsLineCrossingType
    {
        None,
        CrossesMinY,
        CrossesMaxX,
        CrossesMaxY,
        CrossesMinX,
    }

    public class BoundsLineCrossing
    {
        public Point2<double> P0
        {
            get { return p0; }
            set { p0 = value; }
        }

        public Point2<double> P1
        {
            get { return p1; }
            set { p1 = value; }
        }

        public bool HasCrossed
        {
            get { return hasCrossed; }
            set { hasCrossed = value; }
        }

        public BoundsLineCrossingType CrossingTypePoint0
        {
            get { return crossingTypePoint0; }
            set { crossingTypePoint0 = value; }
        }

        public BoundsLineCrossingType CrossingTypePoint1
        {
            get { return crossingTypePoint1; }
            set { crossingTypePoint1 = value; }
        }

        public BoundsLineCrossing (Point2<double> point0, Point2<double> point1)
        {
            this.p0 = point0;
            this.p1 = point1;
        }

        private bool hasCrossed;
        private Point2<double> p0;
        private Point2<double> p1;
        private BoundsLineCrossingType crossingTypePoint0;
        private BoundsLineCrossingType crossingTypePoint1;
    }

    public sealed class CohenSutherland2DClipper
    {
        static public void Clip (BoundsLineCrossing crossing, Bounds2 bounds)
        {
            int outCode0, outCode1;

            Point2<double> P0 = crossing.P0;
            Point2<double> P1 = crossing.P1;

            while (true)
            {
                outCode0 = OutCodes (bounds, P0);
                outCode1 = OutCodes (bounds, P1);

                crossing.P0 = P0;
                crossing.P1 = P1;

                if (RejectCheck (outCode0, outCode1))
                    return;

                if (AcceptCheck (outCode0, outCode1))
                    return;

                if (outCode0 == 0)
                {
                    double tempCoord; int tempCode;
                    tempCoord = P0.X; P0.X = P1.X; P1.X = tempCoord;
                    tempCoord = P0.Y; P0.Y = P1.Y; P1.Y = tempCoord;
                    tempCode = outCode0; outCode0 = outCode1; outCode1 = tempCode;
                    BoundsLineCrossingType tempType;
                    tempType = crossing.CrossingTypePoint0; crossing.CrossingTypePoint0 = crossing.CrossingTypePoint1; crossing.CrossingTypePoint1 = tempType;
                }

                if ((outCode0 & 1) != 0)
                {
                    P0.X += (P1.X - P0.X) * (bounds.MaxY - P0.Y) / (P1.Y - P0.Y);
                    P0.Y = bounds.MaxY;
                    crossing.CrossingTypePoint0 = BoundsLineCrossingType.CrossesMaxY;
                    crossing.HasCrossed = true;
                }
                else
                    if ((outCode0 & 2) != 0)
                    {
                        P0.X += (P1.X - P0.X) * (bounds.MinY - P0.Y) / (P1.Y - P0.Y);
                        P0.Y = bounds.MinY;
                        crossing.CrossingTypePoint0 = BoundsLineCrossingType.CrossesMinY;
                        crossing.HasCrossed = true;
                    }
                    else
                        if ((outCode0 & 4) != 0)
                        {
                            P0.Y += (P1.Y - P0.Y) * (bounds.MaxX - P0.X) / (P1.X - P0.X);
                            P0.X = bounds.MaxX;
                            crossing.CrossingTypePoint0 = BoundsLineCrossingType.CrossesMaxX;
                            crossing.HasCrossed = true;
                        }
                        else
                            if ((outCode0 & 8) != 0)
                            {
                                P0.Y += (P1.Y - P0.Y) * (bounds.MinX - P0.X) / (P1.X - P0.X);
                                P0.X = bounds.MinX;
                                crossing.CrossingTypePoint0 = BoundsLineCrossingType.CrossesMinX;
                                crossing.HasCrossed = true;
                            }
            }
        }

        private CohenSutherland2DClipper () { }

        private static int OutCodes (Bounds2 bounds, Point2<double> P)
        {
            int Code = 0;

            if (P.Y > bounds.MaxY) Code += 1; /* code for above */
            else if (P.Y < bounds.MinY) Code += 2; /* code for below */

            if (P.X > bounds.MaxX) Code += 4; /* code for right */
            else if (P.X < bounds.MinX) Code += 8; /* code for left */

            return Code;
        }

        private static bool RejectCheck (int outCode1, int outCode2)
        {
            if ((outCode1 & outCode2) != 0) return true;
            return (false);
        }

        private static bool AcceptCheck (int outCode1, int outCode2)
        {
            if ((outCode1 == 0) && (outCode2 == 0)) return (true);
            return (false);
        }
    }
}
