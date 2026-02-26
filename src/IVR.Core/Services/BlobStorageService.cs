using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using IVR.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace IVR.Core.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageService> _logger;
    private const string ContainerName = "ivr-prompts";

    public BlobStorageService(BlobServiceClient blobServiceClient, ILogger<BlobStorageService> logger)
    {
        _containerClient = blobServiceClient.GetBlobContainerClient(ContainerName);
        _logger = logger;
    }

    public async Task<string> UploadAudioAsync(Stream audioStream, string fileName, string contentType)
    {
        await _containerClient.CreateIfNotExistsAsync(PublicAccessType.None);

        var blobName = $"{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid()}/{fileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        await blobClient.UploadAsync(audioStream, options);
        _logger.LogInformation("Uploaded audio file {BlobName}", blobName);

        return blobName;
    }

    public async Task<Stream?> DownloadAudioAsync(string blobName)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
        {
            _logger.LogWarning("Audio file {BlobName} not found", blobName);
            return null;
        }

        var response = await blobClient.DownloadStreamingAsync();
        return response.Value.Content;
    }

    public async Task DeleteAudioAsync(string blobName)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
        _logger.LogInformation("Deleted audio file {BlobName}", blobName);
    }

    public async Task<string> GetAudioUrlAsync(string blobName, TimeSpan? expiry = null)
    {
        var blobClient = _containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
            throw new FileNotFoundException($"Audio file {blobName} not found");

        if (blobClient.CanGenerateSasUri)
        {
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = ContainerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.Add(expiry ?? TimeSpan.FromHours(1))
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }

        return blobClient.Uri.ToString();
    }

    public async Task<List<string>> ListAudioFilesAsync(string? prefix = null)
    {
        var blobs = new List<string>();

        await foreach (var blob in _containerClient.GetBlobsAsync(prefix: prefix))
        {
            blobs.Add(blob.Name);
        }

        return blobs;
    }
}
