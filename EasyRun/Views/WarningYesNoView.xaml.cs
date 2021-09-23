using EasyRun.Dialogs;

namespace EasyRun.Views
{
    public partial class WarningYesNoView : DialogBase
    {
        public DialogTextParameter Model => this.Parameter as DialogWarningYesNoParameter;

        public WarningYesNoView()
        {
            this.InitializeComponent();

            DataContext = this;
        }     
    }
}