
using KnowledgeBank.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using Serilog;
using static Qdrant.Client.Grpc.Conditions;
using KnowledgeBank.Models;
using System.Text.Json;
using HandlebarsDotNet;
using KnowledgeBank.Responses;
using KnowledgeBank.Utils;
using KnowledgeBank.Data;

namespace KnowledgeBank.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
[Produces("application/json")]
public class AIController : ControllerBase
{
    private readonly Serilog.ILogger _logger;
    private readonly RAGSystem _ragSystem;
    private readonly ResourceManager _resourceManager;

    public AIController(RAGSystem ragSystem, ResourceManager resourceManager)
    {
        _logger = Log.ForContext<AIController>();
        _ragSystem = ragSystem;
        _resourceManager = resourceManager;
    }


    [HttpPost("generate-tags")]
    public async Task<IActionResult> GenerateTags(string id)
    {
        // Check if the resource exists
        if (!ValidityUtil.IsValidId(id))
            return BadRequest(new ApiResponse(false, "Invalid ID."));

        try
        {
            // Check if resource exists
            if (!await _resourceManager.ResourceExistsAsync(id))
                return NotFound(new ApiResponse(false, "Resource not found."));


            // Get the existing AI-generated tags from the resource
            var existingAiTags = await _resourceManager.GetResourcePropertyOrDefaultAsync<string?>(Guid.Parse(id), r => r.AiGeneratedTags);

            if (!string.IsNullOrWhiteSpace(existingAiTags))
            {
                try
                {
                    var existingTagsList = JsonSerializer.Deserialize<List<string>>(existingAiTags);
                    if (existingTagsList != null && existingTagsList.Count > 0)
                    {
                        _logger.Information("Returning existing AI-generated tags for resource {ResourceId}", id);
                        return Ok(new ApiResponse(true, "AI-generated tags already exist", new
                        {
                            Tags = existingTagsList
                        }));
                    }
                }
                catch (JsonException)
                {
                    _logger.Warning("Failed to deserialize existing AI-generated tags for resource {ResourceId}. Regenerating tags.", id);
                }
            }


            // If no existing tags, proceed to generate new tags
            _logger.Information("Generating new AI tags");

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


            try
            {
                // Convert to list and save to database
                var generatedTagsList = uniqueTags.ToList();


                // print the unique tags
                _logger.Information("Tags generated successfully");
                if (generatedTagsList.Count > 0)
                {
                    // Serialize the tags to JSON and save to the resource
                    await _resourceManager.BeginTransaction();
                    var tagsJson = JsonSerializer.Serialize(generatedTagsList);
                    await _resourceManager.UpdateResourceAsync(Guid.Parse(id), r => r.AiGeneratedTags, tagsJson);
                    await _resourceManager.Commit();
                    _logger.Information("AI-generated tags saved to resource {ResourceId}", id);
                }
                _logger.Information("Tags generated successfully");

                return Ok(
                        new ApiResponse(true, "Tags generated successfully", new
                        {
                            Tags = uniqueTags.ToList()
                        })
                    );
            }
            catch (Exception)
            {
                await _resourceManager.Rollback();
                _logger.Error("An error occurred while saving AI-generated tags to the resource {ResourceId}", id);
                return StatusCode(500, new ApiResponse(false, "An error occurred while saving AI-generated tags."));
            }
        }
        catch (Exception)
        {
            _logger.Error("An error occurred while generating tags.");
            return StatusCode(500, new ApiResponse(false, "An error occurred while generating tags."));
        }
    }



    [HttpPost("vector-search")]
    public async Task<IActionResult> VectorSearch(string query = "What is charge?")
    {
        _logger.Information("Vector search initiated with query: {Query}", query);

        try
        {
            var search = await _ragSystem.QdrantClient.QueryGroupsAsync(
                collectionName: RAGSystem.COLLECTION_NAME,
                groupBy: "resourceId",
                limit: 10
            );
        }
        catch (Exception)
        {

            throw;
        }

        return Ok(new
        {
            Message = "Vector search initiated",
            Query = query
        });
    }
}

public class TagsExtraction
{
    public required List<string> Tags { get; set; }
}