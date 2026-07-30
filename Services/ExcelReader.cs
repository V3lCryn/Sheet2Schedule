using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Sheet2Schedule.Models;

namespace Sheet2Schedule.Services
{
    public class ExcelReader
    {
        public List<ScheduleRow> ReadRows(string excelFilePath, EquipmentScheduleDefinition definition)
        {
            if (!File.Exists(excelFilePath))
                throw new FileNotFoundException($"Excel file not found: {excelFilePath}");

            try
            {
                return ReadRowsFromFile(excelFilePath, definition);
            }
            catch (Exception)
            {
                // Fall back to image-stripped copy if direct read fails.
            }

            string tempPath = null;
            try
            {
                tempPath = CreateImageStrippedCopy(excelFilePath);
                return ReadRowsFromFile(tempPath, definition);
            }
            finally
            {
                if (tempPath != null && File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }

        public List<string> GetSheetNames(string excelFilePath)
        {
            try
            {
                using (var workbook = new XLWorkbook(excelFilePath))
                {
                    return workbook.Worksheets.Select(w => w.Name).ToList();
                }
            }
            catch
            {
                string tempPath = CreateImageStrippedCopy(excelFilePath);
                try
                {
                    using (var workbook = new XLWorkbook(tempPath))
                    {
                        return workbook.Worksheets.Select(w => w.Name).ToList();
                    }
                }
                finally
                {
                    if (File.Exists(tempPath))
                    {
                        try { File.Delete(tempPath); } catch { }
                    }
                }
            }
        }

        public (EquipmentScheduleDefinition Definition, List<ScheduleRow> Rows) ReadCustomRange(
            string excelFilePath, string sheetName, string scheduleName,
            string startColumn, int startRow, string endColumn, int endRow)
        {
            try
            {
                return ReadCustomRangeFromFile(excelFilePath, sheetName, scheduleName, startColumn, startRow, endColumn, endRow);
            }
            catch (Exception ex) when (IsLikelyFileLockOrImageIssue(ex))
            {
                string tempPath = null;
                try
                {
                    tempPath = CreateImageStrippedCopy(excelFilePath);
                    return ReadCustomRangeFromFile(tempPath, sheetName, scheduleName, startColumn, startRow, endColumn, endRow);
                }
                finally
                {
                    if (tempPath != null && File.Exists(tempPath))
                    {
                        try { File.Delete(tempPath); } catch { }
                    }
                }
            }
        }

        private bool IsLikelyFileLockOrImageIssue(Exception ex)
        {
            return ex is IOException || ex is UnauthorizedAccessException;
        }

        private (EquipmentScheduleDefinition Definition, List<ScheduleRow> Rows) ReadCustomRangeFromFile(
            string excelFilePath, string sheetName, string scheduleName,
            string startColumn, int startRow, string endColumn, int endRow)
        {
            using (var workbook = new XLWorkbook(excelFilePath))
            {
                if (!workbook.Worksheets.Contains(sheetName))
                    throw new SheetNotFoundException(sheetName, workbook.Worksheets.Select(w => w.Name));

                var sheet = workbook.Worksheet(sheetName);

                var startCell = sheet.Cell($"{startColumn}{startRow}");
                var endCell = sheet.Cell($"{endColumn}{endRow}");
                int startColNum = startCell.Address.ColumnNumber;
                int endColNum = endCell.Address.ColumnNumber;

                var definition = new EquipmentScheduleDefinition
                {
                    ScheduleName = scheduleName,
                    SheetName = sheetName,
                    HeaderRow = startRow,
                    DataStartRow = startRow + 1,
                    Columns = new List<ColumnMapping>()
                };

                var headerRow = sheet.Row(startRow);
                for (int c = startColNum; c <= endColNum; c++)
                {
                    string colLetter = sheet.Cell(startRow, c).Address.ColumnLetter;
                    string headerText = CleanHeaderText(headerRow.Cell(c).GetFormattedString());

                    if (string.IsNullOrWhiteSpace(headerText))
                        continue;

                    definition.Columns.Add(new ColumnMapping
                    {
                        ExcelColumn = colLetter,
                        RevitFieldName = headerText
                    });
                }

                if (definition.Columns.Count == 0)
                    throw new InvalidOperationException("No column headers were found in the first row of that range. Check the range and try again.");

                var rows = new List<ScheduleRow>();
                for (int r = startRow + 1; r <= endRow; r++)
                {
                    var excelRow = sheet.Row(r);
                    var scheduleRow = new ScheduleRow();

                    foreach (var col in definition.Columns)
                    {
                        string value = CleanHeaderText(excelRow.Cell(col.ExcelColumn).GetFormattedString());
                        scheduleRow.Values[col.RevitFieldName] = value;
                    }

                    rows.Add(scheduleRow);
                }

                return (definition, rows);
            }
        }

        /// <summary>
        /// Excel header/data cells frequently contain embedded line breaks (e.g.
        /// "SUPPLY\n(L/s)") because engineers wrap text inside a cell for layout.
        /// Revit's SetCellText does not handle embedded newlines well and it can leave
        /// a schedule unrenderable, so we flatten any newline/tab/repeated whitespace
        /// into single spaces here.
        /// </summary>
        private string CleanHeaderText(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";

            string flattened = raw
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Replace("\t", " ");

            while (flattened.Contains("  "))
                flattened = flattened.Replace("  ", " ");

            return flattened.Trim();
        }

        private string CreateImageStrippedCopy(string sourcePath)
        {
            string tempPath = Path.Combine(Path.GetTempPath(), $"Sheet2Schedule_{Guid.NewGuid()}.xlsx");
            File.Copy(sourcePath, tempPath, overwrite: true);

            try
            {
                using (var document = SpreadsheetDocument.Open(tempPath, isEditable: true))
                {
                    WorkbookPart workbookPart = document.WorkbookPart;

                    foreach (WorksheetPart worksheetPart in workbookPart.WorksheetParts)
                    {
                        bool changed = false;

                        var drawingRef = worksheetPart.Worksheet.Elements<Drawing>().FirstOrDefault();
                        if (drawingRef != null)
                        {
                            drawingRef.Remove();
                            changed = true;
                        }

                        var legacyDrawingRef = worksheetPart.Worksheet.Elements<LegacyDrawing>().FirstOrDefault();
                        if (legacyDrawingRef != null)
                        {
                            legacyDrawingRef.Remove();
                            changed = true;
                        }

                        if (changed)
                            worksheetPart.Worksheet.Save();

                        if (worksheetPart.DrawingsPart != null)
                            worksheetPart.DeletePart(worksheetPart.DrawingsPart);

                        var vmlParts = worksheetPart.GetPartsOfType<VmlDrawingPart>().ToList();
                        foreach (var vmlPart in vmlParts)
                            worksheetPart.DeletePart(vmlPart);
                    }
                }
            }
            catch
            {
                // If stripping fails, fall back to reading the original as-is.
            }

            return tempPath;
        }

        private List<ScheduleRow> ReadRowsFromFile(string excelFilePath, EquipmentScheduleDefinition definition)
        {
            var rows = new List<ScheduleRow>();

            using (var workbook = new XLWorkbook(excelFilePath))
            {
                if (!workbook.Worksheets.Contains(definition.SheetName))
                    throw new SheetNotFoundException(definition.SheetName, workbook.Worksheets.Select(w => w.Name));

                var sheet = workbook.Worksheet(definition.SheetName);

                int rowNum = definition.DataStartRow;
                while (true)
                {
                    var excelRow = sheet.Row(rowNum);

                    bool anyValue = definition.Columns.Any(c =>
                        !excelRow.Cell(c.ExcelColumn).IsEmpty());

                    if (!anyValue)
                        break;

                    var scheduleRow = new ScheduleRow();
                    foreach (var col in definition.Columns)
                    {
                        string value = excelRow.Cell(col.ExcelColumn).GetFormattedString()?.Trim() ?? string.Empty;
                        scheduleRow.Values[col.RevitFieldName] = value;
                    }

                    rows.Add(scheduleRow);
                    rowNum++;
                }
            }

            return rows;
        }
    }

    public class SheetNotFoundException : System.Exception
    {
        public SheetNotFoundException(string requested, IEnumerable<string> available)
            : base($"Sheet '{requested}' not found. Available sheets: {string.Join(", ", available)}")
        {
        }
    }
}