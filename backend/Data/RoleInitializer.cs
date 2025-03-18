using KnowledgeBank.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Data
{
    public static class RoleInitializer
    {
        public static readonly string[] roleNames =
        {
            "admin",
            "user"
        };

        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            UserManager<User> userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            // Create all the roles
            foreach (string roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            // Create a default admin user
            User user = new User { Email = "admin@admin.nl", UserName = "admin@admin.nl" };
            IdentityResult result = await userManager.CreateAsync(user, "Admin123!");

            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, "admin");
        }
    }
}
