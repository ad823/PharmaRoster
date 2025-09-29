using Basic;
using Microsoft.AspNetCore.Mvc;
using PharmaRosterLib;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

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
        /// 查詢行事曆日 (ScheduleDay)，並包含需求班次、已指派班次、排班日誌、特殊日與請假紀錄
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於查詢指定日期區間的行事曆資料 (ScheduleDayClass)。  
        /// 系統會自動關聯以下資訊：  
        /// - <c>RequiredShifts</c>：當日需求班次  
        /// - <c>AssignedShifts</c>：當日已指派班次  
        /// - <c>ScheduleLogs</c>：當日排班日誌  
        /// - <c>SpecialDays</c>：當日是否為特殊日 (如國定假日)  
        /// - <c>LeaveRequests</c>：落在該日範圍內的請假紀錄  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "get_schedule_days",
        ///   "ValueAry": [
        ///     "date_start=2025-09-01",
        ///     "date_end=2025-09-30",
        ///     "page=1",
        ///     "pageSize=20",
        ///     "sortOrder=asc"
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_schedule_days",
        ///   "Result": "共取得(2)筆資料",
        ///   "TimeTaken": "53ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "D111-2222-3333-4444",
        ///       "date": "2025-09-01",
        ///       "created_at": "2025-09-01 00:00:00",
        ///       "updated_at": "2025-09-01 00:00:00",
        ///       "RequiredShifts": [ { "GUID": "...", "shift_group_guid": "...", "required_count": "3" } ],
        ///       "AssignedShifts": [ { "GUID": "...", "staff_guid": "...", "shift_group_guid": "...", "status": "confirmed" } ],
        ///       "ScheduleLogs": [ { "GUID": "...", "action": "auto_schedule", "timestamp": "2025-09-01 01:23:45" } ],
        ///       "SpecialDays": [ { "GUID": "...", "date": "2025-09-01", "override_required_count": "2" } ],
        ///       "LeaveRequests": [ { "GUID": "...", "staff_guid": "S001", "start_date": "2025-09-01", "end_date": "2025-09-02", "reason": "休假" } ]
        ///     }
        ///   ],
        ///   "TotalCount": 2,
        ///   "TotalPages": 1,
        ///   "CurrentPage": 1,
        ///   "PageSize": 20
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少必要參數：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_schedule_days",
        ///   "Result": "ValueAry 不能為空"
        /// }
        /// ```
        ///
        /// - 系統例外：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_schedule_days",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - <c>date_start</c> 與 <c>date_end</c> 必須為有效日期字串 (yyyy-MM-dd)，可只給其中一個。  
        /// - 回傳資料會依據日期排序 (asc/desc)。  
        /// - 預設 <c>pageSize</c> 為 50 筆。  
        /// - 結果中會附帶分頁資訊 (TotalCount, TotalPages, CurrentPage, PageSize)。  
        /// </remarks>
        /// <param name="returnData">統一封裝的請求與回應物件，需包含 <c>ValueAry</c> 條件</param>
        /// <returns>JSON 格式的回應字串，包含行事曆日及其關聯資料</returns>
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
                    List<WorkShiftRequirementClass> workShiftRequirements = shiftGroupClass.workShiftRanges.UpdateRequirements(requiredShift.workShiftRequirements);
                    workShiftRequirements = workShiftRequirements.UpdateRequirements(input.workShiftRequirements);
                    input.workShiftRequirements = workShiftRequirements;
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
                        assignedShift, scheduleDay, staff, shift_group_guid,
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
        [HttpPost("auto_schedule")]
        public async Task<string> auto_schedule([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "auto_schedule";

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
                var scheduleHistorys = dt_scheduleHistory.DataTableToRowList()
                    .SQLToClass<StaffScheduleHistoryClass>() ?? new List<StaffScheduleHistoryClass>();

                scheduleHistorys = scheduleHistorys
                    .Where(x => x.shift_group_guid == shift_group_guid)
                    .ToList();

                // 4. 更新群組成員的 weight
                var keypairs_scheduleHistorys = scheduleHistorys.CoverToDictionaryBy_staff_guid();
                foreach (var member in shiftGroupClass.Members)
                {
                    var scheduleHistorys_buf = keypairs_scheduleHistorys.SortDictionaryBy_staff_guid(member.staff_guid);
                    if (scheduleHistorys_buf.Count > 0)
                    {
                        member.weight = (member.weight.StringToUInt32() + scheduleHistorys_buf.Count).ToString();
                    }
                }
                shiftGroupClass.Members = shiftGroupClass.Members.SortByWeightAndOrderIndex();

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

                    foreach (var req in day.RequiredShifts)
                    {
                        req.shift_group = new ShiftGroupClass();

                        foreach (var wr in req.workShiftRequirements.OrderBy(x => x.TimeRange.Value.start))
                        {
                            int requiredCount = wr.required_count.StringToInt32();
                            int assignedCount = wr.assigned_count.StringToInt32();

                            while (assignedCount < requiredCount)
                            {
                                var candidate = shiftGroupClass.Members.FirstOrDefault();
                                if (candidate == null) break;

                                StaffClass staff = candidate.staff_info;
                                if (staff == null) break;

                                var newHistory = new StaffScheduleHistoryClass
                                {
                                    GUID = Guid.NewGuid().ToString(),
                                    staff_guid = staff.GUID,
                                    date = req.date,
                                    time = wr.time,
                                    department = wr.department,
                                    req_shift_guid = req.GUID,
                                    shift_group_guid = shift_group_guid,
                                    created_at = DateTime.Now.ToDateTimeString_6(),
                                    updated_at = DateTime.Now.ToDateTimeString_6(),
                                    status = "正常",
                                    source = "自動排班"
                                };

                                var validation = ScheduleValidator.ValidateSchedule(staff, newHistory);
                                if (!validation.isValid)
                                {
                                    validationErrors.Add($"[{staff.staff_name}] {req.date} {wr.time} → {validation.message}");
                                    shiftGroupClass.Members.Remove(candidate);
                                    shiftGroupClass.Members.Add(candidate);
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
                                histories_add.Add(newHistory);

                                assignedCount++;
                                wr.assigned_count = assignedCount.ToString();

                                shiftGroupClass.Members.Remove(candidate);
                                shiftGroupClass.Members.Add(candidate);
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
                    returnData.Code = -200;
                    returnData.Result = "部分排班檢核失敗：\n" + string.Join("\n", validationErrors);
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
                    VALUES ('{newDay.GUID}', '{newDay.date}', '{newDay.created_at}', '{newDay.updated_at}')
                ";

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
                    if (sg_buf.Count > 0) requiredShift.shift_group = sg_buf[0];
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
            string shift_group_guid,
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
                shift_group_guid = shift_group_guid,
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
                assignedShift, scheduleDay, staff, shift_group_guid,
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
            string shift_group_guid,
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
                shift_group_guid = shift_group_guid,
                source = assignedShift.source == null ? "手動調整" : assignedShift.source,
                status = "正常"
            };

            // === 找舊的 AssignedShift ===
            AssignedShiftClass assignedShiftClass = scheduleDay.AssignedShifts
                .SerchByStaffGUID(assignedShift.req_shift_guid, assignedShift.staff_guid, assignedShift.shift_requirement);

            if (assignedShiftClass == null)
            {
                // 新增 AssignedShift
                assignedShift.GUID = Guid.NewGuid().ToString();
                assignedShift.updated_at = DateTime.Now.ToDateTimeString_6();
                assignedShift.created_at = DateTime.Now.ToDateTimeString_6();
                assignedShift.status = "正常";
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
}
