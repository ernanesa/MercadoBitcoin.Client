namespace MercadoBitcoin.Client.Internal;

/// <summary>
/// Static cache for globally shared JSON serializer options.
/// Avoids recreating JsonSerializerOptions on every serialization call.
/// </summary>
internal static class JsonOptionsCache
{
    /// <summary>
    /// Gets the default JSON serializer options with source generators configured.
    /// </summary>
    public static System.Text.Json.JsonSerializerOptions Default { get; } = new()
    {
        TypeInfoResolver = MercadoBitcoinJsonSerializerContext.Default,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };
}
