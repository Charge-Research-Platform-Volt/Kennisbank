using System.Collections.Concurrent;
using KnowledgeBank.Services.Storage;

namespace KnowledgeBank.IntegrationTests.Infrastructure;

/// <summary>In-memory replacement for S3, so tests need no object storage.</summary>
public sealed class InMemoryStorageService : IStorageService
{
    private readonly ConcurrentDictionary<string, byte[]> objects = new();
    private readonly ConcurrentDictionary<string, DateTime> lastModified = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, byte[]>> uploads = new();

    private static string Key(string bucket, string name) => $"{bucket}/{name}";

    public bool Exists(string bucketName, string objectName) => objects.ContainsKey(Key(bucketName, objectName));

    /// <summary>Backdates an object, so StorageCleanupService treats it as past its grace period.</summary>
    public void SetLastModified(string bucketName, string objectName, DateTime modifiedUtc) => lastModified[Key(bucketName, objectName)] = modifiedUtc;

    private void Store(string bucketName, string objectName, byte[] data)
    {
        objects[Key(bucketName, objectName)] = data;
        lastModified[Key(bucketName, objectName)] = DateTime.UtcNow;
    }

    public Task CreateBucketAsync(string bucketName) => Task.CompletedTask;

    public Task DeleteBucketAsync(string bucketName) => Task.CompletedTask;

    public async Task UploadObjectAsync(string bucketName, string objectName, Stream content, IDictionary<string, string>? metadata = null, string? contentType = null)
    {
        using MemoryStream buffer = new();
        await content.CopyToAsync(buffer);
        Store(bucketName, objectName, buffer.ToArray());
    }

    public Task<ObjectDownloadResponse> DownloadObjectAsync(string bucketName, string objectName)
    {
        if (!objects.TryGetValue(Key(bucketName, objectName), out byte[]? data))
            throw new FileNotFoundException($"Object {objectName} not found");

        return Task.FromResult(new ObjectDownloadResponse(new MemoryStream(data), new Dictionary<string, string>(), data.Length, "application/octet-stream"));
    }

    public Task DeleteObjectAsync(string bucketName, string objectName)
    {
        objects.TryRemove(Key(bucketName, objectName), out _);
        lastModified.TryRemove(Key(bucketName, objectName), out _);
        return Task.CompletedTask;
    }

    public Task<IDictionary<string, string>> GetObjectMetadataAsync(string bucketName, string objectName)
        => Task.FromResult<IDictionary<string, string>>(new Dictionary<string, string>());

    public Task<StorageObjectInfo[]> ListObjectsAsync(string bucketName, string? prefix = null)
        => Task.FromResult(objects.Keys
            .Where(k => k.StartsWith($"{bucketName}/{prefix}"))
            .Select(k => new StorageObjectInfo(k[(bucketName.Length + 1)..], lastModified.GetValueOrDefault(k, DateTime.UtcNow)))
            .ToArray());

    public Task<string> InitiateMultipartUploadAsync(string bucketName, string objectName, IDictionary<string, string>? metadata = null, string? contentType = null)
    {
        string uploadId = Guid.NewGuid().ToString();
        uploads[uploadId] = new();
        return Task.FromResult(uploadId);
    }

    public async Task<string> UploadPartAsync(string bucketName, string objectName, string uploadId, int partNumber, Stream content)
    {
        using MemoryStream buffer = new();
        await content.CopyToAsync(buffer);
        uploads[uploadId][partNumber] = buffer.ToArray();
        return $"etag-{partNumber}";
    }

    public Task CompleteMultipartUploadAsync(string bucketName, string objectName, string uploadId, IDictionary<int, string> partETags)
    {
        if (uploads.TryRemove(uploadId, out var stored))
            Store(bucketName, objectName, [.. stored.OrderBy(p => p.Key).SelectMany(p => p.Value)]);
        return Task.CompletedTask;
    }

    public Task AbortMultipartUploadAsync(string bucketName, string objectName, string uploadId)
    {
        uploads.TryRemove(uploadId, out _);
        return Task.CompletedTask;
    }
}
