using System;
using System.Drawing;
using System.Windows.Forms;

namespace FlySightViewer.Controls
{
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

        public struct Value
        {
            public float velh;
            public float velv;
            public float ratio;
            public float altitude;

            public float this[DisplayMode idx]
            {
                get
                {
                    switch (idx)
                    {
                        case DisplayMode.HorizontalVelocity: return velh;
                        case DisplayMode.VerticalVelocity: return velv;
                        case DisplayMode.GlideRatio: return ratio;
                        case DisplayMode.Altitude: return altitude;
                    }
                    return 0.0f;
                }
            }
        }

        private const float FeetPerMeter = 3.2808399f;
        private const float MeterPerSecondToMilesPerHour = 2.23693629f;
        private const float MeterPerSecondToKilometerPerHour = 3.6f;

        private static Pen mPen = new Pen(Color.Purple, 2.0f);
        private static Brush mBrush = new SolidBrush(Color.FromArgb(128, 80, 80, 192));
        private static Font mFont = new Font("Arial", 8, FontStyle.Regular, GraphicsUnit.Pixel);

        private LogEntry mEntry;
        private DisplayMode mMode = DisplayMode.Altitude;
        private Units mUnits = Units.Metric;
        private Value[] mValues;
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
                    CalcMinMax();
                    Invalidate();
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
                        minValue = Math.Min(minValue, mValues[i][mMode]);
                        maxValue = Math.Max(maxValue, mValues[i][mMode]);
                    }

                    if (maxValue > minValue)
                    {
                        int log = (int)Math.Log10(maxValue - minValue);
                        mStep = (float)Math.Pow(10, log);
                    }
                    else
                    {
                        mStep = 1.0f;
                    }

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
                mValues = new Value[mEntry.Records.Count];
                ClampRange();

                if (mUnits == Units.Metric)
                {
                    CalcMetricValues();
                }
                else
                {
                    CalcImperialValues();
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
                    float value = mValues[i][mMode];
                    mPoints[idx].X = 10 + (idx * dx);
                    mPoints[idx].Y = Height - ((value - mMinValue) * dy);
                    idx++;
                }

                // draw grid.
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.DrawLine(Pens.Gray, 0, centerY, Width, centerY);
                g.DrawLine(Pens.Gray, 10, 0, 10, Height);

                float fheight = mFont.Height;
                int minI = (int)(mMinValue / mStep) - 1;
                int maxI = (int)(mMaxValue / mStep) + 1;
                for (int i = minI; i < maxI; ++i)
                {
                    float value = i * mStep;
                    float y = Height - ((value - mMinValue) * dy);
                    g.DrawLine(Pens.Gray, 0, y, Width, y);

                    if (dy * mStep > 40)
                    {
                        for (int j = 1; j < 5; ++j)
                        {
                            float v = value + j * mStep * 0.2f;
                            float l = Height - ((v - mMinValue) * dy);
                            g.DrawLine(Pens.Silver, 0, l, Width, l);
                        }
                    }

                    if (mShowUnits)
                    {
                        g.DrawString(value.ToString(), mFont, Brushes.Black, 10, y - fheight);
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

        protected override void OnResize(EventArgs e)
        {
            Invalidate();
            base.OnResize(e);
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

        private void CalcMetricValues()
        {
            int idx = 0;
            foreach (Record rec in mEntry.Records)
            {
                float mps = (float)Math.Sqrt(rec.VelocityEast * rec.VelocityEast + rec.VelocityNorth * rec.VelocityNorth);
                mValues[idx].velh = mps * MeterPerSecondToKilometerPerHour;
                mValues[idx].velv = rec.VelocityDown * MeterPerSecondToKilometerPerHour;
                mValues[idx].ratio = mValues[idx].velh / mValues[idx].velv;
                mValues[idx].altitude = rec.Altitude / 1000.0f;
                idx++;
            }
            LowPass();
            LowPass();
            LowPass();
        }

        #endregion

        #region -- Imperial calculations --------------------------------------

        private void CalcImperialValues()
        {
            int idx = 0;
            foreach (Record rec in mEntry.Records)
            {
                float mps = (float)Math.Sqrt(rec.VelocityEast * rec.VelocityEast + rec.VelocityNorth * rec.VelocityNorth);
                mValues[idx].velh = mps * MeterPerSecondToMilesPerHour;
                mValues[idx].velv = rec.VelocityDown * MeterPerSecondToMilesPerHour;
                mValues[idx].ratio = mValues[idx].velh / mValues[idx].velv;
                mValues[idx].altitude = (rec.Altitude * FeetPerMeter) / 1000.0f;
                idx++;
            }
            LowPass();
            LowPass();
            LowPass();
        }

        #endregion
        
        public void LowPass()
        {
            int num = mValues.Length - 1;
            for (int i = 1; i < num; i++)
            {
                mValues[i].velh = (mValues[i - 1].velh + (mValues[i].velh * 2) + mValues[i + 1].velh) / 4;
                mValues[i].velv = (mValues[i - 1].velv + (mValues[i].velv * 2) + mValues[i + 1].velv) / 4;
                mValues[i].altitude = (mValues[i - 1].altitude + (mValues[i].altitude * 2) + mValues[i + 1].altitude) / 4;
                mValues[i].ratio = mValues[i].velh / mValues[i].velv;
            }
        }
    }
}
