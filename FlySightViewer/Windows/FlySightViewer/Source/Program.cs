using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using FlySightViewer.Forms;

namespace FlySightViewer
{
    static class Program
    {
        private static MainForm mMainForm;
 
        public static string Version
        {
            get
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
                if (info != null)
                {
                    return string.Format("{0}.{1}.{2}", info.ProductMajorPart,
                        info.ProductMinorPart, info.ProductBuildPart);
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public static MainForm Form
        {
            get { return mMainForm; }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] aArguments)
        {
            // start app.
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            mMainForm = new MainForm();

            // try loading files.
            if (aArguments.Length > 0)
            {
                if (Path.GetExtension(aArguments[0]) == ".fly")
                {
                    Project.LoadProject(aArguments[0]);
                }
            }

            // run.
            Application.Run(mMainForm);
        }
    }
}
