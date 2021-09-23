using EasyRun.Dialogs;

namespace EasyRun.Views
{
    public partial class EditView : DialogBase
    {
        public DialogTextParameter Model => this.Parameter as DialogTextParameter;

        public EditView()
        {
            this.InitializeComponent();

            DataContext = this;
        }

        private new void Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Text.Focusable = true;
            Text.Focus();
        }
    }
}