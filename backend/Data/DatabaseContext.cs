using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Data
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public DbSet<Drive> Drives { get; set; }

        public DbSet<Tag> Tags { get; set; }


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