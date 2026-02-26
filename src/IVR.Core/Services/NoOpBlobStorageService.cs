using IVR.Core.Interfaces;

namespace IVR.Core.Services;

/// <summary>
/// No-op implementation of IBlobStorageService used when Azure Storage credentials are not configured.
/// Returns empty results for all operations.
/// </summary>
public class NoOpBlobStorageService : IBlobStorageService
{
    public Task<string> UploadAudioAsync(Stream audioStream, string fileName, string contentType) => Task.FromResult(string.Empty);
    public Task<Stream?> DownloadAudioAsync(string blobName) => Task.FromResult<Stream?>(null);
    public Task DeleteAudioAsync(string blobName) => Task.CompletedTask;
    public Task<string> GetAudioUrlAsync(string blobName, TimeSpan? expiry = null) => Task.FromResult(string.Empty);
    public Task<List<string>> ListAudioFilesAsync(string? prefix = null) => Task.FromResult(new List<string>());
}
