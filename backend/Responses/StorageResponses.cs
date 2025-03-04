using System.ComponentModel;

namespace backend.Responses
{
    public struct STORAGE_RESPONSE_TYPE
    {
        public const string MESSAGE = "message";
        public const string UPLOAD = "upload";
        public const string FILE = "file";
        public const string CONTAINER = "container";
        public const string PAGE = "page";
    }

    public class StorageResponse
    {
        public string Message { get; }
        public string ResponseType { get; }

        public StorageResponse(string message, string responseType = STORAGE_RESPONSE_TYPE.MESSAGE)
        {
            Message = message;
            ResponseType = responseType;
        }
    }

    /// <summary>
    /// Result of a file upload operation
    /// </summary>
    public class FileUploadResult : StorageResponse
    {
        /// <summary>
        /// Name of the uploaded file
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name of the container it is uploaded in
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        public long Size { get; set; }

        public FileUploadResult(string id, string fileType, long size) : base("File uploaded successfully", STORAGE_RESPONSE_TYPE.UPLOAD)
        {
            this.Id = id;
            this.FileType = fileType;
            this.Size = size;
        }
    }

    public class FileResponse : StorageResponse
    {
        public string Id { get; set; }

        public string FileType { get; set; }

        public FileResponse(string message, string id, string fileType) : base(message, STORAGE_RESPONSE_TYPE.FILE)
        {
            this.Id = id;
            this.FileType = fileType;
        }
    }

    public class ContainerResponse : StorageResponse
    {
        public string ContainerName { get; set; }

        public ContainerResponse(string message, string containerName) : base(message, STORAGE_RESPONSE_TYPE.CONTAINER)
        {
            this.ContainerName = containerName;
        }
    }

    public class PageResponse : StorageResponse
    {
        public string ContainerName { get; set; }
        public string? ContinuationToken { get; set; }
        public string[] Files { get; set; }
        public int PageSize { get; set; }

        public PageResponse(string message, string containerName, string? continuationToken, string[] files, int pageSize) : base(message, STORAGE_RESPONSE_TYPE.PAGE)
        {
            this.ContainerName = containerName;
            this.ContinuationToken = continuationToken;
            this.Files = files;
            this.PageSize = pageSize;
        }
    }
}
