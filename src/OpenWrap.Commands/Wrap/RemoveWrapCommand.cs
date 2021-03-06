﻿using System;
using System.Collections.Generic;
using System.Linq;
using OpenWrap.PackageManagement;
using OpenWrap.PackageModel;
using OpenWrap.Services;

namespace OpenWrap.Commands.Wrap
{
    [Command(Verb = "remove", Noun = "wrap")]
    public class RemoveWrapCommand : WrapCommand
    {
        [CommandInput(IsRequired = true, Position = 0)]
        public string Name { get; set; }

        bool? _project;

        [CommandInput]
        public bool Project
        {
            get { return _project ?? !System; }
            set { _project = value; }
        }

        [CommandInput]
        public bool System { get; set; }

        [CommandInput]
        public Version Version { get; set; }

        [CommandInput]
        public bool Clean { get; set; }

        [CommandInput]
        public string Scope { get; set; }

        bool? _last;

        [CommandInput]
        public bool Last
        {
            get { return _last ?? false; }
            set { _last = value; }
        }

        public override IEnumerable<ICommandOutput> Execute()
        {
            return Either(VerifyInputs()).Or(ExecuteCore());
        }

        IEnumerable<ICommandOutput> VerifyInputs()
        {
            if (Version != null && Last)
                yield return new Error("Cannot use '-Last' and '-Version' together.");
            if (System && !HostEnvironment.SystemRepository.PackagesByName[Name].Any())
                yield return new Error("Cannot find package named '{0}' in system repository.", Name);
            if (Project && HostEnvironment.ProjectRepository == null)
                yield return new Error("Not in a package directory.");
        }

        IEnumerable<ICommandOutput> ExecuteCore()
        {
            var options = Options;
            if (Project && Version != null)
                yield return new Warning("Because you have selected a specific version to remove from the project repository, your descriptor file will not be updated. To remove the dependency from your descriptor, either do not specify a version or use the set-wrap command to update the version of a package in use by your project.");
            var targetDescriptor = HostEnvironment.GetOrCreateScopedDescriptor(Scope);

            if (Project)
            {
                using (ChangeMonitor(targetDescriptor))
                {
                    foreach (var m in PackageManager.RemoveProjectPackage(PackageRequest, targetDescriptor.Value, HostEnvironment.ProjectRepository, options)) yield return ToOutput(m);
                    if (Successful) TrySaveDescriptorFile(targetDescriptor);
                }
            }
            if (System)
                foreach (var m in PackageManager.RemoveSystemPackage(PackageRequest, HostEnvironment.SystemRepository, Options)) yield return ToOutput(m);
        }


        protected PackageRemoveOptions Options
        {
            get
            {
                var options = PackageRemoveOptions.Default;
                //if (Clean)
                //    options |= PackageRemoveOptions.Clean;
                return options;
            }
        }

        protected PackageRequest PackageRequest
        {
            get
            {
                if (Last)
                    return PackageRequest.Last(Name);
                if (Version != null)
                    return PackageRequest.Exact(Name, Version);
                return PackageRequest.Any(Name);
            }
        }
    }
}
