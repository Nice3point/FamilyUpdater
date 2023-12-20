namespace FamilyUpdater.Core;

public class Logger
{
    private const string FileName = "Revit-FamilyUpdater.log";

    public Logger()
    {
        LogPath = Path.Combine(Path.GetTempPath(), FileName);
        if (File.Exists(LogPath)) File.Delete(LogPath);
    }

    public string LogPath { get; }
    public int Records { get; private set; }

    public void AppendText(string text)
    {
        Records++;
        File.AppendAllText(LogPath, text);
    }
}