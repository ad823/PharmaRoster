using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        /// <summary>排班紀錄 (僅程式用，不建立資料表欄位)</summary>
        [JsonPropertyName("schedules")]
        public List<ScheduleDayClass> schedules { get; set; } = new List<ScheduleDayClass>();
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
