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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


