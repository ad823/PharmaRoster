using System.Text.Json.Serialization;

namespace PharmaRosterLib.DTOs.Auth
{
    public class RegisteredStaffView
    {
        [JsonPropertyName("GUID")]
        public string GUID { get; set; }

        [JsonPropertyName("staff_id")]
        public string staff_id { get; set; }

        [JsonPropertyName("staff_name")]
        public string staff_name { get; set; }

        [JsonPropertyName("staff_simple_name")]
        public string staff_simple_name { get; set; }

        [JsonPropertyName("account")]
        public string account { get; set; }

        [JsonPropertyName("role")]
        public string role { get; set; }

        [JsonPropertyName("is_locked")]
        public string is_locked { get; set; }

        [JsonPropertyName("fail_count")]
        public string fail_count { get; set; }

        [JsonPropertyName("last_login_at")]
        public string last_login_at { get; set; }

        [JsonPropertyName("last_logout_at")]
        public string last_logout_at { get; set; }

        [JsonPropertyName("created_at")]
        public string created_at { get; set; }

        [JsonPropertyName("updated_at")]
        public string updated_at { get; set; }
    }
}