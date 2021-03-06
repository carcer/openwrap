﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using OpenWrap;
using OpenWrap.Commands.contexts;
using OpenWrap.Commands.Wrap;
using OpenWrap.PackageManagement;
using OpenWrap.Testing;

namespace listWrap_specs
{
    public class filtering_project_package_list_by_name : command_context<ListWrapCommand>
    {
        public filtering_project_package_list_by_name()
        {
            given_project_package("one-ring", "1.0");
            given_project_package("sauron", "2.0");
            given_dependency("depends: one-ring");
            given_dependency("depends: sauron");

            when_executing_command("one*");
        }
        [Test]
        public void matching_package_is_returned()
        {
            Results.OfType<PackageFoundCommandOutput>()
                    .ShouldHaveCountOf(1)
                    .First().Name.ShouldBe("one-ring");
        }
    }
    public class filtering_project_with_different_casing : command_context<ListWrapCommand>
    {
        public filtering_project_with_different_casing()
        {
            given_project_package("one-ring", "1.0");
            given_dependency("depends: one-ring");

            when_executing_command("*Rin*");
        }
        [Test]
        public void casing_is_ignored()
        {
            Results.OfType<PackageFoundCommandOutput>()
                    .ShouldHaveCountOf(1)
                    .First().Name.ShouldBe("one-ring");
        }
    }
    public class listing_packages_from_all_repositories : command_context<ListWrapCommand>
    {
        public listing_packages_from_all_repositories()
        {
            given_remote_repository("first");
            given_remote_repository("second");
            given_remote_package("first", "one-ring", "1.0.0".ToVersion());
            given_remote_package("second", "ring-of-power", "1.0.0".ToVersion());

            when_executing_command("ring", "-remote");
        }

        [Test]
        public void packages_are_found_in_any_remote()
        {
            Results.OfType<PackageFoundCommandOutput>()
                    .ShouldHaveCountOf(2)
                    .Check(x => x.ShouldHaveAtLeastOne(n => n.Name.Equals("one-ring")))
                    .Check(x => x.ShouldHaveAtLeastOne(n => n.Name.Equals("ring-of-power")));
        }
    }
    public class listing_packages_with_detailed_option : command_context<ListWrapCommand>
    {
        public listing_packages_with_detailed_option()
        {
            given_project_package("one-ring", "1.1", "desc 1.1");
            given_project_package("sauron", "2.0");
            given_dependency("depends: one-ring");
            given_dependency("depends: sauron");

            when_executing_command("one*", "-detailed");
        }
        [Test]
        public void correct_description_is_returned()
        {
            Results.OfType<PackageFoundCommandOutput>()
                    .ShouldHaveCountOf(1)
                    .First().ToString().ShouldContain("desc 1.1");
        }
    }

    public class listing_packages_will_hightlight_current : command_context<ListWrapCommand>
    {
        public listing_packages_will_hightlight_current()
        {
            given_project_package("one-ring", "1.0");
            given_remote_package("one-ring", "1.0".ToVersion());
            given_remote_package("one-ring", "1.1".ToVersion());
            given_remote_package("one-ring-rules-them-all", "1.1".ToVersion());
            given_project_package("sauron", "2.0");
            given_dependency("depends: one-ring");
            given_dependency("depends: sauron");

            when_executing_command("*n*", "-remote");
        }
        [Test]
        public void correct_version_is_highlighted()
        {
            var s = Results.OfType<PackageFoundCommandOutput>()
                    .ShouldHaveCountOf(2)
                    .First().ToString();
            Console.WriteLine(s);
            s.ShouldContain("current: 1.0");
        }

        [Test]
        public void current_version_is_still_listed_as_available()
        {
            Results.OfType<PackageFoundCommandOutput>()
                    .ShouldHaveCountOf(2)
                    .First().ToString().ShouldContain("available: 1.0, 1.1");
        }
        [Test]
        public void current_version_list_only_for_installed_package()
        {
            Results.OfType<PackageFoundCommandOutput>()
                    .ShouldHaveCountOf(2)
                    .ElementAt(1).ToString().ShouldNotContain("current: ");
        }
    }
}
