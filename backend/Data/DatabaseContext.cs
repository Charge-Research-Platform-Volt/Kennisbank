using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Data
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public DbSet<FileItem> Files { get; set; }

        public DbSet<Tag> Tags { get; set; }
        
        public DbSet<FileTagLink> FileTagLinks { get; set; }


         protected override void OnModelCreating(ModelBuilder modelBuilder)
         {
            modelBuilder.Entity<FileTagLink>()
            .HasKey(ft => new { ft.DocId, ft.TagId }); // Define composite primary key

            base.OnModelCreating(modelBuilder);
         }
    }
}