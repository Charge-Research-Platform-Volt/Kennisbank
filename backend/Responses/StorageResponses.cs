namespace backend.Responses
{
    public enum STORAGE_RESPONSE_TYPE { MESSAGE, UPLOAD, FILE, CONTAINER, PAGE }
    public class StorageResponse
    {
        public string Message { get; }
        public STORAGE_RESPONSE_TYPE ResponseType { get; }

        public StorageResponse(string message, STORAGE_RESPONSE_TYPE responseType = STORAGE_RESPONSE_TYPE.MESSAGE)
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
        public string FileName { get; set; }

        /// <summary>
        /// Name of the container it is uploaded in
        /// </summary>
        public string ContainerName { get; set; }

        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        public long Size { get; set; }

        public FileUploadResult(string fileName, string containerName, long size) : base("File uploaded successfully", STORAGE_RESPONSE_TYPE.UPLOAD)
        {
            this.FileName = fileName;
            this.ContainerName = containerName;
            this.Size = size;
        }
    }

    public class FileResponse : StorageResponse
    {
        public string FileName { get; set; }

        public string ContainerName { get; set; }

        public FileResponse(string message, string fileName, string containerName) : base(message, STORAGE_RESPONSE_TYPE.FILE)
        {
            this.FileName = fileName;
            this.ContainerName = containerName;
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
