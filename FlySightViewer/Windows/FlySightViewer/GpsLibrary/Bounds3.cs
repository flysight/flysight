using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Brejc.Geometry
{
    [Serializable]
    public class Bounds3 : ICloneable
    {
        public double MinX
        {
            [DebuggerStepThrough]
            get { return minX; }
            [DebuggerStepThrough]
            set { minX = value; }
        }

        public double MaxX
        {
            [DebuggerStepThrough]
            get { return maxX; }
            [DebuggerStepThrough]
            set { maxX = value; }
        }

        public double MinY
        {
            [DebuggerStepThrough]
            get { return minY; }
            [DebuggerStepThrough]
            set { minY = value; }
        }

        public double MaxY
        {
            [DebuggerStepThrough]
            get { return maxY; }
            [DebuggerStepThrough]
            set { maxY = value; }
        }

        public double MinZ
        {
            [DebuggerStepThrough]
            get { return minZ; }
            [DebuggerStepThrough]
            set { minZ = value; }
        }

        public double MaxZ
        {
            [DebuggerStepThrough]
            get { return maxZ; }
            [DebuggerStepThrough]
            set { maxZ = value; }
        }

        public Point3<double> Center
        {
            [DebuggerStepThrough]
            get
            {
                return new Point3<double> ((minX + maxX) / 2, (minY + maxY) / 2, (minZ + maxZ) / 2);
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

        public double DeltaZ
        {
            [DebuggerStepThrough]
            get { return maxZ - minZ; }
            [DebuggerStepThrough]
            set { maxZ = minZ + value; }
        }

        public Point3<double> MinPoint
        {
            [DebuggerStepThrough]
            get { return new Point3<double> (minX, minY, minZ); }
        }

        public Point3<double> MaxPoint
        {
            [DebuggerStepThrough]
            get { return new Point3<double> (maxX, maxY, maxZ); }
        }

        [DebuggerStepThrough]
        public Bounds3 () { }

        [DebuggerStepThrough]
        public Bounds3 (double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            this.minX = minX;
            this.minY = minY;
            this.minZ = minZ;
            this.maxX = maxX;
            this.maxY = maxY;
            this.maxZ = maxZ;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bounds3"/> class using
        /// an array of points which all have to be included in the boundaries.
        /// </summary>
        /// <param name="pointsToCover">Points which this instance should cover.</param>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
        public Bounds3(IEnumerable<Point3<double>> pointsToCover)
        {
            foreach (Point3<double> point in pointsToCover)
                ExtendToCover (point);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bounds3"/> class so that it
        /// has a specified point in its center and has width and height equal to the speficied <c>size</c> parameter.
        /// </summary>
        /// <param name="centerPoint">The center point of the new boundary.</param>
        /// <param name="size">The width and the height of the boundary.</param>
        public Bounds3 (Point3<double> centerPoint, double size)
        {
            double halfSize = size / 2;
            minX = centerPoint.X - halfSize;
            minY = centerPoint.Y - halfSize;
            minZ = centerPoint.Z - halfSize;
            maxX = centerPoint.X + halfSize;
            maxY = centerPoint.Y + halfSize;
            maxZ = centerPoint.Z + halfSize;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bounds3"/> class with the two
        /// points specifying its extent.
        /// </summary>
        /// <param name="point1">The first point.</param>
        /// <param name="point2">The second point.</param>
        public Bounds3 (Point3<double> point1, Point3<double> point2)
        {
            ExtendToCover (point1);
            ExtendToCover (point2);
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

            if (minZ > maxZ)
            {
                swap = minZ;
                minZ = maxZ;
                maxZ = swap;
            }
        }

        public void ExtendToCover (Point3<double> point)
        {
            ExtendToCover (point.X, point.Y, point.Z);
        }

        public void ExtendToCover (double x, double y, double z)
        {
            if (x.CompareTo (minX) < 0)
                minX = x;
            if (x.CompareTo (maxX) > 0)
                maxX = x;
            if (y.CompareTo (minY) < 0)
                minY = y;
            if (y.CompareTo (maxY) > 0)
                maxY = y;
            if (z.CompareTo (minZ) < 0)
                minZ = z;
            if (z.CompareTo (maxZ) > 0)
                maxZ = z;
        }

        /// <summary>
        /// Returns the intersection of two <see cref="Bounds3"/> objects.
        /// </summary>
        /// <param name="with"><see cref="Bounds3"/> object to intersect with.</param>
        /// <returns>Intersection of two <see cref="Bounds3"/> objects.</returns>
        public Bounds3 Intersect (Bounds3 with)
        {
            if (with == null)
                throw new ArgumentNullException ("with");

            return new Bounds3 (Math.Max (MinX, with.MinX), Math.Max (MinY, with.MinY), Math.Max (MinZ, with.MinZ),
                Math.Min (MaxX, with.MaxX), Math.Min (MaxY, with.MaxY), Math.Min (MaxZ, with.MaxZ));
        }

        /// <summary>
        /// Checks if the object intersects with specified boundaries.
        /// </summary>
        /// <returns><c>True</c> if it intersects, <c>false</c> otherwise.</returns>
        public bool IntersectsWith (double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            return !(MinY > maxY || MaxY < minY || MaxX < minX || MinX > maxX || MaxZ < minZ || MinZ > maxZ);
        }

        /// <summary>
        /// Checks if the object intersects with specified boundaries.
        /// </summary>
        /// <param name="other">The other boundaries.</param>
        /// <returns>
        /// 	<c>True</c> if it intersects, <c>false</c> otherwise.
        /// </returns>
        public bool IntersectsWith (Bounds3 other)
        {
            return IntersectsWith (other.minX, other.minY, other.minZ, other.maxX, other.maxY, other.maxZ);
        }

        /// <summary>
        /// Checks if the specified geographic point is inside bounds.
        /// </summary>
        /// <returns><c>True</c> if the point is inside, <c>false</c> otherwise.</returns>
        public bool IsInside (double x, double y, double z)
        {
            return MinX <= x && x <= MaxX && MinY <= y && y <= MaxY && MinZ <= z && z <= MaxZ;
        }

        /// <summary>
        /// Checks if the specified geographic point is inside bounds.
        /// </summary>
        /// <returns><c>True</c> if the point is inside, <c>false</c> otherwise.</returns>
        public bool IsInside (Point3<double> point)
        {
            return MinX <= point.X && point.X <= MaxX
                && MinY <= point.Y && point.Y <= MaxY
                && MinZ <= point.Z && point.Z <= MaxZ;
        }

        /// <summary>
        /// Returns the union of two <see cref="Bounds3"/> objects.
        /// </summary>
        /// <param name="with"><see cref="Bounds3"/> object to unite with.</param>
        /// <returns>Union of two <see cref="Bounds3"/> objects.</returns>
        public Bounds3 Union (Bounds3 with)
        {
            if (with == null)
                throw new ArgumentNullException ("with");

            return new Bounds3 (Math.Min (MinX, with.MinX), Math.Min (MinY, with.MinY), Math.Min (MinZ, with.MinZ),
                Math.Max (MaxX, with.MaxX), Math.Max (MaxY, with.MaxY), Math.Max (MaxZ, with.MaxZ));
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
        /// <returns>Inflated <see cref="Bounds3"/> object.</returns>
        public Bounds3 InflateBy (double factor)
        {
            if (factor < 0)
                throw new ArgumentOutOfRangeException ("factor", factor, "Factor cannot be a negative value.");

            double xFactor = DeltaX * (factor - 1) / 2;
            double yFactor = DeltaY * (factor - 1) / 2;
            double zFactor = DeltaZ * (factor - 1) / 2;

            return new Bounds3 (MinX - xFactor, MinY - yFactor, MinZ - zFactor,
                MaxX + xFactor, MaxY + yFactor, MaxZ + zFactor);
        }

        public Bounds3 Inflate (double dx, double dy, double dz)
        {
            return new Bounds3 (MinX - dx, MinY - dy, MinZ - dz,
                MaxX + dx, MaxY + dy, MaxZ + dz);
        }

        public object Clone ()
        {
            return new Bounds3 (minX, minY, minZ, maxX, maxY, maxZ);
        }

        /// <summary>
        /// Compares the current <see cref="Bounds3"/> object to the specified object for equivalence.
        /// </summary>
        /// <param name="obj">The <see cref="Bounds3"/> object to test for equivalence with the current object.</param>
        /// <returns>
        /// <c>true</c> if the two <see cref="Bounds3"/> objects are equal; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals (object obj)
        {
            if (obj == null)
                return false;

            Bounds3 that = obj as Bounds3;

            if (that == null)
                return false;

            return minX.Equals (that.minX) && minY.Equals (that.minY) && minZ.Equals (that.minZ)
                && maxX.Equals (that.maxX) && maxY.Equals (that.MaxY) && maxZ.Equals (that.MaxZ);
        }

        /// <summary>
        /// Returns the hash code for this <see cref="Bounds3"/> object.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer hash code.
        /// </returns>
        public override int GetHashCode ()
        {
            return minX.GetHashCode () ^ minY.GetHashCode () ^ minZ.GetHashCode () ^ maxX.GetHashCode () ^ maxY.GetHashCode () ^ maxZ.GetHashCode ();
        }

        public override string ToString ()
        {
            return String.Format (System.Globalization.CultureInfo.InvariantCulture,
                "{0},{1},{2},{3},{4},{5}", minX, minY, minZ, maxX, maxY, maxZ);
        }

        private double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
        private double minZ = double.MaxValue, maxZ = double.MaxValue;
    }
}
