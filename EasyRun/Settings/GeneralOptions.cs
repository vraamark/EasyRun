using Community.VisualStudio.Toolkit;
using EasyRun.PubSubEvents;
using PubSub;
using System.ComponentModel;

namespace EasyRun.Settings
{
    internal class GeneralOptions : BaseOptionModel<GeneralOptions>
    {
        private readonly Hub pubSub = Hub.Default;

        private bool autosaveSelectionsAsDefault;

        [Category("Profiles")]
        [DisplayName("Autosave selections as default")]
        [Description("Selections will automatically be saved as defaults. The will update the *.EasyRun.json file when selections change.")]
        [DefaultValue(false)]
        public bool AutosaveSelectionsAsDefault
        {
            get { return autosaveSelectionsAsDefault; }
            set
            {
                var oldValue = autosaveSelectionsAsDefault;
                autosaveSelectionsAsDefault = value;
                pubSub.Publish(new PubSubOptionChange(nameof(AutosaveSelectionsAsDefault), oldValue.ToString(), value.ToString()));
            }
        }

        [Category("Profiles")]
        [DisplayName("Sync with startup project selections")]
        [Description("Selections will automatically change in current profile if startup projects are changed from solution explorer.")]
        [DefaultValue(false)]
        public bool SyncWithStartupProjects { get; set; } = true;
    }
}
