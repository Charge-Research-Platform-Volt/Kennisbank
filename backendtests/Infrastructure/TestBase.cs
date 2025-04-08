using Npgsql;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Data;

namespace backend.Tests.Infrastructure;


/// <summary>
/// Base class for tests that need a PostgreSQL database.
/// 
/// This class provides:
/// 1. Direct connection to a PostgreSQL database
/// 2. A fresh test database for each test method, cloned from a template database
/// 3. Automatic cleanup of test databases after each test
/// 4. Extension points for test-specific setup and teardown
/// 
/// To start writing tests, simply inherit from this class, create a
/// [SetUp] method to setup the controller. This class provides many virtual
/// methods that can be overriden to setup your test class.
/// </summary>
public abstract class TestBase 
{
    /// <summary>Database context connected to the test database</summary>
    protected DatabaseContext Context;
    
    /// <summary>Name of the current test database</summary>
    protected string TestDatabaseName;
    
    /// <summary>Reference to the shared PostgreSQL container</summary>
    protected string MasterConnectionString;

    /// <summary>Name of template database</summary>
    private const string TemplateDbName = "template_test_db";

    /// <summary>
    /// One-time setup for the test class.
    /// Sets up the master connection string and creates the template database if needed.
    /// </summary>
    [OneTimeSetUp]
    public async Task GlobalSetUp()
    {
        // Determine the host based on runtime environment
        string dbHost = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" 
            ? "database"   // To run test in CI/CD
            : "localhost"; // To run test locally
        
        MasterConnectionString = $"Host={dbHost};Database=postgres;Username=postgres;Password=postgres";
        
        // Create template database if it doesn't exist
        await EnsureTemplateDatabase();
        
        // Call the virtual method for test class-specific one-time setup
        await OnGlobalSetUp();
    }

    private async Task EnsureTemplateDatabase()
    {
        await using NpgsqlConnection connection = new NpgsqlConnection(MasterConnectionString);
        await connection.OpenAsync();

        // Check if template database exists
        bool templateExists;
        using (NpgsqlCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{TemplateDbName}'";
            object? result = await cmd.ExecuteScalarAsync();
            templateExists = result != null;
        }

        if (templateExists)
        {
            // Terminate all existing connections to the template database to avoid conflicts
            using (NpgsqlCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = $@"
                    SELECT pg_terminate_backend(pg_stat_activity.pid)
                    FROM pg_stat_activity
                    WHERE pg_stat_activity.datname = '{TemplateDbName}'
                    AND pid <> pg_backend_pid();";
                await cmd.ExecuteNonQueryAsync();
            }
            
            // Drop existing template database
            using (NpgsqlCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"DROP DATABASE IF EXISTS {TemplateDbName};";
                await cmd.ExecuteNonQueryAsync();
            }
        }
        
        // Create template database
        using (NpgsqlCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = $"CREATE DATABASE {TemplateDbName};";
            await cmd.ExecuteNonQueryAsync();
        }
        
        // Set up the template database schema
        await SetupTemplateDatabase(TemplateDbName);
    }

