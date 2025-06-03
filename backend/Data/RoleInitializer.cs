using KnowledgeBank.Models;
using Microsoft.AspNetCore.Identity;

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
            User admin = new User("Admin", "The Admin", "admin@admin.nl");
            IdentityResult adminResult = await userManager.CreateAsync(admin, "Admin123!");

            //Create default User user
            User user = new User("User", "The User", "user@user.nl");
            IdentityResult userResult = await userManager.CreateAsync(user, "User123!");

            if (adminResult.Succeeded)
                await userManager.AddToRoleAsync(admin, "admin");
            if (userResult.Succeeded)
                await userManager.AddToRoleAsync(user, "user");
        }
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


