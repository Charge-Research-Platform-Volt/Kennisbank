using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.Replication;

namespace KnowledgeBank.Extensions;

public static class DatabaseContextExtensions 
{
    public static async Task EnsureDatabaseSetupAsync(this DatabaseContext context) 
    {
        await context.EnsureVectorExtensionsCreatedAsync();
        await context.CreateLibraryViewAsync();
        await context.CreateTrashViewAsync();
    }
    
    public static async Task EnsureVectorExtensionsCreatedAsync(this DatabaseContext context) 
    {
        try 
        {
            // Enable required extensions
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE EXTENSION IF NOT EXISTS vector;
                CREATE EXTENSION IF NOT EXISTS pg_trgm;
            ");

            // Create HNSW index for resource semantic search
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX IF NOT EXISTS idx_resource_chunks_embedding_hnsw
                ON ""resource-chunks""
                USING hnsw (embedding vector_cosine_ops)
                WITH (m = 16, ef_construction = 100);
            ");

            // Create trigram index for resource text search
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX IF NOT EXISTS idx_resource_chunks_text_trgm
                ON ""resource-chunks""
                USING GIN (""chunk-text"" gin_trgm_ops);
            ");

            // Create HNSW index for entity semantic search
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX IF NOT EXISTS idx_entity_chunks_embedding_hnsw
                ON ""entity-chunks""
                USING hnsw (embedding vector_cosine_ops)
                WITH (m = 16, ef_construction = 100);
            ");

            // Create trigram index for entity text search
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX IF NOT EXISTS idx_entity_chunks_text_trgm
                ON ""entity-chunks""
                USING GIN (""chunk-text"" gin_trgm_ops);
            ");
        }
        catch (Exception e) 
        {
            Serilog.Log.Error(e, "Failed to create vector extensions/indexed");
        }
    }
    
