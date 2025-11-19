using System.Text.Json;

namespace MercadoBitcoin.Client.Generated
{
    /// <summary>
    /// Partial extension of the generated client to align serialization options with the Source Generation context
    /// and reduce AOT warnings related to dynamic reflection.
    /// </summary>
    public partial class Client
    {
        static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
        {
            var context = MercadoBitcoinJsonSerializerContext.Default;

            // FIX: Assigns the type resolver from the generated context.
            // This is the crucial line for AOT compatibility.
            settings.TypeInfoResolver = context;

            // The lines below are kept to ensure consistency, although TypeInfoResolver already encapsulates these options.
            settings.PropertyNamingPolicy = context.Options.PropertyNamingPolicy;
            settings.DefaultIgnoreCondition = context.Options.DefaultIgnoreCondition;
            settings.PropertyNameCaseInsensitive = context.Options.PropertyNameCaseInsensitive;
            settings.WriteIndented = context.Options.WriteIndented;
        }
    }

    public partial class OpenClient
    {
        static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
        {
            var context = MercadoBitcoinJsonSerializerContext.Default;

            // FIX: Assigns the type resolver also for the OpenClient.
            settings.TypeInfoResolver = context;

            settings.PropertyNamingPolicy = context.Options.PropertyNamingPolicy;
            settings.DefaultIgnoreCondition = context.Options.DefaultIgnoreCondition;
            settings.PropertyNameCaseInsensitive = context.Options.PropertyNameCaseInsensitive;
            settings.WriteIndented = context.Options.WriteIndented;
        }
    }
}