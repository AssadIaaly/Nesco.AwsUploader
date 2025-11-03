# Nesco.AwsUploader.Components

Pre-built Blazor components for AWS S3 file uploads with support for both server-side and client-side upload methods. This package provides ready-to-use UI components and helper utilities for seamless S3 integration in Blazor applications.

## Features

- **AwsUploadButton**: Simple, ready-to-use upload button component
- **AdvancedAwsUploadButton**: Feature-rich component with version selector and detailed controls
- **API Helper Utilities**: Programmatic upload methods using HttpClient
- **Dual Upload Modes**: Server upload and direct presigned URL upload
- **MudBlazor Compatible**: Works seamlessly with MudBlazor design system
- **Event Callbacks**: Rich event system for upload lifecycle management
- **Automatic Upload**: Files upload immediately upon selection
- **Validation**: Built-in file size and type validation

## Installation

```bash
dotnet add package Nesco.AwsUploader.Components
```

## Prerequisites

⚠️ **Important**: This package requires the backend server package to be installed and configured:

```bash
# In your server/backend project
dotnet add package Nesco.AwsUploader.Server
```

Please refer to [Nesco.AwsUploader.Server README](https://www.nuget.org/packages/Nesco.AwsUploader.Server) for backend configuration instructions.

## Quick Start

### 1. Configure Backend (Required)

In your server project's `Program.cs`:

```csharp
using Nesco.AwsUploader.Server;

var builder = WebApplication.CreateBuilder(args);

// Add AWS S3 services
builder.Services.AddAwsS3Uploader(options =>
{
    options.AccessKeyId = builder.Configuration["AWS:AccessKeyId"]!;
    options.SecretAccessKey = builder.Configuration["AWS:SecretAccessKey"]!;
    options.BucketName = builder.Configuration["AWS:BucketName"]!;
    options.Region = builder.Configuration["AWS:Region"]!;
    options.FolderPrefix = "uploads";
});

// Add controllers (required for API endpoints)
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

### 2. Register HttpClient (For WebAssembly/API Usage)

```csharp
// In Program.cs (Client project for WebAssembly)
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
```

### 3. Add Imports

In your `_Imports.razor`:

```razor
@using Nesco.AwsUploader.Components
```

### 4. Use the Component

```razor
<AwsUploadButton
    Version="UploadVersion.ServerUpload"
    ButtonText="Upload File"
    Folder="documents"
    Accept=".pdf,.jpg,.png"
    MaxFileSize="5242880"
    OnUploadComplete="HandleUploadComplete"
    OnUploadError="HandleUploadError" />

@code {
    private void HandleUploadComplete(AwsUploadButton.UploadResult result)
    {
        Console.WriteLine($"Upload successful: {result.Url}");
    }

    private void HandleUploadError(AwsUploadButton.UploadErrorResult error)
    {
        Console.WriteLine($"Upload failed: {error.ErrorMessage}");
    }
}
```

## Components

### AwsUploadButton

A streamlined upload button that automatically uploads files upon selection.

**Features:**
- Automatic upload on file selection
- Progress indicator
- Event-driven callbacks
- File validation
- Customizable styling

**Example:**

```razor
<AwsUploadButton
    Version="UploadVersion.DirectUpload"
    ButtonText="Upload to S3"
    UploadingText="Uploading..."
    ButtonClass="btn-primary"
    Folder="invoices"
    CustomFileName="invoice-2024.pdf"
    PreserveFilename="false"
    Accept=".pdf"
    MaxFileSize="10485760"
    AllowedExtensions='new[] { ".pdf", ".jpg", ".png" }'
    OnUploadComplete="HandleComplete"
    OnUploadError="HandleError"
    OnUploadStart="HandleStart"
    OnFileSelected="HandleFileSelected" />

@code {
    private void HandleComplete(AwsUploadButton.UploadResult result)
    {
        // result.Url - Public URL of uploaded file
        // result.Message - Success message
        // result.FileName - Original filename
        // result.UploadMethod - ServerUpload or DirectUpload
        // result.S3Key - S3 object key (for DirectUpload)
    }

    private void HandleError(AwsUploadButton.UploadErrorResult error)
    {
        // error.ErrorMessage - Error description
        // error.FileName - File that failed
        // error.StatusCode - HTTP status code
        // error.Exception - Full exception (if available)
    }

    private void HandleStart()
    {
        Console.WriteLine("Upload started");
    }

    private void HandleFileSelected(IBrowserFile file)
    {
        Console.WriteLine($"File selected: {file.Name}");
    }
}
```

**Parameters:**

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| Version | UploadVersion | ServerUpload | Upload method (ServerUpload or DirectUpload) |
| ButtonText | string | "Upload File" | Button text when idle |
| UploadingText | string | "Uploading..." | Text shown during upload |
| ButtonClass | string | "btn-primary" | CSS class for button styling |
| Disabled | bool | false | Disable the button |
| Accept | string? | null | File type filter (e.g., ".pdf,.jpg") |
| AllowMultiple | bool | false | Allow multiple file selection |
| MaxFileSize | long | 5242880 | Max file size in bytes (5MB default) |
| MinFileSize | long | 0 | Min file size in bytes |
| AllowedExtensions | string[]? | null | Allowed file extensions |
| MaxFiles | int | 10 | Max number of files (when AllowMultiple is true) |
| ServerUploadEndpoint | string | "/api/aws-upload/server" | Server upload API endpoint |
| PresignedUrlEndpoint | string | "/api/aws-upload/presigned-url" | Presigned URL API endpoint |
| Folder | string? | null | S3 folder path (e.g., "documents/invoices") |
| CustomFileName | string? | null | Custom filename to use |
| PreserveFilename | bool | true | Preserve original filename |
| OnUploadComplete | EventCallback<UploadResult> | - | Callback when upload succeeds |
| OnUploadError | EventCallback<UploadErrorResult> | - | Callback when upload fails |
| OnUploadStart | EventCallback | - | Callback when upload starts |
| OnFileSelected | EventCallback<IBrowserFile> | - | Callback when file is selected |

### AdvancedAwsUploadButton

A feature-rich component with upload method selector and detailed UI controls.

**Features:**
- Radio button selector for upload method
- Separate file input and upload button
- Status messages and alerts
- File URL display

**Example:**

```razor
<AdvancedAwsUploadButton
    ShowVersionSelector="true"
    Version="UploadVersion.ServerUpload"
    Label="Select Your File"
    HelpText="Maximum file size: 5 MB"
    ButtonText="Upload File"
    UploadingText="Processing..."
    Folder="uploads"
    Accept=".pdf,.doc,.docx"
    MaxFileSize="5242880"
    AllowedExtensions='new[] { ".pdf", ".doc", ".docx" }'
    OnUploadComplete="HandleUploadComplete"
    OnUploadError="HandleUploadError" />
```

**Parameters:** (Same as AwsUploadButton, plus:)

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| ShowVersionSelector | bool | false | Show radio buttons to select upload method |
| Label | string | "Select File" | Label for file input |
| HelpText | string? | null | Help text below file input |

## MudBlazor Integration

The components work seamlessly with MudBlazor. Here's how to integrate them:

### Setting Up Interactivity with MudBlazor

Blazor offers three render modes: **InteractiveServer**, **InteractiveWebAssembly**, and **InteractiveAuto**. The upload components support all modes.

#### Option 1: Interactive Server

For server-side rendering with SignalR:

```razor
@page "/upload"
@rendermode InteractiveServer
@inject IAwsS3Service AwsS3Service

<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-8">
    <MudPaper Class="pa-6">
        <MudText Typo="Typo.h4" Class="mb-4">Upload to AWS S3</MudText>

        <AwsUploadButton
            Version="UploadVersion.ServerUpload"
            ButtonText="Upload File"
            ButtonClass="btn-primary"
            Folder="documents"
            OnUploadComplete="HandleComplete" />
    </MudPaper>
</MudContainer>

@code {
    private void HandleComplete(AwsUploadButton.UploadResult result)
    {
        // Handle upload completion
    }
}
```

#### Option 2: Interactive WebAssembly

For client-side rendering with WebAssembly:

```razor
@page "/upload"
@rendermode InteractiveWebAssembly
@inject HttpClient Http

<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-8">
    <MudPaper Class="pa-6">
        <MudText Typo="Typo.h4" Class="mb-4">Upload to AWS S3</MudText>

        <AwsUploadButton
            Version="UploadVersion.DirectUpload"
            ButtonText="Upload File"
            ButtonClass="btn-primary"
            Folder="documents"
            OnUploadComplete="HandleComplete" />
    </MudPaper>
</MudContainer>

@code {
    private void HandleComplete(AwsUploadButton.UploadResult result)
    {
        // Handle upload completion
    }
}
```

#### Option 3: Interactive Auto (Recommended)

Automatically chooses between Server and WebAssembly:

```razor
@page "/upload"
@rendermode InteractiveAuto

<!-- Component works with both render modes -->
<AwsUploadButton
    Version="UploadVersion.ServerUpload"
    ButtonText="Upload File"
    Folder="documents" />
```

### MudBlazor Custom Implementation

You can also use MudBlazor components with the API helpers:

```razor
@page "/custom-upload"
@rendermode InteractiveServer
@inject IAwsS3Service AwsS3Service

<MudContainer MaxWidth="MaxWidth.Medium">
    <MudPaper Class="pa-6">
        <MudText Typo="Typo.h4" GutterBottom="true">Custom Upload</MudText>

        <MudFileUpload T="IBrowserFile"
                       @ref="_fileUpload"
                       FilesChanged="HandleFileSelected"
                       Accept=".pdf,.jpg,.png">
            <ActivatorContent>
                <MudButton Variant="Variant.Filled"
                           Color="Color.Primary"
                           StartIcon="@Icons.Material.Filled.CloudUpload"
                           Disabled="@_isUploading">
                    @if (_isUploading)
                    {
                        <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                        <span>Uploading...</span>
                    }
                    else
                    {
                        <span>Select & Upload File</span>
                    }
                </MudButton>
            </ActivatorContent>
        </MudFileUpload>

        @if (!string.IsNullOrEmpty(_statusMessage))
        {
            <MudAlert Severity="@(_isError ? Severity.Error : Severity.Success)" Class="mt-4">
                @_statusMessage
            </MudAlert>
        }

        @if (!string.IsNullOrEmpty(_uploadedFileUrl))
        {
            <MudAlert Severity="Severity.Info" Class="mt-4">
                <MudText><strong>File URL:</strong></MudText>
                <MudLink Href="@_uploadedFileUrl" Target="_blank">@_uploadedFileUrl</MudLink>
            </MudAlert>
        }
    </MudPaper>
</MudContainer>

@code {
    private MudFileUpload<IBrowserFile>? _fileUpload;
    private IBrowserFile? _selectedFile;
    private bool _isUploading = false;
    private string? _statusMessage;
    private string? _uploadedFileUrl;
    private bool _isError = false;
    private const long MaxFileSize = 5242880; // 5 MB

    private async Task HandleFileSelected(IBrowserFile? file)
    {
        _selectedFile = file;
        _statusMessage = null;
        _uploadedFileUrl = null;
        _isError = false;

        // Automatically trigger upload when file is selected
        if (_selectedFile != null)
        {
            await UploadFile();
        }
    }

    private async Task UploadFile()
    {
        if (_selectedFile == null) return;

        _isUploading = true;

        // Use helper from library
        var result = await AwsUploadHelper.UploadFileAsync(
            AwsS3Service,
            _selectedFile,
            folder: "documents",
            customFileName: null,
            preserveFilename: true,
            maxFileSize: MaxFileSize);

        if (result.Success)
        {
            _uploadedFileUrl = result.Url;
            _statusMessage = "File uploaded successfully!";
        }
        else
        {
            _statusMessage = result.ErrorMessage;
            _isError = true;
        }

        _isUploading = false;
    }
}
```

## API Helper Usage (Programmatic)

For WebAssembly scenarios where you want full control, use the `AwsUploadApiHelper` class:

### Server Upload via API

```csharp
@inject HttpClient Http

@code {
    private async Task UploadViaServer(IBrowserFile file)
    {
        var result = await AwsUploadApiHelper.UploadViaServerAsync(
            Http,
            file,
            folder: "documents",
            customFileName: null,
            preserveFilename: true,
            maxFileSize: 5242880,
            serverEndpoint: "/api/aws-upload/server"
        );

        if (result.Success)
        {
            Console.WriteLine($"Uploaded: {result.Url}");
            Console.WriteLine($"Message: {result.Message}");
        }
        else
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
        }
    }
}
```

### Direct Upload via Presigned URL

```csharp
@inject HttpClient Http

