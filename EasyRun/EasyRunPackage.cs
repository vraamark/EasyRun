using EasyRun.Logging;
using EasyRun.PubSubEvents;
using EasyRun.Settings;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using PubSub;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace EasyRun
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid("e4568a5d-446c-4d1e-b30a-9b1ef8625ffd")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(EasyRunToolWindow))]
    [ProvideOptionPage(typeof(DialogPageProvider.General), "EasyRun", "General", 0, 0, true)]
    public sealed class EasyRunPackage : AsyncPackage, IVsSolutionEvents, IVsSolutionEvents4, IVsSolutionLoadEvents, IVsSelectionEvents
    {
        private Hub pubSub = Hub.Default;
        private IVsSolution2 solution = null;
        private uint solutionEventsCookie;

        private IVsMonitorSelection monitorSelection = null;
        private uint selectionEventsCookie;

        protected override void Dispose(bool disposing)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            base.Dispose(disposing);

            // Unadvise all events
            if (solution != null && solutionEventsCookie != 0)
            {
                solution.UnadviseSolutionEvents(solutionEventsCookie);
            }

            if (monitorSelection != null && selectionEventsCookie != 0)
            {
                monitorSelection.UnadviseSelectionEvents(selectionEventsCookie);
            }
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = GetGlobalService(typeof(DTE)) as DTE2;
            Assumes.Present(dte);

            IVsOutputWindow outputWindow = await GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
            var outputWindow2 = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
            Logger.Initialize(outputWindow, outputWindow2);

            bool isSolutionLoaded = await IsSolutionLoadedAsync();

            if (isSolutionLoaded)
            {
                HandleOpenSolution();
            }

            await AdviseEventsAsync();

            await EasyRunToolWindowCommand.InitializeAsync(this);
        }

        private async Task AdviseEventsAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);

            solution = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution2;
            if (solution != null)
            {
                // Register for solution events
                solution.AdviseSolutionEvents(this, out solutionEventsCookie);
            }

            monitorSelection = await GetServiceAsync(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            if (monitorSelection != null)
            {
                // Remember debugging UI context cookie for later
                //ms.GetCmdUIContextCookie(VSConstants.UICONTEXT.Debugging_guid, out debuggingCookie);
                // Register for selection events
                monitorSelection.AdviseSelectionEvents(this, out selectionEventsCookie);
            }

        }

        private async Task<bool> IsSolutionLoadedAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            var solService = await GetServiceAsync(typeof(SVsSolution)) as IVsSolution;

            Assumes.Present(solService);

            ErrorHandler.ThrowOnFailure(solService.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out object value));

            return value is bool isSolOpen && isSolOpen;
        }

        private void HandleOpenSolution()
        {
            pubSub.Publish(new PubSubSolution(PubSubEventTypes.AfterOpenSolution, true));
        }

        private static Project GetProjectFromHierarchy(IVsHierarchy pHierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var propertyResult = pHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object project);
            if (propertyResult != VSConstants.S_OK)
            {
                return null;
            }

            return project as Project;
        }

        private static string GetProjectNameFromHierarchy(IVsHierarchy pHierarchy)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var project = GetProjectFromHierarchy(pHierarchy);
            if (project == null)
            {
                return null;
            }

            return project.Name;
        }


        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            pubSub.Publish(new PubSubSolution(PubSubEventTypes.OnAfterOpenProject, false) {IsAdded = fAdded != 0 });
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var projectName = GetProjectNameFromHierarchy(pHierarchy);

            pubSub.Publish(new PubSubSolution(PubSubEventTypes.OnBeforeCloseProject) { IsRemoved = fRemoved != 0, ProjectName = projectName});
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            pubSub.Publish(new PubSubSolution(PubSubEventTypes.OnAfterLoadProject));
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            pubSub.Publish(new PubSubSolution(PubSubEventTypes.BeforeCloseSolution));
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            pubSub.Publish(new PubSubSolution(PubSubEventTypes.AfterCloseSolution));
            return VSConstants.S_OK;
        }

        public int OnAfterRenameProject(IVsHierarchy pHierarchy)
        {
            pubSub.Publish(new PubSubSolution(PubSubEventTypes.OnAfterRenameProject));
            return VSConstants.S_OK;
        }

        public int OnQueryChangeProjectParent(IVsHierarchy pHierarchy, IVsHierarchy pNewParentHier, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterChangeProjectParent(IVsHierarchy pHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterAsynchOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeOpenSolution(string pszSolutionFilename)
        {
            pubSub.Publish(new PubSubSolution(PubSubEventTypes.BeforeOpenSolution));
            return VSConstants.S_OK;
        }

        public int OnBeforeBackgroundSolutionLoadBegins()
        {
            return VSConstants.S_OK;
        }

        public int OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
        {
            pfShouldDelayLoadToNextIdle = false;
            return VSConstants.S_OK;
        }

        public int OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterBackgroundSolutionLoadComplete()
        {
            HandleOpenSolution();
            return VSConstants.S_OK;
        }

        public int OnSelectionChanged(IVsHierarchy pHierOld, uint itemidOld, IVsMultiItemSelect pMISOld, ISelectionContainer pSCOld, IVsHierarchy pHierNew, uint itemidNew, IVsMultiItemSelect pMISNew, ISelectionContainer pSCNew)
        {
            return VSConstants.S_OK;
        }

        public int OnElementValueChanged(uint elementid, object varValueOld, object varValueNew)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (elementid == (uint)VSConstants.VSSELELEMID.SEID_StartupProject)
            {
                if (GeneralOptions.Instance.SyncWithStartupProjects)
                {
                    pubSub.Publish(new PubSubSolution(PubSubEventTypes.OnStartupProjectChanged));
                }
            }
            return VSConstants.S_OK;
        }

        public int OnCmdUIContextChanged(uint dwCmdUICookie, int fActive)
        {
            return VSConstants.S_OK;
        }
    }
}
