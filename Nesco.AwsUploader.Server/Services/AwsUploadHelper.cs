using Microsoft.AspNetCore.Components.Forms;

namespace Nesco.AwsUploader.Server.Services;

/// <summary>
/// Result of an AWS upload operation
/// </summary>
public class AwsUploadResult
{
    /// <summary>
    /// Indicates if the upload was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The URL to access the uploaded file
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Error message if upload failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static AwsUploadResult SuccessResult(string url)
    {
        return new AwsUploadResult
        {
            Success = true,
            Url = url
        };
    }

    /// <summary>
    /// Creates a failure result
    /// </summary>
    public static AwsUploadResult FailureResult(string errorMessage)
    {
        return new AwsUploadResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Result of a presigned URL generation operation
/// </summary>
public class AwsPresignedUrlResult
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The presigned URL for uploading
    /// </summary>
    public string? PresignedUrl { get; set; }

    /// <summary>
    /// The public URL to access the file after upload
    /// </summary>
    public string? PublicUrl { get; set; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static AwsPresignedUrlResult SuccessResult(string presignedUrl, string publicUrl)
    {
        return new AwsPresignedUrlResult
        {
            Success = true,
            PresignedUrl = presignedUrl,
            PublicUrl = publicUrl
        };
    }

    /// <summary>
    /// Creates a failure result
    /// </summary>
    public static AwsPresignedUrlResult FailureResult(string errorMessage)
    {
        return new AwsPresignedUrlResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Helper class for server-side AWS S3 upload operations
/// </summary>
public static class AwsUploadHelper
{
    /// <summary>
    /// Upload file to S3 using IAwsS3Service (server-side upload)
    /// </summary>
    /// <param name="s3Service">AWS S3 Service instance</param>
    /// <param name="file">Browser file to upload</param>
    /// <param name="folder">Optional folder path in S3</param>
    /// <param name="customFileName">Optional custom filename</param>
    /// <param name="preserveFilename">If true, preserve original filename; if false, generate GUID</param>
    /// <param name="maxFileSize">Maximum file size in bytes (default: 5MB)</param>
    /// <returns>AwsUploadResult with upload status and details</returns>
    public static async Task<AwsUploadResult> UploadFileAsync(
        IAwsS3Service s3Service,
        IBrowserFile file,
        string? folder = null,
        string? customFileName = null,
        bool preserveFilename = true,
        long maxFileSize = 5242880)
    {
        try
        {
            // Validate file size
            if (file.Size > maxFileSize)
            {
                return AwsUploadResult.FailureResult(
                    $"File size ({file.Size / 1024.0 / 1024.0:F2} MB) exceeds maximum allowed size ({maxFileSize / 1024.0 / 1024.0:F2} MB)");
            }

            var contentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType;

            using var stream = file.OpenReadStream(maxAllowedSize: maxFileSize);

            var url = await s3Service.UploadFileAsync(
                stream,
                file.Name,
                contentType,
                folder,
                customFileName,
                preserveFilename);

            return AwsUploadResult.SuccessResult(url);
        }
        catch (Exception ex)
        {
            return AwsUploadResult.FailureResult(ex.Message);
        }
    }

    /// <summary>
    /// Generate presigned URL for client-side upload
    /// </summary>
    /// <param name="s3Service">AWS S3 Service instance</param>
    /// <param name="file">Browser file to get presigned URL for</param>
    /// <param name="folder">Optional folder path in S3</param>
    /// <param name="customFileName">Optional custom filename</param>
    /// <param name="preserveFilename">If true, preserve original filename; if false, generate GUID</param>
    /// <param name="maxFileSize">Maximum file size in bytes (default: 5MB)</param>
    /// <returns>AwsPresignedUrlResult with presigned URL and public URL</returns>
    public static async Task<AwsPresignedUrlResult> GeneratePresignedUrlAsync(
        IAwsS3Service s3Service,
        IBrowserFile file,
        string? folder = null,
        string? customFileName = null,
        bool preserveFilename = true,
        long maxFileSize = 5242880)
    {
        try
        {
            // Validate file size
            if (file.Size > maxFileSize)
            {
                return AwsPresignedUrlResult.FailureResult(
                    $"File size ({file.Size / 1024.0 / 1024.0:F2} MB) exceeds maximum allowed size ({maxFileSize / 1024.0 / 1024.0:F2} MB)");
            }

            var contentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType;

            var (uploadUrl, key) = await s3Service.GeneratePresignedUploadUrlAsync(
                file.Name,
                contentType,
                folder,
                customFileName,
                preserveFilename);

            var publicUrl = await s3Service.GetPublicUrlAsync(key);

            return AwsPresignedUrlResult.SuccessResult(uploadUrl, publicUrl);
        }
        catch (Exception ex)
        {
            return AwsPresignedUrlResult.FailureResult(ex.Message);
        }
    }
}
