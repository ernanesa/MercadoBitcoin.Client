using System;

namespace MercadoBitcoin.Client
{
    /// <summary>
    /// Exceção base para todos os erros retornados pela API Mercado Bitcoin.
    /// </summary>
    public class MercadoBitcoinApiException : Exception
    {
        public ErrorResponse Error { get; }

        public string? ErrorCode => Error?.Code;
        public string? ErrorMessage => Error?.Message;

        public MercadoBitcoinApiException(string message, ErrorResponse error) : base(message)
        {
            Error = error;
            if (!string.IsNullOrWhiteSpace(error?.Code))
                Data["ApiErrorCode"] = error.Code;
            if (!string.IsNullOrWhiteSpace(error?.Message))
                Data["ApiErrorMessage"] = error.Message;
        }

        public MercadoBitcoinApiException(string message, Exception innerException, ErrorResponse error) : base(message, innerException)
        {
            Error = error;
            if (!string.IsNullOrWhiteSpace(error?.Code))
                Data["ApiErrorCode"] = error.Code;
            if (!string.IsNullOrWhiteSpace(error?.Message))
                Data["ApiErrorMessage"] = error.Message;
        }
    }

    /// <summary>
    /// Exceção lançada quando a API retorna erro de autenticação ou autorização (401/403).
    /// </summary>
    public class MercadoBitcoinUnauthorizedException : MercadoBitcoinApiException
    {
        public MercadoBitcoinUnauthorizedException(string message, ErrorResponse error) : base(message, error) { }
        public MercadoBitcoinUnauthorizedException(string message, Exception innerException, ErrorResponse error) : base(message, innerException, error) { }
    }

    /// <summary>
    /// Exceção lançada quando a API retorna erro de validação de parâmetros (400).
    /// </summary>
    public class MercadoBitcoinValidationException : MercadoBitcoinApiException
    {
        public MercadoBitcoinValidationException(string message, ErrorResponse error) : base(message, error) { }
        public MercadoBitcoinValidationException(string message, Exception innerException, ErrorResponse error) : base(message, innerException, error) { }
    }

    /// <summary>
    /// Exceção lançada quando a API retorna erro de rate limit (429).
    /// </summary>
    public class MercadoBitcoinRateLimitException : MercadoBitcoinApiException
    {
        public MercadoBitcoinRateLimitException(string message, ErrorResponse error) : base(message, error) { }
        public MercadoBitcoinRateLimitException(string message, Exception innerException, ErrorResponse error) : base(message, innerException, error) { }
    }
}
