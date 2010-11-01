using System;
using System.Drawing;
using GMap.NET;
using GMap.NET.WindowsForms;

namespace FlySightViewer.Controls
{
    /// <summary>
    /// custom map of GMapControl
    /// </summary>
    public class Map : GMapControl
    {
        private static Pen mRedPen = new Pen(Color.Red, 2.0f);
        private static Pen mSelectedPen = new Pen(Color.Lime, 3.0f);

        private LogEntry mEntry;
        private Point[] mPoints;
        private Point[] mSelectedPoints;
        private RectLatLng mBounds;
        private Range mShowRange;

        public LogEntry LogEntry
        {
            get { return mEntry; }
            set
            {
                if (!object.ReferenceEquals(mEntry, value))
                {
                    mEntry = value;
                    mShowRange = Range.Invalid;
                    Setup();
                }
            }
        }

        public Range DisplayRange
        {
            get { return mShowRange; }
            set
            {
                if (mShowRange != value)
                {
                    mShowRange = value;
                    mSelectedPoints = new Point[mShowRange.Width];
                    Invalidate();
                }
            }
        }

        protected override void OnPaintEtc(System.Drawing.Graphics g)
        {
            base.OnPaintEtc(g);
            if (mEntry != null)
            {
                int idx = 0;
                foreach (Record rec in mEntry.Records)
                {
                    mPoints[idx++] = FromLatLngToLocal(rec.Location);
                }
                g.DrawLines(mRedPen, mPoints);

                if (mShowRange.IsValid)
                {
                    idx = 0;
                    for (int i = mShowRange.Min; i < mShowRange.Max; ++i)
                    {
                        mSelectedPoints[idx++] = mPoints[i];
                    }
                    g.DrawLines(mSelectedPen, mSelectedPoints);
                }
            }
        }
        
        private void Setup()
        {
            if (mEntry != null && mEntry.Records.Count > 0)
            {
                double left = double.MaxValue;
                double top = double.MinValue;
                double right = double.MinValue;
                double bottom = double.MaxValue;

                foreach (Record rec in mEntry.Records)
                {
                    left = Math.Min(left, rec.Location.Lng);
                    top = Math.Max(top, rec.Location.Lat);
                    right = Math.Max(right, rec.Location.Lng);
                    bottom = Math.Min(bottom, rec.Location.Lat);
                }

                mBounds = RectLatLng.FromLTRB(left, top, right, bottom);
                mPoints = new Point[mEntry.Records.Count];
                SetZoomToFitRect(mBounds);
            }
            else
            {
                mPoints = null;
            }
        }
    }
}
