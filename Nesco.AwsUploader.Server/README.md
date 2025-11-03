# Nesco.AwsUploader.Server

A comprehensive server-side library for AWS S3 file uploads in ASP.NET Core applications. This package provides services, API endpoints, and helper utilities for uploading files to AWS S3 with support for both direct uploads and presigned URL generation.

## Features

- **IAwsS3Service**: Dependency injection-ready service for S3 operations
- **Ready-to-use API Controllers**: Pre-built endpoints for file uploads
- **Presigned URL Support**: Generate secure, time-limited URLs for client-side uploads
- **Helper Utilities**: Simplified upload methods for Blazor applications
- **Flexible File Naming**: Support for custom filenames, preserved filenames, or GUID-based naming
- **Folder Organization**: Organize uploads into folders within your S3 bucket

## Installation

```bash
dotnet add package Nesco.AwsUploader.Server
```

## Prerequisites

You need an AWS account with:
- An S3 bucket created
- IAM credentials (Access Key ID and Secret Access Key) with S3 permissions

## Configuration

### 1. Add AWS Configuration to appsettings.json

```json
{
  "AWS": {
    "AccessKeyId": "your-access-key-id",
    "SecretAccessKey": "your-secret-access-key",
    "BucketName": "your-bucket-name",
    "Region": "us-east-1",
    "FolderPrefix": "uploads"
  }
}
```

**Configuration Options:**
- `AccessKeyId`: Your AWS IAM access key
- `SecretAccessKey`: Your AWS IAM secret key
- `BucketName`: The name of your S3 bucket
- `Region`: AWS region (e.g., us-east-1, eu-west-1)
- `FolderPrefix`: Root folder for all uploads (default: "uploads")

### 2. Register Services in Program.cs

```csharp
using Nesco.AwsUploader.Server;

var builder = WebApplication.CreateBuilder(args);

// Add AWS S3 Uploader services
builder.Services.AddAwsS3Uploader(options =>
{
    options.AccessKeyId = builder.Configuration["AWS:AccessKeyId"]!;
    options.SecretAccessKey = builder.Configuration["AWS:SecretAccessKey"]!;
    options.BucketName = builder.Configuration["AWS:BucketName"]!;
    options.Region = builder.Configuration["AWS:Region"]!;
    options.FolderPrefix = builder.Configuration["AWS:FolderPrefix"];
});

// Add controllers for API endpoints
builder.Services.AddControllers();

var app = builder.Build();

// Map controller endpoints
app.MapControllers();

app.Run();
```

## Usage

### Option 1: Using IAwsS3Service Directly (Recommended for Server-Side)

Inject `IAwsS3Service` into your Razor components or services:

```csharp
@inject IAwsS3Service AwsS3Service

@code {
    private async Task UploadFile(IBrowserFile file)
    {
        var contentType = string.IsNullOrEmpty(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        using var stream = file.OpenReadStream(maxAllowedSize: 5242880); // 5MB

        // Upload with original filename
        var url = await AwsS3Service.UploadFileAsync(
            stream,
            file.Name,
            contentType,
            folder: "documents",           // Optional: subfolder
            customFileName: null,          // Optional: custom name
            preserveFilename: true         // Keep original filename
        );

        Console.WriteLine($"File uploaded: {url}");
    }
}
```

### Option 2: Using AwsUploadHelper (Simplified for Blazor)

The helper class provides a simplified API with automatic error handling:

```csharp
using Nesco.AwsUploader.Server.Services;

@inject IAwsS3Service AwsS3Service

@code {
    private async Task UploadFile(IBrowserFile file)
    {
        // Simple upload with result handling
        var result = await AwsUploadHelper.UploadFileAsync(
            AwsS3Service,
            file,
            folder: "invoices",
            customFileName: null,
            preserveFilename: true,
            maxFileSize: 5242880 // 5MB
        );

        if (result.Success)
        {
            Console.WriteLine($"Upload successful! URL: {result.Url}");
        }
        else
        {
            Console.WriteLine($"Upload failed: {result.ErrorMessage}");
        }
    }
}
```

### Option 3: Generate Presigned URLs for Client-Side Upload

Allow clients to upload directly to S3 without routing through your server:

```csharp
@inject IAwsS3Service AwsS3Service

@code {
    private async Task GetPresignedUrl(IBrowserFile file)
    {
        var result = await AwsUploadHelper.GeneratePresignedUrlAsync(
            AwsS3Service,
            file,
            folder: "temp",
            preserveFilename: true,
            maxFileSize: 5242880
        );

        if (result.Success)
        {
            // Give the presigned URL to the client
            var presignedUrl = result.PresignedUrl;
            var publicUrl = result.PublicUrl;

            // Client can now PUT the file directly to presignedUrl
            // After upload, the file will be accessible at publicUrl
        }
    }
}
```

### Option 4: Using API Endpoints

The package includes built-in API controllers at:

#### Server Upload Endpoint
```
POST /api/aws-upload/server
Content-Type: multipart/form-data

Parameters:
- file: The file to upload (required)
- folder: Subfolder path (optional)
- customFileName: Custom filename (optional)
- preserveFilename: true/false (optional, default: true)

Response:
{
  "url": "https://your-bucket.s3.amazonaws.com/uploads/folder/file.pdf",
  "message": "File uploaded successfully"
}
```

#### Presigned URL Endpoint
```
POST /api/aws-upload/presigned-url
Content-Type: application/json

Body:
{
  "fileName": "document.pdf",
  "contentType": "application/pdf",
  "folder": "documents",
  "customFileName": null,
  "preserveFilename": true
}

Response:
{
  "presignedUrl": "https://your-bucket.s3.amazonaws.com/...",
  "key": "uploads/documents/document.pdf",
  "publicUrl": "https://your-bucket.s3.amazonaws.com/uploads/documents/document.pdf",
  "message": "Presigned URL generated successfully"
}
```

