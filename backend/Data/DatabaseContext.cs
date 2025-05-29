using System.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace KnowledgeBank.Data
{
    /// <summary>
    /// Represents the database context for the KnowledgeBank application.
    /// /// This context is used to interact with the database and manage entities such as resources, tags, users, and their relationships.
    // It inherits from IdentityDbContext to provide user authentication and authorization features.
    ///
    /// Author: Elia Jabbour, Rens van Moorsel, Jelle van het Schut, Aiden van Dijk, Abel Dietrich, Jason van Otterlo, Justin Liem
    /// </summary>
    public class DatabaseContext : IdentityDbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public DbSet<Resource> Resources { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ResourceTagRelation> ResourceTagRelations { get; set; }
        public DbSet<User> AppUsers { get; set; } // Renamed to avoid conflict with IdentityDbContext.Users
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<ResourceVector> Vectors { get; set; }
        public DbSet<DocumentMetadata> DocumentMetadata { get; set; }
        public DbSet<WebsiteMetadata> WebsiteMetadata { get; set; }
        public DbSet<AudioMetadata> AudioMetadata { get; set; }
        public DbSet<VideoMetadata> VideoMetadata { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Organisation> Organisations { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<ResourceType> ResourceTypes { get; set; }


        public DbSet<ResourceAuthorRelation> ResourceAuthorRelations { get; set; }
        public DbSet<ResourceRelatedPersonRelation> ResourceRelatedPersonRelations { get; set; }
        public DbSet<ResourceOrganisationRelation> ResourceOrganisationRelations { get; set; }
        public DbSet<ResourceRelatedOrganisationRelation> ResourceRelatedOrganisationRelations { get; set; }
        public DbSet<PersonOrganisationRelation> PersonOrganisationRelations { get; set; }
        public DbSet<PersonRelationship> PersonRelationships { get; set; }
        public DbSet<OrganisationRelationship> OrganisationRelationships { get; set; }
        public DbSet<ResourceRegionRelation> ResourceRegionRelations { get; set; }
        public DbSet<ResourceSourceRelation> ResourceSourceRelations { get; set; }
        public DbSet<ResourceRelatedSourceRelation> ResourceRelatedSourceRelations { get; set; }

        /// <summary>
        /// Configures the model for the database context, including relationships and constraints.
        /// 
        /// Author: Elia Jabbour, Abel Dietrich
        /// </summary>
        /// <param name="modelBuilder">The model builder used to configure the model.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Resource>()
                .HasOne(f => f.Vector)
                .WithOne(v => v.Resource)
                .HasForeignKey<ResourceVector>(v => v.ResourceId)
                .OnDelete(DeleteBehavior.Cascade);    // Automatically deletes vector on file delete

            modelBuilder.Entity<ResourceTagRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.TagId }); // Define composite primary key

            modelBuilder.Entity<ResourceAuthorRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.PersonId });

            modelBuilder.Entity<ResourceRelatedPersonRelation>()
               .HasKey(ft => new { ft.ResourceId, ft.PersonId });

            modelBuilder.Entity<ResourceOrganisationRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.OrganisationId });

            modelBuilder.Entity<ResourceRelatedOrganisationRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.OrganisationId });

            modelBuilder.Entity<PersonOrganisationRelation>()
                .HasKey(ft => new { ft.PersonId, ft.OrganisationId });

            modelBuilder.Entity<PersonRelationship>()
                .HasKey(ft => new { ft.SourcePersonId, ft.TargetPersonId });

            modelBuilder.Entity<OrganisationRelationship>()
                .HasKey(ft => new { ft.SourceOrganisationId, ft.TargetOrganisationId });

            modelBuilder.Entity<ResourceRegionRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.RegionId });

            modelBuilder.Entity<ResourceSourceRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.Url });

            modelBuilder.Entity<ResourceRelatedSourceRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.Url });

            modelBuilder.Entity<OrganisationRelationship>()
                .HasOne(or => or.SourceOrganisation)
                .WithMany(o => o.TargetRelationships)
                .HasForeignKey(or => or.SourceOrganisationId);

            modelBuilder.Entity<OrganisationRelationship>()
                .HasOne(or => or.TargetOrganisation)
                .WithMany(o => o.SourceRelationships)
                .HasForeignKey(or => or.TargetOrganisationId);

            modelBuilder.Entity<PersonRelationship>()
                .HasOne(or => or.SourcePerson)
                .WithMany(o => o.TargetRelationships)
                .HasForeignKey(or => or.SourcePersonId);

            modelBuilder.Entity<PersonRelationship>()
                .HasOne(or => or.TargetPerson)
                .WithMany(o => o.SourceRelationships)
                .HasForeignKey(or => or.TargetPersonId);

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Saves database changes and ensures that any file updates related to FileItem entities 
        /// are processed within the same transaction. This guarantees consistency between the database 
        /// and the file system and between FileItem entities and their search vectors. 
        /// 
        /// Author: Abel Dietrich, Elia Jabbour, Jelle van het Schut       
        /// </summary>
        /// <param name="cancellationToken">A token used to observe operation cancellation.</param>
        /// <returns>The number of state entries written to the database.</returns>
        public async Task<int> SaveResourceChangesAsync(CancellationToken cancellationToken = default)
        {
            // Get files that were added or modified
            List<Resource> updatedResources = ChangeTracker.Entries<Resource>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .Select(e => e.Entity)
                .ToList();

            // First save the changes to the database
            int result = await base.SaveChangesAsync(cancellationToken);

            // If there are any updated files, update their search vectors
            if (updatedResources.Count != 0) await UpdateResourceVectorAsync(updatedResources);

            return result;
        }

        /// <summary>
        /// Updates or inserts full-text search vectors for the given list of updated FileItem entities. 
        /// If a vector already exists for a file, it is updated. Otherwise, a new vector is inserted.
        /// 
        /// Author: Abel Dietrich, Jelle van het Schut, Rens van Moorsel
        /// </summary>
        /// <param name="updatedResources">A list of files that were added or modified.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        private async Task UpdateResourceVectorAsync(List<Resource> updatedResources)
        {
            // Generates an Enumerable<Task> of SQL queries that inserts the
            // vector, if there's a conflict, replace existing vector instead
            foreach (Resource resource in updatedResources)
            {
                await Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO ""resource-vectors"" (id, ""resource-id"", vector)
                    VALUES (gen_random_uuid(), {resource.Id}, to_tsvector('english', {resource.Title} || ' ' || {resource.Description ?? ""}))
                    ON CONFLICT (""resource-id"") 
                    DO UPDATE SET vector = EXCLUDED.vector;");
            }

        }
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


