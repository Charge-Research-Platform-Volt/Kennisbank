using KnowledgeBank.Models;
using Microsoft.AspNetCore.Identity;

namespace KnowledgeBank.Data
{
    /// <summary>
    /// Initializes roles in the KnowledgeBank application.
    /// This class is responsible for creating the default roles and assigning them to users.
    /// 
    /// Author: Abel Dietrich, Jason van Otterlo
    /// </summary>
    public static class RoleInitializer
    {
        public static readonly string[] roleNames =
        {
            "admin",
            "user"
        };

        /// <summary>
        /// Initializes the roles in the application.
        /// This method creates the roles defined in the roleNames array if they do not already exist,
        /// and creates a default admin user with the "admin" role and a default user with the "user" role.
        /// It should be called during the application startup to ensure that the roles are set up correctly.
        /// 
        /// Author: Abel Dietrich, Jason van Otterlo
        /// </summary>
        /// <param name="serviceProvider">The service provider to resolve dependencies.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
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
            User admin = new User { Email = "admin@admin.nl", UserName = "admin@admin.nl" };
            IdentityResult adminResult = await userManager.CreateAsync(admin, "Admin123!");

            //Create default User user
            User user = new User { Email = "user@user.nl", UserName = "user@user.nl" };
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


