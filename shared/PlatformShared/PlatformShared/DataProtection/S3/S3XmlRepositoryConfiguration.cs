using System;

namespace PlatformShared.DataProtection.S3
{
    public class S3XmlRepositoryConfiguration
    {
        public string BucketName { get; set; }
        public string KeyId { get; set; }

        private string _folderPath = string.Empty;
        public string Prefix
        {
            get
            {
                return _folderPath;
            }

            set
            {
                if (String.IsNullOrWhiteSpace(value))
                {
                    _folderPath = string.Empty;
                    return;
                }

                while (value.EndsWith("/"))
                {
                    value = value.Substring(value.Length - 1);
                }

                _folderPath = value;
            }
        }
    }
}
