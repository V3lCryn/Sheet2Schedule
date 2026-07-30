using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace Sheet2Schedule.Commands
{
    /// <summary>
    /// Read-only smoke test. Run this FIRST after installing/reloading the add-in to confirm
    /// it loaded correctly and can query the active document. It performs no Transaction and
    /// cannot modify the model in any way - safe to run on a live project if needed, though
    /// the recommendation is still to always test on a local sandbox copy first.
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    public class WallCounter : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Walls)
                .WhereElementIsNotElementType();

            int wallCount = collector.GetElementCount();

            TaskDialog.Show(
                "Sheet2Schedule - Smoke Test",
                $"Add-in loaded successfully.\n\nActive document: {doc.Title}\nWalls found: {wallCount}\n\nNo changes were made to the model.");

            return Result.Succeeded;
        }
    }
}
