using KnowledgeBank.IntegrationTests.Infrastructure;
using KnowledgeBank.Utils;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KnowledgeBank.IntegrationTests;

/// <summary>A document without a file extension shows up as "document" in the library and the trash.</summary>
[Collection(KennisbankCollection.Name)]
public class LibraryViewFileTypeTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task DocumentWithoutExtension_IsShownAsDocument()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid resourceId = await admin.CreateResourceAsync(await admin.CreateResourceTypeAsync());
        await fixture.WaitForEmbeddingAsync(resourceId);

        await using NpgsqlConnection db = new(fixture.Services.GetRequiredService<EnvironmentConfig>().GetVariableValue(EnvironmentVariable.DATABASE_CONNECTION_STRING));
        await db.OpenAsync();

        // The API always stores an extension for documents, so set up this edge case directly
        await Execute(db, $"UPDATE resources SET filetype = 'document', \"file-ext\" = NULL WHERE id = '{resourceId}'");
        Assert.Equal("document", await Scalar(db, $"SELECT \"FileType\" FROM libraryview WHERE \"Id\" = '{resourceId}'"));

        await Execute(db, $"UPDATE resources SET trashed = true, \"trash-date\" = now() WHERE id = '{resourceId}'");
        Assert.Equal("document", await Scalar(db, $"SELECT \"FileType\" FROM trashview WHERE \"Id\" = '{resourceId}'"));
    }

    private static async Task Execute(NpgsqlConnection db, string sql)
    {
        await using NpgsqlCommand command = new(sql, db);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string?> Scalar(NpgsqlConnection db, string sql)
    {
        await using NpgsqlCommand command = new(sql, db);
        return await command.ExecuteScalarAsync() as string;
    }
}
