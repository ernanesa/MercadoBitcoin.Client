using System.Text.Json;
using System.Text.Json.Serialization;

namespace MercadoBitcoin.Client;

/// <summary>
/// Provides a globally cached JsonSerializerOptions instance for optimal performance.
/// </summary>
/// <remarks>
/// JsonSerializerOptions instances are expensive to create due to internal caching of type metadata.
/// This class provides a singleton instance that should be used throughout the application.
/// </remarks>
internal static class JsonOptionsCache
{
    /// <summary>
    /// The default cached JsonSerializerOptions instance with source-generated serialization context.
    /// </summary>
    public static readonly JsonSerializerOptions Default = CreateDefaultOptions();

    /// <summary>
    /// Options optimized for WebSocket message parsing (case-insensitive, flexible).
    /// </summary>
    public static readonly JsonSerializerOptions WebSocket = CreateWebSocketOptions();

    /// <summary>
    /// Options optimized for high-performance scenarios (minimal features).
    /// </summary>
    public static readonly JsonSerializerOptions HighPerformance = CreateHighPerformanceOptions();

    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = MercadoBitcoinJsonSerializerContext.Default,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        return options;
    }

    private static JsonSerializerOptions CreateWebSocketOptions()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = MercadoBitcoinJsonSerializerContext.Default,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        return options;
    }

    private static JsonSerializerOptions CreateHighPerformanceOptions()
    {
        var options = new JsonSerializerOptions
        {
            TypeInfoResolver = MercadoBitcoinJsonSerializerContext.Default,
            PropertyNameCaseInsensitive = false, // Exact match for performance
            NumberHandling = JsonNumberHandling.Strict,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Faster encoding
        };

        return options;
    }
}
