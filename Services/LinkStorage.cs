using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace Sheet2Schedule.Services
{
    /// <summary>
    /// Attaches "which Excel file did this schedule come from" metadata directly onto
    /// the Revit ViewSchedule element using Extensible Storage.
    /// </summary>
    public static class LinkStorage
    {
        private static readonly Guid SchemaGuid = new Guid("A3C1E2B4-9F6D-4A2E-8B3C-1D5E7F9A0B21");
        private const string SchemaName = "Sheet2Schedule_LinkInfo";
        private const string FieldSourcePath = "SourceExcelPath";
        private const string FieldConfigName = "ConfigFileName";
        private const string FieldLastUpdated = "LastUpdatedUtc";

        private static Schema GetOrCreateSchema()
        {
            Schema schema = Schema.Lookup(SchemaGuid);
            if (schema != null)
                return schema;

            var builder = new SchemaBuilder(SchemaGuid);
            builder.SetSchemaName(SchemaName);
            builder.SetReadAccessLevel(AccessLevel.Public);
            builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.AddSimpleField(FieldSourcePath, typeof(string));
            builder.AddSimpleField(FieldConfigName, typeof(string));
            builder.AddSimpleField(FieldLastUpdated, typeof(string));
            return builder.Finish();
        }

        public static void SetLinkInfo(ViewSchedule schedule, string excelPath, string configFileName)
        {
            Schema schema = GetOrCreateSchema();
            Entity entity = new Entity(schema);
            entity.Set(FieldSourcePath, excelPath);
            entity.Set(FieldConfigName, configFileName);
            entity.Set(FieldLastUpdated, DateTime.UtcNow.ToString("o"));
            schedule.SetEntity(entity);
        }

        public static ScheduleLinkInfo GetLinkInfo(ViewSchedule schedule)
        {
            Schema schema = Schema.Lookup(SchemaGuid);
            if (schema == null) return null;

            Entity entity = schedule.GetEntity(schema);
            if (entity == null || !entity.IsValid()) return null;

            return new ScheduleLinkInfo
            {
                ScheduleId = schedule.Id,
                ScheduleName = schedule.Name,
                SourceExcelPath = entity.Get<string>(FieldSourcePath),
                ConfigFileName = entity.Get<string>(FieldConfigName),
                LastUpdatedUtc = entity.Get<string>(FieldLastUpdated)
            };
        }

        public static bool HasLinkInfo(ViewSchedule schedule)
        {
            Schema schema = Schema.Lookup(SchemaGuid);
            if (schema == null) return false;
            Entity entity = schedule.GetEntity(schema);
            return entity != null && entity.IsValid();
        }
    }

    public class ScheduleLinkInfo
    {
        public ElementId ScheduleId { get; set; }
        public string ScheduleName { get; set; }
        public string SourceExcelPath { get; set; }
        public string ConfigFileName { get; set; }
        public string LastUpdatedUtc { get; set; }
    }
}