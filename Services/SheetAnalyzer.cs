using System.Collections.Generic;
using System.Linq;
using ClosedXML.Excel;

namespace Sheet2Schedule.Services
{
    /// <summary>
    /// Reads a raw preview of an unfamiliar Excel sheet and makes a best-effort guess
    /// at where the header row and data start row are, so the "New Schedule Type"
    /// wizard can pre-fill sensible defaults for the user to confirm/adjust rather than
    /// requiring them to figure it out from scratch.
    /// </summary>
    public class SheetAnalyzer
    {
        public class PreviewResult
        {
            public List<string> ColumnLetters { get; set; }
            public List<List<string>> Rows { get; set; } // Rows[rowIndex][colIndex], 0-based
            public int GuessedHeaderRow { get; set; }     // 1-based Excel row number
            public int GuessedDataStartRow { get; set; }  // 1-based Excel row number
        }

        public PreviewResult AnalyzeSheet(string excelFilePath, string sheetName, int maxRows = 15, int maxCols = 25)
        {
            using (var workbook = new XLWorkbook(excelFilePath))
            {
                var sheet = workbook.Worksheet(sheetName);

                var columnLetters = new List<string>();
                for (int c = 1; c <= maxCols; c++)
                    columnLetters.Add(GetColumnLetter(c));

                var rows = new List<List<string>>();
                for (int r = 1; r <= maxRows; r++)
                {
                    var rowValues = new List<string>();
                    for (int c = 1; c <= maxCols; c++)
                    {
                        string val = sheet.Cell(r, c).GetFormattedString()?.Trim() ?? "";
                        rowValues.Add(val);
                    }
                    rows.Add(rowValues);
                }

                int guessedHeaderRow = GuessHeaderRow(rows);

                return new PreviewResult
                {
                    ColumnLetters = columnLetters,
                    Rows = rows,
                    GuessedHeaderRow = guessedHeaderRow,
                    GuessedDataStartRow = guessedHeaderRow + 1
                };
            }
        }

        /// <summary>
        /// Heuristic: the header row is usually the row with the most non-empty text
        /// cells, among the first several rows (title rows above it tend to have only
        /// one or two cells filled in; the header row itself tends to have a value in
        /// most columns).
        /// </summary>
        private int GuessHeaderRow(List<List<string>> rows)
        {
            int bestRowIndex = 0;
            int bestCount = -1;

            int scanLimit = System.Math.Min(rows.Count, 12);
            for (int i = 0; i < scanLimit; i++)
            {
                int nonEmptyCount = rows[i].Count(cell => !string.IsNullOrWhiteSpace(cell));
                if (nonEmptyCount > bestCount)
                {
                    bestCount = nonEmptyCount;
                    bestRowIndex = i;
                }
            }

            return bestRowIndex + 1; // convert to 1-based Excel row number
        }

        private string GetColumnLetter(int columnNumber)
        {
            string letter = "";
            while (columnNumber > 0)
            {
                int remainder = (columnNumber - 1) % 26;
                letter = (char)('A' + remainder) + letter;
                columnNumber = (columnNumber - 1) / 26;
            }
            return letter;
        }
    }
}