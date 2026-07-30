using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Sheet2Schedule.Models;

namespace Sheet2Schedule.Services
{
    public static class ConfigLoader
    {
        public static EquipmentScheduleDefinition Load(string configFilePath)
        {
            if (!File.Exists(configFilePath))
                throw new FileNotFoundException($"Schedule config not found: {configFilePath}");

            string json = File.ReadAllText(configFilePath);
            return JsonConvert.DeserializeObject<EquipmentScheduleDefinition>(json);
        }

        public static List<(string FileName, string DisplayName)> ListAvailableConfigs(string configDirectory)
        {
            var results = new List<(string, string)>();

            if (!Directory.Exists(configDirectory))
                return results;

            foreach (var filePath in Directory.GetFiles(configDirectory, "*.json"))
            {
                try
                {
                    var definition = Load(filePath);
                    string fileName = Path.GetFileName(filePath);
                    results.Add((fileName, definition.ScheduleName));
                }
                catch
                {
                    // Skip any malformed config file rather than crashing the picker.
                }
            }

            return results;
        }

        /// <summary>
        /// Writes a new config JSON file, built from the "New Schedule Type" wizard.
        /// Filename is derived from the schedule name, sanitized to be filesystem-safe.
        /// Won't overwrite an existing file - appends a number if there's a collision.
        /// </summary>
        public static string Save(EquipmentScheduleDefinition definition, string configDirectory)
        {
            if (!Directory.Exists(configDirectory))
                Directory.CreateDirectory(configDirectory);

            string baseFileName = SanitizeFileName(definition.ScheduleName);
            string fileName = baseFileName + ".json";
            string fullPath = Path.Combine(configDirectory, fileName);

            int suffix = 2;
            while (File.Exists(fullPath))
            {
                fileName = $"{baseFileName}_{suffix}.json";
                fullPath = Path.Combine(configDirectory, fileName);
                suffix++;
            }

            string json = JsonConvert.SerializeObject(definition, Formatting.Indented);
            File.WriteAllText(fullPath, json);

            return fullPath;
        }

        private static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars().Concat(new[] { ' ', '-' }).ToArray();
            foreach (char c in invalidChars)
                name = name.Replace(c, '_');
            return name.ToLowerInvariant();
        }
    }
}