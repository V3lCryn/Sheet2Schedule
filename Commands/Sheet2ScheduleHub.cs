using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Sheet2Schedule.UI;

namespace Sheet2Schedule.Commands
{
    /// <summary>
    /// Single ribbon entry point for Sheet2Schedule. Opens HubForm, which offers the
    /// three retained actions (Import by Range, Manage Data Links, Import Log) from one
    /// place instead of separate ribbon buttons.
    ///
    /// Manual transaction mode because Import by Range (one of the hub's options)
    /// starts its own transaction internally when the user chooses it - the other two
    /// options are read-only/manage-only and don't need one.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class Sheet2ScheduleHub : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            using (var hub = new HubForm(doc))
            {
                hub.ShowDialog();
            }

            return Result.Succeeded;
        }
    }
}
