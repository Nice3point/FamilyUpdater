using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FamilyUpdater
{
    public static class FileFormats
    {
        public static List<string> Formats => new()
        {
            ".rvt",
            ".rfa",
            ".rte",
            ".rft"
        };

        public static string GetFileFilter()
        {
            var builder = new StringBuilder();
            for (var index = 0; index < Formats.Count; index++)
            {
                var format = Formats[index];
                builder.Append("*.");
                builder.Append(format);
                if (index != Formats.Count - 1) builder.Append(";");
            }

            return builder.ToString();
        }

        public static List<string> GetFilteredFiles(this IEnumerable<string> files)
        {
            return files.Where(file => file.EndsWith(StringComparison.OrdinalIgnoreCase, Formats)).ToList();
        }

        private static bool EndsWith(this string value, StringComparison comparison, IEnumerable<string> values)
        {
            return values.Any(extension => value.EndsWith(extension, comparison));
        }
    }
}