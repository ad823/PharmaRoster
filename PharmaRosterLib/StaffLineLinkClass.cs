using Basic;
using SQLUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace PharmaRosterLib
{
    /// <summary>
    /// 人員 LINE 綁定關係表
    /// </summary>
    [Description("staff_line_link")]
    public class StaffLineLinkClass
    {
        /// <summary>唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>人員編號 (唯一)</summary>
        [JsonPropertyName("staff_id")]
        [Description("VARCHAR,50,UNIQUE")]
        public string staff_id { get; set; }

        /// <summary>LINE 使用者 ID</summary>
        [JsonPropertyName("line_user_id")]
        [Description("VARCHAR,100,INDEX")]
        public string line_user_id { get; set; }

        /// <summary>是否啟用 (0/1)</summary>
        [JsonPropertyName("is_enabled")]
        [Description("VARCHAR,5,NONE")]
        public string is_enabled { get; set; } = "1";

        /// <summary>首次綁定時間</summary>
        [JsonPropertyName("linked_at")]
        [Description("DATETIME,50,NONE")]
        public string linked_at { get; set; }

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
