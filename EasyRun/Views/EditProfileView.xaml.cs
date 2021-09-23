using EasyRun.Dialogs;

namespace EasyRun.Views
{
    public partial class EditProfileView : DialogBase
    {
        public DialogProfileParameter Model => this.Parameter as DialogProfileParameter;

        public EditProfileView()
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