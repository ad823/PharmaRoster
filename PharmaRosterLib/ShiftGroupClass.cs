using Basic;
using PharmaRosterLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// 排班群組 (Shift Group)
/// </summary>
/// <remarks>
/// 用途：  
/// - 定義一組固定或循環的排班群組，例如「小夜固定班」、「大夜循環班」。  
/// - 每個群組可包含多位成員 (ShiftGroupMemberClass)。  
///
/// 功能：  
/// - 作為排班系統的群組化管理基礎。  
/// - 可與其他排班規則搭配，實現自動化與公平分配。  
/// 
/// 資料表名稱：<c>shift_groups</c>
/// </remarks>
[Description("shift_groups")]
public class ShiftGroupClass
{
    /// <summary>唯一識別碼 (GUID)</summary>
    [JsonPropertyName("GUID")]
    [Description("VARCHAR,50,PRIMARY")]
    public string GUID { get; set; }

    /// <summary>群組名稱 (如：小夜固定班、大夜循環班)</summary>
    [JsonPropertyName("group_name")]
    [Description("VARCHAR,100,NONE")]
    public string group_name { get; set; }

    /// <summary>
    /// 排序號 (用於前端顯示/排序)
    /// </summary>
    [JsonPropertyName("sort_order")]
    [Description("VARCHAR,11,NONE")]
    public string sort_order { get; set; } = "0";

    /// <summary>群組描述 (用途/規則備註)</summary>
    [JsonPropertyName("description")]
    [Description("TEXT,50,NONE")]
    public string description { get; set; }

    /// <summary>循環索引 (記錄上次排班位置)</summary>
    [JsonPropertyName("last_index")]
    [Description("VARCHAR,11,NONE")]
    public string last_index { get; set; } = "0";

    /// <summary>
    /// 班別屬性 (day=白班, swing=小夜, midnight=大夜, holiday=假日班)
    /// </summary>
    [JsonPropertyName("shift_type")]
    [Description("VARCHAR,20,NONE")]
    public string shift_type { get; set; }

    /// <summary>
    /// 上班需求時段清單
    /// </summary>
    /// <remarks>
    /// - 存放於 JSON (TEXT) 欄位，例如：
    ///   [
    ///     {"day":"Monday","time":"08:00-16:00","required_count":"2","department":"門診"},
    ///     {"day":"Monday","time":"08:00-16:00","required_count":"1","department":"急診"},
    ///     {"day":"Tuesday","time":"09:00-17:00","required_count":"1","department":"兒科"}
    ///   ]
    /// </remarks>
    [JsonPropertyName("work_shift_requirements")]
    [Description("TEXT, 20,NONE")]
    public string work_shift_requirements { get; set; }

    /// <summary>
    /// 解析後的上班需求時段清單 (程式用，不寫回 DB)
    /// </summary>
    [JsonPropertyName("workShiftRequirements")]
    public List<WorkShiftRequirementClass> workShiftRanges
    {
        get
        {
            if (string.IsNullOrWhiteSpace(work_shift_requirements)) return new List<WorkShiftRequirementClass>();
            try
            {
                return JsonSerializer.Deserialize<List<WorkShiftRequirementClass>>(work_shift_requirements)
                       ?? new List<WorkShiftRequirementClass>();
            }
            catch
            {
                return new List<WorkShiftRequirementClass>();
            }
        }
        set
        {
            if (value == null)
            {
                work_shift_requirements = null;
            }
            else
            {
                work_shift_requirements = JsonSerializer.Serialize(value);
            }
        }
    }

    /// <summary>最後更新時間</summary>
    [JsonPropertyName("updated_at")]
    [Description("DATETIME,50,NONE")]
    public string updated_at { get; set; }

    /// <summary>建立時間</summary>
    [JsonPropertyName("created_at")]
    [Description("DATETIME,50,NONE")]
    public string created_at { get; set; }

    /// <summary>
    /// 群組成員清單
    /// </summary>
    /// <remarks>
    /// - 非資料表欄位，為物件關聯。  
    /// - 對應至 <c>shift_group_members</c> 資料表。  
    /// </remarks>
    public List<ShiftGroupMemberClass> Members { get; set; }
}
/// <summary>
/// 上班需求時段
/// </summary>
public class WorkShiftRequirementClass
{
    /// <summary>星期 (Monday, Tuesday, ...)</summary>
    [JsonPropertyName("day")]
    public string day { get; set; }

