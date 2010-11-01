using System;
using System.Windows.Forms;
using FlySightViewer.Forms;
using System.Reflection;
using System.Diagnostics;

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
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            mMainForm = new MainForm();
            Application.Run(mMainForm);
        }
    }
}
