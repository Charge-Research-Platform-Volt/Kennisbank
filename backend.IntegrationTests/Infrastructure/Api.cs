using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBank.IntegrationTests.Infrastructure;

/// <summary>Ids of the document types the AddProjectDocuments migration seeds.</summary>
public static class ProjectDocumentTypes
{
    public static readonly Guid Minutes = Guid.Parse("9b3eeb99-8bb2-4d92-9b54-aa27f6e20657");
}

/// <summary>Small helpers for setting up test data through the real API, the way the frontend does.</summary>
public static class Api
{
    /// <summary>A name that can't collide with data from other tests (they share one database).</summary>
    public static string Unique(string name) => $"{name} {Guid.NewGuid().ToString("N")[..8]}";

    public static async Task<Guid> CreateResourceTypeAsync(this HttpClient client)
        => await ReadGuidAsync(await client.PutAsJsonAsync("/resource-types", new { name = Unique("Report") }));

    public static async Task<Guid> CreateTagAsync(this HttpClient client, string? name = null)
        => await ReadGuidAsync(await client.PutAsJsonAsync("/tags", new { name = name ?? Unique("Tag") }));

    public static async Task<Guid> CreateResourceAsync(this HttpClient client, Guid typeId, string? title = null, params string[] tags)
        => await ReadGuidAsync(await client.PutAsJsonAsync("/resources", new
        {
            title = title ?? Unique("Resource"),
            description = "Integration test resource",
            typeId = typeId.ToString(),
            languageCode = "en",
            tags
        }));

    public static async Task<Guid> CreatePersonAsync(this HttpClient client)
        => await ReadGuidAsync(await client.PutAsJsonAsync("/persons", new { name = Unique("Person") }));

    public static async Task<Guid> CreateProjectAsync(this HttpClient client, params Guid[] members)
    {
        HttpResponseMessage response = await client.PutAsJsonAsync("/projects", new { title = Unique("Project"), projectType = "root", members });
        await EnsureAsync(response, HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("projectId").GetGuid();
    }

    /// <summary>Runs the chunked upload the frontend uses (init, one part, finalize) and returns the object name.</summary>
    public static async Task<string> UploadFileAsync(this HttpClient client, string fileName, byte[] content, string purpose = "Library")
    {
        HttpResponseMessage init = await client.PostAsJsonAsync("/files/upload/init", new { fileName, fileSize = content.Length, purpose });
        await EnsureAsync(init, HttpStatusCode.OK);
        JsonElement started = await init.Content.ReadFromJsonAsync<JsonElement>();
        string objectName = started.GetProperty("objectName").GetString()!;
        string uploadId = started.GetProperty("uploadId").GetString()!;

        HttpResponseMessage part = await client.PostAsync($"/files/upload/part/{objectName}/{uploadId}/1?purpose={purpose}", new ByteArrayContent(content));
        await EnsureAsync(part, HttpStatusCode.OK);
        string eTag = (await part.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("eTag").GetString()!;

        HttpResponseMessage finalize = await client.PostAsJsonAsync("/files/upload/finalize", new { objectName, uploadId, purpose, partETags = new Dictionary<int, string> { [1] = eTag } });
        await EnsureAsync(finalize, HttpStatusCode.NoContent);

        return objectName;
    }

    public static Task<string> UploadProjectDocumentFileAsync(this HttpClient client, string fileName = "document.pdf", byte[]? content = null)
        => client.UploadFileAsync(fileName, content ?? "%PDF-1.4 test"u8.ToArray(), "ProjectDocument");

    /// <summary>Uploads a file and creates a project document from it, the way the upload dialog does.</summary>
    public static async Task<Guid> CreateProjectDocumentAsync(this HttpClient client, Guid projectId, string visibility = "ProjectMembers",
        string generalAccessType = "Editor", object[]? grants = null, string? title = null, string fileName = "document.pdf", byte[]? content = null, Guid? typeId = null)
    {
        string objectName = await client.UploadProjectDocumentFileAsync(fileName, content);
        return await ReadGuidAsync(await client.PostAsJsonAsync($"/projects/{projectId}/documents", new
        {
            objectName,
            fileName,
            title = title ?? Unique("Document"),
            typeId = typeId ?? ProjectDocumentTypes.Minutes,
            visibility,
            generalAccessType,
            grants = grants ?? []
        }));
    }

    public static object Grant(Guid userId, string accessType) => new { userId, accessType };

    public static async Task<LibraryResult> BrowseLibraryAsync(this HttpClient client, object request)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync("/library", request);
        await EnsureAsync(response, HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<LibraryResult>())!;
    }

    public static async Task<Guid> ReadGuidAsync(HttpResponseMessage response)
    {
        await EnsureAsync(response, HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    /// <summary>Like EnsureSuccessStatusCode, but shows the response body when it fails.</summary>
    public static async Task EnsureAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
            throw new HttpRequestException($"{response.RequestMessage?.Method} {response.RequestMessage?.RequestUri?.PathAndQuery}: expected {(int)expected}, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }

    /// <summary>Waits until a resource's background ingestion has finished (either way).</summary>
    public static async Task<Resource> WaitForEmbeddingAsync(this KennisbankFixture fixture, Guid resourceId)
    {
        Resource? resource = null;
        await WaitUntilAsync(async () =>
        {
            using IServiceScope scope = fixture.Services.CreateScope();
            DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            resource = await db.Resources.AsNoTracking().FirstOrDefaultAsync(r => r.Id == resourceId);
            return resource?.EmbeddingStatus is EmbeddingStatus.Completed or EmbeddingStatus.Failed;
        }, $"embedding of resource {resourceId}");
        return resource!;
    }

    /// <summary>Polls until <paramref name="condition"/> holds, for background work and Meilisearch's asynchronous indexing.</summary>
    public static async Task WaitUntilAsync(Func<Task<bool>> condition, string what, int timeoutSeconds = 30)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (!await condition())
        {
            if (DateTime.UtcNow > deadline) throw new TimeoutException($"Timed out waiting for {what}");
            await Task.Delay(100);
        }
    }
}
