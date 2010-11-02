using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlySightViewer.Forms
{
    public partial class GpsDownloadForm : Form
    {
        private static KeyValuePair<string, string>[] inputOptions = new KeyValuePair<string, string>[]
        {
            new KeyValuePair<string, string>("garmin", "Garmin serial/USB protocol"), 
            new KeyValuePair<string, string>("baroiq", "Brauniger IQ Series Barograph"), 
            new KeyValuePair<string, string>("dg-100", "GlobalSat DG-100/BT-335"), 
            new KeyValuePair<string, string>("magellan","Magellan serial protocol"), 
            new KeyValuePair<string, string>("mtk","MTK Logger (iBlue 747,Qstarz BT-1000,...)"), 
            new KeyValuePair<string, string>("navilink","NaviGPS GT-11/BGT-11"), 
            new KeyValuePair<string, string>("wbt","Wintec WBT-100/200 GPS"), 
        };

        private static KeyValuePair<string, string>[] portOptions = new KeyValuePair<string, string>[]
        {
            new KeyValuePair<string, string>("usb:", "USB"),
            new KeyValuePair<string, string>("com1:", "COM1"), 
            new KeyValuePair<string, string>("com2:", "COM2"), 
            new KeyValuePair<string, string>("com3:", "COM3"), 
            new KeyValuePair<string, string>("com4:", "COM4"), 
            new KeyValuePair<string, string>("com5:", "COM5"), 
            new KeyValuePair<string, string>("com6:", "COM6"), 
            new KeyValuePair<string, string>("com7:", "COM7"), 
            new KeyValuePair<string, string>("com8:", "COM8"), 
            new KeyValuePair<string, string>("com9:", "COM9"), 
            new KeyValuePair<string, string>("com10:", "COM10"), 
        };

        public GpsDownloadForm()
        {
            InitializeComponent();
            FillInputPortSelectionBox(portOptions, "com3:");
            FillGpsSourceTypeSelectionBox(inputOptions, "mtk");
            comboBoxSourceType_SelectionChangeCommitted(null, EventArgs.Empty);
        }

        public string SourceType
        {
            get { return ((KeyValuePair<string, string>)comboBoxSourceType.SelectedItem).Key; }
        }

        public string InputPort
        {
            get { return ((KeyValuePair<string, string>)comboBoxInputPort.SelectedItem).Key; }
        }

        public void FillGpsSourceTypeSelectionBox(KeyValuePair<string, string>[] options, string defaultOption)
        {
            comboBoxSourceType.Items.Clear();
            comboBoxSourceType.DisplayMember = "Value";
            foreach (KeyValuePair<string, string> pair in options)
            {
                comboBoxSourceType.Items.Add(pair);
                if (pair.Key == defaultOption)
                {
                    comboBoxSourceType.SelectedItem = pair;
                }
            }
        }

        public void FillInputPortSelectionBox(KeyValuePair<string, string>[] options, string defaultOption)
        {
            comboBoxInputPort.Items.Clear();
            comboBoxInputPort.DisplayMember = "Value";
            foreach (KeyValuePair<string, string> pair in options)
            {
                comboBoxInputPort.Items.Add(pair);
                if (pair.Key == defaultOption)
                {
                    comboBoxInputPort.SelectedItem = pair;
                }
            }
        }

        private void comboBoxSourceType_SelectionChangeCommitted(object sender, EventArgs e)
        {
            switch (SourceType)
            {
                case "garmin":
                    comboBoxInputPort.SelectedItem = portOptions[0];
                    comboBoxInputPort.Enabled = true;
                    break;
                case "magellan":
                case "dg-100":
                case "baroiq":
                case "mtk":
                case "navilink":
                case "wbt":
                    comboBoxInputPort.SelectedItem = portOptions[3];
                    comboBoxInputPort.Enabled = false;
                    break;
            }
        }
    }
}
