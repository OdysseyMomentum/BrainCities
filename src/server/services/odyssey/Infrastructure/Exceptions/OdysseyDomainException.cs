using System;

namespace Odyssey.API.Infrastructure.Exceptions
{
    public class OdysseyDomainException : Exception
    {
        public OdysseyDomainException()
        { }

        public OdysseyDomainException(string message)
            : base(message)
        { }

        public OdysseyDomainException(string message, Exception innerException)
            : base(message, innerException)
        { }

    }
}
