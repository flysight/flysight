using Brejc.GpsLibrary.Gpx;

namespace Brejc.GpsLibrary
{
    public interface IGpsCommunicator
    {
        GpxFile DownloadGpsData();
    }
}
