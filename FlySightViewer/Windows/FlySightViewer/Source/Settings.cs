using System.Drawing;
using Microsoft.Win32;

namespace FlySightViewer
{
    public class Settings
    {
        public string GPSBabelPath { get; set; }

        private static Settings defaultInstance = new Settings();

        public static Settings Instance
        {
            get { return defaultInstance; }
        }

        public Settings()
        {
            Load();
        }

        public void Save()
        {
            Registry.SetValue("HKEY_CURRENT_USER\\Software\\FlySightViewer", "GPSBabel", GPSBabelPath, RegistryValueKind.String);
        }

        public void Load()
        {
            GPSBabelPath = GetValue("HKEY_CURRENT_USER\\Software\\FlySightViewer", "GPSBabel", string.Empty);
        }

        static bool GetValue(string location, string key, bool def)
        {
            object value = Registry.GetValue(location, key, def ? 1 : 0);
            if (value == null)
            {
                return def;
            }
            return (int)value == 1;
        }
        
        static int GetValue(string location, string key, int def)
        {
            object value = Registry.GetValue(location, key, def);
            if (value == null)
            {
                return def;
            }
            return (int)value;
        }

        static Color GetValue(string location, string key, Color def)
        {
            object value = Registry.GetValue(location, key, def.ToArgb());
            if (value == null)
            {
                return def;
            }
            return Color.FromArgb((int)value);
        }

        static string GetValue(string location, string key, string def)
        {
            object value = Registry.GetValue(location, key, def);
            if (value == null)
            {
                return def;
            }
            return (string)value;
        }
    }
}
