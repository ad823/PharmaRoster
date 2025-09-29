using Basic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PharmaRosterLib
{
    /// <summary>
    /// 群組成員 (Shift Group Member)
    /// </summary>
    /// <remarks>
    /// 用途：  
    /// - 對應「排班群組」(ShiftGroupClass) 的成員清單。  
    /// - 每個成員代表一名指定人員 (staff)。  
    ///
    /// 功能：  
    /// - 建立群組與人員的關聯。  
    /// - 作為群組內人員的管理基礎，可搭配自動排班規則使用。  
    /// 
    /// 資料表名稱：<c>shift_group_members</c>
    /// </remarks>
    [Description("shift_group_members")]
    public class ShiftGroupMemberClass
    {
        /// <summary>唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>群組 GUID (對應 shift_groups.GUID)</summary>
        [JsonPropertyName("group_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string group_guid { get; set; }

        /// <summary>人員 GUID (對應 staff.GUID)</summary>
        [JsonPropertyName("staff_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string staff_guid { get; set; }

        /// <summary>固定序號 (人工排序用)</summary>
        [JsonPropertyName("order_index")]
        [Description("VARCHAR,11,NONE")]
        public string order_index { get; set; }

        /// <summary>成員排班權重 (數值越小越優先被分配)</summary>
        /// <remarks>
        /// - 建議初始值設為 0。  
        /// - 系統可根據排班歷史自動調整。  
        /// </remarks>
        [JsonPropertyName("weight")]
        [Description("VARCHAR,11,NONE")]
        public string weight { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("created_at")]
        [Description("DATETIME,0,NONE")]
        public string created_at { get; set; }

        [JsonPropertyName("staff_info")]
        public StaffClass staff_info { get; set; } = new StaffClass();
    }
    public static class ShiftGroupMemberExtensions
    {
        /// <summary>
        /// 依照 <c>order_index</c> 排序 (轉 int，比較安全)
        /// </summary>
        public static List<ShiftGroupMemberClass> SortByOrderIndex(this List<ShiftGroupMemberClass> members)
        {
            return members
                .OrderBy(m => m.order_index.StringToInt32())
                .ToList();
        }

        /// <summary>
        /// 先依 <c>weight</c> 排序，再依 <c>order_index</c> 排序  
        /// - weight 越小 → 優先  
        /// - 同 weight 時 → order_index 越小越優先
        /// </summary>
        public static List<ShiftGroupMemberClass> SortByWeightAndOrderIndex(this List<ShiftGroupMemberClass> members)
        {
            return members
                .OrderBy(m => m.weight.StringToInt32())
                .ThenBy(m => m.order_index.StringToInt32())
                .ToList();
        }
    }

}
