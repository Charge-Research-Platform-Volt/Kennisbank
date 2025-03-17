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

            context.Database.ExecuteSqlRaw("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            context.Database.ExecuteSqlRaw("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_name_trgm ON files USING GIN (name gin_trgm_ops);");
            context.Database.ExecuteSqlRaw("CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_description_trgm ON files USING GIN (description gin_trgm_ops);");
            context.Database.ExecuteSqlRaw("ALTER TABLE file_vectors ALTER COLUMN vector SET DATA TYPE tsvector USING vector::tsvector;");
            context.Database.ExecuteSqlRaw("CREATE INDEX idx_file_vector ON file_vectors USING GIN(vector);");

        }
    }
}