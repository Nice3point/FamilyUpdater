using Nuke.Common;
using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    Target Compile => _ => _
        .TriggeredBy(Cleaning)
        .Executes(() =>
        {
            var configurations = GetConfigurations(BuildConfiguration, InstallerConfiguration);
            configurations.ForEach(configuration =>
            {
                DotNetBuild(settings => settings
                    .SetConfiguration(configuration)
                    .SetVerbosity(DotNetVerbosity.Minimal));
            });
        });
}