using System.Collections.Generic;

namespace Brejc.GpsLibrary.Gpx.Filtering
{
    public class FileFilterChain
    {
        public IList<IFileFilter> Filters
        {
            get { return filters; }
        }

        public GpxFile ApplyFilters (GpxFile inputFile)
        {
            foreach (IFileFilter filter in filters)
                inputFile = filter.ApplyFilter(inputFile);

            return inputFile;
        }

        private readonly List<IFileFilter> filters = new List<IFileFilter>();
    }
}
