using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.MSBuild;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
// [AzurePipelines(AzurePipelinesImage.WindowsLatest, InvokedTargets = new[] { nameof(InitializeBuilder) })]
partial class Build : NukeBuild
{
    [Solution] readonly Solution Solution;
    AbsolutePath BundleDirectory;
    ProjectInfo InstallerInfo;
    AbsolutePath OutputDirectory;
    ProjectInfo ProjectInfo;

    Target InitializeBuilder => _ => _
        .Executes(() =>
        {
            InstallerInfo   = new ProjectInfo(Solution, "Installer");
            ProjectInfo     = new ProjectInfo(Solution, "FamilyUpdater");
            OutputDirectory = RootDirectory / "output";
            BundleDirectory = OutputDirectory / $"{ProjectInfo.ProjectName}.bundle";
        });

    Target Cleaning => _ => _
        .TriggeredBy(InitializeBuilder)
        .Executes(() =>
        {
            if (!Directory.Exists(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
                return;
            }

            var directoryInfo = new DirectoryInfo(OutputDirectory);
            foreach (var file in directoryInfo.GetFiles()) file.Delete();
            foreach (var dir in directoryInfo.GetDirectories()) dir.Delete(true);
        });

    Target Restore => _ => _
        .TriggeredBy(Cleaning)
        .Executes(() =>
        {
            MSBuild(s => s
                .SetTargetPath(Solution)
                .SetTargets("Restore"));
        });

    Target Compile => _ => _
        .TriggeredBy(Restore)
        .Executes(() =>
        {
            var releaseConfigurations = GetReleaseConfigurations();
            if (releaseConfigurations.Count == 0) throw new Exception("There are no configurations in the project.");
            foreach (var configuration in releaseConfigurations) BuildProject(configuration);
        });

    Target CreateInstaller => _ => _
        .TriggeredBy(Compile)
        .Executes(() =>
        {
            var proc = new Process();
            proc.StartInfo.FileName  = InstallerInfo.ExecutableFile;
            proc.StartInfo.Arguments = $"\"{ProjectInfo.BinDirectory}\"";
            proc.Start();
        });

    Target CreateBundle => _ => _
        .TriggeredBy(Compile)
        .Executes(() =>
        {
            var addInsDirectory = new DirectoryInfo(ProjectInfo.BinDirectory).GetDirectories()
                .Where(dir => dir.Name.StartsWith("AddIn"))
                .ToList();

            if (addInsDirectory.Count == 0) throw new Exception("There are no packaged assemblies in the project. Try to build the project again.");
            var contentDirectory = BundleDirectory / "Contents";
            var versionPattern = new Regex(@"\d+");
            foreach (var directoryInfo in addInsDirectory)
            {
                var version = versionPattern.Match(directoryInfo.Name).Value;
                if (string.IsNullOrEmpty(version))
                {
                    Logger.Warn($"Missing version number for build \"{directoryInfo.Name}\"");
                    continue;
                }

                var buildDirectory = contentDirectory / version;
                CopyFilesContent(directoryInfo.FullName, buildDirectory);
            }
        });

    Target ZipBundle => _ => _
        .TriggeredBy(CreateBundle)
        .Executes(() =>
        {
            var archiveName = $"{BundleDirectory}.zip";
            ZipFile.CreateFromDirectory(BundleDirectory, archiveName);
        });

    public static int Main() => Execute<Build>(x => x.InitializeBuilder);

    List<string> GetReleaseConfigurations() =>
        Solution.Configurations
            .Select(pair => pair.Key)
            .Where(s => s.StartsWith("Release"))
            .Select(s => s.Replace("|Any CPU", ""))
            .ToList();

    static void CopyFilesContent(string sourcePath, string targetPath)
    {
        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
    }

    void BuildProject(string configuration) =>
        MSBuild(s => s
                .SetTargetPath(Solution)
                .SetTargets("Rebuild")
                .SetConfiguration(configuration)
            // .SetMSBuildPlatform(MSBuildPlatform.x64)
            // .SetMaxCpuCount(Environment.ProcessorCount)
            // .DisableNodeReuse()
        );
}