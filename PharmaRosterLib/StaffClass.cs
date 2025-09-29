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

        /// <summary>角色 (藥師/藥助/工讀生等)</summary>
        [JsonPropertyName("role")]
        [Description("VARCHAR,50,NONE")]
        public string role { get; set; }

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

        /// <summary>狀態 (正常/支援/例外/取消)</summary>
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
        /// <summary>
        /// 檢查新增班表是否符合規則
        /// </summary>
        /// <param name="staff">人員資訊 (含屬性)</param>
        /// <param name="existingSchedules">既有排班歷程</param>
        /// <param name="newSchedule">待新增的班表</param>
        /// <returns>檢核結果 (true=通過, false=違反)</returns>
        public static (bool isValid, string message) ValidateSchedule( StaffClass staff, StaffScheduleHistoryClass newSchedule)
        {
            List<StaffScheduleHistoryClass> existingSchedules = staff.scheduleHistories;
            var newDate = newSchedule.date.StringToDateTime();
            var newRange = newSchedule.TimeRange;

            if (!newRange.HasValue)
                return (false, "新增排班時間格式錯誤");

            // === 規則 2: 上班間隔至少 11 小時 ===
            var lastShift = existingSchedules
                .OrderByDescending(s => s.date)
                .ThenByDescending(s => s.TimeRange?.end)
                .FirstOrDefault();

            if (lastShift?.TimeRange != null)
            {
                var lastEnd = lastShift.date.StringToDateTime().Add(lastShift.TimeRange.Value.end);
                var newStart = newDate.Add(newRange.Value.start);

                if ((newStart - lastEnd).TotalHours < 11)
                    return (false, "班表間隔未滿 11 小時");
            }

            // === 規則 3: 連續上班 ≤ 12 天 ===
            var ordered = existingSchedules
                .OrderBy(s => s.date)
                .Select(s => s.date.StringToDateTime().Date)
                .Distinct()
                .ToList();

            if (!ordered.Contains(newDate.Date))
                ordered.Add(newDate.Date);

            int maxStreak = 1, currentStreak = 1;
            for (int i = 1; i < ordered.Count; i++)
            {
                if ((ordered[i] - ordered[i - 1]).TotalDays == 1)
                {
                    currentStreak++;
                    maxStreak = Math.Max(maxStreak, currentStreak);
                }
                else
                {
                    currentStreak = 1;
                }
            }

            if (maxStreak > 12)
                return (false, "連續上班超過 12 天");

            // === 規則 1: 14 天內至少休 2 天 ===
            var startOfWeek = newDate.AddDays(-(int)newDate.DayOfWeek).Date; // 固定週一開始
            var endOfWeek = startOfWeek.AddDays(13);

            var daysWorked = ordered.Where(d => d >= startOfWeek && d <= endOfWeek).Distinct().Count();
            var daysOff = 14 - daysWorked;
            if (daysOff < 2)
                return (false, "14 天內休假不足 2 天");

            if(staff.staffAttributes != null)
            {
                // === 規則 4: 孕婦/哺乳限制 ===
                if ((staff.staffAttributes.pregnant == "true" || staff.staffAttributes.breastfeeding == "true"))
                {
                    if (newRange.Value.end > new TimeSpan(22, 0, 0))
                        return (false, "孕婦/哺乳不可排 22:00 以後的班");
                }
            }
          

            return (true, "檢核通過");
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
