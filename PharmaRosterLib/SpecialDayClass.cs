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
    /// 特殊日 (Special Day)
    /// </summary>
    /// <remarks>
    /// - 用於記錄國定假日、颱風假、醫院特殊日。  
    /// - 可覆蓋每日需求數量 (若為 null 表示不覆蓋)。  
    /// - 若需針對不同班別設定，請搭配 <c>SpecialDayShiftOverrideClass</c>。  
    /// </remarks>
    [Description("special_days")]
    public class SpecialDayClass
    {
        /// <summary>唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>特殊日日期</summary>
        [JsonPropertyName("date")]
        [Description("DATE,20,INDEX")]
        public string date { get; set; }

        /// <summary>說明 (如：中秋節、颱風假)</summary>
        [JsonPropertyName("description")]
        [Description("VARCHAR,200,NONE")]
        public string description { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("created_at")]
        [Description("DATETIME,20,NONE")]
        public string created_at { get; set; }

    }
    public static class SpecialDayMethod
    {
        static public System.Collections.Generic.Dictionary<string, List<SpecialDayClass>> CoverToDictionaryByDate(this List<SpecialDayClass> classes)
        {
            Dictionary<string, List<SpecialDayClass>> dictionary = new Dictionary<string, List<SpecialDayClass>>();

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
                    List<SpecialDayClass> values = new List<SpecialDayClass> { item };
                    dictionary[key] = values;
                }
            }

            return dictionary;
        }
        static public List<SpecialDayClass> SortDictionaryByDate(this System.Collections.Generic.Dictionary<string, List<SpecialDayClass>> dictionary, string val)
        {
            if (dictionary.ContainsKey(val))
            {
                return dictionary[val];
            }
            return new List<SpecialDayClass>();
        }
    }

}
