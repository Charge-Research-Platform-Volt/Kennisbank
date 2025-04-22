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

            CreateVectors(context);
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

        if (context.Database.EnsureCreated()) 
            CreateVectors(context);
    }

    private static void CreateVectors(DatabaseContext context)
    {
        context.Database.ExecuteSqlRaw("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        context.Database.ExecuteSqlRaw("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_title_trgm ON resources USING GIN (title gin_trgm_ops);");
        context.Database.ExecuteSqlRaw("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_description_trgm ON resources USING GIN (description gin_trgm_ops);");
        context.Database.ExecuteSqlRaw("ALTER TABLE \"resource-vectors\" ALTER COLUMN vector SET DATA TYPE tsvector USING vector::tsvector;");
        context.Database.ExecuteSqlRaw("CREATE INDEX idx_resource_vector ON \"resource-vectors\" USING GIN(vector);");
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


