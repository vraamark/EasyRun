using EasyRun.Binding;
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

        public ServiceType ServiceType
        {
            get { return serviceType; }
            set { SetProperty(ref serviceType, value); }
        }

        public string Name
        {
            get { return name; }
            set { SetProperty(ref name, value); }
        }

        public string TyeName
        {
            get { return tyeName; }
            set { SetProperty(ref tyeName, value); }
        }

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

        public bool DefaultSelected
        {
            get { return defaultSelected; }
            set { SetProperty(ref defaultSelected, value); }
        }

        public string Bindings
        {
            get { return bindings; }
            set { SetProperty(ref bindings, value); }
        }

        public string Arguments
        {
            get { return arguments; }
            set { SetProperty(ref arguments, value); }
        }

        public string EnvVariables
        {
            get { return envVariables; }
            set { SetProperty(ref envVariables, value); }
        }
        
        public string SecretEnvVariables
        {
            get { return secretEnvVariables; }
            set { SetProperty(ref secretEnvVariables, value); }
        }

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
