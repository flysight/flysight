using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace FlySightLog.Source
{
    public struct Range
    {
        public int Min;
        public int Max;

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

        public static Range Invalid = new Range(-1, -1);
    }

    public partial class Graph : UserControl
    {
        public enum DisplayMode
        {
            HorizontalVelocity,
            VerticalVelocity,
            GlideRatio,
            Altitude,
        }

        public enum Units
        {
            Metric,
            Imperial
        }

        private const float FeetPerMeter = 3.2808399f;
        private const float MeterPerSecondToMilesPerHour = 2.23693629f;
        private const float MeterPerSecondToKilometerPerHour = 3.6f;

        private static Pen mPen = new Pen(Color.Purple, 2.0f);
        private static Brush mBrush = new SolidBrush(Color.FromArgb(128, 80, 80, 192));

        private LogEntry mEntry;
        private DisplayMode mMode = DisplayMode.Altitude;
        private Units mUnits = Units.Metric;
        private float[] mValues;
        private PointF[] mPoints;
        private Range mShowRange;
        private int mSelectMin = -1;
        private int mSelectMax = -1;
        private bool mAllowSelect = false;
        private float mStep;
        private float mMinValue;
        private float mMaxValue;
        private bool mShowUnits = true;

        public Graph()
        {
            InitializeComponent();
            DoubleBuffered = true;
        }

        public LogEntry LogEntry
        {
            get { return mEntry; }
            set
            {
                if (!object.ReferenceEquals(mEntry, value))
                {
                    mEntry = value;
                    Setup();

                    mSelectMin = -1;
                    mSelectMax = -1;
                    if (mValues != null)
                    {
                        DisplayRange = new Range(0, mValues.Length);
                    }
                }
            }
        }

        public DisplayMode Mode
        {
            get { return mMode; }
            set
            {
                if (!object.ReferenceEquals(mMode, value))
                {
                    mMode = value;
                    Setup();
                }
            }
        }

        public Units Unit
        {
            get { return mUnits; }
            set
            {
                if (!object.ReferenceEquals(mUnits, value))
                {
                    mUnits = value;
                    Setup();
                }
            }
        }

        public bool AllowSelect
        {
            get { return mAllowSelect; }
            set { mAllowSelect = value; }
        }

        public bool ShowUnits
        {
            get { return mShowUnits; }
            set { mShowUnits = value; Invalidate(); }
        }

        public Range SelectRange
        {
            get { return new Range(mSelectMax, mSelectMin); }
        }

        public Range DisplayRange
        {
            get { return mShowRange; }
            set
            {
                if (value.IsValid)
                {
                    mShowRange = value;
                }
                else
                {
                    mShowRange = new Range(0, mValues.Length);
                }

                CalcMinMax();
                Invalidate();
            }
        }

        public event EventHandler SelectChanged;

        private void CalcMinMax()
        {
            if (mValues != null)
            {
                ClampRange();
                if (mShowRange.Width > 0)
                {

                    // calculate min/max.
                    float minValue = float.MaxValue;
                    float maxValue = float.MinValue;
                    for (int i = mShowRange.Min; i < mShowRange.Max; i++)
                    {
                        minValue = Math.Min(minValue, mValues[i]);
                        maxValue = Math.Max(maxValue, mValues[i]);
                    }
                    int log = (int)Math.Log10(maxValue - minValue);

                    mStep = (float)Math.Pow(10, log);

                    mMinValue = (float)Math.Round(minValue / mStep) * mStep - (mStep * 0.5f);
                    mMaxValue = (float)Math.Round(maxValue / mStep) * mStep + (mStep * 0.5f);

                    while ((mMaxValue - mMinValue) / mStep < 5)
                    {
                        mStep *= 0.5f;
                    }
                }
            }
        }

        private void ClampRange()
        {
            int min = Math.Min(Math.Max(0, mShowRange.Min), mValues.Length);
            int max = Math.Min(Math.Max(0, mShowRange.Max), mValues.Length);
            mShowRange = new Range(min, max);
            mPoints = new PointF[mShowRange.Width];
        }

        private void Setup()
        {
            if (mEntry != null && mEntry.Records.Count > 0)
            {
                mValues = new float[mEntry.Records.Count];
                ClampRange();

                if (mUnits == Units.Metric)
                {
                    switch (mMode)
                    {
                        case DisplayMode.HorizontalVelocity:
                            CalcMetricHorizontalVelocity();
                            break;
                        case DisplayMode.VerticalVelocity:
                            CalcMetricVerticalVelocity();
                            break;
                        case DisplayMode.GlideRatio:
                            CalcMetricGlideRatio();
                            break;
                        case DisplayMode.Altitude:
                            CalcMetricAltitude();
                            break;
                    }
                }
                else
                {
                    switch (mMode)
                    {
                        case DisplayMode.HorizontalVelocity:
                            CalcImperialHorizontalVelocity();
                            break;
                        case DisplayMode.VerticalVelocity:
                            CalcImperialVerticalVelocity();
                            break;
                        case DisplayMode.GlideRatio:
                            CalcImperialGlideRatio();
                            break;
                        case DisplayMode.Altitude:
                            CalcImperialAltitude();
                            break;
                    }
                }
            }
            else
            {
                mValues = null;
            }

            CalcMinMax();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            if (mValues != null)
            {
                // calculate deltas.
                float dx = (Width - 10) / (float)mShowRange.Width;
                float dy = Height / (mMaxValue - mMinValue);
                float centerY = Height - ((0 - mMinValue) * dy);

                // calculate all points.
                int idx = 0;
                for (int i = mShowRange.Min; i < mShowRange.Max; ++i)
                {
                    float value = mValues[i];
                    mPoints[idx].X = 10 + (idx * dx);
                    mPoints[idx].Y = Height - ((value - mMinValue) * dy);
                    idx++;
                }

                // draw grid.
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.DrawLine(Pens.Gray, 0, centerY, Width, centerY);
                g.DrawLine(Pens.Gray, 10, 0, 10, Height);

                float fheight = SystemFonts.DefaultFont.Height;
                int minI = (int)(mMinValue / mStep) - 1;
                int maxI = (int)(mMaxValue / mStep) + 1;
                for (int i = minI; i < maxI; ++i)
                {
                    float value = i * mStep;
                    float y = Height - ((value - mMinValue) * dy);
                    g.DrawLine(Pens.Gray, 0, y, Width, y);
                    if (mShowUnits)
                    {
                        g.DrawString(value.ToString(), SystemFonts.DefaultFont, Brushes.Black, 10, y - fheight);
                    }
                }

                // draw actual graph.
                g.DrawLines(mPen, mPoints);

                // draw selection box.
                if (mSelectMin > 0 && mSelectMax > 0)
                {
                    int x = Math.Min(mSelectMin, mSelectMax);
                    int width = Math.Abs(mSelectMax - mSelectMin);
                    g.FillRectangle(mBrush, 10 + (x * dx), 0, width * dx, Height);
                }
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (mAllowSelect)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    mSelectMin = Math.Max(0, GetIndex(e.X));
                }
                else if (e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    mSelectMin = -1;
                    mSelectMax = -1;
                    if (SelectChanged != null)
                    {
                        SelectChanged(this, EventArgs.Empty);
                    }
                    Invalidate();
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (mAllowSelect)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    mSelectMax = Math.Max(0, GetIndex(e.X));
                    if (SelectChanged != null)
                    {
                        SelectChanged(this, EventArgs.Empty);
                    }
                    Invalidate();
                }
            }
            base.OnMouseMove(e);
        }

        private int GetIndex(float aX)
        {
            if (mValues != null)
            {
                float dx = (Width - 10) / (float)mValues.Length;
                return (int)((aX - 10) / dx);
            }
            return -1;
        }

        #region -- Metric calculations ----------------------------------------

        private void CalcMetricHorizontalVelocity()
        {
            int idx = 0;
            foreach (Record rec in mEntry.Records)
            {
                float mps = (float)Math.Sqrt(rec.VelocityEast * rec.VelocityEast + rec.VelocityNorth * rec.VelocityNorth);
                mValues[idx++] = mps * MeterPerSecondToKilometerPerHour;
            }
        }

        private void CalcMetricVerticalVelocity()
        {
            int idx = 0;
            foreach (Record rec in mEntry.Records)
            {
                mValues[idx++] = rec.VelocityDown * MeterPerSecondToKilometerPerHour;
            }
        }

        private void CalcMetricGlideRatio()
        {
            int idx = 0;
            foreach (Record rec in mEntry.Records)
            {
                if (rec.VelocityDown != 0)
                {
                    mValues[idx++] = (float)Math.Sqrt(rec.VelocityEast * rec.VelocityEast + rec.VelocityNorth * rec.VelocityNorth) / rec.VelocityDown;
                }
                else
                {
                    mValues[idx++] = 0;
                }
            }
        }

        private void CalcMetricAltitude()
        {
            int idx = 0;
            foreach (Record rec in mEntry.Records)
            {
                mValues[idx++] = rec.Altitude / 1000.0f;
            }
        }

        #endregion

        #region -- Imperial calculations --------------------------------------

        private void CalcImperialHorizontalVelocity()
        {
            int idx = 0;
            foreach (Record rec in mEntry.Records)
            {
                float mps = (float)Math.Sqrt(rec.VelocityEast * rec.VelocityEast + rec.VelocityNorth * rec.VelocityNorth);
                mValues[idx++] = mps * MeterPerSecondToMilesPerHour;
            }
        }

        private void CalcImperialVerticalVelocity()
        {
            int idx = 0;
            foreach (Record rec in mEntry.Records)
            {
                mValues[idx++] = rec.VelocityDown * MeterPerSecondToMilesPerHour;
            }
        }

        private void CalcImperialGlideRatio()
        {
            CalcMetricGlideRatio(); // same for both.
        }

        private void CalcImperialAltitude()
        {
            int idx = 0;
            foreach (Record rec in mEntry.Records)
            {
                mValues[idx++] = (rec.Altitude * FeetPerMeter) / 1000.0f;
            }
        }

        #endregion
    }
}
