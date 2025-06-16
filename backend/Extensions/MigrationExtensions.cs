using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();
        using DatabaseContext context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        // Apply migrations only if there are any pending migrations
        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
        }
    }

    public static void EnsureDeletedDatabase(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();
        using DatabaseContext context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        context.Database.EnsureDeleted();
    }

    public static void EnsureCreatedDatabase(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();
        using DatabaseContext context = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        context.Database.EnsureCreated();
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


