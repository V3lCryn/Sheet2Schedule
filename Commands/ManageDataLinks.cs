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
            return Run(commandData.Application.ActiveUIDocument.Document);
        }

        /// <summary>
        /// Core logic, callable both from Execute (standalone) and from HubForm
        /// (as one of the three consolidated actions).
        /// </summary>
        public static Result Run(Document doc)
        {
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
                    "Run \"Import by Range\" first to create one.");
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