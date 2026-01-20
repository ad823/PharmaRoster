using Basic;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PharmaRosterLib
{
    /// <summary>
    /// 單日排班表 (以日期為單位)
    /// </summary>
    [Description("schedule_days")]
    public class ScheduleDayClass
    {
        /// <summary>唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>日期 (yyyy-MM-dd)</summary>
        [JsonPropertyName("date")]
        [Description("DATETIME,0,UNIQUE")]
        public string date { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("created_at")]
        [Description("DATETIME,0,NONE")]
        public string created_at { get; set; }

        /// <summary>更新時間</summary>
        [JsonPropertyName("updated_at")]
        [Description("DATETIME,0,NONE")]
        public string updated_at { get; set; }

        // 以下為關聯，不建立資料表欄位
        [JsonPropertyName("required_shifts")]
        public List<RequiredShiftClass> RequiredShifts { get; set; } = new List<RequiredShiftClass>();

        [JsonPropertyName("assigned_shifts")]
        public List<AssignedShiftClass> AssignedShifts { get; set; } = new List<AssignedShiftClass>();

        [JsonPropertyName("leave_requests")]
        public List<LeaveRequestClass> LeaveRequests { get; set; } = new List<LeaveRequestClass>();

        [JsonPropertyName("special_days")]
        public List<SpecialDayClass> SpecialDays { get; set; } = new List<SpecialDayClass>();

        [JsonPropertyName("workload_summaries")]
        public List<WorkloadSummaryClass> WorkloadSummaries { get; set; } = new List<WorkloadSummaryClass>();

        [JsonPropertyName("schedule_logs")]
        public List<ScheduleLogClass> ScheduleLogs { get; set; } = new List<ScheduleLogClass>();
    }

    /// <summary>
    /// 需求班次 (與 ShiftGroup 關聯)
    /// </summary>
    [Description("required_shifts")]
    public class RequiredShiftClass
    {
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>日期 (yyyy-MM-dd)</summary>
        [JsonPropertyName("date")]
        [Description("DATETIME,0,INDEX")]
        public string date { get; set; }

        /// <summary>對應群組 GUID (如大夜循環班)</summary>
        [JsonPropertyName("shift_group_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string shift_group_guid { get; set; }

        /// <summary>需求班次明細 (JSON 陣列字串)</summary>
        /// <remarks>
        /// 存放格式範例：
        /// [
        ///   {"time":"08:00-16:00","required_count":"1","department":"門診"},
        ///   {"time":"09:00-17:00","required_count":"2","department":"急診"}
        /// ]
        /// </remarks>
        [JsonPropertyName("shift_requirements")]
        [Description("TEXT,20,NONE")]
        public string shift_requirements { get; set; }

        /// <summary>已轉換的需求班次清單 (程式用，不寫回 DB)</summary>
        [JsonPropertyName("workShiftRequirements")]
        public List<WorkShiftRequirementClass> workShiftRequirements
        {
            get
            {
                if (string.IsNullOrWhiteSpace(shift_requirements))
                {
                    return new List<WorkShiftRequirementClass>();
                }

                try
                {
                    return JsonSerializer.Deserialize<List<WorkShiftRequirementClass>>(shift_requirements)
                           ?? new List<WorkShiftRequirementClass>();
                }
                catch
                {
                    return new List<WorkShiftRequirementClass>();
                }
            }
            set
            {
                if (value == null || value.Count == 0)
                {
                    shift_requirements = "[]"; // ⚠️ 避免 null
                }
                else
                {
                    shift_requirements = JsonSerializer.Serialize(value);
                }
            }
        }


        /// <summary>總需求人數 (所有時段需求數量加總)</summary>
        [JsonPropertyName("total_required_count")]
        [Description("VARCHAR,10,NONE")]
        public string total_required_count
        {
            get
            {
                if (workShiftRequirements == null || workShiftRequirements.Count == 0) return "0";
                return workShiftRequirements
                    .Sum(x => x.required_count.StringToInt32())
                    .ToString();
            }
            set
            {
                // 僅為了 JSON 反序列化需要，不做實際設定
            }
        }

        /// <summary>建立時間</summary>
        [JsonPropertyName("created_at")]
        [Description("DATETIME,0,NONE")]
        public string created_at { get; set; }

        /// <summary>更新時間</summary>
        [JsonPropertyName("updated_at")]
        [Description("DATETIME,0,NONE")]
        public string updated_at { get; set; }

        /// <summary>對應的群組資訊 (非資料表欄位)</summary>
        [JsonPropertyName("shift_group")]
        public ShiftGroupClass shift_group { get; set; }
    }


    /// <summary>
    /// 已分派班次 (人員實際分配結果)
    /// </summary>
    [Description("assigned_shifts")]
    public class AssignedShiftClass
    {
        /// <summary>唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>日期 (yyyy-MM-dd)</summary>
        [JsonPropertyName("date")]
        [Description("DATETIME,0,INDEX")]
        public string date { get; set; }

        /// <summary>人員 GUID</summary>
        [JsonPropertyName("staff_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string staff_guid { get; set; }

        /// <summary>需求班次 GUID (對應 RequiredShiftClass)</summary>
        [JsonPropertyName("req_shift_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string req_shift_guid { get; set; }

        /// <summary>
        /// 需求班次明細 JSON (直接存 WorkShiftRequirementClass 的序列化字串)
        /// </summary>
        [JsonPropertyName("shift_requirement")]
        [Description("VARCHAR,300,NONE")]
        public string shift_requirement { get; set; }

        /// <summary>來源 (自動排班/手動調整/臨時支援)</summary>
        [JsonPropertyName("source")]
        public string source { get; set; }

        /// <summary>狀態 (normal=正常, support=支援, exception=例外)</summary>
        [JsonPropertyName("status")]
        [Description("VARCHAR,20,NONE")]
        public string status { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("created_at")]
        [Description("DATETIME,0,NONE")]
        public string created_at { get; set; }

        /// <summary>更新時間</summary>
        [JsonPropertyName("updated_at")]
        [Description("DATETIME,0,NONE")]
        public string updated_at { get; set; }

        // ===== 關聯物件 =====

        /// <summary>對應人員資訊 (非資料表欄位)</summary>
        [JsonPropertyName("staff")]
        public StaffClass staff { get; set; }

        /// <summary>對應需求班次 (非資料表欄位)</summary>
        [JsonPropertyName("required_shift")]
        public RequiredShiftClass required_shift { get; set; }

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
                }}
            }
        
    }




    /// <summary>
    /// 排班操作紀錄
    /// </summary>
    [Description("schedule_logs")]
    public class ScheduleLogClass
    {
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>日期</summary>
        [JsonPropertyName("date")]
        [Description("DATETIME,0,INDEX")]
        public string date { get; set; }

        /// <summary>操作人員 GUID</summary>
        [JsonPropertyName("operator_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string operator_guid { get; set; }

        /// <summary>動作 (新增/修改/刪除/AI調整)</summary>
        [JsonPropertyName("action")]
        [Description("VARCHAR,20,NONE")]
        public string action { get; set; }

        /// <summary>描述 (例如修改原因)</summary>
        [JsonPropertyName("description")]
        [Description("TEXT,0,NONE")]
        public string description { get; set; }

        /// <summary>操作時間</summary>
        [JsonPropertyName("created_at")]
        [Description("DATETIME,0,NONE")]
        public string created_at { get; set; }
    }

    /// <summary>
    /// 公平性統計 (每位人員的排班統計資料) - 可存入 DB
    /// </summary>
    [Description("workload_summaries")]
    public class WorkloadSummaryClass
    {
        /// <summary>唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>人員 GUID</summary>
        [JsonPropertyName("staff_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string staff_guid { get; set; }

        /// <summary>累積總班數</summary>
        [JsonPropertyName("total_shifts")]
        [Description("VARCHAR,10,NONE")]
        public string total_shifts { get; set; }

        /// <summary>連續上班天數</summary>
        [JsonPropertyName("consecutive_days")]
        [Description("VARCHAR,10,NONE")]
        public string consecutive_days { get; set; }

        /// <summary>小夜班次數</summary>
        [JsonPropertyName("evening_shifts")]
        [Description("VARCHAR,10,NONE")]
        public string evening_shifts { get; set; }

        /// <summary>大夜班次數</summary>
        [JsonPropertyName("night_shifts")]
        [Description("VARCHAR,10,NONE")]
        public string night_shifts { get; set; }

        /// <summary>例假日班次數</summary>
        [JsonPropertyName("weekend_shifts")]
        [Description("VARCHAR,10,NONE")]
        public string weekend_shifts { get; set; }

        /// <summary>平日班次數</summary>
        [JsonPropertyName("weekday_shifts")]
        [Description("VARCHAR,10,NONE")]
        public string weekday_shifts { get; set; }

        /// <summary>支援班次數 (跨部門/臨時調整)</summary>
        [JsonPropertyName("support_shifts")]
        [Description("VARCHAR,10,NONE")]
        public string support_shifts { get; set; }

        /// <summary>更新時間</summary>
        [JsonPropertyName("updated_at")]
        [Description("DATETIME,0,NONE")]
        public string updated_at { get; set; }
    }
    public static class ScheduleDayMethod
    {
        static public ScheduleDayClass SerchByDate(this List<ScheduleDayClass> scheduleDays, string date)
        {
            var result = (from temp in scheduleDays
                          where temp.date.StringToDateTime().ToDateString('-') == date.StringToDateTime().ToDateString('-')
                          select temp).FirstOrDefault();
            return result ?? null;
        }

    }
    public static class RequiredShiftMethod
    {
        static public System.Collections.Generic.Dictionary<string, List<RequiredShiftClass>> CoverToDictionaryByDate(this List<RequiredShiftClass> classes)
        {
            Dictionary<string, List<RequiredShiftClass>> dictionary = new Dictionary<string, List<RequiredShiftClass>>();

            foreach (var item in classes)
            {
                string key = item.date;

                // 如果字典中已經存在該索引鍵，則將值添加到對應的列表中
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key].Add(item);
                }
                // 否則創建一個新的列表並添加值
                else
                {
                    List<RequiredShiftClass> values = new List<RequiredShiftClass> { item };
                    dictionary[key] = values;
                }
            }

            return dictionary;
        }
        static public List<RequiredShiftClass> SortDictionaryByDate(this System.Collections.Generic.Dictionary<string, List<RequiredShiftClass>> dictionary, string val)
        {
            if (dictionary.ContainsKey(val))
            {
                return dictionary[val];
            }
            return new List<RequiredShiftClass>();
        }

        static public System.Collections.Generic.Dictionary<string, List<RequiredShiftClass>> CoverToDictionaryByGUID(this List<RequiredShiftClass> classes)
        {
            Dictionary<string, List<RequiredShiftClass>> dictionary = new Dictionary<string, List<RequiredShiftClass>>();

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
                    List<RequiredShiftClass> values = new List<RequiredShiftClass> { item };
                    dictionary[key] = values;
                }
            }

            return dictionary;
        }
        static public List<RequiredShiftClass> SortDictionaryByGUID(this System.Collections.Generic.Dictionary<string, List<RequiredShiftClass>> dictionary, string val)
        {
            if (dictionary.ContainsKey(val))
            {
                return dictionary[val];
            }
            return new List<RequiredShiftClass>();
        }


        /// <summary>
        /// 根據 shift_group.sort_order 排序 RequiredShifts
        /// </summary>
        /// <param name="requiredShifts">需求班次清單</param>
        /// <returns>排序後的需求班次清單</returns>
        public static List<RequiredShiftClass> SortRequiredShifts(this List<RequiredShiftClass> requiredShifts)
        {
            if (requiredShifts == null || requiredShifts.Count == 0)
                return new List<RequiredShiftClass>();

            return requiredShifts
                .OrderBy(r => r.shift_group?.sort_order.StringToInt32() ?? 0) // 依群組排序號
                .ThenBy(r => r.shift_group?.group_name ?? string.Empty)      // 次排序：群組名稱
                .ToList();
        }

    }
    public static class AssignedShiftMethod
    {
        static public System.Collections.Generic.Dictionary<string, List<AssignedShiftClass>> CoverToDictionaryByDate(this List<AssignedShiftClass> classes)
        {
            Dictionary<string, List<AssignedShiftClass>> dictionary = new Dictionary<string, List<AssignedShiftClass>>();

            foreach (var item in classes)
            {
                string key = item.date;

                // 如果字典中已經存在該索引鍵，則將值添加到對應的列表中
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key].Add(item);
                }
                // 否則創建一個新的列表並添加值
                else
                {
                    List<AssignedShiftClass> values = new List<AssignedShiftClass> { item };
                    dictionary[key] = values;
                }
            }

            return dictionary;
        }
        static public List<AssignedShiftClass> SortDictionaryByDate(this System.Collections.Generic.Dictionary<string, List<AssignedShiftClass>> dictionary, string val)
        {
            if (dictionary.ContainsKey(val))
            {
                return dictionary[val];
            }
            return new List<AssignedShiftClass>();
        }


        static public AssignedShiftClass SerchByStaffGUID(this List<AssignedShiftClass> assignedShifts, string req_shift_guid, string staff_guid , string shift_requirement)
        {
            var result = (from temp in assignedShifts
                          where temp.staff_guid == staff_guid
                          where temp.req_shift_guid == req_shift_guid
                          where temp.shift_requirement == shift_requirement
                          select temp).FirstOrDefault();
            return result ?? null;
        }
    }
    public static class ScheduleLogMethod
    {
        static public System.Collections.Generic.Dictionary<string, List<ScheduleLogClass>> CoverToDictionaryByDate(this List<ScheduleLogClass> classes)
        {
            Dictionary<string, List<ScheduleLogClass>> dictionary = new Dictionary<string, List<ScheduleLogClass>>();

            foreach (var item in classes)
            {
                string key = item.date;

                // 如果字典中已經存在該索引鍵，則將值添加到對應的列表中
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key].Add(item);
                }
                // 否則創建一個新的列表並添加值
                else
                {
                    List<ScheduleLogClass> values = new List<ScheduleLogClass> { item };
                    dictionary[key] = values;
                }
            }

            return dictionary;
        }
        static public List<ScheduleLogClass> SortDictionaryByDate(this System.Collections.Generic.Dictionary<string, List<ScheduleLogClass>> dictionary, string val)
        {
            if (dictionary.ContainsKey(val))
            {
                return dictionary[val];
            }
            return new List<ScheduleLogClass>();
        }

    }
}
