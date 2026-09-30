namespace KnowledgeBank.Services.Storage;

/// <summary>Downloaded object with stream, metadata, and content info.</summary>
public record ObjectDownloadResponse(
    Stream Stream,
    IDictionary<string, string> Metadata,
    long ContentLength,
    string? ContentType = null
);

public record StorageObjectInfo(string Key, DateTime? LastModifiedUtc);

/// <summary>
/// Abstraction for object storage (S3, MinIO, etc.).
/// All methods throw on failure - if it returns, it succeeded.
/// </summary>
public interface IStorageService
{
    // ==================== Bucket Operations ====================

    /// <summary>Creates a bucket. Idempotent - succeeds if already exists.</summary>
    Task CreateBucketAsync(string bucketName);

    /// <summary>Deletes a bucket. Must be empty first.</summary>
    Task DeleteBucketAsync(string bucketName);

    // ==================== Object CRUD ====================

    /// <summary>Uploads an object to storage.</summary>
    Task UploadObjectAsync(
        string bucketName,
        string objectName,
        Stream content,
        IDictionary<string, string>? metadata = null,
        string? contentType = null
    );

    /// <summary>Downloads an object from storage.</summary>
    Task<ObjectDownloadResponse> DownloadObjectAsync(string bucketName, string objectName);

    /// <summary>Deletes an object from storage.</summary>
    Task DeleteObjectAsync(string bucketName, string objectName);

    /// <summary>Gets object metadata without downloading content.</summary>
    Task<IDictionary<string, string>> GetObjectMetadataAsync(string bucketName, string objectName);

    // ==================== Listing ====================

    /// <summary>Lists all object keys in a bucket, optionally filtered by prefix.</summary>
    Task<StorageObjectInfo[]> ListObjectsAsync(string bucketName, string? prefix = null);

    // ==================== Multipart Uploads ====================

    /// <summary>Starts a multipart upload. Returns uploadId for subsequent calls.</summary>
    Task<string> InitiateMultipartUploadAsync(string bucketName, string objectName, IDictionary<string, string>? metadata = null, string? contentType = null);

    /// <summary>Uploads a part. Returns ETag needed for completion.</summary>
    Task<string> UploadPartAsync(string bucketName, string objectName, string uploadId, int partNumber, Stream content);

    /// <summary>Completes multipart upload by assembling all parts.</summary>
    Task CompleteMultipartUploadAsync(
        string bucketName,
        string objectName,
        string uploadId,
        IDictionary<int, string> partETags
    );

    /// <summary>Aborts multipart upload and deletes uploaded parts.</summary>
    Task AbortMultipartUploadAsync(string bucketName, string objectName, string uploadId);
}
