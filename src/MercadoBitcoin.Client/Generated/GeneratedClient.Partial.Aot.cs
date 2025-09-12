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
            var ctx = MercadoBitcoinJsonSerializerContext.Default;

            // CORREÇÃO: Atribui o resolvedor de tipos do contexto gerado.
            // Esta é a linha crucial para a compatibilidade com AOT.
            settings.TypeInfoResolver = ctx;

            // As linhas abaixo são mantidas para garantir consistência, embora o TypeInfoResolver já encapsule estas opções.
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

            // CORREÇÃO: Atribui o resolvedor de tipos também para o OpenClient.
            settings.TypeInfoResolver = ctx;

            settings.PropertyNamingPolicy = ctx.Options.PropertyNamingPolicy;
            settings.DefaultIgnoreCondition = ctx.Options.DefaultIgnoreCondition;
            settings.PropertyNameCaseInsensitive = ctx.Options.PropertyNameCaseInsensitive;
            settings.WriteIndented = ctx.Options.WriteIndented;
        }
    }
}