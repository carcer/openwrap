﻿extern alias resharper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenFileSystem.IO;
using OpenWrap.PackageManagement.Exporters;
using OpenWrap.PackageManagement.Monitoring;
using OpenWrap.Runtime;
using OpenWrap.Services;
using JetBrainsKey = resharper::JetBrains.Util.Key;


namespace OpenWrap.Resharper
{
    internal class ResharperProjectUpdater : IResolvedAssembliesUpdateListener
    {
        static readonly JetBrainsKey ISWRAP = new JetBrainsKey("FromOpenWrap");
        readonly Func<ExecutionEnvironment> _env;
        readonly IEnumerable<string> _ignoredAssemblies;
        readonly object _lock = new object();
        readonly resharper::JetBrains.ProjectModel.IProject _project;

        public ResharperProjectUpdater(IFile descriptor, resharper::JetBrains.ProjectModel.IProject project, Func<ExecutionEnvironment> env)
        {
            _project = project;
            _ignoredAssemblies = ReadIgnoredAssemblies();
            Descriptor = descriptor;
            _env = env;
        }
        public string ProjectPath { get { return _project.ProjectFile.Location.FullPath; } }
        public ExecutionEnvironment Environment
        {
            get { return _env(); }
        }

        public bool IsLongRunning
        {
            get { return true; }
        }

        public IFile Descriptor { get; private set; }

        public void AssembliesUpdated(IEnumerable<IAssemblyReferenceExportItem> resolvedAssemblies)
        {
            ResharperLocks.WriteCookie("Updating references...",
                                       () =>
                                       {
                                           string projectFilePath = _project.ProjectFile.GetPresentableProjectPath();

                                           var resolvedAssemblyPaths = resolvedAssemblies.Select(x => x.FullPath).ToList();

                                           var owProjectAssemblyReferences = _project.GetAssemblyReferences()
                                                   .Where(x => x.GetProperty(ISWRAP) != null).ToList();

                                           var owProjectAssemblyReferencePaths = owProjectAssemblyReferences
                                                   .Select(x => x.HintLocation.FullPath).ToList();

                                           foreach (var path in resolvedAssemblyPaths
                                                   .Where(x => !owProjectAssemblyReferencePaths.Contains(x) &&
                                                               _ignoredAssemblies.Any(i => x.Contains(i + ".dll")) == false))
                                           {
                                               ResharperLogger.Debug("Adding reference {0} to {1}", projectFilePath, path);

                                               var assembly = _project.AddAssemblyReference(path);
                                               assembly.SetProperty(ISWRAP, true);
                                           }
                                           foreach (var toRemove in owProjectAssemblyReferencePaths.Where(x => !resolvedAssemblyPaths.Contains(x)))
                                           {
                                               ResharperLogger.Debug("Removing reference {0} to {1}",
                                                                     projectFilePath,
                                                                     toRemove);
                                               _project.RemoveModuleReference(
                                                       owProjectAssemblyReferences.First(
                                                               x => x.HintLocation.FullPath == toRemove));
                                           }
                                       });
        }

        static IEnumerable<string> ReadIgnoredAssemblies()
        {
            // TODO: https://github.com/openrasta/openwrap/issues/issue/125
            return new string[0];
        }
    }

    public static class ResharperLocks
    {
        public static void WriteCookie(string description, Action invoke)
        {
            resharper::JetBrains.Application.Shell.Instance.Invocator.ReentrancyGuard.Dispatcher
                    .Invoke(description,
                            () => resharper::JetBrains.Application.Shell.Instance.Invocator.ReentrancyGuard.ExecuteOrQueue
                                          (
                                                  description,
                                                  () => invoke()));
        }
    }

    internal static class ResharperLogger
    {
        public static void Debug(string text, params string[] args)
        {
            Debugger.Log(0, "resharper", DateTime.Now.ToShortTimeString() + ":" + string.Format(text, args) + "\r\n");
        }
    }
}