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

        public DbSet<Tag> Tags { get; set; }
        public DbSet<FileTagLink> FileTagLinks { get; set; }
        public DbSet<UserTag> UserTags { get; set; }
        public DbSet<User> AppUsers { get; set; } // Renamed to avoid conflict with IdentityDbContext.Users
        public DbSet<FileVector> Vectors { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Organisation> Organisations {get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<DocAuthorLink> DocAuthors { get; set; }
        public DbSet<DocRelatedPersonLink> DocRelatedPersons { get; set; }
        public DbSet<DocOrganisationLink> DocOrganisations { get; set; }
        public DbSet<DocRelatedOrganisationLink> DocRelatedOrganisations { get; set; }
        public DbSet<PersonOrganisationLink> PersonOrganisations { get; set;}
        public DbSet<PersonPersonLink> PersonPersons { get; set; }
        public DbSet<OrganisationOrganisationLink> OrganisationOrganisations { get; set; }
        public DbSet<DocRegionLink> DocRegions { get; set; }
        public DbSet<DocSourceLink> DocSources { get; set; }
        public DbSet<DocRelatedSourceLink> DocRelatedSources { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileItem>()
                .HasOne(f => f.Vector)
                .WithOne(v => v.File)
                .HasForeignKey<FileVector>(v => v.FileId)
                .OnDelete(DeleteBehavior.Cascade);    // Automatically deletes vector on file delete

            modelBuilder.Entity<FileTagLink>()
                .HasKey(ft => new { ft.DocId, ft.TagId }); // Define composite primary key

            modelBuilder.Entity<DocAuthorLink>()
                .HasKey(ft => new { ft.DocId, ft.PersonId});
                
            modelBuilder.Entity<DocRelatedPersonLink>()
               .HasKey(ft => new { ft.DocId, ft.PersonId});
                
            modelBuilder.Entity<DocOrganisationLink>()
                .HasKey(ft => new { ft.DocId, ft.OrganisationId});

            modelBuilder.Entity<DocRelatedOrganisationLink>()
                .HasKey(ft => new { ft.DocId, ft.OrganisationId});
                
            modelBuilder.Entity<PersonOrganisationLink>()
                .HasKey(ft => new { ft.PersonId, ft.OrganisationId});

            modelBuilder.Entity<PersonPersonLink>()
                .HasKey(ft => new { ft.PersonId, ft.PersonId2});
                
            modelBuilder.Entity<OrganisationOrganisationLink>()
                .HasKey(ft => new { ft.OrganisationId, ft.OrganisationId2});
                
            modelBuilder.Entity<DocRegionLink>()
                .HasKey(ft => new { ft.DocId, ft.RegionId});
                
            modelBuilder.Entity<DocSourceLink>()
                .HasKey(ft => new { ft.DocId, ft.Source});
                
            modelBuilder.Entity<DocRelatedSourceLink>()
                .HasKey(ft => new { ft.DocId, ft.Source});

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
            var updateTasks = updatedFiles.Select(file =>
                Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO file_vectors (id, file_id, vector)
                    VALUES (gen_random_uuid(), {file.Id}, to_tsvector('english', {file.Name} || ' ' || {file.Description}))
                    ON CONFLICT (file_id) 
                    DO UPDATE SET vector = EXCLUDED.vector;"));

            // Task that will complete when all subtasks have completed
            await Task.WhenAll(updateTasks);
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