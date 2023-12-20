namespace FamilyUpdater.Core;

public static class FileFormats
{
    private static IEnumerable<string> Formats => new List<string>
    {
        ".rvt",
        ".rfa",
        ".rte",
        ".rft"
    };

    public static List<string> GetFilteredFiles(this string folder, SearchOption searchOption)
    {
        return Directory.EnumerateFiles(folder, "*.*", searchOption)
            .Where(file => file.EndsWith(StringComparison.OrdinalIgnoreCase, Formats))
            .ToList();
    }

    private static bool EndsWith(this string value, StringComparison comparison, IEnumerable<string> values)
    {
        return values.Any(extension => value.EndsWith(extension, comparison));
    }
}