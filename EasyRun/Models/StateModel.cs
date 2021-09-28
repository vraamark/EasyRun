using EasyRun.Binding;
using System.Collections.ObjectModel;

namespace EasyRun.Models
{
    public class StateModel : BindableBase
    {
        private bool running;
        private bool stopping;
        private ObservableCollection<LoggingTargetModel> loggingTargets = LoggingTargetModel.GetDefault();
        private bool showInfo;
        private string showInfoText;
        private bool autosaveSelectionsAsDefault;

        public bool Running
        {
            get { return running; }
            set
            {
                if (SetProperty(ref running, value))
                {
                    RaisePropertyChanged(nameof(NotRunning));
                }
            }
        }

        public bool NotRunning
        {
            get { return !running; }
        }

        public bool Stopping
        {
            get { return stopping; }
            set { SetProperty(ref stopping, value); }
        }

        public ObservableCollection<LoggingTargetModel> LoggingTargets
        {
            get { return loggingTargets; }
            set { SetProperty(ref loggingTargets, value); }
        }

        public bool ShowInfo
        {
            get { return showInfo; }
            set { SetProperty(ref showInfo, value); }
        }

        public string ShowInfoText
        {
            get { return showInfoText; }
            set { SetProperty(ref showInfoText, value); }
        }

        public bool AutosaveSelectionsAsDefault
        {
            get { return autosaveSelectionsAsDefault; }
            set { SetProperty(ref autosaveSelectionsAsDefault, value); }
        }
    }
}
