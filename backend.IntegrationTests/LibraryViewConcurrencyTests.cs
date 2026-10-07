using KnowledgeBank.IntegrationTests.Infrastructure;
using KnowledgeBank.Utils;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KnowledgeBank.IntegrationTests;

/// <summary>
/// Two transactions changing the library at the same time must both end up in LibraryView.
/// The view is refreshed by a trigger inside each transaction; a refresh that had to wait for the
/// other one must not overwrite the view with data from before that other transaction committed.
/// </summary>
[Collection(KennisbankCollection.Name)]
public class LibraryViewConcurrencyTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task ConcurrentChanges_AreBothVisibleInTheLibraryView()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid typeId = await admin.CreateResourceTypeAsync();
        Guid first = await admin.CreateResourceAsync(typeId);
        Guid second = await admin.CreateResourceAsync(typeId);
        await fixture.WaitForEmbeddingAsync(first);
        await fixture.WaitForEmbeddingAsync(second);
        string newTitle = Api.Unique("Renamed concurrently");

        string connectionString = fixture.Services.GetRequiredService<EnvironmentConfig>().GetVariableValue(EnvironmentVariable.DATABASE_CONNECTION_STRING);
        await using NpgsqlConnection a = new(connectionString);
        await using NpgsqlConnection b = new(connectionString);
        await a.OpenAsync();
        await b.OpenAsync();

        // A renames the first resource; its trigger refreshes the view, which then stays locked until A commits
        await using NpgsqlTransaction txA = await a.BeginTransactionAsync();
        await Execute(a, txA, $"UPDATE resources SET title = '{newTitle}' WHERE id = '{first}'");

        // B changes the second resource meanwhile; its refresh has to wait for A
        await using NpgsqlTransaction txB = await b.BeginTransactionAsync();
        Task bUpdate = Execute(b, txB, $"UPDATE resources SET note = 'touched' WHERE id = '{second}'");
        await Task.Delay(500);
        Assert.False(bUpdate.IsCompleted, "B's refresh should be waiting for A's");

        await txA.CommitAsync();
        await bUpdate;
        await txB.CommitAsync();

        await using NpgsqlCommand read = new($"SELECT \"Name\" FROM libraryview WHERE \"Id\" = '{first}'", a);
        Assert.Equal(newTitle, (string?)await read.ExecuteScalarAsync());
    }

    private static async Task Execute(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql)
    {
        await using NpgsqlCommand command = new(sql, connection, transaction);
        await command.ExecuteNonQueryAsync();
    }
}
