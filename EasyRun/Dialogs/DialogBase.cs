using EasyRun.Binding;
using EasyRun.Dialogs.Interfaces;
using System;
using System.Windows.Controls;

namespace EasyRun.Dialogs
{
    public class DialogBase : UserControl
    {
        public IDialogParameter Parameter { get; set; }
        public Action<IDialogParameter> ResponseAction { get; set; }

        public RelayCommand OkCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }

        public DialogBase()
        {
            OkCommand = new RelayCommand(_ => Ok());
            CancelCommand = new RelayCommand(_ => Cancel());
        }

        private void Ok()
        {
            Parameter.Response = DialogResponse.Ok;
            ResponseAction?.Invoke(Parameter);
        }

        private void Cancel()
        {
            Parameter.Response = DialogResponse.Cancel;
            ResponseAction?.Invoke(Parameter);
        }

    }
}
