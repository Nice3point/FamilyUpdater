using System.IO;
using System.Text;
using Autodesk.Revit.ApplicationServices;

namespace FamilyUpdater.Core;

public sealed class ModelsUpdater(Application application, Logger logger)
{
    public void UpdateFiles(List<string> sourceFolders, string destinationFolder, bool recursiveSearch)
    {
        if (!Directory.Exists(destinationFolder)) Directory.CreateDirectory(destinationFolder);

        foreach (var folder in sourceFolders)
        {
            var files = folder.GetRevitFiles(recursiveSearch);
            foreach (var file in files)
            {
                try
                {
                    UpdateFile(file, destinationFolder);
                }
                catch (Exception exception)
                {
                    WriteException(logger, file, exception);
                }
            }
        }
    }
    
    private void UpdateFile(string file, string savedFolder)
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
}