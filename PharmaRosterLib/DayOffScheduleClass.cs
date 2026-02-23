using Basic;
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

        [Description("VARCHAR,5,NONE")]
        public string enable_weekoff_selection { get; set; }

        [Description("DATETIME,0,NONE")]
        public string weekoff_selection_update_at { get; set; }

        [Description("VARCHAR,5,NONE")]
        public string enable_annualleave_selection { get; set; }

        [Description("DATETIME,0,NONE")]
        public string annualleave_selection_update_at { get; set; }

        [Description("VARCHAR,5,NONE")]
        public string is_completed_locked { get; set; }

        [Description("DATETIME,0,NONE")]
        public string is_completed_locked_update_at { get; set; }

        [Description("DATETIME,0,NONE")]
        public string created_at { get; set; }

        [Description("DATETIME,0,NONE")]
        public string updated_at { get; set; }

        // ===========================
        // ✅ 任選一天放假（Any Date）統計（非SQL欄位）
        // ===========================

        /// <summary>
        /// 任選一天放假的「總額度天數」
        /// 定義：staff_dayoff_option 中 is_any_date = true 的 option 筆數（不去重）
        /// </summary>
        public string any_date_quota_days { get; set; }

        /// <summary>
        /// 任選一天放假的 option 筆數（同 any_date_quota_days，保留欄位名稱給前端顯示用）
        /// </summary>
        public string any_date_option_count { get; set; }

        /// <summary>
        /// 有任選一天放假權限的人數（staff_guid 去重）
        /// </summary>
        public string any_date_staff_count { get; set; }

        /// <summary>
        /// 任選一天放假已使用天數（若你尚未定義「使用」規則，可先回 0）
        /// </summary>
        public string any_date_used_days { get; set; }

        /// <summary>
        /// 任選一天放假剩餘可用天數
        /// remaining = quota - used
        /// </summary>
        public string any_date_remaining_days { get; set; }

        /// <summary>
        /// 任選一天放假是否已滿（true/false）
        /// remaining <= 0
        /// </summary>
        public string any_date_is_full { get; set; }

        /// <summary>表單包含的日期清單（非資料表欄位）</summary>
        public List<DayOffScheduleDayClass> days { get; set; } = new List<DayOffScheduleDayClass>();
    }

    [Description("dayoff_schedule_day")]
    public class DayOffScheduleDayClass
    {
        // ===========================
        // ✅ SQL 欄位
        // ===========================

        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        [Description("VARCHAR,50,INDEX")]
        public string form_guid { get; set; }

        [Description("DATETIME,0,INDEX")]
        public string date { get; set; }

        [Description("VARCHAR,10,NONE")]
        public string am_max_dayoff_count { get; set; }

        [Description("VARCHAR,10,NONE")]
        public string pm_max_dayoff_count { get; set; }

        [Description("DATETIME,0,NONE")]
        public string created_at { get; set; }

        [Description("DATETIME,0,NONE")]
        public string updated_at { get; set; }

        // ===========================
        // ✅ 非 SQL 欄位（前端顯示用）
        // ===========================

        // ---------- 已選擇 ----------
        public string am_dayoff_count { get; set; }
        public string pm_dayoff_count { get; set; }
        public string ff_dayoff_count { get; set; }

        // ---------- 剩餘 ----------
        public string am_remaining_count { get; set; }
        public string pm_remaining_count { get; set; }
        public string is_am_full { get; set; }
        public string is_pm_full { get; set; }

        // ---------- 建議池 ----------
        public string am_suggest_pool { get; set; }
        public string pm_suggest_pool { get; set; }

        // ---------- 建議可再選 ----------
        public string am_suggest_count { get; set; }
        public string pm_suggest_count { get; set; }

        // ---------- 明細 ----------
        public List<DayOffScheduleItemClass> items { get; set; }

        // ===========================
        // ✅ 建構子：避免 null
        // ===========================

        public DayOffScheduleDayClass()
        {
            items = new List<DayOffScheduleItemClass>();

            ResetComputedFields();
        }

        // ===========================
        // ✅ 重設所有計算欄位
        // ===========================

        public void ResetComputedFields()
        {
            am_dayoff_count = "0";
            pm_dayoff_count = "0";
            ff_dayoff_count = "0";

            am_remaining_count = "0";
            pm_remaining_count = "0";

            is_am_full = "false";
            is_pm_full = "false";

            am_suggest_pool = "0";
            pm_suggest_pool = "0";

            am_suggest_count = "0";
            pm_suggest_count = "0";
        }
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
    /// <summary>
    /// 人員排休選項資料（週休/特休填寫時的可選日期與實際選擇）
    /// </summary>
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
        /// 使用者實際選擇的放假日 (yyyy-MM-dd 或 yyyy-MM-dd HH:mm:ss 皆可)
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

        /// <summary>
        /// 是否為「系統強制放假(FF)」
        /// true = 系統強制放假，不允許使用者更改選擇
        /// </summary>
        [Description("VARCHAR,5,NONE")]
        public string is_force_ff { get; set; } = "false";

        /// <summary>
        /// 系統強制放假(FF)設定時間
        /// </summary>
        [Description("DATETIME,0,NONE")]
        public string force_ff_at { get; set; } = "";

        /// <summary>是否已選全天假</summary>
        [Description("VARCHAR,5,NONE")]
        public string selected_full { get; set; }

        /// <summary>是否已選半天假（上午）</summary>
        [Description("VARCHAR,5,NONE")]
        public string selected_half_am { get; set; }

        /// <summary>是否已選半天假（下午）</summary>
        [Description("VARCHAR,5,NONE")]
        public string selected_half_pm { get; set; }

   
        [Description("DATETIME,20,NONE")]
        public string updated_at { get; set; }

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
            date = DateTime.MinValue.ToDateTimeString();
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
        // 安全選擇（含規則 / 禁止 / FF）
        // =========================================================

        /// <summary>
        /// 嘗試選擇全天假（含 FF / 禁止 / 可選性檢核）
        /// </summary>
        public bool TrySelectFullDay(string offDate, out string error)
        {
            error = "";

            // 系統強制FF：不可變更
            if (is_force_ff == "true")
            {
                error = "此放假為系統強制(FF)，不可更改";
                return false;
            }

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

        /// <summary>
        /// 嘗試選擇上午半天假（含 FF / 禁止 / 可選性檢核）
        /// </summary>
        public bool TrySelectHalfAM(string offDate, out string error)
        {
            error = "";

            // 系統強制FF：不可變更
            if (is_force_ff == "true")
            {
                error = "此放假為系統強制(FF)，不可更改";
                return false;
            }

            if (is_forbidden == "true")
            {
                error = "此放假選項已被管理端禁止";
                return false;
            }
            //if (can_half_am != "true")
            //{
            //    error = "不可選擇上午半天假";
            //    return false;
            //}

            SelectHalfAM(offDate);
            return true;
        }

        /// <summary>
        /// 嘗試選擇下午半天假（含 FF / 禁止 / 可選性檢核）
        /// </summary>
        public bool TrySelectHalfPM(string offDate, out string error)
        {
            error = "";

            // 系統強制FF：不可變更
            if (is_force_ff == "true")
            {
                error = "此放假為系統強制(FF)，不可更改";
                return false;
            }

            if (is_forbidden == "true")
            {
                error = "此放假選項已被管理端禁止";
                return false;
            }
            //if (can_half_pm != "true")
            //{
            //    error = "不可選擇下午半天假";
            //    return false;
            //}

            SelectHalfPM(offDate);
            return true;
        }

        /// <summary>
        /// 取消選擇（FF=true 時仍建議禁止外部呼叫，這裡不阻擋，以免管理端要強制調整）
        /// </summary>
        public void CancelSelection()
        {
            ClearSelection();
        }

        // =========================================================
        // 系統強制 FF
        // =========================================================

        /// <summary>
        /// 設定「系統強制放假(FF)」：會強制選擇全天 OFF
        /// </summary>
        /// <param name="offDate">放假日期</param>
        /// <param name="shift">班別預設 OFF</param>
        public void SetForceFF(string offDate, string shift = "OFF")
        {
            is_force_ff = "true";
            force_ff_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            assigned_shift = shift;
            SelectFullDay(offDate);
        }

        /// <summary>
        /// 取消「系統強制放假(FF)」
        /// </summary>
        public void CancelForceFF()
        {
            is_force_ff = "false";
            force_ff_at = "";
        }

        // =========================================================
        // 防呆一致性校正（建議每次存檔 / 回傳前呼叫）
        // =========================================================

        /// <summary>
        /// 一致性校正：
        /// 1) FF=true → 不允許變更（直接 return）
        /// 2) 禁止選擇 → 清空
        /// 3) 同時選多個 → 清空
        /// 4) 選了不該選的（can_xxx != true）→ 清空
        /// </summary>
        public void NormalizeSelection()
        {
            if(selected_full.StringToBool() == false && selected_half_am.StringToBool() == false && selected_half_pm.StringToBool() == false)date = DateTime.MinValue.ToDateTimeString();
            // 系統強制FF → 不允許任何變更（保留系統設定）
            if (is_force_ff == "true")
            {
                // 若你想更強制：FF 一律必須 selected_full=true，可加：
                // if (selected_full != "true") SelectFullDay(date);
                return;
            }

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
            if (selected_half_am == "true" && can_full != "true")
            {
                ClearSelection();
                return;
            }
            if (selected_half_pm == "true" && can_full != "true")
            {
                ClearSelection();
                return;
            }
        }
    }

}
