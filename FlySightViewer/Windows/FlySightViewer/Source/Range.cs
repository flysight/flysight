using System;

namespace FlySightViewer
{
    public struct Range : IEquatable<Range>
    {
        public int Min;
        public int Max;

        public static Range Invalid = new Range(-1, -1);

        public Range(int min, int max)
        {
            Min = Math.Min(min, max);
            Max = Math.Max(min, max);
        }

        public int Width
        {
            get { return Max - Min; }
        }

        public bool IsValid
        {
            get { return Min < Max; }
        }

        public static bool operator ==(Range a, Range b)
        {
            return a.Min == b.Min && a.Max == b.Max;
        }

        public static bool operator !=(Range a, Range b)
        {
            return a.Min != b.Min || a.Max != b.Max;
        }

        public bool Equals(Range other)
        {
            return Min == other.Min && Max == other.Max;
        }

        public override bool Equals(object obj)
        {
            if (obj is Range)
            {
                return Equals((Range)obj);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Min.GetHashCode() ^ Max.GetHashCode();
        }
    }
}
