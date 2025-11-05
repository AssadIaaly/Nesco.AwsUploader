using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nesco.AwsUploader.Server.Configuration;

namespace Nesco.AwsUploader.Server.Services;

/// <summary>
/// Implementation of AWS S3 upload service
/// </summary>
public class AwsS3Service : IAwsS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsS3Options _options;
    private readonly ILogger<AwsS3Service> _logger;

    public AwsS3Service(IAmazonS3 s3Client, IOptions<AwsS3Options> options, ILogger<AwsS3Service> logger)
    {
        _s3Client = s3Client;
        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrEmpty(_options.BucketName))
        {
            throw new InvalidOperationException("AWS S3 BucketName is not configured");
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? folder = null, string? customFileName = null, bool preserveFilename = true)
    {
        try
        {
            var key = GenerateS3Key(fileName, folder, customFileName, preserveFilename);

            var putRequest = new PutObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = contentType
            };

            await _s3Client.PutObjectAsync(putRequest);

            _logger.LogInformation("File uploaded successfully to S3: {Key}", key);

            // Return appropriate URL based on configuration
            if (_options.UsePublicUrls)
            {
                return await GetPublicUrlAsync(key);
            }
            else
            {
                return await GeneratePresignedDownloadUrlAsync(key, _options.PresignedUrlExpirationMinutes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to S3");
            throw;
        }
    }

    public async Task<(string uploadUrl, string key)> GeneratePresignedUploadUrlAsync(string fileName, string contentType, string? folder = null, string? customFileName = null, bool preserveFilename = true)
    {
        try
        {
            var key = GenerateS3Key(fileName, folder, customFileName, preserveFilename);

            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(_options.PresignedUrlExpirationMinutes),
                ContentType = contentType
            };

            var url = await _s3Client.GetPreSignedURLAsync(request);

            _logger.LogInformation("Generated presigned upload URL for: {Key}", key);

            return (url, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL");
            throw;
        }
    }

    public async Task<string> GeneratePresignedDownloadUrlAsync(string key, int expirationMinutes)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _options.BucketName,
                Key = key,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
            };

            var url = await _s3Client.GetPreSignedURLAsync(request);

            _logger.LogInformation("Generated presigned download URL for: {Key}", key);

            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned download URL");
            throw;
        }
    }

    public async Task<string> GetPublicUrlAsync(string key)
    {
        try
        {
            var region = await _s3Client.GetBucketLocationAsync(_options.BucketName);
            var regionEndpoint = string.IsNullOrEmpty(region.Location.Value) ? "us-east-1" : region.Location.Value;
            return $"https://{_options.BucketName}.s3.{regionEndpoint}.amazonaws.com/{key}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public URL");
            throw;
        }
    }

    private string GenerateS3Key(string fileName, string? folder = null, string? customFileName = null, bool preserveFilename = true)
    {
        // Determine the final filename
        string finalFileName;
        if (!string.IsNullOrEmpty(customFileName))
        {
            // Use provided custom filename
            finalFileName = customFileName;
        }
        else if (preserveFilename)
        {
            // Preserve original filename
            finalFileName = fileName;
        }
        else
        {
            // Generate GUID filename with original extension
            var extension = Path.GetExtension(fileName);
            finalFileName = $"{Guid.NewGuid()}{extension}";
        }

        // Sanitize the final filename
        finalFileName = SanitizeFileName(finalFileName);

        // Build the folder path: FolderPrefix is ALWAYS the root
        var pathParts = new List<string>();

        // Add the configured FolderPrefix (root public folder) if it exists
        if (!string.IsNullOrEmpty(_options.FolderPrefix))
        {
            pathParts.Add(_options.FolderPrefix.Trim('/'));
        }

        // Add the provided folder parameter as a subfolder under FolderPrefix
        if (!string.IsNullOrEmpty(folder))
        {
            pathParts.Add(folder.Trim('/'));
        }

        // Construct the full S3 key
        if (pathParts.Count > 0)
        {
            var folderPath = string.Join("/", pathParts);
            return $"{folderPath}/{finalFileName}";
        }
        else
        {
            return finalFileName;
        }
    }

    public async Task<bool> DeleteFileAsync(string key)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(request);
            _logger.LogInformation("File deleted successfully from S3: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file from S3: {Key}", key);
            return false;
        }
    }

    public async Task<(bool Success, string? Url)> CopyFileAsync(string sourceKey, string destinationKey)
    {
        try
        {
            _logger.LogInformation("Attempting to copy {SourceKey} to {DestinationKey}", sourceKey, destinationKey);

            var request = new CopyObjectRequest
            {
                SourceBucket = _options.BucketName,
                SourceKey = sourceKey,
                DestinationBucket = _options.BucketName,
                DestinationKey = destinationKey
            };

            var response = await _s3Client.CopyObjectAsync(request);
            _logger.LogInformation("Copy successful. HTTP Status: {StatusCode}", response.HttpStatusCode);

            // Generate URL for the copied file (consistent with UploadFileAsync)
            string url;
            if (_options.UsePublicUrls)
            {
                url = await GetPublicUrlAsync(destinationKey);
            }
            else
            {
                url = await GeneratePresignedDownloadUrlAsync(destinationKey, _options.PresignedUrlExpirationMinutes);
            }

            _logger.LogInformation("Generated URL for copied file: {Url}", url);
            return (true, url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error copying file in S3 from {SourceKey} to {DestinationKey}", sourceKey, destinationKey);
            return (false, null);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove or replace invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        foreach (var c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }
        return fileName;
    }
}
