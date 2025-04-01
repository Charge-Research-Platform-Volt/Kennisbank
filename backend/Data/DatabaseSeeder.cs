using backend.Responses;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;

namespace backend.Data
{
    public static class DatabaseSeeder
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        private static DatabaseContext database;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        public static async Task Seed(IServiceProvider serviceProvider)
        {
            // Get database service
            using IServiceScope scope = serviceProvider.CreateScope();
            database = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            // Seed data
            await database.ResourceTypes.AddAsync(new() { Id = new Guid("0cc285a8-0f07-11f0-a0a6-5600051f1387"), Name = "Unknown"});

            // Save changes
            await database.SaveChangesAsync();
        }
    }
}
