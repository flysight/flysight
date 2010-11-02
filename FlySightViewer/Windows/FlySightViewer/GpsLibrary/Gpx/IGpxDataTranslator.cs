using System.IO;

namespace Brejc.GpsLibrary.Gpx
{
    /// <summary>
    /// Defines methods for reading and writing GPS data to an GPX file. Each version (1.0, 1.1) of GPX file format
    /// should have its own implementation of this interface.
    /// </summary>
    public interface IGpxDataTranslator
    {
        /// <summary>
        /// Reads the GPX data from a stream and puts it into the provided <see cref="GpxFile"/> object.
        /// </summary>
        /// <param name="gpxFile">The GPX file object to fill the data into.</param>
        /// <param name="stream">The stream from which to read GPX data.</param>
        void ReadGpxData (GpxFile gpxFile, Stream stream);
        /// <summary>
        /// Writes the GPX data from the specified <see cref="GpxFile"/> object to a file.
        /// </summary>
        /// <param name="gpxFile">The GPX file to write data from.</param>
        /// <param name="fileName">Name of the GPX file to write data to.</param>
        void WriteGpxData(GpxFile gpxFile, string fileName);
    }
}