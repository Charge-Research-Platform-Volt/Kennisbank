using System.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KnowledgeBank.Data
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public DbSet<FileItem> Files { get; set; }

        public DbSet<Tag> Tags { get; set; }
        public DbSet<FileTagLink> FileTagLinks { get; set; }
        public DbSet<UserTag> UserTags { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<User> Users { get; set; }
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

            // Configure composite keys for join tables
            modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
            modelBuilder.Entity<RolePermission>().HasKey(rp => new { rp.RoleId, rp.PermissionId });

            // Configure relationships
            // modelBuilder.Entity<UserRole>()
            //             .HasOne(ur => ur.User)
            //             .WithMany(u => u.UserRoles)
            //             .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                        .HasOne(ur => ur.Role)
                        .WithMany(r => r.UserRoles)
                        .HasForeignKey(ur => ur.RoleId);

            modelBuilder.Entity<RolePermission>()
                        .HasOne(rp => rp.Role)
                        .WithMany(r => r.RolePermissions)
                        .HasForeignKey(rp => rp.RoleId);

            modelBuilder.Entity<RolePermission>()
                        .HasOne(rp => rp.Permission)
                        .WithMany(p => p.RolePermissions)
                        .HasForeignKey(rp => rp.PermissionId);

            SeedInitialData(modelBuilder);
        }


        private void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Seed roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Admin", Description = "Administrator with full priviledges." },
                new Role { Id = 2, Name = "User", Description = "Regular user." }
            );

            // Seed permissions
            modelBuilder.Entity<Permission>().HasData(
                // File permissions
                new Permission { Id = 1, Name = "Permissions.Files.Upload", Description = "Can upload files." },
                new Permission { Id = 2, Name = "Permissions.Files.Download", Description = "Can download files." },
                new Permission { Id = 3, Name = "Permissions.Files.Edit", Description = "Can edit files." },
                new Permission { Id = 4, Name = "Permissions.Files.Delete", Description = "Can delete files." },
                new Permission { Id = 5, Name = "Permissions.Files.View", Description = "Can view files." },

                // User permissions
                new Permission { Id = 6, Name = "Permissions.Users.View", Description = "Can view users." },
                new Permission { Id = 7, Name = "Permissions.Users.Create", Description = "Can add new users." },
                new Permission { Id = 8, Name = "Permissions.Users.Delete", Description = "Can delete users." },
                new Permission { Id = 9, Name = "Permissions.Users.Edit", Description = "Can edit users." }
            );

            modelBuilder.Entity<RolePermission>().HasData(
                // Admin permissions (all)
                new RolePermission { RoleId = 1, PermissionId = 1 },
                new RolePermission { RoleId = 1, PermissionId = 2 },
                new RolePermission { RoleId = 1, PermissionId = 3 },
                new RolePermission { RoleId = 1, PermissionId = 4 },
                new RolePermission { RoleId = 1, PermissionId = 5 },
                new RolePermission { RoleId = 1, PermissionId = 6 },
                new RolePermission { RoleId = 1, PermissionId = 7 },
                new RolePermission { RoleId = 1, PermissionId = 8 },
                new RolePermission { RoleId = 1, PermissionId = 9 },

                // User permissions
                new RolePermission { RoleId = 2, PermissionId = 1 },
                new RolePermission { RoleId = 2, PermissionId = 2 },
                new RolePermission { RoleId = 2, PermissionId = 5 },
                new RolePermission { RoleId = 2, PermissionId = 6 }
            );
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
            // Begin database transaction to ensure consistency
            using IDbContextTransaction transaction = await Database.BeginTransactionAsync(cancellationToken);

            try
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

                // Commit the transaction
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                // If anything goes wrong, roll back transaction to ensure consistency
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
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