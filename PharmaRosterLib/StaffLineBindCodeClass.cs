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
    /// 人員 LINE 綁定碼資料表
    /// </summary>
    [Description("staff_line_bind_code")]
    public class StaffLineBindCodeClass
    {
        /// <summary>唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>綁定碼 (唯一)</summary>
        [JsonPropertyName("bind_code")]
        [Description("VARCHAR,20,UNIQUE")]
        public string bind_code { get; set; }

        /// <summary>人員編號</summary>
        [JsonPropertyName("staff_id")]
        [Description("VARCHAR,50,INDEX")]
        public string staff_id { get; set; }

        /// <summary>到期時間</summary>
        [JsonPropertyName("expire_at")]
        [Description("DATETIME,50,INDEX")]
        public string expire_at { get; set; }

        /// <summary>是否已使用 (0/1)</summary>
        [JsonPropertyName("is_used")]
        [Description("VARCHAR,5,NONE")]
        public string is_used { get; set; } = "0";

        /// <summary>使用時間</summary>
        [JsonPropertyName("used_at")]
        [Description("DATETIME,50,NONE")]
        public string used_at { get; set; }

        /// <summary>使用時的 LINE userId</summary>
        [JsonPropertyName("used_line_user_id")]
        [Description("VARCHAR,100,NONE")]
        public string used_line_user_id { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("created_at")]
        [Description("DATETIME,50,NONE")]
        public string created_at { get; set; }

        /// <summary>建立者</summary>
        [JsonPropertyName("created_by")]
        [Description("VARCHAR,50,NONE")]
        public string created_by { get; set; }
    }
}
