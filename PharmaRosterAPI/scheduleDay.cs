using Basic;
using Microsoft.AspNetCore.Mvc;
using PharmaRosterLib;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
        public string get_staffs([FromBody] returnData returnData)
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
                    input.workShiftRequirements = shiftGroupClass.workShiftRanges.UpdateRequirements(requiredShift.workShiftRequirements);
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
        /// 刪除每日需求班次 (RequiredShift)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於刪除指定的每日需求班次 (RequiredShiftClass)。  
        /// 系統會依據傳入的 <c>GUID</c> 清單逐筆檢查，若紀錄存在則刪除。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "delete_requiredShifts",
        ///   "Data": [
        ///     { "GUID": "1111-2222-3333-4444" },
        ///     { "GUID": "5555-6666-7777-8888" }
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "delete_requiredShifts",
        ///   "Result": "刪除(2)筆資料",
        ///   "TimeTaken": "28ms",
        ///   "Data": []
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少必要欄位：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_requiredShifts",
        ///   "Result": "參數驗證失敗：guid 為必填"
        /// }
        /// ```
        ///
        /// - Data 格式錯誤：  
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
        /// - <c>GUID</c> 為必填欄位，必須對應到資料表中的紀錄。  
        /// - 若傳入的 <c>GUID</c> 不存在，系統會略過，不會產生錯誤。  
        /// - 此 API 僅刪除需求班次 (RequiredShift)，不會影響其他關聯資料。  
        /// </remarks>
        /// <param name="returnData">統一封裝的請求與回應物件，需包含 Data 陣列</param>
        /// <returns>JSON 格式的回應字串，包含刪除筆數與狀態</returns>
        [HttpPost("delete_requiredShifts")]
        public string delete_requiredShifts([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_requiredShifts";

            try
            {
                var sql_RequiredShift = MethodClass.GetSQLControl<RequiredShiftClass>();
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
                var requiredShifts = GetAllRequiredShifts();
                var output = new List<RequiredShiftClass>();
                var list_delete = new List<object[]>();
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

                    if (list_objects.Count != 0)
                    {
                        list_delete.Add(list_objects[0]);
                    }
                }

                if (list_delete.Count > 0) sql_RequiredShift.DeleteExtra(null, list_delete);

                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Result = $"刪除({list_delete.Count})筆資料";
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
        /// 新增或更新實際排班紀錄 (AssignedShift)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於批次新增或更新每日實際排班 (AssignedShiftClass)。  
        /// 系統會根據指定的 <c>requ_shift_guid</c> 與 <c>staff_guid</c> 進行檢核：  
        /// - 若該日期與需求班次下尚無此人員 → 新增一筆排班。  
        /// - 若已存在相同紀錄 → 更新 (狀態與時間)。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "add_and_update_assigned_shifts",
        ///   "Data": [
        ///     {
        ///       "GUID": "",
        ///       "date": "2025-09-22",
        ///       "requ_shift_guid": "R123-456-789",
        ///       "staff_guid": "S001-123-456",
        ///       "status": "",
        ///       "created_at": "",
        ///       "updated_at": ""
        ///     },
        ///     {
        ///       "GUID": "",
        ///       "date": "2025-09-22",
        ///       "requ_shift_guid": "R123-456-789",
        ///       "staff_guid": "S002-123-456",
        ///       "status": "",
        ///       "created_at": "",
        ///       "updated_at": ""
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
        ///   "Result": "新增(1)筆資料,修改(1)筆資料",
        ///   "TimeTaken": "57ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "A123-456B-789C",
        ///       "date": "2025-09-22",
        ///       "requ_shift_guid": "R123-456-789",
        ///       "staff_guid": "S001-123-456",
        ///       "status": "正常",
        ///       "created_at": "2025-09-22 09:00:00",
        ///       "updated_at": "2025-09-22 09:00:00"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少 Data：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_assigned_shifts",
        ///   "Result": "Data 不能為空"
        /// }
        /// ```
        ///
        /// - Data 格式錯誤：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_assigned_shifts",
        ///   "Result": "Data 格式錯誤或無有效資料"
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
        /// - <c>requ_shift_guid</c> 必須對應到有效的 RequiredShift 紀錄。  
        /// - <c>staff_guid</c> 必須為指定班群 (ShiftGroup) 下的有效人員。  
        /// - 新增時系統自動產生 GUID，更新時會沿用既有 GUID。  
        /// - 預設會將 <c>status</c> 設為 "正常"。  
        /// </remarks>
        /// <param name="returnData">統一封裝的請求與回應物件，需包含 Data 陣列</param>
        /// <returns>JSON 格式的回應字串，包含新增/更新筆數與狀態</returns>
        [HttpPost("add_and_update_assigned_shifts")]
        public async Task<string> add_and_update_assigned_shifts([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "add_and_update_assigned_shifts";

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

                List<AssignedShiftClass> input = returnData.Data.ObjToClass<List<AssignedShiftClass>>();
                if (input == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }


                var shiftGroups = await shiftGroup.GetShiftGroupsAsync();
                Dictionary<string, List<ShiftGroupClass>> keyValuePairs_shiftGroups = shiftGroups.CoverToDictionaryByGUID();
                var shiftGroups_buf = new List<ShiftGroupClass>();

                var sql_AssignedShift = MethodClass.GetSQLControl<AssignedShiftClass>();
                var assignedShifts_add = new List<AssignedShiftClass>();
                var assignedShifts_update = new List<AssignedShiftClass>();
                var output = new List<AssignedShiftClass>();


                string[] dates = (from x in input select x.date).Distinct().ToList().ToArray();
                List<ScheduleDayClass> scheduleDays = await GetSchedulesDayAsync(dates);

                var requiredShifts = await GetAllRequiredShiftsAsync();
                Dictionary<string, List<RequiredShiftClass>> keyValuePairs_requiredShifts = requiredShifts.CoverToDictionaryByGUID();
                var requiredShifts_buf = new List<RequiredShiftClass>();


                foreach (var assignedShift in input)
                {
                    string date = assignedShift.date;
                    string req_shift_guid = assignedShift.req_shift_guid;
                    string staff_guid = assignedShift.staff_guid;
                    if (date.Check_Date_String() == false) continue;
                    requiredShifts_buf = keyValuePairs_requiredShifts.SortDictionaryByGUID(req_shift_guid);
                    if (requiredShifts_buf.Count == 0) continue;

                    string shift_group_guid = requiredShifts_buf[0].shift_group_guid;
                    shiftGroups_buf = keyValuePairs_shiftGroups.SortDictionaryByGUID(shift_group_guid);

                    StaffClass staff = shiftGroups_buf[0].SerchStaff(staff_guid);
                    if (staff == null) continue;
                    ScheduleDayClass scheduleDay = scheduleDays.SerchByDate(date);
                    if (scheduleDay == null) continue;

                    AssignedShiftClass assignedShiftClass = scheduleDay.AssignedShifts.SerchByStaffGUID(req_shift_guid, staff_guid);
                    if (assignedShiftClass == null)
                    {
                        assignedShift.GUID = Guid.NewGuid().ToString();
                        assignedShift.updated_at = DateTime.Now.ToDateTimeString_6();
                        assignedShift.created_at = DateTime.Now.ToDateTimeString_6();
                        assignedShift.status = "正常";
                        assignedShifts_add.Add(assignedShift);
                        output.Add(assignedShift);
                    }
                    else
                    {
                        assignedShiftClass.updated_at = DateTime.Now.ToDateTimeString_6();
                        assignedShiftClass.status = "正常";
                        assignedShifts_update.Add(assignedShiftClass);
                        output.Add(assignedShiftClass);
                    }

                }

                if (assignedShifts_add.Count > 0) sql_AssignedShift.AddRows(null, assignedShifts_add.ClassToSQL<AssignedShiftClass>());
                if (assignedShifts_update.Count > 0) sql_AssignedShift.UpdateByDefulteExtra(null, assignedShifts_update.ClassToSQL<AssignedShiftClass>());

                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Result = $"新增({assignedShifts_add.Count})筆資料,修改({assignedShifts_update.Count})筆資料";
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
        /// 刪除已指派的排班紀錄 (AssignedShift)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於刪除指定的 **AssignedShift** 實際排班紀錄。  
        /// 系統會依據傳入的 <c>GUID</c> 進行刪除：  
        /// - 若 GUID 存在 → 刪除該筆排班紀錄。  
        /// - 若 GUID 不存在 → 忽略，不進行刪除。  
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
        ///   "Result": "刪除(2)筆資料",
        ///   "TimeTaken": "34ms",
        ///   "Data": []
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
        /// - 若輸入的 GUID 在資料庫中不存在，該筆會被忽略，不影響其他刪除動作。  
        /// </remarks>
        /// <param name="returnData">統一封裝的請求與回應物件，需包含 Data 陣列 (每筆至少需有 GUID)</param>
        /// <returns>JSON 格式的回應字串，包含刪除筆數與執行結果</returns>
        [HttpPost("delete_assigned_shifts")]
        public string delete_assigned_shifts([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_assigned_shifts";

            try
            {
                var sql_assigned_shifts = MethodClass.GetSQLControl<AssignedShiftClass>();
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
                var requiredShifts = GetAllRequiredShifts();
                var output = new List<RequiredShiftClass>();
                var list_delete = new List<object[]>();
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
                    List<object[]> list_objects = sql_assigned_shifts.GetRowsByDefult(null, "GUID", temp.GUID);

                    if (list_objects.Count != 0)
                    {
                        list_delete.Add(list_objects[0]);
                    }
                }

                if (list_delete.Count > 0) sql_assigned_shifts.DeleteExtra(null, list_delete);

                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Result = $"刪除({list_delete.Count})筆資料";
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
                    if (sg_buf.Count > 0) requiredShift.shift_group = sg_buf[0];
                }
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

    }
}
