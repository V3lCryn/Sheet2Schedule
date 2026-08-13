using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Sheet2Schedule.UI;

namespace Sheet2Schedule.Commands
{
    /// <summary>Ribbon entry point - opens the read-only import audit log viewer.</summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class ViewImportLog : IExternalCommand
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
            using (var form = new ImportLogForm(doc))
            {
                form.ShowDialog();
            }

            return Result.Succeeded;
        }
    }
}