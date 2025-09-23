using Basic;
using PharmaRosterLib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using System.Threading.Tasks;

namespace PharmaRosterAPI
{
    [Route("phar_roster_api/[controller]")]
    [ApiController]
    public class leaveRequest : ControllerBase
    {
        /// <summary>
        /// 初始化 LeaveRequest 資料表
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於初始化與建立 **LeaveRequest 資料表**。  
        /// 通常在系統啟動或第一次執行時呼叫，確保資料表存在並能正確進行資料操作。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "init",
        ///   "ValueAry": []
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例
        /// ✅ 成功
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "init",
        ///   "Result": "初始化 leaveRequest 資料表完成",
        ///   "Data": [
        ///     {
        ///       "TableName": "leave_requests",
        ///       "Columns": [
        ///         { "Name": "GUID", "Type": "VARCHAR(50)", "IsPrimary": true },
        ///         { "Name": "staff_guid", "Type": "VARCHAR(50)" },
        ///         { "Name": "start_date", "Type": "DATETIME" },
        ///         { "Name": "end_date", "Type": "DATETIME" },
        ///         { "Name": "reason", "Type": "TEXT" },
        ///         { "Name": "created_at", "Type": "DATETIME" }
        ///       ]
        ///     }
        ///   ],
        ///   "TimeTaken": "0.004s"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (例外狀況)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "init",
        ///   "Result": "Exception: 資料庫連線失敗",
        ///   "Data": [],
        ///   "TimeTaken": "0.002s"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - 此 API 執行時通常不需附帶參數。  
        /// - 系統會自動檢查並建立 <c>leave_requests</c> 資料表。  
        /// - 若資料表已存在，則只會回傳確認資訊，不會重複建立。  
        /// </remarks>
        /// <param name="returnData">前端傳入的 Request，一般情況下僅需指定 Method 即可</param>
        /// <returns>回傳標準化 JSON，包含 Code、Method、Result、TimeTaken 與 Data (資料表結構)</returns>
        [HttpPost("init")]
        public string init([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "init";

            try
            {
                List<Table> tables = new List<Table>();
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<LeaveRequestClass>());

                returnData.Code = 200;
                returnData.Data = tables;
                returnData.Result = "初始化 leaveRequest 資料表完成";
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
        /// 查詢請假 / 特例清單 (Leave Requests)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於依據查詢條件 (GUID、staff_guid、日期區間) 取得 **請假 / 特例 (LeaveRequest)** 資料，  
        /// 並支援分頁與排序功能。  
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "get_leaveRequests",
        ///   "ValueAry": [
        ///     "staff_guid=S1234567-ABCD-1234-ABCD-987654321000",
        ///     "start_date=2025-09-01",
        ///     "end_date=2025-09-30",
        ///     "page=1",
        ///     "pageSize=20",
        ///     "sortBy=created_at",
        ///     "sortOrder=desc"
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例  
        /// ✅ 成功  
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_leaveRequests",
        ///   "Result": "共取得(2)筆資料",
        ///   "Data": [
        ///     {
        ///       "GUID": "L1234567-89AB-CDEF-0123-456789ABCDEF",
        ///       "staff_guid": "S1234567-ABCD-1234-ABCD-987654321000",
        ///       "start_date": "2025-09-25",
        ///       "end_date": "2025-09-28",
        ///       "reason": "家庭旅遊",
        ///       "created_at": "2025-09-20 09:00:00",
        ///       "staff_info": {
        ///         "GUID": "S1234567-ABCD-1234-ABCD-987654321000",
        ///         "staff_id": "STAFF001",
        ///         "staff_name": "王小明",
        ///         "role": "藥師",
        ///         "created_at": "2025-01-01 08:00:00",
        ///         "updated_at": "2025-09-18 14:30:00"
        ///       }
        ///     },
        ///     {
        ///       "GUID": "L9876543-21FE-DCBA-4321-1234567890AB",
        ///       "staff_guid": "S9876543-21FE-DCBA-4321-1234567890AB",
        ///       "start_date": "2025-09-10",
        ///       "end_date": "2025-09-12",
        ///       "reason": "醫療休養",
        ///       "created_at": "2025-09-08 16:20:00",
        ///       "staff_info": {
        ///         "GUID": "S9876543-21FE-DCBA-4321-1234567890AB",
        ///         "staff_id": "STAFF002",
        ///         "staff_name": "李小華",
        ///         "role": "藥助",
        ///         "created_at": "2024-11-01 08:00:00",
        ///         "updated_at": "2025-09-15 10:00:00"
        ///       }
        ///     }
        ///   ],
        ///   "TimeTaken": "0.005s",
        ///   "TotalCount": 2,
        ///   "TotalPages": 1,
        ///   "CurrentPage": 1,
        ///   "PageSize": 20
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (缺少查詢條件)  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_leaveRequests",
        ///   "Result": "ValueAry 不能為空",
        ///   "Data": [],
        ///   "TimeTaken": "0.001s"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - `ValueAry` 為必填，需以 `key=value` 形式傳入查詢條件。  
        /// - 支援的 key：`GUID`、`staff_guid`、`start_date`、`end_date`、`page`、`pageSize`、`sortBy`、`sortOrder`。  
        /// - 日期條件支援範圍查詢 (start_date ~ end_date)。  
        /// - 若無指定排序，預設依 `created_at desc` 排序。  
        /// - 回傳的每筆資料會包含其對應的 `staff_info` (Staff 基本資訊)。  
        /// </remarks>
        /// <param name="returnData">前端傳入的 Request，需包含 ValueAry 作為查詢條件</param>
        /// <returns>回傳標準化 JSON，包含 LeaveRequest 清單及分頁資訊</returns>
        [HttpPost("get_leaveRequests")]
        public string get_leaveRequests([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_leaveRequests";

            try
            {
                // 驗證必填
                if (returnData.ValueAry == null || returnData.ValueAry.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "ValueAry 不能為空";
                    return returnData.JsonSerializationt();
                }
                var sql_staff = MethodClass.GetSQLControl<StaffClass>();

                (List<LeaveRequestClass> leaveRequests, int totalCount, int totalPages, int pageSize, int currentPage) = GetLeaveRequests(returnData.ValueAry);
                returnData.Code = 200;
                returnData.Result = $"共取得({leaveRequests.Count})筆資料";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = leaveRequests;
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
        /// 新增或更新 LeaveRequest 請假資料
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於批次新增或更新 **LeaveRequest 請假資料**。  
        /// 系統會依據 <c>GUID</c> 檢核：  
        /// - 若資料庫不存在該 <c>GUID</c> → 新增 (系統自動產生新 GUID)。  
        /// - 若資料庫已存在該 <c>GUID</c> → 更新 (保留既有 GUID，刷新欄位內容)。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "add_and_update_leaveRequests",
        ///   "Data": [
        ///     {
        ///       "GUID": "",
        ///       "staff_guid": "STAFF-001",
        ///       "start_date": "2025-09-20",
        ///       "end_date": "2025-09-22",
        ///       "reason": "家庭因素"
        ///     },
        ///     {
        ///       "GUID": "1C6EAC92-25E1-46D7-AB5C-21AB3D123456",
        ///       "staff_guid": "STAFF-002",
        ///       "start_date": "2025-09-23",
        ///       "end_date": "2025-09-24",
        ///       "reason": "病假"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例
        /// ✅ 成功
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "add_and_update_leaveRequests",
        ///   "Result": "新增(1)筆資料,修改(1)筆資料",
        ///   "TimeTaken": "0.032s",
        ///   "Data": []
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (缺少必要欄位)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_leaveRequests",
        ///   "Result": "參數驗證失敗：staff_guid 為必填"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (傳入格式錯誤)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_leaveRequests",
        ///   "Result": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (例外狀況)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_leaveRequests",
        ///   "Result": "Exception message"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - `GUID` 新增時可傳空字串，系統會自動產生。  
        /// - `staff_guid`、`start_date`、`end_date` 為必填欄位。  
        /// - `start_date`、`end_date` 必須符合日期格式 (yyyy-MM-dd)。  
        /// - 傳入多筆資料時，系統會逐筆判斷新增或更新。  
        /// </remarks>
        /// <param name="returnData">前端傳入的 Request，包含欲新增或更新的 Data 陣列 (LeaveRequestClass)</param>
        /// <returns>回傳標準化 JSON，包含 Code、Method、Result、TimeTaken 與 Data</returns>
        [HttpPost("add_and_update_leaveRequests")]
        public string add_and_update_leaveRequests([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "add_and_update_leaveRequests";

            try
            {
                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<LeaveRequestClass> input = returnData.Data.ObjToClass<List<LeaveRequestClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var sql_leaveRequest = MethodClass.GetSQLControl<LeaveRequestClass>();
                var output = new List<LeaveRequestClass>();
                var datas_add = new List<LeaveRequestClass>();
                var datas_update = new List<LeaveRequestClass>();

                // === 2. 寫入流程 ===
                foreach (var temp in input)
                {
                    // 檢核必填
                    if (temp.staff_guid.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "參數驗證失敗：staff_guid 為必填";
                        return returnData.JsonSerializationt();
                    }
                    if (temp.start_date.Check_Date_String() == false)
                    {
                        returnData.Code = -200;
                        returnData.Result = "參數驗證失敗：start_date 為必填";
                        return returnData.JsonSerializationt();
                    }
                    if (temp.end_date.Check_Date_String() == false)
                    {
                        returnData.Code = -200;
                        returnData.Result = "參數驗證失敗：end_date 為必填";
                        return returnData.JsonSerializationt();
                    }

                    List<object[]> list_objects = sql_leaveRequest.GetRowsByDefult(null, "GUID", temp.GUID);

                    if (list_objects.Count == 0)
                    {
                        temp.GUID = Guid.NewGuid().ToString();
                        temp.created_at = DateTime.Now.ToDateTimeString_6();
                        datas_add.Add(temp);
                    }
                    else
                    {
                        LeaveRequestClass leaveRequest = list_objects[0].SQLToClass<LeaveRequestClass>();
                        temp.GUID = leaveRequest.GUID;
                        temp.created_at = DateTime.Now.ToDateTimeString_6();
                        datas_update.Add(temp);
                    }
                }

                if (datas_add.Count > 0) sql_leaveRequest.AddRows(null, datas_add.ClassToSQL<LeaveRequestClass>());
                if (datas_update.Count > 0) sql_leaveRequest.UpdateByDefulteExtra(null, datas_update.ClassToSQL<LeaveRequestClass>());

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
        /// 刪除 LeaveRequest 請假資料
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於批次刪除 **LeaveRequest 請假資料**。  
        /// 系統會依據 <c>GUID</c> 檢核是否存在於資料庫：  
        /// - 若存在 → 刪除紀錄。  
        /// - 若不存在 → 略過不處理。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "delete_leaveRequests",
        ///   "Data": [
        ///     { "GUID": "C5B2E3D1-9F21-4F2A-B3A7-0DBB12345678" },
        ///     { "GUID": "7A6D12A9-5B10-43E2-B7E1-ABCD98765432" }
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例
        /// ✅ 成功
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "delete_leaveRequests",
        ///   "Result": "刪除(2)筆資料",
        ///   "Data": [],
        ///   "TimeTaken": "0.005s"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (缺少必要欄位)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_leaveRequests",
        ///   "Result": "參數驗證失敗：guid 為必填"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (傳入格式錯誤)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_leaveRequests",
        ///   "Result": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (例外狀況)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_leaveRequests",
        ///   "Result": "Exception message"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - `GUID` 為必填欄位，必須指定要刪除的紀錄。  
        /// - 傳入多筆資料時，系統會逐筆檢核並刪除。  
        /// - 若 GUID 不存在於資料庫，該筆資料將被忽略，不會報錯。  
        /// </remarks>
        /// <param name="returnData">前端傳入的 Request，包含欲刪除的 Data 陣列 (需含 GUID)</param>
        /// <returns>回傳標準化 JSON，包含 Code、Method、Result、TimeTaken 與 Data</returns>
        [HttpPost("delete_leaveRequests")]
        public string delete_leaveRequests([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_leaveRequests";

            try
            {
                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<LeaveRequestClass> input = returnData.Data.ObjToClass<List<LeaveRequestClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var sql_leaveRequest = MethodClass.GetSQLControl<LeaveRequestClass>();
                var output = new List<LeaveRequestClass>();
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
                    List<object[]> list_objects = sql_leaveRequest.GetRowsByDefult(null, "GUID", temp.GUID);

                    if (list_objects.Count != 0)
                    {
                        list_delete.Add(list_objects[0]);
                    }
                }

                if (list_delete.Count > 0) sql_leaveRequest.DeleteExtra(null, list_delete);

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
        /// 查詢請假 / 特例 (LeaveRequest) 清單
        /// </summary>
        /// <remarks>
        /// <b>用途：</b><br/>
        /// 本方法用於依據查詢條件 (GUID、staff_guid、日期區間等) 取得 <c>leave_requests</c> 資料表的紀錄，
        /// 並支援分頁與排序功能。
        ///
        /// <b>功能說明：</b><br/>
        /// 1. 支援依 <c>GUID</c> 查詢單筆紀錄。  
        /// 2. 支援依 <c>staff_guid</c> 查詢指定人員的請假紀錄。  
        /// 3. 支援依 <c>start_date</c> 與 <c>end_date</c> 過濾日期區間。  
        /// 4. 提供分頁 (<c>page</c>、<c>pageSize</c>) 與排序 (<c>sortBy</c>、<c>sortOrder</c>) 功能。  
        /// 5. 回傳分頁資訊 (總筆數、總頁數、目前頁次)。  
        ///
        /// <b>輸入參數 (ValueAry)：</b><br/>
        /// 請以字串陣列傳入查詢條件，每個元素格式為 <c>key=value</c>。  
        /// 支援的 key 包含：  
        /// - <c>GUID</c>：指定唯一識別碼 (選填)。  
        /// - <c>staff_guid</c>：人員識別碼 (選填)。  
        /// - <c>start_date</c>：查詢起始日期 (yyyy-MM-dd，可選填)。  
        /// - <c>end_date</c>：查詢結束日期 (yyyy-MM-dd，可選填)。  
        /// - <c>page</c>：頁碼 (預設 1)。  
        /// - <c>pageSize</c>：每頁筆數 (預設 50)。  
        /// - <c>sortBy</c>：排序欄位 (預設 updated_at)。  
        /// - <c>sortOrder</c>：排序方式 asc/desc (預設 desc)。  
        ///
        /// 範例：
        /// <code>
        /// var ValueAry = new List(string)
        /// {
        ///     "staff_guid=STAFF-12345",
        ///     "start_date=2025-09-01",
        ///     "end_date=2025-09-30",
        ///     "page=1",
        ///     "pageSize=20",
        ///     "sortBy=start_date",
        ///     "sortOrder=asc"
        /// };
        ///
        /// var result = GetLeaveRequests(ValueAry);
        /// </code>
        ///
        /// <b>回傳內容：</b><br/>
        /// - <c>staffClasses</c> (List&lt;LeaveRequestClass&gt;)：查詢到的請假紀錄清單。  
        /// - <c>totalCount</c> (int)：符合條件的總筆數。  
        /// - <c>totalPages</c> (int)：總頁數。  
        /// - <c>pageSize</c> (int)：每頁筆數。  
        /// - <c>currentPage</c> (int)：目前頁碼。  
        ///
        /// </remarks>
        /// <param name="ValueAry">
        /// 查詢條件的字串陣列，每個元素格式為 <c>key=value</c>。  
        /// 支援的 key 包含：GUID、staff_guid、start_date、end_date、page、pageSize、sortBy、sortOrder。
        /// </param>
        /// <returns>
        /// Tuple (staffClasses, totalCount, totalPages, pageSize, currentPage)：  
        /// - staffClasses：符合條件的請假紀錄清單  
        /// - totalCount：總筆數  
        /// - totalPages：總頁數  
        /// - pageSize：每頁筆數  
        /// - currentPage：目前頁碼  
        /// </returns>
        public static (List<LeaveRequestClass> leaveRequests, int totalCount, int totalPages, int pageSize, int currentPage) GetLeaveRequests(List<string> ValueAry)
        {
            return GetLeaveRequestsAsync(ValueAry).GetAwaiter().GetResult();
        }
        /// <summary>
        /// 查詢請假 / 特例 (LeaveRequest) 清單 (非同步)
        /// </summary>
        public static async Task<(List<LeaveRequestClass> leaveRequests, int totalCount, int totalPages, int pageSize, int currentPage)> GetLeaveRequestsAsync(List<string> ValueAry)
        {
            string Esc(string s) => (s ?? "").Replace("'", "''");
            var sql_leaveRequest = MethodClass.GetSQLControl<LeaveRequestClass>();
            var sql_staff = MethodClass.GetSQLControl<StaffClass>();

            List<StaffClass> staffs = staff.GetAllStaffs(false);
            Dictionary<string, List<StaffClass>> keyValuePairs_staffs = staffs.CoverToDictionaryByGUID();

            string GetVal(string key) =>
                ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                ?.Split('=')[1];

            string GUID = GetVal("GUID") ?? "";
            string staff_guid = GetVal("staff_guid") ?? "";
            string start_date = GetVal("start_date") ?? "";
            string end_date = GetVal("end_date") ?? "";

            int page = (GetVal("page") ?? "1").StringToInt32();
            int pageSize = (GetVal("pageSize") ?? "50").StringToInt32();
            string sortBy = GetVal("sortBy") ?? "created_at";
            string sortOrder = (GetVal("sortOrder") ?? "desc").ToUpper();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 2000;

            List<LeaveRequestClass> leaveRequests = new List<LeaveRequestClass>();
            int totalCount = 0, totalPages = 0;

            string where = $" WHERE 1=1 ";
            if (!GUID.StringIsEmpty()) where += $" AND GUID = '{Esc(GUID)}' ";
            if (!staff_guid.StringIsEmpty()) where += $" AND staff_guid = '{Esc(staff_guid)}' ";
            if (!start_date.StringIsEmpty()) where += $" AND start_date >= '{Esc(start_date)}' ";
            if (!end_date.StringIsEmpty()) where += $" AND end_date <= '{Esc(end_date)}' ";

            string orderBy = $" ORDER BY {sortBy} {sortOrder} ";
            int offset = (page - 1) * pageSize;

            string countSql = $"SELECT COUNT(*) AS cnt FROM {sql_leaveRequest.Database}.{sql_leaveRequest.TableName} {where}";
            var dtCnt = await sql_leaveRequest.WtrteCommandAndExecuteReaderAsync(countSql);
            totalCount = dtCnt.Rows[0]["cnt"].ToString().StringToInt32();
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            string querySql = $@"
                                SELECT * FROM {sql_leaveRequest.Database}.{sql_leaveRequest.TableName}
                                {where}
                                {orderBy}
                                LIMIT {offset}, {pageSize}";
            var dt = await sql_leaveRequest.WtrteCommandAndExecuteReaderAsync(querySql);
            leaveRequests = dt.DataTableToRowList().SQLToClass<LeaveRequestClass>() ?? new List<LeaveRequestClass>();

            foreach (var leaveRequest in leaveRequests)
            {
                if (keyValuePairs_staffs.ContainsKey(leaveRequest.staff_guid))
                {
                    leaveRequest.staff_info = keyValuePairs_staffs[leaveRequest.staff_guid][0];
                }
            }

            return (leaveRequests, totalCount, totalPages, pageSize, page);
        }

    }
}
