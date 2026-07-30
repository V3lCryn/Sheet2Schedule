using System.Collections.Generic;
using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Sheet2Schedule.Services;
using Sheet2Schedule.UI;

namespace Sheet2Schedule.Commands
{
    /// <summary>
    /// Ribbon entry point for the "New Schedule Type" wizard. Picks an Excel file,
    /// peeks at its sheet names, and opens the wizard so the user can build a new
    /// config for a schedule type this tool has never seen before.
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class NewScheduleType : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            string excelPath;
            using (var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Select the Excel workbook containing the new schedule type",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx"
            })
            {
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return Result.Cancelled;

                excelPath = dialog.FileName;
            }

            List<string> sheetNames;
            try
            {
                var reader = new ExcelReader();
                sheetNames = reader.GetSheetNames(excelPath);
            }
            catch (System.Exception ex)
            {
                TaskDialog.Show("Sheet2Schedule", $"Could not read the Excel file.\n\nDetails: {ex.Message}");
                return Result.Cancelled;
            }

            if (sheetNames.Count == 0)
            {
                TaskDialog.Show("Sheet2Schedule", "No sheets found in this workbook.");
                return Result.Cancelled;
            }

            string configDir = Path.Combine(
                Path.GetDirectoryName(typeof(NewScheduleType).Assembly.Location), "Config");

            using (var wizard = new NewScheduleTypeForm(excelPath, sheetNames, configDir))
            {
                wizard.ShowDialog();
            }

            return Result.Succeeded;
        }
    }
}