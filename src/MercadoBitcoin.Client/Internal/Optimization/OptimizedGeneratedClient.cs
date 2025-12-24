using System.Buffers;
using System.IO.Pipelines;
using System.Net.Http;
using System.Text.Json;
using MercadoBitcoin.Client.Generated;

namespace MercadoBitcoin.Client.Internal.Optimization
{
    internal class OptimizedGeneratedClient : Generated.Client
    {
        public OptimizedGeneratedClient(HttpClient httpClient) : base(httpClient)
        {
        }

        protected override async Task<ObjectResponseResult<T>> ReadObjectResponseAsync<T>(HttpResponseMessage response, IReadOnlyDictionary<string, IEnumerable<string>> headers, CancellationToken cancellationToken)
        {
            if (response == null || response.Content == null)
            {
                return new ObjectResponseResult<T>(default!, string.Empty);
            }

            // Beast Mode v5.0: Zero-allocation response parsing using PipeReader
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var reader = PipeReader.Create(stream);

            try
            {
                while (true)
                {
                    ReadResult result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    if (result.IsCompleted)
                    {
                        if (buffer.IsEmpty) return new ObjectResponseResult<T>(default!, string.Empty);

                        var jsonReader = new Utf8JsonReader(buffer);
                        // Use JsonTypeInfo for AOT compatibility and to avoid the warning
                        var typeInfo = (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>)JsonSerializerSettings.GetTypeInfo(typeof(T));
                        var typedBody = JsonSerializer.Deserialize(ref jsonReader, typeInfo);
                        return new ObjectResponseResult<T>(typedBody!, string.Empty);
                    }

                    // Advance the reader to the end of the current buffer to request more data
                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
            catch (JsonException exception)
            {
                var message = "Could not deserialize the response body stream as " + typeof(T).FullName + ".";
                throw new ApiException(message, (int)response.StatusCode, string.Empty, headers, exception);
            }
            finally
            {
                await reader.CompleteAsync().ConfigureAwait(false);
            }
        }
    }

    internal class OptimizedOpenClient : Generated.OpenClient
    {
        public OptimizedOpenClient(HttpClient httpClient) : base(httpClient)
        {
        }

        protected override async Task<ObjectResponseResult<T>> ReadObjectResponseAsync<T>(HttpResponseMessage response, IReadOnlyDictionary<string, IEnumerable<string>> headers, CancellationToken cancellationToken)
        {
            if (response == null || response.Content == null)
            {
                return new ObjectResponseResult<T>(default!, string.Empty);
            }

            // Beast Mode v5.0: Zero-allocation response parsing using PipeReader
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var reader = PipeReader.Create(stream);

            try
            {
                while (true)
                {
                    ReadResult result = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    if (result.IsCompleted)
                    {
                        if (buffer.IsEmpty) return new ObjectResponseResult<T>(default!, string.Empty);

                        var jsonReader = new Utf8JsonReader(buffer);
                        // Use JsonTypeInfo for AOT compatibility and to avoid the warning
                        var typeInfo = (System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>)JsonSerializerSettings.GetTypeInfo(typeof(T));
                        var typedBody = JsonSerializer.Deserialize(ref jsonReader, typeInfo);
                        return new ObjectResponseResult<T>(typedBody!, string.Empty);
                    }

                    // Advance the reader to the end of the current buffer to request more data
                    reader.AdvanceTo(buffer.Start, buffer.End);
                }
            }
            catch (JsonException exception)
            {
                var message = "Could not deserialize the response body stream as " + typeof(T).FullName + ".";
                throw new ApiException(message, (int)response.StatusCode, string.Empty, headers, exception);
            }
            finally
            {
                await reader.CompleteAsync().ConfigureAwait(false);
            }
        }
    }
}
