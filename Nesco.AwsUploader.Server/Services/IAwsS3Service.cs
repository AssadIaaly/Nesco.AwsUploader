namespace Nesco.AwsUploader.Server.Services;

/// <summary>
/// Service for uploading files to AWS S3
/// </summary>
public interface IAwsS3Service
{
    /// <summary>
    /// Upload file to S3 bucket (server-side upload)
    /// </summary>
    /// <param name="fileStream">File stream to upload</param>
    /// <param name="fileName">Name of the file</param>
    /// <param name="contentType">Content type of the file</param>
    /// <param name="folder">Optional folder path in S3</param>
    /// <param name="customFileName">Optional custom filename to use</param>
    /// <param name="preserveFilename">If true, preserve original filename; if false, generate GUID filename</param>
    /// <returns>URL to access the uploaded file</returns>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string? folder = null, string? customFileName = null, bool preserveFilename = true);

    /// <summary>
    /// Generate a presigned URL for client-side upload
    /// </summary>
    /// <param name="fileName">Name of the file</param>
    /// <param name="contentType">Content type of the file</param>
    /// <param name="folder">Optional folder path in S3</param>
    /// <param name="customFileName">Optional custom filename to use</param>
    /// <param name="preserveFilename">If true, preserve original filename; if false, generate GUID filename</param>
    /// <returns>Tuple of (presigned upload URL, S3 key)</returns>
    Task<(string uploadUrl, string key)> GeneratePresignedUploadUrlAsync(string fileName, string contentType, string? folder = null, string? customFileName = null, bool preserveFilename = true);

    /// <summary>
    /// Generate a presigned URL for downloading/viewing a file
    /// </summary>
    /// <param name="key">S3 object key</param>
    /// <param name="expirationMinutes">Expiration time in minutes</param>
    /// <returns>Presigned download URL</returns>
    Task<string> GeneratePresignedDownloadUrlAsync(string key, int expirationMinutes);

    /// <summary>
    /// Get the permanent public URL for an S3 object
    /// </summary>
    /// <param name="key">S3 object key</param>
    /// <returns>Public URL</returns>
    Task<string> GetPublicUrlAsync(string key);
}