@code {
    private async Task UploadViaPresignedUrl(IBrowserFile file)
    {
        var result = await AwsUploadApiHelper.UploadViaPresignedUrlAsync(
            Http,
            file,
            folder: "documents",
            customFileName: null,
            preserveFilename: true,
            maxFileSize: 5242880,
            presignedEndpoint: "/api/aws-upload/presigned-url"
        );

        if (result.Success)
        {
            Console.WriteLine($"Uploaded: {result.Url}");
            Console.WriteLine($"Message: {result.Message}");
        }
        else
        {
            Console.WriteLine($"Error: {result.ErrorMessage}");
        }
    }
}
```

## Upload Methods Comparison

### Server Upload (Version.ServerUpload)

**How it works:**
1. Client sends file to your ASP.NET Core server
2. Server uploads file to S3
3. Server returns public URL to client

**Pros:**
- Full server-side control
- Can add custom validation/processing
- Works with any authentication mechanism

**Cons:**
- File goes through your server (uses bandwidth)
- Slower for large files
- Higher server resource usage

**Best for:**
- Small to medium files
- When you need server-side validation
- When you need to process files before upload

### Direct Upload (Version.DirectUpload)

**How it works:**
1. Client requests presigned URL from server
2. Server generates time-limited upload URL
3. Client uploads directly to S3 using presigned URL

**Pros:**
- Faster uploads (no server intermediary)
- Reduced server bandwidth usage
- Better scalability

**Cons:**
- Limited server-side validation
- Requires CORS configuration on S3
- Slightly more complex setup

**Best for:**
- Large files
- High-volume uploads
- Reducing server load

## Complete Examples

### Example 1: Simple Upload with Bootstrap

```razor
@page "/simple-upload"
@inject HttpClient Http

