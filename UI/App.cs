using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;

namespace Sheet2Schedule.UI
{
    public class App : IExternalApplication
    {
        private const string TabName = "Sheet2Schedule";
        private const string PanelName = "Data Sheet";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                application.CreateRibbonTab(TabName);
            }
            catch
            {
                // Tab already exists - safe to ignore.
            }

            RibbonPanel panel = application.CreateRibbonPanel(TabName, PanelName);

            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            var smokeTestData = new PushButtonData(
                "WallCounter",
                "Smoke Test",
                assemblyPath,
                "Sheet2Schedule.Commands.WallCounter")
            {
                ToolTip = "Read-only test: counts walls in the active view. Confirms the add-in is loaded correctly without changing anything."
            };
            panel.AddItem(smokeTestData);

            var importData = new PushButtonData(
                "ImportCamelData",
                "Import\nSchedule",
                assemblyPath,
                "Sheet2Schedule.Commands.ImportCamelData")
            {
                ToolTip = "Reads a mechanical equipment schedule from Excel and creates a new native Revit schedule. Does not touch AutoCAD or any existing Revit schedule."
            };
            panel.AddItem(importData);

            var importByRangeData = new PushButtonData(
                "ImportByRange",
                "Import by\nRange",
                assemblyPath,
                "Sheet2Schedule.Commands.ImportByRange")
            {
                ToolTip = "Import a specific cell range (e.g. B2:P29) from Excel without needing a saved schedule type."
            };
            panel.AddItem(importByRangeData);

            var manageLinksData = new PushButtonData(
                "ManageDataLinks",
                "Manage\nData Links",
                assemblyPath,
                "Sheet2Schedule.Commands.ManageDataLinks")
            {
                ToolTip = "View and reload the Excel sources behind schedules created by this tool - similar to Revit's Manage Links, but for Excel-imported schedules."
            };
            panel.AddItem(manageLinksData);

            var viewLogData = new PushButtonData(
                "ViewImportLog",
                "Import\nLog",
                assemblyPath,
                "Sheet2Schedule.Commands.ViewImportLog")
            {
                ToolTip = "View the history of every schedule import/reload performed on this project."
            };
            panel.AddItem(viewLogData);

            var newScheduleTypeData = new PushButtonData(
                "NewScheduleType",
                "New Schedule\nType...",
                assemblyPath,
                "Sheet2Schedule.Commands.NewScheduleType")
            {
                ToolTip = "Build a new schedule type from any Excel file/sheet this tool hasn't seen before - no code changes needed."
            };
            panel.AddItem(newScheduleTypeData);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}