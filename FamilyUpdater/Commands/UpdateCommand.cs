using System.Diagnostics;
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

[UsedImplicitly]
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
        var searchOption = recursiveDir == TaskDialogResult.Yes ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var logger = new Logger();
        var rootPath = Directory.GetParent(folders[0])!.FullName;
        var savedFolder = Path.Combine(rootPath, $"Revit {commandData.Application.Application.VersionNumber} families");
        if (!Directory.Exists(savedFolder)) Directory.CreateDirectory(savedFolder);

        UpdateFolders(folders, searchOption, savedFolder, commandData.Application, logger);

        Process.Start(savedFolder);
        if (logger.Records > 0) Process.Start(logger.LogPath);

        return Result.Succeeded;
    }

    private void UpdateFolders(List<string> folders, SearchOption searchOption, string savedFolder, UIApplication uiApplication, Logger logger)
    {
        try
        {
            uiApplication.Application.FailuresProcessing += ResolveFailures;
            uiApplication.DialogBoxShowing += ResolveDialogBox;
            foreach (var folder in folders)
            {
                var files = folder.GetFilteredFiles(searchOption);
                foreach (var file in files)
                {
                    try
                    {
                        UpdateFile(file, savedFolder, uiApplication.Application);
                    }
                    catch (Exception exception)
                    {
                        WriteException(logger, file, exception);
                    }
                }
            }
        }
        finally
        {
            uiApplication.Application.FailuresProcessing -= ResolveFailures;
            uiApplication.DialogBoxShowing -= ResolveDialogBox;
        }
    }

    private static void UpdateFile(string file, string savedFolder, Application application)
    {
        var fileName = Path.GetFileName(file);
        var savedFilePath = Path.Combine(savedFolder, fileName);

        var document = application.OpenDocumentFile(file);
        var saveAsOptions = new SaveAsOptions { OverwriteExistingFile = true };
        document.SaveAs(savedFilePath, saveAsOptions);
        document.Close(false);
    }

    private static void WriteException(Logger logger, string file, Exception e)
    {
        var errorBuilder = new StringBuilder();
        errorBuilder.Append(new string('=', 20));
        errorBuilder.Append("Error №");
        errorBuilder.Append(logger.Records + 1);
        errorBuilder.Append(" with file ");
        errorBuilder.Append(file);
        errorBuilder.Append(new string('=', 20));
        errorBuilder.Append("\n");
        errorBuilder.Append(e.Message);
        errorBuilder.Append("\n");
        errorBuilder.Append("\n");
        logger.AppendText(errorBuilder.ToString());
    }

    private void ResolveDialogBox(object sender, DialogBoxShowingEventArgs args)
    {
        args.OverrideResult(1);
    }

    private void ResolveFailures(object sender, FailuresProcessingEventArgs args)
    {
        args.GetFailuresAccessor().DeleteAllWarnings();
    }
}