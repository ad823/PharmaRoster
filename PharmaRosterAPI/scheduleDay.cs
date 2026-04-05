using Basic;
using Microsoft.AspNetCore.Mvc;
using MyOffice;
using NPOI.SS.UserModel;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Ocsp;
using PharmaRosterLib;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using static NPOI.HSSF.Util.HSSFColor;
using static SQLUI.Table;

namespace PharmaRosterAPI
{
    [Route("phar_roster_api/[controller]")]
    [ApiController]
    public class scheduleDay : ControllerBase
    {
        /// <summary>
        /// 初始化 ScheduleDay 相關資料表
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於建立與排班 (ScheduleDay) 相關的資料表，若資料表不存在則會自動建立。  
        /// 主要包含以下表格：  
        /// - <c>ScheduleDayClass</c>：行事曆每日排班資訊  
        /// - <c>RequiredShiftClass</c>：每日需求班次設定  
        /// - <c>AssignedShiftClass</c>：每日實際指派班次  
        /// - <c>ScheduleLogClass</c>：排班運算與調整紀錄  
        /// - <c>WorkloadSummaryClass</c>：工作量彙總資料  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "init"
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "init",
        ///   "Result": "初始化 scheduleDay 資料表完成",
        ///   "TimeTaken": "45ms",
        ///   "Data": [
        ///     { "TableName": "schedule_day", "Status": "Created" },
        ///     { "TableName": "required_shift", "Status": "Created" },
        ///     { "TableName": "assigned_shift", "Status": "Created" },
        ///     { "TableName": "schedule_log", "Status": "Created" },
        ///     { "TableName": "workload_summary", "Status": "Created" }
        ///   ]
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 系統例外：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "init",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - 本 API 僅進行資料表檢查與建立，不會影響既有資料。  
        /// - 若資料表已存在，回傳狀態會顯示為 "Exists"。  
        /// - 建議在系統初始化或版本升級時執行一次。  
        /// </remarks>
        /// <param name="returnData">統一封裝的請求與回應物件</param>
        /// <returns>JSON 格式的回應字串，包含建立的資料表狀態</returns>
        [HttpPost("init")]
        public string init([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "init";

            try
            {
                List<Table> tables = new List<Table>();
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<ScheduleDayClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<RequiredShiftClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<AssignedShiftClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<ScheduleLogClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<WorkloadSummaryClass>());

                returnData.Code = 200;
                returnData.Data = tables;
                returnData.Result = "初始化 scheduleDay 資料表完成";
                returnData.TimeTaken = $"{timer}";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception: {ex.Message}";
                return returnData.JsonSerializationt(true);
            }
        }

