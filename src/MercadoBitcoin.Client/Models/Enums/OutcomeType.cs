namespace MercadoBitcoin.Client.Models.Enums;

public enum OutcomeType
{
    Success,
    HttpError,
    Timeout,
    Cancellation,
    NetworkError,
    CircuitBreakerOpen,
    RateLimitExceeded,
    AuthenticationError,
    UnknownError
}
