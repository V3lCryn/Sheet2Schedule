using System;
using System.Text.RegularExpressions;

namespace Sheet2Schedule.Services
{
    /// <summary>
    /// Parses a simple Excel range string like "B2:P29" into its component column
    /// letters and row numbers, for the "Import by Range" quick-import feature.
    /// </summary>
    public static class RangeParser
    {
        public class ParsedRange
        {
            public string StartColumn { get; set; }
            public int StartRow { get; set; }
            public string EndColumn { get; set; }
            public int EndRow { get; set; }
        }

        public static ParsedRange Parse(string rangeText)
        {
            if (string.IsNullOrWhiteSpace(rangeText))
                throw new FormatException("Range cannot be empty.");

            string cleaned = rangeText.Trim().Replace(" ", "").ToUpperInvariant();
            string pattern = @"^([A-Z]+)(\d+):([A-Z]+)(\d+)$";
            var match = Regex.Match(cleaned, pattern);

            if (!match.Success)
                throw new FormatException($"\"{rangeText}\" isn't a valid range. Use a format like B2:P29.");

            var result = new ParsedRange
            {
                StartColumn = match.Groups[1].Value,
                StartRow = int.Parse(match.Groups[2].Value),
                EndColumn = match.Groups[3].Value,
                EndRow = int.Parse(match.Groups[4].Value)
            };

            if (result.EndRow <= result.StartRow)
                throw new FormatException("The end row must be after the start row.");

            return result;
        }
    }
}