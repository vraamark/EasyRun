using EasyRun.Dialogs.Interfaces;
using EasyRun.Logging;
using EasyRun.Views;
using Microsoft.VisualStudio.Shell;
using System;
using System.Windows;

namespace EasyRun.Dialogs
{
    public static class Dialog
    {
        private static void Show<TView, TParameter>(DependencyObject parent, TParameter parameter, Action<TParameter> responseAction)
            where TView : class, new()
            where TParameter : class, IDialogParameter
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dialogHost = UITools.FindChild<DialogHost>(parent);

            if (dialogHost is null)
            {
                Logger.LogActive("Failed to find DialogHost control.");
                return;
            }

            var dialog = new TView() as DialogBase;
            dialog.Parameter = parameter;

            dialog.ResponseAction = result =>
            {
                dialogHost.Children.Remove(dialog);
                if (result is TParameter resultParameter)
                {
                    responseAction?.Invoke(resultParameter);
                }
            };

            dialogHost.Children.Add(dialog);
            dialog.Visibility = Visibility.Visible;
        }

        public static void ShowTextDialog(this DependencyObject parent, string title, string text, Action<DialogTextParameter> responseAction)
        {
            Show<EditView, DialogTextParameter>(parent, new DialogTextParameter { Title = title, Text = text }, responseAction);
        }

        public static void ShowProfileDialog(this DependencyObject parent, string title, string text, bool useTye, Action<DialogProfileParameter> responseAction)
        {
            Show<EditProfileView, DialogProfileParameter>(parent, new DialogProfileParameter { Title = title, Text = text, UseTye = useTye }, responseAction);
        }

        public static void ShowWarningYesNoDialog(this DependencyObject parent, string title, string warning, Action<DialogWarningYesNoParameter> responseAction)
        {
            Show<WarningYesNoView, DialogWarningYesNoParameter>(parent, new DialogWarningYesNoParameter { Title = title, Text = warning }, responseAction);
        }
        public static void ShowWarningOkDialog(this DependencyObject parent, string title, string warning)
        {
            Show<WarningOkView, DialogWarningOkParameter>(parent, new DialogWarningOkParameter { Title = title, Text = warning }, null);
        }
    }
}
