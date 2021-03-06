﻿using System;
using NUnit.Framework;
using OpenWrap.Commands.contexts;
using OpenWrap.Commands.Wrap;
using OpenWrap.Testing;
using CommandOutputExtensions = OpenWrap.Commands.CommandOutputExtensions;

namespace OpenWrap.Tests.Commands
{
    class adding_non_existant_wrap : command_context<AddWrapCommand>
    {
        public adding_non_existant_wrap()
        {
            given_currentdirectory_package("sauron", new Version(1, 0, 0));
            when_executing_command("-Name", "saruman");
        }
        [Test]
        public void package_installation_is_unsuccessfull()
        {
            Results.ShouldHaveAtLeastOne(x => CommandOutputExtensions.Success(x) == false);
        }
    }
}