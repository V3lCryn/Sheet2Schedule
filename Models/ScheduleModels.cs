using System.Collections.Generic;

namespace Sheet2Schedule.Models
{
    /// <summary>
    /// One row of raw data read from an Excel schedule sheet, keyed by column header text
    /// so the same generic model works for Fan Coil, Split, VRF, AHU, etc. without needing
    /// a bespoke class per schedule type.
    /// </summary>
    public class ScheduleRow
    {
        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();

        public string Get(string columnKey) =>
            Values.TryGetValue(columnKey, out var v) ? v : string.Empty;
    }

    /// <summary>
    /// Describes one Excel sheet -> Revit Key Schedule mapping. This is what makes the
    /// engine config-driven: adding "Split Schedule" or "VRF Indoor" later means adding
    /// a new EquipmentScheduleDefinition, not writing new C# code.
    /// </summary>
    public class EquipmentScheduleDefinition
    {
        /// <summary>Friendly name, e.g. "Fan Coil Unit Schedule". Used as the Revit schedule name suffix.</summary>
        public string ScheduleName { get; set; }

        /// <summary>Exact Excel sheet name/tab to read from, e.g. "FAN COIL UNITS SCHEDULE".</summary>
        public string SheetName { get; set; }

        /// <summary>1-based row number where the real column headers live (skipping title/notes rows).</summary>
        public int HeaderRow { get; set; }

        /// <summary>1-based row number where data starts.</summary>
        public int DataStartRow { get; set; }

        /// <summary>Ordered list of columns to bring across.</summary>
        public List<ColumnMapping> Columns { get; set; } = new List<ColumnMapping>();
    }

    public class ColumnMapping
    {
        /// <summary>Excel column letter, e.g. "B".</summary>
        public string ExcelColumn { get; set; }

        /// <summary>Header text as it should appear in the Revit schedule.</summary>
        public string RevitFieldName { get; set; }

        /// <summary>Optional: groups two-tier headers together, e.g. "Cooling Capacity" for GTH/TSH sub-columns.</summary>
        public string HeaderGroup { get; set; }
    }
}