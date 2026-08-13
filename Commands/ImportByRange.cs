using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Sheet2Schedule.Services;
using Sheet2Schedule.UI;

namespace Sheet2Schedule.Commands
{
    /// <summary>
    /// Ribbon entry point for "Import by Range" - a quick, no-saved-config import for
    /// one-off cases where the user just wants to pick an exact cell range rather than
    /// build a reusable schedule type.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ImportByRange : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            return Run(commandData.Application.ActiveUIDocument.Document);
        }

        /// <summary>
        /// Core logic, callable both from Execute (standalone) and from HubForm
        /// (as one of the three consolidated actions).
        /// </summary>
        public static Result Run(Document doc)
        {
            string excelPath;
            using (var dialog = new OpenFileDialog
            {
                Title = "Select the Excel workbook",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx"
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return Result.Cancelled;

                excelPath = dialog.FileName;
            }

            List<string> sheetNames;
            try
            {
                var reader = new ExcelReader();
                sheetNames = reader.GetSheetNames(excelPath);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Sheet2Schedule", $"Could not read the Excel file.\n\nDetails: {ex.Message}");
                return Result.Cancelled;
            }

            using (var form = new ImportByRangeForm(excelPath, sheetNames))
            {
                if (form.ShowDialog() != DialogResult.OK || !form.UserConfirmedImport)
                    return Result.Cancelled;

                using (Transaction t = new Transaction(doc, "Import Custom Range (Data Sheet)"))
                {
                    t.Start();
                    try
                    {
                        var writer = new RevitDataWriter();
                        ViewSchedule schedule = writer.WriteScheduleTable(
                            doc, form.ResultDefinition, form.ResultRows, excelPath, "(custom range - not saved)");

                        // Diagnostic: read the data straight back out of the schedule to
                        // confirm it's actually stored, independent of whether the view
                        // is visually rendering it.
                        string diagnostic = "";
                        try
                        {
                            var tableData = schedule.GetTableData();
                            var header = tableData.GetSectionData(SectionType.Header);
                            diagnostic = $"\n\nDiagnostic check:\n" +
                                         $"Header section rows: {header.NumberOfRows}, columns: {header.NumberOfColumns}\n" +
                                         $"Cell [0,0]: \"{header.GetCellText(0, 0)}\"\n" +
                                         $"Cell [1,0]: \"{header.GetCellText(1, 0)}\"\n" +
                                         $"Cell [2,0]: \"{header.GetCellText(2, 0)}\"\n" +
                                         $"Cell [2,1]: \"{header.GetCellText(2, 1)}\"";
                        }
                        catch (Exception diagEx)
                        {
                            diagnostic = $"\n\n(Diagnostic check itself failed: {diagEx.Message})";
                        }

                        ImportLogService.AddEntry(doc, new Sheet2Schedule.Models.LogEntry
                        {
                            TimestampUtc = DateTime.UtcNow,
                            Action = "Import by Range",
                            ScheduleName = schedule.Name,
                            SourceExcelPath = excelPath,
                            RowCount = form.ResultRows.Count,
                            UserName = doc.Application.Username
                        });

                        t.Commit();

                        TaskDialog.Show(
                            "Sheet2Schedule",
                            $"Success.\n\nCreated schedule: {schedule.Name}\nRows imported: {form.ResultRows.Count}{diagnostic}");

                        return Result.Succeeded;
                    }
                    catch (Exception ex)
                    {
                        if (t.HasStarted() && !t.HasEnded())
                            t.RollBack();

                        TaskDialog.Show("Sheet2Schedule - Error", $"Something went wrong and has been rolled back.\n\nDetails: {ex.Message}");
                        return Result.Failed;
                    }
                }
            }
        }
    }
}