using KnowledgeBank.Data;
using KnowledgeBank.Security;
using KnowledgeBank.Extensions;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using KnowledgeBank.Services.Background;
using KnowledgeBank.Services;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Vector;
using KnowledgeBank.Hubs;

using Microsoft.AspNetCore.Http.Features;
using KnowledgeBank.Utils;
using DotNetEnv;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Services.Search;
using Npgsql;
using KnowledgeBank.Services.Domain;
using Meilisearch;

namespace KnowledgeBank
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Load .env file for local development (only when not in container)
            string envFile = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env.local");
            if (File.Exists(envFile))
            {
                Env.Load(envFile);
            }

            // Ensure the current directory is set to the directory of the executable
            // # Builder
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            ConfigureLogging();

            // The environment variables are loaded from the .env file or environment
            EnvironmentConfig environmentConfig = new EnvironmentConfig(builder.Configuration);
            environmentConfig.CheckEnvironmentVariables();
            builder.Services.AddSingleton(environmentConfig);

            // # Configuration
            builder.Services.Configure<OwnerUserConfig>(builder.Configuration.GetSection(OwnerUserConfig.SectionName));
            
            // Validate owner config
            OwnerUserConfig? ownerConfig = builder.Configuration.GetSection(OwnerUserConfig.SectionName).Get<OwnerUserConfig>();
            if (ownerConfig == null)
            {
                Log.Fatal("OwnerUser configuration section is missing from configuration.");
                throw new InvalidOperationException("OwnerUser configuration section is required.");
            }

            if (string.IsNullOrWhiteSpace(ownerConfig.Email))
            {
                Log.Fatal("OwnerUser: Email is required but not configured.");
                throw new InvalidOperationException("OwnerUser: Email configuration is required.");
            }

            if (string.IsNullOrWhiteSpace(ownerConfig.Password))
            {
                Log.Fatal("OwnerUser: Password is required but not configured.");
                throw new InvalidOperationException("OwnerUser: Password configuration is required.");
            }

            Log.Information("OwnerUser configuration validated successfully for: {Email}", ownerConfig.Email);

            // # Services
            builder.Services.AddControllers();
            builder.Services.AddProblemDetails();
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<IStorageService, S3StorageService>();
            builder.Services.AddSingleton<ExtractionJobService>();

            builder.Services.AddSingleton<TextExtractionService>();
            builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, CustomAuthorizationMiddlewareResultHandler>();
            builder.Services.AddAuthorization(options =>
            {
                foreach (string roleName in RoleInitializer.roleNames)
                {
                    options.AddPolicy($"Require{char.ToUpper(roleName[0]) + roleName.Substring(1)}Role", policy => policy.RequireRole(roleName));
                }

                // This line terminates the handler on first failure, when more information is required, set this to true.
                // This gives an increase in performance, but omits some information which might be required for complex
                // authorization scenarios. This setting only affects the authorization middleware, not the controllers.
                options.InvokeHandlersAfterFailure = false;
            });

            // Add this line after the code below to enable authentication with JWT tokens: .AddBearerToken(IdentityConstants.BearerScheme);
            builder.Services.AddAuthentication().AddCookie(IdentityConstants.ApplicationScheme);

            builder.Services.AddIdentityCore<User>()
                            .AddRoles<IdentityRole>()
                            .AddEntityFrameworkStores<DatabaseContext>()
                            .AddApiEndpoints();

            builder.Services.Configure<IdentityOptions>(options =>
            {
                // Currently the only addition is +, we could use this string to add even more email compatibility:
                // "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+!#$%'&*=/^`{|}~"
                // Another option: use guid as user name in asp net databse
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

                options.User.RequireUniqueEmail = true;
            });

            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen(ConfigureSwagger);
            builder.Services.AddScoped<RoleInitializer>();

            // # Database context - use NpgsqlDataSourceBuilder to register pgvector types
            // This is required for Npgsql 7.0+ to properly handle vector types

            // IMPORTANT: Create the vector extension BEFORE building the NpgsqlDataSource
            // The datasource caches type info on first connection, so the extension must exist first
            string connectionString = environmentConfig.GetVariableValue(EnvironmentVariable.DATABASE_CONNECTION_STRING);
            using (var conn = new NpgsqlConnection(connectionString))
            {
                conn.Open();
                using var cmd = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector", conn);
                cmd.ExecuteNonQuery();
                Log.Information("Vector extension created/verified before datasource initialization");
            }

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UseVector();
            var dataSource = dataSourceBuilder.Build();

            builder.Services.AddDbContextFactory<DatabaseContext>(options =>
                options.UseNpgsql(dataSource, o => o.UseVector())
                    // Person/Organisation self-referencing relationships (SourcePerson/TargetPerson,
                    // SourceOrganisation/TargetOrganisation) are always populated via EF's own
                    // change-tracker fix-up, not the explicit .Include(...).ThenInclude(...) chains
                    // used for them across PersonService/OrganisationService — EF warns about this
                    // by design, but throws by default; downgrade it back to a no-op warning.
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.NavigationBaseIncludeIgnored))
                    // TEMPORARY: diagnosing the production login issue - shows real parameter
                    // values in SQL logs instead of redacted '?'. Remove once resolved.
                    .EnableSensitiveDataLogging()
            );

            // Resource management
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<TagService>();
            builder.Services.AddScoped<RegionService>();
            builder.Services.AddScoped<JournalService>();
            builder.Services.AddScoped<PersonService>();
            builder.Services.AddScoped<OrganisationService>();
            builder.Services.AddScoped<ResourceTypeService>();
            builder.Services.AddScoped<ResourceService>();
            builder.Services.AddScoped<LibraryService>();
            builder.Services.AddScoped<ProjectService>();
            builder.Services.AddScoped<ChatService>();
            builder.Services.AddScoped<MessageAttachmentService>();
            builder.Services.AddScoped<ChatOrchestrationService>();
            builder.Services.AddScoped<ChatToolExecutor>();
            builder.Services.AddScoped<MergeSuggestionService>();

            // Retrieval Augmented Generation system
            builder.Services.AddSingleton<MistralHttpClient>();
            builder.Services.AddSingleton<MistralStatusService>();
            builder.Services.AddSingleton<AiService>();
            builder.Services.AddSingleton<EmbeddingService>();
            builder.Services.AddScoped<IngestionService>();
            builder.Services.AddScoped<MetadataExtractionService>();
            builder.Services.AddScoped<IVectorStore, PostgresVectorStore>();

            // Meilisearch
            builder.Services.AddSingleton(sp =>
            {
                var config = sp.GetRequiredService<EnvironmentConfig>();
                return new MeilisearchClient(config.GetVariableValue(EnvironmentVariable.MEILISEARCH_URL), config.GetVariableValue(EnvironmentVariable.MEILISEARCH_API_KEY));
            });
            builder.Services.AddSingleton<LibrarySearchIndexService>();
            builder.Services.AddScoped<ChunkSearchIndexService>();
            builder.Services.AddScoped<TaxonomySearchIndexService>();
            builder.Services.AddScoped<AttachmentChunkSearchIndexService>();

            // Background services
            builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            builder.Services.AddSingleton<ReembedRunState>();
            builder.Services.AddHostedService<QueuedHostedService>();

            // # Mailer;
            builder.Services.AddSingleton(new MailUtils(
                builder.Configuration.GetValue<string>("EMAIL_SMTP_HOST") ?? throw new ArgumentNullException("EMAIL_SMTP_HOST needs to be set"),
                builder.Configuration.GetValue<int?>("EMAIL_TLS_PORT") ?? throw new ArgumentNullException("EMAIL_TLS_PORT needs to be set"),
                builder.Configuration.GetValue<string>("EMAIL_ADDRESS") ?? throw new ArgumentNullException("EMAIL_ADDRESS needs to be set"),
                builder.Configuration.GetValue<string>("EMAIL_PASSWORD") ?? throw new ArgumentNullException("EMAIL_PASSWORD needs to be set"),
                builder.Configuration.GetValue<string>("EMAIL_FROM_NAME") ?? throw new ArgumentNullException("EMAIL_FROM_NAME needs to be set")
            ));

            // CORS to allow Cross Origin Resource Sharing
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins(builder.Configuration.GetValue<string>("HOST_URL") ?? throw new ArgumentNullException("HOST_URL needs to be set"))
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            builder.Services.Configure<FormOptions>(options =>
            {
                // Set the limit to 100 MB
                options.MultipartBodyLengthLimit = 110100480; // 105 * 1024c * 1024
                options.ValueLengthLimit = int.MaxValue;
                options.MultipartHeadersLengthLimit = int.MaxValue;
            });

            builder.Services.AddHostedService<TrashbinCleanupService>(); // Add the background service for cleaning up the trashbin
            builder.Services.AddHostedService<InvitationsCleanupService>(); // Add the background service for cleaning up invitations
            builder.Services.AddHostedService<VPNCleanupService>(); // Add the background service for cleaning up orphaned VPN users
            builder.Services.AddHostedService<StorageCleanupService>();

            // Webscrape service client
            builder.Services.AddSingleton<WebscrapeClient>();

            // VPN Service
            builder.Services.AddSingleton<VPNService>();

            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.Limits.MaxRequestBodySize = 10485760; // 10 MB in bytes
            });

            // # Application
            WebApplication app = builder.Build();

            // # Apply pending migrations automatically
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                try
                {
                    // Note: Vector extension was already created before datasource initialization
                    await db.Database.MigrateAsync();
                    Log.Information("Database migrations applied successfully");
                }
                catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P07")
                {
                    // Table already exists - this is okay, migrations were partially applied
                    Log.Warning("Database tables already exist, skipping migration: {Message}", ex.Message);
                    Log.Information("Database schema appears to be already initialized");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to apply database migrations");
                    throw;
                }
            }
            
            // Set up bucket (only in development - production buckets must be pre-created)
            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                IStorageService storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
                string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

                Log.Information("Creating bucket '{BucketName}' in object storage...", bucketName);
                await storageService.CreateBucketAsync(bucketName);
            }

            // # Middleware
            if (app.Environment.IsDevelopment())
            {
                // Run only in development environment:
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI(ConfigureSwaggerUI);
            }
            app.UseExceptionHandler();
            app.UseStatusCodePages();

            // Seeding the database with initial data
            using (IServiceScope scope = app.Services.CreateScope())
            {
                RoleInitializer roleInitializer = scope.ServiceProvider.GetRequiredService<RoleInitializer>();
                await roleInitializer.InitializeAsync();
                
                var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
                await db.SaveChangesAsync();

                await db.EnsureDatabaseSetupAsync();

                var librarySearchIndexService = scope.ServiceProvider.GetRequiredService<LibrarySearchIndexService>();
                var chunkSearchIndexService = scope.ServiceProvider.GetRequiredService<ChunkSearchIndexService>();
                var taxonomySearchIndexService = scope.ServiceProvider.GetRequiredService<TaxonomySearchIndexService>();
                var attachmentChunkSearchIndexService = scope.ServiceProvider.GetRequiredService<AttachmentChunkSearchIndexService>();

                await Task.WhenAll(
                    librarySearchIndexService.EnsureIndexConfiguredAsync(),
                    chunkSearchIndexService.EnsureIndexConfiguredAsync(),
                    taxonomySearchIndexService.EnsureIndexConfiguredAsync(),
                    attachmentChunkSearchIndexService.EnsureIndexConfiguredAsync()
                );
            }

            app.UseRouting();
            app.UseCors("AllowFrontend");
            app.UseAuthentication();
            app.UseAuthorization();

            // Hub and Controllers must be added after Authentication and Authorization
            app.MapHub<AppHub>("/hub"); // SignalR hub for chat functionality
            app.MapControllers();

            app.MapGroup("Auth").MapIdentityApi<User>().WithTags("Auth").AddEndpointFilter(async (efiContext, next) =>
            {
                if (HideEndpointFilter.PathsToHide.Any(p => p == efiContext.HttpContext.Request.Path))
                    return Results.Forbid();
                return await next(efiContext);
            });

            app.Lifetime.ApplicationStarted.Register(() =>
            {
                Log.Information("Backend started successfully on {Urls}", string.Join(", ", app.Urls));
            });
            app.Run();
        }

        /// <summary>
        /// Configures Swagger documentation settings.
        /// </summary>
        /// <param name="c">The <see cref="SwaggerGenOptions"/> instance to configure.</param>
        /// <remarks>
        /// This method sets up Swagger with API information and enables annotations.
        /// </remarks>
        private static void ConfigureSwagger(SwaggerGenOptions c)
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "KnowledgeBank", Version = "v1" });
            c.EnableAnnotations();
            c.DocumentFilter<HideEndpointFilter>();
            c.AddSignalRSwaggerGen(); // Add SignalR support for Swagger
        }

        /// <summary>
        /// Configures the Swagger UI settings.
        /// </summary>
        /// <param name="c">The Swagger UI options to configure.</param>
        /// <remarks>
        /// This method sets up the Swagger endpoint, route prefix, and document title for the API documentation.
        /// The documentation will be available at the "/docs" route.
        /// </remarks>
        private static void ConfigureSwaggerUI(SwaggerUIOptions c)
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Version-1");
            c.RoutePrefix = "docs";
            c.DocumentTitle = "KnowledgeBank API";
            c.DocExpansion(DocExpansion.None);
        }

        /// <summary>
        /// Configures the application logging system using Serilog.
        /// </summary>
        /// <remarks>
        /// This method sets up Serilog with configuration from "serilogsettings.json" file
        /// and establishes two logging sinks:
        /// 1. Console output for immediate visibility
        /// 2. Daily rolling text files stored in the "logs" directory with the naming pattern "log-YYYYMMDD.txt"
        /// </remarks>
        private static void ConfigureLogging()
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                                        .SetBasePath(Directory.GetCurrentDirectory())
                                        .AddJsonFile("serilogsettings.json")
                                        .Build();

            Log.Logger = new LoggerConfiguration()
                                .ReadFrom.Configuration(configuration)
                                .CreateLogger();
        }
    }
}
