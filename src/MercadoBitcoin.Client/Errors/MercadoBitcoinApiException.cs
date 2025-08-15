using System;

namespace MercadoBitcoin.Client
{
    public class MercadoBitcoinApiException : Exception
    {
        public ErrorResponse Error { get; }

        public MercadoBitcoinApiException(string message, ErrorResponse error) : base(message)
        {
            Error = error;
        }

        public MercadoBitcoinApiException(string message, Exception innerException, ErrorResponse error) : base(message, innerException)
        {
            Error = error;
        }
    }
}
