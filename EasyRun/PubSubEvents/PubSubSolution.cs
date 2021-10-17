using Microsoft.VisualStudio.Shell.Interop;

namespace EasyRun.PubSubEvents
{
    public class PubSubSolution
    {
        public PubSubSolution(PubSubEventTypes type)
        {
            Type = type;
        }

        public PubSubSolution(PubSubEventTypes type, bool forceReload)
        {
            Type = type;
            ForceReload = forceReload;
        }

        public PubSubEventTypes Type { get; }

        public bool ForceReload { get; }

        public bool IsAdded { get; set; }

        public bool IsRemoved { get; set; }

        public string ProjectName { get; set; }

        public DBGMODE DbgModeNew { get; set; }
    }
}
