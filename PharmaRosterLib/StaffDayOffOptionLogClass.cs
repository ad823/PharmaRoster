using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace PharmaRosterLib
{
    /// <summary>
    /// staff_dayoff_option 的填寫/異動歷程紀錄
    /// </summary>
    [Description("staff_dayoff_option_log")]
    public class StaffDayOffOptionLogClass
    {
        /// <summary>Log GUID</summary>
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>所屬表單 GUID</summary>
        [Description("VARCHAR,50,INDEX")]
        public string form_guid { get; set; }

        /// <summary>所屬表單名稱（冗餘欄位，方便查詢與報表）</summary>
        [Description("VARCHAR,150,INDEX")]
        public string form_name { get; set; }

        /// <summary>人員 GUID（方便與 staff_dayoff_option join）</summary>
        [Description("VARCHAR,50,INDEX")]
        public string staff_guid { get; set; }

        /// <summary>人員工號（你前端登入用 staff_id）</summary>
        [Description("VARCHAR,50,INDEX")]
        public string staff_id { get; set; }

        /// <summary>排休列 item_guid</summary>
        [Description("VARCHAR,50,INDEX")]
        public string item_guid { get; set; }

        /// <summary>option_guid（staff_dayoff_option.GUID，可為空：例如首次建立前記錄）</summary>
        [Description("VARCHAR,50,INDEX")]
        public string option_guid { get; set; }

        /// <summary>
        /// 流程階段：weekly / annual / admin（管理端操作）
        /// </summary>
        [Description("VARCHAR,20,INDEX")]
        public string stage { get; set; }

        /// <summary>
        /// 異動類型：
        /// select_full / select_half_am / select_half_pm / cancel_selection /
        /// set_force_ff / cancel_force_ff /
        /// set_forbidden / cancel_forbidden /
        /// update_suggested_dates / update_assigned_shift
        /// </summary>
        [Description("VARCHAR,50,INDEX")]
        public string action { get; set; }

        /// <summary>異動原因（可選：例如前端顯示、管理端備註）</summary>
        [Description("VARCHAR,300,NONE")]
        public string reason { get; set; }

        /// <summary>
        /// 變更前 option 狀態（JSON：StaffDayOffOptionClass 的精簡快照）
        /// </summary>
        [Description("VARCHAR,2000,NONE")]
        public string before_json { get; set; }

        /// <summary>
        /// 變更後 option 狀態（JSON：StaffDayOffOptionClass 的精簡快照）
        /// </summary>
        [Description("VARCHAR,2000,NONE")]
        public string after_json { get; set; }

        /// <summary>
        /// 異動來源：frontend / admin / system
        /// </summary>
        [Description("VARCHAR,20,NONE")]
        public string source { get; set; }

        /// <summary>
        /// 操作者（可用 staff_id 或 account_id；若是系統自動則填 SYSTEM）
        /// </summary>
        [Description("VARCHAR,50,INDEX")]
        public string operator_id { get; set; }

        /// <summary>建立時間</summary>
        [Description("DATETIME,0,INDEX")]
        public string created_at { get; set; }

        /// <summary>（可選）IP</summary>
        [Description("VARCHAR,50,NONE")]
        public string client_ip { get; set; }

        /// <summary>（可選）UserAgent</summary>
        [Description("VARCHAR,300,NONE")]
        public string user_agent { get; set; }
    }
}