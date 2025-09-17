using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Amazon.S3;
using Amazon.S3.Model;
using System.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime;
using Amazon;
using PlatformShared.Helpers;

namespace PlatformShared.DataProtection.S3
{
    public class S3XmlRepository : IXmlRepository
    {
        private readonly IAmazonS3 _s3Client;
        private readonly S3XmlRepositoryConfiguration _configuration;

        public S3XmlRepository(IAmazonS3 s3Client, S3XmlRepositoryConfiguration configuration)
        {
            if (String.IsNullOrWhiteSpace(configuration.BucketName))
            {
                throw new ArgumentException("BucketName must be specified.");
            }

            if (String.IsNullOrWhiteSpace(configuration.KeyId))
            {
                throw new ArgumentException("KeyId must be specified.");
            }

            _s3Client = s3Client;
            _configuration = configuration;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            var request = new ListObjectsRequest
            {
                BucketName = _configuration.BucketName,
                Prefix = _configuration.Prefix
            };

            var result = AsyncHelpers.RunSync(() => _s3Client.ListObjectsAsync(request));
            if (result.IsTruncated)
            {
                throw new TooManyObjectsException();
            }

            var s3Objects = result.S3Objects.Select(o => GetElement(o.Key)).ToArray();
            return s3Objects;
        }

        private XElement GetElement(string key)
        {
            var request = new GetObjectRequest
            {
                BucketName = _configuration.BucketName,
                Key = key
            };
            // Does not support non-async in .NET Core:
            var result = AsyncHelpers.RunSync(() => _s3Client.GetObjectAsync(request));
            using (var stream = result.ResponseStream)
            {
                var xDocument = XDocument.Load(stream);
                return xDocument.Elements().First();
            }
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            using (var memoryStream = new MemoryStream())
            {
                var key = $"{GetPrefix()}{DateTime.UtcNow.ToString("s")}_{friendlyName}";

                var document = new XDocument();
                document.Add(element);
                document.Save(memoryStream);

                var request = new PutObjectRequest
                {
                    BucketName = _configuration.BucketName,
                    Key = key,
                    InputStream = memoryStream,
                    ServerSideEncryptionMethod = ServerSideEncryptionMethod.AWSKMS,
                    ServerSideEncryptionKeyManagementServiceKeyId = _configuration.KeyId
                };

                // Does not support non-async in .NET Core:
                var result = AsyncHelpers.RunSync(() => _s3Client.PutObjectAsync(request));
            }
        }

        private object GetPrefix()
        {
            return _configuration.Prefix == String.Empty
                ? String.Empty
                : $"{_configuration.Prefix}/";
        }
    }
}