<div class="container mt-5">
    <div class="card">
        <div class="card-body">
            <h4 class="card-title">Upload File to AWS S3</h4>

            <AwsUploadButton
                Version="UploadVersion.ServerUpload"
                ButtonText="Choose File & Upload"
                ButtonClass="btn-primary"
                Accept=".pdf,.jpg,.png"
                MaxFileSize="5242880"
                Folder="uploads"
                OnUploadComplete="HandleUploadComplete"
                OnUploadError="HandleUploadError" />

            @if (!string.IsNullOrEmpty(_message))
            {
                <div class="alert alert-@(_isError ? "danger" : "success") mt-3">
                    @_message
                </div>
            }
        </div>
    </div>
</div>

@code {
    private string? _message;
    private bool _isError;

    private void HandleUploadComplete(AwsUploadButton.UploadResult result)
    {
        _message = $"File uploaded successfully! URL: {result.Url}";
        _isError = false;
    }

    private void HandleUploadError(AwsUploadButton.UploadErrorResult error)
    {
        _message = $"Upload failed: {error.ErrorMessage}";
        _isError = true;
    }
}
```

### Example 2: Advanced Upload with Method Selection

```razor
@page "/advanced-upload"
@inject HttpClient Http

<div class="container mt-5">
    <div class="card">
        <div class="card-body">
            <h4 class="card-title">Advanced File Upload</h4>

            <AdvancedAwsUploadButton
                ShowVersionSelector="true"
                Version="UploadVersion.ServerUpload"
                Label="Select Document"
                HelpText="Supported formats: PDF, DOCX, Images (max 10 MB)"
                ButtonText="Upload Document"
                ButtonClass="btn-success"
                Accept=".pdf,.doc,.docx,.jpg,.png"
                MaxFileSize="10485760"
                AllowedExtensions='new[] { ".pdf", ".doc", ".docx", ".jpg", ".png" }'
                Folder="documents"
                PreserveFilename="true"
                OnUploadComplete="HandleComplete"
                OnUploadError="HandleError" />
        </div>
    </div>
