using System.Text.Json;

namespace MercadoBitcoin.Client.Generated
{
    public partial class Client
    {
        /// <summary>
        /// Sets the JsonSerializerOptions for this instance, overriding the default lazy initialization.
        /// This allows injecting options configured with Source Generators or custom converters.
        /// </summary>
        internal void SetSerializerOptions(JsonSerializerOptions options)
        {
            _instanceSettings = options;
        }
    }

    public partial class OpenClient
    {
        /// <summary>
        /// Sets the JsonSerializerOptions for this instance, overriding the default lazy initialization.
        /// This allows injecting options configured with Source Generators or custom converters.
        /// </summary>
        internal void SetSerializerOptions(JsonSerializerOptions options)
        {
            _instanceSettings = options;
        }
    }
}
