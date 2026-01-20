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
    [Description("dayoff_schedule_form")]
    public class DayOffScheduleFormClass
    {
        /// <summary>表單唯一識別碼</summary>
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>表單名稱</summary>
        [Description("VARCHAR,150,INDEX")]
        public string form_name { get; set; }

        // =========================================================
        // ✅ 表單流程控制（新增 SQL 欄位）
        // =========================================================

        /// <summary>
        /// 是否開放進入「週休選擇」流程 (true / false)。
        /// <para>true：前端可進入週休選擇頁面並進行排假</para>
        /// <para>false：前端不可進入或僅可檢視</para>
        /// </summary>
        [Description("VARCHAR,5,NONE")]
        public string enable_weekoff_selection { get; set; }


        /// <summary>週休選擇進入時間</summary>
        [Description("DATETIME,0,NONE")]
        public string weekoff_selection_update_at { get; set; }

        /// <summary>
        /// 是否開放進入「特休選擇」流程 (true / false)。
        /// <para>true：前端可進入特休選擇頁面並進行排假</para>
        /// <para>false：前端不可進入或僅可檢視</para>
        /// </summary>
        [Description("VARCHAR,5,NONE")]
        public string enable_annualleave_selection { get; set; }

        /// <summary>週休選擇進入時間</summary>
        [Description("DATETIME,0,NONE")]
        public string annualleave_selection_update_at { get; set; }

        /// <summary>
        /// 是否完成鎖定 (true / false)。
        /// <para>true：整張表單流程完成並鎖定，不可再異動</para>
        /// <para>false：仍可繼續調整週休/特休/組別/名額等</para>
        /// </summary>
        [Description("VARCHAR,5,NONE")]
        public string is_completed_locked { get; set; }

        /// <summary>完成鎖定進入時間</summary>
        [Description("DATETIME,0,NONE")]
        public string is_completed_locked_update_at { get; set; }

        /// <summary>建立時間</summary>
        [Description("DATETIME,0,NONE")]
        public string created_at { get; set; }

        /// <summary>更新時間</summary>
        [Description("DATETIME,0,NONE")]
        public string updated_at { get; set; }

        /// <summary>表單包含的日期清單（非資料表欄位）</summary>
        public List<DayOffScheduleDayClass> days { get; set; } = new List<DayOffScheduleDayClass>();
    }

    [Description("dayoff_schedule_day")]
    public class DayOffScheduleDayClass
    {
        /// <summary>日期唯一識別碼</summary>
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>所屬表單 GUID</summary>
        [Description("VARCHAR,50,INDEX")]
        public string form_guid { get; set; }

        /// <summary>日期 (yyyy-MM-dd)</summary>
        [Description("DATETIME,0,INDEX")]
        public string date { get; set; }

        /// <summary>上午可休假人數</summary>
        [Description("VARCHAR,10,NONE")]
        public string am_max_dayoff_count { get; set; }

        /// <summary>下午可休假人數</summary>
        [Description("VARCHAR,10,NONE")]
        public string pm_max_dayoff_count { get; set; }

        public string am_dayoff_count { get; set; }
        public string pm_dayoff_count { get; set; }

        /// <summary>建立時間</summary>
        [Description("DATETIME,0,NONE")]
        public string created_at { get; set; }

        /// <summary>更新時間</summary>
        [Description("DATETIME,0,NONE")]
        public string updated_at { get; set; }

        /// <summary>當日所有排休列（一人一列，非資料表欄位）</summary>
        public List<DayOffScheduleItemClass> items { get; set; } = new List<DayOffScheduleItemClass>();
    }

    [Description("dayoff_schedule_item")]
    public class DayOffScheduleItemClass
    {
        /// <summary>排休列唯一識別碼</summary>
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>所屬表單 GUID</summary>
        [Description("VARCHAR,50,INDEX")]
        public string form_guid { get; set; }

        /// <summary>所屬日期 GUID</summary>
        [Description("VARCHAR,50,INDEX")]
        public string day_guid { get; set; }

        /// <summary>所屬排休列 GUID</summary>
        [Description("VARCHAR,50,INDEX")]
        public string option_guid { get; set; }

        /// <summary>此排休列代表的日期 (Row Date)</summary>
        [Description("DATETIME,0,INDEX")]
        public string date { get; set; }

        /// <summary>是否為特殊節日</summary>
        [Description("VARCHAR,10,NONE")]
        public string is_special_day { get; set; } = "false";

        /// <summary>人員 GUID</summary>
        [Description("VARCHAR,50,INDEX")]
        public string staff_guid { get; set; }

        /// <summary>人員工號 / ID</summary>
        [Description("VARCHAR,50,INDEX")]
        public string staff_id { get; set; }

        /// <summary>人員姓名 / ID</summary>
        [Description("VARCHAR,100,INDEX")]
        public string staff_name { get; set; }

        /// <summary>姓名代號</summary>
        [Description("VARCHAR,10,NONE")]
        public string staff_simple_name { get; set; }

        /// <summary>
        /// 已選擇的放假類型  
        /// 空字串 = 尚未選擇  
        /// FULL / HALF_AM / HALF_PM
        /// </summary>
        [Description("VARCHAR,20,NONE")]
        public string selected_dayoff_type { get; set; }

        [Description("VARCHAR,10,NONE")]
        public string position { get; set; }
        /// <summary>
        /// 需求班次明細 JSON (直接存 WorkShiftRequirementClass 的序列化字串)
        /// </summary>
        [JsonPropertyName("shift_requirement")]
        [Description("VARCHAR,300,NONE")]
        public string shift_requirement { get; set; }

        /// <summary>建立時間</summary>
        [Description("DATETIME,0,NONE")]
        public string created_at { get; set; }

        /// <summary>更新時間</summary>
        [Description("DATETIME,0,NONE")]
        public string updated_at { get; set; }

        /// <summary>人員資訊（非資料表欄位）</summary>
        public StaffClass staff { get; set; } = new StaffClass();

        /// <summary>
        /// 放假選擇狀態（規則 / 狀態 / 管理端禁止，非資料表欄位）
        /// </summary>
        public StaffDayOffOptionClass option { get; set; } = new StaffDayOffOptionClass();

        /// <summary>
        /// 對應需求班次明細 (正反序列化 JSON)  
        /// - Getter: 將 <c>req_shift_detail_key</c> JSON 反序列化成物件。  
        /// - Setter: 將物件序列化後存回 <c>req_shift_detail_key</c>。  
        /// </summary>
        [JsonPropertyName("workShiftRequirement")]
        public WorkShiftRequirementClass workShiftRequirement
        {
            get
            {
                if (string.IsNullOrWhiteSpace(shift_requirement)) return null;
                try
                {
                    return JsonSerializer.Deserialize<WorkShiftRequirementClass>(shift_requirement);
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                if (value == null)
                {
                    shift_requirement = null;
                }
                else
                {
                    shift_requirement = JsonSerializer.Serialize(value);
                }
            }
        }
    }
    [Description("staff_dayoff_option")]
    public class StaffDayOffOptionClass
    {
        /// <summary>Option 唯一識別碼</summary>
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>所屬表單 GUID</summary>
        [Description("VARCHAR,50,INDEX")]
        public string form_guid { get; set; }

        /// <summary>所屬排休列 GUID（回指 DayOffScheduleItemClass）</summary>
        [Description("VARCHAR,50,INDEX")]
        public string item_guid { get; set; }

        /// <summary>人員 GUID</summary>
        [Description("VARCHAR,50,INDEX")]
        public string staff_guid { get; set; }

        /// <summary>
        /// 使用者實際選擇的放假日 (yyyy-MM-dd)
        /// 尚未選擇為空字串
        /// </summary>
        [Description("DATETIME,0,NONE")]
        public string date { get; set; }

        /// <summary>
        /// 系統建議 / 可選的放假日清單（JSON 字串）
        /// </summary>
        [Description("VARCHAR,500,NONE")]
        public string suggested_dates { get; set; }

        /// <summary>
        /// 是否為任選日期
        /// true = 由使用者任選日期（不受 suggested_dates 限制）
        /// </summary>
        [Description("VARCHAR,5,NONE")]
        public string is_any_date { get; set; } = "false";

        /// <summary>
        /// 當日被指派的班別
        /// DAY / EVENING / NIGHT / OFF
        /// </summary>
        [Description("VARCHAR,30,NONE")]
        public string assigned_shift { get; set; }

        /// <summary>是否可選全天假</summary>
        [Description("VARCHAR,5,NONE")]
        public string can_full { get; set; }

        /// <summary>是否可選半天假（上午）</summary>
        [Description("VARCHAR,5,NONE")]
        public string can_half_am { get; set; }

        /// <summary>是否可選半天假（下午）</summary>
        [Description("VARCHAR,5,NONE")]
        public string can_half_pm { get; set; }

        /// <summary>
        /// 是否被管理端禁止選擇
        /// true = 一律不可選
        /// </summary>
        [Description("VARCHAR,5,NONE")]
        public string is_forbidden { get; set; }

        /// <summary>是否已選全天假</summary>
        [Description("VARCHAR,5,NONE")]
        public string selected_full { get; set; }

        /// <summary>是否已選半天假（上午）</summary>
        [Description("VARCHAR,5,NONE")]
        public string selected_half_am { get; set; }

        /// <summary>是否已選半天假（下午）</summary>
        [Description("VARCHAR,5,NONE")]
        public string selected_half_pm { get; set; }

        // =========================================================
        // 非資料表欄位（程式端使用）
        // =========================================================

        /// <summary>
        /// 建議 / 可選放假日清單（JSON 反序列化）
        /// </summary>
        public List<string> suggested_dates_list
        {
            get
            {
                if (string.IsNullOrWhiteSpace(suggested_dates))
                    return new List<string>();
                try
                {
                    return JsonSerializer.Deserialize<List<string>>(suggested_dates);
                }
                catch
                {
                    return new List<string>();
                }
            }
            set
            {
                if (value == null || value.Count == 0)
                    suggested_dates = "[]";
                else
                    suggested_dates = JsonSerializer.Serialize(value);
            }
        }

        // =========================================================
        // 核心互斥與狀態控制
        // =========================================================

        /// <summary>清空所有選擇</summary>
        public void ClearSelection()
        {
            selected_full = "false";
            selected_half_am = "false";
            selected_half_pm = "false";
            date = "";
        }

        /// <summary>選擇全天假（會自動互斥）</summary>
        public void SelectFullDay(string offDate)
        {
            selected_full = "true";
            selected_half_am = "false";
            selected_half_pm = "false";
            date = offDate;
        }

        /// <summary>選擇上午半天假（會自動互斥）</summary>
        public void SelectHalfAM(string offDate)
        {
            selected_full = "false";
            selected_half_am = "true";
            selected_half_pm = "false";
            date = offDate;
        }

        /// <summary>選擇下午半天假（會自動互斥）</summary>
        public void SelectHalfPM(string offDate)
        {
            selected_full = "false";
            selected_half_am = "false";
            selected_half_pm = "true";
            date = offDate;
        }

        // =========================================================
        // 安全選擇（含規則 / 禁止）
        // =========================================================

        public bool TrySelectFullDay(string offDate, out string error)
        {
            error = "";

            if (is_forbidden == "true")
            {
                error = "此放假選項已被管理端禁止";
                return false;
            }
            if (can_full != "true")
            {
                error = "不可選擇全天假";
                return false;
            }

            SelectFullDay(offDate);
            return true;
        }

        public bool TrySelectHalfAM(string offDate, out string error)
        {
            error = "";

            if (is_forbidden == "true")
            {
                error = "此放假選項已被管理端禁止";
                return false;
            }
            if (can_half_am != "true")
            {
                error = "不可選擇上午半天假";
                return false;
            }

            SelectHalfAM(offDate);
            return true;
        }

        public bool TrySelectHalfPM(string offDate, out string error)
        {
            error = "";

            if (is_forbidden == "true")
            {
                error = "此放假選項已被管理端禁止";
                return false;
            }
            if (can_half_pm != "true")
            {
                error = "不可選擇下午半天假";
                return false;
            }

            SelectHalfPM(offDate);
            return true;
        }

        /// <summary>取消選擇</summary>
        public void CancelSelection()
        {
            ClearSelection();
        }

        // =========================================================
        // 防呆一致性校正（建議每次存檔 / 回傳前呼叫）
        // =========================================================

        public void NormalizeSelection()
        {
            // 管理端禁止 → 一律清空
            if (is_forbidden == "true")
            {
                ClearSelection();
                return;
            }

            int count =
                (selected_full == "true" ? 1 : 0) +
                (selected_half_am == "true" ? 1 : 0) +
                (selected_half_pm == "true" ? 1 : 0);

            // 同時選多個 → 清空
            if (count > 1)
            {
                ClearSelection();
                return;
            }

            // 選了不該選的 → 清空
            if (selected_full == "true" && can_full != "true")
            {
                ClearSelection();
                return;
            }
            if (selected_half_am == "true" && can_half_am != "true")
            {
                ClearSelection();
                return;
            }
            if (selected_half_pm == "true" && can_half_pm != "true")
            {
                ClearSelection();
                return;
            }
        }
    }

}
