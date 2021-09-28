using EasyRun.Binding;
using EasyRun.Dialogs;
using EasyRun.Logging;
using EasyRun.Models;
using EasyRun.PubSubEvents;
using EasyRun.Settings;
using EasyRun.Studio;
using EasyRun.Tye;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using PubSub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace EasyRun.Views
{
    public partial class EasyRunToolWindowView : UserControl
    {
        private readonly Guid instanceId = Guid.NewGuid();
        private readonly DTE2 dte;
        private readonly SettingsManager settingsManager = new SettingsManager();
        private readonly TyeManager tyeManager = new TyeManager();
        private bool solutionIsOpening;

        private Hub pubSub = Hub.Default;

        private List<ServiceModel> vsServiceList = new List<ServiceModel>();
        private IObservable<string> textChangedObservable;

        // Models
        public EasyRunModel Model { get; set; } = new EasyRunModel();
        public StateModel State { get; set; } = new StateModel();

        // Commands
        public RelayCommand AddDockerCommand { get; set; }
        public RelayCommand SaveProfileCommand { get; set; }
        public RelayCommand EditProfileCommand { get; set; }
        public RelayCommand CopyProfileCommand { get; set; }
        public RelayCommand AddProfileCommand { get; set; }
        public RelayCommand DeleteProfileCommand { get; set; }
        public RelayCommand RunCommand { get; set; }
        public RelayCommand StopCommand { get; set; }
        public RelayCommand ShowDashboardCommand { get; set; }
        public RelayCommand ServiceSelectionCommand { get; set; }
        public RelayCommand HideInfoCommand { get; set; }
        public RelayCommand SaveSelectionsAsDefaultCommand { get; set; }
        
        public EasyRunToolWindowView()
        {
            this.InitializeComponent();

            DataContext = this;
            State.AutosaveSelectionsAsDefault = GeneralOptions.Instance.AutosaveSelectionsAsDefault;

            dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            Assumes.Present(dte);

            textChangedObservable =
                Observable.FromEventPattern(Filter, "TextChanged")
                .Select(evt => ((TextBox)evt.Sender).Text)
                .Throttle(TimeSpan.FromMilliseconds(800))
                .ObserveOn(this);

            textChangedObservable.Subscribe(FilterChanged);

            pubSub.Subscribe<PubSubSolution>(this, solutionEvent =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                SolutionEvents(solutionEvent);
            });

            pubSub.Subscribe<PubSubOptionChange>(this, change =>
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (change.OptionId.Equals(nameof(GeneralOptions.AutosaveSelectionsAsDefault)))
                {
                    var newValue = bool.Parse(change.NewValue);
                    if (State.AutosaveSelectionsAsDefault != newValue)
                    {
                        State.AutosaveSelectionsAsDefault = newValue;
                    }
                }
            });

            AddDockerCommand = new RelayCommand(_ => AddDocker());
            SaveProfileCommand = new RelayCommand(_ => SaveProfile());
            EditProfileCommand = new RelayCommand(_ => EditProfile());
            CopyProfileCommand = new RelayCommand(_ => CopyProfile());
            AddProfileCommand = new RelayCommand(_ => AddProfile());
            DeleteProfileCommand = new RelayCommand(_ => DeleteProfile());
            RunCommand = new RelayCommand(_ => Run());
            StopCommand = new RelayCommand(_ => Stop());
            ShowDashboardCommand = new RelayCommand(_ => ShowDashboard());
            ServiceSelectionCommand = new RelayCommand(_ => ServiceSelection());
            HideInfoCommand = new RelayCommand(_ => HideInfo());
            SaveSelectionsAsDefaultCommand = new RelayCommand(_ => SaveSelectionsAsDefault());

            if (!settingsManager.IsLoaded())
            {
                pubSub.Publish(new PubSubSolution(PubSubEventTypes.AfterOpenSolution));
            }
        }

        private void Run()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            settingsManager.SaveProfiles(Model);

            if (Model.SelectedProfile?.UseTye == true)
            {
                var anySelected = Model.SelectedProfile.FilteredServices.Any(w => w.Selected);

                if (!anySelected)
                {
                    Logger.LogActive("Please select at least one service/project.");
                    return;
                }

                RunInTye();
            }
        }

        private void Stop()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (State.Running && Model.SelectedProfile.UseTye)
            {
                State.Stopping = true;
                tyeManager.StopTye(Model.SelectedProfile);
            }
        }

        private void ShowDashboard()
        {
            if (Model.SelectedProfile != null)
            {
                System.Diagnostics.Process.Start($"http://localhost:{Model.SelectedProfile.TyePort}");
            }
        }

        private void RunInTye()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            settingsManager.SaveProfiles(Model);

            var yamlFilename = tyeManager.BuildTyeManifest(dte, Model.SelectedProfile, instanceId);

            if (string.IsNullOrEmpty(yamlFilename))
            {
                //TODO: Show "snackbar info" about where to find EasyRun log output window
            }
            else
            {
                State.Stopping = false;

                tyeManager.RunTye(
                    dte,
                    Model.SelectedProfile,
                    yamlFilename,
                    () => State.Running = true,
                    () => TyeHasStopped());

                State.Running = tyeManager.TyeIsRunning;
            }
        }

        private void TyeHasStopped()
        {
            State.Running = false;
            State.Stopping = false;
        }

        private void ServiceSelection()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SelectServices();
        }

        private void SaveProfile()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            bool success = settingsManager.SaveProfiles(Model);
            if (success)
            {
                ShowInfo("Profiles was saved.", true);
            }
            else
            {
                ShowInfo("Nothing new to save.", true);
            }
        }

        private void SaveSelectionsAsDefault()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            bool success = settingsManager.SaveSelectionsAsDefault(Model, Model.SelectedProfile);
            if (success)
            {
                ShowInfo("Profile defaults was saved.", true);
            }
            else
            {
                ShowInfo("Nothing new to save.", true);
            }
        }

        private bool ValidProfileName(string name, string title, bool edit)
        {
            if (string.IsNullOrEmpty(name))
            {
                this.ShowWarningOkDialog(title, "Profile name cannot be empty");
                return false;
            }

            if (edit && name.Equals(Model.SelectedProfile.Name, StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            var foundProfile = Model.Profiles.FirstOrDefault(a => a.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

            if (foundProfile != null)
            {
                this.ShowWarningOkDialog(title, "You cannot have two profiles with the same name");
                return false;
            }

            return true;
        }

        private void AddProfile()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.ShowProfileDialog("Add new profile", "", true, result => AddProfile(result));
        }

        private void AddProfile(DialogProfileParameter profile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (profile.Response == DialogResponse.Ok && ValidProfileName(profile.Text, "Add new profile", false))
            {
                Model.Profiles.Add(new ProfileModel { Name = profile.Text, Filter = "", UseTye = profile.UseTye }.MapServices(vsServiceList));
                Model.SelectedProfile = Model.Profiles.Where(w => w.Name == profile.Text).First();
                SelectServices();
            }
        }

        private void EditProfile()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (Model.SelectedProfile != null)
            {
                this.ShowProfileDialog("Edit profile", Model.SelectedProfile.Name, Model.SelectedProfile.UseTye, result => EditProfile(result));
            }
        }

        private void EditProfile(DialogProfileParameter profile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (profile.Response == DialogResponse.Ok && ValidProfileName(profile.Text, "Edit profile", true))
            {
                Model.SelectedProfile.Name = profile.Text;
                Model.SelectedProfile.UseTye = profile.UseTye;
            }
        }

        private void CopyProfile()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (Model.SelectedProfile != null)
            {
                this.ShowTextDialog("Copy Profile", Model.SelectedProfile.Name, result => CopyProfile(result));
            }
        }

        private void CopyProfile(DialogTextParameter profile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (profile.Response == DialogResponse.Ok && ValidProfileName(profile.Text, "Copy profile", false))
            {
                var currentProfile = JsonConvert.SerializeObject(Model.SelectedProfile);
                var newProfile = JsonConvert.DeserializeObject<ProfileModel>(currentProfile);
                newProfile.Name = profile.Text;
                newProfile.SetFilter(newProfile.Filter);

                Model.Profiles.Add(newProfile);
                Model.SelectedProfile = Model.Profiles.Where(w => w.Name == profile.Text).First();

                SelectServices();
            }
        }

        private void DeleteProfile()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (Model.Profiles.Count() <= 1)
            {
                this.ShowWarningOkDialog("Delete profile", "There has to be at least one profile left.");
            }
            else if (!string.IsNullOrEmpty(Model.SelectedProfile.Name))
            {
                this.ShowWarningYesNoDialog("Delete profile", $"Are you sure you want to delete the profile '{Model.SelectedProfile.Name}'?", result => DeleteProfile(result));
            }
        }

        private void DeleteProfile(DialogWarningYesNoParameter warning)
        {
            if (warning.Response == DialogResponse.Ok)
            {
                var toBeDeleted = Model.SelectedProfile;

                Model.SelectedProfile = Model.Profiles.Where(w => w.Name != Model.SelectedProfile.Name).First();

                Model.Profiles.Remove(toBeDeleted);
            }
        }

        private void AddDocker()
        {
            if (Model.SelectedProfile != null)
            {
                this.ShowTextDialog("Add new Docker service", "", result => AddDocker(result));
            }
        }

        private void AddDocker(DialogTextParameter service)
        {
            if (service.Response == DialogResponse.Ok)
            {
                Model.SelectedProfile.Services.Add(new ServiceModel { Name = service.Text, ServiceType = ServiceType.Image });
            }
        }

        private void SolutionEvents(PubSubSolution solutionEvent)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            switch (solutionEvent.Type)
            {
                case PubSubEventTypes.BeforeOpenSolution:
                    BeforeOpenSolution();
                    break;

                case PubSubEventTypes.AfterOpenSolution:
                    AfterOpenSolution(solutionEvent.ForceReload);
                    break;

                case PubSubEventTypes.BeforeCloseSolution:
                    BeforeCloseSolution();
                    break;

                case PubSubEventTypes.AfterCloseSolution:
                    AfterCloseSolution();
                    break;

                case PubSubEventTypes.OnAfterRenameProject:
                    OnAfterRenameProject();
                    break;

                case PubSubEventTypes.OnAfterLoadProject:
                    SelectServices();
                    break;

                case PubSubEventTypes.OnAfterOpenProject:
                    OnAfterOpenProject(solutionEvent.IsAdded);
                    break;

                case PubSubEventTypes.OnBeforeCloseProject:
                    OnBeforeCloseProject(solutionEvent.IsRemoved, solutionEvent.ProjectName);
                    break;
            }
        }

        private void OnAfterOpenProject(bool isAdded)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(dte.Solution.FullName))
            {
                return;
            }

            if (isAdded || !solutionIsOpening)
            {
                vsServiceList = dte.GetVsServiceList();
                settingsManager.SyncWithVsServices(Model, vsServiceList);
            }
        }

        private void OnBeforeCloseProject(bool isRemoved, string projectName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (string.IsNullOrEmpty(dte.Solution.FullName))
            {
                return;
            }

            if (isRemoved)
            {
                vsServiceList = dte.GetVsServiceList(projectName);

                settingsManager.SyncWithVsServices(Model, vsServiceList);
            }
        }


        private void OnAfterRenameProject()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var changedServiceList = dte.GetVsServiceList();

            var changedDic = changedServiceList.ToDictionary(k => k.ProjectFile, v => v);
            var currentDic = vsServiceList.ToDictionary(k => k.ProjectFile, v => v);

            var currentServices = vsServiceList.Where(w => !changedDic.ContainsKey(w.ProjectFile));
            var changedServices = changedServiceList.Where(w => !currentDic.ContainsKey(w.ProjectFile));

            if (currentServices.Count() == 1 && changedServices.Count() == 1)
            {
                var changedService = changedServices.First();
                var currentService = currentServices.First();

                foreach (var profile in Model.Profiles)
                {
                    foreach(var service in profile.Services.Where(w => w.ServiceType == ServiceType.Project))
                    {
                        if (service.ProjectFile == currentService.ProjectFile)
                        {
                            service.ProjectFile = changedService.ProjectFile;
                            service.Name = changedService.Name;
                        }
                    }
                    profile.RefreshFilter();
                }
            }
            else
            {
                Logger.LogActive("Project/Service renaming failed.");
            }

            vsServiceList = changedServiceList;
        }

        private void BeforeOpenSolution()
        {
            solutionIsOpening = true;
        }

        private void AfterOpenSolution(bool forceReload)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (!forceReload && settingsManager.IsLoaded())
                {
                    return;
                }

                vsServiceList = dte.GetVsServiceList();

                var loadedModel = settingsManager.LoadProfiles(vsServiceList);

                if (loadedModel is null)
                {
                    Model.SetToDefault(vsServiceList);
                }
                else
                {
                    Model.Profiles.Clear();

                    Model.SelectedProfile = null;
                    Model.Profiles = loadedModel.Profiles;
                    Model.SettingsId = loadedModel.SettingsId;

                    if (string.IsNullOrEmpty(loadedModel.SelectedProfileName))
                    {
                        Model.SelectedProfile = Model.Profiles.First();
                    }
                    else
                    {
                        var selectedProfile = Model.Profiles.FirstOrDefault(w => w.Name == loadedModel.SelectedProfileName);
                        Model.SelectedProfile = selectedProfile ?? Model.Profiles.First();
                    }
                }
            }
            finally
            {
                solutionIsOpening = false;
            }
        }       

        private void BeforeCloseSolution()
        {
            solutionIsOpening = false;
            ThreadHelper.ThrowIfNotOnUIThread();
            tyeManager.StopTye(Model.SelectedProfile);
            settingsManager.SaveProfiles(Model);
        }

        private void AfterCloseSolution()
        {
            solutionIsOpening = false;
            ThreadHelper.ThrowIfNotOnUIThread();
            vsServiceList.Clear();
            Model.SetToDefault(vsServiceList);
            settingsManager.SolutionClosed();
        }

        private void FilterChanged(string filterText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!string.IsNullOrEmpty(Model.SelectedProfile.Name))
            {
                Model.SelectedProfile.SetFilter(filterText);
                Model.RaisePropertyChanged(nameof(Model.AllSelected));
            }
        }

        private void SelectServices()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Model.RaisePropertyChanged(nameof(Model.AllSelected));

            if (Model.SelectedProfile?.UseTye == false)
            {
                var selectedServices = Model
                    .SelectedProfile
                    .FilteredServices.Where(w => w.Selected && w.ServiceType == ServiceType.Project)
                    .Select(s => (object)s.ProjectFile)
                    .ToArray();

                if (selectedServices.Length == 1)
                {
                    dte.Solution.SolutionBuild.StartupProjects = selectedServices[0];
                }
                else if (selectedServices.Length > 1)
                {
                    dte.Solution.SolutionBuild.StartupProjects = selectedServices;
                }
                else
                {
                    Logger.LogActive("Please select at least one project/service.");
                    return;
                }
            }
        }

        private void ProfileChanged(object sender, SelectionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            SelectServices();
        }

        private void TextBox_OnlyNumbers(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]");
        }

        private void ShowInfo(string info, bool autoHide)
        {
            State.ShowInfoText = info;
            State.ShowInfo = true;

            if (autoHide)
            {
                Observable
                    .Timer(TimeSpan.FromSeconds(7))
                    .ObserveOn(this)
                    .Subscribe(_ =>
                    {
                        State.ShowInfo = false;
                    });
            }
        }

        private void HideInfo()
        {
            State.ShowInfo = false;
            State.ShowInfoText = string.Empty;
        }

    }
}