    /// <summary>時間區間 (例如: 08:00-16:00)</summary>
    [JsonPropertyName("time")]
    public string time { get; set; }

    /// <summary>需求人數</summary>
    [JsonPropertyName("required_count")]
    public string required_count { get; set; }

    /// <summary>
    /// 已加入人數 (已分派人員數)
    /// </summary>
    [JsonPropertyName("assigned_count")]
    public string assigned_count { get; set; }
    
    /// <summary>部門 / 科別 (例如: 門診, 急診, 兒科)</summary>
    [JsonPropertyName("department")]
    public string department { get; set; }

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
/// ShiftGroup 相關擴充工具
/// </summary>
public static class ShiftGroupExtensions
{
    /// <summary>
    /// 根據 sort_order 進行升冪排序
    /// </summary>
    /// <param name="groups">排班群組清單</param>
    /// <returns>排序後的清單</returns>
    public static List<ShiftGroupClass> SortByOrder(this List<ShiftGroupClass> groups)
    {
        if (groups == null || groups.Count == 0) return new List<ShiftGroupClass>();

        return groups
            .OrderBy(g => g.sort_order.StringToInt32()) // 字串轉數字排序
            .ThenBy(g => g.group_name) // 若排序號相同，則依群組名稱排序
            .ToList();
    }
    public static StaffClass SerchStaff(this ShiftGroupClass group, string staff_guid)
    {
        List<StaffClass> staffClasses = new List<StaffClass>();

        staffClasses = (from temp in @group.Members
                        where temp.staff_guid == staff_guid
                        select temp.staff_info).ToList();

        return staffClasses[0] ?? null;
    }

    static public System.Collections.Generic.Dictionary<string, List<ShiftGroupClass>> CoverToDictionaryByGUID(this List<ShiftGroupClass> classes)
    {
        Dictionary<string, List<ShiftGroupClass>> dictionary = new Dictionary<string, List<ShiftGroupClass>>();

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
                List<ShiftGroupClass> values = new List<ShiftGroupClass> { item };
                dictionary[key] = values;
            }
        }

        return dictionary;
    }
    static public List<ShiftGroupClass> SortDictionaryByGUID(this System.Collections.Generic.Dictionary<string, List<ShiftGroupClass>> dictionary, string GUID)
    {
        if (dictionary.ContainsKey(GUID))
        {
            return dictionary[GUID];
        }
        return new List<ShiftGroupClass>();
    }

    /// <summary>
    /// 檢查群組是否包含指定的時段 (完整包含判斷)
    /// </summary>
    /// <param name="group">ShiftGroup 物件</param>
    /// <param name="timeRange">格式: "HH:mm-HH:mm"</param>
    /// <returns>true=包含, false=不包含</returns>
    public static bool ContainsShift(this ShiftGroupClass group, string timeRange)
    {
        if (group == null || string.IsNullOrWhiteSpace(timeRange)) return false;

        var parts = timeRange.Split('-');
        if (parts.Length != 2 ||
            !TimeSpan.TryParse(parts[0], out var start) ||
            !TimeSpan.TryParse(parts[1], out var end))
            return false;

        return group.ContainsShift(start, end);
    }

    /// <summary>
    /// 檢查群組是否包含指定的時段 (完整包含判斷)
    /// </summary>
    public static bool ContainsShift(this ShiftGroupClass group, TimeSpan start, TimeSpan end)
    {
        if (group == null || group.workShiftRanges == null) return false;

        foreach (var range in group.workShiftRanges)
        {
            if (range.TimeRange.Value.start <= start && range.TimeRange.Value.end >= end)
            {
                return true; // 找到一個完整覆蓋的區間
            }
        }
        return false;
    }

    /// <summary>
    /// 檢查群組是否與指定時段相交 (部分重疊即可)
    /// </summary>
    public static bool OverlapsShift(this ShiftGroupClass group, TimeSpan start, TimeSpan end)
    {
        if (group == null || group.workShiftRanges == null) return false;

        foreach (var range in group.workShiftRanges)
        {
            if (start < range.TimeRange.Value.end && end > range.TimeRange.Value.start)
            {
                return true; // 存在交集
            }
        }
        return false;
    }
}

