using System.IO;

namespace FamilyUpdater.Core;

public static class FileFormats
{
    public static List<string> GetRevitFiles(this string folder, bool recursiveSearch)
    {
        string[] formats =
        [
            ".rvt",
            ".rfa",
            ".rte",
            ".rft"
        ];

        var files = new List<string>();
        var searchOption = recursiveSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        foreach (var file in Directory.EnumerateFiles(folder, "*.*", searchOption))
        {
            if (file.EndsWith(StringComparison.OrdinalIgnoreCase, formats)) files.Add(file);
        }

        return files;
    }

    private static bool EndsWith(this string value, StringComparison comparison, IEnumerable<string> values)
    {
        return values.Any(extension => value.EndsWith(extension, comparison));
    }
}