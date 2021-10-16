using EasyRun.Binding;
using EasyRun.CustomAttributes;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace EasyRun.Models
{
    public class ProfileModel : BindableBase
    {
        private string name = string.Empty;
        private string filter = string.Empty;
        private bool attachDebugger;
        private bool waitOnAttachDebugger = true;
        private bool watch;
        private int tyePort = 8000;
        private LoggingTargetType loggingTargetType;
        private string loggingPath = "./.logs";
        private string loggerUrl;

        private bool useTye;

        private ObservableCollection<ServiceModel> services = new ObservableCollection<ServiceModel>();
        private ObservableCollection<ServiceModel> filteredServices = new ObservableCollection<ServiceModel>();

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        // For some reason, Visual Studio Combobox style want a property called Text. If not present, we get a binding error.
        [JsonIgnore]
        public string Text
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        [JsonTarget(JsonTargetType.Solution)]
        public LoggingTargetType LoggingTargetType
        {
            get { return loggingTargetType; }
            set { SetProperty(ref loggingTargetType, value); }
        }

        [JsonTarget(JsonTargetType.Solution)]
        public string LoggingPath
        {
            get { return loggingPath; }
            set { SetProperty(ref loggingPath, value); }
        }

        [JsonTarget(JsonTargetType.Solution)]
        public string LoggerUrl
        {
            get { return loggerUrl; }
            set { SetProperty(ref loggerUrl, value); }
        }

        [JsonTarget(JsonTargetType.Solution)]
        public bool UseTye
        {
            get { return useTye; }
            set { SetProperty(ref useTye, value); }
        }

        [JsonTarget(JsonTargetType.User)]
        public bool AttachDebugger
        {
            get { return attachDebugger; }
            set { SetProperty(ref attachDebugger, value); }
        }

        [JsonTarget(JsonTargetType.User)]
        public bool WaitOnAttachDebugger
        {
            get { return waitOnAttachDebugger; }
            set { SetProperty(ref waitOnAttachDebugger, value); }
        }

        [JsonTarget(JsonTargetType.User)]
        public bool Watch
        {
            get { return watch; }
            set { SetProperty(ref watch, value); }
        }

        [JsonTarget(JsonTargetType.Solution)]
        public int TyePort
        {
            get { return tyePort; }
            set { SetProperty(ref tyePort, value); }
        }

        [JsonTarget(JsonTargetType.Solution)]
        public string Filter
        {
            get { return filter; }
            set { SetProperty(ref filter, value); }
        }

        [JsonTarget(JsonTargetType.Solution)]
        public ObservableCollection<ServiceModel> Services
        {
            get { return services; }
            set { SetProperty(ref services, value); }
        }

        // This is filtered services
        [JsonIgnore]
        public ObservableCollection<ServiceModel> FilteredServices
        {
            get { return filteredServices; }
            set { SetProperty(ref filteredServices, value); }
        }

        public ProfileModel MapServices(List<ServiceModel> defaultServices)
        {
            Services.Clear();
            if (defaultServices != null)
            {
                foreach (var service in defaultServices)
                {
                    Services.Add(service.Clone());
                }
            }
            RefreshFilter();

            return this;
        }

        public ProfileModel SyncVsServices(Dictionary<string, ServiceModel> vsServiceDictionary)
        {
            // Remove any service type Project that no longer exists.
            Services = new ObservableCollection<ServiceModel>(Services.Where(w => w.ServiceType != ServiceType.Project || vsServiceDictionary.ContainsKey(w.Name)));

            var currentDic = Services.ToDictionary(k => k.Name, v => v);
            var missingServices = vsServiceDictionary.Where(w => !currentDic.ContainsKey(w.Value.Name));

            // Add services that is missing in our profile.
            foreach (var service in missingServices)
            {
                Services.Add(service.Value.Clone());
            }

            RefreshFilter();

            return this;
        }

        public void SetFilter(string filterText)
        {
            if (string.IsNullOrEmpty(filterText))
            {
                FilteredServices = new ObservableCollection<ServiceModel>(Services.OrderBy(o => o.Name));
            }
            else
            {
                var regex = new Regex(filterText);

                FilteredServices = new ObservableCollection<ServiceModel>(
                    Services
                        .Where(w => regex.IsMatch(w.Name))
                        .OrderBy(o => o.Name)
                    );
            }
        }
        public void RefreshFilter()
        {
            SetFilter(Filter);
        }
    }
}