public static class WorkShiftRequirementExtensions
{
    /// <summary>
    /// 依照日期 → 時間區間排序 (由早到晚)
    /// </summary>
    /// <param name="requirements">班表需求清單</param>
    /// <returns>排序後的清單</returns>
    public static List<WorkShiftRequirementClass> SortByDayAndTime(this List<WorkShiftRequirementClass> requirements)
    {
        if (requirements == null || requirements.Count == 0) return new List<WorkShiftRequirementClass>();

        // 定義星期順序
        var dayOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Monday", 1 },
            { "Tuesday", 2 },
            { "Wednesday", 3 },
            { "Thursday", 4 },
            { "Friday", 5 },
            { "Saturday", 6 },
            { "Sunday", 7 }
        };

        return requirements
            .OrderBy(r => dayOrder.ContainsKey(r.day) ? dayOrder[r.day] : int.MaxValue) // 依星期排序
            .ThenBy(r => r.TimeRange.HasValue ? r.TimeRange.Value.start : TimeSpan.MaxValue) // 依開始時間排序
            .ThenBy(r => r.TimeRange.HasValue ? r.TimeRange.Value.end : TimeSpan.MaxValue)   // 依結束時間排序
            .ToList();
    }
    /// <summary>
    /// 比對兩個 WorkShiftRequirementClass 清單，以 original 為主更新需求人數
    /// </summary>
    /// <param name="original">原始清單</param>
    /// <param name="target">待配對清單 (若無對應則忽略)</param>
    /// <param name="defaultCount">預設需求人數</param>
    /// <returns>更新後的新清單 (保持 original 的結構)</returns>
    public static List<WorkShiftRequirementClass> UpdateRequirements(this List<WorkShiftRequirementClass> original, List<WorkShiftRequirementClass> target, string defaultCount = "0")
    {
        if (original == null) return new List<WorkShiftRequirementClass>();
        if (target == null) target = new List<WorkShiftRequirementClass>();

        return original.Select(ori =>
        {
            var match = target.FirstOrDefault(t =>
                string.Equals(t.day, ori.day, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(t.time, ori.time, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(t.department, ori.department, StringComparison.OrdinalIgnoreCase));

            return new WorkShiftRequirementClass
            {
                day = ori.day,
                time = ori.time,
                department = ori.department,
                required_count = match?.required_count ?? ori.required_count ?? defaultCount
            };
        }).ToList();

        
    }
    /// <summary>
    /// 檢查需求清單中是否有時段完整涵蓋指定的 WorkShiftRequirementClass，
    /// 並且部門 (department) 必須相同
    /// </summary>
    /// <param name="requirements">需求清單</param>
    /// <param name="requirementClas">要檢查的需求時段</param>
    /// <returns>若有需求時段完整涵蓋 requirementClas，回傳 true，否則 false</returns>
    public static bool ContainsTime(this List<WorkShiftRequirementClass> requirements, WorkShiftRequirementClass requirementClas)
    {
        if (requirements == null || requirements.Count == 0 || requirementClas == null) return false;

        var checkRange = requirementClas.TimeRange;
        if (!checkRange.HasValue) return false;

        return requirements.Any(r =>
        {
            var range = r.TimeRange;
            return range.HasValue &&
                   string.Equals(r.department, requirementClas.department, StringComparison.OrdinalIgnoreCase) &&
                   checkRange.Value.start >= range.Value.start &&
                   checkRange.Value.end <= range.Value.end;
        });
    }
    /// <summary>
    /// 依據指定日期過濾需求清單，只保留符合星期的資料
    /// </summary>
    /// <param name="requirements">需求清單</param>
    /// <param name="date">要比對的日期 (yyyy-MM-dd)</param>
    /// <returns>符合指定日期星期的需求清單</returns>
    public static List<WorkShiftRequirementClass> FilterByDate(this List<WorkShiftRequirementClass> requirements, string date)
    {
        if (requirements == null || requirements.Count == 0) return new List<WorkShiftRequirementClass>();
        if (string.IsNullOrWhiteSpace(date)) return new List<WorkShiftRequirementClass>();

        if (!DateTime.TryParse(date, out var parsedDate))
            return new List<WorkShiftRequirementClass>();

        string dayOfWeek = parsedDate.DayOfWeek.ToString(); // Monday, Tuesday, ...

        return requirements
            .Where(r => string.Equals(r.day, dayOfWeek, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

}
