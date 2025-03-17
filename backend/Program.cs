using backend.Data;
using backend.Security;
using KnowledgeBank.Data;
using KnowledgeBank.Extensions;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.Threading.Tasks;

namespace KnowledgeBank
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // # Builder
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            ConfigureLogging();

            // # Services
            builder.Services.AddControllers();
            builder.Services.AddSingleton<IAzureBlobService, AzureBlobService>();
            builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, CustomAuthorizationMiddlewareResultHandler>();
            builder.Services.AddAuthorization(options =>
            {
                foreach (string roleName in RoleInitializer.roleNames)
                {
                    options.AddPolicy($"Require{ char.ToUpper(roleName[0]) + roleName.Substring(1) }Role", policy => policy.RequireRole(roleName));
                }

                // This line terminates the handler on first failure, when more information is required, set this to true.
                // This gives an increase in performance, but omits some information which might be required for complex
                // authorization scenarios. This setting only affects the authorization middleware, not the controllers.
                options.InvokeHandlersAfterFailure = false;
            });
            //
            // Add this line after the code below to enable authentication with JWT tokens: .AddBearerToken(IdentityConstants.BearerScheme);
            builder.Services.AddAuthentication().AddCookie(IdentityConstants.ApplicationScheme);
            
            builder.Services.AddIdentityCore<User>()
                            .AddRoles<IdentityRole>()
                            .AddEntityFrameworkStores<DatabaseContext>()
                            .AddApiEndpoints();


            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerGen(ConfigureSwagger);


            // # Database context
            builder.Services.AddDbContext<DatabaseContext>(
                // CONNECTION_STRING is set in docker-compose.dev.yml file
                options => options.UseNpgsql(builder.Configuration.GetValue<string>("CONNECTION_STRING")
            ));

            // CORS to allow Cross Origin Resource Sharing
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend", policy =>
                {
                    policy.WithOrigins("http://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

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

                app.ApplyMigrations();
            }

            // Initialize roles
            using (IServiceScope scope = app.Services.CreateScope())
            {
                await RoleInitializer.InitializeAsync(app.Services);
            }

            app.UseRouting();
            app.UseCors("AllowFrontend");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapIdentityApi<User>();

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
    }
}
