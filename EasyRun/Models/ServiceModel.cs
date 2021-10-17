using EasyRun.Binding;
using EasyRun.CustomAttributes;
using Newtonsoft.Json;

namespace EasyRun.Models
{
    public class ServiceModel : BindableBase
    {
        private string name;
        private string tyeName;
        private string projectFile;
        private bool selected;
        private bool defaultSelected;
        private string bindings;
        private string arguments;
        private string envVariables;
        private string secretEnvVariables;
        private int replicas = 1;
        private ServiceType serviceType;
        private bool debuggerIsAttached;

        [JsonTarget(JsonTargetType.Solution)]
        public ServiceType ServiceType
        {
            get { return serviceType; }
            set { SetProperty(ref serviceType, value); }
        }

        [JsonIgnore]
        public bool DebuggerIsAttached
        {
            get { return debuggerIsAttached; }
            set { SetProperty(ref debuggerIsAttached, value); }
        }

        [JsonTarget(JsonTargetType.Solution)]
        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        [JsonTarget(JsonTargetType.Solution)]
        public string TyeName
        {
            get { return tyeName; }
            set { SetProperty(ref tyeName, value); }
        }

        [JsonTarget(JsonTargetType.Solution)]
        public string ProjectFile
        {
            get { return projectFile; }
            set { SetProperty(ref projectFile, value); }
        }

        [JsonIgnore]
        public bool Selected
        {
            get { return selected; }
            set { SetProperty(ref selected, value); }
        }

        [JsonTarget(JsonTargetType.Solution)]
        public bool DefaultSelected
        {
            get { return defaultSelected; }
            set { SetProperty(ref defaultSelected, value); }
        }

        [JsonTarget(JsonTargetType.Solution)]
        public string Bindings
        {
            get { return bindings; }
            set { SetProperty(ref bindings, value); }
        }

        [JsonTarget(JsonTargetType.Solution)]
        public string Arguments
        {
            get { return arguments; }
            set { SetProperty(ref arguments, value); }
        }

        [JsonTarget(JsonTargetType.Solution)]
        public string EnvVariables
        {
            get { return envVariables; }
            set { SetProperty(ref envVariables, value); }
        }

        [JsonTarget(JsonTargetType.Secrets)]
        public string SecretEnvVariables
        {
            get { return secretEnvVariables; }
            set { SetProperty(ref secretEnvVariables, value); }
        }

        [JsonTarget(JsonTargetType.Solution)]
        public int Replicas
        {
            get { return replicas; }
            set { SetProperty(ref replicas, value); }
        }

        public ServiceModel Clone()
        {
            return new ServiceModel
            {
                Name = this.Name,
                ProjectFile = this.ProjectFile,
                Selected = this.Selected,
                Arguments = this.Arguments,
                EnvVariables = this.EnvVariables,
            };
        }
    }
}
