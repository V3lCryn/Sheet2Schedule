using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Sheet2Schedule.Services;
using Sheet2Schedule.UI;

namespace Sheet2Schedule.Commands
{
    /// <summary>
    /// Entry point for the "Manage Data Links" ribbon button. Finds every schedule in
    /// the active document that this tool created (identified via LinkStorage
    /// Extensible Storage data) and opens the management dialog for it.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ManageDataLinks : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            List<ViewSchedule> managedSchedules = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSchedule))
                .Cast<ViewSchedule>()
                .Where(LinkStorage.HasLinkInfo)
                .ToList();

            if (managedSchedules.Count == 0)
            {
                TaskDialog.Show(
                    "Sheet2Schedule",
                    "No schedules created by this tool were found in the current document.\n\n" +
                    "Run \"Import Schedule\" first to create one.");
                return Result.Succeeded;
            }

            using (var form = new ManageDataLinksForm(doc, managedSchedules))
            {
                form.ShowDialog();
            }

            return Result.Succeeded;
        }
    }
}