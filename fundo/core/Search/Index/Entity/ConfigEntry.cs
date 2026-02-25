using System;

namespace fundo.core.Search.Index.Entity
{
    /// <summary>
    /// Simple key/value pair used to persist configuration values for the search index.
    /// </summary>
    internal class ConfigEntry
    {
        // Primary key - identity/auto-increment column
        public long Id { get; set; }

        // Logical configuration key, e.g. "IndexedSearchEnabled" or "IndexDrive_C".
        public string Key { get; set; } = string.Empty;

        // Stored configuration value, limited to 260 characters in the database.
        public string Value { get; set; } = string.Empty;

        public ConfigEntry()
        {
        }

        public ConfigEntry(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
