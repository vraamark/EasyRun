﻿using EasyRun.Exceptions;
using EasyRun.Logging;
using EasyRun.Models;
using EasyRun.Settings;
using EasyRun.Tye.DTOS;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace EasyRun.Tye
{
    public class TyeManager
    {
        private const int TyeQueryInterval = 1;

        private readonly string[] newLines = new[] { "\r\n", "\r", "\n" };

        private string loggerArg = string.Empty;
        private bool tyeQuerySubscribed;
        private IDisposable tyeQuery;

        private System.Diagnostics.Process tyeProcess;

        private Action processExitedAction;

        private Guid instanceId;

        public bool TyeIsRunning { get; set; }
        public bool TyeIsStopping { get; set; }

        public string BuildTyeManifest(DTE2 dte, ProfileModel profile, Guid instanceId)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            dte.ExecuteCommand("File.SaveAll");

            var solutionFilename = SettingsManager.GetSolutionFilename(dte);
            if (string.IsNullOrEmpty(solutionFilename))
            {
                Logger.LogActive("Please open a solution before running Tye.");
                return null;
            }

            if (profile is null || !profile.UseTye)
            {
                Logger.LogActive("Select a Tye profile before running Tye.");
                return null;
            }

            this.instanceId = instanceId;

            StringBuilder yaml = new StringBuilder();

            var solutionPath = Path.GetDirectoryName(solutionFilename);
            var solutionName = Path.GetFileNameWithoutExtension(solutionFilename);

            try
            {
                // Service Name
                yaml.Append("# tye application configuration file generated by EasyRun")
                    .AppendLine()
                    .Append("#")
                    .AppendLine()
                    .AppendFormat("name: {0}", solutionName.ToLower())
                    .AppendLine()
                    .AppendLine();

                // Logging
                AddLogging(yaml, profile.LoggingTargetType, profile.LoggingPath, profile.LoggerUrl, out loggerArg);

                // Services
                yaml.Append("services:")
                    .AppendLine();

                foreach (var service in profile.FilteredServices.Where(w => w.Selected))
                {
                    var projectFile = PathUtility.GetAbsolutePath(solutionPath, service.ProjectFile);
                    string serviceName = BuildTyeServiceName(service);

                    yaml.AppendFormat("- name: {0}", serviceName)
                        .AppendLine()
                        .AppendFormat("  project: {0}", projectFile)
                        .AppendLine();

                    AddBindings(yaml, service.Bindings);
                    AddReplicas(yaml, service.Replicas);
                    AddArgs(yaml, service.Arguments);
                    AddEnvVariables(yaml, service.EnvVariables, service.SecretEnvVariables);
                }

                var yamlFilename = GetTyeYamlFilename();
                File.WriteAllText(yamlFilename, yaml.ToString());

                return yamlFilename;
            }
            catch (TyeManifestException ex)
            {
                Logger.LogActive(ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return null;
            }
        }

        public bool IsTyeHostRunning(ProfileModel profile)
        {
            var tyePort = GetTyeHostPort(profile);

            return TyeHostApi.IsTyeHostRunning(tyePort);
        }

        public void RunTye(DTE2 dte, ProfileModel profile, string yamlFilename, Action processStarted, Action processExited)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ResetDebuggerAttachedInfo(profile);

            if (profile is null || !profile.UseTye)
            {
                Logger.LogActive("Select a Tye profile before running Tye.");
                return;
            }
            
            processExitedAction = processExited;

            tyeProcess = new System.Diagnostics.Process();

            var tyePort = GetTyeHostPort(profile);

            var tyeArgs = $"run {yamlFilename} --port {tyePort}";

            tyeArgs = ConcatTyeRunArgs(tyeArgs, "--debug *", profile.AttachDebugger && profile.WaitOnAttachDebugger);
            tyeArgs = ConcatTyeRunArgs(tyeArgs, "--watch", profile.Watch && !profile.AttachDebugger);
            tyeArgs = ConcatTyeRunArgs(tyeArgs, loggerArg, true);

            tyeProcess.StartInfo.FileName = "tye";
            tyeProcess.StartInfo.Arguments = tyeArgs;
            tyeProcess.EnableRaisingEvents = true;
            tyeProcess.Exited += TyeProcessExited;

            //TODO: Log errors if start fails.

            if (tyeProcess.Start())
            {
                TyeIsStopping = false;
                TyeIsRunning = true;
                if (profile.AttachDebugger)
                {
                    tyeQuery = Observable.Interval(TimeSpan.FromSeconds(TyeQueryInterval))
                        .Select(number => Observable.FromAsync(async () => await QueryTyeServicesAsync(dte, number, profile)))
                        .Concat()
                        .Subscribe();
                    tyeQuerySubscribed = true;
                }
                processStarted?.Invoke();
            }
            else
            {
                tyeProcess.Exited -= TyeProcessExited;
                TyeIsStopping = false;
                TyeIsRunning = false;
                processExitedAction();
            }
        }

        public void StopTye(ProfileModel profile)
        {
            if (TyeIsRunning && tyeProcess != null && !tyeProcess.HasExited)
            {
                TyeIsStopping = true;

                ThreadHelper.JoinableTaskFactory.Run(async delegate
                {
                    await TyeHostApi.ShutdownTyeHostAsync(GetTyeHostPort(profile));
                });
            }
        }

        public void ResetDebuggerAttachedInfo(ProfileModel profile)
        {
            if (profile != null)
            {
                foreach (var service in profile.Services)
                {
                    service.DebuggerIsAttached = false;
                }
            }
        }

        private string BuildTyeServiceName(ServiceModel service)
        {
            if (string.IsNullOrEmpty(service.TyeName))
            {
                return service.Name.Replace(".", "-").ToLower();
            }
            else
            {
                return service.TyeName.Replace(".", "-").ToLower();
            }
        }

        public int GetTyeHostPort(ProfileModel profile)
        {
            return profile?.TyePort == 0 ? 8000 : profile.TyePort;
        }

        private async System.Threading.Tasks.Task<bool> QueryTyeServicesAsync(DTE2 dte, long number, ProfileModel profile)
        {
            var tyeServices = await TyeHostApi.GetTyeServicesAsync(GetTyeHostPort(profile));

            if (tyeServices != null)
            {
                var expectedServices = profile.FilteredServices.Where(w => w.Selected).Sum(s => s.Replicas == 0 ? 1 : s.Replicas);

                var pidDic = GetPidList(tyeServices);
                
                if (expectedServices == pidDic.Count)
                {
                    UnsubscribeTyeQuery();

                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    var serviceDic = profile.Services.ToDictionary(k => BuildTyeServiceName(k), v => v);

                    foreach (Process2 process in dte.Debugger.LocalProcesses)
                    {
                        if (pidDic.TryGetValue(process.ProcessID, out var replicaItem))
                        {
                            process.Attach();

                            if (serviceDic.TryGetValue(replicaItem.ServiceName, out var service))
                            {
                                service.DebuggerIsAttached = true;
                            }
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public async System.Threading.Tasks.Task AttachDetachDebuggerAsync(DTE2 dte, ProfileModel profile, ServiceModel service)
        {
            var tyeServices = await TyeHostApi.GetTyeServicesAsync(GetTyeHostPort(profile));

            if (tyeServices != null)
            {
                var pidDic = GetPidList(tyeServices, BuildTyeServiceName(service));

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (service.DebuggerIsAttached)
                {
                    foreach (Process2 process in dte.Debugger.DebuggedProcesses)
                    {
                        if (pidDic.TryGetValue(process.ProcessID, out var replicaItem))
                        {
                            process.Detach(false);
                        }
                    }
                }
                else
                {
                    foreach (Process2 process in dte.Debugger.LocalProcesses)
                    {
                        if (pidDic.TryGetValue(process.ProcessID, out var replicaItem))
                        {
                            process.Attach();
                        }
                    }
                }

                service.DebuggerIsAttached = !service.DebuggerIsAttached;
            }
        }

        private Dictionary<int, ReplicaItem> GetPidList(List<TyeServiceDTO> tyeServices, string name = null)
        {
            var pidList = new Dictionary<int, ReplicaItem>();

            foreach (var service in tyeServices)
            {
                if (name is null || service.Description.Name == name)
                {
                    foreach (var replica in service.Replicas.Values)
                    {
                        pidList.Add(replica.Pid, new ReplicaItem { ServiceName = service.Description?.Name, ReplicaName = replica.Name });
                    }
                }
            }

            return pidList;
        }

        private string GetTyeYamlDirectory()
        {
            var tmpPath = Path.GetTempPath();
            var yamlPath = Path.Combine(tmpPath, "easyrun", instanceId.ToString());

            if (!Directory.Exists(yamlPath))
            {
                Directory.CreateDirectory(yamlPath);
            }

            return yamlPath;
        }

        private string GetTyeYamlFilename()
        {
            return Path.Combine(GetTyeYamlDirectory(), "tye-easyrun.yaml");
        }

        private void TyeProcessExited(object sender, System.EventArgs e)
        {
            TyeIsStopping = false;
            TyeIsRunning = false;

            UnsubscribeTyeQuery();

            tyeProcess.Exited -= TyeProcessExited;
            
            ThreadHelper.JoinableTaskFactory.Run(async delegate {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                processExitedAction?.Invoke();

                try
                {
                    var yamlFileName = GetTyeYamlFilename();

                    if (!string.IsNullOrEmpty(yamlFileName) && File.Exists(yamlFileName))
                    {
                        File.Delete(yamlFileName);
                    }

                    var yamlDirectory = GetTyeYamlDirectory();
                    if (!string.IsNullOrEmpty(yamlDirectory))
                    {
                        Directory.Delete(yamlDirectory, true);
                    }

                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            });
        }

        private void UnsubscribeTyeQuery()
        {
            if (tyeQuerySubscribed)
            {
                tyeQuerySubscribed = false;
                tyeQuery?.Dispose();
            }
        }

        private void AddLogging(StringBuilder yaml, LoggingTargetType loggingTargetType, string loggingPath, string loggerUrl, out string loggerArg)
        {
            loggerArg = string.Empty;

            var target = LoggingTargetModel.GetDefault().First(f => f.TargetType == loggingTargetType);

            if (target.AsExtension)
            {
                if (string.IsNullOrEmpty(loggingPath))
                {
                    loggingPath = "./.logs";
                }

                yaml.Append("extensions:")
                    .AppendLine()
                    .AppendFormat("- name: {0}", target.YamlName)
                    .AppendLine()
                    .AppendFormat("  logPath: {0}", loggingPath)
                    .AppendLine()
                    .AppendLine();
            }
            else if (loggingTargetType != LoggingTargetType.Console)
            {
                if (string.IsNullOrEmpty(loggerUrl))
                {
                    throw new TyeManifestException("The selected logger type need and logger url.");
                }
                else
                {
                    loggerArg = $" --logs {target.YamlName}={loggerUrl}";
                }
            }
        }

        private void AddBindings(StringBuilder yaml, string bindings)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!string.IsNullOrEmpty(bindings))
            {
                yaml.Append("  bindings:")
                    .AppendLine();

                var bindingLines = bindings.Split(newLines, StringSplitOptions.None);

                foreach (var binding in bindingLines)
                {
                    var indent = "- ";

                    var protocolPort = binding.Split(':');

                    if (protocolPort.Length != 3)
                    {
                        throw new TyeManifestException($"Invalid bindings: {binding}. Use one binding definition pr. line in the format [name]:[protocol]:[port]. Use ::8080 to only set port.");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(protocolPort[0]))
                        {
                            yaml.AppendFormat("  {0}name: {1}", indent, protocolPort[0])
                                .AppendLine();
                            indent = "  ";
                        }
                        else if (bindingLines.Length > 1)
                        {
                            throw new TyeManifestException("Cannot have multiple service bindings without names. Please specify names for each service binding. Format is [name]:[protocol]:[port]");
                        }


                        if (!string.IsNullOrEmpty(protocolPort[1]))
                        {
                            yaml.AppendFormat("  {0}protocol: {1}", indent, protocolPort[1])
                                .AppendLine();
                            indent = "  ";
                        }

                        if (!string.IsNullOrEmpty(protocolPort[2]))
                        {
                            yaml.AppendFormat("  {0}port: {1}", indent, protocolPort[2])
                                .AppendLine();
                            indent = "  ";
                        }
                    }
                }
            }
        }

        private void AddEnvVariables(StringBuilder yaml, string envVariables, string secretEnvVariables)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var envStr = string.Join("\n", envVariables, secretEnvVariables);

            var envs = envStr.Split(newLines,StringSplitOptions.RemoveEmptyEntries);

            if (envs.Length > 0)
            {
                yaml.Append("  env:")
                    .AppendLine();

                foreach (var line in envs)
                {
                    var keyValue = line.Split('=');
                    if (keyValue.Length != 2)
                    {
                        throw new TyeManifestException($"Invalid environment variable: {line}. The format is <key>=<value>.");
                    }
                    else
                    {
                        yaml.AppendFormat("  - {0}={1}", keyValue[0].Replace(":", "__"), keyValue[1])
                            .AppendLine();
                    }
                }
            }
        }

        private void AddArgs(StringBuilder yaml, string args)
        {
            if (!string.IsNullOrEmpty(args))
            {
                yaml.AppendFormat("  args: {0}", args)
                    .AppendLine();
            }
        }

        private void AddReplicas(StringBuilder yaml, int replicas)
        {
            if (replicas>1)
            {
                yaml.AppendFormat("  replicas: {0}", replicas)
                    .AppendLine();
            }
        }

        private string ConcatTyeRunArgs(string tyeRunArgs, string argsToConcat, bool doIt)
        {
            return doIt && !string.IsNullOrEmpty(argsToConcat) ? $"{tyeRunArgs} {argsToConcat}" : tyeRunArgs;
        }
    }
}
