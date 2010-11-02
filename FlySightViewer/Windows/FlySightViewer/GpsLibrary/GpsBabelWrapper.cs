using System.Diagnostics;
using System.IO;
using System.Text;

namespace Brejc.GpsLibrary
{
    public class GpsBabelWrapper
    {
        public string GpsBabelPath
        {
            get { return gpsBabelPath; }
            set { gpsBabelPath = value; }
        }

        public string InputPort
        {
            get { return inputPort; }
            set { inputPort = value; }
        }

        public string ResponseError
        {
            get { return responseError; }
        }

        public string ResponseStandard
        {
            get { return responseStandard; }
        }

        public string SourceType
        {
            get { return sourceType; }
            set { sourceType = value; }
        }

        public string Execute()
        {
            if (false == File.Exists(GpsBabelPath))
                throw new FileNotFoundException("gpsbabel executable could not be found", GpsBabelPath);

            string path = Path.ChangeExtension(Path.GetTempFileName(), "gpx");
            ProcessStartInfo info = new ProcessStartInfo(GpsBabelPath, ConstructCommandLineArguments(path));
            info.CreateNoWindow = true;
            info.ErrorDialog = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.UseShellExecute = false;

            using (Process p = new Process())
            {
                p.StartInfo = info;
                p.Start();
                //p.WaitForExit();

                responseStandard = p.StandardOutput.ReadToEnd();
                responseError = p.StandardError.ReadToEnd();
                return path;
            }
        }

        public string ConstructCommandLineArguments(string output)
        {
            StringBuilder args = new StringBuilder();
            args.AppendFormat("-t -i {0} -f {1} -o gpx,gpxver=1.1 -F \"{2}\"", sourceType, inputPort, output);
            return args.ToString();
        }

        private string gpsBabelPath;
        private string sourceType = "garmin";
        private string inputPort = "usb:";
        private string responseStandard;
        private string responseError;
    }
}
