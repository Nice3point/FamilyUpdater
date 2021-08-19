using Nuke.Common.CI.GitHubActions;

[GitHubActions("CreatePackage",
    GitHubActionsImage.WindowsLatest,
    AutoGenerate = true,
    OnPullRequestBranches = new[] { "main" })]
partial class Build
{
}