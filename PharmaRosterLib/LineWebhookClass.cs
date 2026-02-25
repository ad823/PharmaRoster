using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PharmaRosterLib
{
    public class LineWebhookRequest
    {
        [JsonPropertyName("events")]
        public List<LineWebhookEvent> events { get; set; } = new List<LineWebhookEvent>();
    }

    public class LineWebhookEvent
    {
        [JsonPropertyName("type")]
        public string type { get; set; }

        [JsonPropertyName("replyToken")]
        public string replyToken { get; set; }

        [JsonPropertyName("source")]
        public LineWebhookSource source { get; set; }

        [JsonPropertyName("message")]
        public LineWebhookMessage message { get; set; }
    }

    public class LineWebhookSource
    {
        [JsonPropertyName("userId")]
        public string userId { get; set; }

        [JsonPropertyName("type")]
        public string type { get; set; }
    }

    public class LineWebhookMessage
    {
        [JsonPropertyName("type")]
        public string type { get; set; }

        [JsonPropertyName("text")]
        public string text { get; set; }
    }

}
