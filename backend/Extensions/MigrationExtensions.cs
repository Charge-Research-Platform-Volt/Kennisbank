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
            context.Database.Migrate();
    }
}