using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace EventEase.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly string _connectionString;
        private readonly ILogger<BlobStorageService> _logger;

        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
        {
            _connectionString = configuration.GetConnectionString("AzureStorage")
                ?? "UseDevelopmentStorage=true";
            _logger = logger;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string containerName = "venue-images")
        {
            var containerClient = new BlobContainerClient(_connectionString, containerName);

            // Create the container if it doesn't exist and set public access
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Generate a unique blob name to avoid collisions
            var extension = Path.GetExtension(file.FileName);
            var blobName = $"{Guid.NewGuid()}{extension}";
            var blobClient = containerClient.GetBlobClient(blobName);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = file.ContentType
            };

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            _logger.LogInformation("Image uploaded to blob storage: {BlobUri}", blobClient.Uri.ToString());
            return blobClient.Uri.ToString();
        }

        public async Task DeleteImageAsync(string blobUrl, string containerName = "venue-images")
        {
            if (string.IsNullOrWhiteSpace(blobUrl))
                return;

            try
            {
                var uri = new Uri(blobUrl);
                // Extract just the filename part (last segment of path)
                var blobName = Path.GetFileName(uri.LocalPath);
                if (string.IsNullOrEmpty(blobName)) return;

                var containerClient = new BlobContainerClient(_connectionString, containerName);
                var blobClient = containerClient.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);

                _logger.LogInformation("Blob deleted: {BlobName}", blobName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete blob: {BlobUrl}", blobUrl);
            }
        }
    }
}
