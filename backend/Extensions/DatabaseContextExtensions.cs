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
            // Step 0: Enable required extensions for fuzzy search
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        
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
                    ""description"" as ""Description"",
                    ""publication-date"" as ""PublicationDate"",
                    'resource' as ""Type"",
                    CASE
                        WHEN ""filetype"" = 'website' THEN 'website'
                        WHEN ""file-ext"" = NULL THEN 'document'
                        ELSE ""file-ext""
                    END as ""FileType"",
                    ""creation-date"" as ""CreationDate"",
                    
                    -- Create search vector from multiple fields
                    to_tsvector(
                        coalesce(""title"", '') || ' ' ||
                        coalesce(""description"", '') || ' '
                    ) as ""SearchVector""
                FROM ""resources""
                WHERE ""trashed"" = false
                
                UNION ALL
                
                SELECT
                    ""id"" as ""Id"",
                    ""name"" as ""Name"",
                    ""description"" as ""Description"",
                    NULL as ""PublicationDate"",
                    'person' as ""Type"",
                    'person' as ""FileType"",
                    ""creation-date"" as ""CreationDate"",
                    
                    -- Create search vector from multiple fields
                    to_tsvector(
                        coalesce(""name"", '') || ' ' ||
                        coalesce(""description"", '') || ' ' ||
                        coalesce(""email-address"", '') || ' ' ||
                        coalesce(""occupation"", '') || ' '
                    ) as ""SearchVector""
                FROM ""persons""
                
                UNION ALL
                
                SELECT
                    ""id"" as ""Id"",
                    ""name"" as ""Name"",
                    ""description"" as ""Description"",
                    NULL as ""PublicationDate"",
                    'organisation' as ""Type"",
                    'organisation' as ""FileType"",
                    ""creation-date"" as ""CreationDate"",
                    
                    -- Create search vector from multiple fields
                    to_tsvector(
                        coalesce(""name"", '') || ' ' ||
                        coalesce(""description"", '') || ' ' ||
                        coalesce(""email-address"", '') || ' ' ||
                        coalesce(""website"", '') || ' '
                    ) as ""SearchVector""
                FROM ""organisations"";");

            // Step 3: Create indexes
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE UNIQUE INDEX idx_resourcegridview_id ON ResourceGridView(""Id"");");
            
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX idx_resourcegridview_type ON ResourceGridView(""Type"");");
            
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX idx_resourcegridview_name ON ResourceGridView(""Name"");");
                
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX idx_resourcegridview_search ON ResourceGridView USING GIN(""SearchVector"");");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX idx_resourcegridview_name_trgm ON ResourceGridView USING GIN(""Name"" gin_trgm_ops);");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX idx_resourcegridview_desc_trgm ON ResourceGridView USING GIN(""Description"" gin_trgm_ops);");

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