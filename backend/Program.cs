using KnowledgeBank.Data;
using KnowledgeBank.Security;
using KnowledgeBank.Extensions;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using KnowledgeBank.BackgroundServices;
using KnowledgeBank.Services;
using Hubs;

using Microsoft.AspNetCore.Http.Features;
using KnowledgeBank.Utils;

namespace KnowledgeBank
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Ensure the current directory is set to the directory of the executable
            // # Builder
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            ConfigureLogging();

            // The environment variables are loaded from the .env file configured in the docker-compose file.
            EnvironmentConfig environmentConfig = new EnvironmentConfig(builder.Configuration);
            environmentConfig.CheckEnvironmentVariables();
            builder.Services.AddSingleton(environmentConfig);

            // # Services
            builder.Services.AddControllers();
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<IAzureBlobService, AzureBlobService>();
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

            // Swagger
            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen(ConfigureSwagger);


            // # Database context
            builder.Services.AddDbContext<DatabaseContext>(
                options => options.UseNpgsql(environmentConfig.GetVariableValue(EnvironmentVariable.CONNECTION_STRING))
            );


            // Resource management
            builder.Services.AddScoped<ResourceManager>();


            // Retrieval Augmented Generation system
            builder.Services.AddSingleton<RAGSystem, RAGSystem>();
            builder.Services.AddScoped<RAGManger>();


            // Background services
            builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
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

            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.Limits.MaxRequestBodySize = 110100480; // 105 MB in bytes
            });

            // # Application
            WebApplication app = builder.Build();

            // # Create database if it does not exist
            app.EnsureCreatedDatabase();

            // # Middleware
            if (app.Environment.IsDevelopment())
            {
                // Run only in development environment:
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI(ConfigureSwaggerUI);
                app.UseDeveloperExceptionPage();
            }

            // Seeding the database with initial data
            using (IServiceScope scope = app.Services.CreateScope())
            {
                await RoleInitializer.InitializeAsync(app.Services);
                await DatabaseSeeder.Seed(app.Services);
                await scope.ServiceProvider.GetRequiredService<DatabaseContext>().EnsureViewsCreatedAsync();

                if (app.Environment.IsDevelopment())
                {
                    // Seed test data only in development environment:
                    await TestDataSeeder.Seed(app.Services);
                }
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowFrontend");
            app.UseAuthentication();
            app.UseAuthorization();

            // Hub and Controllers must be added after Authentication and Authorization
            app.MapHub<Chat>("/chat"); // SignalR hub for chat functionality
            app.MapControllers();

            app.MapGroup("Auth").MapIdentityApi<User>().WithTags("Auth").WithOpenApi(ConfigureIdentityApiOptions).AddEndpointFilter(async (efiContext, next) =>
            {
                if (HideEndpointFilter.PathsToHide.Any(p => p == efiContext.HttpContext.Request.Path))
                    return Results.Forbid();
                return await next(efiContext);
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


        /// <summary>
        /// Configures the OpenAPI operation metadata for identity API endpoints.
        /// </summary>
        /// <param name="operation">The OpenAPI operation to configure.</param>
        /// <returns>The configured OpenAPI operation with updated summary information.</returns>
        /// <remarks>
        /// This method sets the summary description for identity-related API endpoints that handle user management operations.
        /// </remarks>
        private static OpenApiOperation ConfigureIdentityApiOptions(OpenApiOperation operation)
        {
            operation.Summary = "Identity endpoints for user management";
            return operation;
        }
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


