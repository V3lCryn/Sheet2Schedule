using System;

namespace Sheet2Schedule.Models
{
    /// <summary>One record of an import/reload action, for the audit log.</summary>
    public class LogEntry
    {
        public DateTime TimestampUtc { get; set; }
        public string Action { get; set; }       // "Import", "Reload", "Reload From"
        public string ScheduleName { get; set; }
        public string SourceExcelPath { get; set; }
        public int RowCount { get; set; }
        public string UserName { get; set; }
    }
}