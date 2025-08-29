using System.Text.Json;

namespace MercadoBitcoin.Client.Generated
{
    /// <summary>
    /// Extensão parcial do client gerado para alinhar as opções de serialização com o contexto Source Generation
    /// e reduzir warnings AOT relacionados a reflexão dinâmica.
    /// </summary>
    public partial class Client
    {
        static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
        {
            // Copia configurações do contexto gerado
            var ctx = MercadoBitcoinJsonSerializerContext.Default;
            settings.PropertyNamingPolicy = ctx.Options.PropertyNamingPolicy;
            settings.DefaultIgnoreCondition = ctx.Options.DefaultIgnoreCondition;
            settings.PropertyNameCaseInsensitive = ctx.Options.PropertyNameCaseInsensitive;
            settings.WriteIndented = ctx.Options.WriteIndented;
        }
    }

    public partial class OpenClient
    {
        static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
        {
            var ctx = MercadoBitcoinJsonSerializerContext.Default;
            settings.PropertyNamingPolicy = ctx.Options.PropertyNamingPolicy;
            settings.DefaultIgnoreCondition = ctx.Options.DefaultIgnoreCondition;
            settings.PropertyNameCaseInsensitive = ctx.Options.PropertyNameCaseInsensitive;
            settings.WriteIndented = ctx.Options.WriteIndented;
        }
    }
}