using Microsoft.EntityFrameworkCore;
using System;
using fundo.core.Search.Index.Entity;

namespace fundo.core.Search.Index
{
    internal class SearchIndexContext : DbContext
    {
        public DbSet<FileEntity> FileEntities { get; set; } = null!;
        public DbSet<StorageDevice> StorageDevices { get; set; } = null!;
        public DbSet<ConfigEntry> ConfigEntries { get; set; } = null!;

        public SearchIndexContext(DbContextOptions<SearchIndexContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // FileEntity
            var file = modelBuilder.Entity<FileEntity>();
            file.HasKey(f => f.Id);
            file.Property(f => f.Id).ValueGeneratedOnAdd();
            file.Property(f => f.FileName).HasMaxLength(260).IsRequired();
            file.Property(f => f.Path).HasMaxLength(260).IsRequired();
            file.Property(f => f.FileType).HasMaxLength(50).IsRequired();

            file.HasOne(f => f.StorageDevice)
                .WithMany(d => d.Files)
                .HasForeignKey(f => f.StorageDeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            file.HasIndex(f => new { f.FileName, f.FileType, f.FileDate })
                .HasDatabaseName("IX_FileEntity_SearchFields");


            // StorageDevice
            var storage = modelBuilder.Entity<StorageDevice>();
            storage.HasKey(d => d.Id);
            storage.Property(d => d.Id).ValueGeneratedOnAdd();
            storage.Property(d => d.StorageName)
                   .HasMaxLength(260)
                   .IsRequired();
            storage.HasIndex(d => d.Id).HasDatabaseName("IX_StorageDevice_Id");
            storage.HasIndex(d => d.StorageName).HasDatabaseName("IX_StorageDevice_StorageName");

            
            // Config
            var config = modelBuilder.Entity<ConfigEntry>();
            config.HasKey(c => c.Id);
            config.Property(c => c.Id).ValueGeneratedOnAdd();
            config.Property(c => c.Key).IsRequired();
            config.Property(c => c.Value).HasMaxLength(260).IsRequired();
            config.HasIndex(c => c.Key).HasDatabaseName("IX_ConfigEntry_Key");
        }
    }
}
