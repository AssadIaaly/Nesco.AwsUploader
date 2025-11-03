using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace Nesco.AwsUploader.Components;

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
    /// Success or informational message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Error message if upload failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static AwsUploadResult SuccessResult(string url, string? message = null)
    {
        return new AwsUploadResult
        {
            Success = true,
            Url = url,
            Message = message ?? "Upload successful"
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
/// Helper class for client-side AWS S3 upload operations using HTTP API
/// </summary>
public static class AwsUploadApiHelper
{
    /// <summary>
    /// Upload file to S3 via server API endpoint
    /// </summary>
    /// <param name="http">HttpClient instance</param>
    /// <param name="file">Browser file to upload</param>
    /// <param name="folder">Optional folder path in S3</param>
    /// <param name="customFileName">Optional custom filename</param>
    /// <param name="preserveFilename">If true, preserve original filename; if false, generate GUID</param>
    /// <param name="maxFileSize">Maximum file size in bytes (default: 5MB)</param>
    /// <param name="serverEndpoint">Server upload endpoint (default: /api/aws-upload/server)</param>
    /// <returns>AwsUploadResult with upload status and details</returns>
    public static async Task<AwsUploadResult> UploadViaServerAsync(
        HttpClient http,
        IBrowserFile file,
        string? folder = null,
        string? customFileName = null,
        bool preserveFilename = true,
        long maxFileSize = 5242880,
        string serverEndpoint = "/api/aws-upload/server")
    {
        try
        {
            // Validate file size
            if (file.Size > maxFileSize)
            {
                return AwsUploadResult.FailureResult(
                    $"File size ({file.Size / 1024.0 / 1024.0:F2} MB) exceeds maximum allowed size ({maxFileSize / 1024.0 / 1024.0:F2} MB)");
            }

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: maxFileSize));
            var contentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType;
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            content.Add(fileContent, "file", file.Name);

            // Add optional parameters
            if (!string.IsNullOrEmpty(folder))
            {
                content.Add(new StringContent(folder), "folder");
            }
            if (!string.IsNullOrEmpty(customFileName))
            {
                content.Add(new StringContent(customFileName), "customFileName");
            }
            content.Add(new StringContent(preserveFilename.ToString()), "preserveFilename");

            var response = await http.PostAsync(serverEndpoint, content);
            var result = await response.Content.ReadFromJsonAsync<UploadResponse>();

            if (response.IsSuccessStatusCode && result != null)
            {
                return AwsUploadResult.SuccessResult(result.Url!, result.Message);
            }
            else
            {
                return AwsUploadResult.FailureResult(result?.Error ?? "Upload failed");
            }
        }
        catch (Exception ex)
        {
            return AwsUploadResult.FailureResult(ex.Message);
        }
    }

    /// <summary>
    /// Upload file to S3 using presigned URL
    /// </summary>
    /// <param name="http">HttpClient instance</param>
    /// <param name="file">Browser file to upload</param>
    /// <param name="folder">Optional folder path in S3</param>
    /// <param name="customFileName">Optional custom filename</param>
    /// <param name="preserveFilename">If true, preserve original filename; if false, generate GUID</param>
    /// <param name="maxFileSize">Maximum file size in bytes (default: 5MB)</param>
    /// <param name="presignedEndpoint">Presigned URL endpoint (default: /api/aws-upload/presigned-url)</param>
    /// <returns>AwsUploadResult with upload status and details</returns>
    public static async Task<AwsUploadResult> UploadViaPresignedUrlAsync(
        HttpClient http,
        IBrowserFile file,
        string? folder = null,
        string? customFileName = null,
        bool preserveFilename = true,
        long maxFileSize = 5242880,
        string presignedEndpoint = "/api/aws-upload/presigned-url")
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

            // Step 1: Get presigned URL from server
            var presignedRequest = new
            {
                FileName = file.Name,
                ContentType = contentType,
                Folder = folder,
                CustomFileName = customFileName,
                PreserveFilename = (bool?)preserveFilename
            };

            var response = await http.PostAsJsonAsync(presignedEndpoint, presignedRequest);
            var result = await response.Content.ReadFromJsonAsync<PresignedUrlResponse>();

            if (!response.IsSuccessStatusCode || result == null)
            {
                return AwsUploadResult.FailureResult(result?.Error ?? "Failed to get presigned URL");
            }

            // Step 2: Upload directly to S3 using presigned URL
            // Note: S3 presigned URLs don't support chunked transfer encoding,
            // so we need to read the file into memory first
            using var httpClient = new HttpClient();
            using var fileStream = file.OpenReadStream(maxAllowedSize: maxFileSize);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var streamContent = new StreamContent(memoryStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            streamContent.Headers.ContentLength = memoryStream.Length;

            var uploadResponse = await httpClient.PutAsync(result.PresignedUrl, streamContent);

            if (uploadResponse.IsSuccessStatusCode)
            {
                var publicUrl = result.PublicUrl ?? result.PresignedUrl!.Split('?')[0];
                return AwsUploadResult.SuccessResult(publicUrl, "File uploaded successfully");
            }
            else
            {
                var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                return AwsUploadResult.FailureResult(
                    $"Failed to upload to S3. Status: {uploadResponse.StatusCode}. Error: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            return AwsUploadResult.FailureResult(ex.Message);
        }
    }

    private class UploadResponse
    {
        public string? Url { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }

    private class PresignedUrlResponse
    {
        public string? PresignedUrl { get; set; }
        public string? Key { get; set; }
        public string? PublicUrl { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }
}
