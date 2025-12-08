using System.Text.Json.Serialization;

namespace ARISESLCOM.DTO.Rede
{
    public class RedeOAuthTokenRequest
    {
        [JsonPropertyName("grant_type")]
        public string GrantType { get; set; } = "client_credentials";
    }
}
