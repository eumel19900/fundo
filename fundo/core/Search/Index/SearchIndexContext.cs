using Microsoft.EntityFrameworkCore;
using System;
using fundo.core.Search.Index.Entity;

namespace fundo.core.Search.Index
{
    internal class SearchIndexContext : DbContext
    {
        public DbSet<FileEntity> FileEntities { get; set; } = null!;

        public SearchIndexContext(DbContextOptions<SearchIndexContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var file = modelBuilder.Entity<FileEntity>();

            // Primary key and identity
            file.HasKey(f => f.Id);
            file.Property(f => f.Id).ValueGeneratedOnAdd();

            // Configure required lengths if desired (optional)
            file.Property(f => f.FileName).HasMaxLength(260).IsRequired();
            file.Property(f => f.Path).HasMaxLength(260).IsRequired();
            file.Property(f => f.FileType).HasMaxLength(50).IsRequired();
            file.Property(f => f.DriveLocation).HasMaxLength(50).IsRequired();

            // Index for the requested fields
            file.HasIndex(f => new { f.DriveLocation, f.FileName, f.FileType, f.FileDate }).HasDatabaseName("IX_FileEntity_SearchFields");
        }
    }
}
