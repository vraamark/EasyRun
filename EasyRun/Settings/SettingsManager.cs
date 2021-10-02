﻿using EasyRun.Logging;
using EasyRun.Models;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace EasyRun.Settings
{
    public class SettingsManager
    {
        private static readonly byte[] additionalEntropy = { 99, 17, 45, 67, 23, 77 };

        private FileMonitor fileMonitor = new FileMonitor();

        public string ProfileChecksum { get; set; }
        public bool LoadHasBeenCalled { get; set; }
        public string SolutionFilename { get; set; }

        public bool SaveProfiles(EasyRunModel model)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!LoadHasBeenCalled)
            {
                // If we haven't tried to load, then there is nothing to save (no open solution/project).
                return false;
            }

            ChangePathsToRelative(model);
            SetSelectDefaultsOnSave(model);

            var currentChecksum = CalcProfileChecksum(model);
            if (currentChecksum == ProfileChecksum)
            {
                // Nothing new to save.
                return false;
            }

            try
            {
                fileMonitor.Pause = true;

                var settingsFilename = GetSettingsFilename();

                if (string.IsNullOrEmpty(settingsFilename))
                {
                    return false;
                }

                var jsonSettings = JsonConvert.SerializeObject(
                    model,
                    Formatting.Indented,
                    new JsonSerializerSettings()
                    { ContractResolver = new IgnorePropertiesResolver(nameof(ServiceModel.SecretEnvVariables)) });

                File.WriteAllText(settingsFilename, jsonSettings);

                SaveDefaultSelectedProfile(model);
                SaveSecretEnvVariables(model);

                ProfileChecksum = currentChecksum;

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return false;
            }
            finally
            {
                fileMonitor.Pause = false;
            }
        }

        public bool SaveSelectionsAsDefault(EasyRunModel model, ProfileModel profile)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (profile is null)
            {
                return false;
            }

            foreach (var service in profile.Services)
            {
                service.DefaultSelected = service.Selected;
            }

            return SaveProfiles(model);
        }

        private void ChangePathsToRelative(EasyRunModel model)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solutionFilename = GetSolutionFilename();
            var solutionDirectory = Path.GetDirectoryName(solutionFilename);

            if (!string.IsNullOrEmpty(solutionDirectory))
            {
                foreach (var profile in model.Profiles)
                {
                    foreach (var service in profile.Services)
                    {                        
                        if (Path.IsPathRooted(service.ProjectFile))
                        {
                            service.ProjectFile = PathUtility.GetRelativePath(service.ProjectFile, solutionDirectory, false);
                        }
                    }
                }
            }
        }

        public void SolutionClosed()
        {
            ProfileChecksum = string.Empty;
            LoadHasBeenCalled = false;
            SolutionFilename = string.Empty;
        }

        private void SaveSecretEnvVariables(EasyRunModel model)
        {
            var secrets = new Dictionary<string, string>();

            foreach (var profile in model.Profiles)
            {
                foreach (var service in profile.Services)
                {
                    var secret = service.SecretEnvVariables;

                    if (!string.IsNullOrEmpty(secret))
                    {
                        secret = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(secret), additionalEntropy, DataProtectionScope.LocalMachine));
                    }
                    secrets.Add($"{profile.Name}.{service.Name}", secret);
                }
            }

            var jsonSecrets = JsonConvert.SerializeObject(secrets, Formatting.Indented);

            var secretFilename = GetSecretFilename(model.SettingsId);

            File.WriteAllText(secretFilename, jsonSecrets);
        }

        private void SaveDefaultSelectedProfile(EasyRunModel model)
        {
            var defaultSelection = GetDefaultSelectionSettingsFilename(model.SettingsId);
            File.WriteAllText(defaultSelection, model.SelectedProfileName);
        }

        public bool IsLoaded()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var currentSolutionFileName = GetSolutionFilename();

            return LoadHasBeenCalled && !string.IsNullOrEmpty(currentSolutionFileName) && currentSolutionFileName.Equals(SolutionFilename, StringComparison.OrdinalIgnoreCase);
        }

        public EasyRunModel LoadProfiles(List<ServiceModel> vsServiceList)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            LoadHasBeenCalled = true;

            try
            {
                var settingsFilename = GetSettingsFilename();

                if (!string.IsNullOrEmpty(settingsFilename) && File.Exists(settingsFilename))
                {
                    fileMonitor.Start(settingsFilename, "settings");

                    var jsonSettings = File.ReadAllText(settingsFilename);
                    var model = JsonConvert.DeserializeObject<EasyRunModel>(jsonSettings);

                    SetSelectDefaultsOnLoad(model);

                    SyncWithVsServices(model, vsServiceList);

                    LoadDefaultSelectedProfile(model);
                    LoadSecretEnvVariables(model);

                    ProfileChecksum = CalcProfileChecksum(model);

                    SolutionFilename = GetSolutionFilename();

                    return model;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return null;
        }

        public void SetSelectDefaultsOnLoad(EasyRunModel model)
        {
            foreach (var profile in model.Profiles)
            {
                foreach (var service in profile.Services)
                {
                    service.Selected = service.DefaultSelected;
                }
            }
        }

        public void SetSelectDefaultsOnSave(EasyRunModel model)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (GeneralOptions.Instance.AutosaveSelectionsAsDefault)
            {
                foreach (var profile in model.Profiles)
                {
                    foreach (var service in profile.Services)
                    {
                        service.DefaultSelected = service.Selected;
                    }
                }
            }
        }

        public void SyncWithVsServices(EasyRunModel model, List<ServiceModel> vsServiceList)
        {
            var vsServiceDictionary = vsServiceList.ToDictionary(k => k.Name, v => v);
            foreach (var profile in model.Profiles)
            {
                profile.SyncVsServices(vsServiceDictionary);
            }
        }

        private void LoadSecretEnvVariables(EasyRunModel model)
        {
            var secretFilename = GetSecretFilename(model.SettingsId);

            if (!string.IsNullOrEmpty(secretFilename) && File.Exists(secretFilename))
            {
                var jsonSecrets = File.ReadAllText(secretFilename);
                var secrets = JsonConvert.DeserializeObject<Dictionary<string,string>>(jsonSecrets);

                foreach (var profile in model.Profiles)
                {
                    foreach (var service in profile.Services)
                    {
                        if (secrets.TryGetValue($"{profile.Name}.{service.Name}", out var secret) && !string.IsNullOrEmpty(secret))
                        {
                            secret = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(secret), additionalEntropy, DataProtectionScope.LocalMachine));
                            service.SecretEnvVariables = secret;
                        }
                    }
                }
            }
        }
        private void LoadDefaultSelectedProfile(EasyRunModel model)
        {
            var defaultSelection = GetDefaultSelectionSettingsFilename(model.SettingsId);
            if (File.Exists(defaultSelection))
            {
                model.SelectedProfileName = File.ReadAllText(defaultSelection);
            }
        }

        private string GetSecretFilename(Guid settingsId)
        {
            return Path.Combine(GetLocalSettingsDirectory(settingsId), "Secrets.json");
        }

        private string GetDefaultSelectionSettingsFilename(Guid settingsId)
        {
            return Path.Combine(GetLocalSettingsDirectory(settingsId), "DefaultSelection.txt");
        }

        private string GetLocalSettingsDirectory(Guid settingsId)
        {
            var docFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            var easyRunFolder = Path.Combine(docFolder, "EasyRun", settingsId.ToString());

            if (!Directory.Exists(easyRunFolder))
            {
                Directory.CreateDirectory(easyRunFolder);
            }
            return easyRunFolder;
        }

        private string GetSettingsFilename()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            Assumes.Present(dte);

            var solutionFilename = GetSolutionFilename(dte);

            return string.IsNullOrEmpty(solutionFilename) ? null : solutionFilename + ".EasyRun.json";
        }

        private string GetSolutionFilename()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE2;
            Assumes.Present(dte);

            return GetSolutionFilename(dte);
        }

        public static string GetSolutionFilename(DTE2 dte)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Assumes.Present(dte);

            var solutionFilename = dte.Solution.FileName;

            if (string.IsNullOrEmpty(solutionFilename))
            {
                return null;
            }

            return solutionFilename;
        }

        public static string GetRelativePathToSolution(DTE2 dte, string path)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var solutionFilename = GetSolutionFilename(dte);

            if (solutionFilename == null)
            {
                return path;
            }

            var solutionDirectory = Path.GetDirectoryName(solutionFilename);

            return PathUtility.GetRelativePath(path, solutionDirectory, false);
        }

        private string CalcProfileChecksum(EasyRunModel model)
        {
            using (var shaManager = new SHA256Managed())
            {
                var checksumValue = model.SelectedProfileName;
                checksumValue += JsonConvert.SerializeObject(model);

                var encodedChecksumValue = Encoding.UTF8.GetBytes(checksumValue);
                return BitConverter.ToString(shaManager.ComputeHash(encodedChecksumValue));
            }
        }
    }
}
