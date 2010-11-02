using System.Windows.Forms;
using FlySightViewer.WinFormsUI.Docking;

namespace FlySightViewer.Forms
{
    public partial class DataForm : DockContent
    {
        public DataForm()
        {
            InitializeComponent();
        }

        public LogEntry SelectedEntry
        {
            set
            {
                if (value != null)
                {
                    mRawData.DataSource = value.Records;
                    mRawData.Columns[0].DefaultCellStyle.Format = "hh:mm:ss.ff";
                    mRawData.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
                }
                else
                {
                    mRawData.DataSource = null;
                }
            }
        }
    }
}
