using System;
using System.IO.Pipelines;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace MercadoBitcoin.Client.Internal.Optimization
{
    /// <summary>
    /// Helper for high-performance JSON parsing using System.IO.Pipelines.
    /// </summary>
    internal static class PipeParsingHelper
    {
        public static async Task<T?> DeserializeAsync<T>(
            PipeReader reader,
            JsonTypeInfo<T> typeInfo,
            CancellationToken ct)
        {
            using var stream = reader.AsStream();
            return await JsonSerializer.DeserializeAsync(stream, typeInfo, ct).ConfigureAwait(false);
        }

        public static async Task<T?> DeserializeFromResponseAsync<T>(
            HttpResponseMessage response,
            JsonTypeInfo<T> typeInfo,
            CancellationToken ct)
        {
            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            return await JsonSerializer.DeserializeAsync(stream, typeInfo, ct).ConfigureAwait(false);
        }
    }
}
