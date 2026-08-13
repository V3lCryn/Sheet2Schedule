using System;
using System.IO;
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
            string assemblyDir = Path.GetDirectoryName(assemblyPath);

            var hubData = new PushButtonData(
                "Sheet2ScheduleHub",
                "Sheet2Schedule",
                assemblyPath,
                "Sheet2Schedule.Commands.Sheet2ScheduleHub")
            {
                ToolTip = "Import Excel schedules into Revit, manage existing data links, and view the import history - all from one place."
            };

            // Large ribbon icon (32px) and small fallback (16px, used if the panel collapses).
            // Resources/ ships alongside the DLL via the csproj's CopyToOutputDirectory setting.
            string icon32Path = Path.Combine(assemblyDir, "Resources", "logo_32.png");
            string icon16Path = Path.Combine(assemblyDir, "Resources", "logo_16.png");

            if (File.Exists(icon32Path))
                hubData.LargeImage = new BitmapImage(new Uri(icon32Path));
            if (File.Exists(icon16Path))
                hubData.Image = new BitmapImage(new Uri(icon16Path));

            panel.AddItem(hubData);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}