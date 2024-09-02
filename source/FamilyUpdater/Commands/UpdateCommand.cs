using System.Diagnostics;
using System.IO;
using System.Text;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using FamilyUpdater.Core;
using Microsoft.WindowsAPICodePack.Dialogs;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using TaskDialogResult = Autodesk.Revit.UI.TaskDialogResult;

namespace FamilyUpdater.Commands;

[Transaction(TransactionMode.ReadOnly)]
public class UpdateCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        var fileDialog = new CommonOpenFileDialog
        {
            Title = "Selecting folders",
            IsFolderPicker = true,
            Multiselect = true,
            ShowHiddenItems = true,
            RestoreDirectory = true,
            EnsurePathExists = true
        };

        if (fileDialog.ShowDialog() != CommonFileDialogResult.Ok) return Result.Cancelled;

        var folders = fileDialog.FileNames.ToList();
        var recursiveDir = TaskDialog.Show("Options", "Search in subfolders?", TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

        var logger = new Logger();
        var rootPath = Directory.GetParent(folders[0])!.FullName;
        var application = commandData.Application.Application;
        var savedFolder = Path.Combine(rootPath, $"Revit {application.VersionNumber} models");
        var updater = new ModelsUpdater(application, logger);

        try
        {
            application.FailuresProcessing += ResolveFailures;
            commandData.Application.DialogBoxShowing += ResolveDialogBox;

            updater.UpdateFiles(folders, savedFolder, recursiveDir == TaskDialogResult.Yes);
        }
        finally
        {
            application.FailuresProcessing -= ResolveFailures;
            commandData.Application.DialogBoxShowing -= ResolveDialogBox;
        }

        Process.Start(savedFolder);
        if (logger.Records > 0) Process.Start(logger.LogPath);

        return Result.Succeeded;
    }

    private static void ResolveDialogBox(object? sender, DialogBoxShowingEventArgs args)
    {
        args.OverrideResult(1);
    }

    private static void ResolveFailures(object? sender, FailuresProcessingEventArgs args)
    {
        args.GetFailuresAccessor().DeleteAllWarnings();
    }
}