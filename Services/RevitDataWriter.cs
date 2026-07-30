using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Sheet2Schedule.Models;

namespace Sheet2Schedule.Services
{
    public class RevitDataWriter
    {
        public ViewSchedule WriteScheduleTable(
            Document doc,
            EquipmentScheduleDefinition definition,
            List<ScheduleRow> rows,
            string sourceExcelPath,
            string configFileName)
        {
            string scheduleName = MakeUniqueScheduleName(doc, definition.ScheduleName);

            ElementId categoryId = new ElementId(BuiltInCategory.OST_GenericModel);
            ViewSchedule schedule = ViewSchedule.CreateSchedule(doc, categoryId);
            schedule.Name = scheduleName;

            BuildTable(schedule, definition, rows);

            LinkStorage.SetLinkInfo(schedule, sourceExcelPath, configFileName);

            return schedule;
        }

        public void RefreshScheduleTable(ViewSchedule schedule, EquipmentScheduleDefinition definition, List<ScheduleRow> rows)
        {
            TableData tableData = schedule.GetTableData();
            TableSectionData header = tableData.GetSectionData(SectionType.Header);

            while (header.NumberOfRows > 0)
                header.RemoveRow(header.NumberOfRows - 1);

            while (header.NumberOfColumns > 0)
                header.RemoveColumn(header.NumberOfColumns - 1);

            BuildTable(schedule, definition, rows, header);
        }

        private void BuildTable(ViewSchedule schedule, EquipmentScheduleDefinition definition, List<ScheduleRow> rows, TableSectionData header = null)
        {
            if (header == null)
            {
                TableData tableData = schedule.GetTableData();
                header = tableData.GetSectionData(SectionType.Header);
            }

            // Only reserve a group-header row if at least one column actually has a
            // HeaderGroup. Writing a completely blank first row (which is what happened
            // for range-based imports, where no groups exist) appears to leave the
            // schedule unrenderable, so we skip it entirely in that case.
            bool hasGroups = definition.Columns.Any(c => !string.IsNullOrWhiteSpace(c.HeaderGroup));
            int headerRowCount = hasGroups ? 2 : 1;

            int totalCols = definition.Columns.Count;
            int totalRows = headerRowCount + rows.Count;

            while (header.NumberOfColumns < totalCols)
                header.InsertColumn(header.NumberOfColumns);

            while (header.NumberOfRows < totalRows)
                header.InsertRow(header.NumberOfRows);

            SetColumnWidths(header, definition);
            WriteHeaderRows(header, definition, hasGroups);
            WriteDataRows(header, definition, rows, headerRowCount);
        }

        private void SetColumnWidths(TableSectionData header, EquipmentScheduleDefinition definition)
        {
            for (int c = 0; c < definition.Columns.Count; c++)
            {
                double width = 0.15;

                string fieldName = definition.Columns[c].RevitFieldName ?? "";
                if (fieldName.IndexOf("Comment", StringComparison.OrdinalIgnoreCase) >= 0
                    || fieldName.IndexOf("Area Served", StringComparison.OrdinalIgnoreCase) >= 0
                    || fieldName.IndexOf("Base Model", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    width = 0.30;
                }

                header.SetColumnWidth(c, width);
            }
        }

        private void WriteHeaderRows(TableSectionData header, EquipmentScheduleDefinition definition, bool hasGroups)
        {
            // Column-name row sits at index 1 when there's a group row above it,
            // otherwise it's the very first row.
            int nameRowIndex = hasGroups ? 1 : 0;

            int col = 0;
            int groupStart = -1;
            string currentGroup = null;

            foreach (var column in definition.Columns)
            {
                header.SetCellText(nameRowIndex, col, column.RevitFieldName ?? "");

                if (hasGroups && !string.IsNullOrWhiteSpace(column.HeaderGroup))
                {
                    if (column.HeaderGroup != currentGroup)
                    {
                        if (currentGroup != null && col - 1 > groupStart)
                            header.MergeCells(new TableMergedCell(0, groupStart, 0, col - 1));

                        currentGroup = column.HeaderGroup;
                        groupStart = col;
                        header.SetCellText(0, col, column.HeaderGroup);
                    }
                }
                else
                {
                    currentGroup = null;
                }

                col++;
            }

            if (hasGroups && currentGroup != null && col - 1 > groupStart)
                header.MergeCells(new TableMergedCell(0, groupStart, 0, col - 1));
        }

        private void WriteDataRows(TableSectionData header, EquipmentScheduleDefinition definition, List<ScheduleRow> rows, int headerRowCount)
        {
            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                for (int c = 0; c < definition.Columns.Count; c++)
                {
                    string value = row.Get(definition.Columns[c].RevitFieldName) ?? "";
                    header.SetCellText(r + headerRowCount, c, value);
                }
            }
        }

        private string MakeUniqueScheduleName(Document doc, string baseName)
        {
            string safeBaseName = SanitizeName(baseName);
            string candidate = $"{safeBaseName} - AUTO";

            var existingNames = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSchedule))
                .Cast<ViewSchedule>()
                .Select(v => v.Name)
                .ToHashSet();

            if (!existingNames.Contains(candidate))
                return candidate;

            return $"{safeBaseName} - AUTO - {DateTime.Now:yyyyMMdd_HHmmss}";
        }

        private string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            char[] illegalChars = { ':', '\\', '/', '?', '*', '[', ']', '{', '}', '|', ';', '<', '>', '`', '~' };
            foreach (char c in illegalChars)
                name = name.Replace(c.ToString(), "");
            return name.Trim();
        }
    }
}