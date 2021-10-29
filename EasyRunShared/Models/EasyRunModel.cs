using EasyRun.Binding;
using EasyRun.CustomAttributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EasyRun.Models
{
    public class EasyRunModel : BindableBase
    {
        private ProfileModel selectedProfile = new ProfileModel();
        private ObservableCollection<ProfileModel> profiles = new ObservableCollection<ProfileModel>();

        [JsonTarget(JsonTargetType.Solution)]
        public Guid SettingsId { get; set; } = Guid.NewGuid();

        public ObservableCollection<ProfileModel> Profiles
        {
            get { return profiles; }
            set { SetProperty(ref profiles, value); }
        }

        [JsonTarget(JsonTargetType.User)]
        public string SelectedProfileName { get; set; }

        [JsonIgnore]
        public ProfileModel SelectedProfile
        {
            get { return selectedProfile; }
            set { SetProperty(ref selectedProfile, value, () => SelectedProfileName = value?.Name); }
        }

        [JsonIgnore]
        public bool AllSelected {
            get { return IsAllSelected(); }
            set { SelectAll(value); }
        }

        public void SetToDefault(List<ServiceModel> vsServiceList)
        {
            Profiles.Clear();
            Profiles.Add(new ProfileModel { Name = "Default", Filter = "" }.MapServices(vsServiceList));
            SelectedProfile = Profiles.First();
        }

        private bool IsAllSelected()
        {
            if (SelectedProfile != null)
            {
                return !SelectedProfile.FilteredServices.Any(a => !a.Selected);
            }

            return false;
        }

        private void SelectAll(bool select)
        {
            if (SelectedProfile != null)
            {
                foreach(var service in SelectedProfile.FilteredServices)
                {
                    service.Selected = select;
                }
            }
        }
    }
}
