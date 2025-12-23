using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace MercadoBitcoin.Client.Internal.Helpers
{
    /// <summary>
    /// Helper centralizes serialization using Source Generation to avoid dynamic code in AOT.
    /// </summary>
    internal static class JsonHelper
    {
        public static byte[] SerializeToUtf8Bytes<T>(T value)
        {
            var typeInfo = GetTypeInfo<T>() ?? throw new NotSupportedException($"Type not registered for source-gen JSON: {typeof(T).FullName}. Add [JsonSerializable] to context.");
            return JsonSerializer.SerializeToUtf8Bytes(value, typeInfo);
        }

        public static T? Deserialize<T>(string json)
        {
            var typeInfo = GetTypeInfo<T>() ?? throw new NotSupportedException($"Type not registered for source-gen JSON: {typeof(T).FullName}. Add [JsonSerializable] to context.");
            return JsonSerializer.Deserialize(json, typeInfo);
        }

        public static async Task<T?> DeserializeAsync<T>(Stream utf8Json, CancellationToken cancellationToken)
        {
            var typeInfo = GetTypeInfo<T>() ?? throw new NotSupportedException($"Type not registered for source-gen JSON: {typeof(T).FullName}. Add [JsonSerializable] to context.");
            return await JsonSerializer.DeserializeAsync(utf8Json, typeInfo, cancellationToken).ConfigureAwait(false);
        }

        private static JsonTypeInfo<T>? GetTypeInfo<T>()
        {
            return (JsonTypeInfo<T>?)MercadoBitcoinJsonSerializerContext.Default.GetTypeInfo(typeof(T));
        }
    }
}