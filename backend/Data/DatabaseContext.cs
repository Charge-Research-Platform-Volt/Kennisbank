using System.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace KnowledgeBank.Data
{
    public class DatabaseContext : IdentityDbContext<User>
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public DbSet<Resource> Resources { get; set; }
        public DbSet<Project> Projects {get; set;}
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ResourceTagRelation> ResourceTagRelations { get; set; }
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<DocumentMetadata> DocumentMetadata { get; set; }
        public DbSet<WebsiteMetadata> WebsiteMetadata { get; set; }
        public DbSet<AudioMetadata> AudioMetadata { get; set; }
        public DbSet<VideoMetadata> VideoMetadata { get; set; }
        public DbSet<Person> Persons { get; set; }
        public DbSet<Organisation> Organisations {get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<ResourceType> ResourceTypes { get; set; }


        public DbSet<ResourceAuthorRelation> ResourceAuthorRelations { get; set; }
        public DbSet<ResourceRelatedPersonRelation> ResourceRelatedPersonRelations { get; set; }
        public DbSet<ResourceOrganisationRelation> ResourceOrganisationRelations { get; set; }
        public DbSet<ResourceRelatedOrganisationRelation> ResourceRelatedOrganisationRelations { get; set; }
        public DbSet<PersonOrganisationRelation> PersonOrganisationRelations { get; set;}
        public DbSet<PersonRelationship> PersonRelationships { get; set; }
        public DbSet<OrganisationRelationship> OrganisationRelationships { get; set; }
        public DbSet<ResourceRegionRelation> ResourceRegionRelations { get; set; }
        public DbSet<ResourceSourceRelation> ResourceSourceRelations { get; set; }
        public DbSet<ResourceRelatedSourceRelation> ResourceRelatedSourceRelations { get; set; }
        public DbSet<ProjectFolderRelation> ProjectFolderRelations { get; set; }
        public DbSet<ProjectTagRelation> ProjectTagRelations { get; set; }
        public DbSet<ProjectResourceRelation> ProjectResourceRelations { get; set; }
        public DbSet<ProjectCreatorRelation> ProjectCreatorRelations { get; set; }
        
        
        public DbSet<ResourceGridItem> ResourceGridItems { get; set; }
        public DbSet<ResourceGridSearchResult> ResourceGridSearchResults { get; set; }
        public DbSet<ResourceTrashItem> ResourceTrashItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("pg_trgm");

            modelBuilder.Entity<ResourceTagRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.TagId }); // Define composite primary key

            modelBuilder.Entity<ResourceAuthorRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.AuthorId});
                
            modelBuilder.Entity<ResourceRelatedPersonRelation>()
               .HasKey(ft => new { ft.ResourceId, ft.PersonId});
                
            modelBuilder.Entity<ResourceOrganisationRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.OrganisationId});

            modelBuilder.Entity<ResourceRelatedOrganisationRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.OrganisationId});
                
            modelBuilder.Entity<PersonOrganisationRelation>()
                .HasKey(ft => new { ft.PersonId, ft.OrganisationId});

            modelBuilder.Entity<PersonRelationship>()
                .HasKey(ft => new { ft.SourcePersonId, ft.TargetPersonId});
                
            modelBuilder.Entity<OrganisationRelationship>()
                .HasKey(ft => new { ft.SourceOrganisationId, ft.TargetOrganisationId});
                
            modelBuilder.Entity<ResourceRegionRelation>()
                .HasKey(ft => new { ft.ResourceId, ft.RegionId});
                
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

            // Projects
            modelBuilder.Entity<ProjectResourceRelation>()
                .HasKey(prr => new {prr.ProjectId, prr.ResourceId});

            modelBuilder.Entity<ProjectCreatorRelation>()
                .HasKey(pcr => new {pcr.ProjectId, pcr.CreatorId});

            modelBuilder.Entity<ProjectTagRelation>()
                .HasKey(ptr => new { ptr.ProjectId, ptr.TagId });

            modelBuilder.Entity<ProjectFolderRelation>()
                .HasKey(pfr => new { pfr.ParentId, pfr.ChildId});

            modelBuilder.Entity<ProjectFolderRelation>()
                .HasOne(pfr => pfr.ParentFolder)
                .WithMany(u => u.ChildFolders)
                .HasForeignKey(pfr => pfr.ParentId);

            modelBuilder.Entity<ProjectFolderRelation>()
                .HasOne(pfr => pfr.ChildFolder)
                .WithMany(f => f.ParentFolders)
                .HasForeignKey(pfr => pfr.ChildId);

            // Resource grid view
            modelBuilder.Entity<ResourceGridItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToView("resourcegridview");
            });

            modelBuilder.Entity<ResourceGridSearchResult>(entity =>
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

            // Resource trash view
            modelBuilder.Entity<ResourceTrashItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToView("resourcetrashview");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


