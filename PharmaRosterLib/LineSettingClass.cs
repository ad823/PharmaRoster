using System.ComponentModel;
using System.Text.Json.Serialization;

namespace PharmaRosterLib
{
    /// <summary>
    /// LINE 設定資料表 (Key-Value)
    /// 用途：儲存 add_friend_url 等可變動設定，避免寫死在程式
    /// </summary>
    [Description("line_settings")]
    public class LineSettingClass
    {
        /// <summary>唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>設定鍵 (唯一) 例如 add_friend_url</summary>
        [JsonPropertyName("setting_key")]
        [Description("VARCHAR,100,UNIQUE")]
        public string setting_key { get; set; }

        /// <summary>設定值 例如 https://lin.ee/xxxxxx</summary>
        [JsonPropertyName("setting_value")]
        [Description("VARCHAR,2000,NONE")]
        public string setting_value { get; set; }

        /// <summary>是否啟用 (0/1)</summary>
        [JsonPropertyName("is_enabled")]
        [Description("VARCHAR,5,NONE")]
        public string is_enabled { get; set; } = "1";

        /// <summary>建立時間</summary>
        [JsonPropertyName("created_at")]
        [Description("DATETIME,50,NONE")]
        public string created_at { get; set; }

        /// <summary>更新時間</summary>
        [JsonPropertyName("updated_at")]
        [Description("DATETIME,50,NONE")]
        public string updated_at { get; set; }

        /// <summary>建立者</summary>
        [JsonPropertyName("created_by")]
        [Description("VARCHAR,50,NONE")]
        public string created_by { get; set; }

        /// <summary>更新者</summary>
        [JsonPropertyName("updated_by")]
        [Description("VARCHAR,50,NONE")]
        public string updated_by { get; set; }
    }
}