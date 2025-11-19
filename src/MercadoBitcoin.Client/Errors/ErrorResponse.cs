namespace MercadoBitcoin.Client.Errors
{
    public class ErrorResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        public void Reset()
        {
            Code = string.Empty;
            Message = string.Empty;
        }
    }
}
