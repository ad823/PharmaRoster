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
    /// 請假 / 特例 (Leave Request)
    /// </summary>
    [Description("leave_requests")]
    public class LeaveRequestClass
    {
        /// <summary>唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>人員 GUID</summary>
        [JsonPropertyName("staff_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string staff_guid { get; set; }

        /// <summary>開始日期 (yyyy-MM-dd)</summary>
        [JsonPropertyName("start_date")]
        [Description("DATETIME,50,NONE")]
        public string start_date { get; set; }

        /// <summary>結束日期 (yyyy-MM-dd)</summary>
        [JsonPropertyName("end_date")]
        [Description("DATETIME,50,NONE")]
        public string end_date { get; set; }

        /// <summary>原因</summary>
        [JsonPropertyName("reason")]
        [Description("TEXT,50,NONE")]
        public string reason { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("created_at")]
        [Description("DATETIME,50,NONE")]
        public string created_at { get; set; }

        [JsonPropertyName("staff_info")]
        public StaffClass staff_info { get; set; } = new StaffClass();
    }
}

