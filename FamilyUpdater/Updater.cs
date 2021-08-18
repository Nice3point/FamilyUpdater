using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.WindowsAPICodePack.Dialogs;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using TaskDialogResult = Autodesk.Revit.UI.TaskDialogResult;

namespace FamilyUpdater
{
    [Transaction(TransactionMode.ReadOnly)]
    public class Updater : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var fileDialog = new CommonOpenFileDialog
            {
                Title           = "Папку выбери, ладно можешь несколько",
                IsFolderPicker  = true,
                Multiselect     = true,
                ShowHiddenItems = true
            };
            if (fileDialog.ShowDialog() != CommonFileDialogResult.Ok) return Result.Cancelled;
            var folders = fileDialog.FileNames.ToList();

            var recursiveDir = TaskDialog.Show("Там вопрос снизу", "В подпапках ищем?", TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);
            var searchOption = recursiveDir == TaskDialogResult.Ok ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            var logger = new Logger();
            foreach (var folder in folders)
            {
                var files = Directory.GetFiles(folder, "*.*", searchOption).GetFilteredFiles();

                foreach (var file in files)
                    try
                    {
                        var fileName = Path.GetFileName(file);
                        var path = Path.GetDirectoryName(file);
                        var savedFolder = Path.Combine(path!, commandData.Application.Application.VersionNumber);
                        var savedFile = Path.Combine(savedFolder, fileName);
                        var document = commandData.Application.Application.OpenDocumentFile(file);

                        var saveAsOptions = new SaveAsOptions { OverwriteExistingFile = true };
                        if (!Directory.Exists(savedFolder)) Directory.CreateDirectory(savedFolder);
                        document.SaveAs(savedFile, saveAsOptions);
                    }
                    catch (Exception e)
                    {
                        var errorBuilder = new StringBuilder();
                        errorBuilder.Append(new string('=', 20));
                        errorBuilder.Append("Ошибка №");
                        errorBuilder.Append(logger.RecordNumbers + 1);
                        errorBuilder.Append(new string('=', 20));
                        errorBuilder.Append("\n");
                        errorBuilder.Append(e.Message);
                        errorBuilder.Append("\n");
                        errorBuilder.Append("\n");
                        logger.AppendText(errorBuilder.ToString());
                    }
            }

            if (logger.HaveLogData) Process.Start(logger.LogPath);

            return Result.Succeeded;
        }
    }
}