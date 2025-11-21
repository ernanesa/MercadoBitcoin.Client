namespace MercadoBitcoin.Client.Diagnostics
{
    /// <summary>
    /// Constants for diagnostics, metrics, and tracing.
    /// </summary>
    public static class MercadoBitcoinDiagnostics
    {
        /// <summary>
        /// The name of the Meter used by the library.
        /// </summary>
        public const string MeterName = "MercadoBitcoin.Client";

        /// <summary>
        /// The name of the histogram tracking HTTP request duration.
        /// </summary>
        public const string RequestDurationHistogram = "mb_client_http_request_duration";

        /// <summary>
        /// The name of the counter tracking retry attempts.
        /// </summary>
        public const string RetryCounter = "mb_client_http_retries";

        /// <summary>
        /// The version of the Meter.
        /// </summary>
        public const string MeterVersion = "4.0.0";
    }
}
