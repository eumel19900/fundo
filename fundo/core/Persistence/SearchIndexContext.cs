using fundo.core.Persistence.Entity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.RegularExpressions;

namespace fundo.core.Persistence
{
    internal class SearchIndexContext : DbContext
    {
        public DbSet<FileEntity> FileEntities { get; set; } = null!;
        public DbSet<StorageDevice> StorageDevices { get; set; } = null!;
        public DbSet<PropertyEntry> PropertyEntries { get; set; } = null!;

        public static bool Regexp(string pattern, string input) => throw new NotSupportedException();

        public SearchIndexContext(DbContextOptions<SearchIndexContext> options) : base(options)
        {
            if (Database.GetDbConnection() is SqliteConnection sqlite)
            {
                sqlite.Open();

                sqlite.CreateFunction(
                    "REGEXP",
                    (string pattern, string input) =>
                        input != null && Regex.IsMatch(input, pattern)
                );
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDbFunction(
                typeof(SearchIndexContext).GetMethod(nameof(Regexp))!)
                .HasName("REGEXP");
            // FileEntity
            var file = modelBuilder.Entity<FileEntity>();
            file.HasKey(f => f.Id);
            file.Property(f => f.Id).ValueGeneratedOnAdd();
            file.Property(f => f.FileName).HasMaxLength(260).IsRequired();
            file.Property(f => f.Path).HasMaxLength(260).IsRequired();
            file.Property(f => f.FileType).HasMaxLength(50).IsRequired();
            file.Property(f => f.FileAttributesValue).IsRequired();

            file.HasOne(f => f.StorageDevice)
                .WithMany(d => d.Files)
                .HasForeignKey(f => f.StorageDeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            file.HasIndex(f => f.FileName)
                .HasDatabaseName("IX_FileEntity_FileName");
            file.HasIndex(f => f.FileType)
                .HasDatabaseName("IX_FileEntity_FileType");
            file.HasIndex(f => f.CreationTime)
                .HasDatabaseName("IX_FileEntity_CreationTime");
            file.HasIndex(f => f.ModifiedTime)
                .HasDatabaseName("IX_FileEntity_ModifiedTime");
            file.HasIndex(f => f.LastAccessTime)
                .HasDatabaseName("IX_FileEntity_LastAccessTime");


            // StorageDevice
            var storage = modelBuilder.Entity<StorageDevice>();
            storage.HasKey(d => d.Id);
            storage.Property(d => d.Id).ValueGeneratedOnAdd();
            storage.Property(d => d.StorageName)
                   .HasMaxLength(260)
                   .IsRequired();
            storage.HasIndex(d => d.Id).HasDatabaseName("IX_StorageDevice_Id");
            storage.HasIndex(d => d.StorageName).HasDatabaseName("IX_StorageDevice_StorageName");


            // PropertyEntry
            var property = modelBuilder.Entity<PropertyEntry>();
            property.HasKey(c => c.Id);
            property.Property(c => c.Id).ValueGeneratedOnAdd();
            property.Property(c => c.Key).IsRequired();
            property.Property(c => c.Value).HasMaxLength(260).IsRequired();
            property.HasIndex(c => c.Key).HasDatabaseName("IX_PropertyEntry_Key");
        }
    }
}
