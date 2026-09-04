using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace KnowledgeBank.Data
{
    public class DatabaseContext : IdentityDbContext<User>
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public DbSet<Resource> Resources { get; set; }
        public DbSet<ResourceChunk> ResourceChunks { get; set; }
        public DbSet<EntityChunk> EntityChunks { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ResourceTagRelation> ResourceTagRelations { get; set; }
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<Entity> Entities { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Organisation> Organisations { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<Journal> Journals { get; set; }
        public DbSet<ResourceType> ResourceTypes { get; set; }
        public DbSet<Chats> Chats { get; set; }
        public DbSet<Messages> Messages { get; set; }
        public DbSet<MessageAttachments> MessageAttachments { get; set; }

        public DbSet<ResourceAuthorRelation> ResourceAuthorRelations { get; set; }
        public DbSet<ResourceRelatedPersonRelation> ResourceRelatedPersonRelations { get; set; }
        public DbSet<ResourceOrganisationRelation> ResourceOrganisationRelations { get; set; }
        public DbSet<PersonOrganisationRelation> PersonOrganisationRelations { get; set; }
        public DbSet<PersonRelationship> PersonRelationships { get; set; }
        public DbSet<OrganisationRelationship> OrganisationRelationships { get; set; }
        public DbSet<ResourceRegionRelation> ResourceRegionRelations { get; set; }
        public DbSet<ProjectFolderRelation> ProjectFolderRelations { get; set; }
        public DbSet<ProjectTagRelation> ProjectTagRelations { get; set; }
        public DbSet<ProjectItemRelation> ProjectItemRelations { get; set; }
        public DbSet<ProjectMemberRelation> ProjectMemberRelations { get; set; }
        
        
        public DbSet<LibraryItem> LibraryItems { get; set; }
        public DbSet<LibrarySearchResult> LibrarySearchResults { get; set; }
        public DbSet<TrashItem> TrashItems { get; set; }

        public DbSet<ChangelogEntry> Changelog { get; set; }

        public DbSet<DismissedMergeSuggestion> DismissedMergeSuggestions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure TPT inheritance for Entity hierarchy
            modelBuilder.Entity<Entity>()
                .UseTptMappingStrategy();

            modelBuilder.Entity<Entity>()
                .Property(e => e.EmbeddingStatus)
                .HasConversion<string>();

            modelBuilder.Entity<Resource>()
                .Property(r => r.EmbeddingStatus)
                .HasConversion<string>();

            modelBuilder.Entity<Person>()
                .ToTable("persons");

            modelBuilder.Entity<Organisation>()
                .ToTable("organisations");

            modelBuilder.Entity<ResourceTagRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.TagId });

            modelBuilder.Entity<ResourceAuthorRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.AuthorId });

            modelBuilder.Entity<ResourceAuthorRelation>()
                .HasOne(ra => ra.Author)
                .WithMany(e => e.ResourceAuthorRelations)
                .HasForeignKey(ra => ra.AuthorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ResourceRelatedPersonRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.PersonId });

            modelBuilder.Entity<ResourceOrganisationRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.OrganisationId });

            modelBuilder.Entity<PersonOrganisationRelation>()
                .HasKey(ft => new { ft.PersonId, ft.OrganisationId });

            modelBuilder.Entity<PersonRelationship>()
                .HasKey(ft => new { ft.SourcePersonId, ft.TargetPersonId });

            modelBuilder.Entity<PersonRelationship>()
                .HasOne(or => or.SourcePerson)
                .WithMany(o => o.TargetRelationships)
                .HasForeignKey(or => or.SourcePersonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PersonRelationship>()
                .HasOne(or => or.TargetPerson)
                .WithMany(o => o.SourceRelationships)
                .HasForeignKey(or => or.TargetPersonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrganisationRelationship>()
                .HasKey(ft => new { ft.SourceOrganisationId, ft.TargetOrganisationId });

            modelBuilder.Entity<OrganisationRelationship>()
                .HasOne(or => or.SourceOrganisation)
                .WithMany(o => o.TargetRelationships)
                .HasForeignKey(or => or.SourceOrganisationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrganisationRelationship>()
                .HasOne(or => or.TargetOrganisation)
                .WithMany(o => o.SourceRelationships)
                .HasForeignKey(or => or.TargetOrganisationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ResourceRegionRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.RegionId });


            // Resource chunks
            modelBuilder.Entity<ResourceChunk>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Relationship to Resource with cascade delete
                entity.HasOne(rc => rc.Resource)
                    .WithMany()
                    .HasForeignKey(rc => rc.ResourceId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Store enum as string for readability
                entity.Property(e => e.ChunkType)
                    .HasConversion<string>();

                // Configure vector column with proper pgvector type
                entity.Property(e => e.Embedding)
                    .HasColumnType("vector(1024)");

                // Index on resource-id for fast lookups
                entity.HasIndex(e => e.ResourceId)
                    .HasDatabaseName("idx_resource_chunks_resource_id");

                // Composite index for common query pattern
                entity.HasIndex(e => new { e.ResourceId, e.ChunkPart })
                    .HasDatabaseName("id_resource_chunks_resource_part");
            });

            // Entity chunks
            modelBuilder.Entity<EntityChunk>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(ec => ec.Entity)
                    .WithMany()
                    .HasForeignKey(ec => ec.EntityId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.ChunkType)
                    .HasConversion<string>();

                entity.Property(e => e.Embedding)
                    .HasColumnType("vector(1024)");

                entity.HasIndex(e => e.EntityId)
                    .HasDatabaseName("idx_entity_chunks_entity_id");
            });

            // Configure Chat entity
            modelBuilder.Entity<Chats>()
                .HasKey(c => c.Id);

            // Configure Message entity
            modelBuilder.Entity<Messages>(entity =>
             {
                 entity.HasKey(e => e.Id);

                 // Configure relationships
                 entity.HasOne(m => m.Chat)
                       .WithMany(c => c.Messages)
                       .HasForeignKey(m => m.ChatId)
                       .OnDelete(DeleteBehavior.Cascade);
             });

            modelBuilder.Entity<MessageAttachments>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(a => a.Chat)
                      .WithMany()
                      .HasForeignKey(a => a.ChatId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.Message)
                      .WithMany()
                      .HasForeignKey(a => a.MessageId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Library view
            modelBuilder.Entity<LibraryItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToView("libraryview");
                entity.Property(e => e.EmbeddingStatus)
                    .HasConversion<string>();
            });

            modelBuilder.Entity<LibrarySearchResult>(entity =>
            {
                entity.HasNoKey();
                entity.ToView(null);
            });

            // Relation indexes for better query performance
            modelBuilder.Entity<ResourceTagRelation>()
                .HasIndex(rt => rt.TagId)
                .HasDatabaseName("idx_resource_tag_tag_id");

            modelBuilder.Entity<ResourceRegionRelation>()
                .HasIndex(rr => rr.RegionId)
                .HasDatabaseName("idx_resource_region_region_id");
            // Projects
            modelBuilder.Entity<ProjectItemRelation>()
                .HasKey(pir => new { pir.ProjectId, pir.ItemId });

            modelBuilder.Entity<ProjectMemberRelation>()
                .HasKey(pcr => new { pcr.ProjectId, pcr.UserId });

            modelBuilder.Entity<ProjectTagRelation>()
                .HasKey(ptr => new { ptr.ProjectId, ptr.TagId });

            modelBuilder.Entity<ProjectFolderRelation>()
                .HasKey(pfr => new { pfr.ParentId, pfr.ChildId });

            modelBuilder.Entity<ProjectFolderRelation>()
                .HasOne(pfr => pfr.ParentFolder)
                .WithMany(u => u.ChildFolders)
                .HasForeignKey(pfr => pfr.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectFolderRelation>()
                .HasOne(pfr => pfr.ChildFolder)
                .WithMany(f => f.ParentFolders)
                .HasForeignKey(pfr => pfr.ChildId)
                .OnDelete(DeleteBehavior.Cascade);

            // Trash view
            modelBuilder.Entity<TrashItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToView("trashview");
            });

            // Dismissed merge suggestions
            modelBuilder.Entity<DismissedMergeSuggestion>()
                .HasKey(d => new { d.EntityType, d.Id1, d.Id2 });

            base.OnModelCreating(modelBuilder);
        }
    }
}
