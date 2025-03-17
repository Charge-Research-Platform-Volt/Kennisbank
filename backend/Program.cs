using backend.Data;
using KnowledgeBank.Data;
using KnowledgeBank.Extensions;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace KnowledgeBank
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // # Builder
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            ConfigureLogging();

            // # Services
            builder.Services.AddControllers();
            builder.Services.AddSingleton<IAzureBlobService, AzureBlobService>();

            // # Authentication
            builder.Services.AddAuthorization();
            builder.Services.AddAuthorizationBuilder();


            // Add this line after the code below to enable authentication with JWT tokens: .AddBearerToken(IdentityConstants.BearerScheme);
            builder.Services.AddAuthentication().AddCookie(IdentityConstants.ApplicationScheme).AddBearerToken(IdentityConstants.BearerScheme);
            builder.Services.AddIdentityCore<User>().AddEntityFrameworkStores<DatabaseContext>().AddApiEndpoints();
            // builder.Services.AddIdentityApiEndpoints<User>().AddEntityFrameworkStores<DatabaseContext>();

            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen(ConfigureSwagger);


            // # Database context
            builder.Services.AddDbContext<DatabaseContext>(
                // CONNECTION_STRING is set in docker-compose.dev.yml file
                options => options.UseNpgsql(builder.Configuration.GetValue<string>("CONNECTION_STRING")
            ));


            // # Application
            WebApplication app = builder.Build();


            // # Middleware
            if (app.Environment.IsDevelopment())
            {
                // Run only in development environment:
                app.MapOpenApi();
                app.UseSwagger();
                app.UseSwaggerUI(ConfigureSwaggerUI);
                app.UseDeveloperExceptionPage();

                // Apply database migrations
                app.ApplyMigrations();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.MapControllers();

            // # Authentication
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapGroup("Auth").MapIdentityApi<User>().WithTags("Auth").WithOpenApi(ConfigureIdentityApiOptions);

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
            var configuration = new ConfigurationBuilder()
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