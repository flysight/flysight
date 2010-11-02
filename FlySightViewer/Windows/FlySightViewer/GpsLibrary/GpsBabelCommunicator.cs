using System;
using Brejc.GpsLibrary.Gpx;
using Brejc.GpsLibrary.Gpx.Filtering;

namespace Brejc.GpsLibrary
{
    public class GpsBabelCommunicator
    {
        public GpsBabelWrapper GpsBabelWrapper
        {
            get { return gpsBabelWrapper; }
        }

        public string DownloadGpsData()
        {
            string file = gpsBabelWrapper.Execute();
            if (!string.IsNullOrEmpty(gpsBabelWrapper.ResponseError))
            {
                throw new InvalidOperationException(gpsBabelWrapper.ResponseError);
            }
            return file;
        }

        private readonly GpsBabelWrapper gpsBabelWrapper = new GpsBabelWrapper();
    }
}