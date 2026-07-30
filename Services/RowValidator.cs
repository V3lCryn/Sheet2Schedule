using System.Collections.Generic;
using System.Linq;
using Sheet2Schedule.Models;

namespace Sheet2Schedule.Services
{
    /// <summary>
    /// Checks parsed Excel rows for obvious problems before they're written to Revit.
    /// Currently checks: the first mapped column (treated as the row's "key field",
    /// e.g. Unit Reference) must not be blank. Rows failing this are excluded from
    /// what gets imported, and reported back so the user can decide whether to proceed
    /// or go fix the Excel file first.
    /// </summary>
    public static class RowValidator
    {
        public static (List<ScheduleRow> ValidRows, List<ValidationIssue> Issues) Validate(
            List<ScheduleRow> rows, EquipmentScheduleDefinition definition)
        {
            var validRows = new List<ScheduleRow>();
            var issues = new List<ValidationIssue>();

            if (definition.Columns.Count == 0)
                return (rows, issues);

            string keyFieldName = definition.Columns.First().RevitFieldName;

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                string keyValue = row.Get(keyFieldName);

                if (string.IsNullOrWhiteSpace(keyValue))
                {
                    issues.Add(new ValidationIssue
                    {
                        RowNumber = i + 1,
                        Message = $"Missing \"{keyFieldName}\" - row skipped"
                    });
                    continue;
                }

                validRows.Add(row);
            }

            return (validRows, issues);
        }
    }
}