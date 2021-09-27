using EasyRun.Models;
using EasyRun.Settings;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using VSLangProj;

namespace EasyRun.Studio
{
    public static class VSProjects
    {
        private static readonly Guid vsProjectKindSolutionFolder = new Guid("2150E333-8FDC-42A3-9474-1A3956D46DE8");
        private static readonly Guid vsProjectKindMisc = new Guid("66A2671D-8FB5-11D2-AA7E-00C04F688DDE");

        public static List<ServiceModel> GetVsServiceList(this DTE2 dte, string excludeProjectName = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var newVsServiceList = new List<ServiceModel>();

            Projects projects = dte.Solution.Projects;

            if (projects.Count > 0)
            {
                foreach (Project project in projects)
                {
                    AddProject(dte, project, newVsServiceList, excludeProjectName);
                    FindProjects(dte, project.ProjectItems, newVsServiceList, excludeProjectName);
                }
            }

            return newVsServiceList.OrderBy(o => o.Name).ToList();
        }

        private static void FindProjects(DTE2 dte, ProjectItems projectItems, List<ServiceModel> newVsServiceList, string excludeProjectName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (projectItems != null)
            {
                foreach (ProjectItem projectItem in projectItems)
                {
                    if (projectItem.Object is Project project)
                    {
                        AddProject(dte, project, newVsServiceList, excludeProjectName);

                        if (project.ProjectItems != null)
                        {
                            FindProjects(dte, project.ProjectItems, newVsServiceList, excludeProjectName);
                        }
                    }
                }
            }
        }

        private static void AddProject(DTE2 dte, Project project, List<ServiceModel> newVsServiceList, string excludeProjectName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                if (Guid.TryParse(project.Kind, out var kind) && (kind == vsProjectKindMisc || kind == vsProjectKindSolutionFolder))
                {
                    return;
                }

                if (GetProperty<prjOutputType>(project, "OutputType", out var outputType) && outputType == prjOutputType.prjOutputTypeLibrary)
                {
                    return;
                }

                var name = project.Name;

                if (!string.IsNullOrEmpty(excludeProjectName) && name.Equals(excludeProjectName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return;
                }

                var filename = project.FileName;

                if (!string.IsNullOrEmpty(filename) && filename.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    filename = SettingsManager.GetRelativePathToSolution(dte, filename);
                    newVsServiceList.Add(new ServiceModel() { Name = name, ProjectFile = filename });
                }
            }
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
            catch (Exception)
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
            {
                // We get an exception if filename cannot be read.
            }
        }

        private static bool GetProperty<T>(Project project, string propertyName, out T value)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                Property property = project.Properties?.Item(propertyName);

                if (property is null || property.Value is null)
                {
                    value = default(T);
                    return false;
                }

                value = (T)property.Value;
                return true;
            }
            catch (Exception)
            {
                value = default(T);
                return false;
            }
        }
    }
}
