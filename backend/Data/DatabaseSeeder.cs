using KnowledgeBank.Models;
using Npgsql.Replication;

namespace KnowledgeBank.Data
{
    public static class DatabaseSeeder
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public const string UnknownResourceTypeId = "0cc285a8-0f07-11f0-a0a6-5600051f1387";
        private static DatabaseContext database;
        private static ResourceManager resourceManager;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        /// <summary>
        /// Helper method for common seed logic
        /// </summary>
        /// <returns></returns>
        private static async Task SeedData()
        {
            // Seed data (this part is common between both Seed and SeedTemplate methods)
            
            // Add the unknown resource type if it doesn't exist
            if (!await resourceManager.ResourceTypeExistsAsync(UnknownResourceTypeId))
                await database.ResourceTypes.AddAsync(new() { Id = new Guid(UnknownResourceTypeId), Name = "Unknown" });

            // Create Scientific Article resource type
            await resourceManager.CreateResourceTypeAsync(new ResourceTypeCreateDto { Name = "Scientific Article" });

            // Save changes
            await database.SaveChangesAsync();
        }

        /// <summary>
        /// Method for seeding the database in Program.cs
        /// </summary>
        /// <param name="serviceProvider">All services such as blob, database and resource manager</param>
        /// <returns></returns>
        public static async Task Seed(IServiceProvider serviceProvider)
        {
            // Get database service from the main database
            using IServiceScope scope = serviceProvider.CreateScope();
            database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            resourceManager = scope.ServiceProvider.GetRequiredService<ResourceManager>();

            // Call the common seed logic
            await SeedData();
        }

        /// <summary>
        /// Method for seeding the template databases in TestBase
        /// </summary>
        /// <param name="database">Database to be seeded with a template</param>
        /// <returns></returns>
        public static async Task SeedTemplate(DatabaseContext database)
        {
            DatabaseSeeder.database = database;
            DatabaseSeeder.resourceManager = new ResourceManager(database);
        
            // Call the common seed logic for the template database
            await SeedData();
        }

    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


