using System.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KnowledgeBank.Data
{
    public class DatabaseContext : IdentityDbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public DbSet<FileItem> Files { get; set; }
        public virtual DbSet<Tag> Tags { get; set; }
        public DbSet<FileTagLink> FileTagLinks { get; set; }
        public DbSet<UserTag> UserTags { get; set; }
        public DbSet<User> AppUsers { get; set; } // Renamed to avoid conflict with IdentityDbContext.Users
        public DbSet<FileVector> Vectors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileItem>()
                .HasOne(f => f.Vector)
                .WithOne(v => v.File)
                .HasForeignKey<FileVector>(v => v.FileId)
                .OnDelete(DeleteBehavior.Cascade);    // Automatically deletes vector on file delete

            modelBuilder.Entity<FileTagLink>()
                .HasKey(ft => new { ft.DocId, ft.TagId }); // Define composite primary key

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Saves database changes and ensures that any file updates related to FileItem entities 
        /// are processed within the same transaction. This guarantees consistency between the database 
        /// and the file system and between FileItem entities and their search vectors.        
        /// </summary>
        /// <param name="cancellationToken">A token used to observe operation cancellation.</param>
        /// <returns>The number of state entries written to the database.</returns>
        public async Task<int> SaveFileChangesAsync(CancellationToken cancellationToken = default)
        {
            // Get files that were added or modified
            var updatedFiles = ChangeTracker.Entries<FileItem>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => e.Entity)
                .ToList();

            // First save the changes to the database
            var result = await base.SaveChangesAsync(cancellationToken);

            // If there are any updated files, update their search vectors
            if (updatedFiles.Count != 0) await UpdateFileVectorAsync(updatedFiles);

            return result;
        }

        /// <summary>
        /// Updates or inserts full-text search vectors for the given list of updated FileItem entities. 
        /// If a vector already exists for a file, it is updated. Otherwise, a new vector is inserted.
        /// </summary>
        /// <param name="updatedFiles">A list of files that were added or modified.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task UpdateFileVectorAsync(List<FileItem> updatedFiles)
        {
            // Generates an Enumerable<Task> of SQL queries that inserts the
            // vector, if there's a conflict, replace existing vector instead
            foreach (var file in updatedFiles)
            {
                await Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO file_vectors (id, file_id, vector)
                    VALUES (gen_random_uuid(), {file.Id}, to_tsvector('english', {file.Name} || ' ' || {file.Description ?? ""}))
                    ON CONFLICT (file_id) 
                    DO UPDATE SET vector = EXCLUDED.vector;");
            }
        }

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