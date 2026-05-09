using PharmaRosterLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PharmaRosterLib.Helpers.ImportSchedule
{
    /// <summary>
    /// 班表匯入 Staff 解析結果
    /// </summary>
    public class ImportScheduleResolvedStaff
    {
        /// <summary>
        /// 人員 GUID
        /// </summary>
        public string GUID { get; set; }

        /// <summary>
        /// 工號
        /// </summary>
        public string staff_id { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string staff_name { get; set; }

        /// <summary>
        /// 簡名
        /// </summary>
        public string staff_simple_name { get; set; }
    }

    /// <summary>
    /// Staff 匯入解析結果
    /// </summary>
    public class ImportScheduleResolveResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 錯誤訊息
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 解析後的人員清單
        /// </summary>
        public List<ImportScheduleResolvedStaff> Staffs { get; set; } = new List<ImportScheduleResolvedStaff>();
    }

    /// <summary>
    /// 班表匯入 Staff 工具
    /// </summary>
    public static class ImportScheduleStaffHelper
    {
        /// <summary>
        /// 建立「簡名 -> Staff 清單」對照表
        /// 規則：只收單字簡名
        /// </summary>
        public static Dictionary<string, List<StaffClass>> BuildSimpleNameMap(List<StaffClass> staffClasses)
        {
            Dictionary<string, List<StaffClass>> result = new Dictionary<string, List<StaffClass>>();

            if (staffClasses == null) return result;

            foreach (var st in staffClasses)
            {
                if (st == null) continue;

                string simpleName = st.staff_simple_name;
                if (string.IsNullOrWhiteSpace(simpleName)) continue;

                // 規則：一個字代表一位人
                if (simpleName.Length != 1) continue;

                if (!result.ContainsKey(simpleName))
                {
                    result[simpleName] = new List<StaffClass>();
                }

                result[simpleName].Add(st);
            }

            return result;
        }

        /// <summary>
        /// 驗證簡名是否存在
        /// </summary>
        public static bool ExistsSimpleName(Dictionary<string, List<StaffClass>> simpleNameMap, string simpleName)
        {
            if (simpleNameMap == null) return false;
            if (string.IsNullOrWhiteSpace(simpleName)) return false;

            return simpleNameMap.ContainsKey(simpleName);
        }

        /// <summary>
        /// 驗證簡名是否唯一對應
        /// </summary>
        public static bool IsUniqueSimpleName(Dictionary<string, List<StaffClass>> simpleNameMap, string simpleName)
        {
            if (!ExistsSimpleName(simpleNameMap, simpleName)) return false;

            return simpleNameMap[simpleName] != null && simpleNameMap[simpleName].Count == 1;
        }

        /// <summary>
        /// 解析簡名清單為 Staff 清單
        /// 規則：
        /// 1. 每個簡名必須存在
        /// 2. 每個簡名必須唯一對應
        /// </summary>
        public static ImportScheduleResolveResult ResolveSimpleNames(
            Dictionary<string, List<StaffClass>> simpleNameMap,
            List<string> simpleNames)
        {
            ImportScheduleResolveResult result = new ImportScheduleResolveResult();

            if (simpleNameMap == null)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "Staff 簡名字典未建立";
                return result;
            }

            if (simpleNames == null || simpleNames.Count == 0)
            {
                result.IsSuccess = false;
                result.ErrorMessage = "沒有可解析的簡名";
                return result;
            }

            foreach (string simpleName in simpleNames)
            {
                if (!ExistsSimpleName(simpleNameMap, simpleName))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"找不到簡名：{simpleName}";
                    return result;
                }

                if (!IsUniqueSimpleName(simpleNameMap, simpleName))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = $"簡名不是唯一對應：{simpleName}";
                    return result;
                }

                StaffClass st = simpleNameMap[simpleName].First();

                result.Staffs.Add(new ImportScheduleResolvedStaff
                {
                    GUID = st.GUID,
                    staff_id = st.staff_id,
                    staff_name = st.staff_name,
                    staff_simple_name = st.staff_simple_name
                });
            }

            result.IsSuccess = true;
            result.ErrorMessage = "";
            return result;
        }

        /// <summary>
        /// 檢查同一天同一人是否已出現在其他班別
        /// dayStaffUsedMap:
        /// key = 日期|staff_guid
        /// value = 班別資訊
        /// </summary>
        public static bool TryCheckAndRegisterDailyDuplicate(
            Dictionary<string, string> dayStaffUsedMap,
            string dateText,
            ImportScheduleResolvedStaff staff,
            string currentShiftInfo,
            out string errorMessage)
        {
            errorMessage = "";

            if (dayStaffUsedMap == null)
            {
                errorMessage = "dayStaffUsedMap 未建立";
                return false;
            }

            if (staff == null || string.IsNullOrWhiteSpace(staff.GUID))
            {
                errorMessage = "Staff 資料不完整";
                return false;
            }

            string key = $"{dateText}|{staff.GUID}";

            if (dayStaffUsedMap.ContainsKey(key))
            {
                errorMessage = $"同一天同一人不可出現在多個班別：{staff.staff_simple_name}，已出現在 {dayStaffUsedMap[key]}";
                return false;
            }

            dayStaffUsedMap[key] = currentShiftInfo;
            return true;
        }

        /// <summary>
        /// 取得解析後 Staff 的工號清單字串
        /// </summary>
        public static string JoinStaffIds(List<ImportScheduleResolvedStaff> staffs)
        {
            if (staffs == null || staffs.Count == 0) return "";
            return string.Join(",", staffs.Select(x => x.staff_id));
        }

        /// <summary>
        /// 取得解析後 Staff 的姓名清單字串
        /// </summary>
        public static string JoinStaffNames(List<ImportScheduleResolvedStaff> staffs)
        {
            if (staffs == null || staffs.Count == 0) return "";
            return string.Join(",", staffs.Select(x => x.staff_name));
        }

        /// <summary>
        /// 取得解析後 Staff 的簡名清單字串
        /// </summary>
        public static string JoinSimpleNames(List<ImportScheduleResolvedStaff> staffs)
        {
            if (staffs == null || staffs.Count == 0) return "";
            return string.Join(",", staffs.Select(x => x.staff_simple_name));
        }
    }
}