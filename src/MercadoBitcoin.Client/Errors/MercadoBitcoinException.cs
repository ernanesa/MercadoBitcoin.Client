using System;
using System.Collections.Generic;

namespace MercadoBitcoin.Client.Errors
{
    public class MercadoBitcoinException : Exception
    {
        public int StatusCode { get; }
        public string? Response { get; }
        public IReadOnlyDictionary<string, IEnumerable<string>>? Headers { get; }

        public MercadoBitcoinException(string message, int statusCode, string? response, IReadOnlyDictionary<string, IEnumerable<string>>? headers, Exception? innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            Response = response;
            Headers = headers;
        }

        public MercadoBitcoinException(string message) : base(message)
        {
        }

        public MercadoBitcoinException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
