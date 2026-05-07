namespace EventEase.Services
{
    public interface IBlobStorageService
    {
        Task<string> UploadImageAsync(IFormFile file, string containerName = "venue-images");
        Task DeleteImageAsync(string blobUrl, string containerName = "venue-images");
    }
}
