
using KnowledgeBank.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using Serilog;
using static Qdrant.Client.Grpc.Conditions;
using KnowledgeBank.Models;
using System.Text.Json;
using HandlebarsDotNet;

namespace KnowledgeBank.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
[Produces("application/json")]
public class AIController : ControllerBase
{
    private readonly Serilog.ILogger _logger;
    private readonly RAGSystem _ragSystem;

    public AIController(RAGSystem ragSystem)
    {
        _logger = Log.ForContext<AIController>();
        _ragSystem = ragSystem;
    }


    [HttpPost("generate-tags")]
    public async Task<IActionResult> GenerateTags(string id = "0d4dfb35-3b75-4729-a7cf-92f4823b1b5c")
    {
        ulong offset = 0;
        ulong limit = 20;
        HashSet<string> uniqueTags = [];

        string source =
@"You are tasked with generating relevant tags for a large document, which is provided to you in smaller chunks. For each batch, you will receive:
- A list of tags that have already been generated for the document.
- The current group of content chunks.

Instructions:
1. Carefully read the provided content chunks.
2. Generate new, relevant tags that accurately reflect the content of these chunks.
3. Do not repeat or include any tags that have already been generated (see the list below).
4. Ensure all tags are concise, specific, and directly related to the content.

Previously Generated Tags:
{{tags}}

Current Document Chunks:
{{#each content}}
{{text}}
---
{{/each}}";

        var template = Handlebars.Compile(source);

        var jsonSchema = """
        {
            "title": "Tags Extraction",
            "type": "object",
            "properties": {
                "Tags": {
                    "type": "array",
                    "items": {
                        "type": "string"
                    },
                    "description": "A list of tags extracted from the document chunks.",
                    "example": ["tag1", "tag2", "tag3"]
                }
            },
            "required": ["Tags"]
        }
        """;



        ChatCompletionOptions options = new ChatCompletionOptions()
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat("TagsExtraction", BinaryData.FromString(jsonSchema))
        };

        while (true)
        {
            // Perform a vector search to retrieve results
            var results = await _ragSystem.QdrantClient.QueryAsync(
                RAGSystem.COLLECTION_NAME,
                filter: MatchKeyword("resourceId", id),
                limit: limit,
                offset: offset
            );

            // If no results are returned, break the loop
            if (results.Count == 0) break;

            // Prepare the data for the template
            var data = new
            {
                tags = uniqueTags.Count == 0 ? "No tags generated yet." : string.Join(", ", uniqueTags),
                content = results.Select(item =>
                {
                    var payload = CustomPayload.FromPayload(item.Payload);
                    return new { text = payload.ChunkText };
                }).ToList()
            };
            var result = template(data);
            Console.WriteLine(result);

            var chat = new List<ChatMessage>()
            {
                new SystemChatMessage("You are a helpful AI assistant that extracts tags from a document and returns them as a JSON"),
                new UserChatMessage(result)
            };

            // Get a completion with structured output
            var response = await _ragSystem.ChatClient.CompleteChatAsync(chat, options);
            Console.WriteLine(response.Value.Content[0].Text);

            var jsonOutput = response.Value.Content[0].Text;
            var tagsExtraction = JsonSerializer.Deserialize<TagsExtraction>(jsonOutput);
            if (tagsExtraction == null || tagsExtraction.Tags == null) continue;

            _logger.Information("Extracted tags: {Tags}", string.Join(", ", tagsExtraction.Tags));
            // Add the tags to the unique set
            foreach (var tag in tagsExtraction.Tags)
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    var unique = uniqueTags.Add(tag.Trim().ToLowerInvariant());
                    if (unique)
                    {
                        _logger.Information("New tag added: {Tag}", tag.Trim());
                    }
                    else
                    {
                        _logger.Information("Tag already exists: {Tag}", tag.Trim());
                    }
                }
            }


            // Increment the offset for the next batch
            offset += limit;
        }


        // print the unique tags
        _logger.Information("Unique tags extracted: {Tags}", string.Join(", ", uniqueTags));


        return Ok(new
        {
            Tags = uniqueTags.ToList(),
        });
    }
}

public class TagsExtraction
{
    public required List<string> Tags { get; set; }
}

