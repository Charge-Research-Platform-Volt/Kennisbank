using System.Linq.Expressions;
using System.Reflection;
using KnowledgeBank.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using OpenAI.Chat;
using Serilog;

namespace KnowledgeBank.Controllers;
#pragma warning disable SKEXP0001, SKEXP0010, SKEXP0020, SKEXP0050, SKEXP0070

[ApiController]
[Authorize]
[Route("[controller]")]
[Produces("application/json")]
public class AIController : ControllerBase
{
    private readonly Serilog.ILogger _logger;
    private readonly IRAGSystem _ragSystem;

    public AIController(IRAGSystem ragSystem)
    {
        _logger = Log.ForContext<AIController>();
        _ragSystem = ragSystem;
    }


    [HttpPost("generate-tags")]
    public async Task<IActionResult> GenerateTags(string id = "8ab383a0-2091-44e6-8a8b-bebdf9b647b7")
    {
        _logger.Information("Generating tags for file with ID: {Id}", id);

        // ------------------
        Expression<Func<ResourceVectorStoreRecord, bool>> filter = x => x.ResourceId == id;
        IAsyncEnumerable<ResourceVectorStoreRecord> searchResult = _ragSystem.Collection.GetAsync(filter: filter, top: 10);

        var searchResults = await searchResult.ToListAsync();

        // console log the results
        Console.WriteLine(searchResults.Count());
        // foreach (var result in searchResults)
        // {
        //     Console.WriteLine($"ResourceId: {result.ResourceId}, Content: {result.ChunkText}");
        // }

        // console log searchResults as json
        // var json = System.Text.Json.JsonSerializer.Serialize(searchResults);
        // Console.WriteLine(json);

        // ----


        // var handlebarsPromptYaml = EmbeddedResource.Read("GenerateTags.yaml");
        // var templateFactory = new HandlebarsPromptTemplateFactory();
        // var function = _ragSystem.Kernel.CreateFunctionFromPromptYaml(handlebarsPromptYaml, templateFactory);


        // var arguments = new KernelArguments()
        // {
        //     { "documentInfo", new{ title = "Text title"}},
        //     { "content", searchResults.ToList() },
        // };

        // var promptTemplateConfig = new PromptTemplateConfig()
        // {
        //     Template = handlebarsPromptYaml,
        //     TemplateFormat = "handlebars",
        //     Name = "GenerateTags",
        // };



        // var promptTemplate = templateFactory.Create(promptTemplateConfig);
        // var renderedPrompt = await promptTemplate.RenderAsync(_ragSystem.Kernel, arguments);
        // Console.WriteLine($"Rendered Prompt:\n{renderedPrompt}\n");

        // var response = await _ragSystem.Kernel.InvokeAsync(function, arguments);
        // Console.WriteLine(response);
        // ----


        ChatResponseFormat chatResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
            jsonSchemaFormatName: "movie_result",
            jsonSchema: BinaryData.FromString("""
        {
            "type": "object",
            "properties": {
                "Movies": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "Title": { "type": "string" },
                            "Director": { "type": "string" },
                            "ReleaseYear": { "type": "integer" },
                            "Rating": { "type": "number" },
                            "IsAvailableOnStreaming": { "type": "boolean" },
                            "Tags": { "type": "array", "items": { "type": "string" } }
                        },
                        "required": ["Title", "Director", "ReleaseYear", "Rating", "IsAvailableOnStreaming", "Tags"],
                        "additionalProperties": false
                    }
                }
            },
            "required": ["Movies"],
            "additionalProperties": false
        }
        """),
            jsonSchemaIsStrict: true);

        var executionSettings = new OpenAIPromptExecutionSettings
        {
            ResponseFormat = chatResponseFormat
        };

        var result = await _ragSystem.Kernel.InvokePromptAsync("What are the top 10 movies of all time?", new(executionSettings));
        Console.WriteLine(result);



        // ------------------


        return Ok(new
        {
            Message = "Vector search initiated",
            Query = id
        });
    }
}


public static class EmbeddedResource
{
    private static readonly string? s_namespace = typeof(EmbeddedResource).Namespace;

    internal static string Read(string fileName)
    {
        // Get the current assembly. Note: this class is in the same assembly where the embedded resources are stored.
        Assembly assembly =
            typeof(EmbeddedResource).GetTypeInfo().Assembly ??
            throw new InvalidOperationException($"[{s_namespace}] {fileName} assembly not found");

        // Resources are mapped like types, using the namespace and appending "." (dot) and the file name
        var resourceName = $"{s_namespace}." + fileName;
        using Stream resource =
            assembly.GetManifestResourceStream(resourceName) ??
            throw new InvalidOperationException($"{resourceName} resource not found");

        // Return the resource content, in text format.
        using var reader = new StreamReader(resource);

        return reader.ReadToEnd();
    }
}