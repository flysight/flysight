using System;
using FlySightViewer.WinFormsUI.Docking;
using FlySightViewer.Controls;

namespace FlySightViewer.Forms
{
    public partial class GraphForm : DockContent
    {
        public event EventHandler DisplayRangeChanged;

        public GraphForm()
        {
            InitializeComponent();

            mGraphMode.Items.AddRange(Enum.GetNames(typeof(Graph.DisplayMode)));
            mGraphMode.SelectedIndex = 1;
        }

        public Range DisplayRange
        {
            get { return mGraph.DisplayRange; }
            set
            {
                if (value != mGraph.DisplayRange)
                {
                    if (value.Width > 10)
                    {
                        mGraph.DisplayRange = value;
                    }
                    else
                    {
                        mGraph.DisplayRange = Range.Invalid;
                    }

                    if (DisplayRangeChanged != null)
                    {
                        DisplayRangeChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public LogEntry SelectedEntry
        {
            set
            {
                mAltitudeGraph.LogEntry = value;
                mGraph.LogEntry = value;                
            }
        }

        private void OnRangeSelectChanged(object sender, EventArgs e)
        {
            DisplayRange = mAltitudeGraph.SelectRange;
        }

        private void OnModeSelected(object sender, EventArgs e)
        {
            int idx = mGraphMode.SelectedIndex;
            Graph.DisplayMode[] values = (Graph.DisplayMode[])Enum.GetValues(typeof(Graph.DisplayMode));
            mGraph.Mode = values[idx];
            UpdateGraphMode();
        }

        private void UpdateGraphMode()
        {
            if (mGraph.Mode == Graph.DisplayMode.GlideRatio)
            {
                mImperial.Hide();
                mMetric.Hide();
            }
            else
            {
                mImperial.Show();
                mMetric.Show();
                switch (mGraph.Mode)
                {
                    case Graph.DisplayMode.HorizontalVelocity:
                    case Graph.DisplayMode.VerticalVelocity:
                        mImperial.Text = "MPH";
                        mMetric.Text = "KMPH";
                        break;
                    case Graph.DisplayMode.Altitude:
                        mImperial.Text = "ft (x1000)";
                        mMetric.Text = "KM";
                        break;
                }
            }
        }

        private void OnUnitCheckedChanged(object sender, EventArgs e)
        {
            if (mImperial.Checked)
            {
                mGraph.Unit = Graph.Units.Imperial;
            }
            else
            {
                mGraph.Unit = Graph.Units.Metric;
            }
        }
    }
}