## File Naming Logic

The package supports three file naming strategies:

1. **Custom Filename** (Priority 1):
   ```csharp
   customFileName: "invoice-2024.pdf"
   // Result: uploads/folder/invoice-2024.pdf
   ```

2. **Preserve Original** (Priority 2):
   ```csharp
   preserveFilename: true
   // Result: uploads/folder/original-name.pdf
   ```

3. **GUID-based** (Priority 3):
   ```csharp
   preserveFilename: false
   // Result: uploads/folder/a1b2c3d4-e5f6-7890-abcd-ef1234567890.pdf
   ```

## Folder Structure

All uploads are organized under the root `FolderPrefix` (default: "uploads"):

```
your-bucket/
└── uploads/                    (FolderPrefix)
    ├── documents/              (Optional subfolder)
    │   └── file1.pdf
    ├── invoices/               (Optional subfolder)
    │   └── file2.pdf
    └── file3.pdf               (No subfolder)
```

## API Reference

### IAwsS3Service Methods

```csharp
public interface IAwsS3Service
{
    // Upload a file directly to S3
    Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string? folder = null,
        string? customFileName = null,
        bool preserveFilename = true
    );

    // Generate a presigned URL for client-side upload
    Task<(string uploadUrl, string key)> GeneratePresignedUploadUrlAsync(
        string fileName,
        string contentType,
        string? folder = null,
        string? customFileName = null,
        bool preserveFilename = true,
        int expirationMinutes = 60
    );

    // Get public URL for a file
    Task<string> GetPublicUrlAsync(string key);

    // Delete a file from S3
    Task DeleteFileAsync(string key);
}
```

### AwsUploadHelper Methods

```csharp
public static class AwsUploadHelper
{
    // Upload file with simplified error handling
    public static Task<AwsUploadResult> UploadFileAsync(
        IAwsS3Service s3Service,
        IBrowserFile file,
        string? folder = null,
        string? customFileName = null,
        bool preserveFilename = true,
        long maxFileSize = 5242880
    );

    // Generate presigned URL with simplified error handling
    public static Task<AwsPresignedUrlResult> GeneratePresignedUrlAsync(
        IAwsS3Service s3Service,
        IBrowserFile file,
        string? folder = null,
        string? customFileName = null,
        bool preserveFilename = true,
        long maxFileSize = 5242880
    );
}
```

## Advanced Scenarios

### Custom File Validation

```csharp
private async Task<AwsUploadResult> UploadWithValidation(IBrowserFile file)
{
    // Validate file size
    const long maxSize = 10 * 1024 * 1024; // 10MB
    if (file.Size > maxSize)
    {
        return AwsUploadResult.FailureResult("File too large");
    }

    // Validate file type
    var allowedTypes = new[] { ".pdf", ".jpg", ".png" };
    var extension = Path.GetExtension(file.Name).ToLowerInvariant();
    if (!allowedTypes.Contains(extension))
    {
        return AwsUploadResult.FailureResult("Invalid file type");
    }

    // Upload
    return await AwsUploadHelper.UploadFileAsync(
        AwsS3Service,
        file,
        folder: "validated",
        preserveFilename: true,
        maxFileSize: maxSize
    );
}
```

### Generating Time-Based Filenames

```csharp
private async Task UploadWithTimestamp(IBrowserFile file)
{
    var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
    var extension = Path.GetExtension(file.Name);
    var customName = $"{timestamp}{extension}";

    var result = await AwsUploadHelper.UploadFileAsync(
        AwsS3Service,
        file,
        folder: "timestamped",
        customFileName: customName,
        preserveFilename: false // customFileName takes priority anyway
    );
}
```

### Delete Uploaded Files

```csharp
@inject IAwsS3Service AwsS3Service

@code {
    private async Task DeleteFile(string fileUrl)
    {
        // Extract key from URL
        // Example: https://bucket.s3.amazonaws.com/uploads/folder/file.pdf
        // Key: uploads/folder/file.pdf

        var uri = new Uri(fileUrl);
        var key = uri.AbsolutePath.TrimStart('/');

        await AwsS3Service.DeleteFileAsync(key);
    }
}
```

## Security Best Practices

1. **Never commit AWS credentials** to source control
2. **Use environment variables** or Azure Key Vault for production
3. **Limit IAM permissions** to only required S3 operations
4. **Set appropriate CORS rules** on your S3 bucket for presigned URLs
5. **Validate file types and sizes** before upload
6. **Use presigned URLs** with short expiration times (default: 60 minutes)

## S3 Bucket CORS Configuration

For presigned URL uploads to work from web browsers, configure CORS on your S3 bucket:

```json
[
    {
        "AllowedHeaders": ["*"],
        "AllowedMethods": ["PUT", "POST", "GET"],
        "AllowedOrigins": ["https://yourdomain.com"],
        "ExposeHeaders": ["ETag"]
    }
]
```

## Troubleshooting

### Upload fails with "Access Denied"
- Check IAM credentials have `s3:PutObject` permission
- Verify bucket policy allows uploads
- Ensure bucket name is correct

### Presigned URL upload fails with 403
- Check S3 bucket CORS configuration
- Verify presigned URL hasn't expired
- Ensure Content-Type header matches the one used to generate URL

### Files uploaded to wrong location
- Check `FolderPrefix` in configuration
- Verify `folder` parameter in upload calls
- All files go under `FolderPrefix/folder/filename`

## License

This package is open source and free to use for anyone.

## Support

For issues and questions, please contact support or visit the project repository.
