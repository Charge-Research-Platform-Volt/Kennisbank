using KnowledgeBank.Data;
using KnowledgeBank.IntegrationTests.Infrastructure;
using KnowledgeBank.Models;
using Meilisearch;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBank.IntegrationTests;

[Collection(KennisbankCollection.Name)]
public class StartupTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task EmptyDatabase_IsFullyMigrated()
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task OwnerIsSeededAsAdmin()
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        UserManager<User> users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        User? owner = await users.FindByEmailAsync(KennisbankFixture.OwnerEmail);

        Assert.NotNull(owner);
        Assert.True(await users.IsInRoleAsync(owner, "admin"));
    }

    [Fact]
    public async Task LibraryAndTrashViews_ExistWithRefreshTriggers()
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        List<string> views = await db.Database
            .SqlQueryRaw<string>("SELECT matviewname AS \"Value\" FROM pg_matviews")
            .ToListAsync();
        List<string> triggers = await db.Database
            .SqlQueryRaw<string>("SELECT DISTINCT tgname AS \"Value\" FROM pg_trigger WHERE NOT tgisinternal")
            .ToListAsync();

        Assert.Contains("libraryview", views);
        Assert.Contains("trashview", views);
        Assert.Contains("refresh_library_on_resource_change", triggers);
    }

    [Fact]
    public async Task SearchIndexes_AreCreated()
    {
        MeilisearchClient meili = fixture.Services.GetRequiredService<MeilisearchClient>();

        var indexes = await meili.GetAllIndexesAsync();

        Assert.Superset(
            new HashSet<string> { "library", "chunks", "taxonomy", "attachment-chunks" },
            indexes.Results.Select(i => i.Uid).ToHashSet());
    }
}
