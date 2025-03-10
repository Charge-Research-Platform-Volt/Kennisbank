using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Models.User;

namespace KnowledgeBank.Data
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public DbSet<FileItem> Files { get; set; }

        public DbSet<Tag> Tags { get; set; }

        public DbSet<User> Users { get; set; }


        // protected override void OnModelCreating(ModelBuilder modelBuilder)
        // {
        //     base.OnModelCreating(modelBuilder);

        //     // Drive
        //     modelBuilder.Entity<Drive>().HasKey(d => d.Id);
        //     modelBuilder.Entity<Drive>().Property(d => d.Name).IsRequired();
        //     modelBuilder.Entity<Drive>().Property(d => d.Description).IsRequired();
        //     modelBuilder.Entity<Drive>().Property(d => d.ImageUrl).IsRequired();
        // }
    }
}