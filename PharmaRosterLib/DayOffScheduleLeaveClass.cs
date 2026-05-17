using System.ComponentModel;
using System.Text.Json.Serialization;

namespace PharmaRosterLib
{
    /// <summary>
    /// 排休表單專用請假資料 (DayOff Schedule Leave)。
    /// </summary>
    /// <remarks>
    /// 此資料與一般 <c>leave_requests</c> 分開管理，用於指定某一張排休表單中，
    /// 人員於特定日期範圍與時段已請假，因此不可再選擇對應排休時段，並可於後續名額計算中扣減每日可休人數。
    /// </remarks>
    [Description("dayoff_schedule_leave")]
    public class DayOffScheduleLeaveClass
    {
        /// <summary>唯一識別碼。</summary>
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>所屬排休表單 GUID。</summary>
        [JsonPropertyName("form_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string form_guid { get; set; }

        /// <summary>請假人員 GUID。</summary>
        [JsonPropertyName("staff_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string staff_guid { get; set; }

        /// <summary>請假人員工號。</summary>
        [JsonPropertyName("staff_id")]
        [Description("VARCHAR,50,INDEX")]
        public string staff_id { get; set; }

        /// <summary>請假人員姓名。</summary>
        [JsonPropertyName("staff_name")]
        [Description("VARCHAR,100,INDEX")]
        public string staff_name { get; set; }

        /// <summary>
        /// 假別代碼：LONG_LEAVE / MARRIAGE / FUNERAL / SPECIAL / OTHER。
        /// </summary>
        [JsonPropertyName("leave_type")]
        [Description("VARCHAR,30,INDEX")]
        public string leave_type { get; set; }

        /// <summary>請假開始日期，格式 yyyy-MM-dd。</summary>
        [JsonPropertyName("start_date")]
        [Description("DATETIME,0,INDEX")]
        public string start_date { get; set; }

        /// <summary>請假結束日期，格式 yyyy-MM-dd。</summary>
        [JsonPropertyName("end_date")]
        [Description("DATETIME,0,INDEX")]
        public string end_date { get; set; }

        /// <summary>
        /// 請假時段：FULL / AM / PM。
        /// </summary>
        [JsonPropertyName("leave_period")]
        [Description("VARCHAR,10,INDEX")]
        public string leave_period { get; set; }

        /// <summary>請假原因或備註。</summary>
        [JsonPropertyName("reason")]
        [Description("TEXT,50,NONE")]
        public string reason { get; set; }

        /// <summary>
        /// 資料來源：MANUAL / LEAVE_REQUEST。
        /// </summary>
        [JsonPropertyName("source_type")]
        [Description("VARCHAR,30,INDEX")]
        public string source_type { get; set; }

        /// <summary>來源資料 GUID；若由一般 leaveRequest 匯入，記錄來源 leave_requests.GUID。</summary>
        [JsonPropertyName("source_ref_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string source_ref_guid { get; set; }

        /// <summary>建立時間。</summary>
        [JsonPropertyName("created_at")]
        [Description("DATETIME,0,NONE")]
        public string created_at { get; set; }

        /// <summary>更新時間。</summary>
        [JsonPropertyName("updated_at")]
        [Description("DATETIME,0,NONE")]
        public string updated_at { get; set; }

        /// <summary>人員基本資料；非資料表欄位，供 API 回傳顯示使用。</summary>
        [JsonPropertyName("staff_info")]
        public StaffClass staff_info { get; set; } = new StaffClass();
    }
}
