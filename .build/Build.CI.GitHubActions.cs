using Nuke.Common.CI.GitHubActions;

[GitHubActions("CreatePackage",
    GitHubActionsImage.WindowsLatest,
    OnPullRequestBranches = new[] { "main" },
    OnPushBranches = new[] { "main" })]
partial class Build
{
}