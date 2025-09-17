using System;

namespace PlatformShared.DataProtection.S3
{
    internal class TooManyObjectsException : Exception
    {
        public TooManyObjectsException()
        {
        }

        public TooManyObjectsException(string message) : base(message)
        {
        }

        public TooManyObjectsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
