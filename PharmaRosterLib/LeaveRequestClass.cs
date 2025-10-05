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
    /// 請假 / 特例 (Leave Request)
    /// </summary>
    [Description("leave_requests")]
    public class LeaveRequestClass
    {
        /// <summary>唯一識別碼 (GUID)</summary>
        [JsonPropertyName("GUID")]
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>人員 GUID</summary>
        [JsonPropertyName("staff_guid")]
        [Description("VARCHAR,50,INDEX")]
        public string staff_guid { get; set; }

        /// <summary>開始日期 (yyyy-MM-dd)</summary>
        [JsonPropertyName("start_date")]
        [Description("DATETIME,50,NONE")]
        public string start_date { get; set; }

        /// <summary>結束日期 (yyyy-MM-dd)</summary>
        [JsonPropertyName("end_date")]
        [Description("DATETIME,50,NONE")]
        public string end_date { get; set; }

        /// <summary>原因</summary>
        [JsonPropertyName("reason")]
        [Description("TEXT,50,NONE")]
        public string reason { get; set; }

        /// <summary>建立時間</summary>
        [JsonPropertyName("created_at")]
        [Description("DATETIME,50,NONE")]
        public string created_at { get; set; }

        [JsonPropertyName("staff_info")]
        public StaffClass staff_info { get; set; } = new StaffClass();
    }
    public static class LeaveRequestExtensions
    {
        /// <summary>
        /// 取得指定員工在特定日期的假單清單
        /// </summary>
        /// <param name="leaves">假單清單</param>
        /// <param name="staff_guid">員工 GUID</param>
        /// <param name="date">指定日期 (yyyy-MM-dd)</param>
        /// <returns>符合條件的假單清單 (若無則為空)</returns>
        public static List<LeaveRequestClass> GetLeavesByStaffAndDate(this List<LeaveRequestClass> leaves, string staff_guid, string date)
        {
            if (leaves == null || leaves.Count == 0) return new List<LeaveRequestClass>();
            if (string.IsNullOrWhiteSpace(staff_guid) || string.IsNullOrWhiteSpace(date)) return new List<LeaveRequestClass>();

            DateTime targetDate;
            if (!DateTime.TryParse(date, out targetDate)) return new List<LeaveRequestClass>();

            return leaves.Where(l =>
                l.staff_guid == staff_guid &&
                DateTime.TryParse(l.start_date, out var startDate) &&
                DateTime.TryParse(l.end_date, out var endDate) &&
                targetDate.Date >= startDate.Date &&
                targetDate.Date <= endDate.Date
            ).ToList();
        }
        /// <summary>
        /// 檢查指定員工在特定日期是否有請假
        /// </summary>
        /// <param name="leaves">假單清單</param>
        /// <param name="staff_guid">員工 GUID</param>
        /// <param name="date">指定日期 (yyyy-MM-dd)</param>
        /// <returns>若該員工當天有請假，回傳 true；否則 false</returns>
        public static bool HasLeaveOnDate(this List<LeaveRequestClass> leaves, string staff_guid, string date)
        {
            if (leaves == null || leaves.Count == 0) return false;
            if (string.IsNullOrWhiteSpace(staff_guid) || string.IsNullOrWhiteSpace(date)) return false;

            if (!DateTime.TryParse(date, out DateTime targetDate)) return false;

            return leaves.Any(l =>
                l.staff_guid == staff_guid &&
                DateTime.TryParse(l.start_date, out var startDate) &&
                DateTime.TryParse(l.end_date, out var endDate) &&
                targetDate.Date >= startDate.Date &&
                targetDate.Date <= endDate.Date
            );
        }

    }
}