</div>

@code {
    private void HandleComplete(string url)
    {
        Console.WriteLine($"Upload complete: {url}");
    }

    private void HandleError(string error)
    {
        Console.WriteLine($"Upload error: {error}");
    }
}
```

### Example 3: Custom Validation with MudBlazor

```razor
@page "/validated-upload"
@rendermode InteractiveServer
@inject IAwsS3Service AwsS3Service
@inject ISnackbar Snackbar

<MudContainer MaxWidth="MaxWidth.Medium" Class="mt-8">
    <MudPaper Class="pa-6">
        <MudText Typo="Typo.h4" GutterBottom="true">Validated Upload</MudText>

        <MudFileUpload T="IBrowserFile"
                       FilesChanged="HandleFileSelected"
                       Accept=".pdf">
            <ActivatorContent>
                <MudButton Variant="Variant.Filled"
                           Color="Color.Primary"
                           StartIcon="@Icons.Material.Filled.Upload"
                           Disabled="@_isUploading">
                    Select PDF File
                </MudButton>
            </ActivatorContent>
        </MudFileUpload>

        @if (_isUploading)
        {
            <MudProgressLinear Indeterminate="true" Class="mt-4" />
        }
    </MudPaper>
</MudContainer>

@code {
    private bool _isUploading = false;
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    private async Task HandleFileSelected(IBrowserFile file)
    {
        // Custom validation
        if (!file.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            Snackbar.Add("Only PDF files are allowed", Severity.Error);
            return;
        }

        if (file.Size > MaxFileSize)
        {
            Snackbar.Add($"File size exceeds {MaxFileSize / 1024.0 / 1024.0:F2} MB limit", Severity.Error);
            return;
        }

        _isUploading = true;

        var result = await AwsUploadHelper.UploadFileAsync(
            AwsS3Service,
            file,
            folder: "validated-pdfs",
            preserveFilename: true,
            maxFileSize: MaxFileSize
        );

        _isUploading = false;

        if (result.Success)
        {
            Snackbar.Add($"File uploaded: {result.Url}", Severity.Success);
        }
        else
        {
            Snackbar.Add($"Upload failed: {result.ErrorMessage}", Severity.Error);
        }
    }
}
```

## Render Mode Best Practices

### When to use InteractiveServer
- Small to medium file uploads
- When you need server-side validation
- When you have IAwsS3Service available
- Best with ServerUpload method

### When to use InteractiveWebAssembly
- Large file uploads
- High-volume scenarios
- When you want to reduce server load
- Best with DirectUpload method

### When to use InteractiveAuto
- You want flexibility
- Mixed upload scenarios
- Let Blazor choose the best mode

## Troubleshooting

### Component doesn't render
- Ensure `@using Nesco.AwsUploader.Components` in _Imports.razor
- Check that backend server package is installed and configured
- Verify render mode is set on the page

### Upload fails with 404
- Verify controllers are mapped in Program.cs: `app.MapControllers()`
- Check API endpoints: `/api/aws-upload/server` and `/api/aws-upload/presigned-url`
- Ensure server package is properly configured

### Direct upload fails with CORS error
- Configure CORS on your S3 bucket
- Allow PUT and POST methods
- Add your domain to AllowedOrigins

### File uploads but validation fails
- Check `MaxFileSize` parameter matches server limits
- Verify `AllowedExtensions` array is correctly formatted
- Ensure file meets minimum size requirements

## License

This package is open source and free to use for anyone.

## Related Packages

- **Nesco.AwsUploader.Server** - Backend server package (required)

## Support

For issues and questions, please contact support or visit the project repository.
