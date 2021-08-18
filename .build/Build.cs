using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.MSBuild;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

[CheckBuildProjectConfigurations]
class Build : NukeBuild
{
    const string ProjectName = "FamilyUpdater";
    const string InstallerName = "Installer";

    [Solution] readonly Solution Solution;
    string _outputDirectory;

    Target Cleaning => _ => _
        .Executes(() =>
        {
            if (!Directory.Exists(OutputDirectory)) return;
            var directoryInfo = new DirectoryInfo(OutputDirectory);
            foreach (var file in directoryInfo.GetFiles()) file.Delete();
            foreach (var dir in directoryInfo.GetDirectories()) dir.Delete(true);
        });

    Target Compile => _ => _
        .TriggeredBy(Cleaning)
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
            var installerExe = GetExeDirectory(InstallerName);
            var projectDirectory = GetBinDirectory(ProjectName);
            var proc = new Process();
            proc.StartInfo.FileName  = installerExe;
            proc.StartInfo.Arguments = $"\"{projectDirectory}\"";
            proc.Start();
        });

    Target CreateBundle => _ => _
        .TriggeredBy(Compile)
        .Executes(() =>
        {
            var projectDirectory = GetBinDirectory(ProjectName);
            var addInsDirectory = new DirectoryInfo(projectDirectory).GetDirectories()
                .Where(dir => dir.Name.StartsWith("AddIn"))
                .ToList();

            if (addInsDirectory.Count == 0) throw new Exception("There are no packaged assemblies in the project. Try to build the project again.");

            var bundleDirectory = GetBundleDirectory();
            var contentDirectory = Path.Combine(bundleDirectory, "Contents");

            var versionPattern = new Regex(@"\d+");
            foreach (var directoryInfo in addInsDirectory)
            {
                var version = versionPattern.Match(directoryInfo.Name).Value;
                if (string.IsNullOrEmpty(version))
                {
                    Logger.Warn($"Missing version number for build \"{directoryInfo.Name}\"");
                    continue;
                }

                var buildDirectory = Path.Combine(contentDirectory, version);
                CopyFilesContent(directoryInfo.FullName, buildDirectory);
            }
        });

    Target ZipBundle => _ => _
        .TriggeredBy(CreateBundle)
        .Executes(() =>
        {
            var bundleDirectory = GetBundleDirectory();
            var archiveName = $"{bundleDirectory}.zip";
            ZipFile.CreateFromDirectory(bundleDirectory, archiveName);
        });

    string OutputDirectory => _outputDirectory ??= Path.Combine(Solution.Directory!, "output");

    public static int Main() => Execute<Build>(x => x.Cleaning);

    string GetBundleDirectory()
    {
        var bundleName = $"{ProjectName}.bundle";
        var bundleDirectory = Path.Combine(OutputDirectory, bundleName);
        return bundleDirectory;
    }

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

    string GetBinDirectory(string projectName)
    {
        var project = Solution.GetProject(projectName);
        if (project == null) throw new NullReferenceException($"Cannon find project \"{projectName}\"");
        return Path.Join(project.Directory, "bin");
    }

    string GetExeDirectory(string projectName)
    {
        var binDirectory = GetBinDirectory(projectName);
        var releaseDirectory = Path.Join(binDirectory, "Release");
        return Path.Join(releaseDirectory, $"{projectName}.exe");
    }

    void BuildProject(string configuration) =>
        MSBuild(s => s
            .SetTargetPath(Solution)
            .SetConfiguration(configuration)
            .SetMSBuildPlatform(MSBuildPlatform.x64)
            .SetMaxCpuCount(Environment.ProcessorCount)
            .DisableNodeReuse());
}