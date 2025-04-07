using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public class HideEndpointFilter : IDocumentFilter
{
    public static string[] PathsToHide = new[] { "/Auth/register" };

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var path in PathsToHide)
        {
            if (swaggerDoc.Paths.ContainsKey(path))
            {
                swaggerDoc.Paths.Remove(path);
            }
        }
    }
}