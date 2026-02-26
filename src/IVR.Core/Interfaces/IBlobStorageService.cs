namespace IVR.Core.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAudioAsync(Stream audioStream, string fileName, string contentType);
    Task<Stream?> DownloadAudioAsync(string blobName);
    Task DeleteAudioAsync(string blobName);
    Task<string> GetAudioUrlAsync(string blobName, TimeSpan? expiry = null);
    Task<List<string>> ListAudioFilesAsync(string? prefix = null);
}
