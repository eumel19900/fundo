
namespace fundo.core.Persistence.Entity
{
    /// <summary>
    /// Simple key/value pair used to persist configuration values for the search index.
    /// </summary>
    internal class PropertyEntry
    {
        // Primary key - identity/auto-increment column
        public long Id { get; set; }

        // Logical configuration key, e.g. "IndexedSearchEnabled" or "IndexDrive_C".
        public string Key { get; set; } = string.Empty;

        // Stored configuration value, limited to 260 characters in the database.
        public string Value { get; set; } = string.Empty;

        public PropertyEntry()
        {
        }

        public PropertyEntry(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
}