    /// <summary>
    /// Sets up the schema and initial data in the template database.
    /// </summary>
    /// <param name="templateDbName">Name of the template database</param>
    private async Task SetupTemplateDatabase(string templateDbName)
    {
        string templateConnectionString = $"{MasterConnectionString};Database={templateDbName}";
        
        DbContextOptions<DatabaseContext> options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseNpgsql(templateConnectionString)
            .Options;

        await using DatabaseContext templateContext = new DatabaseContext(options);
        
        // Apply migrations or create schema
        await templateContext.Database.EnsureCreatedAsync();
        
        // Seed the template database with common test data
        await SeedTemplateDatabase(templateContext);
        
        await templateContext.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds the template database with common data that all tests will need.
    /// Override this method in derived classes to add common seed data.
    /// </summary>
    /// <param name="context">Database context connected to the template database</param>
    protected virtual Task SeedTemplateDatabase(DatabaseContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Extension point for test class-specific one-time setup.
    /// Override this method in derived classes to add custom one-time setup logic.
    /// </summary>
    protected virtual Task OnGlobalSetUp()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// One-time teardown for the test class.
    /// </summary>
    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        // Call the virtual method for test class-specific one-time teardown
        await OnGlobalTearDown();
    }

    /// <summary>
    /// Extension point for test class-specific one-time teardown.
    /// Override this method in derived classes to add custom one-time teardown logic.
    /// </summary>
    protected virtual Task OnGlobalTearDown()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Setup for each individual test method.
    /// Creates a fresh test database by cloning the template database.
    /// </summary>
    [SetUp]
    public async Task SetUpTestDb()
    {
        // Generate a unique name for this test database
        TestDatabaseName = $"testdb_{Guid.NewGuid():N}";

        // Connect to the main postgres database
        await using NpgsqlConnection connection = new NpgsqlConnection(MasterConnectionString);
        await connection.OpenAsync();
        
        // Make sure no connections to the template database exist
        using (NpgsqlCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = $@"
                SELECT pg_terminate_backend(pg_stat_activity.pid)
                FROM pg_stat_activity
                WHERE pg_stat_activity.datname = '{TemplateDbName}'
                AND pid <> pg_backend_pid();";
            await cmd.ExecuteNonQueryAsync();
        }

        // Create a new test database by cloning the template database
        using (NpgsqlCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = $"CREATE DATABASE {TestDatabaseName} TEMPLATE {TemplateDbName};";
            await cmd.ExecuteNonQueryAsync();
        }

        // Create a database context connected to the test database
        DbContextOptions<DatabaseContext> options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseNpgsql($"{MasterConnectionString};Database={TestDatabaseName}")
            .Options;

        Context = new DatabaseContext(options);
        
        // Seed the test database with test-specific data
        await SeedTestDatabase(Context);
        
        // Call the virtual method for test-specific setup
        await OnTestSetUp();
    }

    /// <summary>
    /// Seeds the current test database with data specific to this test class.
    /// Override this method in derived classes to add test-specific seed data.
    /// 
    /// NOTE: This seeds the per-test database instance, NOT the template database.
    /// Each test gets its own copy of the database, so changes made here won't affect other tests.
    /// </summary>
    /// <param name="context">Database context connected to the test database</param>
    protected virtual Task SeedTestDatabase(DatabaseContext context)
    {
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Extension point for test-specific setup.
    /// Override this method in derived classes to add custom setup logic.
    /// </summary>
    protected virtual Task OnTestSetUp()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Teardown for each individual test method.
    /// Disposes the database context and drops the test database.
    /// </summary>
    [TearDown]
    public async Task TearDownTestDb()
    {
        // Call the virtual method for test-specific teardown
        await OnTestTearDown();
        
        // Dispose the database context
        await Context.DisposeAsync();

        // Clear all connection pools to ensure resources are released
        NpgsqlConnection.ClearAllPools();
        
        // Drop the test database to clean up
        await using NpgsqlConnection connection = new NpgsqlConnection(MasterConnectionString);
        await connection.OpenAsync();

        // First, terminate all connections to the test database
        using (NpgsqlCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = $@"
                SELECT pg_terminate_backend(pg_stat_activity.pid)
                FROM pg_stat_activity
                WHERE pg_stat_activity.datname = '{TestDatabaseName}'
                AND pid <> pg_backend_pid();";
            await cmd.ExecuteNonQueryAsync();
        }

        // Then drop the test database
        using (NpgsqlCommand cmd = connection.CreateCommand())
        {
            cmd.CommandText = $"DROP DATABASE IF EXISTS {TestDatabaseName};";
            await cmd.ExecuteNonQueryAsync();
        }
    }
    
    /// <summary>
    /// Extension point for test-case-specific teardown.
    /// Override this method in derived classes to add custom teardown logic.
    /// </summary>
    [TearDown]
    protected virtual Task OnTestTearDown()
    {
        return Task.CompletedTask;
    }
}
