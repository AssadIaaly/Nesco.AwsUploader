using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nesco.AwsUploader.Server.Configuration;
using Nesco.AwsUploader.Server.Services;

namespace Nesco.AwsUploader.Server.Extensions;

/// <summary>
/// Extension methods for configuring AWS S3 upload services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds AWS S3 upload services with explicit configuration using Action
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Action to configure options</param>
    public static IServiceCollection AddAwsS3Uploader(
        this IServiceCollection services,
        Action<AwsS3Options> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        // Configure options
        services.Configure(configureOptions);

        // Create options instance to setup AWS client
        var options = new AwsS3Options();
        configureOptions(options);

        // Validate required options
        if (string.IsNullOrEmpty(options.BucketName))
        {
            throw new InvalidOperationException("AWS S3 BucketName is required");
        }

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var region = Amazon.RegionEndpoint.GetBySystemName(options.Region);

            if (!string.IsNullOrEmpty(options.AccessKey) && !string.IsNullOrEmpty(options.SecretKey))
            {
                var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
                return new AmazonS3Client(credentials, region);
            }

            // Fallback to default credentials (environment variables, IAM role, etc.)
            return new AmazonS3Client(region);
        });

        services.AddScoped<IAwsS3Service, AwsS3Service>();

        return services;
    }

}
