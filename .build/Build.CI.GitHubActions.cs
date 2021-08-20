using Nuke.Common.CI.GitHubActions;

[GitHubActions("CreatePackageTest",
    GitHubActionsImage.WindowsLatest,
    PublishArtifacts = true,
    InvokedTargets = new[] { nameof(InitializeBuilder), nameof(Restore), nameof(Cleaning), nameof(Compile), nameof(CreateInstaller), nameof(CreateBundle), nameof(ZipBundle) },
    OnPullRequestBranches = new[] { "main" },
    OnPushBranches = new[] { "main" })]
partial class Build
{
}