namespace Brejc.GpsLibrary.Gpx.Filtering
{
    public interface IFileFilter
    {
        GpxFile ApplyFilter(GpxFile input);
    }
}