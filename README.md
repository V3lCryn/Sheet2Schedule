# Sheet2Schedule

Import equipment schedules directly from Excel into native Revit schedule
views, no manual re-typing and no AutoCAD Data Link intermediary step.

## What it does

Sheet2Schedule reads a range from an Excel workbook and writes it straight
into a Revit schedule view, preserving header groupings, so you can turn a
mechanical/electrical/plumbing equipment schedule maintained in Excel into a
live Revit schedule in a couple of clicks.

## Requirements

- Revit 2024 (Revit 2025/2026 support is on the roadmap)
- Windows

## Installation

1. Copy `Sheet2Schedule.dll` to a folder of your choice (default expected
   location: `C:\Program Files\Sheet2Schedule\`).
2. Copy `Sheet2Schedule.addin` to
   `%AppData%\Autodesk\Revit\Addins\2024\`, updating the `<Assembly>` path
   inside it to match wherever you placed the DLL in step 1.
3. Launch Revit. A "Sheet2Schedule" tab will appear on the ribbon.

## Usage

1. Open the Sheet2Schedule ribbon tab and choose **Import by Range**.
2. Select your Excel file and worksheet. A starting range is suggested
   automatically, adjust it if needed.
3. Preview the data, resolve any flagged rows, and click Import.
4. Use **Manage Data Links** at any time to reload a schedule if the source
   Excel file changes.

Sample configs are included under `Config/` to show the expected column
mapping format.

## License

Commercial license, see `LICENSE.txt`. Not open source.
