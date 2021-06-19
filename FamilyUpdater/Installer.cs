using System;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Controls;

namespace FamilyUpdater
{
    public static class Installer
    {
        private const string InstallationDir = @"%AppDataFolder%\Autodesk\Revit\Addins\";

        public static void Install()
        {
            const string fileMask = @"Addin\*.*";
            var project = new Project
            {
                Name         = "Family updater",
                OutFileName  = "FamilyUpdater",
                Version      = new Version(1, 0),
                Platform     = Platform.x64,
                UI           = WUI.WixUI_InstallDir,
                InstallScope = InstallScope.perUser,
                GUID         = new Guid("A269FC90-D5D6-41CA-93BB-ABA190F31E59"),
                Dirs = new[]
                {
                    new Dir($"{InstallationDir}",
                        new Dir("2020",
                            new Files(fileMask)),
                        new Dir("2021",
                            new Files(fileMask)),
                        new Dir("2022",
                            new Files(fileMask)))
                }
            };
            project.RemoveDialogsBetween(NativeDialogs.WelcomeDlg, NativeDialogs.InstallDirDlg);
            project.BuildMsi();
        }
    }
}