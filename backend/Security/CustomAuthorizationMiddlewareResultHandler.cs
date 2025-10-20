using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace KnowledgeBank.Security
{
    public class CustomAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

        public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Challenged)
            {
                // User isn't authenticated - return 401 Unauthorized
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.Append("WWW-Authenticate", "Bearer");
                await context.Response.WriteAsJsonAsync(new { message = "Authentication required" });
                return;
            }
            else if (authorizeResult.Forbidden)
            {
                // User is authenticated but doesn't have the permission - return 403 Forbidden
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "Insufficient permissions" });
                return;
            }

            // All other cases are handled by default handler
            await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)