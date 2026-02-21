using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using fundo.core.Search.Index.Entity;

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
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folder = Path.Combine(localAppData, "Fundo");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            var dbPath = Path.Combine(folder, "fundo.db");
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
    }
}
