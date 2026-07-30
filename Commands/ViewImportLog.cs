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
            Document doc = commandData.Application.ActiveUIDocument.Document;

            using (var form = new ImportLogForm(doc))
            {
                form.ShowDialog();
            }

            return Result.Succeeded;
        }
    }
}