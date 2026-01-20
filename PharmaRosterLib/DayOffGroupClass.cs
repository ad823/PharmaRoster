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
        /// <para>用於關聯 dayOff_form 或對應表單資料。</para>
        /// </summary>
        [Description("VARCHAR,50,INDEX")]
        public string form_guid { get; set; }

        /// <summary>
        /// 組別排序序號。
        /// <para>數值越小越前面，例如 1,2,3…</para>
        /// </summary>
        [Description("VARCHAR,10,NONE")]
        public string order_index { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Description("DATETIME,0,NONE")]
        public string created_at { get; set; }

        /// <summary>
        /// 更新時間。
        /// </summary>
        [Description("DATETIME,0,NONE")]
        public string updated_at { get; set; }

        /// <summary>
        /// 組內成員清單（非 SQL 欄位）。
        /// <para>回傳給前端用於顯示、拖曳排序、調整組內人員順序。</para>
        /// </summary>
        public List<DayOffGroupMemberClass> members { get; set; } = new List<DayOffGroupMemberClass>();
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
        [Description("DATETIME,0,NONE")]
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
        [Description("DATETIME,0,NONE")]
        public string annualleave_completed_at { get; set; }

        /// <summary>
        /// 建立時間。
        /// </summary>
        [Description("DATETIME,0,NONE")]
        public string created_at { get; set; }

        /// <summary>
        /// 更新時間。
        /// </summary>
        [Description("DATETIME,0,NONE")]
        public string updated_at { get; set; }
    }




}
