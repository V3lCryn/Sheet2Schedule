using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Sheet2Schedule.Services;
using Sheet2Schedule.UI;

namespace Sheet2Schedule.Commands
{
    /// <summary>
    /// Main entry point. Prompts for the Excel workbook, detects which schedule type(s)
    /// it matches, validates the data, and writes it into a brand-new Revit schedule.
    ///
    /// Safety:
    ///  - Entire write operation runs inside ONE Transaction. Any exception rolls the
    ///    whole thing back - the model is left exactly as it was before the command ran.
    ///  - Only ever CREATES a new schedule, never edits/deletes an existing one.
    ///  - Excel file is opened read-only and is never written to.
    ///
    /// A ProgressStatusForm is shown throughout so the user always sees what stage
    /// the tool is at, rather than wondering if Revit has frozen on a large file.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ImportCamelData : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // --- 1. Pick the Excel file ---
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

            using (var progress = new ProgressStatusForm("Reading Excel file..."))
            {
                progress.Show();

                // --- 2. Peek inside the file to see which sheets it actually has ---
                var reader = new ExcelReader();
                List<string> sheetNames;
                try
                {
                    sheetNames = reader.GetSheetNames(excelPath);
                }
                catch (Exception ex)
                {
                    progress.Close();
                    TaskDialog.Show("Sheet2Schedule", $"Could not read the Excel file.\n\nDetails: {ex.Message}");
                    return Result.Cancelled;
                }

                // --- 3. Only offer schedule types whose expected sheet actually exists ---
                progress.UpdateStatus("Checking schedule types...");

                string configDir = Path.Combine(
                    Path.GetDirectoryName(typeof(App_ConfigLocator).Assembly.Location), "Config");

                var allConfigs = ConfigLoader.ListAvailableConfigs(configDir);
                var matchingConfigs = new List<(string FileName, string DisplayName)>();

                foreach (var config in allConfigs)
                {
                    var definitionCheck = ConfigLoader.Load(Path.Combine(configDir, config.FileName));
                    if (sheetNames.Contains(definitionCheck.SheetName))
                        matchingConfigs.Add(config);
                }

                if (matchingConfigs.Count == 0)
                {
                    progress.Close();
                    TaskDialog.Show(
                        "Sheet2Schedule",
                        "This workbook doesn't contain any sheet matching a known schedule type.\n\n" +
                        $"Sheets found: {string.Join(", ", sheetNames)}");
                    return Result.Cancelled;
                }

                string selectedConfigFileName;
                if (matchingConfigs.Count == 1)
                {
                    selectedConfigFileName = matchingConfigs[0].FileName;
                }
                else
                {
                    progress.Hide();
                    using (var picker = new ScheduleTypePickerForm(matchingConfigs))
                    {
                        if (picker.ShowDialog() != DialogResult.OK)
                        {
                            progress.Close();
                            return Result.Cancelled;
                        }

                        selectedConfigFileName = picker.SelectedConfigFileName;
                    }
                    progress.Show();
                }

                string configPath = Path.Combine(configDir, selectedConfigFileName);
                var definition = ConfigLoader.Load(configPath);

                // --- 4. Read the data ---
                progress.UpdateStatus("Reading schedule data...");
                var allRows = reader.ReadRows(excelPath, definition);

                if (allRows.Count == 0)
                {
                    progress.Close();
                    TaskDialog.Show("Sheet2Schedule", "No data rows were found in the sheet. Nothing was created.");
                    return Result.Cancelled;
                }

                // --- 5. Validate ---
                progress.UpdateStatus("Validating data...");
                var (rows, issues) = RowValidator.Validate(allRows, definition);

                if (issues.Count > 0)
                {
                    if (rows.Count == 0)
                    {
                        progress.Close();
                        TaskDialog.Show("Sheet2Schedule",
                            $"All {issues.Count} row(s) had issues and none could be imported. Check the source Excel file.");
                        return Result.Cancelled;
                    }

                    progress.Hide();
                    using (var warningForm = new ValidationWarningForm(issues, rows.Count))
                    {
                        warningForm.ShowDialog();
                        if (!warningForm.UserChoseProceed)
                        {
                            progress.Close();
                            return Result.Cancelled;
                        }
                    }
                    progress.Show();
                }

                // --- 6. Write to Revit inside a single rollback-safe transaction ---
                progress.UpdateStatus("Writing schedule to Revit...");

                using (Transaction t = new Transaction(doc, "Import Equipment Schedule (Data Sheet)"))
                {
                    t.Start();
                    try
                    {
                        var writer = new RevitDataWriter();
                        ViewSchedule schedule = writer.WriteScheduleTable(doc, definition, rows, excelPath, selectedConfigFileName);

                        ImportLogService.AddEntry(doc, new Sheet2Schedule.Models.LogEntry
                        {
                            TimestampUtc = DateTime.UtcNow,
                            Action = "Import",
                            ScheduleName = schedule.Name,
                            SourceExcelPath = excelPath,
                            RowCount = rows.Count,
                            UserName = doc.Application.Username
                        });

                        t.Commit();
                        progress.Close();

                        TaskDialog.Show(
                            "Sheet2Schedule",
                            $"Success.\n\nCreated schedule: {schedule.Name}\nRows imported: {rows.Count}\n\n" +
                            "This is a brand-new schedule - nothing existing was modified. " +
                            "Open it from the Project Browser under Schedules/Quantities to check it over " +
                            "before placing it on a sheet.");

                        return Result.Succeeded;
                    }
                    catch (Exception ex)
                    {
                        if (t.HasStarted() && !t.HasEnded())
                            t.RollBack();

                        progress.Close();

                        message = ex.Message;
                        TaskDialog.Show(
                            "Sheet2Schedule - Error",
                            $"Something went wrong and has been rolled back. No changes were made.\n\nDetails: {ex.Message}");

                        return Result.Failed;
                    }
                }
            }
        }
    }

    internal class App_ConfigLocator { }
}