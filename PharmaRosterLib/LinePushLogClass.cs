using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PharmaRosterLib
{
    /// <summary>
    /// LINE 推播紀錄資料表
    /// </summary>
    [Description("line_push_log")]
    public class LinePushLogClass
    {
        /// <summary>唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>目標類型 (staff/group)</summary>
        [JsonPropertyName("target_type")]
        [Description("VARCHAR,20,INDEX")]
        public string target_type { get; set; }

        /// <summary>目標識別 (staff_id 或 group_guid)</summary>
        [JsonPropertyName("target_id")]
        [Description("VARCHAR,50,INDEX")]
        public string target_id { get; set; }

        /// <summary>人員編號</summary>
        [JsonPropertyName("staff_id")]
        [Description("VARCHAR,50,INDEX")]
        public string staff_id { get; set; }

        /// <summary>LINE 使用者 ID</summary>
        [JsonPropertyName("line_user_id")]
        [Description("VARCHAR,100,NONE")]
        public string line_user_id { get; set; }

        /// <summary>訊息內容</summary>
        [JsonPropertyName("message")]
        [Description("VARCHAR,2000,NONE")]
        public string message { get; set; }

        /// <summary>送出狀態 (success/fail)</summary>
        [JsonPropertyName("send_status")]
        [Description("VARCHAR,20,INDEX")]
        public string send_status { get; set; }

        /// <summary>HTTP 狀態碼</summary>
        [JsonPropertyName("http_status")]
        [Description("VARCHAR,10,NONE")]
        public string http_status { get; set; }

        /// <summary>錯誤訊息</summary>
        [JsonPropertyName("error_message")]
        [Description("VARCHAR,1000,NONE")]
        public string error_message { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("created_at")]
        [Description("DATETIME,50,INDEX")]
        public string created_at { get; set; }
    }
}
