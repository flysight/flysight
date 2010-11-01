using FlySightViewer.WinFormsUI.Docking;
using GMap.NET;

namespace FlySightViewer.Forms
{
    public partial class MapForm : DockContent
    {
        public MapForm()
        {
            InitializeComponent();

            // config map 
            mMap.Position = new PointLatLng(54.6961334816182, 25.2985095977783);
            mMap.MapType = MapType.GoogleHybrid;
            mMap.MinZoom = 1;
            mMap.MaxZoom = 17;
            mMap.Zoom = 3;
        }

        public Range DisplayRange
        {
            get { return mMap.DisplayRange; }
            set { mMap.DisplayRange = value; }
        }

        public LogEntry SelectedEntry
        {
            set { mMap.LogEntry = value; }
        }
    }
}
