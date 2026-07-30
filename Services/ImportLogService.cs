using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using Newtonsoft.Json;
using Sheet2Schedule.Models;

namespace Sheet2Schedule.Services
{
    /// <summary>
    /// Stores an audit log of every Import/Reload/Reload From action directly on the
    /// document's ProjectInformation element via Extensible Storage, as a JSON-serialized
    /// list. This means the log travels with the .rvt file itself (useful if the file is
    /// shared or opened on another machine), rather than living only on the local disk.
    /// Must be called from within an already-open Transaction.
    /// </summary>
    public static class ImportLogService
    {
        private static readonly Guid SchemaGuid = new Guid("7B4E2C81-3A9F-4D6E-B812-5F0C9E1A4D77");
        private const string SchemaName = "Sheet2Schedule_ImportLog";
        private const string FieldLogJson = "LogEntriesJson";

        private static Schema GetOrCreateSchema()
        {
            Schema schema = Schema.Lookup(SchemaGuid);
            if (schema != null)
                return schema;

            var builder = new SchemaBuilder(SchemaGuid);
            builder.SetSchemaName(SchemaName);
            builder.SetReadAccessLevel(AccessLevel.Public);
            builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.AddSimpleField(FieldLogJson, typeof(string));
            return builder.Finish();
        }

        /// <summary>Appends one entry. Must be called inside an open Transaction.</summary>
        public static void AddEntry(Document doc, LogEntry entry)
        {
            var entries = GetEntries(doc);
            entries.Add(entry);

            Schema schema = GetOrCreateSchema();
            Entity storageEntity = new Entity(schema);
            storageEntity.Set(FieldLogJson, JsonConvert.SerializeObject(entries));

            ProjectInfo projectInfo = doc.ProjectInformation;
            projectInfo.SetEntity(storageEntity);
        }

        public static List<LogEntry> GetEntries(Document doc)
        {
            Schema schema = Schema.Lookup(SchemaGuid);
            if (schema == null) return new List<LogEntry>();

            ProjectInfo projectInfo = doc.ProjectInformation;
            Entity entity = projectInfo.GetEntity(schema);
            if (entity == null || !entity.IsValid())
                return new List<LogEntry>();

            string json = entity.Get<string>(FieldLogJson);
            if (string.IsNullOrEmpty(json))
                return new List<LogEntry>();

            return JsonConvert.DeserializeObject<List<LogEntry>>(json) ?? new List<LogEntry>();
        }
    }
}