using System.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using Serilog;
using KnowledgeBank.Utils;

namespace KnowledgeBank.Services.Storage;

public class S3StorageService : IStorageService
{
    private readonly IAmazonS3 client;
    private readonly Serilog.ILogger logger;

    public S3StorageService(EnvironmentConfig environmentConfig)
    {
        AmazonS3Config config = new AmazonS3Config
        {
            ServiceURL = environmentConfig.GetVariableValue(EnvironmentVariable.S3_ENDPOINT),
            ForcePathStyle = true,
            AuthenticationRegion = environmentConfig.GetVariableValue(EnvironmentVariable.S3_REGION)
        };

        client = new AmazonS3Client(
            environmentConfig.GetVariableValue(EnvironmentVariable.S3_ACCESS_KEY),
            environmentConfig.GetVariableValue(EnvironmentVariable.S3_SECRET_KEY),
            config
        );

        logger = Log.ForContext<S3StorageService>();
    }

    // ==================== Bucket Operations ====================

    /// <inheritdoc />
    public async Task CreateBucketAsync(string bucketName)
    {
        logger.Information("Creating bucket {BucketName}", bucketName);
        try
        {
            await client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyOwnedByYou" || ex.ErrorCode == "BucketAlreadyExists")
        {
            logger.Information("Bucket {BucketName} already exists, skipping creation", bucketName);
        }
    }

    /// <inheritdoc />
    public async Task DeleteBucketAsync(string bucketName)
    {
        logger.Information("Deleting bucket {BucketName}", bucketName);
        await client.DeleteBucketAsync(new DeleteBucketRequest { BucketName = bucketName });
    }

    // ==================== Object CRUD ====================

    /// <inheritdoc />
    public async Task UploadObjectAsync(
        string bucketName,
        string objectName,
        Stream content,
        IDictionary<string, string>? metadata = null,
        string? contentType = null)
    {
        logger.Information("Uploading {ObjectName} into {BucketName}", objectName, bucketName);
    
        PutObjectRequest request = new()
        {
            BucketName = bucketName,
            Key = objectName,
            InputStream = content,
            ContentType = contentType
        };
        
        if (metadata != null) 
        {
            foreach (var kvp in metadata)
                request.Metadata.Add(kvp.Key, kvp.Value);
        }

        await client.PutObjectAsync(request);
    }
    
    /// <inheritdoc />
    public async Task<ObjectDownloadResponse> DownloadObjectAsync(string bucketName, string objectName) 
    {
        logger.Information("Downloading {ObjectName} from {BucketName}", objectName, bucketName);
        
        GetObjectRequest request = new()
        {
            BucketName = bucketName,
            Key = objectName
        };

        GetObjectResponse response = await client.GetObjectAsync(request);

        // Convert metadata collection to dictionary, stripping the x-amz-meta- prefix
        // S3 lowercases all keys, so we use case-insensitive dictionary
        Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase);
        foreach (var key in response.Metadata.Keys)
        {
            string normalizedKey = key.StartsWith("x-amz-meta-") ? key["x-amz-meta-".Length..] : key;
            metadata[normalizedKey] = response.Metadata[key];
        }

        return new ObjectDownloadResponse
        (
            response.ResponseStream,
            metadata,
            response.ContentLength,
            response.Headers.ContentType
        );
    }
    
    /// <inheritdoc />
    public async Task DeleteObjectAsync(string bucketName, string objectName) 
    {
        logger.Information("Deleting {ObjectName} from {BucketName}", objectName, bucketName);
    
        DeleteObjectRequest request = new()
        {
            BucketName = bucketName,
            Key = objectName
        };

        await client.DeleteObjectAsync(request);
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, string>> GetObjectMetadataAsync(string bucketName, string objectName)
    {
        logger.Information("Retrieving metadata of {ObjectName} from {BucketName}", objectName, bucketName);

        GetObjectMetadataResponse response = await client.GetObjectMetadataAsync(bucketName, objectName);

        // Convert metadata collection to dictionary, stripping the x-amz-meta- prefix
        // S3 lowercases all keys, so we use case-insensitive dictionary
        Dictionary<string, string> metadata = new(StringComparer.OrdinalIgnoreCase);
        foreach (var key in response.Metadata.Keys)
        {
            string normalizedKey = key.StartsWith("x-amz-meta-") ? key["x-amz-meta-".Length..] : key;
            metadata[normalizedKey] = response.Metadata[key];
        }

        return metadata;
    }

    // ==================== Listing ====================

    /// <inheritdoc />
    public async Task<string[]> ListObjectsAsync(string bucketName, string? prefix = null)
    {
        logger.Information("Listing objects in {BucketName} with prefix '{Prefix}'", bucketName, prefix ?? "(none)");

        var request = new ListObjectsV2Request
        {
            BucketName = bucketName,
            Prefix = prefix
        };

        var keys = new List<string>();
        ListObjectsV2Response response;

        do
        {
            response = await client.ListObjectsV2Async(request);
            keys.AddRange((response.S3Objects ?? []).Select(o => o.Key));
            request.ContinuationToken = response.NextContinuationToken;
        } while (response.IsTruncated == true);

        return keys.ToArray();
    }

    // ==================== Multipart Uploads ====================
    
    /// <inheritdoc />
    public async Task<string> InitiateMultipartUploadAsync(string bucketName, string objectName, IDictionary<string, string>? metadata = null, string? contentType = null) 
    {
        logger.Information("Initiating multipart upload for {ObjectName} in {BucketName}", objectName, bucketName);
    
        InitiateMultipartUploadRequest request = new()
        {
            BucketName = bucketName,
            Key = objectName,
            ContentType = contentType
        };
        
        if (metadata != null) 
        {
            foreach (var kvp in metadata)
                request.Metadata.Add(kvp.Key, kvp.Value);
        }

        InitiateMultipartUploadResponse response = await client.InitiateMultipartUploadAsync(request);

        return response.UploadId;
    }
    
    /// <inheritdoc />
    public async Task<string> UploadPartAsync(
        string bucketName,
        string objectName,
        string uploadId,
        int partNumber,
        Stream content
    ) 
    {
        UploadPartRequest request = new()
        {
            BucketName = bucketName,
            Key = objectName,
            UploadId = uploadId,
            PartNumber = partNumber,
            InputStream = content
        };

        UploadPartResponse response = await client.UploadPartAsync(request);

        return response.ETag;
    }
    
    /// <inheritdoc />
    public async Task CompleteMultipartUploadAsync(
        string bucketName,
        string objectName,
        string uploadId,
        IDictionary<int, string> partETags
    ) 
    {
        logger.Information("Completing multipart upload for {ObjectName} in {BucketName}", objectName, bucketName);

        CompleteMultipartUploadRequest request = new()
        {
            BucketName = bucketName,
            Key = objectName,
            UploadId = uploadId,
            PartETags = partETags.Select(kvp => new PartETag(kvp.Key, kvp.Value)).ToList()
        };

        await client.CompleteMultipartUploadAsync(request);
    }
    
    /// <inheritdoc />
    public async Task AbortMultipartUploadAsync(string bucketName, string objectName, string uploadId)
    {
        logger.Information("Aborting multipart upload for {ObjectName} in {BucketName}", objectName, bucketName);

        AbortMultipartUploadRequest request = new()
        {
            BucketName = bucketName,
            Key = objectName,
            UploadId = uploadId
        };

        await client.AbortMultipartUploadAsync(request);
    }
}
