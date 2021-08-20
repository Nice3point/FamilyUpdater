using Nuke.Common.CI.GitHubActions;

[GitHubActions("CreatePackageTest",
    GitHubActionsImage.WindowsLatest,
    AutoGenerate = false,
    OnPullRequestBranches = new[] { "main" },
    OnPushBranches = new[] { "main" })]
partial class Build
{
//      - uses: actions/upload-artifact@v1
//        with:
//          name: output
//          path: output
}