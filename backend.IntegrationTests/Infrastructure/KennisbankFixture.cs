using System.Net.Http.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using KnowledgeBank.Models;
using KnowledgeBank.Tests.Helpers;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace KnowledgeBank.IntegrationTests.Infrastructure;

/// <summary>
/// Starts Postgres (pgvector) and Meilisearch in throwaway containers, a local fake for the AI APIs,
/// and the real app on top of them — once per test run, shared by every test in the collection.
///
/// Docker cleanup: both databases write to tmpfs (memory) instead of their declared volumes, so no
/// Docker volumes are created at all. The containers are removed on dispose, and Testcontainers' Ryuk
/// reaper removes anything left behind if the run is killed. Pulled images stay cached for the next run.
/// </summary>
public sealed class KennisbankFixture : IAsyncLifetime
{
    public const string OwnerEmail = "owner@kennisbank.test";
    public const string OwnerPassword = "Owner-Passw0rd!";
    public const string UserPassword = "User-Passw0rd!";
    private const string MeiliKey = "test-meili-master-key";

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("pgvector/pgvector:pg16")
        .WithTmpfsMount("/var/lib/postgresql/data")
        .Build();

    private readonly IContainer meilisearch = new ContainerBuilder("getmeili/meilisearch:v1.50.0")
        .WithPortBinding(7700, assignRandomHostPort: true)
        .WithEnvironment("MEILI_MASTER_KEY", MeiliKey)
        .WithEnvironment("MEILI_NO_ANALYTICS", "true")
        .WithTmpfsMount("/meili_data")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(7700).ForPath("/health")))
        .Build();

    public FakeAiServer Ai { get; } = new();
    public InMemoryStorageService Storage { get; } = new();
    public KennisbankFactory Factory { get; private set; } = null!;
    public IServiceProvider Services => Factory.Services;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(postgres.StartAsync(), meilisearch.StartAsync());

        // Program.Main reads its configuration before builder.Build(), where factory-level overrides don't
        // reach yet, so everything is provided as process environment variables instead
        Dictionary<string, string?> settings = TestEnvironment.DefaultValues();
        settings[nameof(EnvironmentVariable.ASPNETCORE_ENVIRONMENT)] = "Testing";
        settings[nameof(EnvironmentVariable.DATABASE_CONNECTION_STRING)] = postgres.GetConnectionString();
        settings[nameof(EnvironmentVariable.MEILISEARCH_URL)] = $"http://{meilisearch.Hostname}:{meilisearch.GetMappedPublicPort(7700)}";
        settings[nameof(EnvironmentVariable.MEILISEARCH_API_KEY)] = MeiliKey;
        settings[nameof(EnvironmentVariable.EMBEDDINGS_ENDPOINT)] = Ai.EmbeddingsEndpoint;
        settings[nameof(EnvironmentVariable.MISTRAL_ENDPOINT)] = Ai.MistralEndpoint;
        settings[nameof(EnvironmentVariable.WEBSCRAPE_SERVICE_URL)] = Ai.Url + "/webscrape";
        settings[nameof(EnvironmentVariable.HEADSCALE_URL)] = Ai.Url + "/headscale";
        settings["OwnerUser__Email"] = OwnerEmail;
        settings["OwnerUser__Password"] = OwnerPassword;
        settings["EMAIL_SMTP_HOST"] = "smtp.invalid";
        settings["EMAIL_TLS_PORT"] = "587";
        settings["EMAIL_ADDRESS"] = "noreply@kennisbank.test";
        settings["EMAIL_PASSWORD"] = "test-email-password";
        settings["EMAIL_FROM_NAME"] = "Kennisbank tests";

        foreach (var (key, value) in settings)
            Environment.SetEnvironmentVariable(key, value);

        Factory = new KennisbankFactory(Storage);
        _ = Factory.Server; // starts the app: migrations, seeding, views, search indexes

        AssertNoRealServicesConfigured();
    }

    /// <summary>
    /// Safety net: a local .env.local with real keys must never be picked up by the tests,
    /// so stop before any test runs if the app isn't pointed at the local fakes.
    /// </summary>
    private void AssertNoRealServicesConfigured()
    {
        EnvironmentConfig config = Services.GetRequiredService<EnvironmentConfig>();

        void Expect(EnvironmentVariable variable, string expected)
        {
            string actual = config.GetVariableValue(variable);
            if (actual != expected)
                throw new InvalidOperationException($"Refusing to run integration tests: {variable} is not the test value. Is a real .env being loaded?");
        }

        Expect(EnvironmentVariable.EMBEDDINGS_ENDPOINT, Ai.EmbeddingsEndpoint);
        Expect(EnvironmentVariable.MISTRAL_ENDPOINT, Ai.MistralEndpoint);
        Expect(EnvironmentVariable.EMBEDDINGS_API_KEY, "test-embeddings_api_key");
        Expect(EnvironmentVariable.MISTRAL_API_KEY, "test-mistral_api_key");
    }

    // Clients and users

    public HttpClient CreateAnonymousClient() => Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    public Task<HttpClient> LoginAsAdminAsync() => LoginAsync(OwnerEmail, OwnerPassword);

    /// <summary>Creates a fresh regular user and returns a logged-in client plus the user's id.</summary>
    public async Task<(HttpClient Client, Guid UserId)> LoginAsNewUserAsync(string role = "user")
    {
        string email = $"user-{Guid.NewGuid():N}@kennisbank.test";

        using IServiceScope scope = Services.CreateScope();
        UserManager<User> users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        User user = new("Test", "User", email);

        IdentityResult created = await users.CreateAsync(user, UserPassword);
        if (!created.Succeeded) throw new InvalidOperationException(string.Join(", ", created.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(user, role);

        return (await LoginAsync(email, UserPassword), Guid.Parse(user.Id));
    }

    public async Task<Guid> GetOwnerIdAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        User owner = (await scope.ServiceProvider.GetRequiredService<UserManager<User>>().FindByEmailAsync(OwnerEmail))!;
        return Guid.Parse(owner.Id);
    }

    private async Task<HttpClient> LoginAsync(string email, string password)
    {
        HttpClient client = CreateAnonymousClient();
        HttpResponseMessage response = await client.PostAsJsonAsync("/Auth/login?useCookies=true", new { email, password });
        response.EnsureSuccessStatusCode();
        return client;
    }

    public async Task DisposeAsync()
    {
        if (Factory != null) await Factory.DisposeAsync();
        Ai.Dispose();
        await Task.WhenAll(postgres.DisposeAsync().AsTask(), meilisearch.DisposeAsync().AsTask());
    }
}

[CollectionDefinition(Name)]
public class KennisbankCollection : ICollectionFixture<KennisbankFixture>
{
    public const string Name = "Kennisbank";
}
