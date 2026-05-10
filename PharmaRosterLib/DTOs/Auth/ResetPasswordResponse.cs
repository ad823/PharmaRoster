using System.Text.Json.Serialization;

namespace PharmaRosterLib.DTOs.Auth
{
    public class ResetPasswordResponse
    {
        [JsonPropertyName("staff_id")]
        public string staff_id { get; set; }

        [JsonPropertyName("account")]
        public string account { get; set; }

        [JsonPropertyName("temp_password")]
        public string temp_password { get; set; }
    }
}