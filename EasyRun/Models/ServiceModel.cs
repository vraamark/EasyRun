﻿using EasyRun.Binding;
using System.ComponentModel;

namespace EasyRun.Models
{
    public class ServiceModel : BindableBase
    {
        private string name;
        private string tyeName;
        private string projectFile;
        private bool selected;
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

        [DefaultValue("")]
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

        public bool Selected
        {
            get { return selected; }
            set { SetProperty(ref selected, value); }
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

        [DefaultValue(1)]
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
