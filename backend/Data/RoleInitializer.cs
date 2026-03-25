using CsvHelper.Configuration.Attributes;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace KnowledgeBank.Data
{
    public class RoleInitializer
    {
        public static readonly string[] roleNames =
        [
            "admin",
            "user"
        ];

        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<User> userManager;
        private readonly OwnerUserConfig ownerConfig;
        
        public RoleInitializer(RoleManager<IdentityRole> roleManager, UserManager<User> userManager, IOptions<OwnerUserConfig> ownerConfig) 
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
            this.ownerConfig = ownerConfig.Value;
        }

        public async Task InitializeAsync()
        {
            // Create all the roles
            foreach (string roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // Check if owner user already exists
            User? existingOwner = await userManager.FindByEmailAsync(ownerConfig.Email);
            
            if (existingOwner == null) 
            {
                // Create the owner user
                User owner = new User(ownerConfig.FirstName, ownerConfig.LastName, ownerConfig.Email);
                IdentityResult adminResult = await userManager.CreateAsync(owner, ownerConfig.Password);

                if (adminResult.Succeeded)
                    await userManager.AddToRoleAsync(owner, "admin");
                else
                    throw new InvalidOperationException($"Failed to create owner user: {string.Join(", ", adminResult.Errors.Select(e => e.Description))}");
            }
            else 
            {
                // Ensure admin role for owner, just to be safe
                if (!await userManager.IsInRoleAsync(existingOwner, "admin"))
                    await userManager.AddToRoleAsync(existingOwner, "admin");
            }
        }
    }
}
