using EasyRun.Dialogs;

namespace EasyRun.Views
{
    public partial class WarningOkView : DialogBase
    {
        public DialogTextParameter Model => this.Parameter as DialogWarningOkParameter;

        public WarningOkView()
        {
            this.InitializeComponent();

            DataContext = this;
        }     
    }
}