    public static async Task CreateLibraryViewAsync(this DatabaseContext context)
    {
        using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Step 1: Drop existing objects
            // (old resource-grid-named objects are dropped too, to clean up databases from before the LibraryView rename)
            await context.Database.ExecuteSqlRawAsync(@"
                DROP TRIGGER IF EXISTS refresh_grid_on_resource_change ON ""resources"";
                DROP TRIGGER IF EXISTS refresh_library_on_resource_change ON ""resources"";");

            await context.Database.ExecuteSqlRawAsync(@"
                DROP TRIGGER IF EXISTS refresh_grid_on_entity_change ON ""entities"";
                DROP TRIGGER IF EXISTS refresh_library_on_entity_change ON ""entities"";");

            await context.Database.ExecuteSqlRawAsync(@"
                DROP TRIGGER IF EXISTS refresh_grid_on_person_change ON ""persons"";
                DROP TRIGGER IF EXISTS refresh_library_on_person_change ON ""persons"";");

            await context.Database.ExecuteSqlRawAsync(@"
                DROP TRIGGER IF EXISTS refresh_grid_on_organisation_change ON ""organisations"";
                DROP TRIGGER IF EXISTS refresh_library_on_organisation_change ON ""organisations"";");

            await context.Database.ExecuteSqlRawAsync(@"
                DROP FUNCTION IF EXISTS refresh_resource_grid_view();
                DROP FUNCTION IF EXISTS refresh_library_view();");

            await context.Database.ExecuteSqlRawAsync(@"
                DROP MATERIALIZED VIEW IF EXISTS ResourceGridView;
                DROP MATERIALIZED VIEW IF EXISTS LibraryView;");

            // Step 2: Create materialized view
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE MATERIALIZED VIEW LibraryView AS
                SELECT
                    ""id"" as ""Id"",
                    ""embedding-status"" as ""EmbeddingStatus"",
                    ""title"" as ""Name"",
                    ""description"" as ""Description"",
                    ""publication-date"" as ""PublicationDate"",
                    ""publication-date-precision"" as ""PublicationDatePrecision"",
                    'resource' as ""Type"",
                    CASE
                        WHEN ""filetype"" = 'website' THEN 'website'
                        WHEN ""file-ext"" = NULL THEN 'document'
                        ELSE ""file-ext""
                    END as ""FileType"",
                    ""source-url"" as ""SourceUrl"",
                    ""created-on"" as ""CreatedOn"",
                    ""type-id"" as ""TypeId"",
                    ""journal-id"" as ""JournalId"",
                    NULL::text[] as ""Aliases"",

                    -- Create search vector from multiple fields
                    to_tsvector(
                        coalesce(""title"", '') || ' ' ||
                        coalesce(""description"", '') || ' '
                    ) as ""SearchVector""
                FROM ""resources""
                WHERE ""trashed"" = false

                UNION ALL

                SELECT
                    p.""id"" as ""Id"",
                    e.""embedding-status"" as ""EmbeddingStatus"",
                    e.""name"" as ""Name"",
                    e.""description"" as ""Description"",
                    NULL as ""PublicationDate"",
                    2 as ""PublicationDatePrecision"",
                    'person' as ""Type"",
                    'person' as ""FileType"",
                    NULL as ""SourceUrl"",
                    e.""created-on"" as ""CreatedOn"",
                    NULL::uuid as ""TypeId"",
                    NULL::uuid as ""JournalId"",
                    e.""aliases"" as ""Aliases"",

                    -- Create search vector from multiple fields
                    to_tsvector(
                        coalesce(e.""name"", '') || ' ' ||
                        coalesce(e.""description"", '') || ' ' ||
                        coalesce(e.""email-address"", '') || ' ' ||
                        coalesce(p.""occupation"", '') || ' ' ||
                        coalesce(array_to_string(e.""aliases"", ' '), '') || ' '
                    ) as ""SearchVector""
                FROM ""persons"" p
                INNER JOIN ""entities"" e ON p.""id"" = e.""id""
                WHERE e.""trashed"" = false

                UNION ALL

                SELECT
                    o.""id"" as ""Id"",
                    e.""embedding-status"" as ""EmbeddingStatus"",
                    e.""name"" as ""Name"",
                    e.""description"" as ""Description"",
                    NULL as ""PublicationDate"",
                    2 as ""PublicationDatePrecision"",
                    'organisation' as ""Type"",
                    'organisation' as ""FileType"",
                    NULL as ""SourceUrl"",
                    e.""created-on"" as ""CreatedOn"",
                    NULL::uuid as ""TypeId"",
                    NULL::uuid as ""JournalId"",
                    e.""aliases"" as ""Aliases"",

                    -- Create search vector from multiple fields
                    to_tsvector(
                        coalesce(e.""name"", '') || ' ' ||
                        coalesce(e.""description"", '') || ' ' ||
                        coalesce(e.""email-address"", '') || ' ' ||
                        coalesce(o.""website"", '') || ' ' ||
                        coalesce(array_to_string(e.""aliases"", ' '), '') || ' '
                    ) as ""SearchVector""
                FROM ""organisations"" o
                INNER JOIN ""entities"" e ON o.""id"" = e.""id""
                WHERE e.""trashed"" = false
            ;");

            // Step 3: Create indexes
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE UNIQUE INDEX idx_libraryview_id ON LibraryView(""Id"");");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX idx_libraryview_type ON LibraryView(""Type"");");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX idx_libraryview_name ON LibraryView(""Name"");");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX idx_libraryview_search ON LibraryView USING GIN(""SearchVector"");");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX idx_libraryview_name_trgm ON LibraryView USING GIN(""Name"" gin_trgm_ops);");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE INDEX idx_libraryview_desc_trgm ON LibraryView USING GIN(""Description"" gin_trgm_ops);");

            // Step 4: Create refresh function
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE OR REPLACE FUNCTION refresh_library_view()
                RETURNS TRIGGER AS $$
                BEGIN
                    REFRESH MATERIALIZED VIEW CONCURRENTLY LibraryView;
                    RETURN NULL;
                END;
                $$ LANGUAGE plpgsql;");

            // Step 5: Create triggers
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TRIGGER refresh_library_on_resource_change
                    AFTER INSERT OR UPDATE OR DELETE ON ""resources""
                    FOR EACH STATEMENT
                    EXECUTE FUNCTION refresh_library_view();");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TRIGGER refresh_library_on_entity_change
                    AFTER INSERT OR UPDATE OR DELETE ON ""entities""
                    FOR EACH STATEMENT
                    EXECUTE FUNCTION refresh_library_view();");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TRIGGER refresh_library_on_person_change
                    AFTER INSERT OR UPDATE OR DELETE ON ""persons""
                    FOR EACH STATEMENT
                    EXECUTE FUNCTION refresh_library_view();");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TRIGGER refresh_library_on_organisation_change
                    AFTER INSERT OR UPDATE OR DELETE ON ""organisations""
                    FOR EACH STATEMENT
                    EXECUTE FUNCTION refresh_library_view();");

            await transaction.CommitAsync();
            Serilog.Log.Information("Materialized view LibraryView with triggers created successfully");
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            Serilog.Log.Error(e, "Failed to create library view");
        }
    }
    
    public static async Task CreateTrashViewAsync(this DatabaseContext context)
    {
        using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // Step 1: Drop existing objects
            // (old resource-trash-named function/view are dropped too, to clean up databases from before the TrashView rename)
            await context.Database.ExecuteSqlRawAsync(@"
                DROP TRIGGER IF EXISTS refresh_trash_on_resource_change ON ""resources"";");

            await context.Database.ExecuteSqlRawAsync(@"
                DROP TRIGGER IF EXISTS refresh_trash_on_entity_change ON ""entities"";");

            await context.Database.ExecuteSqlRawAsync(@"
                DROP TRIGGER IF EXISTS refresh_trash_on_person_change ON ""persons"";");

            await context.Database.ExecuteSqlRawAsync(@"
                DROP TRIGGER IF EXISTS refresh_trash_on_organisation_change ON ""organisations"";");

            await context.Database.ExecuteSqlRawAsync(@"
                DROP FUNCTION IF EXISTS refresh_resource_trash_view();
                DROP FUNCTION IF EXISTS refresh_trash_view();");

            await context.Database.ExecuteSqlRawAsync(@"
                DROP MATERIALIZED VIEW IF EXISTS ResourceTrashView;
                DROP MATERIALIZED VIEW IF EXISTS TrashView;");

            // Step 2: Create materialized view
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE MATERIALIZED VIEW TrashView AS
                SELECT
                    ""id"" as ""Id"",
                    ""title"" as ""Name"",
                    ""publication-date"" as ""PublicationDate"",
                    ""trash-date"" as ""TrashDate"",
                    'resource' as ""Type"",
                    CASE
                        WHEN ""filetype"" = 'website' THEN 'website'
                        WHEN ""file-ext"" = NULL THEN 'document'
                        ELSE ""file-ext""
                    END as ""FileType""
                FROM ""resources""
                WHERE ""trashed"" = true

                UNION ALL

                SELECT
                    p.""id"" as ""Id"",
                    e.""name"" as ""Name"",
                    NULL as ""PublicationDate"",
                    e.""trash-date"" as ""TrashDate"",
                    'person' as ""Type"",
                    'person' as ""FileType""
                FROM ""persons"" p
                INNER JOIN ""entities"" e ON p.""id"" = e.""id""
                WHERE e.""trashed"" = true

                UNION ALL

                SELECT
                    o.""id"" as ""Id"",
                    e.""name"" as ""Name"",
                    NULL as ""PublicationDate"",
                    e.""trash-date"" as ""TrashDate"",
                    'organisation' as ""Type"",
                    'organisation' as ""FileType""
                FROM ""organisations"" o
                INNER JOIN ""entities"" e ON o.""id"" = e.""id""
                WHERE e.""trashed"" = true
            ;");

            // Step 3: Create indexes
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE UNIQUE INDEX idx_trashview_id ON TrashView(""Id"");");

            // Step 4: Create refresh function
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE OR REPLACE FUNCTION refresh_trash_view()
                RETURNS TRIGGER AS $$
                BEGIN
                    REFRESH MATERIALIZED VIEW CONCURRENTLY TrashView;
                    RETURN NULL;
                END;
                $$ LANGUAGE plpgsql;");

            // Step 5: Create triggers
            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TRIGGER refresh_trash_on_resource_change
                    AFTER INSERT OR UPDATE OR DELETE ON ""resources""
                    FOR EACH STATEMENT
                    EXECUTE FUNCTION refresh_trash_view();");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TRIGGER refresh_trash_on_entity_change
                    AFTER INSERT OR UPDATE OR DELETE ON ""entities""
                    FOR EACH STATEMENT
                    EXECUTE FUNCTION refresh_trash_view();");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TRIGGER refresh_trash_on_person_change
                    AFTER INSERT OR UPDATE OR DELETE ON ""persons""
                    FOR EACH STATEMENT
                    EXECUTE FUNCTION refresh_trash_view();");

            await context.Database.ExecuteSqlRawAsync(@"
                CREATE TRIGGER refresh_trash_on_organisation_change
                    AFTER INSERT OR UPDATE OR DELETE ON ""organisations""
                    FOR EACH STATEMENT
                    EXECUTE FUNCTION refresh_trash_view();");

            await transaction.CommitAsync();
            Serilog.Log.Information("Materialized view TrashView with triggers created successfully");
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            Serilog.Log.Error(e, "Failed to create trash view");
        }
    }
}
