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
                }
                else
                {
                    mRawData.DataSource = null;
                }
            }
        }
    }
}
