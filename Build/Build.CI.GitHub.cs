using System.Text;
using Nuke.Common.Git;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.GitHub;
using Octokit;

sealed partial class Build
{
    Target PublishGitHub => _ => _
        .DependsOn(CreateInstaller)
        .Requires(() => GitHubToken)
        .Requires(() => GitRepository)
        .OnlyWhenStatic(() => IsServerBuild && GitRepository.IsOnMainOrMasterBranch())
        .Executes(async () =>
        {
            GitHubTasks.GitHubClient = new GitHubClient(new ProductHeaderValue(Solution.Name))
            {
                Credentials = new Credentials(GitHubToken)
            };

            var gitHubName = GitRepository.GetGitHubName();
            var gitHubOwner = GitRepository.GetGitHubOwner();

            Validate();

            var artifacts = Directory.GetFiles(ArtifactsDirectory, "*");
            var changelog = CreateChangelog(Version);

            var newRelease = new NewRelease(Version)
            {
                Name = Version,
                Body = changelog,
                TargetCommitish = GitRepository.Commit
            };

            var release = await GitHubTasks.GitHubClient.Repository.Release.Create(gitHubOwner, gitHubName, newRelease);
            await UploadArtifactsAsync(release, artifacts);
        });

    static void Validate()
    {
        var tags = GitTasks.Git("tag --list", logger:(_, _) => {});
        Assert.False(tags.Any(tag => tag.Text == Version), $"A Release with the specified tag already exists in the repository: {Version}");

        Log.Information("Version: {Version}", Version);
    }

    string CreateChangelog(string version)
    {
        if (!File.Exists(ChangeLogPath))
        {
            Log.Warning("Unable to locate the changelog file: {Log}", ChangeLogPath);
            return string.Empty;
        }

        Log.Information("Changelog: {Path}", ChangeLogPath);

        var changelog = ReadChangelog(version);
        if (changelog.Length == 0)
        {
            Log.Warning("No version entry exists in the changelog: {Version}", version);
            return string.Empty;
        }

        WriteCompareUrl(version, changelog);
        return changelog.ToString();
    }

    void WriteCompareUrl(string version, StringBuilder changelog)
    {
        var tags = GitTasks.Git("tag --list", logger:(_, _) => {});
        if (tags.Count == 0) return;

        changelog.Append("Full changelog: ");
        changelog.Append(GitRepository.GetGitHubCompareTagsUrl(version, tags.Last().Text));
    }

    StringBuilder ReadChangelog(string version)
    {
        const char separator = '#';

        var hasEntry = false;
        var changelog = new StringBuilder();
        foreach (var line in File.ReadLines(ChangeLogPath))
        {
            if (hasEntry)
            {
                if (line.StartsWith(separator)) break;
                if (line == string.Empty) continue;

                if (changelog.Length > 0) changelog.AppendLine();
                changelog.Append(line);
                continue;
            }

            if (line.StartsWith(separator) && line.Contains(version))
            {
                hasEntry = true;
            }
        }

        return changelog;
    }

    static async Task UploadArtifactsAsync(Release createdRelease, IEnumerable<string> artifacts)
    {
        foreach (var file in artifacts)
        {
            var releaseAssetUpload = new ReleaseAssetUpload
            {
                ContentType = "application/x-binary",
                FileName = Path.GetFileName(file),
                RawData = File.OpenRead(file)
            };

            await GitHubTasks.GitHubClient.Repository.Release.UploadAsset(createdRelease, releaseAssetUpload);
            Log.Information("Artifact: {Path}", file);
        }
    }
}