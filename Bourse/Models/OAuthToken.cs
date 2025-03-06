using Newtonsoft.Json;

namespace Bourse.Models
{
    public class OAuthToken
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        // Propriété calculée pour l'heure d'expiration du jeton
        public DateTime ExpirationTime => DateTime.UtcNow.AddSeconds(ExpiresIn);
    }
}
