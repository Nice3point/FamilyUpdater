using System.IO;

namespace FamilyUpdater
{
    public class Logger
    {
        private const string LogFileName = "Revit-FamilyUpdater.log";

        public Logger()
        {
            var logFolder = Path.GetTempPath();
            LogPath = Path.Combine(logFolder, LogFileName);
            DeleteLog();
        }

        public string LogPath { get; }
        public bool HaveLogData { get; private set; }
        public int RecordNumbers { get; private set; }

        public void AppendText(string text)
        {
            HaveLogData = true;
            RecordNumbers++;
            File.AppendAllText(LogPath, text);
        }

        private void DeleteLog()
        {
            if (File.Exists(LogPath)) File.Delete(LogPath);
        }
    }
}