        /// <summary>
        /// 查詢排班日 (ScheduleDay) 資料
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於查詢指定日期區間內的排班日資料。  
        /// 系統會依據傳入的參數 `ValueAry` 取得對應日期範圍內的排班資訊，  
        /// 並附加分頁資訊於回傳最外層欄位中。  
        ///  
        /// - 回傳每日的排班結構 (ScheduleDayClass)。  
        /// - 支援分頁查詢。  
        /// - 可用於顯示月曆、週檢視或明細列表等排班畫面。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "get_schedule_days",
        ///   "ValueAry": [
        ///     "date_start=2025-10-01",
        ///     "date_end=2025-10-31",
        ///     "page=1",
        ///     "page_size=10"
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_schedule_days",
        ///   "Result": "共取得(10)筆資料",
        ///   "TimeTaken": "45ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "DAY001",
        ///       "date": "2025-10-01",
        ///       "created_at": "2025-10-01 08:00:00",
        ///       "updated_at": "2025-10-01 08:00:00",
        ///       "required_shifts": [],
        ///       "assigned_shifts": [],
        ///       "schedule_logs": []
        ///     },
        ///     {
        ///       "GUID": "DAY002",
        ///       "date": "2025-10-02",
        ///       "created_at": "2025-10-02 08:00:00",
        ///       "updated_at": "2025-10-02 08:00:00",
        ///       "required_shifts": [],
        ///       "assigned_shifts": [],
        ///       "schedule_logs": []
        ///     }
        ///   ],
        ///   "TotalCount": "10",
        ///   "TotalPages": "1",
        ///   "CurrentPage": "1",
        ///   "PageSize": "10"
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少查詢條件：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_schedule_days",
        ///   "Result": "ValueAry 不能為空"
        /// }
        /// ```
        ///
        /// - 系統例外錯誤：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_schedule_days",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - `ValueAry` 需包含查詢條件：  
        ///   - `date_start`：起始日期 (yyyy-MM-dd)。  
        ///   - `date_end`：結束日期 (yyyy-MM-dd)。  
        ///   - `page` (選填)：查詢頁次，預設為第 1 頁。  
        ///   - `page_size` (選填)：每頁筆數，預設為系統設定值。  
        /// - 回傳資料包含每日期的完整排班結構 (ScheduleDayClass)。  
        /// - 分頁資訊 (`TotalCount`, `TotalPages`, `CurrentPage`, `PageSize`) 會直接附加在最外層。  
        /// - 若查無資料，`Data` 會回傳空陣列。  
        /// </remarks>
        /// <param name="returnData">統一封裝的請求與回應物件，需包含 ValueAry 查詢條件 (日期區間與分頁設定)</param>
        /// <returns>JSON 格式的回應字串，包含排班日資料與分頁資訊</returns>
        [HttpPost("get_schedule_days")]
        public string get_schedule_days([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_schedule_days";

            try
            {
                // 驗證必填
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "ValueAry 不能為空";
                    return returnData.JsonSerializationt();
                }
                var sql_staff = MethodClass.GetSQLControl<StaffClass>();

                (List<ScheduleDayClass> scheduleDays, int totalCount, int totalPages, int pageSize, int currentPage) = GetScheduleDay(returnData.ValueAry);
                returnData.Code = 200;
                returnData.Result = $"共取得({scheduleDays.Count})筆資料";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = scheduleDays;
                returnData.AddExtra("TotalCount", totalCount.ToString());
                returnData.AddExtra("TotalPages", totalPages.ToString());
                returnData.AddExtra("CurrentPage", currentPage.ToString());
                returnData.AddExtra("PageSize", pageSize.ToString());
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }

        /// <summary>
        /// 查詢小夜班 (Swing Shift) 排班資料
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於查詢特定日期區間內的小夜班 (Swing Shift) 排班資料。  
        /// 系統會依據傳入的查詢條件 (`ValueAry`) 取得對應的 ScheduleDay 資料，  
        /// 並篩選出班別屬性為 <c>shift_type = "swing"</c> 的排班結果。  
        ///  
        /// - 僅回傳「小夜班」班別的 AssignedShift。  
        /// - 支援分頁回傳。  
        /// - 附加分頁資訊於 Extra 欄位：`TotalCount`, `TotalPages`, `CurrentPage`, `PageSize`。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "get_swing_schedules",
        ///   "ValueAry": [
        ///     "date_start=2025-10-01",
        ///     "date_end=2025-10-31",
        ///     "page=1",
        ///     "page_size=10"
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_swing_schedules",
        ///   "Result": "共取得(3)筆資料",
        ///   "TimeTaken": "58ms",
        ///   "Data": [
        ///     {
        ///       "date": "2025-10-03",
        ///       "AssignedShifts": [
        ///         {
        ///           "GUID": "AS001",
        ///           "date": "2025-10-03",
        ///           "staff_guid": "S001",
        ///           "req_shift_guid": "RQ001",
        ///           "status": "正常",
        ///           "workShiftRequirement": {
        ///             "day": "Friday",
        ///             "time": "15:30-24:00",
        ///             "department": "藥局",
        ///             "shift_type": "swing"
        ///           }
        ///         }
        ///       ]
        ///     },
        ///     {
        ///       "date": "2025-10-04",
        ///       "AssignedShifts": [
        ///         {
        ///           "GUID": "AS002",
        ///           "date": "2025-10-04",
        ///           "staff_guid": "S002",
        ///           "req_shift_guid": "RQ002",
        ///           "status": "正常",
        ///           "workShiftRequirement": {
        ///             "day": "Saturday",
        ///             "time": "16:00-24:00",
        ///             "department": "急診",
        ///             "shift_type": "swing"
        ///           }
        ///         }
        ///       ]
        ///     }
        ///   ],
        ///   "Extra": {
        ///     "TotalCount": "3",
        ///     "TotalPages": "1",
        ///     "CurrentPage": "1",
        ///     "PageSize": "10"
        ///   }
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 未提供 ValueAry：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_swing_schedules",
        ///   "Result": "ValueAry 不能為空"
        /// }
        /// ```
        ///
        /// - 系統例外錯誤：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_swing_schedules",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - 僅回傳班別類型為 <c>swing</c> (小夜班) 的排班資料。  
        /// - 需於 `ValueAry` 中提供 `date_start` 與 `date_end` 作為查詢條件。  
        /// - 若未指定 `page` 與 `page_size`，系統會套用預設值。  
        /// - 回傳的 ScheduleDay 不包含 RequiredShifts、LeaveRequests 等其他關聯資料。  
        /// - 所有分頁資訊皆附加於回傳物件的 Extra 欄位。  
        /// </remarks>
        /// <param name="returnData">封裝請求與回應資料的統一物件，需包含 ValueAry (日期與分頁參數)</param>
        /// <returns>JSON 格式回應，包含小夜班排班資料與分頁資訊</returns>
        [HttpPost("get_swing_schedules")]
        public string get_swing_schedules([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_schedule_days";

            try
            {
                // 驗證必填
                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "ValueAry 不能為空";
                    return returnData.JsonSerializationt();
                }
                var sql_staff = MethodClass.GetSQLControl<StaffClass>();

                (List<ScheduleDayClass> scheduleDays, int totalCount, int totalPages, int pageSize, int currentPage) = GetScheduleDay(returnData.ValueAry);

                var shift_groups = shiftGroup.GetShiftGroups(false);
                var shift_groups_buf = new List<ShiftGroupClass>();
                Dictionary<string, List<ShiftGroupClass>> keyValuePairs_shift_groups = shift_groups.CoverToDictionaryByGUID();
                foreach (ScheduleDayClass scheduleDay in scheduleDays)
                {
                    scheduleDay.RequiredShifts = new List<RequiredShiftClass>();
                    scheduleDay.ScheduleLogs = new List<ScheduleLogClass>();
                    scheduleDay.SpecialDays = new List<SpecialDayClass>();
                    scheduleDay.LeaveRequests = new List<LeaveRequestClass>();
                    List<AssignedShiftClass> assignedShifts = new List<AssignedShiftClass>();
                    foreach (var assignedShift in scheduleDay.AssignedShifts)
                    {
                        shift_groups_buf = keyValuePairs_shift_groups.SortDictionaryByGUID(assignedShift.req_shift_guid);
                        if(shift_groups_buf.Count > 0)
                        {
                            assignedShift.workShiftRequirement.shift_type = shift_groups_buf[0].shift_type;
                        }
                        if (assignedShift.workShiftRequirement.shift_type == ShiftTypeEnum.swing.GetEnumName())
                        {
                            assignedShifts.Add(assignedShift);
                        }
                    }
                    scheduleDay.AssignedShifts = assignedShifts; 
                }

                returnData.Code = 200;
                returnData.Result = $"共取得({scheduleDays.Count})筆資料";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = scheduleDays;
                returnData.AddExtra("TotalCount", totalCount.ToString());
                returnData.AddExtra("TotalPages", totalPages.ToString());
                returnData.AddExtra("CurrentPage", currentPage.ToString());
                returnData.AddExtra("PageSize", pageSize.ToString());
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }

        /// <summary>
        /// 新增或更新假日需求班次 (Holiday RequiredShift)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於設定「假日排班需求」，針對指定日期及星期 (day_of_week)  
        /// 建立或更新對應的 **每日需求班次 (RequiredShift)**。  
        ///  
        /// - 若該日期尚未建立行事曆 (ScheduleDay)，系統會自動建立。  
        /// - 若該日期與班群 (ShiftGroup) 尚無需求班次 → 新增。  
        /// - 若已存在相同日期與班群 → 更新。  
        /// - 系統會自動將班群內的班別 (`workShiftRanges`) 套用為該日的 `workShiftRequirements`。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "add_and_update_holiday_requiredShift",
        ///   "ValueAry": [ "day_of_week=Sunday" ],
        ///   "Data": {
        ///     "GUID": "",
        ///     "date": "2025-10-12",
        ///     "shift_group_guid": "G123-456B-789C",
        ///     "workShiftRequirements": [
        ///       {
        ///         "day": "Sunday",
        ///         "time": "08:00-16:00",
        ///         "required_count": "2",
        ///         "department": "門診"
        ///       },
        ///       {
        ///         "day": "Sunday",
        ///         "time": "16:00-24:00",
        ///         "required_count": "1",
        ///         "department": "急診"
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "add_and_update_holiday_requiredShift",
        ///   "Result": "新增(1)筆資料,修改(0)筆資料",
        ///   "TimeTaken": "42ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "R001",
        ///       "date": "2025-10-12",
        ///       "shift_group_guid": "G123-456B-789C",
        ///       "workShiftRequirements": [
        ///         {
        ///           "day": "Sunday",
        ///           "time": "08:00-16:00",
        ///           "required_count": "2",
        ///           "department": "門診"
        ///         },
        ///         {
        ///           "day": "Sunday",
        ///           "time": "16:00-24:00",
        ///           "required_count": "1",
        ///           "department": "急診"
        ///         }
        ///       ]
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少必要欄位：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_holiday_requiredShift",
        ///   "Result": "參數驗證失敗：date 格式錯誤"
        /// }
        /// ```
        ///
        /// - 班群不存在：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_holiday_requiredShift",
        ///   "Result": "參數驗證失敗：shift_group_guid 格式錯誤"
        /// }
        /// ```
        ///
        /// - 星期格式錯誤：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_holiday_requiredShift",
        ///   "Result": "參數驗證失敗：day_of_week 格式錯誤"
        /// }
        /// ```
        ///
        /// - 系統例外：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_holiday_requiredShift",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - <c>date</c> 為必填欄位，格式需為 yyyy-MM-dd。  
        /// - <c>shift_group_guid</c> 必須為有效的班群 GUID。  
        /// - <c>day_of_week</c> 可選填，若提供，需符合星期名稱 (Monday~Sunday)。  
        /// - 系統會將班群內的 <c>workShiftRanges</c> 轉為當日需求班次 <c>workShiftRequirements</c>。  
        /// - 若該日期已存在相同班群的 RequiredShift，則自動進行更新。  
        /// - `required_count` 可省略，預設為 "1"。  
        /// - 新增時由系統自動產生 GUID。  
        /// </remarks>
        /// <param name="returnData">統一封裝的請求與回應物件，需包含 Data (RequiredShiftClass) 與 ValueAry (day_of_week)</param>
        /// <returns>JSON 格式的回應字串，包含新增或修改結果</returns>
        [HttpPost("add_and_update_holiday_requiredShift")]
        public string add_and_update_holiday_requiredShift([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "add_and_update_holiday_requiredShift";

            try
            {
                // 解析參數
                string GetVal(string key) =>
                   returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string day_of_week = GetVal("day_of_week") ?? "";

                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                    return FailJson(returnData, -200, "Data 不能為空");

                RequiredShiftClass input = returnData.Data.ObjToClass<RequiredShiftClass>();
                if (input == null)
                    return FailJson(returnData, -200, "Data 格式錯誤或無有效資料");

                if (!string.IsNullOrWhiteSpace(day_of_week) && !day_of_week.Check_DayOfWeek_String())
                    return FailJson(returnData, -200, "參數驗證失敗：day_of_week 格式錯誤");

                var sql_RequiredShift = MethodClass.GetSQLControl<RequiredShiftClass>();
                var sql_ScheduleDay = MethodClass.GetSQLControl<ScheduleDayClass>();
                var sql_ShiftGroup = MethodClass.GetSQLControl<ShiftGroupClass>();
                var datas_add = new List<RequiredShiftClass>();
                var datas_update = new List<RequiredShiftClass>();

                string date = input.date;
                if (date.Check_Date_String() == false)
                    return FailJson(returnData, -200, "參數驗證失敗：date 格式錯誤");

                // === 2. 取得班群 ===
                ShiftGroupClass shiftGroupClass = shiftGroup.GetShiftGroups(input.shift_group_guid);
                if (shiftGroupClass == null)
                    return FailJson(returnData, -200, "參數驗證失敗：shift_group_guid 格式錯誤");

                // === 3. 檢查班群設定 ===
                if (shiftGroupClass.workShiftRanges == null || shiftGroupClass.workShiftRanges.Count == 0)
                    return FailJson(returnData, -200, $"該班群未設定任何班別，請先設定班表");

                // ✅ (1) 確認班別中有資料 → 將其星期改成目前要加入的星期

                shiftGroupClass.workShiftRanges = shiftGroupClass.workShiftRanges.Where(x => x.day.ToNormalizedWeekday() == day_of_week.ToNormalizedWeekday()).ToList();
                var wsr = shiftGroupClass.workShiftRanges;
                foreach (var range in wsr)
                {
                    range.day = date.StringToDateTime().DayOfWeek.ToString(); // 更新星期
                }
                shiftGroupClass.workShiftRanges = wsr;

                var input_wsr = input.workShiftRequirements;
                foreach (var range in input_wsr)
                {
                    range.day = date.StringToDateTime().DayOfWeek.ToString(); // 更新星期
                }
                input.workShiftRequirements = input_wsr;

                // === 4. 確認 ScheduleDay 是否存在
                date = date.StringToDateTime().ToDateString();
                string sql = $"SELECT * FROM {sql_ScheduleDay.Database}.{sql_ScheduleDay.TableName} WHERE `date` = '{date}'";
                DataTable dt = sql_ScheduleDay.WtrteCommandAndExecuteReader(sql);
                if (dt.Rows.Count == 0)
                {
                    ScheduleDayClass scheduleDayClass = new ScheduleDayClass()
                    {
                        GUID = Guid.NewGuid().ToString(),
                        date = date,
                        created_at = DateTime.Now.ToDateTimeString_6(),
                        updated_at = DateTime.Now.ToDateTimeString_6()
                    };
                    sql_ScheduleDay.AddRow(null, scheduleDayClass.ClassToSQL<ScheduleDayClass>());
                }

                // === 5. 新增或更新 RequiredShift ===
                List<object[]> list_objects = sql_RequiredShift.GetRowsByDefult(null, new string[] { "shift_group_guid", "date" }, new string[] { input.shift_group_guid, date });

                if (list_objects.Count == 0)
                {
                    // 新增
                    input.GUID = Guid.NewGuid().ToString();
                    input.created_at = DateTime.Now.ToDateTimeString_6();
                    input.updated_at = DateTime.Now.ToDateTimeString_6();
                    input.workShiftRequirements = shiftGroupClass.workShiftRanges.UpdateRequirements(new List<WorkShiftRequirementClass>());
                    datas_add.Add(input);
                }
                else
                {
                    // 更新
                    RequiredShiftClass requiredShift = list_objects[0].SQLToClass<RequiredShiftClass>();
                    input.GUID = requiredShift.GUID;
                    input.created_at = requiredShift.created_at;
                    input.updated_at = DateTime.Now.ToDateTimeString_6();
                    List<WorkShiftRequirementClass> workShiftRequirements = shiftGroupClass.workShiftRanges.UpdateRequirements(requiredShift.workShiftRequirements);
                    workShiftRequirements = workShiftRequirements.UpdateRequirements(input.workShiftRequirements);
                    input.workShiftRequirements = workShiftRequirements;
                    datas_update.Add(input);
                }

                if (datas_add.Count > 0) sql_RequiredShift.AddRows(null, datas_add.ClassToSQL<RequiredShiftClass>());
                if (datas_update.Count > 0) sql_RequiredShift.UpdateByDefulteExtra(null, datas_update.ClassToSQL<RequiredShiftClass>());

                // === 6. 成功回傳 ===
                returnData.Code = 200;
                returnData.Result = $"新增({datas_add.Count})筆資料,修改({datas_update.Count})筆資料";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = datas_add.Concat(datas_update).ToList();
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }


        /// <summary>
        /// 新增或更新每日需求班次 (RequiredShift)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於針對指定日期與班群 (ShiftGroup)，新增或更新每日需求班次 (RequiredShiftClass)。  
        /// - 若該日期尚未建立行事曆紀錄 (ScheduleDay)，系統會自動建立。  
        /// - 若該日期與班群尚無需求班次，則新增。  
        /// - 若已存在相同日期與班群的紀錄，則更新。  
        /// - 同時會依據 ShiftGroup 內定義的 <c>workShiftRanges</c> 自動產生或更新 <c>workShiftRequirements</c>。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "add_and_update_requiredShift",
        ///   "ValueAry": [],
        ///   "Data": {
        ///     "GUID": "",
        ///     "date": "2025-09-22",
        ///     "shift_group_guid": "G1234567-89AB-CDEF-0123-456789ABCDEF",
        ///     "required_count": "3",
        ///     "workShiftRequirements": [
        ///       {
        ///         "day": "Monday",
        ///         "time": "08:00-16:00",
        ///         "required_count": "2",
        ///         "department": "門診"
        ///       },
        ///       {
        ///         "day": "Monday",
        ///         "time": "16:00-00:00",
        ///         "required_count": "1",
        ///         "department": "急診"
        ///       }
        ///     ],
        ///     "created_at": "",
        ///     "updated_at": ""
        ///   }
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "add_and_update_requiredShift",
        ///   "Result": "新增(1)筆資料,修改(0)筆資料",
        ///   "TimeTaken": "41ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "ABC123-DEF456",
        ///       "date": "2025-09-22",
        ///       "shift_group_guid": "G1234567-89AB-CDEF-0123-456789ABCDEF",
        ///       "required_count": "3",
        ///       "workShiftRequirements": [
        ///         {
        ///           "day": "Monday",
        ///           "time": "08:00-16:00",
        ///           "required_count": "2",
        ///           "department": "門診"
        ///         },
        ///         {
        ///           "day": "Monday",
        ///           "time": "16:00-00:00",
        ///           "required_count": "1",
        ///           "department": "急診"
        ///         }
        ///       ],
        ///       "created_at": "2025-09-22 08:00:00",
        ///       "updated_at": "2025-09-22 08:00:00"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少必要欄位：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_requiredShift",
        ///   "Result": "參數驗證失敗：date 格式錯誤"
        /// }
        /// ```
        ///
        /// - 班群 GUID 錯誤：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_requiredShift",
        ///   "Result": "參數驗證失敗：shift_group_guid 格式錯誤"
        /// }
        /// ```
        ///
        /// - Data 格式錯誤：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_requiredShift",
        ///   "Result": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// - 系統例外：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_requiredShift",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - <c>date</c> 必須為有效日期字串 (yyyy-MM-dd)。  
        /// - <c>shift_group_guid</c> 必須存在於 ShiftGroup 資料表。  
        /// - 系統會自動建立對應的 <c>ScheduleDay</c> 記錄 (若尚未存在)。  
        /// - 新增時由系統自動產生 GUID，更新時會沿用既有 GUID。  
        /// - <c>required_count</c> 為字串數值，需轉換為整數後使用。  
        /// - <c>workShiftRequirements</c> 需包含各班別的需求人數，並指定 day、time、department。  
        /// </remarks>
        /// <param name="returnData">統一封裝的請求與回應物件，需包含 Data 欄位 (RequiredShiftClass)</param>
        /// <returns>JSON 格式的回應字串，包含新增/更新筆數與狀態</returns>
        [HttpPost("add_and_update_requiredShift")]
        public string add_and_update_requiredShift([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "add_and_update_requiredShift";

            try
            {
                // 解析參數
                string GetVal(string key) =>
                   returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];
                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                RequiredShiftClass input = returnData.Data.ObjToClass<RequiredShiftClass>();
                if (input == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }
                var sql_RequiredShift = MethodClass.GetSQLControl<RequiredShiftClass>();
                var sql_ScheduleDay = MethodClass.GetSQLControl<ScheduleDayClass>();
                var sql_ShiftGroup = MethodClass.GetSQLControl<ShiftGroupClass>();
                var output = new List<RequiredShiftClass>();
                var datas_add = new List<RequiredShiftClass>();
                var datas_update = new List<RequiredShiftClass>();

                string date = input.date;
                if (date.Check_Date_String() == false)
                {
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：date 格式錯誤";
                    return returnData.JsonSerializationt();
                }
                ShiftGroupClass shiftGroupClass = shiftGroup.GetShiftGroups(input.shift_group_guid);
                if (shiftGroupClass == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：shift_group_guid 格式錯誤";
                    return returnData.JsonSerializationt();
                }
                //ShiftRequirementList
                date = date.StringToDateTime().ToDateString();
                string sql = $"SELECT * FROM {sql_ScheduleDay.Database}.{sql_ScheduleDay.TableName} WHERE `date` = '{date}'";
                DataTable dt = sql_ScheduleDay.WtrteCommandAndExecuteReader(sql);
                if (dt.DataTableToRowList().Count == 0)
                {
                    ScheduleDayClass scheduleDayClass = new ScheduleDayClass();
                    scheduleDayClass.GUID = Guid.NewGuid().ToString();
                    scheduleDayClass.date = date;
                    scheduleDayClass.created_at = DateTime.Now.ToDateTimeString_6();
                    scheduleDayClass.updated_at = DateTime.Now.ToDateTimeString_6();
                    sql_ScheduleDay.AddRow(null, scheduleDayClass.ClassToSQL<ScheduleDayClass>());
                }

                List<object[]> list_objects = sql_RequiredShift.GetRowsByDefult(null, new string[] { "shift_group_guid", "date" }, new string[] { input.shift_group_guid, date });

                if (list_objects.Count == 0)
                {
                    input.GUID = Guid.NewGuid().ToString();
                    input.created_at = DateTime.Now.ToDateTimeString_6();
                    input.updated_at = DateTime.Now.ToDateTimeString_6();
                    input.workShiftRequirements = shiftGroupClass.workShiftRanges.UpdateRequirements(new List<WorkShiftRequirementClass>());
                    datas_add.Add(input);
                }
                else
                {
                    RequiredShiftClass requiredShift = list_objects[0].SQLToClass<RequiredShiftClass>();
                    input.GUID = requiredShift.GUID;
                    input.created_at = requiredShift.created_at;
                    input.updated_at = DateTime.Now.ToDateTimeString_6();
                    bool flag_update = true;
                    for(int i = 0; i < input.workShiftRequirements.Count; i++)
                    {
                        if (input.workShiftRequirements[i].department == "其他" && input.workShiftRequirements[i].shift_type == "holiday")
                        {
                            flag_update = false;
                            break;
                        }
                    }
                    if(flag_update)
                    {
                        List<WorkShiftRequirementClass> workShiftRequirements = shiftGroupClass.workShiftRanges.UpdateRequirements(requiredShift.workShiftRequirements);
                        workShiftRequirements = workShiftRequirements.UpdateRequirements(input.workShiftRequirements);
                        input.workShiftRequirements = workShiftRequirements;
                    }
               
                    datas_update.Add(input);
                }


                if (datas_add.Count > 0) sql_RequiredShift.AddRows(null, datas_add.ClassToSQL<RequiredShiftClass>());
                if (datas_update.Count > 0) sql_RequiredShift.UpdateByDefulteExtra(null, datas_update.ClassToSQL<RequiredShiftClass>());

                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Result = $"新增({datas_add.Count})筆資料,修改({datas_update.Count})筆資料";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = output;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }

        /// <summary>
        /// 刪除或更新每日需求班次 (RequiredShift)，並同步處理對應的已指派班次 (AssignedShift) 與歷史紀錄 (StaffScheduleHistory)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於刪除或更新指定的每日需求班次 (RequiredShift)，並依據傳入的 <c>workShiftRequirements</c> 判斷：  
        /// - 若需求班次人數調整為 <c>0</c> → 系統會自動刪除對應的 AssignedShift。  
        /// - 被刪除的 AssignedShift 對應之歷史紀錄 (StaffScheduleHistory) 狀態會更新為「取消」。  
        /// - 其他仍需保留的需求班次，會更新至資料庫。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "delete_requiredShifts",
        ///   "Data": [
        ///     {
        ///       "GUID": "R123-456B-789C",
        ///       "date": "2025-09-29",
        ///       "shift_group_guid": "G123-456B-789C",
        ///       "workShiftRequirements": [
        ///         { "day": "Monday", "time": "08:00-12:00", "required_count": "0", "department": "門診" },
        ///         { "day": "Monday", "time": "12:00-16:00", "required_count": "2", "department": "門診" }
        ///       ]
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "delete_requiredShifts",
        ///   "Result": "更新 [RequiredShift] (1)筆資料 ,[AssignedShift] (1)筆資料",
        ///   "Data": [
        ///     {
        ///       "GUID": "R123-456B-789C",
        ///       "date": "2025-09-29",
        ///       "shift_group_guid": "G123-456B-789C",
        ///       "workShiftRequirements": [
        ///         { "day": "Monday", "time": "08:00-12:00", "required_count": "0", "department": "門診" },
        ///         { "day": "Monday", "time": "12:00-16:00", "required_count": "2", "department": "門診" }
        ///       ]
        ///     }
        ///   ],
        ///   "TimeTaken": "52ms"
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少必要參數：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_requiredShifts",
        ///   "Result": "參數驗證失敗：guid 為必填"
        /// }
        /// ```
        ///
        /// - Data 為空或格式錯誤：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_requiredShifts",
        ///   "Result": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// - 系統例外：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_requiredShifts",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - <c>GUID</c> 為必填欄位，需對應到既有的 RequiredShift 紀錄。  
        /// - 傳入的 <c>workShiftRequirements</c> 會覆蓋原本的需求配置，並將人數為 0 的區間視為「刪除」。  
        /// - 被刪除的 AssignedShift，會同時更新 StaffScheduleHistory 的狀態為「取消」。  
        /// - 支援批次處理，可同時更新/刪除多筆 RequiredShift。  
        /// </remarks>
        /// <param name="returnData">統一封裝的請求與回應物件，需包含 Data 陣列 (每筆至少需有 GUID)</param>
        /// <returns>JSON 格式的回應字串，包含更新的 RequiredShift、刪除的 AssignedShift 與處理狀態</returns>
        [HttpPost("delete_requiredShifts")]
        public string delete_requiredShifts([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_requiredShifts";

            try
            {
                var sql_RequiredShift = MethodClass.GetSQLControl<RequiredShiftClass>();
                var sql_AssignedShift = MethodClass.GetSQLControl<AssignedShiftClass>();
                var sql_StaffHistory = MethodClass.GetSQLControl<StaffScheduleHistoryClass>();

                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<RequiredShiftClass> input = returnData.Data.ObjToClass<List<RequiredShiftClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                List<RequiredShiftClass> requiredShifts_update = new List<RequiredShiftClass>();
                var AssignedShift_delete = new List<AssignedShiftClass>();

                // === 2. 刪除流程 ===
                foreach (var temp in input)
                {
                    // 檢核必填
                    if (temp.GUID.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "參數驗證失敗：guid 為必填";
                        return returnData.JsonSerializationt();
                    }

                    List<object[]> list_objects = sql_RequiredShift.GetRowsByDefult(null, "GUID", temp.GUID);
                    if (list_objects.Count == 0) continue;

                    RequiredShiftClass requiredShift = list_objects[0].SQLToClass<RequiredShiftClass>();

                    // 更新需求班次
                    requiredShift.workShiftRequirements = requiredShift.workShiftRequirements.UpdateRequirements(temp.workShiftRequirements);
                    requiredShifts_update.Add(requiredShift);

                    // 找到該 RequiredShift 對應的 AssignedShift
                    var AssignedShift_buf = sql_AssignedShift
                        .GetRowsByDefult(null, new string[] { "req_shift_guid" }, new string[] { temp.GUID })
                        .SQLToClass<AssignedShiftClass>();

                    // 找出人數 = 0 的需求班次
                    List<WorkShiftRequirementClass> workShiftRequirements = (from _temp in requiredShift.workShiftRequirements
                                                                             where _temp.required_count.StringToInt32() == 0
                                                                             select _temp).ToList();
                    workShiftRequirements = workShiftRequirements.FilterByDate(requiredShift.date);

                    foreach (var ass in AssignedShift_buf)
                    {
                        if (ass.workShiftRequirement == null) continue;

                        // 如果班次被清空 → 加入刪除清單
                        if (workShiftRequirements.ContainsTime(ass.workShiftRequirement))
                        {
                            AssignedShift_delete.Add(ass);
                        }
                    }
                }

                // === 3. 更新資料庫 ===
                if (requiredShifts_update.Count > 0)
                    sql_RequiredShift.UpdateByDefulteExtra(null, requiredShifts_update.ClassToSQL<RequiredShiftClass>());

                if (AssignedShift_delete.Count > 0)
                {
                    // 1. 刪除 AssignedShift
                    sql_AssignedShift.DeleteExtra(null, AssignedShift_delete.ClassToSQL<AssignedShiftClass>());

                    // 2. 更新 StaffScheduleHistory → 狀態改為「取消」
                    foreach (var ass in AssignedShift_delete)
                    {
                        var histories = sql_StaffHistory
                            .GetRowsByDefult(null, new string[] { "assigned_shift_guid" }, new string[] { ass.GUID })
                            .SQLToClass<StaffScheduleHistoryClass>();

                        foreach (var his in histories.Where(h => h.status == "正常"))
                        {
                            his.status = "取消";
                            his.updated_at = DateTime.Now.ToDateTimeString_6();
                            sql_StaffHistory.UpdateByDefulteExtra(null, new List<object[]> { his.ClassToSQL<StaffScheduleHistoryClass>() });
                        }
                    }
                }

                // === 4. 成功回傳 ===
                returnData.Code = 200;
                returnData.Result = $"更新 [RequiredShift] ({requiredShifts_update.Count})筆資料 ,[AssignedShift] ({AssignedShift_delete.Count})筆資料";
                returnData.Data = requiredShifts_update;
                returnData.TimeTaken = $"{timer}";
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }


        /// <summary>
        /// 新增或更新每日需求班次 (RequiredShift)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於針對指定日期與班群 (ShiftGroup)，新增或更新每日需求班次 (RequiredShiftClass)。  
        /// - 若該日期尚未建立行事曆紀錄 (ScheduleDay)，系統會自動建立。  
        /// - 若該日期與班群尚無需求班次，則新增。  
        /// - 若已存在相同日期與班群的紀錄，則更新。  
        /// - 同時會依據 ShiftGroup 內定義的 <c>workShiftRanges</c> 自動產生或更新 <c>workShiftRequirements</c>。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "add_and_update_requiredShift",
        ///   "ValueAry": [],
        ///   "Data": {
        ///     "GUID": "",
        ///     "date": "2025-09-22",
        ///     "shift_group_guid": "G1234567-89AB-CDEF-0123-456789ABCDEF",
        ///     "required_count": "3",
        ///     "workShiftRequirements": [
        ///       {
        ///         "day": "Monday",
        ///         "time": "08:00-16:00",
        ///         "required_count": "2",
        ///         "department": "門診"
        ///       },
        ///       {
        ///         "day": "Monday",
        ///         "time": "16:00-00:00",
        ///         "required_count": "1",
        ///         "department": "急診"
        ///       }
        ///     ],
        ///     "created_at": "",
        ///     "updated_at": ""
        ///   }
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "add_and_update_requiredShift",
        ///   "Result": "新增(1)筆資料,修改(0)筆資料",
        ///   "TimeTaken": "41ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "ABC123-DEF456",
        ///       "date": "2025-09-22",
        ///       "shift_group_guid": "G1234567-89AB-CDEF-0123-456789ABCDEF",
        ///       "required_count": "3",
        ///       "workShiftRequirements": [
        ///         {
        ///           "day": "Monday",
        ///           "time": "08:00-16:00",
        ///           "required_count": "2",
        ///           "department": "門診"
        ///         },
        ///         {
        ///           "day": "Monday",
        ///           "time": "16:00-00:00",
        ///           "required_count": "1",
        ///           "department": "急診"
        ///         }
        ///       ],
        ///       "created_at": "2025-09-22 08:00:00",
        ///       "updated_at": "2025-09-22 08:00:00"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少必要欄位：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_requiredShift",
        ///   "Result": "參數驗證失敗：date 格式錯誤"
        /// }
        /// ```
        ///
        /// - 班群 GUID 錯誤：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_requiredShift",
        ///   "Result": "參數驗證失敗：shift_group_guid 格式錯誤"
        /// }
        /// ```
        ///
        /// - Data 格式錯誤：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_requiredShift",
        ///   "Result": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// - 系統例外：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_requiredShift",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - <c>date</c> 必須為有效日期字串 (yyyy-MM-dd)。  
        /// - <c>shift_group_guid</c> 必須存在於 ShiftGroup 資料表。  
        /// - 系統會自動建立對應的 <c>ScheduleDay</c> 記錄 (若尚未存在)。  
        /// - 新增時由系統自動產生 GUID，更新時會沿用既有 GUID。  
        /// - <c>required_count</c> 為字串數值，需轉換為整數後使用。  
        /// - <c>workShiftRequirements</c> 需包含各班別的需求人數，並指定 day、time、department。  
        /// </remarks>
        /// <param name="returnData">統一封裝的請求與回應物件，需包含 Data 欄位 (RequiredShiftClass)</param>
        /// <returns>JSON 格式的回應字串，包含新增/更新筆數與狀態</returns>
        [HttpPost("add_and_update_assigned_shifts")]
        public async Task<string> add_and_update_assigned_shifts([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "add_and_update_assigned_shifts";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<AssignedShiftClass> input = returnData.Data.ObjToClass<List<AssignedShiftClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var sql_AssignedShift = MethodClass.GetSQLControl<AssignedShiftClass>();
                var sql_StaffHistory = MethodClass.GetSQLControl<StaffScheduleHistoryClass>();

                var assignedShifts_add = new List<AssignedShiftClass>();
                var assignedShifts_update = new List<AssignedShiftClass>();
                var histories_add = new List<StaffScheduleHistoryClass>();
                var histories_update = new List<StaffScheduleHistoryClass>();
                var output = new List<AssignedShiftClass>();

                var shiftGroups = await shiftGroup.GetShiftGroupsAsync();
                var keyValuePairs_shiftGroups = shiftGroups.CoverToDictionaryByGUID();

                var requiredShifts = await GetAllRequiredShiftsAsync();
                var keyValuePairs_requiredShifts = requiredShifts.CoverToDictionaryByGUID();

                string[] dates = input.Select(x => x.date).Distinct().ToArray();
                List<ScheduleDayClass> scheduleDays = await GetSchedulesDayAsync(dates);

                // 用來收集所有檢核失敗訊息
                var validationErrors = new List<string>();

                foreach (var assignedShift in input)
                {
                    if (assignedShift.workShiftRequirement == null) continue;
                    if (!assignedShift.date.Check_Date_String()) continue;

                    var requiredShifts_buf = keyValuePairs_requiredShifts.SortDictionaryByGUID(assignedShift.req_shift_guid);
                    if (requiredShifts_buf.Count == 0) continue;

                    string shift_group_guid = requiredShifts_buf[0].shift_group_guid;
                    var shiftGroups_buf = keyValuePairs_shiftGroups.SortDictionaryByGUID(shift_group_guid);
                    if (shiftGroups_buf.Count == 0) continue;

                    StaffClass staff = shiftGroups_buf[0].SerchStaff(assignedShift.staff_guid);
                    if (staff == null) continue;

                    ScheduleDayClass scheduleDay = scheduleDays.SerchByDate(assignedShift.date);
                    if (scheduleDay == null) continue;

                    // ✅ 呼叫共用工具
                    ValidateAndAddOrUpdateAssignedShift(
                        assignedShift, scheduleDay, staff, shiftGroups_buf[0],
                        assignedShifts_add, assignedShifts_update,
                        histories_add, histories_update,
                        output, validationErrors
                    );
                }

                // === 3. 成功回傳 ===
                if (validationErrors.Count > 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "排班檢核失敗：\n" + string.Join("\n", validationErrors);
                    return returnData.JsonSerializationt(true);
                }
                // === 資料庫寫入 ===
                if (assignedShifts_add.Count > 0) sql_AssignedShift.AddRows(null, assignedShifts_add.ClassToSQL<AssignedShiftClass>());
                if (assignedShifts_update.Count > 0) sql_AssignedShift.UpdateByDefulteExtra(null, assignedShifts_update.ClassToSQL<AssignedShiftClass>());

                if (histories_add.Count > 0) sql_StaffHistory.AddRows(null, histories_add.ClassToSQL<StaffScheduleHistoryClass>());
                if (histories_update.Count > 0) sql_StaffHistory.UpdateByDefulteExtra(null, histories_update.ClassToSQL<StaffScheduleHistoryClass>());

                returnData.Code = 200;
                returnData.Result = $"新增({assignedShifts_add.Count})筆資料, 修改({assignedShifts_update.Count})筆資料\n" +
                                    $"歷程新增({histories_add.Count})筆, 歷程更新({histories_update.Count})筆";

                returnData.Result = "排班新增成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = output;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }

        /// <summary>
        /// 新增或更新已指派的排班紀錄 (AssignedShift)，並同步維護歷史紀錄 (StaffScheduleHistory)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於新增或更新指定日期與人員的排班紀錄 (AssignedShift)，並自動同步寫入或更新對應的歷史紀錄 (StaffScheduleHistory)。  
        /// - 若無舊的排班 → 新增 AssignedShift 與對應的 StaffScheduleHistory。  
        /// - 若已有排班 → 更新 AssignedShift 與 StaffScheduleHistory。  
        /// - 系統會自動進行排班檢核 (ScheduleValidator)，若檢核失敗則中止處理。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "add_and_update_assigned_shifts",
        ///   "Data": [
        ///     {
        ///       "GUID": "",
        ///       "date": "2025-09-29",
        ///       "req_shift_guid": "REQ-123456789",
        ///       "staff_guid": "STAFF-987654321",
        ///       "status": "",
        ///       "workShiftRequirement": {
        ///         "day": "Monday",
        ///         "time": "08:00-12:00",
        ///         "required_count": "1",
        ///         "department": "門診"
        ///       }
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "add_and_update_assigned_shifts",
        ///   "Result": "新增(1)筆資料, 修改(0)筆資料, 歷程新增(1)筆, 歷程更新(0)筆",
        ///   "TimeTaken": "58ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "A123-456B-789C",
        ///       "date": "2025-09-29",
        ///       "req_shift_guid": "REQ-123456789",
        ///       "staff_guid": "STAFF-987654321",
        ///       "status": "正常",
        ///       "workShiftRequirement": {
        ///         "day": "Monday",
        ///         "time": "08:00-12:00",
        ///         "required_count": "1",
        ///         "department": "門診"
        ///       }
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少必要欄位：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_assigned_shifts",
        ///   "Result": "Data 不能為空"
        /// }
        /// ```
        ///
        /// - 格式錯誤：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_assigned_shifts",
        ///   "Result": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// - 排班檢核失敗：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_assigned_shifts",
        ///   "Result": "排班檢核失敗：連續上班超過規範"
        /// }
        /// ```
        ///
        /// - 系統例外：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_assigned_shifts",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - <c>date</c> 必須為有效日期字串 (yyyy-MM-dd)。  
        /// - <c>req_shift_guid</c> 必須對應到既有的 RequiredShift 紀錄。  
        /// - <c>staff_guid</c> 必須存在於對應班群 (ShiftGroup) 成員中。  
        /// - <c>workShiftRequirement</c> 為必填，需包含 day、time、department。  
        /// - 新增時會自動產生 GUID，更新時沿用既有 GUID。  
        /// - StaffScheduleHistory 會隨 AssignedShift 的新增或更新而同步維護。  
        /// </remarks>
        /// <param name="returnData">統一封裝的請求與回應物件，需包含 Data 陣列 (每筆至少需有 date、req_shift_guid、staff_guid、workShiftRequirement)</param>
        /// <returns>JSON 格式的回應字串，包含新增/修改的 AssignedShift 筆數與歷史紀錄處理狀態</returns>    
        /// <summary>
        /// 刪除已指派的排班紀錄 (AssignedShift)，並同步更新人員排班歷程 (StaffScheduleHistory)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於刪除指定的 **AssignedShift** 紀錄，並將對應的 **StaffScheduleHistory** 狀態更新為「取消」。  
        /// - 若傳入的 GUID 存在 → 刪除 AssignedShift，並更新所有對應歷程紀錄的狀態。  
        /// - 若傳入的 GUID 不存在 → 忽略該筆，不進行刪除。  
        /// - 刪除時會保留歷程紀錄，但將其標記為「取消」。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "delete_assigned_shifts",
        ///   "Data": [
        ///     { "GUID": "A123-456B-789C" },
        ///     { "GUID": "B234-567C-890D" }
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "delete_assigned_shifts",
        ///   "Result": "刪除 AssignedShift(2)筆，更新 StaffScheduleHistory 狀態為取消(2)筆",
        ///   "TimeTaken": "52ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "A123-456B-789C",
        ///       "date": "2025-09-29",
        ///       "req_shift_guid": "R123-456B-789C",
        ///       "staff_guid": "S123-456B-789C",
        ///       "status": "正常",
        ///       "created_at": "2025-09-20 10:00:00",
        ///       "updated_at": "2025-09-25 14:00:00"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少必要參數：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_assigned_shifts",
        ///   "Result": "參數驗證失敗：guid 為必填"
        /// }
        /// ```
        ///
        /// - Data 為空或格式錯誤：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_assigned_shifts",
        ///   "Result": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// - 系統例外：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_assigned_shifts",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - <c>GUID</c> 為必填欄位，需對應到既有的 AssignedShift 紀錄。  
        /// - 支援批次刪除，傳入多筆 GUID 時會逐一檢核並刪除。  
        /// - 若該 AssignedShift 已刪除，相關的 StaffScheduleHistory 會保留，但狀態改為「取消」。  
        /// - 系統會確保刪除 AssignedShift 的同時，維護人員排班歷程的一致性。  
        /// </remarks>
        /// <param name="returnData">統一封裝的請求與回應物件，需包含 Data 陣列 (每筆至少需有 GUID)</param>
        /// <returns>JSON 格式的回應字串，包含刪除筆數與更新歷程的狀態</returns>
        [HttpPost("delete_assigned_shifts")]
        public string delete_assigned_shifts([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_assigned_shifts";

            try
            {
                var sql_assigned_shifts = MethodClass.GetSQLControl<AssignedShiftClass>();
                var sql_staffHistory = MethodClass.GetSQLControl<StaffScheduleHistoryClass>();

                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<AssignedShiftClass> input = returnData.Data.ObjToClass<List<AssignedShiftClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var output = new List<AssignedShiftClass>();
                var list_delete = new List<object[]>();
                var histories_update = new List<StaffScheduleHistoryClass>();

                // === 2. 刪除流程 ===
                foreach (var temp in input)
                {
                    if (temp.GUID.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "參數驗證失敗：guid 為必填";
                        return returnData.JsonSerializationt();
                    }

                    List<object[]> list_objects = sql_assigned_shifts.GetRowsByDefult(null, "GUID", temp.GUID);
                    if (list_objects.Count == 0) continue;

                    var assignedShift = list_objects[0].SQLToClass<AssignedShiftClass>();
                    if (assignedShift == null) continue;

                    // 加入刪除清單
                    list_delete.Add(list_objects[0]);
                    output.Add(assignedShift);

                    // === 找對應的 StaffScheduleHistory ===
                    var histories = sql_staffHistory
                        .GetRowsByDefult(null, new string[] { "assigned_shift_guid" }, new string[] { assignedShift.GUID })
                        .SQLToClass<StaffScheduleHistoryClass>();

                    foreach (var his in histories)
                    {
                        his.status = "取消"; // 修改狀態
                        his.updated_at = DateTime.Now.ToDateTimeString_6();
                        histories_update.Add(his);
                    }
                }

                // === 3. 執行刪除與更新 ===
                if (list_delete.Count > 0) sql_assigned_shifts.DeleteExtra(null, list_delete);
                if (histories_update.Count > 0) sql_staffHistory.UpdateByDefulteExtra(null, histories_update.ClassToSQL<StaffScheduleHistoryClass>());

                // === 4. 成功回傳 ===
                returnData.Code = 200;
                returnData.Result = $"刪除 AssignedShift({list_delete.Count})筆，更新 StaffScheduleHistory 狀態為取消({histories_update.Count})筆";
                returnData.Result = "刪除排班成功";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = output;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }

        [HttpGet("download_monthly_shift_schedule_pdf")]
        public IActionResult download_monthly_shift_schedule_pdf(string year_month)
        {
            returnData returnData = new returnData();
            returnData.ValueAry.Add($"year_month={year_month}");
            return download_monthly_shift_schedule_pdf(returnData);
        }
        /// <summary>
        /// 下載指定月份的排班表 PDF
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於產生並下載指定年月的「排班月報表 (PDF)」。  
        /// 系統會根據傳入的 `year_month` (格式：yyyy-MM)，讀取當月的排班資料，  
        /// 自動產生對應格式的 PDF 排班表，以 A4 橫式輸出。  
        ///
        /// 報表內容包含：
        /// - 每週的日期、星期對應位置。
        /// - 小夜班、假日班、大夜班各班次人員姓名縮寫。
        /// - HDR 標註、TPN、化療等特殊欄位。
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "download_monthly_shift_schedule_pdf",
        ///   "ValueAry": [
        ///     "year_month=2025-10"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 📤 Response 範例 (成功)
        /// - 成功時回傳 **application/octet-stream** 檔案串流 (PDF)，  
        ///   並於 Response Header 內附帶檔案下載資訊：
        ///
        /// ### 🔹 Response Header
        /// ```
        /// Content-Type: application/octet-stream
        /// Content-Disposition: attachment; filename="2025-10月份值班表.pdf"; filename*=UTF-8''2025-10%E6%9C%88%E4%BB%BD%E5%80%BC%E7%8F%AD%E8%A1%A8.pdf
        /// Access-Control-Expose-Headers: Content-Disposition, Content-Length, Content-Type
        /// ```
        ///
        /// ### 🔹 檔案名稱  
        /// `yyyy-MM月份值班表.pdf`
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少必要參數：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "download_monthly_shift_schedule_pdf",
        ///   "Result": "參數驗證失敗：year_month 為必填"
        /// }
        /// ```
        ///
        /// - year_month 格式錯誤：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "download_monthly_shift_schedule_pdf",
        ///   "Result": "參數驗證失敗：year_month 格式錯誤"
        /// }
        /// ```
        ///
        /// - 系統例外錯誤 (如排班資料缺失、PDF 生成失敗)：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "download_monthly_shift_schedule_pdf",
        ///   "Result": "Exception: 無法產生 PDF，資料來源異常"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - `year_month` 必須為合法年月格式 (yyyy-MM)。  
        /// - 若該月份無任何排班資料，產生的 PDF 會為空白模板。  
        /// - PDF 採用 A4 橫向 (Landscape) 格式輸出。  
        /// - 此 API 為「檔案下載」類型，非 JSON 回傳。  
        /// - 若需在前端觸發下載，請確保 HTTP Response 能處理 `Content-Disposition` header。  
        /// </remarks>
        /// <param name="returnData">統一封裝的請求物件，需包含 ValueAry 內之 year_month 參數</param>
        /// <returns>成功時回傳 PDF 檔案串流，失敗時回傳 JSON 錯誤訊息</returns>
        [HttpPost("download_monthly_shift_schedule_pdf")]       
        public IActionResult download_monthly_shift_schedule_pdf([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "download_monthly_shift_schedule_pdf";

            try
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] >>> API 開始執行 download_monthly_shift_schedule_pdf");

                if (returnData.Data == null)
                {
                    Console.WriteLine("[警告] returnData.Data 為 null");
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return new JsonResult(returnData);
                }

                // 解析參數
                string GetVal(string key) =>
                    returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string year_month = GetVal("year_month") ?? "";
                Console.WriteLine($"[DEBUG] year_month = {year_month}");

                if (string.IsNullOrEmpty(year_month))
                {
                    Console.WriteLine("[錯誤] year_month 參數為空");
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：year_month 為必填";
                    return new JsonResult(returnData);
                }
                if (!Check_YearMonth_String(year_month))
                {
                    Console.WriteLine("[錯誤] year_month 格式錯誤");
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：year_month 格式錯誤";
                    return new JsonResult(returnData);
                }

          

                string[] date_strings = year_month.Split('-');
                if (date_strings.Length != 2)
                {
                    Console.WriteLine("[錯誤] year_month 分割後長度錯誤");
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：year_month 格式錯誤";
                    return new JsonResult(returnData);
                }

                int year = date_strings[0].StringToInt32();
                int month = date_strings[1].StringToInt32();
                Console.WriteLine($"[DEBUG] year={year}, month={month}");

                if (year < 1900 || year > 2100 || month < 1 || month > 12)
                {
                    Console.WriteLine("[錯誤] year 或 month 超出範圍");
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：year_month 格式錯誤";
                    return new JsonResult(returnData);
                }

                // 取得該月份的第一天與天數
                DateTime firstDay = new DateTime(year, month, 1);
                DateTime lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));
                Console.WriteLine($"[INFO] 期間：{firstDay:yyyy-MM-dd} ~ {lastDay:yyyy-MM-dd}");

                List<ScheduleDayClass> scheduleDays = scheduleDay.GetScheduleDay(firstDay.ToDateString('-'), lastDay.ToDateString('-'));
                List<StaffClass> staffs = staff.GetAllStaffs();

                Console.WriteLine($"[INFO] scheduleDays.Count={scheduleDays?.Count}, staffs.Count={staffs?.Count}");

                Dictionary<string, List<StaffClass>> keyValuePairs_staffs = staffs.CoverToDictionaryByGUID();

                int daysInMonth = DateTime.DaysInMonth(year, month);

                DateTime firstDayOfMonth = new DateTime(year, month, 1);
                DateTime lastDayOfMonth = new DateTime(year, month, daysInMonth);

                int offsetToMonday = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;
                DateTime firstMonday = firstDayOfMonth.AddDays(-offsetToMonday);

                int offsetToSunday = 7 - ((int)lastDayOfMonth.DayOfWeek + 6) % 7 - 1;
                DateTime lastSunday = lastDayOfMonth.AddDays(offsetToSunday);

                Console.WriteLine($"[INFO] 輪循範圍：{firstMonday:yyyy-MM-dd} ~ {lastSunday:yyyy-MM-dd}");

                Console.WriteLine("[INFO] 開始反序列化 monthly_shift_schedule_xlsx");
                SheetClass sheet = new SheetClass();
                Console.WriteLine("[INFO] 反序列化完成");
        
                string title = $"{year}-{month}月份值班表";
                if((lastSunday - firstMonday).Days <= 35) sheet = monthly_shift_schedule_5_week_excel.xlsx.JsonDeserializet<SheetClass>();
                else sheet = monthly_shift_schedule_6_week_excel.xlsx.JsonDeserializet<SheetClass>();
                sheet.Rows[0].Cell[0].Text = $"{title}";

                // === 輪循從星期一到星期日（涵蓋整個月）===
                int weekIndex = 1;
                for (DateTime d = firstMonday; d <= lastSunday; d = d.AddDays(1))
                {
                    int dayOfWeek = ((int)d.DayOfWeek + 6) % 7 + 1; // Monday=1...Sunday=7
                    string dateStr = d.ToString("yyyy-MM-dd");

                    // 判斷是否為新的一週
                    if (dayOfWeek == 1 && d > firstMonday)
                    {
                        weekIndex++;
                    }

                    Console.WriteLine($"第{weekIndex}週 | {dateStr} | 星期{dayOfWeek}");

                    // === 可放入生成 Excel / 班表邏輯 ===

                    sheet.Rows[2 + (weekIndex - 1) * 7].Cell[dayOfWeek - 1].Text = $"{d.Day}";
                
                    //小夜班
                    if (d.Month == month)
                    {
                        string text = "";
                        bool flag_HDR = false;
                        ScheduleDayClass schedule = scheduleDays.Where(x => x.date.StringToDateTime().ToDateString('-') == d.ToDateString('-')).ToList().FirstOrDefault();
                        if (schedule == null) continue;
                        List<AssignedShiftClass> assignedShiftClasses = new List<AssignedShiftClass>();

                        text = "";
                        assignedShiftClasses = schedule.AssignedShifts.Where(x => x.workShiftRequirement.time == "12:00-20:00").ToList();
                        for (int i = 0; i < assignedShiftClasses.Count; i++)
                        {
                            AssignedShiftClass asg = assignedShiftClasses[i];
                            StaffClass staff = keyValuePairs_staffs.SortDictionaryByGUID(asg.staff_guid).FirstOrDefault();
                            if (staff == null) { continue; }

                            if (asg.workShiftRequirement.department.Contains("中藥"))
                            {
                                text += $"[{staff.staff_simple_name.Substring(0, 1)}]";
                            }
                            else
                            {
                                text += staff.staff_simple_name.Substring(0, 1);
                            }
                            if (asg.workShiftRequirement.hdr == "true") flag_HDR = true;
                        }
                        sheet.Rows[4 + (weekIndex - 1) * 7].Cell[dayOfWeek - 1].Text = $"{text}";

                        text = "";
                        assignedShiftClasses = schedule.AssignedShifts.Where(x => x.workShiftRequirement.time == "12:30-21:00").ToList();
                        for (int i = 0; i < assignedShiftClasses.Count; i++)
                        {
                            AssignedShiftClass asg = assignedShiftClasses[i];
                            StaffClass staff = keyValuePairs_staffs.SortDictionaryByGUID(asg.staff_guid).FirstOrDefault();
                            if (staff == null) { continue; }

                            if (asg.workShiftRequirement.department.Contains("中藥"))
                            {
                                text += $"[{staff.staff_simple_name.Substring(0, 1)}]";
                            }
                            else
                            {
                                text += staff.staff_simple_name.Substring(0, 1);
                            }
                            if (asg.workShiftRequirement.hdr == "true") flag_HDR = true;
                        }
                        sheet.Rows[5 + (weekIndex - 1) * 7].Cell[dayOfWeek - 1].Text = $"{text}";


                        text = "";
                        assignedShiftClasses = schedule.AssignedShifts.Where(x => x.workShiftRequirement.time == "13:30-22:00").ToList();
                        for (int i = 0; i < assignedShiftClasses.Count; i++)
                        {
                            AssignedShiftClass asg = assignedShiftClasses[i];
                            StaffClass staff = keyValuePairs_staffs.SortDictionaryByGUID(asg.staff_guid).FirstOrDefault();
                            if (staff == null) { continue; }

                            if (asg.workShiftRequirement.department.Contains("中藥"))
                            {
                                text += $"[{staff.staff_simple_name.Substring(0, 1)}]";
                            }
                            else
                            {
                                text += staff.staff_simple_name.Substring(0, 1);
                            }
                            if (asg.workShiftRequirement.hdr == "true") flag_HDR = true;
                        }
                        sheet.Rows[6 + (weekIndex - 1) * 7].Cell[dayOfWeek - 1].Text = $"{text}";

                  
                        text = "";
                        assignedShiftClasses = schedule.AssignedShifts.Where(x => x.workShiftRequirement.time == "14:30-23:00").ToList();
                        for (int i = 0; i < assignedShiftClasses.Count; i++)
                        {
                            AssignedShiftClass asg = assignedShiftClasses[i];
                            StaffClass staff = keyValuePairs_staffs.SortDictionaryByGUID(asg.staff_guid).FirstOrDefault();
                            if (staff == null) { continue; }

                            if (asg.workShiftRequirement.department.Contains("中藥"))
                            {
                                text += $"[{staff.staff_simple_name.Substring(0, 1)}]";
                            }
                            else
                            {
                                text += staff.staff_simple_name.Substring(0, 1);
                            }
                            if (asg.workShiftRequirement.hdr == "true") flag_HDR = true;
                        }
                        sheet.Rows[7 + (weekIndex - 1) * 7].Cell[dayOfWeek - 1].Text = $"{text}";


                        text = "";
                        assignedShiftClasses = schedule.AssignedShifts.Where(x => x.workShiftRequirement.time == "15:30-23:59").ToList();
                        for (int i = 0; i < assignedShiftClasses.Count; i++)
                        {
                            AssignedShiftClass asg = assignedShiftClasses[i];
                            StaffClass staff = keyValuePairs_staffs.SortDictionaryByGUID(asg.staff_guid).FirstOrDefault();
                            if (staff == null) { continue; }

                            if (asg.workShiftRequirement.department.Contains("中藥"))
                            {
                                text += $"[{staff.staff_simple_name.Substring(0, 1)}]";
                            }
                            else
                            {
                                text += staff.staff_simple_name.Substring(0, 1);
                            }
                            if (asg.workShiftRequirement.hdr == "true") flag_HDR = true;
                        }
                        text += "--";
                        assignedShiftClasses = schedule.AssignedShifts.Where(x => x.workShiftRequirement.time == "16:00-23:59").ToList();
                        for (int i = 0; i < assignedShiftClasses.Count; i++)
                        {
                            AssignedShiftClass asg = assignedShiftClasses[i];
                            StaffClass staff = keyValuePairs_staffs.SortDictionaryByGUID(asg.staff_guid).FirstOrDefault();
                            if (staff == null) { continue; }

                            if (asg.workShiftRequirement.department.Contains("中藥"))
                            {
                                text += $"[{staff.staff_simple_name.Substring(0, 1)}]";
                            }
                            else
                            {
                                text += staff.staff_simple_name.Substring(0, 1);
                            }
                            if (asg.workShiftRequirement.hdr == "true") flag_HDR = true;
                        }
                        sheet.Rows[8 + (weekIndex - 1) * 7].Cell[dayOfWeek - 1].Text = $"{text}";

                        if(flag_HDR)
                        {
                            sheet.Rows[2 + (weekIndex - 1) * 7].Cell[dayOfWeek - 1].Text += "(HDR)";
                        }
                    }

                    //大夜班
                    if (d.Month == month)
                    {
                        string text = "";
                        ScheduleDayClass schedule = scheduleDays.Where(x => x.date.StringToDateTime().ToDateString('-') == d.ToDateString('-')).ToList().FirstOrDefault();
                        if (schedule == null) continue;
                        List<AssignedShiftClass> assignedShiftClasses = new List<AssignedShiftClass>();

                        text = "";
                        assignedShiftClasses = schedule.AssignedShifts.Where(x => x.workShiftRequirement.time == "00:00-08:00"
                        && x.workShiftRequirement.department == "門診").ToList();
                        for (int i = 0; i < assignedShiftClasses.Count; i++)
                        {
                            AssignedShiftClass asg = assignedShiftClasses[i];
                            StaffClass staff = keyValuePairs_staffs.SortDictionaryByGUID(asg.staff_guid).FirstOrDefault();
                            if (staff == null) { continue; }
                            text += staff.staff_simple_name.Substring(0, 1);
                        }

                        text += "--";
                        assignedShiftClasses = schedule.AssignedShifts.Where(x => x.workShiftRequirement.time == "00:00-08:00"
                        && x.workShiftRequirement.department == "急診").ToList();
                        for (int i = 0; i < assignedShiftClasses.Count; i++)
                        {
                            AssignedShiftClass asg = assignedShiftClasses[i];
                            StaffClass staff = keyValuePairs_staffs.SortDictionaryByGUID(asg.staff_guid).FirstOrDefault();
                            if (staff == null) { continue; }
                            text += staff.staff_simple_name.Substring(0, 1);
                        }
                        sheet.Rows[3 + (weekIndex - 1) * 7].Cell[dayOfWeek - 1].Text = $"{text}";

                    }

                    //假日班
                    if (d.Month == month)
                    {
                        string text = "";
                        ScheduleDayClass schedule = scheduleDays.Where(x => x.date.StringToDateTime().ToDateString('-') == d.ToDateString('-')).ToList().FirstOrDefault();
                        if (schedule == null) continue;
                        List<AssignedShiftClass> assignedShiftClasses = new List<AssignedShiftClass>();

                        text = "";
                        assignedShiftClasses = schedule.AssignedShifts.Where(x => (x.workShiftRequirement.time == "07:30-16:00" || x.workShiftRequirement.time == "08:00-16:00") 
                        && x.workShiftRequirement.department == "門診").ToList();
                        for (int i = 0; i < assignedShiftClasses.Count; i++)
                        {
                            AssignedShiftClass asg = assignedShiftClasses[i];
                            StaffClass staff = keyValuePairs_staffs.SortDictionaryByGUID(asg.staff_guid).FirstOrDefault();
                            if (staff == null) { continue; }
                            text += staff.staff_simple_name.Substring(0, 1);
                        }

                     
                        assignedShiftClasses = schedule.AssignedShifts.Where(x => (x.workShiftRequirement.time == "07:30-16:00" || x.workShiftRequirement.time == "08:00-16:00")
                        && x.workShiftRequirement.department == "急診").ToList();
                        if (assignedShiftClasses.Count > 0) text += "--"; 
                        for (int i = 0; i < assignedShiftClasses.Count; i++)
                        {
                            AssignedShiftClass asg = assignedShiftClasses[i];
                            StaffClass staff = keyValuePairs_staffs.SortDictionaryByGUID(asg.staff_guid).FirstOrDefault();
                            if (staff == null) { continue; }
                            text += staff.staff_simple_name.Substring(0, 1);
                        }

                        //國定假日
                        assignedShiftClasses = schedule.AssignedShifts.Where(x => (x.workShiftRequirement.time == "08:00-12:00")
                      && x.workShiftRequirement.department == "其他").ToList();
                        if (assignedShiftClasses.Count > 0) text += "(";
                        for (int i = 0; i < assignedShiftClasses.Count; i++)
                        {
                            AssignedShiftClass asg = assignedShiftClasses[i];
                            StaffClass staff = keyValuePairs_staffs.SortDictionaryByGUID(asg.staff_guid).FirstOrDefault();
                            if (staff == null) { continue; }
                            text += (staff.staff_simple_name.Substring(0, 1));
                        }
                        if (assignedShiftClasses.Count > 0) text += ")";
                        if (dayOfWeek == 6 || dayOfWeek == 7 || true)
                        {
                            if (text.Replace("--", "").StringIsEmpty() == false)
                            {
                                sheet.Rows[4 + (weekIndex - 1) * 7].Cell[dayOfWeek - 1].Text = $"{text}";
                            }
                        }
                        //else
                        //{
                        //    if (text.Replace("--", "").StringIsEmpty() == false)
                        //    {
                        //        sheet.Rows[2 + (weekIndex - 1) * 6].Cell[dayOfWeek - 1].Text += $"({text})";
                        //    }
                        //}
                        


        
                        text = "[TPN]";
                        assignedShiftClasses = schedule.AssignedShifts.Where(x => (x.workShiftRequirement.time == "07:30-16:00" || x.workShiftRequirement.time == "08:00-16:00")  && x.workShiftRequirement.department == "TPN").ToList();
                        for (int i = 0; i < assignedShiftClasses.Count; i++)
                        {
                            AssignedShiftClass asg = assignedShiftClasses[i];
                            StaffClass staff = keyValuePairs_staffs.SortDictionaryByGUID(asg.staff_guid).FirstOrDefault();
                            if (staff == null) { continue; }
                            text += staff.staff_simple_name.Substring(0, 1);
                        }
                        if (text.Replace("[TPN]", "").StringIsEmpty() == false)
                        {
                            sheet.Rows[5 + (weekIndex - 1) * 7].Cell[dayOfWeek - 1].Text += $"{text}";
                        }
                        text = "[化療]";
                 
                        assignedShiftClasses = schedule.AssignedShifts.Where(x => (x.workShiftRequirement.time == "08:00-12:00")  && x.workShiftRequirement.department == "化療").ToList();
                        for (int i = 0; i < assignedShiftClasses.Count; i++)
                        {
                            AssignedShiftClass asg = assignedShiftClasses[i];
                            StaffClass staff = keyValuePairs_staffs.SortDictionaryByGUID(asg.staff_guid).FirstOrDefault();
                            if (staff == null) { continue; }
                            text += staff.staff_simple_name.Substring(0, 1);
                        }
                        if (text.Replace("[化療]", "").StringIsEmpty() == false)
                        {
                            sheet.Rows[5 + (weekIndex - 1) * 7].Cell[dayOfWeek - 1].Text += $"{text}";
                        }
                    }
                }

                Console.WriteLine("[INFO] 生成 PDF 中...");
                byte[] bytes_pdf = sheet.SaveToPDF(PdfSharp.PageSize.A4, PdfSharp.PageOrientation.Landscape);
                Console.WriteLine("[INFO] PDF 生成完成，大小：" + bytes_pdf.Length);

                Stream stream = new MemoryStream(bytes_pdf);
                string contentType = "application/octet-stream";
                string originalName = $"schedule_{month}.pdf";
                string utf8FileName = Uri.EscapeDataString(originalName);

                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{originalName}\"; filename*=UTF-8''{utf8FileName}");
                Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition, Content-Length, Content-Type");

                Console.WriteLine("[INFO] API 成功結束，準備回傳檔案");
                return File(stream, contentType);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==== [例外發生] ====");
                Console.WriteLine($"時間：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"訊息：{ex.Message}");
                Console.WriteLine($"堆疊：{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"內層例外：{ex.InnerException.Message}");
                    Console.WriteLine($"內層堆疊：{ex.InnerException.StackTrace}");
                }
                Console.WriteLine("=====================");

                returnData.Code = -200;
                returnData.Result = $"例外：{ex.Message}";
                return new JsonResult(returnData);
            }
        }


        /// <summary>
        /// 下載人員班表清單 PDF（依班別類型與月份）
        /// </summary>
        /// <remarks>
        /// ## 📘 功能說明  
        /// 依據指定的年月 (<c>year_month</c>) 與班別類型 (<c>shift_type</c>)，  
        /// 匯出整個醫療單位的人員排班列表，並以 PDF 檔案形式下載。
        ///
        /// 生成的 PDF 內容會依每位人員的排班資訊產生表格，  
        /// 每頁最多 30 人，底部附上「時段代碼對照表」，適用於排班公告、管理列印等用途。
        ///
        /// ## ⚙️ 執行流程  
        /// 1. 驗證 `year_month` 與 `shift_type` 格式。  
        /// 2. 呼叫 `get_schedule_days()` 取得指定月份所有班表資料。  
        /// 3. 撈取所有人員資料 (每頁 30 筆為一組)。  
        /// 4. 將每位員工的班表依日期填入 DataTable。  
        /// 5. 建立 NPOI SheetClass，套用字型、樣式、欄寬設定。  
        /// 6. 每頁最後一列生成「班別時間代碼對照表」。  
        /// 7. 匯出成 PDF，橫向 A4 格式輸出。
        ///
        /// ## 🧩 表格欄位說明  
        /// | 欄位 | 說明 |
        /// |------|------|
        /// | 序號 | 員工流水號（每頁重新計數） |
        /// | 人員 | 員工姓名 |
        /// | 1~31 | 對應每一天的班別代碼（以數字代表不同時段） |
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "download_staff_list_shift_schedule_pdf",
        ///   "ValueAry": [
        ///     "year_month=2026-01",
        ///     "shift_type=swing"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 🔍 參數說明  
        /// | 參數名稱 | 類型 | 必填 | 範例 | 說明 |
        /// |------------|------|------|------|------|
        /// | year_month | string | ✅ | 2026-01 | 查詢的年月（格式 yyyy-MM） |
        /// | shift_type | string | ✅ | day | 班別類型，需存在於 <see cref="ShiftTypeEnum"/> 中 |
        ///
        /// ## 📤 回傳說明 (成功)
        /// **Header：**
        /// ```
        /// Content-Disposition: attachment; filename="schedule_staff_list_1.pdf"
        /// Content-Type: application/octet-stream
        /// ```
        ///
        /// **Body：**
        /// - PDF 檔案串流內容（自動觸發下載）
        ///
        /// ## ❌ 錯誤回傳範例  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "參數驗證失敗：shift_type 格式錯誤"
        /// }
        /// ```
        /// 或：
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "例外：Object reference not set to an instance of an object."
        /// }
        /// ```
        ///
        /// ## 🖨️ PDF 樣式說明  
        /// - 紙張：A4 橫式 (Landscape)  
        /// - 每頁 30 位人員  
        /// - 字型：微軟正黑體  
        /// - 一般文字：14pt 黑色，置中對齊  
        /// - 標題文字：18pt 藍色 (ROYAL_BLUE)  
        /// - 邊框樣式：<c>BorderStyle.Thin</c>  
        /// - 最末列顯示時間代碼對照表 (例：`【1】:08:00-16:00　【2】:16:00-23:00`)  
        ///
        /// ## 📑 注意事項  
        /// - 若 `shift_type` 不存在於 `ShiftTypeEnum`，會直接返回錯誤。  
        /// - 若當月無排班資料，仍會生成空白 PDF 檔。  
        /// - 此 API 產出檔案流，不建議直接在前端 JSON 測試工具開啟。  
        /// - 前端呼叫時請設定：
        ///   ```js
        ///   axios.post(url, data, { responseType: 'blob' })
        ///   ```
        ///   以正確接收 PDF 檔案。  
        /// </remarks>
        /// <param name="returnData">封裝請求參數與回傳資料的物件，包含 year_month、shift_type。</param>
        /// <returns>PDF 檔案串流結果，供瀏覽器觸發下載。</returns>
        [HttpPost("download_staff_list_shift_schedule_pdf")]
        public IActionResult download_staff_list_shift_schedule_pdf([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "download_staff_list_shift_schedule_pdf";

            try
            {

                if (returnData.Data == null)
                {
                    Console.WriteLine("[警告] returnData.Data 為 null");
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return new JsonResult(returnData);
                }

                // 解析參數
                string GetVal(string key) =>
                    returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];


                string year_month = GetVal("year_month") ?? "";
                string shift_type = GetVal("shift_type") ?? "";
                if (shift_type.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：shift_type 格式錯誤";
                    return new JsonResult(returnData.JsonSerializationt(true));
                }
                if(new ShiftTypeEnum().GetEnumNames().Contains(shift_type) == false)
                {
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：shift_type 格式錯誤";
                    return new JsonResult(returnData.JsonSerializationt(true));
                }
                string[] date_strings = year_month.Split('-');
                if (date_strings.Length != 2)
                {
                    Console.WriteLine("[錯誤] year_month 分割後長度錯誤");
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：year_month 格式錯誤";
                    return new JsonResult(returnData.JsonSerializationt(true));
                }

                int year = date_strings[0].StringToInt32();
                int month = date_strings[1].StringToInt32();
                Console.WriteLine($"[DEBUG] year={year}, month={month}");
                int daysInMonth = DateTime.DaysInMonth(year, month);

                DateTime firstDayOfMonth = new DateTime(year, month, 1);
                DateTime lastDayOfMonth = new DateTime(year, month, daysInMonth);

                returnData returnDat_get_schedule_days = new returnData();
                returnDat_get_schedule_days.ValueAry.Add($"date_start={firstDayOfMonth.ToDateString('-')}");
                returnDat_get_schedule_days.ValueAry.Add($"date_end={lastDayOfMonth.ToDateString('-')}");
                returnDat_get_schedule_days = get_schedule_days(returnDat_get_schedule_days).JsonDeserializet<returnData>();

                List<ScheduleDayClass> scheduleDays = returnDat_get_schedule_days.Data.ObjToClass<List<ScheduleDayClass>>();

                int index = 0;
                List<SheetClass> sheets = new List<SheetClass>();
                DataTable dataTable = null;
                int page = 0;
                List<ShiftGroupClass> shiftGroupClasses = shiftGroup.GetShiftGroups();
                shiftGroupClasses = shiftGroupClasses.Where(x => x.shift_type.ToLower() == shift_type.ToLower()).ToList();

                List<WorkShiftRequirementClass> workShiftRequirements = shiftGroupClasses.SelectMany(x => x.workShiftRanges).ToList();

                var times = workShiftRequirements.Select(x=>x.time)
                    .Distinct()
                    .OrderBy(t => ParseStartMinutes(t))   // ⭐ 由早到晚
                    .ToList();
                var timeIndexMap = times
                    .Select((t, idx) => new { t, idx })
                    .ToDictionary(x => x.t, x => x.idx + 1); // 1-based

                List<StaffClass> staffClasses = staff.GetStaffs(new List<string>() { "pageSize=1000" }).staffClasses;

                List<ShiftGroupMemberClass> shiftGroupMembers = shiftGroupClasses.SelectMany(x => x.Members).Distinct().ToList();
                staffClasses = shiftGroupMembers.Select(x => x.staff_info).Distinct().ToList();
                foreach (var staff in staffClasses)
                {
             
                    if(dataTable == null)
                    {
                        dataTable = new DataTable();
                        dataTable.Columns.Add("序號");
                        dataTable.Columns.Add("人員");
                        for (int i = 0; i < daysInMonth; i++) dataTable.Columns.Add($"{i + 1}");
                        dataTable.NewRow();
                        DataRow dataRow = dataTable.NewRow();
                        for (int i = 0; i < daysInMonth; i++)
                        {
                            dataRow[$"{i + 1}"] = DayOfWeekToZh(new DateTime(year, month, i + 1).DayOfWeek);
                        }
                        dataTable.Rows.Add(dataRow);
                    }
         

                    var assignedShifts = scheduleDays.SelectMany(x => x.AssignedShifts).Where(x => (x.staff_guid == staff.GUID)).ToList();
                    if(assignedShifts.Count == 0)
                    {
                        continue;
                    }
                    bool flag_temp = false;
                    foreach (var asg in assignedShifts)
                    {
                        // ✅ 找 time 在第幾個（1-based）
                        string time = asg.workShiftRequirement.time;
                        int timeIndex = timeIndexMap.TryGetValue(time, out int idx) ? idx : -1;
                        if (timeIndex == -1)
                        {
                            continue;
                        }
                        int day = asg.date.StringToDateTime().Day;

                        // ✅ 1) 先找表單中是否已存在該員工
                        DataRow existRow = dataTable.AsEnumerable().FirstOrDefault(r => r.Field<string>("人員") == staff.staff_name);

                        // ✅ 2) 如果不存在就建立新列
                        if (existRow == null)
                        {
                            existRow = dataTable.NewRow();
                            existRow["序號"] = $"{page * 30 + index + 1}";
                            existRow["人員"] = staff.staff_name;
                            //existRow["權重"] = (staff.DayShiftCount + staff.DayShiftWeightBase).ToString();
                            dataTable.Rows.Add(existRow);
                        }

                       

                        // ✅ 寫入 (day) 欄位
                        existRow[$"{day}"] = timeIndex > 0 ? (timeIndex).ToString() : "◆";
                        flag_temp = true;
                    }
                    if (flag_temp) index++;
                    if (index >= 30)
                    {
                        dataTable.Rows.Add(dataTable.NewRow());
                        SheetClass sheet = dataTable.NPOI_GetSheetClass();
                        for (int i = 0; i < dataTable.Columns.Count; i++)
                        {
                            if (i == 1) sheet.ColumnsWidth.Add(100);
                            else sheet.ColumnsWidth.Add(50);
                        }

                        MyCellStyle myCellStyle = new MyCellStyle();
                        myCellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                        myCellStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                        myCellStyle.FontName = "微軟正黑體";
                        myCellStyle.FontHeightInPoints = 14;
                        myCellStyle.Color = (short)NPOI_Color.BLACK;
                        myCellStyle.FontHeight = 14;
                        myCellStyle.BorderBottom = myCellStyle.BorderLeft = myCellStyle.BorderRight = myCellStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                        myCellStyle.IsBold = false;
                        int style_normal_index = sheet.Add(myCellStyle);

                        myCellStyle = new MyCellStyle();
                        myCellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                        myCellStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                        myCellStyle.FontName = "微軟正黑體";
                        myCellStyle.FontHeightInPoints = 18;
                        myCellStyle.Color = (short)NPOI_Color.ROYAL_BLUE;
                        myCellStyle.FontHeight = 18;
                        myCellStyle.BorderBottom = myCellStyle.BorderLeft = myCellStyle.BorderRight = myCellStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                        myCellStyle.IsBold = true;
                        int style_numtext_index = sheet.Add(myCellStyle);

                        myCellStyle = new MyCellStyle();
                        myCellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
                        myCellStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                        myCellStyle.FontName = "微軟正黑體";
                        myCellStyle.FontHeightInPoints = 18;
                        myCellStyle.FontHeight = 18;
                        myCellStyle.Color = (short)NPOI_Color.ROYAL_BLUE;
                        myCellStyle.BorderBottom = myCellStyle.BorderLeft = myCellStyle.BorderRight = myCellStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                        myCellStyle.IsBold = true;
                        int style_none_index = sheet.Add(myCellStyle);
                        List<MyOffice.CellValue> remove_cells = new List<MyOffice.CellValue>();
                        for (int i = 0; i < sheet.Rows.Count; i++)
                        {
                            for (int k = 0; k < sheet.Rows[i].Cell.Count; k++)
                            {
                                if(k >= 2) sheet.Rows[i].Cell[k].CellStyle_index = style_numtext_index;
                                else sheet.Rows[i].Cell[k].CellStyle_index = style_normal_index;
                                if (i == sheet.Rows.Count -1)
                                {
                                    if(k == 0)
                                    {
                                        foreach (var kv in timeIndexMap)
                                        {
                                            sheet.Rows[i].Cell[k].Text += $"【{kv.Value}】:{kv.Key} ";
                                        }
                                       
                                        sheet.Rows[i].Cell[k].ColStart = 0;
                                        sheet.Rows[i].Cell[k].ColEnd = dataTable.Columns.Count - 1;
                                        sheet.Rows[i].Cell[k].CellStyle_index = style_none_index;
                                    }
                                    //else
                                    //{
                                    //    sheet.RemoveCellValue(i, k);
                                    //}
       
                                }
                            } 
                            
                        }
                        sheet.RemoveRowsTextEmpty(1);
                        sheet.RemoveRowsTextEmpty(sheet.Rows.Count - 1);
                        sheets.Add(sheet);
                        index = 0;
                        dataTable = null;
                        page++;
                    }
                }
                // ✅ 最後一頁不足 30 列：補滿後輸出
                if (dataTable != null && index > 0)
                {
                    // dataTable 第一列是星期 headerRow，所以真正資料列數 = dataTable.Rows.Count - 1
                    int currentStaffRowCount = dataTable.Rows.Count - 1;

                    // ✅ 補空白列直到滿 30 位人員列
                    while (currentStaffRowCount < 30)
                    {
                        dataTable.Rows.Add(dataTable.NewRow());
                        currentStaffRowCount++;
                    }

                    // ✅ 最後再加一列用來印 timeIndexMap 對照表
                    dataTable.Rows.Add(dataTable.NewRow());

                    // ======= 你原本產 Sheet 的整段邏輯（原封不動搬過來） =======
                    SheetClass sheet = dataTable.NPOI_GetSheetClass();
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        if (i == 1) sheet.ColumnsWidth.Add(100);
                        else sheet.ColumnsWidth.Add(50);
                    }

                    MyCellStyle myCellStyle = new MyCellStyle();
                    myCellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                    myCellStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                    myCellStyle.FontName = "微軟正黑體";
                    myCellStyle.FontHeightInPoints = 14;
                    myCellStyle.Color = (short)NPOI_Color.BLACK;
                    myCellStyle.FontHeight = 14;
                    myCellStyle.BorderBottom = myCellStyle.BorderLeft = myCellStyle.BorderRight = myCellStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                    myCellStyle.IsBold = false;
                    int style_normal_index = sheet.Add(myCellStyle);

                    myCellStyle = new MyCellStyle();
                    myCellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                    myCellStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                    myCellStyle.FontName = "微軟正黑體";
                    myCellStyle.FontHeightInPoints = 18;
                    myCellStyle.Color = (short)NPOI_Color.ROYAL_BLUE;
                    myCellStyle.FontHeight = 18;
                    myCellStyle.BorderBottom = myCellStyle.BorderLeft = myCellStyle.BorderRight = myCellStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                    myCellStyle.IsBold = true;
                    int style_numtext_index = sheet.Add(myCellStyle);

                    myCellStyle = new MyCellStyle();
                    myCellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
                    myCellStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                    myCellStyle.FontName = "微軟正黑體";
                    myCellStyle.FontHeightInPoints = 18;
                    myCellStyle.FontHeight = 18;
                    myCellStyle.Color = (short)NPOI_Color.ROYAL_BLUE;
                    myCellStyle.BorderBottom = myCellStyle.BorderLeft = myCellStyle.BorderRight = myCellStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                    myCellStyle.IsBold = true;
                    int style_none_index = sheet.Add(myCellStyle);

                    for (int i = 0; i < sheet.Rows.Count; i++)
                    {
                        for (int k = 0; k < sheet.Rows[i].Cell.Count; k++)
                        {
                            if (k >= 2) sheet.Rows[i].Cell[k].CellStyle_index = style_numtext_index;
                            else sheet.Rows[i].Cell[k].CellStyle_index = style_normal_index;

                            // ✅ 最後一列印 timeIndexMap
                            if (i == sheet.Rows.Count - 1)
                            {
                                if (k == 0)
                                {
                                    foreach (var kv in timeIndexMap.OrderBy(x => x.Value))
                                    {
                                        sheet.Rows[i].Cell[k].Text += $"【{kv.Value}】:{kv.Key} ";
                                    }

                                    sheet.Rows[i].Cell[k].ColStart = 0;
                                    sheet.Rows[i].Cell[k].ColEnd = dataTable.Columns.Count - 1;
                                    sheet.Rows[i].Cell[k].CellStyle_index = style_none_index;
                                }
                            }
                        }
                    }

                    // ✅ 這兩行你原本就有（維持）
                    sheet.RemoveRowsTextEmpty(1);
                    sheet.RemoveRowsTextEmpty(sheet.Rows.Count - 1);

                    sheets.Add(sheet);

                    // ✅ Reset 狀態
                    index = 0;
                    dataTable = null;
                    page++;
                }



                Console.WriteLine("[INFO] 生成 PDF 中...");
                byte[] bytes_pdf = sheets.SaveToPDF(PdfSharp.PageSize.A4, PdfSharp.PageOrientation.Landscape);
                Console.WriteLine("[INFO] PDF 生成完成，大小：" + bytes_pdf.Length);

                Stream stream = new MemoryStream(bytes_pdf);
                string contentType = "application/octet-stream";
                string originalName = $"schedule__staff_list_{month}.pdf";
                string utf8FileName = Uri.EscapeDataString(originalName);

                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{originalName}\"; filename*=UTF-8''{utf8FileName}");
                Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition, Content-Length, Content-Type");

                Console.WriteLine("[INFO] API 成功結束，準備回傳檔案");
                return File(stream, contentType);
            }
            catch (Exception ex)
            {
                Console.WriteLine("==== [例外發生] ====");
                Console.WriteLine($"時間：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"訊息：{ex.Message}");
                Console.WriteLine($"堆疊：{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"內層例外：{ex.InnerException.Message}");
                    Console.WriteLine($"內層堆疊：{ex.InnerException.StackTrace}");
                }
                Console.WriteLine("=====================");

                returnData.Code = -200;
                returnData.Result = $"例外：{ex.Message}";
                return new JsonResult(returnData);
            }
        }

        /// <summary>
        /// 檢查字串是否為合法的「yyyy-MM」格式，例如 "2025-10"
        /// </summary>
        /// <param name="input">輸入字串</param>
        /// <returns>合法回傳 true，否則 false</returns>
        public static bool Check_YearMonth_String(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;

            // 僅允許 "yyyy-MM" 格式
            if (!System.Text.RegularExpressions.Regex.IsMatch(input, @"^\d{4}-(0[1-9]|1[0-2])$"))
                return false;

            try
            {
                // 嘗試轉換為 DateTime 確保有效
                DateTime.ParseExact(input + "-01", "yyyy-MM-dd", null);
                return true;
            }
            catch
            {
                return false;
            }
        }
        [HttpPost("auto_schedule")]
        public async Task<string> auto_schedule([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "auto_schedule";

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string shift_group_guid = GetVal("shift_group_guid") ?? "";

                if (shift_group_guid == "")
                {
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：shift_group_guid 為必填";
                    return returnData.JsonSerializationt();
                }

                // 取得群組
                ShiftGroupClass shiftGroupClass = shiftGroup.GetShiftGroups(shift_group_guid);
                if (shiftGroupClass == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：找不到對應的 shift_group_guid";
                    return returnData.JsonSerializationt();
                }

                // === 🚩 如果是小夜班 → 直接切換呼叫 auto_schedule_swing ===
                if (shiftGroupClass.shift_type == ShiftTypeEnum.swing.GetEnumName() && shiftGroupClass.group_name.Contains("中藥") == false)
                {
                    return await auto_schedule_lastswing(returnData);
                }

                // === 其它班別照舊 ===
                return await auto_schedule_generic(returnData);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception: {ex.Message}";
                return returnData.JsonSerializationt();
            }
        }


        /// <summary>
        /// 自動排班 (Auto Schedule)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於針對指定日期範圍與班群 (ShiftGroup)，自動產生排班結果。  
        /// - 系統會依照已定義的需求班次 (RequiredShift) 與群組成員進行分配。  
        /// - 支援「預覽模式」(preview) 及「提交模式」(commit)：  
        ///   - preview：僅模擬計算，不寫入資料庫。  
        ///   - commit：實際寫入 AssignedShift 與 StaffScheduleHistory。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "auto_schedule",
        ///   "ValueAry": [
        ///     "date_start=2025-10-01",
        ///     "date_end=2025-10-07",
        ///     "shift_group_guid=G123-456B-789C",
        ///     "mode=preview"
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功，preview 模式)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "auto_schedule",
        ///   "Result": "[預覽模式] 自動排班完成，模擬 12 筆排班，12 筆歷程",
        ///   "TimeTaken": "85ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "D001",
        ///       "date": "2025-10-01",
        ///       "required_shifts": [
        ///         {
        ///           "GUID": "R001",
        ///           "shift_group_guid": "G123-456B-789C",
        ///           "workShiftRequirements": [
        ///             {
        ///               "day": "Monday",
        ///               "time": "08:00-16:00",
        ///               "required_count": "2",
        ///               "assigned_count": "2",
        ///               "department": "門診"
        ///             }
        ///           ]
        ///         }
        ///       ],
        ///       "assigned_shifts": [
        ///         {
        ///           "GUID": "A001",
        ///           "date": "2025-10-01",
        ///           "req_shift_guid": "R001",
        ///           "staff_guid": "S001",
        ///           "status": "正常"
        ///         }
        ///       ]
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少必要參數：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "auto_schedule",
        ///   "Result": "參數驗證失敗：date_start, date_end 為必填"
        /// }
        /// ```
        ///
        /// - 找不到班群：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "auto_schedule",
        ///   "Result": "參數驗證失敗：找不到對應的 shift_group_guid"
        /// }
        /// ```
        ///
        /// - 排班檢核失敗：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "auto_schedule",
        ///   "Result": "排班檢核失敗：\n[王小明] 2025-10-01 08:00-16:00 → 已超過連續上班天數限制"
        /// }
        /// ```
        ///
        /// - 系統例外：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "auto_schedule",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - <c>date_start</c>, <c>date_end</c> 為必填，且需為 yyyy-MM-dd 格式。  
        /// - <c>shift_group_guid</c> 必須為有效群組 GUID。  
        /// - mode 預設為 commit，若需模擬請傳入 "preview"。  
        /// - commit 模式會同時寫入 AssignedShift 與 StaffScheduleHistory，確保歷程一致性。  
        /// - 系統會依成員的 weight 與歷史排班數量進行排序，實現公平性分配。  
        /// - 排班檢核 (ScheduleValidator) 會檢查休假、連班、部門限制等規則，不符合者將跳過並記錄錯誤訊息。  
        /// </remarks>
        /// <param name="returnData">統一封裝的請求物件，需包含 ValueAry (日期範圍、shift_group_guid、mode)</param>
        /// <returns>JSON 格式的回應字串，包含排班結果或錯誤訊息</returns>
        [HttpPost("auto_schedule_generic")]
        public async Task<string> auto_schedule_generic([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "auto_schedule_generic";

            try
            {
                var sql_scheduleHistory = MethodClass.GetSQLControl<StaffScheduleHistoryClass>();
                var sql_assignedShift = MethodClass.GetSQLControl<AssignedShiftClass>();

                string GetVal(string key) =>
                    returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string date_start = GetVal("date_start") ?? "";
                string date_end = GetVal("date_end") ?? "";
                string shift_group_guid = GetVal("shift_group_guid") ?? "";
                string mode = (GetVal("mode") ?? "commit").ToLower(); // 預設 commit

                if (date_start == "" || date_end == "")
                {
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：date_start, date_end 為必填";
                    return returnData.JsonSerializationt();
                }
                if (shift_group_guid == "")
                {
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：shift_group_guid 為必填";
                    return returnData.JsonSerializationt();
                }

                // 1. 取得群組
                ShiftGroupClass shiftGroupClass = shiftGroup.GetShiftGroups(shift_group_guid);
                if (shiftGroupClass == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：找不到對應的 shift_group_guid";
                    return returnData.JsonSerializationt();
                }

                // 2. 取得排班日
                List<ScheduleDayClass> scheduleDays = await GetSchedulesDayAsync(date_start, date_end);

                // 3. 取得歷史紀錄
                var dt_scheduleHistory = await sql_scheduleHistory.WtrteCommandAndExecuteReaderAsync(
                    $"SELECT * FROM {sql_scheduleHistory.Database}.{sql_scheduleHistory.TableName} WHERE status = '正常'");
                var scheduleHistorys = dt_scheduleHistory.DataTableToRowList().SQLToClass<StaffScheduleHistoryClass>() ?? new List<StaffScheduleHistoryClass>();

                // 4. 取得請假紀錄
                var leaveRequests = leaveRequest.GetLeaveRequests();

                // 5. 準備輸出與 DB 批次集合
                var assignedShifts_add = new List<AssignedShiftClass>();
                var histories_add = new List<StaffScheduleHistoryClass>();
                var validationErrors = new List<string>();

                // 6. 自動分配人員
                foreach (var day in scheduleDays)
                {
                    day.RequiredShifts = day.RequiredShifts
                        .Where(x => x.shift_group_guid == shift_group_guid)
                        .ToList();

                    if (day.RequiredShifts.Count == 0) continue;

                    foreach (var req in day.RequiredShifts)
                    {
                        req.shift_group = new ShiftGroupClass();
                        req.date = req.date.StringToDateTime().ToDateString('-');

                     

                        foreach (var wr in req.workShiftRequirements.OrderBy(x => x.TimeRange.Value.start))
                        {
                            // 先處理成員排序

                            if (shiftGroupClass.shift_type == ShiftTypeEnum.swing.GetEnumName())
                            {

                                PrepareShiftGroupMembers(shiftGroupClass, scheduleHistorys);

                                AssignSwingShift(shiftGroupClass, req, wr, scheduleHistorys,
                                    assignedShifts_add, histories_add, validationErrors,
                                    leaveRequests, shift_group_guid);
                            }
                            else if (shiftGroupClass.shift_type == ShiftTypeEnum.midnight.GetEnumName())
                            {
                                PrepareShiftGroupMembers(shiftGroupClass, scheduleHistorys);

                                AssignMidnightShift(shiftGroupClass, req, wr, scheduleHistorys,
                                    assignedShifts_add, histories_add, validationErrors,
                                    leaveRequests, shift_group_guid);
                            }
                            else if (shiftGroupClass.shift_type == ShiftTypeEnum.day.GetEnumName())
                            {
                                PrepareShiftGroupMembers(shiftGroupClass, scheduleHistorys);

                                AssignHolidayShift(shiftGroupClass, req, wr, scheduleHistorys,
                                    assignedShifts_add, histories_add, validationErrors,
                                    leaveRequests, shift_group_guid);
                            }
                            else if (shiftGroupClass.shift_type == ShiftTypeEnum.holiday.GetEnumName())
                            {
                                PrepareShiftGroupMembers(shiftGroupClass, scheduleHistorys);

                                AssignHolidayShift(shiftGroupClass, req, wr, scheduleHistorys,
                                    assignedShifts_add, histories_add, validationErrors,
                                    leaveRequests, shift_group_guid);
                            }
                        }
                    }
                }

                // 7. preview 模式 → 不寫 DB
                if (mode == "preview")
                {
                    returnData.Code = validationErrors.Count > 0 ? -200 : 200;
                    returnData.Result = (validationErrors.Count > 0
                        ? "排班檢核失敗：\n" + string.Join("\n", validationErrors)
                        : $"[預覽模式] 自動排班完成，模擬 {assignedShifts_add.Count} 筆排班，{histories_add.Count} 筆歷程");
                    returnData.Data = scheduleDays;
                    returnData.TimeTaken = $"{timer}";
                    return returnData.JsonSerializationt();
                }

                // 8. commit 模式 → 寫 DB
                if (assignedShifts_add.Count > 0) sql_assignedShift.AddRows(null, assignedShifts_add.ClassToSQL<AssignedShiftClass>());
                if (histories_add.Count > 0) sql_scheduleHistory.AddRows(null, histories_add.ClassToSQL<StaffScheduleHistoryClass>());

                if (validationErrors.Count > 0)
                {
                    returnData.Code = 200;
                    returnData.Result = "部分排班檢核失敗：\n" + string.Join("\n", validationErrors);
                    returnData.AddExtra("validationErrors", validationErrors);
                }
                else
                {
                    returnData.Code = 200;
                    returnData.Result = $"自動排班完成，新增 {assignedShifts_add.Count} 筆排班，新增 {histories_add.Count} 筆歷程";
                }

                returnData.TimeTaken = $"{timer}";
                returnData.Data = scheduleDays;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception: {ex.Message}";
                return returnData.JsonSerializationt();
            }
        }



        [HttpPost("auto_schedule_lastswing")]
        public async Task<string> auto_schedule_lastswing([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "auto_schedule_lastswing";

            try
            {
                var sql_scheduleHistory = MethodClass.GetSQLControl<StaffScheduleHistoryClass>();
                var sql_assignedShift = MethodClass.GetSQLControl<AssignedShiftClass>();

                string GetVal(string key) =>
                    returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string date_start = GetVal("date_start") ?? "";
                string date_end = GetVal("date_end") ?? "";
                string shift_group_guid = GetVal("shift_group_guid") ?? "";
                string mode = (GetVal("mode") ?? "preview").ToLower();

                if (string.IsNullOrWhiteSpace(date_start) || string.IsNullOrWhiteSpace(date_end))
                    return FailJson(returnData, -200, "參數驗證失敗：date_start, date_end 為必填");
                if (string.IsNullOrWhiteSpace(shift_group_guid))
                    return FailJson(returnData, -200, "參數驗證失敗：shift_group_guid 為必填");

                var assignedShifts_add_result = new List<AssignedShiftClass>();
                var histories_add_result = new List<StaffScheduleHistoryClass>();
                for(int i = 0; i < new AssignType().GetLength(); i++)
                {
                    while (true)
                    {
                        // 1) 取得群組（必須是小夜）
                        ShiftGroupClass shiftGroupClass = shiftGroup.GetShiftGroups(shift_group_guid);
                        if (shiftGroupClass == null || shiftGroupClass.shift_type != ShiftTypeEnum.swing.GetEnumName())
                            return FailJson(returnData, -200, "參數驗證失敗：shift_group_guid 不是小夜班群組");

                        // 2) 取得排班日
                        List<ScheduleDayClass> scheduleDays = await GetSchedulesDayAsync(date_start, date_end);

                        // 3) 取得歷史
                        var dt_scheduleHistory = await sql_scheduleHistory.WtrteCommandAndExecuteReaderAsync(
                            $"SELECT * FROM {sql_scheduleHistory.Database}.{sql_scheduleHistory.TableName} WHERE status = '正常'");
                        var scheduleHistorys = dt_scheduleHistory.DataTableToRowList().SQLToClass<StaffScheduleHistoryClass>()
                                               ?? new List<StaffScheduleHistoryClass>();

                        // 4) 請假
                        var leaveRequests = leaveRequest.GetLeaveRequests();

                        // 5) 輸出集合
                        var assignedShifts_add = new List<AssignedShiftClass>();
                        var histories_add = new List<StaffScheduleHistoryClass>();
                        var validationErrors = new List<string>();

                        // 1. 先抽出所有 slot
                        var allSlots = ExtractAllSlots(scheduleDays, shift_group_guid);
                        int totalBase = GetTotalRequiredCount(date_start.StringToDateTime().Year, date_start.StringToDateTime().Month, shift_group_guid);

                        AssignBaseShifts((AssignType)i, allSlots, totalBase, shiftGroupClass, scheduleHistorys, assignedShifts_add, histories_add, validationErrors, leaveRequests, shift_group_guid);

                        if (assignedShifts_add.Count > 0) sql_assignedShift.AddRows(null, assignedShifts_add.ClassToSQL<AssignedShiftClass>());
                        if (histories_add.Count > 0) sql_scheduleHistory.AddRows(null, histories_add.ClassToSQL<StaffScheduleHistoryClass>());

                        assignedShifts_add_result.LockAdd(assignedShifts_add);
                        histories_add_result.LockAdd(histories_add);

                        if (assignedShifts_add.Count == 0) break;

                    }
                }
             







                returnData.Code = 200;
                returnData.Result = $"小夜班自動排班完成，新增 {assignedShifts_add_result.Count} 筆排班，新增 {histories_add_result.Count} 筆歷程";
                //returnData.Data = scheduleDays;
                returnData.TimeTaken = $"{timer}";
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception: {ex.Message}";
                return returnData.JsonSerializationt();
            }
        }

        private List<List<NightSlot>> WenToSat(List<NightSlot> baseSlots ,DateTime dt)
        {
            string[] timeRamges = new string[] { "12:30-21:00", "13:30-22:00", "14:30-23:00", "15:30-23:59", "16:00-23:59", };
            List<NightSlot> _baseSlots = new List<NightSlot>();
            List<List<NightSlot>> list_baseSlots = new List<List<NightSlot>>();

            List<DateTime> dates_slots = new List<DateTime>();
            List<string> times_slots = new List<string>();

            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[4], timeRamges[2], timeRamges[1], timeRamges[3] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);
            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[4], timeRamges[2], timeRamges[0], timeRamges[3] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);
            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[4], timeRamges[1], timeRamges[0], timeRamges[3] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);

            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[3], timeRamges[2], timeRamges[1], timeRamges[3] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);
            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[3], timeRamges[2], timeRamges[0], timeRamges[3] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);
            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[3], timeRamges[1], timeRamges[0], timeRamges[3] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);


      
    
    


            return list_baseSlots;
        }
        private List<List<NightSlot>> SunToWen(List<NightSlot> baseSlots, DateTime dt)
        {
            string[] timeRamges = new string[] { "12:30-21:00", "13:30-22:00", "14:30-23:00", "15:30-23:59", "16:00-23:59", };
            List<NightSlot> _baseSlots = new List<NightSlot>();
            List<List<NightSlot>> list_baseSlots = new List<List<NightSlot>>();

            List<DateTime> dates_slots = new List<DateTime>();
            List<string> times_slots = new List<string>();


            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[4], timeRamges[3], timeRamges[1], timeRamges[0] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);
            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[4], timeRamges[3], timeRamges[2], timeRamges[1] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);
            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[4], timeRamges[3], timeRamges[2], timeRamges[0] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);
            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[4], timeRamges[3], timeRamges[3], timeRamges[0] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);
            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[4], timeRamges[3], timeRamges[3], timeRamges[1] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);
            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[4], timeRamges[3], timeRamges[3], timeRamges[2] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);


            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[4], timeRamges[2], timeRamges[1], timeRamges[3] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);
            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[4], timeRamges[2], timeRamges[0], timeRamges[3] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);
            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[4], timeRamges[1], timeRamges[0], timeRamges[3] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);

            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[3], timeRamges[2], timeRamges[1], timeRamges[3] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);
            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[3], timeRamges[2], timeRamges[0], timeRamges[3] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);
            dates_slots = new List<DateTime>() { dt, dt.AddDays(1), dt.AddDays(2), dt.AddDays(3) };
            times_slots = new List<string>() { timeRamges[3], timeRamges[1], timeRamges[0], timeRamges[3] };
            _baseSlots = baseSlots.FindSlotsByDatesAndTimeRanges(dates_slots, times_slots);
            if (_baseSlots.Count > 0) list_baseSlots.Add(_baseSlots);







            return list_baseSlots;
        }

        private static readonly string[] TimeRanges =
        {
            "16:00-23:59",
            "15:30-23:59",
            "14:30-23:00",
            "13:30-22:00",
            "12:30-21:00",
            "12:00-20:00",
        };
        private List<List<NightSlot>> BuildNormalDays( List<NightSlot> baseSlots, DateTime startDate, int dayCount)
        {
            if (dayCount < 1 || dayCount > TimeRanges.Length)
                throw new ArgumentOutOfRangeException(
                    nameof(dayCount),
                    $"dayCount 必須為 1~{TimeRanges.Length}"
                );

            // 建立連續日期
            List<DateTime> dates = new List<DateTime>();
            for (int i = 0; i < dayCount; i++)
                dates.Add(startDate.AddDays(i));

            // 🔥 P(6, dayCount)
            var permutations = BuildPermutations(TimeRanges, dayCount);

            // ⭐ 排序：從早到晚優先
            var orderedPermutations = permutations
                .OrderBy(p => CountInversions(p.Select(ParseStartMinutes).ToList()))
                .ThenBy(p => p.Sum(ParseStartMinutes))
                .ToList();

            var result = new List<List<NightSlot>>();

            foreach (var times in orderedPermutations)
            {
                var slots = baseSlots.FindSlotsByDatesAndTimeRanges(dates, times);

                // 規則：每一天都必須對應到
                if (slots != null && slots.Count == dayCount)
                {
                    result.Add(slots);
                }
            }

            return result;
        }
        private List<List<string>> BuildPermutations( string[] source, int pickCount)
        {
            var results = new List<List<string>>();
            var used = new bool[source.Length];
            var current = new List<string>(pickCount);

            void Dfs()
            {
                if (current.Count == pickCount)
                {
                    results.Add(new List<string>(current));
                    return;
                }

                for (int i = 0; i < source.Length; i++)
                {
                    if (used[i]) continue;

                    used[i] = true;
                    current.Add(source[i]);

                    Dfs();

                    current.RemoveAt(current.Count - 1);
                    used[i] = false;
                }
            }

            Dfs();
            return results;
        }
        private int ParseStartMinutes(string timeRange)
        {
            // "HH:mm-HH:mm"
            var start = timeRange.Split('-')[0];
            var parts = start.Split(':');
            return int.Parse(parts[0]) * 60 + int.Parse(parts[1]);
        }
        private int CountInversions(List<int> values)
        {
            int count = 0;
            for (int i = 0; i < values.Count; i++)
                for (int j = i + 1; j < values.Count; j++)
                    if (values[i] > values[j]) count++;
            return count;
        }

        private List<List<NightSlot>> Normal_4_Days(List<NightSlot> baseSlots, DateTime dt)
        {
            return BuildNormalDays(baseSlots, dt, 4);
        }
        private List<List<NightSlot>> Normal_3_Days(List<NightSlot> baseSlots, DateTime dt)
        {
            return BuildNormalDays(baseSlots, dt, 3);
        }
        private List<List<NightSlot>> Normal_2_Days(List<NightSlot> baseSlots, DateTime dt)
        {
            return BuildNormalDays(baseSlots, dt, 2);
        }
        private List<List<NightSlot>> Normal_1_Days(List<NightSlot> baseSlots, DateTime dt)
        {
            return BuildNormalDays(baseSlots, dt, 1);
        }

     

        public enum AssignType
        {
            holiday,
            continu_nor,
            nor
        }
        /// <summary>
        /// 基礎班分配：每人直接連續排到達到平均需求數量 (需符合驗證條件)
        /// </summary>
        private void AssignBaseShifts(AssignType assignType , List<NightSlot> allSlots,int totalBase,  ShiftGroupClass shiftGroupClass,  List<StaffScheduleHistoryClass> scheduleHistorys, List<AssignedShiftClass> assignedShifts_add,
                                       List<StaffScheduleHistoryClass> histories_add, List<string> validationErrors, List<LeaveRequestClass> leaveRequests, string shift_group_guid)
        {
            List<AssignedShiftClass> assignedShifts_add_buf = new List<AssignedShiftClass>();
            List<StaffScheduleHistoryClass> histories_add_buf = new List<StaffScheduleHistoryClass>();
            var baseSlots = allSlots;
            if (baseSlots == null || baseSlots.Count == 0) return;

          
            int staffCount = Math.Max(1, shiftGroupClass.Members.Count);
            int avgLimit = (int)Math.Ceiling(totalBase / (double)staffCount);

            // 紀錄每人已排數量
            var baseCount = new Dictionary<string, int>();

            // 照 weight / order_index 排人員
            var orderedMembers = shiftGroupClass.Members
                .OrderBy(m => m.weight.StringToInt32())
                .ThenBy(m => m.order_index.StringToInt32())
                .ToList();

            baseSlots = baseSlots
             .OrderBy(s => s.Date)
             .ThenBy(s => GetShiftPriority(s.Start.ToString("HH:mm")))
             .ToList();


            var dates = baseSlots.GetDateStringsFromSlots();
            foreach (var date in dates)
            {
                DateTime dt = date.StringToDateTime();
                var slotsOnDate = baseSlots.Where(s => s.Date.StringToDateTime().ToDateString('-') == date).ToList();
                PrepareShiftGroupMembers(shiftGroupClass, scheduleHistorys);



                List<DateTime> dates_slots = new List<DateTime>();
                List<string> times_slots = new List<string>();
                List<List<NightSlot>> _baseSlots = new List<List<NightSlot>>();


                while (true)
                {
                    if (dt.DayOfWeek == DayOfWeek.Wednesday)
                    {
                        if(assignType == AssignType.holiday)
                        {
                            _baseSlots.LockAdd(WenToSat(baseSlots, dt));
                        }
                        if(assignType == AssignType.continu_nor)
                        {
                            _baseSlots.LockAdd(Normal_3_Days(baseSlots, dt));
                            _baseSlots.LockAdd(Normal_2_Days(baseSlots, dt));
                        }
                        if(assignType == AssignType.nor)
                        {
                            _baseSlots.LockAdd(Normal_1_Days(baseSlots, dt));
                        }

                        break;
                    }
                    else if (dt.DayOfWeek == DayOfWeek.Sunday)
                    {
                        if (assignType == AssignType.holiday)
                        {
                            _baseSlots.LockAdd(SunToWen(baseSlots, dt));
                            _baseSlots.LockAdd(Normal_3_Days(baseSlots, dt));
                            _baseSlots.LockAdd(Normal_2_Days(baseSlots, dt));
                            _baseSlots.LockAdd(Normal_1_Days(baseSlots, dt));

                        }

                        break;
                    }
                    else if (dt.DayOfWeek == DayOfWeek.Saturday)
                    {
                        if (assignType == AssignType.nor)
                        {
                            _baseSlots.LockAdd(Normal_1_Days(baseSlots, dt));
                        }
                        break;
                    }
                    else if (dt.DayOfWeek == DayOfWeek.Monday)
                    {
                        if (assignType == AssignType.continu_nor)
                        {
                            _baseSlots.LockAdd(Normal_4_Days(baseSlots, dt));
                            _baseSlots.LockAdd(Normal_3_Days(baseSlots, dt));
                            _baseSlots.LockAdd(Normal_2_Days(baseSlots, dt));
                        }
                        if (assignType == AssignType.nor)
                        {
                            _baseSlots.LockAdd(Normal_1_Days(baseSlots, dt));
                        }
                        break;
                    }
                    else if (dt.DayOfWeek == DayOfWeek.Tuesday)
                    {
                        if (assignType == AssignType.continu_nor)
                        {
                            _baseSlots.LockAdd(Normal_4_Days(baseSlots, dt));
                            _baseSlots.LockAdd(Normal_3_Days(baseSlots, dt));
                            _baseSlots.LockAdd(Normal_2_Days(baseSlots, dt));
                        }

                        if (assignType == AssignType.nor)
                        {
                            _baseSlots.LockAdd(Normal_1_Days(baseSlots, dt));
                        }

                        break;
                    }
                    else if (dt.DayOfWeek == DayOfWeek.Thursday)
                    {
                        if (assignType == AssignType.holiday)
                        {
                            _baseSlots.LockAdd(Normal_3_Days(baseSlots, dt));
                            _baseSlots.LockAdd(Normal_2_Days(baseSlots, dt));
                        }               
                        if (assignType == AssignType.nor)
                        {
                            _baseSlots.LockAdd(Normal_1_Days(baseSlots, dt));
                        }
                        break;
                    }
                    else if (dt.DayOfWeek == DayOfWeek.Friday)
                    {
                        if (assignType == AssignType.holiday)
                        {
                            _baseSlots.LockAdd(Normal_2_Days(baseSlots, dt));
                        }
                        if (assignType == AssignType.nor)
                        {
                            _baseSlots.LockAdd(Normal_1_Days(baseSlots, dt));
                        }
                        break;
                    }
                    else
                    {
                        break;
                    }
                }

                foreach (var member in shiftGroupClass.Members)
                {
                    
                    foreach (List<NightSlot> slotList in _baseSlots)
                    {

                        StaffClass staff = member.staff_info;

                        List<StaffScheduleHistoryClass> historyClasses = staff.scheduleHistories.FindByMonthAndShiftType(dates[0].StringToDateTime().Year, dates[0].StringToDateTime().Month, ShiftTypeEnum.swing.GetEnumName());

                        if (historyClasses.Count + slotList.Count > avgLimit)
                        {
                            continue;
                        }
                        if (staff == null) continue;

                        if (slotList == null || slotList.Count == 0) continue;
                        bool flag_canAssign = true;
                        foreach (var slot in slotList)
                        {
                            var wr = slot.Wr;
                            var req = slot.Req;
                            if (slot.AssignedCount >= slot.RequiredCount) continue;


                            var newHistory = new StaffScheduleHistoryClass
                            {
                                GUID = Guid.NewGuid().ToString(),
                                staff_guid = staff.GUID,
                                date = req.date,
                                time = wr.time,
                                department = wr.department,
                                req_shift_guid = req.GUID,
                                shift_group_guid = shift_group_guid,
                                shift_type = shiftGroupClass.shift_type,
                                created_at = DateTime.Now.ToDateTimeString_6(),
                                updated_at = DateTime.Now.ToDateTimeString_6(),
                                status = "正常",
                                source = "自動排班"
                            };

                            var validation = ScheduleValidator.ValidateSchedule(staff, newHistory);
                            if (!validation.isValid)
                            {
                                flag_canAssign = false;
                                break;
                            }
                            if (leaveRequests.HasLeaveOnDate(staff.GUID, req.date))
                            {
                                flag_canAssign = false;
                                break;
                            }

                            // 成功指派
                            var assignedShift = new AssignedShiftClass
                            {
                                GUID = Guid.NewGuid().ToString(),
                                date = req.date,
                                staff_guid = staff.GUID,
                                req_shift_guid = req.GUID,
                                shift_requirement = JsonSerializer.Serialize(slot.Wr),
                                created_at = DateTime.Now.ToDateTimeString_6(),
                                updated_at = DateTime.Now.ToDateTimeString_6(),
                                status = "正常",
                                staff = staff
                            };
                         
                            assignedShifts_add_buf.Add(assignedShift);
                            newHistory.assigned_shift_guid = assignedShift.GUID;
                            histories_add_buf.Add(newHistory);
                            scheduleHistorys.Add(newHistory);
                            staff.scheduleHistories.Add(newHistory);



                        }
                        if (flag_canAssign)
                        {
                            assignedShifts_add.LockAdd(assignedShifts_add_buf);
                            histories_add.LockAdd(histories_add_buf);
                            assignedShifts_add_buf.Clear();
                            histories_add_buf.Clear();
                            foreach (var slot in slotList)
                            {
                                slot.AssignedCount++;
                                slot.Wr.assigned_count = slot.AssignedCount.ToString();
                            }
                            break;
                        }
                        else
                        {
                            assignedShifts_add_buf.Clear();
                            histories_add_buf.Clear();

                        }
                    }


                    continue;



                }
            }
            return;

        }
       

        private (List<DateTime> dateTimes , List<string> timeRanges) GetAllDateTimesAndTimeRanges(DateTime date_start)
        {
            var dates = new List<DateTime>();
            var times = new List<string>();
            

            return (dates, times);
        }


     
        /// <summary>
        /// 抽出所有班次 slot（人數攤平 + 跨日處理）
        /// 不論是不是最後班都會取出
        /// </summary>
        /// <summary>
        /// 抽出所有班次 slot（依據需求人數 - 已指派人數 攤平）
        /// 只會抽出 shift_group_guid 相符的需求
        /// </summary>
        private List<NightSlot> ExtractAllSlots(List<ScheduleDayClass> scheduleDays, string shift_group_guid)
        {
            var slots = new List<NightSlot>();
            if (scheduleDays == null) return slots;

            foreach (var day in scheduleDays)
            {
                foreach (var req in day.RequiredShifts.Where(r => r.shift_group_guid == shift_group_guid))
                {
                    if (req.workShiftRequirements == null) continue;
                    var d = req.date.StringToDateTime();

                    foreach (var wr in req.workShiftRequirements)
                    {
                        // --- 安全取 TimeRange ---
                        var tr = wr.TimeRange ?? default;
                        var start = d.Add(tr.start);
                        var end = d.Add(tr.end);

                        // --- 處理跨日（結束時間 <= 開始時間 時）---
                        if (end <= start)
                            end = end.AddDays(1);

                        // --- 計算剩餘可排人數 ---
                        int requiredCount = wr.required_count.StringToInt32();
                        int assignedCount = wr.assigned_count.StringToInt32();
                        int remaining = Math.Max(0, requiredCount - assignedCount);

                        // 沒有剩餘需求就跳過
                        if (remaining == 0) continue;

                        // --- 攤平剩餘需求 ---
                        for (int i = 0; i < remaining; i++)
                        {
                            slots.Add(new NightSlot
                            {
                                Date = d,
                                Time = wr.time,
                                RequiredCount = 1,
                                AssignedCount = 0,
                                Req = req,
                                Wr = wr,
                                Start = start,
                                End = end,
                                IsBase = IsLastShiftOfDay(wr.time)
                                          || tr.end == TimeSpan.Zero
                                          || tr.end >= new TimeSpan(23, 59, 0)
                            });
                        }
                    }
                }
            }

            return slots;
        }
        /// <summary>
        /// 計算指定班別群組 (shift_group_guid) 在整個日期範圍內的需求人數總和
        /// </summary>
        /// <param name="scheduleDays">排班日清單</param>
        /// <param name="shift_group_guid">班別群組 GUID</param>
        /// <returns>需求人數總和 (不扣已指派)</returns>
        public static int GetTotalRequiredCount(int year,int month ,string shift_group_guid)
        {
            DateTime firstDay = new DateTime(year, month, 1);
            DateTime lastDay = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            List<ScheduleDayClass> scheduleDays = scheduleDay.GetScheduleDay(firstDay.ToDateString('-'), lastDay.ToDateString('-'));
            if (scheduleDays == null || scheduleDays.Count == 0) return 0;
            if (string.IsNullOrWhiteSpace(shift_group_guid)) return 0;

            return scheduleDays
                .SelectMany(day => day.RequiredShifts
                    .Where(req => req.shift_group_guid == shift_group_guid)
                    .SelectMany(req => req.workShiftRequirements
                        .Select(wr => wr.required_count.StringToInt32())))
                .Sum();
        }


        // ─────────────────────────────────────────────────────────────
        // 3) 判斷是否為「當日最後班」的字串判定
        //    支援 "15:30-00:00","16:00-00:00","15:30-24:00","16:00-24:00","15:30-23:59","16:00-23:59"
        //    也能容忍沒有冒號的 "1530-0000" 之類格式
        // ─────────────────────────────────────────────────────────────
        private bool IsLastShiftOfDay(string timeRange)
        {
            if (string.IsNullOrWhiteSpace(timeRange)) return false;

            var parts = timeRange.Replace(" ", "")
                                 .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length != 2) return false;

            var endRaw = parts[1];
            var end = endRaw.Replace(":", "");

            // 正規化為 4 位數
            if (end.Length == 3) end = "0" + end;
            if (end.Length != 4) return false;

            // 以文字判定：0000 / 2400 / 2359 → 視為最後班
            return end == "0000" || end == "2400" || end == "2359";
        }

        /// <summary>
        /// 取得班別優先權 (數字越小越優先)
        /// 目前順序：15:30 → 16:00 → 14:30 → 13:30 → 12:30 → 其他
        /// </summary>
        private int GetShiftPriority(string hhmm)
        {
            switch (hhmm)
            {
                case "15:30": return 0;
                case "16:00": return 1;
                case "14:30": return 2;
                case "13:30": return 3;
                case "12:30": return 4;
                default: return 99; // 其他班次最後
            }
        }

        static string DayOfWeekToZh(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Sunday => "日",
                DayOfWeek.Monday => "一",
                DayOfWeek.Tuesday => "二",
                DayOfWeek.Wednesday => "三",
                DayOfWeek.Thursday => "四",
                DayOfWeek.Friday => "五",
                DayOfWeek.Saturday => "六",
                _ => ""
            };
        }

        // 快速回傳失敗json
        private string FailJson(returnData rd, int code, string msg)
        {
            rd.Code = code;
            rd.Result = msg;
            return rd.JsonSerializationt();
        }

        /// <summary>
        /// 根據 weight 與歷史紀錄，重新計算並排序群組成員
        /// </summary>
        static private void PrepareShiftGroupMembers(ShiftGroupClass shiftGroupClass, List<StaffScheduleHistoryClass> scheduleHistorys)
        {
            if (shiftGroupClass == null || shiftGroupClass.Members == null || shiftGroupClass.Members.Count == 0) return;

            List<StaffScheduleHistoryClass> filteredHistorys = null;

            if (shiftGroupClass.shift_type == "holiday")
            {
                filteredHistorys = scheduleHistorys
                                   .Where(x => x.shift_type == shiftGroupClass.shift_type)
                                   .Where(x => x.shift_group_guid == shiftGroupClass.GUID)
                                   .ToList();
            }
            else
            {
                filteredHistorys = scheduleHistorys
                                   .Where(x => x.shift_type == shiftGroupClass.shift_type)
                                   .ToList();
            }
       
            var keypairs_scheduleHistorys = filteredHistorys.CoverToDictionaryBy_staff_guid();

            foreach (var member in shiftGroupClass.Members)
            {
          
                var scheduleHistorys_buf = keypairs_scheduleHistorys.SortDictionaryBy_staff_guid(member.staff_guid);
                if (scheduleHistorys_buf.Count > 0)
                {
                    member.weight = (scheduleHistorys_buf.Count).ToString();
                }
                else
                {
                    member.weight = "0";
                }

                if (shiftGroupClass.shift_type == "swing")
                {
                    member.weight = (member.weight.StringToInt32() + member.staff_info.SwingShiftWeightBase.StringToInt32()).ToString();
                }
                else if (shiftGroupClass.shift_type == "midnight")
                {
                    member.weight = (member.weight.StringToInt32() + member.staff_info.MidnightShiftWeightBase.StringToInt32()).ToString();
                }
                else if (shiftGroupClass.shift_type == "holiday")
                {
                    member.weight = (member.weight.StringToInt32() + member.staff_info.HolidayShiftWeightBase.StringToInt32()).ToString();

                }
            }

            shiftGroupClass.Members = shiftGroupClass.Members.SortByWeightAndOrderIndex();
        }
     
        static private void AssignSwingShift(ShiftGroupClass shiftGroupClass, RequiredShiftClass req, WorkShiftRequirementClass wr,
            List<StaffScheduleHistoryClass> scheduleHistorys, List<AssignedShiftClass> assignedShifts_add,
            List<StaffScheduleHistoryClass> histories_add, List<string> validationErrors,
            List<LeaveRequestClass> leaveRequests, string shift_group_guid)
        {
            AssignShiftCore(shiftGroupClass, req, wr, scheduleHistorys, assignedShifts_add, histories_add, validationErrors, leaveRequests, shift_group_guid);
        }

        static private void AssignMidnightShift(ShiftGroupClass shiftGroupClass, RequiredShiftClass req, WorkShiftRequirementClass wr,
            List<StaffScheduleHistoryClass> scheduleHistorys, List<AssignedShiftClass> assignedShifts_add,
            List<StaffScheduleHistoryClass> histories_add, List<string> validationErrors,
            List<LeaveRequestClass> leaveRequests, string shift_group_guid)
        {
            AssignShiftCore(shiftGroupClass, req, wr, scheduleHistorys, assignedShifts_add, histories_add, validationErrors, leaveRequests, shift_group_guid);
        }

        static private void AssignHolidayShift(ShiftGroupClass shiftGroupClass, RequiredShiftClass req, WorkShiftRequirementClass wr,
            List<StaffScheduleHistoryClass> scheduleHistorys, List<AssignedShiftClass> assignedShifts_add,
            List<StaffScheduleHistoryClass> histories_add, List<string> validationErrors,
            List<LeaveRequestClass> leaveRequests, string shift_group_guid)
        {
            AssignShiftCore(shiftGroupClass, req, wr, scheduleHistorys, assignedShifts_add, histories_add, validationErrors, leaveRequests, shift_group_guid);
        }



        static private void AssignShiftCore(ShiftGroupClass shiftGroupClass, RequiredShiftClass req, WorkShiftRequirementClass wr,
           List<StaffScheduleHistoryClass> scheduleHistorys, List<AssignedShiftClass> assignedShifts_add,
           List<StaffScheduleHistoryClass> histories_add, List<string> validationErrors,
           List<LeaveRequestClass> leaveRequests, string shift_group_guid)
        {
            int requiredCount = wr.required_count.StringToInt32();
            int assignedCount = wr.assigned_count.StringToInt32();

            for (int i = 0; i < shiftGroupClass.Members.Count && assignedCount < requiredCount; i++)
            {
                var candidate = shiftGroupClass.Members[i];
                // ✅ 硬性跳過禁排
                if (candidate.weight.StringToInt32() == int.MaxValue)
                {
                    continue;
                }
                StaffClass staff = candidate.staff_info;
                if (staff == null) continue;

                var newHistory = new StaffScheduleHistoryClass
                {
                    GUID = Guid.NewGuid().ToString(),
                    staff_guid = staff.GUID,
                    date = req.date,
                    time = wr.time,
                    department = wr.department,
                    req_shift_guid = req.GUID,
                    shift_group_guid = shift_group_guid,
                    shift_type = shiftGroupClass.shift_type,
                    created_at = DateTime.Now.ToDateTimeString_6(),
                    updated_at = DateTime.Now.ToDateTimeString_6(),
                    status = "正常",
                    source = "自動排班"
                };

                var validation = ScheduleValidator.ValidateSchedule(staff, newHistory);
                if (!validation.isValid)
                {
                    validationErrors.Add($"[{staff.staff_name}] {req.date} 「{wr.time}」 → {validation.message}");
                    continue;
                }

                bool isHaveLeave = leaveRequests.HasLeaveOnDate(staff.GUID, req.date);
                if (isHaveLeave)
                {
                    validationErrors.Add($"[{staff.staff_name}] {req.date} 「{wr.time}」 → 該日有請假紀錄");
                    continue;
                }

                var assignedShift = new AssignedShiftClass
                {
                    GUID = Guid.NewGuid().ToString(),
                    date = req.date,
                    staff_guid = staff.GUID,
                    req_shift_guid = req.GUID,
                    shift_requirement = JsonSerializer.Serialize(wr),
                    created_at = DateTime.Now.ToDateTimeString_6(),
                    updated_at = DateTime.Now.ToDateTimeString_6(),
                    status = "正常",
                    staff = staff
                };      
                assignedShifts_add.Add(assignedShift);
                


                newHistory.assigned_shift_guid = assignedShift.GUID;
                staff.scheduleHistories.Add(newHistory);
                histories_add.Add(newHistory);
                scheduleHistorys.Add(newHistory);

                assignedCount++;
                wr.assigned_count = assignedCount.ToString();
            }
        }




        public static ScheduleDayClass GetScheduleDay(string date)
        {
            return GetScheduleDayAsync(new string[] { date }).GetAwaiter().GetResult().scheduleDays[0] ?? null;
        }
        public async static Task<ScheduleDayClass> GetSchedulesDayAsync(string date)
        {
            var result = await GetScheduleDayAsync(new string[] { date });
            return result.scheduleDays[0] ?? null;
        }

        public static List<ScheduleDayClass> GetScheduleDay(params string[] dates)
        {
            return GetScheduleDayAsync(dates).GetAwaiter().GetResult().scheduleDays;
        }
        public async static Task<List<ScheduleDayClass>> GetSchedulesDayAsync(params string[] dates)
        {
            var result = await GetScheduleDayAsync(dates);
            return result.scheduleDays;
        }

        public static List<ScheduleDayClass> GetScheduleDay(string date_start, string date_end)
        {
            List<string> ValueAry = new List<string>();
            ValueAry.Add($"date_start={date_start}");
            ValueAry.Add($"date_end={date_end}");
            return GetScheduleDayAsync(ValueAry).GetAwaiter().GetResult().scheduleDays;
        }
        public async static Task<List<ScheduleDayClass>> GetSchedulesDayAsync(string date_start, string date_end)
        {
            List<string> ValueAry = new List<string>();
            ValueAry.Add($"date_start={date_start}");
            ValueAry.Add($"date_end={date_end}");
            var result = await GetScheduleDayAsync(ValueAry);
            return result.scheduleDays;
        }

        private static (List<ScheduleDayClass> scheduleDays, int totalCount, int totalPages, int pageSize, int currentPage) GetScheduleDay(List<string> ValueAry)
        {
            return GetScheduleDayAsync(ValueAry).GetAwaiter().GetResult();
        }


        private static async Task<(List<ScheduleDayClass> scheduleDays, int totalCount, int totalPages, int pageSize, int currentPage)> GetScheduleDayAsync(List<string> ValueAry)
        {
            var sql_ScheduleDay = MethodClass.GetSQLControl<ScheduleDayClass>();

            string GetVal(string key) =>
                ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                ?.Split('=')[1];

            string date_start = GetVal("date_start") ?? "";
            string date_end = GetVal("date_end") ?? "";
            int page = (GetVal("page") ?? "1").StringToInt32();
            int pageSize = (GetVal("pageSize") ?? "50").StringToInt32();
            string sortOrder = (GetVal("sortOrder") ?? "desc").ToUpper();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            string querySql = $@"
        SELECT * FROM {sql_ScheduleDay.Database}.{sql_ScheduleDay.TableName} 
        WHERE 1=1
        {(date_start != "" ? $" AND `date` >= '{date_start.StringToDateTime().ToDateString()}'" : "")}
        {(date_end != "" ? $" AND `date` <= '{date_end.StringToDateTime().ToDateString()}'" : "")}
        ORDER BY date {sortOrder}";

            var dt_ScheduleDay = await sql_ScheduleDay.WtrteCommandAndExecuteReaderAsync(querySql);
            var all_ScheduleDays = dt_ScheduleDay.DataTableToRowList().SQLToClass<ScheduleDayClass>() ?? new List<ScheduleDayClass>();

            // === 🔥 自動補日期 ===
            if (date_start != "" && date_end != "")
            {
                var start = date_start.StringToDateTime();
                var end = date_end.StringToDateTime();

                for (var d = start; d <= end; d = d.AddDays(1))
                {
                    var dateStr = d.ToDateString('-');
                    if (!all_ScheduleDays.Any(x => x.date.StringToDateTime().ToDateString('-') == dateStr))
                    {
                        var newDay = new ScheduleDayClass();
                        newDay.GUID = Guid.NewGuid().ToString();
                        newDay.date = dateStr;
                        newDay.created_at = DateTime.Now.ToDateTimeString();
                        newDay.updated_at = DateTime.Now.ToDateTimeString();

                        string insertSql = $@"
                    INSERT INTO {sql_ScheduleDay.Database}.{sql_ScheduleDay.TableName}
                    (`GUID`, `date`, `created_at`, `updated_at`)
                    VALUES ('{newDay.GUID}', '{newDay.date}', '{newDay.created_at}', '{newDay.updated_at}')";

                        await sql_ScheduleDay.WriteCommandAsync(insertSql);

                        all_ScheduleDays.Add(newDay);
                    }
                }
            }

            await BindScheduleDayRelations(all_ScheduleDays);

            int totalCount = all_ScheduleDays.Count;
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            all_ScheduleDays = all_ScheduleDays
                .OrderBy(x => x.date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (all_ScheduleDays, totalCount, totalPages, pageSize, page);
        }
        private static async Task<(List<ScheduleDayClass> scheduleDays, int totalCount, int totalPages, int pageSize, int currentPage)> GetScheduleDayAsync(string[] dates, int page = 1, int pageSize = 50, string sortOrder = "DESC")
        {
            var sql_ScheduleDay = MethodClass.GetSQLControl<ScheduleDayClass>();

            if (dates == null || dates.Length == 0)
                return (new List<ScheduleDayClass>(), 0, 0, pageSize, page);

            string dateList = string.Join(",", dates.Select(d => $"'{d}'"));
            string querySql = $@"
        SELECT * FROM {sql_ScheduleDay.Database}.{sql_ScheduleDay.TableName}
        WHERE `date` IN ({dateList})
        ORDER BY date {sortOrder}";

            var dt_ScheduleDay = await sql_ScheduleDay.WtrteCommandAndExecuteReaderAsync(querySql);
            var all_ScheduleDays = dt_ScheduleDay.DataTableToRowList().SQLToClass<ScheduleDayClass>() ?? new List<ScheduleDayClass>();

            // === 🔥 若缺少指定日期，自動補上 ===
            foreach (var d in dates)
            {
                if (!all_ScheduleDays.Any(x => x.date.StringToDateTime().ToDateString('-') == d.ToDateString('-')))
                {
                    var newDay = new ScheduleDayClass
                    {
                        GUID = Guid.NewGuid().ToString().ToUpper(),
                        date = d,
                        created_at = DateTime.Now.ToDateTimeString(),
                        updated_at = DateTime.Now.ToDateTimeString(),
                        RequiredShifts = new List<RequiredShiftClass>(),
                        AssignedShifts = new List<AssignedShiftClass>(),
                        ScheduleLogs = new List<ScheduleLogClass>(),
                        SpecialDays = new List<SpecialDayClass>(),
                        LeaveRequests = new List<LeaveRequestClass>()
                    };

                    string insertSql = $@"
                INSERT INTO {sql_ScheduleDay.Database}.{sql_ScheduleDay.TableName}
                (`GUID`, `date`, `created_at`, `updated_at`)
                VALUES ('{newDay.GUID}', '{newDay.date}', '{newDay.created_at}', '{newDay.updated_at}')
            ";

                    await sql_ScheduleDay.WriteCommandAsync(insertSql);

                    all_ScheduleDays.Add(newDay);
                }
            }

            // 綁定關聯
            await BindScheduleDayRelations(all_ScheduleDays);

            // 分頁
            int totalCount = all_ScheduleDays.Count;
            int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            all_ScheduleDays = all_ScheduleDays
                .OrderBy(x => x.date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (all_ScheduleDays, totalCount, totalPages, pageSize, page);
        }


        /// <summary>
        /// 綁定 ScheduleDayClass 的關聯資料
        /// </summary>
        private static async Task BindScheduleDayRelations(List<ScheduleDayClass> scheduleDays)
        {
            if (scheduleDays == null || scheduleDays.Count == 0) return;

            var sql_RequiredShift = MethodClass.GetSQLControl<RequiredShiftClass>();
            var sql_AssignedShift = MethodClass.GetSQLControl<AssignedShiftClass>();
            var sql_ScheduleLog = MethodClass.GetSQLControl<ScheduleLogClass>();
            var sql_SpecialDay = MethodClass.GetSQLControl<SpecialDayClass>();
            var sql_scheduleHistory = MethodClass.GetSQLControl<StaffScheduleHistoryClass>();


            // === 撈取所有關聯資料 ===
            var dt_RequiredShift = await sql_RequiredShift.WtrteCommandAndExecuteReaderAsync($"SELECT * FROM {sql_RequiredShift.Database}.{sql_RequiredShift.TableName}");
            var requiredShifts = dt_RequiredShift.DataTableToRowList().SQLToClass<RequiredShiftClass>() ?? new List<RequiredShiftClass>();
            var keypairs_RequiredShift = requiredShifts.CoverToDictionaryByDate();

            var dt_AssignedShift = await sql_AssignedShift.WtrteCommandAndExecuteReaderAsync($"SELECT * FROM {sql_AssignedShift.Database}.{sql_AssignedShift.TableName}");
            var assignedShifts = dt_AssignedShift.DataTableToRowList().SQLToClass<AssignedShiftClass>() ?? new List<AssignedShiftClass>();
            var keypairs_AssignedShift = assignedShifts.CoverToDictionaryByDate();

            var dt_ScheduleLog = await sql_ScheduleLog.WtrteCommandAndExecuteReaderAsync($"SELECT * FROM {sql_ScheduleLog.Database}.{sql_ScheduleLog.TableName}");
            var scheduleLogs = dt_ScheduleLog.DataTableToRowList().SQLToClass<ScheduleLogClass>() ?? new List<ScheduleLogClass>();
            var keypairs_ScheduleLog = scheduleLogs.CoverToDictionaryByDate();

            var dt_SpecialDay = await sql_SpecialDay.WtrteCommandAndExecuteReaderAsync($"SELECT * FROM {sql_SpecialDay.Database}.{sql_SpecialDay.TableName}");
            var specialDays = dt_SpecialDay.DataTableToRowList().SQLToClass<SpecialDayClass>() ?? new List<SpecialDayClass>();
            var keypairs_SpecialDay = specialDays.CoverToDictionaryByDate();

            var leaveRequests_result = await leaveRequest.GetLeaveRequestsAsync(new List<string>());
            for(int i = 0; i < leaveRequests_result.leaveRequests.Count; i++)
            {
                leaveRequests_result.leaveRequests[i].staff_info.scheduleHistories = new List<StaffScheduleHistoryClass>();
            }
            var shiftGroup_result = await shiftGroup.GetShiftGroupsAsync(new List<string>(), true);
            var keypairs_ShiftGroup = shiftGroup_result.shiftGroups.CoverToDictionaryByGUID();

            var dt_scheduleHistory = await sql_scheduleHistory.WtrteCommandAndExecuteReaderAsync($"SELECT * FROM {sql_scheduleHistory.Database}.{sql_scheduleHistory.TableName} WHERE status = '正常'");
            var scheduleHistorys = dt_scheduleHistory.DataTableToRowList().SQLToClass<StaffScheduleHistoryClass>() ?? new List<StaffScheduleHistoryClass>();
            var keypairs_scheduleHistory = scheduleHistorys.CoverToDictionaryBy_req_shift_guid();

            // === 綁定到每個 ScheduleDay ===
            foreach (var scheduleDay in scheduleDays)
            {
                scheduleDay.RequiredShifts = keypairs_RequiredShift.SortDictionaryByDate(scheduleDay.date);
                scheduleDay.AssignedShifts = keypairs_AssignedShift.SortDictionaryByDate(scheduleDay.date);
                scheduleDay.ScheduleLogs = keypairs_ScheduleLog.SortDictionaryByDate(scheduleDay.date);
                scheduleDay.SpecialDays = keypairs_SpecialDay.SortDictionaryByDate(scheduleDay.date);

                scheduleDay.LeaveRequests = (from temp in leaveRequests_result.leaveRequests
                                             where scheduleDay.date.StringToDateTime()
                                                   .IsInDate(temp.start_date.StringToDateTime(), temp.end_date.StringToDateTime())
                                             select temp).ToList();
                foreach (var requiredShift in scheduleDay.RequiredShifts)
                {
                    var sg_buf = keypairs_ShiftGroup.SortDictionaryByGUID(requiredShift.shift_group_guid);
                    requiredShift.workShiftRequirements = requiredShift.workShiftRequirements.FilterByDate(scheduleDay.date);
                    if (requiredShift.workShiftRequirements != null) requiredShift.workShiftRequirements = requiredShift.workShiftRequirements.SortByDayAndTime();
                    List<WorkShiftRequirementClass> wsr_buf = new List<WorkShiftRequirementClass>();
                    for (int i = 0; i < requiredShift.workShiftRequirements.Count; i++)
                    {
                        WorkShiftRequirementClass wsr = requiredShift.workShiftRequirements[i];
                        List<StaffScheduleHistoryClass> staffScheduleHistories = keypairs_scheduleHistory.SortDictionaryBy_req_shift_guid(requiredShift.GUID);
                        staffScheduleHistories = (from temp in staffScheduleHistories
                                                  where temp.time == wsr.time
                                                  where temp.department == wsr.department
                                                  select temp).ToList();
                        wsr.assigned_count = staffScheduleHistories.Count.ToString();
                        wsr_buf.Add(wsr);
                    }
                    requiredShift.shift_requirements = wsr_buf.JsonSerializationt();
                    if (sg_buf.Count > 0)
                    {
                        requiredShift.shift_group = sg_buf[0];
                        sg_buf[0].Members = new List<ShiftGroupMemberClass>();
                    }
                    else
                    {

                    }
                    if (requiredShift.shift_group != null) requiredShift.shift_group.workShiftRanges = requiredShift.shift_group.workShiftRanges.SortByDayAndTime();
                    requiredShift.shift_group.workShiftRanges = requiredShift.shift_group.workShiftRanges.FilterByDate(scheduleDay.date);
                }

                scheduleDay.RequiredShifts = scheduleDay.RequiredShifts.SortRequiredShifts();

            }
        }

        private static List<RequiredShiftClass> GetAllRequiredShifts(bool shift_group = false)
        {
            return GetAllRequiredShiftsAsync().GetAwaiter().GetResult();
        }
        private static async Task<List<RequiredShiftClass>> GetAllRequiredShiftsAsync(bool shift_group = false)
        {
            string Esc(string s) => (s ?? "").Replace("'", "''");
            var sql_RequiredShift = MethodClass.GetSQLControl<RequiredShiftClass>();
            var sql_ShiftGroupClass = MethodClass.GetSQLControl<ShiftGroupClass>();

            // === 全域搜尋模式 ===
            string querySql = "";
            querySql = $@"SELECT * FROM {sql_RequiredShift.Database}.{sql_RequiredShift.TableName} ";
            var dt_RequiredShift = await sql_RequiredShift.WtrteCommandAndExecuteReaderAsync(querySql);
            var requiredShifts = dt_RequiredShift.DataTableToRowList().SQLToClass<RequiredShiftClass>() ?? new List<RequiredShiftClass>();
            var keypairs_RequiredShift = requiredShifts.CoverToDictionaryByDate();
            var requiredShifts_buf = new List<RequiredShiftClass>();


            var ShiftGroupClass_result = await shiftGroup.GetShiftGroupsAsync(new List<string>(), true);
            var keypairs_ShiftGroupClass = ShiftGroupClass_result.shiftGroups.CoverToDictionaryByGUID();
            var ShiftGroupClass_buf = new List<ShiftGroupClass>();


            if (shift_group)
            {
                // 關聯
                foreach (var requiredShift in requiredShifts)
                {
                    ShiftGroupClass_buf = keypairs_ShiftGroupClass.SortDictionaryByGUID(requiredShift.shift_group_guid);
                    if (ShiftGroupClass_buf.Count > 0) requiredShift.shift_group = ShiftGroupClass_buf[0];
                }
            }


            return (requiredShifts);
        }

        /// <summary>
        /// 驗證並新增/更新排班與歷史紀錄
        /// </summary>
        /// <param name="assignedShift">當前要處理的排班</param>
        /// <param name="scheduleDay">對應的日期班表</param>
        /// <param name="staff">對應的人員</param>
        /// <param name="shift_group_guid">班群 GUID</param>
        /// <param name="assignedShifts_add">待新增的排班清單</param>
        /// <param name="assignedShifts_update">待更新的排班清單</param>
        /// <param name="histories_add">待新增的歷程清單</param>
        /// <param name="histories_update">待更新的歷程清單</param>
        /// <param name="output">回傳結果清單</param>
        /// <param name="validationErrors">收集失敗訊息</param>
        public static void ValidateAndAddOrUpdateAssignedShift(
            AssignedShiftClass assignedShift,
            ScheduleDayClass scheduleDay,
            StaffClass staff,
            ShiftGroupClass shift_group,
            List<AssignedShiftClass> assignedShifts_add,
            List<AssignedShiftClass> assignedShifts_update,
            List<StaffScheduleHistoryClass> histories_add,
            List<StaffScheduleHistoryClass> histories_update,
            List<AssignedShiftClass> output,
            List<string> validationErrors)
        {
            var sql_StaffHistory = MethodClass.GetSQLControl<StaffScheduleHistoryClass>();

            // === 新的歷程物件 ===
            var newHistory = new StaffScheduleHistoryClass
            {
                staff_guid = staff.GUID,
                date = assignedShift.date,
                time = assignedShift.workShiftRequirement?.time,
                department = assignedShift.workShiftRequirement?.department,
                created_at = DateTime.Now.ToDateTimeString_6(),
                updated_at = DateTime.Now.ToDateTimeString_6(),
                req_shift_guid = assignedShift.req_shift_guid,
                shift_type = shift_group.shift_type,
                shift_group_guid = shift_group.GUID,
                source = assignedShift.source == null ? "手動調整" : assignedShift.source,
                status = "正常"
            };

            // === 檢核是否符合規則 ===
            var validation = ScheduleValidator.ValidateSchedule(staff, newHistory);
            if (!validation.isValid)
            {
                validationErrors.Add($"[{staff.staff_name}] {assignedShift.date} {assignedShift.workShiftRequirement?.time} → {validation.message}");
                return; // ❌ 不做後續新增/更新
            }

            // ✅ 通過檢核 → 執行排班與歷史紀錄處理
            AddOrUpdateAssignedShift(
                assignedShift, scheduleDay, staff, shift_group,
                sql_StaffHistory,
                assignedShifts_add, assignedShifts_update,
                histories_add, histories_update,
                output
            );
        }
        /// <summary>
        /// 新增或更新單筆排班與歷史紀錄 (不含檢核)
        /// </summary>
        private static void AddOrUpdateAssignedShift(
            AssignedShiftClass assignedShift,
            ScheduleDayClass scheduleDay,
            StaffClass staff,
            ShiftGroupClass shift_group,
            SQLControl sql_StaffHistory,
            List<AssignedShiftClass> assignedShifts_add,
            List<AssignedShiftClass> assignedShifts_update,
            List<StaffScheduleHistoryClass> histories_add,
            List<StaffScheduleHistoryClass> histories_update,
            List<AssignedShiftClass> output)
        {
            var newHistory = new StaffScheduleHistoryClass
            {
                staff_guid = staff.GUID,
                date = assignedShift.date,
                time = assignedShift.workShiftRequirement?.time,
                department = assignedShift.workShiftRequirement?.department,
                created_at = DateTime.Now.ToDateTimeString_6(),
                updated_at = DateTime.Now.ToDateTimeString_6(),
                req_shift_guid = assignedShift.req_shift_guid,
                shift_type = shift_group.shift_type,
                shift_group_guid = shift_group.GUID,
                source = assignedShift.source == null ? "手動調整" : assignedShift.source,
                status = "正常"
            };

            // === 找舊的 AssignedShift ===
            AssignedShiftClass assignedShiftClass = scheduleDay.AssignedShifts.SerchByStaffGUID(assignedShift.req_shift_guid, assignedShift.staff_guid, assignedShift.shift_requirement);

            if (assignedShiftClass == null)
            {
                // 新增 AssignedShift
                assignedShift.GUID = Guid.NewGuid().ToString();
                assignedShift.updated_at = DateTime.Now.ToDateTimeString_6();
                assignedShift.created_at = DateTime.Now.ToDateTimeString_6();
                assignedShift.status = "正常";
                assignedShift.workShiftRequirement.shift_type = shift_group.shift_type;
                assignedShifts_add.Add(assignedShift);
                output.Add(assignedShift);

                // 新增 StaffScheduleHistory
                newHistory.GUID = Guid.NewGuid().ToString();
                newHistory.assigned_shift_guid = assignedShift.GUID;
                histories_add.Add(newHistory);
            }
            else
            {
                // 更新 AssignedShift
                assignedShiftClass.updated_at = DateTime.Now.ToDateTimeString_6();
                assignedShiftClass.status = "正常";
                assignedShift.workShiftRequirement.shift_type = shift_group.shift_type;
                assignedShifts_update.Add(assignedShiftClass);
                output.Add(assignedShiftClass);

                // 更新 StaffScheduleHistory
                var histories = sql_StaffHistory
                    .GetRowsByDefult(null, new string[] { "assigned_shift_guid" }, new string[] { assignedShiftClass.GUID })
                    .SQLToClass<StaffScheduleHistoryClass>();

                if (histories.Count > 0)
                {
                    var his = histories[0];
                    his.time = newHistory.time;
                    his.updated_at = DateTime.Now.ToDateTimeString_6();
                    his.status = "正常";
                    histories_update.Add(his);
                }
                else
                {
                    // 若沒有歷程 → 新增
                    newHistory.GUID = Guid.NewGuid().ToString();
                    newHistory.assigned_shift_guid = assignedShiftClass.GUID;
                    histories_add.Add(newHistory);
                }
            }
        }

    }




    /// <summary>
    /// 小夜班需求槽位 (封裝 RequiredShift 與 WorkShiftRequirement)
    /// </summary>
    public class ShiftNeedSlot
    {
        public RequiredShiftClass req { get; set; }
        public WorkShiftRequirementClass wr { get; set; }
        public int order { get; set; }
        public int need { get; set; }
    }

    public static class SmallNightScheduler
    {
        /// <summary>
        /// 小夜班 — 連續四天排班 (梯隊模式)
        /// </summary>
        /// <param name="scheduleDays">排班日清單</param>
        /// <param name="group">小夜班群組</param>
        /// <param name="histories">既有的排班歷史 (會動態加入新的歷史)</param>
        /// <param name="shift_group_guid">群組 GUID</param>
        /// <param name="validationErrors">驗證錯誤訊息集合</param>
        /// <param name="preferLatestOnDay4">第四天收尾優先第四班，若滿則塞第五班</param>
        /// <returns>(assignedShifts, histories)</returns>
        public static (List<AssignedShiftClass>, List<StaffScheduleHistoryClass>) Assign(
            List<ScheduleDayClass> scheduleDays,
            ShiftGroupClass group,
            List<StaffScheduleHistoryClass> histories,
            string shift_group_guid,
            List<string> validationErrors,
            bool preferLatestOnDay4 = true)
        {
            var results = new List<AssignedShiftClass>();
            var newHistories = new List<StaffScheduleHistoryClass>();

            if (group?.Members == null || group.Members.Count == 0)
                return (results, newHistories);

            Queue<ShiftGroupMemberClass> queue = new Queue<ShiftGroupMemberClass>(group.Members);

            // 建立每日需求快取
            var dayNeeds = scheduleDays.ToDictionary(
                d => d.date,
                d => d.RequiredShifts
                    .Where(r => r.shift_group_guid == shift_group_guid)
                    .SelectMany(r => r.workShiftRequirements.Select((wr, idx) => new ShiftNeedSlot
                    {
                        req = r,
                        wr = wr,
                        order = idx,
                        need = wr.required_count.StringToInt32()
                    }))
                    .ToList()
            );

            // 每人要吃 4 天
            for (int i = 0; i < scheduleDays.Count - 3; i++)
            {
                string d1 = scheduleDays[i].date;
                string d2 = scheduleDays[i + 1].date;
                string d3 = scheduleDays[i + 2].date;
                string d4 = scheduleDays[i + 3].date;

                while (queue.Count > 0)
                {
                    var member = queue.Dequeue();
                    var staff = member.staff_info;
                    if (staff == null) continue;

                    // 嘗試 4 日班鏈
                    var slot1 = PickSlot(dayNeeds, d1, 0);
                    var slot2 = PickSlot(dayNeeds, d2, 1);
                    var slot3 = PickSlot(dayNeeds, d3, 2);
                    var slot4 = PickDay4Slot(dayNeeds, d4, preferLatestOnDay4);

                    if (slot1.wr == null || slot2.wr == null || slot3.wr == null || slot4.wr == null)
                    {
                        // 無法排 → 放回隊列
                        queue.Enqueue(member);
                        break;
                    }

                    // 成功 → 填入四天
                    foreach (var slot in new[] { slot1, slot2, slot3, slot4 })
                    {
                        slot.wr.assigned_count = (slot.wr.assigned_count.StringToInt32() + 1).ToString();

                        var assigned = new AssignedShiftClass
                        {
                            GUID = Guid.NewGuid().ToString().ToUpper(),
                            date = slot.date,
                            staff_guid = staff.GUID,
                            req_shift_guid = slot.req.GUID,
                            shift_requirement = JsonSerializer.Serialize(slot.wr),
                            created_at = DateTime.Now.ToDateTimeString_6(),
                            updated_at = DateTime.Now.ToDateTimeString_6(),
                            status = "正常",
                            staff = staff
                        };
                        results.Add(assigned);

                        var history = new StaffScheduleHistoryClass
                        {
                            GUID = Guid.NewGuid().ToString().ToUpper(),
                            staff_guid = staff.GUID,
                            date = slot.date,
                            time = slot.wr.time,
                            department = slot.wr.department,
                            req_shift_guid = slot.req.GUID,
                            shift_group_guid = shift_group_guid,
                            created_at = DateTime.Now.ToDateTimeString_6(),
                            updated_at = DateTime.Now.ToDateTimeString_6(),
                            status = "正常",
                            source = "自動排班",
                            assigned_shift_guid = assigned.GUID
                        };
                        newHistories.Add(history);
                    }

                    // 一個人完成四天，不再放回 queue
                }
            }

            return (results, newHistories);
        }

        /// <summary>挑指定日指定班別</summary>
        private static (string date, RequiredShiftClass req, WorkShiftRequirementClass wr) PickSlot(
            Dictionary<string, List<ShiftNeedSlot>> dayNeeds, string date, int order)
        {
            if (!dayNeeds.ContainsKey(date)) return (date, null, null);
            var list = dayNeeds[date];
            var slot = list.FirstOrDefault(x => x.order == order);
            if (slot == null) return (date, null, null);
            if (slot.wr.assigned_count.StringToInt32() >= slot.need) return (date, null, null);
            return (date, slot.req, slot.wr);
        }

        /// <summary>第四天 → 優先第四班，若滿了用第五班</summary>
        private static (string date, RequiredShiftClass req, WorkShiftRequirementClass wr) PickDay4Slot(
            Dictionary<string, List<ShiftNeedSlot>> dayNeeds, string date, bool preferLatestOnDay4)
        {
            if (!dayNeeds.ContainsKey(date)) return (date, null, null);
            var list = dayNeeds[date];

            if (preferLatestOnDay4)
            {
                var slot4 = list.FirstOrDefault(x => x.order == 3);
                if (slot4 != null && slot4.wr.assigned_count.StringToInt32() < slot4.need)
                    return (date, slot4.req, slot4.wr);
            }

            var slot5 = list.FirstOrDefault(x => x.order == 4);
            if (slot5 != null && slot5.wr.assigned_count.StringToInt32() < slot5.need)
                return (date, slot5.req, slot5.wr);

            return (date, null, null);
        }
    }



}
