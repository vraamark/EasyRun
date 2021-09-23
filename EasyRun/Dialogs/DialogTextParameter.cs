using EasyRun.Binding;
using EasyRun.Dialogs.Interfaces;

namespace EasyRun.Dialogs
{
    public class DialogTextParameter : BindableBase, IDialogParameter
    {

        private string text = string.Empty;
        private string title = string.Empty;
        private string okText = "OK";
        private string cancelText = "Cancel";

        public string Text
        {
            get { return text; }
            set { SetProperty(ref text, value); }
        }

        public string Title
        {
            get { return title; }
            set { SetProperty(ref title, value); }
        }

        public DialogResponse Response { get; set; }

        public string OkText
        {
            get { return okText; }
            set { SetProperty(ref okText, value); }
        }

        public string CancelText
        {
            get { return cancelText; }
            set { SetProperty(ref cancelText, value); }
        }

    }
}
