namespace Nesco.AwsUploader.Server.Configuration;

/// <summary>
/// Configuration options for AWS S3 integration
/// </summary>
public class AwsS3Options
{
    /// <summary>
    /// AWS Region (e.g., "us-east-1", "eu-north-1")
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// S3 Bucket name
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// AWS Access Key (optional if using IAM roles or environment variables)
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// AWS Secret Key (optional if using IAM roles or environment variables)
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Folder prefix for uploaded files (default: "uploads")
    /// </summary>
    public string FolderPrefix { get; set; } = "uploads";

    /// <summary>
    /// Default expiration time for presigned URLs in minutes (default: 15)
    /// </summary>
    public int PresignedUrlExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Whether to use public URLs (requires bucket to have public read access)
    /// If false, uses presigned URLs with expiration
    /// </summary>
    public bool UsePublicUrls { get; set; } = true;
}
