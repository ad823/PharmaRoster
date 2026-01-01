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
    /// 人員資料 (Staff)
    /// </summary>
    [Description("staffs")]
    public class StaffClass
    {
        /// <summary>唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>人員編號</summary>
        [JsonPropertyName("staff_id")]
        [Description("VARCHAR,50,UNIQUE")]
        public string staff_id { get; set; }

        /// <summary>姓名</summary>
        [JsonPropertyName("staff_name")]
        [Description("VARCHAR,100,INDEX")]
        public string staff_name { get; set; }


        /// <summary>姓名代號</summary>
        [JsonPropertyName("staff_simple_name")]
        [Description("VARCHAR,10,NONE")]
        public string staff_simple_name { get; set; }


        /// <summary>角色 (藥師/藥助/工讀生等)</summary>
        [JsonPropertyName("role")]
        [Description("VARCHAR,50,NONE")]
        public string role { get; set; }

        /// <summary>日班基礎權重</summary>
        [JsonPropertyName("day_shift_weight_base")]
        [Description("VARCHAR,10,NONE")]
        public string DayShiftWeightBase { get; set; } = "0";

        /// <summary>小夜班基礎權重</summary>
        [JsonPropertyName("swing_shift_weight_base")]
        [Description("VARCHAR,10,NONE")]
        public string SwingShiftWeightBase { get; set; } = "0";

        /// <summary>大夜班基礎權重</summary>
        [JsonPropertyName("midnight_shift_weight_base")]
        [Description("VARCHAR,10,NONE")]
        public string MidnightShiftWeightBase { get; set; } = "0";

        /// <summary>假日班基礎權重</summary>
        [JsonPropertyName("holiday_shift_weight_base")]
        [Description("VARCHAR,10,NONE")]
        public string HolidayShiftWeightBase { get; set; } = "0";


        // ========== 累積次數 (字串形式，可重新計算) ==========
        [JsonPropertyName("day_shift_count")]
        public string DayShiftCount { get; set; } = "0";

        [JsonPropertyName("swing_shift_count")]
        public string SwingShiftCount { get; set; } = "0";

        [JsonPropertyName("midnight_shift_count")]
        public string MidnightShiftCount { get; set; } = "0";

        [JsonPropertyName("holiday_shift_count")]
        public string HolidayShiftCount { get; set; } = "0";


        /// <summary>建立時間</summary>
        [JsonPropertyName("created_at")]
        [Description("DATETIME,50,NONE")]
        public string created_at { get; set; }

        /// <summary>更新時間</summary>
        [JsonPropertyName("updated_at")]
        [Description("DATETIME,50,NONE")]
        public string updated_at { get; set; }

        [JsonPropertyName("staffAttributes")]
        public StaffAttributesClass staffAttributes { get; set; } = new StaffAttributesClass();

        /// <summary>請假/特例紀錄 (僅程式用，不建立資料表欄位)</summary>
        [JsonPropertyName("leaves")]
        public List<LeaveRequestClass> leaveRequests { get; set; } = new List<LeaveRequestClass>();

        /// <summary>所屬群組 (僅程式用，不建立資料表欄位)</summary>
        [JsonPropertyName("groups")]
        public List<ShiftGroupMemberClass> shiftGroupMembers { get; set; } = new List<ShiftGroupMemberClass>();

        /// <summary>排班歷程 (僅程式用，不建立資料表欄位)</summary>
        [JsonPropertyName("scheduleHistories")]
        public List<StaffScheduleHistoryClass> scheduleHistories { get; set; } = new List<StaffScheduleHistoryClass>();


        // ========== 新增：重新計算方法 ==========
        /// <summary>
        /// 重新計算各班別次數，更新到字串屬性
        /// </summary>
        public void RecalculateCounts()
        {
            if (DayShiftWeightBase.StringIsEmpty()) DayShiftWeightBase = "0";
            if (SwingShiftWeightBase.StringIsEmpty()) SwingShiftWeightBase = "0";
            if (MidnightShiftWeightBase.StringIsEmpty()) MidnightShiftWeightBase = "0";
            if (HolidayShiftWeightBase.StringIsEmpty()) HolidayShiftWeightBase = "0";
            DayShiftCount = (scheduleHistories
                .Count(s => s.shift_type == ShiftTypeEnum.day.GetEnumName()) + DayShiftWeightBase?.StringToInt32()).ToString();

            SwingShiftCount = (scheduleHistories
                .Count(s => s.shift_type == ShiftTypeEnum.swing.GetEnumName()) + SwingShiftWeightBase?.StringToInt32()).ToString();

            MidnightShiftCount = (scheduleHistories
                .Count(s => s.shift_type == ShiftTypeEnum.midnight.GetEnumName()) + MidnightShiftWeightBase?.StringToInt32()).ToString();

            HolidayShiftCount = (scheduleHistories
                .Count(s => s.shift_type == ShiftTypeEnum.holiday.GetEnumName()) + HolidayShiftWeightBase?.StringToInt32()).ToString();
        }
    }

    /// <summary>
    /// 人員排班歷程 (Staff Schedule History)
    /// </summary>
    [Description("staff_schedule_histories")]
    public class StaffScheduleHistoryClass
    {
        /// <summary>唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>人員 GUID</summary>
        [JsonPropertyName("staff_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string staff_guid { get; set; }

        /// <summary>班別屬性</summary>
        [JsonPropertyName("shift_type")]
        [Description("VARCHAR,50,INDEX")]
        public string shift_type { get; set; }

        /// <summary>日期 (yyyy-MM-dd)</summary>
        [JsonPropertyName("date")]
        [Description("DATETIME,0,INDEX")]
        public string date { get; set; }

        /// <summary>需求班次 GUID (對應 RequiredShiftClass)</summary>
        [JsonPropertyName("req_shift_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string req_shift_guid { get; set; }

        /// <summary>需求班次 GUID (對應 assigned_shift_guid)</summary>
        [JsonPropertyName("assigned_shift_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string assigned_shift_guid { get; set; }
        
        /// <summary>班別群組 GUID (對應 ShiftGroupClass)</summary>
        [JsonPropertyName("shift_group_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string shift_group_guid { get; set; }

        /// <summary>來源 (自動排班/手動調整/臨時支援)</summary>
        [JsonPropertyName("source")]
        [Description("VARCHAR,50,NONE")]
        public string source { get; set; }

        /// <summary>狀態 (正常/取消)</summary>
        [JsonPropertyName("status")]
        [Description("VARCHAR,20,INDEX")]
        public string status { get; set; }

        /// <summary>班次時間區間 (例如: "08:00-16:00")</summary>
        [JsonPropertyName("time")]
        [Description("VARCHAR,20,NONE")]
        public string time { get; set; }

        /// <summary>部門 / 科別 (例如: 門診, 急診, 兒科)</summary>
        [JsonPropertyName("department")]
        [Description("VARCHAR,50,NONE")]
        public string department { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("created_at")]
        [Description("DATETIME,0,NONE")]
        public string created_at { get; set; }

        /// <summary>更新時間</summary>
        [JsonPropertyName("updated_at")]
        [Description("DATETIME,0,NONE")]
        public string updated_at { get; set; }

        /// <summary>需求班次資訊 (僅程式用，不建表)</summary>
        [JsonPropertyName("workShiftRequirement")]
        public WorkShiftRequirementClass workShiftRequirement { get; set; }

        /// <summary>
        /// 程式用解析 (TimeSpan Start/End)
        /// </summary>
        [JsonIgnore]
        public (TimeSpan start, TimeSpan end)? TimeRange
        {
            get
            {
                if (string.IsNullOrWhiteSpace(time)) return null;
                var parts = time.Split('-');
                if (parts.Length == 2 &&
                    TimeSpan.TryParse(parts[0], out var start) &&
                    TimeSpan.TryParse(parts[1], out var end))
                {
                    return (start, end);
                }
                return null;
            }
        }
    }


    /// <summary>
    /// 人員屬性 (對應 StaffClass.attributes JSON)
    /// </summary>
    [Description("staffattributes")]
    public class StaffAttributesClass
    {
        /// <summary>唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>staff_guid</summary>
        [JsonPropertyName("staff_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string staff_guid { get; set; }

        /// <summary>是否懷孕 (true/false)</summary>
        [JsonPropertyName("pregnant")]
        [Description("VARCHAR,10,NONE")]
        public string pregnant { get; set; }

        /// <summary>是否哺乳期 (true/false)</summary>
        [JsonPropertyName("breastfeeding")]
        [Description("VARCHAR,10,NONE")]
        public string breastfeeding { get; set; }

        /// <summary>是否僅能排夜班 (true/false)</summary>
        [JsonPropertyName("night_only")]
        [Description("VARCHAR,10,NONE")]
        public string night_only { get; set; }

        /// <summary>是否禁止排夜班 (true/false)</summary>
        [JsonPropertyName("no_night")]
        [Description("VARCHAR,10,NONE")]
        public string no_night { get; set; }

        /// <summary>固定班別 (none/day/evening/night/tpn)</summary>
        [JsonPropertyName("fixed_shift")]
        [Description("VARCHAR,20,NONE")]
        public string fixed_shift { get; set; }

        /// <summary>最大連續上班天數</summary>
        [JsonPropertyName("max_consecutive_days")]
        [Description("VARCHAR,10,NONE")]
        public string max_consecutive_days { get; set; }

        /// <summary>偏好休假日 (例如: MON, TUE，多個以逗號分隔)</summary>
        [JsonPropertyName("preferred_days_off")]
        [Description("TEXT,50,NONE")]
        public string preferred_days_off { get; set; }

        /// <summary>是否偏好小夜班 (true/false)</summary>
        [JsonPropertyName("prefer_evening")]
        [Description("VARCHAR,10,NONE")]
        public string prefer_evening { get; set; }

        /// <summary>是否偏好六日班 (true/false)</summary>
        [JsonPropertyName("prefer_weekend")]
        [Description("VARCHAR,10,NONE")]
        public string prefer_weekend { get; set; }

        /// <summary>排除的班別 (例如: tpn,night，多個以逗號分隔)</summary>
        [JsonPropertyName("exclude_shifts")]
        [Description("TEXT,50,NONE")]
        public string exclude_shifts { get; set; }

        /// <summary>專長 (oncology/er/herbal/general)</summary>
        [JsonPropertyName("specialty")]
        [Description("VARCHAR,50,NONE")]
        public string specialty { get; set; }

        /// <summary>是否可支援其他部門 (true/false)</summary>
        [JsonPropertyName("support_role")]
        [Description("VARCHAR,10,NONE")]
        public string support_role { get; set; }
    }

    public static class ScheduleValidator
    {
        public static (bool isValid, string message) ValidateSchedule(StaffClass staff, StaffScheduleHistoryClass newSchedule)
        {
            List<StaffScheduleHistoryClass> list = staff.scheduleHistories ?? new List<StaffScheduleHistoryClass>();
            DateTime dateTime = newSchedule.date.StringToDateTime();
            (TimeSpan, TimeSpan)? timeRange = newSchedule.TimeRange;
            if (!timeRange.HasValue)
            {
                return (false, "新增排班時間格式錯誤");
            }

            DateTime dateTime2 = dateTime.Add(timeRange.Value.Item1);
            DateTime dateTime3 = FixEndTime(dateTime2, timeRange.Value.Item2);

            // ────────────────────────────────────────────────
            // ✅ [新增檢查] 同一天只能排一個班
            // ────────────────────────────────────────────────
            bool alreadyHasShiftToday = list.Any(h =>
                DateTime.TryParse(h.date, out DateTime d) && d.Date == dateTime.Date);

            if (alreadyHasShiftToday)
            {
                return (false, $"同一天 ({dateTime:yyyy-MM-dd}) 已有其他班次，無法重複排班");
            }

            // ────────────────────────────────────────────────
            // 既有班表衝突檢查 (重疊、間隔不足等)
            // ────────────────────────────────────────────────
            foreach (StaffScheduleHistoryClass item in list)
            {
                if (item.TimeRange.HasValue)
                {
                    DateTime dateTime4 = item.date.StringToDateTime().Add(item.TimeRange.Value.start);
                    DateTime dateTime5 = FixEndTime(dateTime4, item.TimeRange.Value.end);

                    if (dateTime2 < dateTime5 && dateTime3 > dateTime4)
                    {
                        return (false, "班次重疊 (與 " + item.date + " 班次衝突)");
                    }

                    double totalHours = (dateTime2 - dateTime5).TotalHours;
                    double totalHours2 = (dateTime4 - dateTime3).TotalHours;
                    double num = Math.Min(Math.Abs(totalHours), Math.Abs(totalHours2));
                    if (num < 11.0)
                    {
                        return (false, $"班表間隔未滿 11 小時 (與 {item.date} 班次衝突，實際 {num:F1} 小時)");
                    }
                }
            }

            // ────────────────────────────────────────────────
            // 連續上班日數檢查
            // ────────────────────────────────────────────────
            List<DateTime> list2 = (from s in list
                                    orderby s.date
                                    select s.date.StringToDateTime().Date).Distinct().ToList();
            if (!list2.Contains(dateTime.Date))
            {
                list2.Add(dateTime.Date);
            }

            int num2 = 1;
            int num3 = 1;
            for (int i = 1; i < list2.Count; i++)
            {
                if ((list2[i] - list2[i - 1]).TotalDays == 1.0)
                {
                    num3++;
                    num2 = Math.Max(num2, num3);
                }
                else
                {
                    num3 = 1;
                }
            }

            if (num2 > 12)
            {
                return (false, "連續上班超過 12 天");
            }

            // ────────────────────────────────────────────────
            // 14 天內至少 2 天休假
            // ────────────────────────────────────────────────
            DateTime startOfPeriod = dateTime.Date.AddDays(-13.0);
            DateTime endOfPeriod = dateTime.Date;
            int num4 = list2.Where((DateTime d) => d >= startOfPeriod && d <= endOfPeriod).Count();
            int num5 = 14 - num4;
            if (num5 < 2)
            {
                return (false, "14 天內休假不足 2 天");
            }

            // ────────────────────────────────────────────────
            // 孕婦 / 哺乳班別檢核
            // ────────────────────────────────────────────────
            if (staff.staffAttributes != null &&
                (staff.staffAttributes.pregnant == "true" || staff.staffAttributes.breastfeeding == "true") &&
                timeRange.Value.Item2 > new TimeSpan(22, 0, 0))
            {
                return (false, "孕婦/哺乳不可排 22:00 以後的班");
            }

            return (true, "檢核通過");
        }




        /// <summary>
        /// 檢查兩個班次之間是否有滿足 11 小時休息間隔
        /// （直接比較前班下班時間與後班上班時間）
        /// </summary>
        public static (bool, string) CheckShiftInterval(
            DateTime prevShiftEnd, DateTime nextShiftStart)
        {
            double gap = (nextShiftStart - prevShiftEnd).TotalHours;

            if (gap < 11.0)
            {
                return (false, $"班表間隔未滿 11 小時 (實際 {gap:F1} 小時)");
            }

            return (true, $"班表間隔符合規範 ({gap:F1} 小時 ≥ 11 小時)");
        }

        /// <summary>
        /// 修正結束時間，避免 00:00 判斷錯亂
        /// </summary>
        private static DateTime FixEndTime(DateTime start, TimeSpan endTime)
        {
            var end = start.Date.Add(endTime);

            // 如果結束時間是 00:00，就改成 23:59
            if (end.TimeOfDay == TimeSpan.Zero)
            {
                end = end.Date.AddHours(23).AddMinutes(59);
            }

            // 如果結束時間 <= 開始時間，往後推一天（應對跨日）
            if (end <= start)
            {
                end = end.AddDays(1);
            }

            return end;
        }
    }

    public static class StaffScheduleHistoryMethod
    {
        /// <summary>
        /// 依 AssignedShift 查詢對應的 StaffScheduleHistory
        /// </summary>
        public static List<StaffScheduleHistoryClass> FindByAssignedShift(this SQLControl sql, AssignedShiftClass assignedShift)
        {
            if (assignedShift == null || assignedShift.workShiftRequirement == null)
                return new List<StaffScheduleHistoryClass>();

            var rows = sql.GetRowsByDefult(
                null,
                new string[] { "staff_guid", "date", "time" },
                new string[] { assignedShift.staff_guid, assignedShift.date, assignedShift.workShiftRequirement.time });

            return rows.SQLToClass<StaffScheduleHistoryClass>() ?? new List<StaffScheduleHistoryClass>();
        }

        /// <summary>
        /// 更新 StaffScheduleHistory 狀態 (單筆)
        /// </summary>
        public static int UpdateStatus(this SQLControl sql, AssignedShiftClass assignedShift, string newStatus)
        {
            var histories = sql.FindByAssignedShift(assignedShift);
            int updatedCount = 0;

            foreach (var history in histories)
            {
                if (history.status != newStatus)
                {
                    history.status = newStatus;
                    history.updated_at = DateTime.Now.ToDateTimeString_6();
                    sql.UpdateByDefulteExtra(null, new List<object[]> { history.ClassToSQL<StaffScheduleHistoryClass>() });
                    updatedCount++;
                }
            }

            return updatedCount;
        }

        /// <summary>
        /// 批次更新 StaffScheduleHistory 狀態 (多筆 AssignedShift)
        /// </summary>
        /// <param name="sql">SQL 控制器</param>
        /// <param name="assignedShifts">多筆已指派班次</param>
        /// <param name="newStatus">要更新的狀態，例如 "取消"</param>
        /// <returns>實際更新的紀錄數</returns>
        public static int BatchUpdateStatus(this SQLControl sql, List<AssignedShiftClass> assignedShifts, string newStatus)
        {
            if (assignedShifts == null || assignedShifts.Count == 0) return 0;

            int totalUpdated = 0;
            foreach (var ass in assignedShifts)
            {
                totalUpdated += sql.UpdateStatus(ass, newStatus);
            }
            return totalUpdated;
        }

        static public System.Collections.Generic.Dictionary<string, List<StaffScheduleHistoryClass>> CoverToDictionaryBy_req_shift_guid(this List<StaffScheduleHistoryClass> classes)
        {
            Dictionary<string, List<StaffScheduleHistoryClass>> dictionary = new Dictionary<string, List<StaffScheduleHistoryClass>>();

            foreach (var item in classes)
            {
                string key = item.req_shift_guid;

                // 如果字典中已經存在該索引鍵，則將值添加到對應的列表中
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key].Add(item);
                }
                // 否則創建一個新的列表並添加值
                else
                {
                    List<StaffScheduleHistoryClass> values = new List<StaffScheduleHistoryClass> { item };
                    dictionary[key] = values;
                }
            }

            return dictionary;
        }
        static public List<StaffScheduleHistoryClass> SortDictionaryBy_req_shift_guid(this System.Collections.Generic.Dictionary<string, List<StaffScheduleHistoryClass>> dictionary, string GUID)
        {
            if (dictionary.ContainsKey(GUID))
            {
                return dictionary[GUID];
            }
            return new List<StaffScheduleHistoryClass>();
        }


        static public System.Collections.Generic.Dictionary<string, List<StaffScheduleHistoryClass>> CoverToDictionaryBy_staff_guid(this List<StaffScheduleHistoryClass> classes)
        {
            Dictionary<string, List<StaffScheduleHistoryClass>> dictionary = new Dictionary<string, List<StaffScheduleHistoryClass>>();

            foreach (var item in classes)
            {
                string key = item.staff_guid;

                // 如果字典中已經存在該索引鍵，則將值添加到對應的列表中
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key].Add(item);
                }
                // 否則創建一個新的列表並添加值
                else
                {
                    List<StaffScheduleHistoryClass> values = new List<StaffScheduleHistoryClass> { item };
                    dictionary[key] = values;
                }
            }

            return dictionary;
        }
        static public List<StaffScheduleHistoryClass> SortDictionaryBy_staff_guid(this System.Collections.Generic.Dictionary<string, List<StaffScheduleHistoryClass>> dictionary, string GUID)
        {
            if (dictionary.ContainsKey(GUID))
            {
                return dictionary[GUID];
            }
            return new List<StaffScheduleHistoryClass>();
        }

        /// <summary>
        /// 從 StaffScheduleHistoryClass 清單中，找出指定日期範圍內、指定班別群組的紀錄
        /// </summary>
        /// <param name="histories">歷程清單</param>
        /// <param name="dateStart">起始日期 (含)</param>
        /// <param name="dateEnd">結束日期 (含)</param>
        /// <param name="shift_group_guid">班別群組 GUID (可為 null 或空字串)</param>
        /// <returns>符合條件的紀錄清單</returns>
        public static List<StaffScheduleHistoryClass> FindByDateRange(
            this List<StaffScheduleHistoryClass> histories,
            DateTime dateStart,
            DateTime dateEnd,
            string shift_group_guid = null)
        {
            if (histories == null || histories.Count == 0)
                return new List<StaffScheduleHistoryClass>();

            return histories
                .Where(h =>
                {
                    // 日期過濾
                    if (!DateTime.TryParse(h.date, out DateTime d))
                        return false;
                    if (d.Date < dateStart.Date || d.Date > dateEnd.Date)
                        return false;

                    // 群組過濾 (如果 shift_group_guid 有傳入)
                    if (!string.IsNullOrWhiteSpace(shift_group_guid) &&
                        !string.Equals(h.shift_group_guid, shift_group_guid, StringComparison.OrdinalIgnoreCase))
                        return false;

                    return true;
                })
                .OrderBy(h => h.date)
                .ThenBy(h => h.time)
                .ToList();
        }

        /// <summary>
        /// 從 StaffScheduleHistoryClass 清單中，找出指定月份內、指定班別類型的紀錄
        /// </summary>
        /// <param name="histories">歷程清單</param>
        /// <param name="year">查詢年份 (例如 2025)</param>
        /// <param name="month">查詢月份 (1–12)</param>
        /// <param name="shiftType">班別類型 (可為 null 或空字串)</param>
        /// <returns>符合條件的紀錄清單</returns>
        public static List<StaffScheduleHistoryClass> FindByMonthAndShiftType(
            this List<StaffScheduleHistoryClass> histories,
            int year,
            int month,
            string shiftType = null)
        {
            if (histories == null || histories.Count == 0)
                return new List<StaffScheduleHistoryClass>();

            // 🗓️ 自動計算該月的起訖日期
            DateTime dateStart = new DateTime(year, month, 1);
            DateTime dateEnd = dateStart.AddMonths(1).AddDays(-1);

            return histories
                .Where(h =>
                {
                    if (!DateTime.TryParse(h.date, out DateTime d))
                        return false;
                    if (d.Date < dateStart.Date || d.Date > dateEnd.Date)
                        return false;

                    if (!string.IsNullOrWhiteSpace(shiftType) &&
                        !string.Equals(h.shift_type, shiftType, StringComparison.OrdinalIgnoreCase))
                        return false;

                    return true;
                })
                .OrderBy(h => h.date)
                .ThenBy(h => h.time)
                .ToList();
        }

    }

    public static class StaffClassMethod
    {      
        static public System.Collections.Generic.Dictionary<string, List<StaffClass>> CoverToDictionaryByGUID(this List<StaffClass> classes)
        {
            Dictionary<string, List<StaffClass>> dictionary = new Dictionary<string, List<StaffClass>>();

            foreach (var item in classes)
            {
                string key = item.GUID;

                // 如果字典中已經存在該索引鍵，則將值添加到對應的列表中
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key].Add(item);
                }
                // 否則創建一個新的列表並添加值
                else
                {
                    List<StaffClass> values = new List<StaffClass> { item };
                    dictionary[key] = values;
                }
            }

            return dictionary;
        }
        static public List<StaffClass> SortDictionaryByGUID(this System.Collections.Generic.Dictionary<string, List<StaffClass>> dictionary, string GUID)
        {
            if (dictionary.ContainsKey(GUID))
            {
                return dictionary[GUID];
            }
            return new List<StaffClass>();
        }
    }

}
