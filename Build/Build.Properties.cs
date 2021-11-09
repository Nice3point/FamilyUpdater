partial class Build
{
    const string InstallerProject = "Installer";

    public const string BuildConfiguration = "Release";
    public const string InstallerConfiguration = "Installer";
    public const string BundleConfiguration = "Store";

    const string AddInBinPrefix = "AddIn";
    const string ArtifactsFolder = "output";

    //The libraries below use the AfterBuild target
    //Change the version here if it is different from the one specified in the .csproj file
    const string WixTargetPath = @"%USERPROFILE%\.nuget\packages\wixsharp\1.18.1\build\WixSharp.targets";
    const string IlRepackTargetPath = @"%USERPROFILE%\.nuget\packages\ilrepack.lib.msbuild.task\2.0.18.2\build\ILRepack.Lib.MSBuild.Task.targets";

    //Specify the path to the MSBuild.exe file here if you are not using VisualStudio
    const string CustomMsBuildPath = @"C:\Program Files\JetBrains\JetBrains Rider\tools\MSBuild\Current\Bin\MSBuild.exe";

    readonly string[] Projects =
    {
        "FamilyUpdater"
    };
}