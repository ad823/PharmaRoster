using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PharmaRosterLib
{
    /// <summary>
    /// staff 是否輪到排休填寫（週休/特休）回傳資料
    /// </summary>
    public class DayOffCheckStaffInRoundResponse
    {
        /// <summary>員工 GUID/識別碼</summary>
        public string staff_id { get; set; }

        /// <summary>目前 active 的排休表單 GUID（若無則為 null）</summary>
        public string active_form_guid { get; set; }

        /// <summary>目前流程階段：none / weekly / annual</summary>
        public string stage { get; set; } = "none";

        /// <summary>目前流程階段顯示文字：無 / 週休 / 特休</summary>
        public string stage_name { get; set; } = "無";

        /// <summary>是否可填寫（更直覺，等同 is_in_round）</summary>
        public bool can_write { get; set; } = false;

        /// <summary>是否在目前輪次（是否輪到）</summary>
        public bool is_in_round { get; set; } = false;

        /// <summary>目前開放組別 GUID（open group）</summary>
        public string open_group_guid { get; set; }

        /// <summary>目前開放組別排序序號（int）</summary>
        public int open_group_order_index { get; set; }

        /// <summary>目前開放組別名稱（若無名稱欄位則為 null）</summary>
        public string open_group_name { get; set; }

        /// <summary>staff 所屬組別 GUID</summary>
        public string staff_group_guid { get; set; }

        /// <summary>staff 所屬組別排序序號（int）</summary>
        public int staff_group_order_index { get; set; }

        /// <summary>staff 所屬組別名稱（若無名稱欄位則為 null）</summary>
        public string staff_group_name { get; set; }

        /// <summary>下一組 GUID（若已是最後一組則為 null）</summary>
        public string next_group_guid { get; set; }

        /// <summary>下一組排序序號（若無則為 0）</summary>
        public int next_group_order_index { get; set; }

        /// <summary>下一組名稱（若無名稱欄位則為 null）</summary>
        public string next_group_name { get; set; }

        /// <summary>還差幾組才輪到（0 表示已輪到或已超過）</summary>
        public int remain_groups_to_open { get; set; }

        /// <summary>給前端直接顯示的訊息</summary>
        public string message { get; set; }

        /// <summary>進度口語訊息（更短更口語）</summary>
        public string progress_message { get; set; }
    }

    public class DayOffCurrentOpenGroupResponse
    {
        public string stage { get; set; }
        public DayOffGroupClass open_group { get; set; }
        public Dictionary<string, int> groups_status_summary { get; set; }
    }
    /// <summary>
    /// 取得排休流程進度 API 回傳資料結構
    /// </summary>
    public class DayOffFlowProgressResponse
    {
        public string form_guid { get; set; }
        public string stage { get; set; }  // none / weekly / annual / finished

        public DayOffGroupClass open_group { get; set; }

        public int total_groups { get; set; }

        public Dictionary<string, int> groups_status_summary { get; set; } = new Dictionary<string, int>();

        public double weekly_progress_percent { get; set; }
        public double annual_progress_percent { get; set; }
        public double overall_progress_percent { get; set; }
    }

    /// <summary>
    /// 查詢目前正在排休流程中的表單回傳資料（Active Form）
    /// </summary>
    public class DayOffActiveFormResponse
    {
        /// <summary>
        /// 目前正在排休的表單 GUID
        /// <para>若目前無任何表單排休中，則為 null</para>
        /// </summary>
        public string active_form_guid { get; set; }

        /// <summary>
        /// 目前流程階段
        /// <para>"none"：沒有任何表單排休中 / 尚未初始化 / 已結束</para>
        /// <para>"weekly"：週休階段（存在 status=1）</para>
        /// <para>"annual"：特休階段（存在 status=3）</para>
        /// </summary>
        public string stage { get; set; } = "none";

        /// <summary>
        /// 目前開放可填寫的組別（精簡版）
        /// <para>週休階段 → status=1 的組別</para>
        /// <para>特休階段 → status=3 的組別</para>
        /// <para>若 stage=none 則為 null</para>
        /// </summary>
        public DayOffGroupClass open_group { get; set; }

        /// <summary>
        /// 該 active_form_guid 底下的狀態統計
        /// </summary>
        public DayOffGroupStatusSummary groups_status_summary { get; set; }
    }
    /// <summary>
    /// 排休組別狀態統計（強型別）
    /// </summary>
    public class DayOffGroupStatusSummary
    {
        public int status_0 { get; set; }
        public int status_1 { get; set; }
        public int status_2 { get; set; }
        public int status_3 { get; set; }
        public int status_4 { get; set; }

        public static DayOffGroupStatusSummary FromGroups(List<DayOffGroupClass> groups)
        {
            if (groups == null) groups = new List<DayOffGroupClass>();
            return new DayOffGroupStatusSummary
            {
                status_0 = groups.Count(x => x.status == "0"),
                status_1 = groups.Count(x => x.status == "1"),
                status_2 = groups.Count(x => x.status == "2"),
                status_3 = groups.Count(x => x.status == "3"),
                status_4 = groups.Count(x => x.status == "4"),
            };
        }
    }




    /// <summary>
    /// 排休通知/排休排序用的「組別」資料。
    /// <para>用途：定義某一張排休表單(form_guid)中，組別的排序順序(order_index)。</para>
    /// <para>members 為非 SQL 欄位，用於回傳組內成員清單。</para>
    /// </summary>
    [Description("dayoff_group")]
    public class DayOffGroupClass
    {
        /// <summary>
        /// 組別唯一識別碼 (GUID)。
        /// <para>資料表主鍵。</para>
        /// </summary>
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>
        /// 所屬排休表單 GUID。
        /// </summary>
        [Description("VARCHAR,50,INDEX")]
        public string form_guid { get; set; }

        /// <summary>
        /// 組別排序序號（越小越前）。
        /// </summary>
        [Description("VARCHAR,10,NONE")]
        public string order_index { get; set; }

        /// <summary>
        /// 組別狀態（VARCHAR）。
        /// <para>
        /// "0"=未輪到(鎖定)
        /// "1"=可填寫週休
        /// "2"=週休填寫完成
        /// "3"=可填寫特休
        /// "4"=特休填寫完成
        /// </para>
        /// </summary>
        [Description("VARCHAR,10,NONE")]
        public string status { get; set; } = "0";

        /// <summary>
        /// 狀態改變時間（最後一次狀態變動）。
        /// </summary>
        [Description("DATETIME,20,NONE")]
        public string status_changed_at { get; set; }

        /// <summary>
        /// 進入「可填寫週休」的時間（只寫第一次）。
        /// </summary>
        [Description("DATETIME,20,NONE")]
        public string weekly_fill_start_at { get; set; }

        /// <summary>
        /// 「週休填寫完成」時間（只寫第一次）。
        /// </summary>
        [Description("DATETIME,20,NONE")]
        public string weekly_completed_at { get; set; }

        /// <summary>
        /// 進入「可填寫特休」的時間（只寫第一次）。
        /// </summary>
        [Description("DATETIME,20,NONE")]
        public string annual_fill_start_at { get; set; }

        /// <summary>
        /// 「特休填寫完成」時間（只寫第一次）。
        /// </summary>
        [Description("DATETIME,20,NONE")]
        public string annual_completed_at { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Description("DATETIME,20,NONE")]
        public string created_at { get; set; }

        /// <summary>
        /// 更新時間。
        /// </summary>
        [Description("DATETIME,20,NONE")]
        public string updated_at { get; set; }

        /// <summary>
        /// 組內成員清單（非 SQL 欄位）。
        /// </summary>
        public List<DayOffGroupMemberClass> members { get; set; } = new List<DayOffGroupMemberClass>();

        /// <summary>
        /// 設定狀態並自動填入各階段時間（每個階段只寫第一次，避免被覆寫）。
        /// </summary>
        public void SetStatusWithTime(string newStatus)
        {
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            this.status = newStatus;
            this.status_changed_at = now;
            this.updated_at = now;

            // 各階段只寫入第一次
            if (newStatus == "1" && string.IsNullOrWhiteSpace(this.weekly_fill_start_at))
                this.weekly_fill_start_at = now;

            if (newStatus == "2" && string.IsNullOrWhiteSpace(this.weekly_completed_at))
                this.weekly_completed_at = now;

            if (newStatus == "3" && string.IsNullOrWhiteSpace(this.annual_fill_start_at))
                this.annual_fill_start_at = now;

            if (newStatus == "4" && string.IsNullOrWhiteSpace(this.annual_completed_at))
                this.annual_completed_at = now;
        }
    }


    /// <summary>
    /// 排休通知/排休排序用的「組別成員」資料。
    /// <para>用途：定義人員(staff_guid)在某個組別(group_guid)中的排序(order_index)。</para>
    /// <para>同一張表單(form_guid)可包含多個組別，每個組別內包含多位人員。</para>
    /// </summary>
    [Description("dayoff_group_member")]
    public class DayOffGroupMemberClass
    {
        /// <summary>
        /// 組別成員唯一識別碼 (GUID)。
        /// <para>資料表主鍵。</para>
        /// </summary>
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>
        /// 所屬排休表單 GUID。
        /// <para>用於加速查詢某張表單的所有組別成員。</para>
        /// </summary>
        [Description("VARCHAR,50,INDEX")]
        public string form_guid { get; set; }

        /// <summary>
        /// 所屬組別 GUID。
        /// <para>對應 dayOff_group.GUID。</para>
        /// </summary>
        [Description("VARCHAR,50,INDEX")]
        public string group_guid { get; set; }

        /// <summary>
        /// 人員 GUID。
        /// <para>對應人員主檔 staff GUID。</para>
        /// </summary>
        [Description("VARCHAR,50,INDEX")]
        public string staff_guid { get; set; }

        /// <summary>
        /// 人員編號。
        /// <para>常用於顯示、搜尋、比對資料。</para>
        /// </summary>
        [JsonPropertyName("staff_id")]
        [Description("VARCHAR,50,INDEX")]
        public string staff_id { get; set; }

        /// <summary>
        /// 人員姓名。
        /// <para>常用於顯示及模糊查詢。</para>
        /// </summary>
        [JsonPropertyName("staff_name")]
        [Description("VARCHAR,100,INDEX")]
        public string staff_name { get; set; }

        /// <summary>
        /// 組內排序序號。
        /// <para>數值越小越前面，例如 1,2,3…</para>
        /// <para>用於排休通知順序、或前端拖曳排序後更新。</para>
        /// </summary>
        [Description("VARCHAR,10,NONE")]
        public string order_index { get; set; }

        // =========================================================
        // ✅ 排假完成狀態（SQL 欄位）
        // =========================================================

        /// <summary>
        /// 是否完成週休排假。
        /// <para>建議值： "true"=已完成, "false"=未完成。</para>
        /// </summary>
        [Description("VARCHAR,5,NONE")]
        public string is_weekoff_completed { get; set; }

        /// <summary>
        /// 週休排假完成時間。
        /// <para>格式：yyyy-MM-dd HH:mm:ss</para>
        /// </summary>
        [Description("DATETIME,20,NONE")]
        public string weekoff_completed_at { get; set; }

        /// <summary>
        /// 是否完成特休排假。
        /// <para>建議值： "true"=已完成, "false"=未完成。</para>
        /// </summary>
        [Description("VARCHAR,5,NONE")]
        public string is_annualleave_completed { get; set; }

        /// <summary>
        /// 特休排假完成時間。
        /// <para>格式：yyyy-MM-dd HH:mm:ss</para>
        /// </summary>
        [Description("DATETIME,20,NONE")]
        public string annualleave_completed_at { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Description("DATETIME,20,NONE")]
        public string created_at { get; set; }

        /// <summary>
        /// 更新時間。
        /// </summary>
        [Description("DATETIME,20,NONE")]
        public string updated_at { get; set; }
    }




}
