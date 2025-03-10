using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Data
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

        public DbSet<FileItem> Files { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<UserTag> UserTags { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure composite keys for join tables
            modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });
            modelBuilder.Entity<RolePermission>().HasKey(rp => new { rp.RoleId, rp.PermissionId });

            // Configure relationships
            modelBuilder.Entity<UserRole>()
                        .HasOne(ur => ur.User)
                        .WithMany(u => u.UserRoles)
                        .HasForeignKey(ur => ur.UserId);

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
                new Role { Id = 2, Name = "User",  Description = "Regular user."}
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
                new Permission { Id = 9, Name = "Permissions.Users.Edit", Description = "Can edit users."}
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
    }
}