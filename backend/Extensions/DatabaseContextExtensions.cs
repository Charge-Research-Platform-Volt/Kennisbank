using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KnowledgeBank.Extensions;

public static class DatabaseContextExtensions 
{
    public static async Task EnsureViewsCreatedAsync(this DatabaseContext context) 
    {
        await context.CreateResourceGridViewAsync();
    }
    
    public static async Task CreateResourceGridViewAsync(this DatabaseContext context) 
    {
        using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();
    
        try 
        {
            // Step 1: Drop existing objects
            await context.Database.ExecuteSqlRawAsync(@"
                DROP TRIGGER IF EXISTS refresh_grid_on_resource_change ON ""resources"";");
            
            await context.Database.ExecuteSqlRawAsync(@"
                DROP TRIGGER IF EXISTS refresh_grid_on_person_change ON ""persons"";");
            
            await context.Database.ExecuteSqlRawAsync(@"
                DROP TRIGGER IF EXISTS refresh_grid_on_organisation_change ON ""organisations"";");
            
            await context.Database.ExecuteSqlRawAsync(@"
                DROP FUNCTION IF EXISTS refresh_resource_grid_view();");
            
            await context.Database.ExecuteSqlRawAsync(@"
                DROP MATERIALIZED VIEW IF EXISTS ResourceGridView;");

            // Step 2: Create materialized view
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE MATERIALIZED VIEW ResourceGridView AS
                SELECT
                    ""id"" as ""Id"",
                    ""title"" as ""Name"",
                    ""publication-date"" as ""PublicationDate"",
                    'resource' as ""Type"",
                    CASE
                        WHEN ""filetype"" = 'website' THEN 'website'
                        WHEN ""file-ext"" = NULL THEN 'document'
                        ELSE ""file-ext""
                    END as ""FileType""
                FROM ""resources""
                WHERE ""archived"" = false
                
                UNION ALL
                
                SELECT
                    ""id"" as ""Id"",
                    ""name"" as ""Name"",
                    NULL as ""PublicationDate"",
                    'person' as ""Type"",
                    'person' as ""FileType""
                FROM ""persons""
                
                UNION ALL
                
                SELECT
                    ""id"" as ""Id"",
                    ""name"" as ""Name"",
                    NULL as ""PublicationDate"",
                    'organisation' as ""Type"",
                    'organisation' as ""FileType""
                FROM ""organisations"";");

            // Step 3: Create indexes
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE UNIQUE INDEX idx_resourcegridview_id ON ResourceGridView(""Id"");");
            
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX idx_resourcegridview_type ON ResourceGridView(""Type"");");
            
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX idx_resourcegridview_name ON ResourceGridView(""Name"");");

            // Step 4: Create refresh function
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE OR REPLACE FUNCTION refresh_resource_grid_view()
                RETURNS TRIGGER AS $$
                BEGIN
                    REFRESH MATERIALIZED VIEW CONCURRENTLY ResourceGridView;
                    RETURN NULL;
                END;
                $$ LANGUAGE plpgsql;");

            // Step 5: Create triggers
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TRIGGER refresh_grid_on_resource_change
                    AFTER INSERT OR UPDATE OR DELETE ON ""resources""
                    FOR EACH STATEMENT
                    EXECUTE FUNCTION refresh_resource_grid_view();");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TRIGGER refresh_grid_on_person_change
                    AFTER INSERT OR UPDATE OR DELETE ON ""persons""
                    FOR EACH STATEMENT
                    EXECUTE FUNCTION refresh_resource_grid_view();");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TRIGGER refresh_grid_on_organisation_change
                    AFTER INSERT OR UPDATE OR DELETE ON ""organisations""
                    FOR EACH STATEMENT
                    EXECUTE FUNCTION refresh_resource_grid_view();");

            await transaction.CommitAsync();
            Serilog.Log.Information("Materialized view ResourceGridView with triggers created successfully");
        }
        catch (Exception e) 
        {
            await transaction.RollbackAsync();
            Serilog.Log.Error(e, "Failed to create resource grid view");
        }
    }
}