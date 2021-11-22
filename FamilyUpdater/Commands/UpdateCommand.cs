using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using FamilyUpdater.Core;
using Microsoft.WindowsAPICodePack.Dialogs;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using TaskDialogResult = Autodesk.Revit.UI.TaskDialogResult;

namespace FamilyUpdater.Commands
{
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
            fileDialog.SelectionChanged += (sender, args) => { };
            if (fileDialog.ShowDialog() != CommonFileDialogResult.Ok) return Result.Cancelled;
            var folders = fileDialog.FileNames.ToList();

            var recursiveDir = TaskDialog.Show("Options", "Search in subfolders?", TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);
            var searchOption = recursiveDir == TaskDialogResult.Yes ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var logger = new Logger();
            var application = commandData.Application.Application;
            var rootPath = Directory.GetParent(folders[0])!.FullName;
            var savedFolder = Path.Combine(rootPath, $"Revit {application.VersionNumber} families");
            if (!Directory.Exists(savedFolder)) Directory.CreateDirectory(savedFolder);

            foreach (var folder in folders)
            {
                var files = folder.GetFilteredFiles(searchOption);
                foreach (var file in files)
                    try
                    {
                        var fileName = Path.GetFileName(file);
                        var savedFilePath = Path.Combine(savedFolder, fileName);

                        var document = application.OpenDocumentFile(file);
                        var saveAsOptions = new SaveAsOptions {OverwriteExistingFile = true};
                        document.SaveAs(savedFilePath, saveAsOptions);
                        document.Close(false);
                    }
                    catch (Exception e)
                    {
                        var errorBuilder = new StringBuilder();
                        errorBuilder.Append(new string('=', 20));
                        errorBuilder.Append("Error №");
                        errorBuilder.Append(logger.RecordNumbers + 1);
                        errorBuilder.Append(" with file ");
                        errorBuilder.Append(file);
                        errorBuilder.Append(new string('=', 20));
                        errorBuilder.Append("\n");
                        errorBuilder.Append(e.Message);
                        errorBuilder.Append("\n");
                        errorBuilder.Append("\n");
                        logger.AppendText(errorBuilder.ToString());
                    }
            }

            Process.Start(savedFolder);
            if (logger.HaveLogData) Process.Start(logger.LogPath);

            return Result.Succeeded;
        }
    }
}