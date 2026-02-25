using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using fundo.core.Search.Index.Entity;
using Windows.Storage;

namespace fundo.core.Search.Index
{
    /// <summary>
    /// Manages the SQLite-backed search index using Entity Framework Core.
    /// - Database file is stored in local application data under a "fundo" folder.
    /// - If the file does not exist it will be created and the schema (tables) will be created via EnsureCreated().
    /// Note: Add package references to Microsoft.EntityFrameworkCore and Microsoft.EntityFrameworkCore.Sqlite.
    /// </summary>
    internal static class SearchIndexStore
    {
        private static string GetDatabasePath()
        {
            string baseFolder = ApplicationData.Current.LocalFolder.Path;
            string folder = Path.Combine(baseFolder, "Fundo");
            Directory.CreateDirectory(folder);

            string dbPath = Path.Combine(folder, "fundo.db");
            return dbPath;
        }

        private static DbContextOptions<SearchIndexContext> CreateOptions()
        {
            var dbPath = GetDatabasePath();
            var builder = new DbContextOptionsBuilder<SearchIndexContext>();
            builder.UseSqlite($"Data Source={dbPath}");
            return builder.Options;
        }


        public static SearchIndexContext CreateContext()
        {
            var options = CreateOptions();
            SearchIndexContext ctx = new SearchIndexContext(options);
            ctx.Database.EnsureCreated();
            return ctx;
        }

        // --- ConfigEntry helpers ---

        public static string? GetConfigValue(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;

            using var ctx = CreateContext();
            var entry = ctx.ConfigEntries
                .AsNoTracking()
                .FirstOrDefault(c => c.Key == key);

            return entry?.Value;
        }

        public static void SetConfigValue(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            using var ctx = CreateContext();

            var entry = ctx.ConfigEntries.FirstOrDefault(c => c.Key == key);
            if (entry == null)
            {
                entry = new ConfigEntry(key, value);
                ctx.ConfigEntries.Add(entry);
            }
            else
            {
                entry.Value = value;
            }

            ctx.SaveChanges();
        }

        // --- StorageDevice helpers ---

        public static StorageDevice GetOrCreateStorageDevice(string storageName)
        {
            if (string.IsNullOrWhiteSpace(storageName))
            {
                throw new ArgumentException("Storage name must not be empty.", nameof(storageName));
            }

            using var ctx = CreateContext();

            var device = ctx.StorageDevices
                .FirstOrDefault(d => d.StorageName == storageName);

            if (device != null)
            {
                return device;
            }

            device = new StorageDevice
            {
                StorageName = storageName
            };

            ctx.StorageDevices.Add(device);
            ctx.SaveChanges();

            return device;
        }

        public static StorageDevice? GetStorageDeviceById(long id)
        {
            using var ctx = CreateContext();
            return ctx.StorageDevices
                .Include(d => d.Files)
                .FirstOrDefault(d => d.Id == id);
        }

        // --- FileEntity helpers ---

        public static FileEntity AddFile(FileEntity file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            using var ctx = CreateContext();
            ctx.FileEntities.Add(file);
            ctx.SaveChanges();
            return file;
        }

        public static void DeleteFile(long id)
        {
            using var ctx = CreateContext();
            var entity = ctx.FileEntities.FirstOrDefault(f => f.Id == id);
            if (entity == null)
            {
                return;
            }

            ctx.FileEntities.Remove(entity);
            ctx.SaveChanges();
        }
    }
}
