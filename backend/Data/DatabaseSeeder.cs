namespace KnowledgeBank.Data
{
    public static class DatabaseSeeder
    {
        public const string UnknownResourceTypeId = "0cc285a8-0f07-11f0-a0a6-5600051f1387";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private static DatabaseContext database;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        // Helper method for common seed logic
        private static async Task SeedData(DatabaseContext database)
        {
            // Seed data (this part is common between both Seed and SeedTemplate methods)
            await database.ResourceTypes.AddAsync(new() { Id = new Guid(UnknownResourceTypeId), Name = "Unknown" });

            // Save changes
            await database.SaveChangesAsync();
        }

        // Method for seeding the database in Program.cs
        public static async Task Seed(IServiceProvider serviceProvider)
        {
            // Get database service from the main database
            using IServiceScope scope = serviceProvider.CreateScope();
            database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            // Call the common seed logic
            await SeedData(database);
        }

        // Method for seeding the template databases in TestBase
        public static async Task SeedTemplate(DatabaseContext database)
        {
            // Call the common seed logic for the template database
            await SeedData(database);
        }

    }
}
