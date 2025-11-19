using System;

namespace MercadoBitcoin.Client.Errors
{
    /// <summary>
    /// Base exception for all errors returned by the Mercado Bitcoin API.
    /// </summary>
    public class MercadoBitcoinApiException : Exception
    {
        private readonly string? _errorCode;
        private readonly string? _errorMessage;
        private ErrorResponse? _errorResponse;

        public ErrorResponse Error
        {
            get
            {
                if (_errorResponse == null)
                {
                    _errorResponse = new ErrorResponse
                    {
                        Code = _errorCode ?? string.Empty,
                        Message = _errorMessage ?? string.Empty
                    };
                }
                return _errorResponse;
            }
        }

        public string? ErrorCode => _errorCode;
        public string? ErrorMessage => _errorMessage;

        public MercadoBitcoinApiException(string message, string errorCode, string errorMessage) : base(message)
        {
            _errorCode = errorCode;
            _errorMessage = errorMessage;

            if (!string.IsNullOrWhiteSpace(errorCode))
                Data["ApiErrorCode"] = errorCode;
            if (!string.IsNullOrWhiteSpace(errorMessage))
                Data["ApiErrorMessage"] = errorMessage;
        }

        public MercadoBitcoinApiException(string message, ErrorResponse error) : base(message)
        {
            _errorResponse = error;
            _errorCode = error?.Code;
            _errorMessage = error?.Message;

            if (!string.IsNullOrWhiteSpace(error?.Code))
                Data["ApiErrorCode"] = error.Code;
            if (!string.IsNullOrWhiteSpace(error?.Message))
                Data["ApiErrorMessage"] = error.Message;
        }

        public MercadoBitcoinApiException(string message, Exception innerException, ErrorResponse error) : base(message, innerException)
        {
            _errorResponse = error;
            _errorCode = error?.Code;
            _errorMessage = error?.Message;

            if (!string.IsNullOrWhiteSpace(error?.Code))
                Data["ApiErrorCode"] = error.Code;
            if (!string.IsNullOrWhiteSpace(error?.Message))
                Data["ApiErrorMessage"] = error.Message;
        }
    }

    /// <summary>
    /// Exception thrown when the API returns an authentication or authorization error (401/403).
    /// </summary>
    public class MercadoBitcoinUnauthorizedException : MercadoBitcoinApiException
    {
        public MercadoBitcoinUnauthorizedException(string message, string errorCode, string errorMessage) : base(message, errorCode, errorMessage) { }
        public MercadoBitcoinUnauthorizedException(string message, ErrorResponse error) : base(message, error) { }
        public MercadoBitcoinUnauthorizedException(string message, Exception innerException, ErrorResponse error) : base(message, innerException, error) { }
    }

    /// <summary>
    /// Exception thrown when the API returns a parameter validation error (400).
    /// </summary>
    public class MercadoBitcoinValidationException : MercadoBitcoinApiException
    {
        public MercadoBitcoinValidationException(string message, string errorCode, string errorMessage) : base(message, errorCode, errorMessage) { }
        public MercadoBitcoinValidationException(string message, ErrorResponse error) : base(message, error) { }
        public MercadoBitcoinValidationException(string message, Exception innerException, ErrorResponse error) : base(message, innerException, error) { }
    }

    /// <summary>
    /// Exception thrown when the API returns a rate limit error (429).
    /// </summary>
    public class MercadoBitcoinRateLimitException : MercadoBitcoinApiException
    {
        public MercadoBitcoinRateLimitException(string message, string errorCode, string errorMessage) : base(message, errorCode, errorMessage) { }
        public MercadoBitcoinRateLimitException(string message, ErrorResponse error) : base(message, error) { }
        public MercadoBitcoinRateLimitException(string message, Exception innerException, ErrorResponse error) : base(message, innerException, error) { }
    }
}
