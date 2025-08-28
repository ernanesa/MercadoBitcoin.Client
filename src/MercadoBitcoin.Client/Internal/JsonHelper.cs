using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MercadoBitcoin.Client.Generated;

namespace MercadoBitcoin.Client.Internal
{
    /// <summary>
    /// Helper centraliza serialização usando Source Generation para evitar dynamic code em AOT.
    /// Para tipos não registrados, recai para opções padrão (ainda pode gerar warning se não controlado, mas minimiza superfícies principais).
    /// </summary>
    internal static class JsonHelper
    {
        public static byte[] SerializeToUtf8Bytes<T>(T value)
        {
            var typeInfo = GetTypeInfo<T>();
            if (typeInfo != null)
            {
                using var buffer = new MemoryStream();
                using var writer = new Utf8JsonWriter(buffer);
                JsonSerializer.Serialize(writer, value, typeInfo);
                writer.Flush();
                return buffer.ToArray();
            }
            return JsonSerializer.SerializeToUtf8Bytes(value, MercadoBitcoinJsonSerializerContext.Default.Options);
        }

        public static T? Deserialize<T>(string json)
        {
            var typeInfo = GetTypeInfo<T>();
            if (typeInfo != null)
            {
                return (T?)JsonSerializer.Deserialize(json, typeInfo);
            }
            return JsonSerializer.Deserialize<T>(json, MercadoBitcoinJsonSerializerContext.Default.Options);
        }

        public static async Task<T?> DeserializeAsync<T>(Stream utf8Json, CancellationToken ct)
        {
            var typeInfo = GetTypeInfo<T>();
            if (typeInfo != null)
            {
                return await JsonSerializer.DeserializeAsync(utf8Json, typeInfo, ct).ConfigureAwait(false);
            }
            return await JsonSerializer.DeserializeAsync<T>(utf8Json, MercadoBitcoinJsonSerializerContext.Default.Options, ct).ConfigureAwait(false);
        }

        private static System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>? GetTypeInfo<T>()
        {
            // Map principal: adicionar se necessário novos DTOs
            return typeof(T) switch
            {
                var t when t == typeof(ErrorResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.ErrorResponse,
                var t when t == typeof(AccountResponse) => (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>?)(object)MercadoBitcoinJsonSerializerContext.Default.AccountResponse,
                _ => null
            };
        }
    }
}