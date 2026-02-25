using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace PharmaRosterLib
{
    /// <summary>
    /// staff_dayoff_option 的異動歷程紀錄
    /// </summary>
    [Description("staff_dayoff_option_log")]
    public class StaffDayOffOptionLogClass
    {
        /// <summary>Log GUID</summary>
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>表單 GUID</summary>
        [Description("VARCHAR,50,INDEX")]
        public string form_guid { get; set; }

        /// <summary>Option GUID</summary>
        [Description("VARCHAR,50,INDEX")]
        public string option_guid { get; set; }

        /// <summary>Item GUID</summary>
        [Description("VARCHAR,50,INDEX")]
        public string item_guid { get; set; }

        /// <summary>Staff GUID</summary>
        [Description("VARCHAR,50,INDEX")]
        public string staff_guid { get; set; }

        /// <summary>姓名</summary>
        [JsonPropertyName("staff_name")]
        [Description("VARCHAR,100,INDEX")]
        public string staff_name { get; set; }

        /// <summary>Staff 工號</summary>
        [Description("VARCHAR,50,INDEX")]
        public string staff_id { get; set; }

        /// <summary>
        /// 階段：weekly / annual / none
        /// </summary>
        [Description("VARCHAR,20,NONE")]
        public string stage { get; set; }

        /// <summary>
        /// 動作：FULL / HALF_AM / HALF_PM / CANCEL
        /// </summary>
        [Description("VARCHAR,20,INDEX")]
        public string action { get; set; }

        /// <summary>此次選擇的 off_date（yyyy-MM-dd），CANCEL 可為空</summary>
        [Description("DATETIME,0,NONE")]
        public string off_date { get; set; }

        // =========================
        // before snapshot
        // =========================
        [Description("VARCHAR,5,NONE")]
        public string before_selected_full { get; set; }

        [Description("VARCHAR,5,NONE")]
        public string before_selected_half_am { get; set; }

        [Description("VARCHAR,5,NONE")]
        public string before_selected_half_pm { get; set; }

        [Description("DATETIME,0,NONE")]
        public string before_date { get; set; }

        // =========================
        // after snapshot
        // =========================
        [Description("VARCHAR,5,NONE")]
        public string after_selected_full { get; set; }

        [Description("VARCHAR,5,NONE")]
        public string after_selected_half_am { get; set; }

        [Description("VARCHAR,5,NONE")]
        public string after_selected_half_pm { get; set; }

        [Description("DATETIME,0,NONE")]
        public string after_date { get; set; }

        /// <summary>備註（可放錯誤訊息或補充）</summary>
        [Description("VARCHAR,500,NONE")]
        public string note { get; set; }

        /// <summary>建立時間</summary>
        [Description("DATETIME,0,NONE")]
        public string created_at { get; set; }
    }
}