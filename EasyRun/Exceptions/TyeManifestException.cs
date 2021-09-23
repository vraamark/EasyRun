using System;

namespace EasyRun.Exceptions
{
    public class TyeManifestException : Exception
    {
        public TyeManifestException() : base()
        {
        }

        public TyeManifestException(string message) : base(message)
        {
        }

        public TyeManifestException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
