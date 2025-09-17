using Amazon.S3;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;

namespace PlatformShared.DataProtection.S3
{
    public static class S3DataProtectionBuilderExtensions
    {
        /// <summary>
        /// Configures the data protection system to persist keys to Amazon S3.
        /// </summary>
        /// <param name="builder">The <see cref="IDataProtectionBuilder"/>.</param>
        /// <param name="configuration">The configuration item.</param>
        /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
        public static IDataProtectionBuilder PersistKeysToS3(this IDataProtectionBuilder builder, S3XmlRepositoryConfiguration configuration)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (String.IsNullOrWhiteSpace(configuration.BucketName))
            {
                throw new ArgumentOutOfRangeException(nameof(configuration.BucketName));
            }

            builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
            {
                var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
                return new ConfigureOptions<KeyManagementOptions>(options =>
                {
                    var s3Service = services.GetService<IAmazonS3>();
                    options.XmlRepository = new S3XmlRepository(s3Service, configuration);
                });
            });

            return builder;
        }
    }
}
