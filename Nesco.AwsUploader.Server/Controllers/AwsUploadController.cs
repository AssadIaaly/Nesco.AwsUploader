using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nesco.AwsUploader.Server.Services;

namespace Nesco.AwsUploader.Server.Controllers;

/// <summary>
/// API Controller for AWS S3 file uploads
/// </summary>
[ApiController]
[Route("api/aws-upload")]
public class AwsUploadController : ControllerBase
{
    private readonly IAwsS3Service _s3Service;
    private readonly ILogger<AwsUploadController> _logger;

    public AwsUploadController(IAwsS3Service s3Service, ILogger<AwsUploadController> logger)
    {
        _s3Service = s3Service;
        _logger = logger;
    }

    /// <summary>
    /// Version 1: Upload file to server, then server uploads to S3
    /// </summary>
    [HttpPost("server")]
    public async Task<IActionResult> UploadToServer(IFormFile file, [FromForm] string? folder = null, [FromForm] string? customFileName = null, [FromForm] bool preserveFilename = true)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No file provided" });
        }

        try
        {
            var contentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType;

            using var stream = file.OpenReadStream();
            var url = await _s3Service.UploadFileAsync(stream, file.FileName, contentType, folder, customFileName, preserveFilename);

            return Ok(new { url, message = "File uploaded successfully via server" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file via server");
            return StatusCode(500, new { error = "Error uploading file", details = ex.Message });
        }
    }

    /// <summary>
    /// Version 2: Generate presigned URL for direct client upload to S3
    /// </summary>
    [HttpPost("presigned-url")]
    public async Task<IActionResult> GetPresignedUrl([FromBody] PresignedUrlRequest request)
    {
        if (string.IsNullOrEmpty(request.FileName))
        {
            return BadRequest(new { error = "Filename is required" });
        }

        try
        {
            var contentType = string.IsNullOrEmpty(request.ContentType) ? "application/octet-stream" : request.ContentType;
            var (presignedUrl, key) = await _s3Service.GeneratePresignedUploadUrlAsync(
                request.FileName,
                contentType,
                request.Folder,
                request.CustomFileName,
                request.PreserveFilename ?? true);

            // Also return the public URL for the uploaded file
            var publicUrl = await _s3Service.GetPublicUrlAsync(key);

            return Ok(new { presignedUrl, key, publicUrl, message = "Presigned URL generated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL");
            return StatusCode(500, new { error = "Error generating presigned URL", details = ex.Message });
        }
    }

    /// <summary>
    /// Get download URL for an uploaded file
    /// </summary>
    [HttpPost("download-url")]
    public async Task<IActionResult> GetDownloadUrl([FromBody] DownloadUrlRequest request)
    {
        if (string.IsNullOrEmpty(request.Key))
        {
            return BadRequest(new { error = "Key is required" });
        }

        try
        {
            var url = await _s3Service.GeneratePresignedDownloadUrlAsync(request.Key, request.ExpirationMinutes ?? 60 * 24 * 7);
            return Ok(new { downloadUrl = url, message = "Download URL generated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating download URL");
            return StatusCode(500, new { error = "Error generating download URL", details = ex.Message });
        }
    }

    /// <summary>
    /// Get public URL for an uploaded file
    /// </summary>
    [HttpPost("public-url")]
    public async Task<IActionResult> GetPublicUrl([FromBody] PublicUrlRequest request)
    {
        if (string.IsNullOrEmpty(request.Key))
        {
            return BadRequest(new { error = "Key is required" });
        }

        try
        {
            var url = await _s3Service.GetPublicUrlAsync(request.Key);
            return Ok(new { publicUrl = url, message = "Public URL retrieved successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public URL");
            return StatusCode(500, new { error = "Error getting public URL", details = ex.Message });
        }
    }
}

public record PresignedUrlRequest(string FileName, string? ContentType, string? Folder = null, string? CustomFileName = null, bool? PreserveFilename = true);
public record DownloadUrlRequest(string Key, int? ExpirationMinutes);
public record PublicUrlRequest(string Key);
