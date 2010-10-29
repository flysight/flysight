
namespace GMap.NET
{
    using System.Drawing;

    public interface IGControl
    {
        PointLatLng Position
        {
            get;
            set;
        }

        Point CurrentPositionGPixel
        {
            get;
        }

        bool IsDragging
        {
            get;
        }

        RectLatLng CurrentViewArea
        {
            get;
        }

        MapType MapType
        {
            get;
            set;
        }

        PureProjection Projection
        {
            get;
        }

        bool CanDragMap
        {
            get;
            set;
        }

        // events
        event CurrentPositionChanged OnCurrentPositionChanged;
        event TileLoadComplete OnTileLoadComplete;
        event TileLoadStart OnTileLoadStart;
        event MapDrag OnMapDrag;
        event MapZoomChanged OnMapZoomChanged;
        event MapTypeChanged OnMapTypeChanged;

        void ReloadMap();

        PointLatLng FromLocalToLatLng(int x, int y);
        Point FromLatLngToLocal(PointLatLng point);
    }
}
