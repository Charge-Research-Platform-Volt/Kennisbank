using Azure.Storage.Blobs;

namespace backend.Data
{
    public class AzureBlob
    {
        private const string connectionString = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://storage:10000/devstoreaccount1;";
        private readonly BlobServiceClient blobService;

        public AzureBlob()
        {
            this.blobService = new BlobServiceClient(connectionString);
        }

        public BlobContainerClient GetContainerClient(string containerName)
        {
            return blobService.GetBlobContainerClient(containerName);
        }

        public async Task<BlobContainerClient> CreateContainerAsync(string containerName)
        {
            BlobContainerClient container = blobService.GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync();
            return container;
        }

        public async Task<bool> UploadBlobAsync(string containerName, string blobName, Stream file, bool overwrite = false)
        {
            BlobContainerClient container = blobService.GetBlobContainerClient(containerName);
            BlobClient blob = container.GetBlobClient(blobName);

            if (await blob.ExistsAsync() && !overwrite)
                return false;
            
            await blob.UploadAsync(file, true);
            return true;
        }

        public async Task<bool> BlobExistsAsync(string containerName, string blobName)
        {
            BlobContainerClient container = blobService.GetBlobContainerClient(containerName);
            BlobClient blob = container.GetBlobClient(blobName);

            return await blob.ExistsAsync();
        }
    }
}
