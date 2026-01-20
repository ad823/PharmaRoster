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
using System.Threading.Tasks;
using MyOffice;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace PharmaRosterAPI
{
    [Route("phar_roster_api/[controller]")]
    [ApiController]
    public class staff : ControllerBase
    {

        /// <summary>
        /// 初始化資料表 (Staff / StaffAttributesClass)
        /// </summary>
        /// <remarks>
        /// 此 API 用於系統初始化，會自動檢查並建立以下資料表：  
        /// - `Staffs`  
        /// - `StaffAttributesClass`  
        ///
        /// 若表已存在，則會檢查欄位並自動補齊缺少的欄位。  
        ///
        /// <br/><br/>
        /// 📥 **Request JSON 範例**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "SQL",
        ///   "Method": "init",
        ///   "ValueAry": [],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// <br/><br/>
        /// 📤 **Response JSON 範例 (成功)**
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "init",
        ///   "Result": "初始化 staff 資料表完成",
        ///   "TimeTaken": "12ms",
        ///   "Data": [
        ///     {
        ///       "TableName": "Staffs",
        ///       "ColumnList": [
        ///         { "Name": "GUID", "TypeName": "VARCHAR(50)", "IndexType": "PRIMARY" },
        ///         { "Name": "staff_id", "TypeName": "VARCHAR(50)", "IndexType": "INDEX" },
        ///         { "Name": "staff_name", "TypeName": "VARCHAR(100)", "IndexType": "NONE" }
        ///       ]
        ///     },
        ///     {
        ///       "TableName": "StaffAttributesClass",
        ///       "ColumnList": [
        ///         { "Name": "GUID", "TypeName": "VARCHAR(50)", "IndexType": "PRIMARY" },
        ///         { "Name": "staff_guid", "TypeName": "VARCHAR(50)", "IndexType": "INDEX" },
        ///         { "Name": "pregnant", "TypeName": "VARCHAR(10)", "IndexType": "NONE" }
        ///       ]
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// <br/><br/>
        /// ❌ **錯誤回傳範例**
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "init",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        /// </remarks>
        /// <param name="returnData">統一封裝的請求與回應物件</param>
        /// <returns>JSON 格式的回應字串</returns>
        [HttpPost("init")]
        public string init([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "init";

            try
            {
                List<Table> tables = new List<Table>();
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<StaffClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<StaffAttributesClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<StaffScheduleHistoryClass>());

                returnData.Code = 200;
                returnData.Data = tables;
                returnData.Result = "初始化 staff 資料表完成";
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
        /// 新增或更新 Staff 人員資料
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於批次新增或更新 **Staff 人員資料**。  
        /// 系統會依據 <c>staff_id</c> 進行檢核：
        /// - 若資料庫無相同 <c>staff_id</c> → 新增 (自動產生 GUID)。  
        /// - 若資料庫已存在相同 <c>staff_id</c> → 更新 (保留既有 GUID)。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "add_and_update",
        ///   "Data": [
        ///     {
        ///       "GUID": "",
        ///       "staff_id": "S001",
        ///       "staff_name": "王大明",
        ///       "role": "藥師",
        ///       "created_at": "2025-09-14 10:00:00",
        ///       "updated_at": "2025-09-14 10:00:00"
        ///     },
        ///     {
        ///       "GUID": "",
        ///       "staff_id": "S002",
        ///       "staff_name": "李小華",
        ///       "role": "藥助",
        ///       "created_at": "2025-09-14 10:05:00",
        ///       "updated_at": "2025-09-14 10:05:00"
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
        ///   "Message": "新增(1)筆資料,修改(1)筆資料",
        ///   "TimeTaken": "0.032s",
        ///   "Data": []
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (缺少必要欄位)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Message": "參數驗證失敗：staff_name 為必填"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (傳入格式錯誤)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Message": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (例外狀況)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Message": "Exception message"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - `GUID` 由系統自動產生，新增時可傳空字串。  
        /// - `staff_id` 與 `staff_name` 為必填欄位。  
        /// - `attributes` 須為 JSON 格式字串，例如：`{"pregnant":"false","night_only":"true"}`。  
        /// - 傳入多筆資料時，系統會逐筆判斷新增或更新。  
        /// </remarks>
        /// <param name="returnData">前端傳入的 Request 包含 Data 陣列</param>
        /// <returns>回傳標準化 JSON，包含 Code、Message、TimeTaken 與 Data</returns>
        [HttpPost("add_and_update_staffs")]
        public string add_and_update_staffs([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "add_and_update_staffs";

            try
            {
                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<StaffClass> input = returnData.Data.ObjToClass<List<StaffClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var sql_staff = MethodClass.GetSQLControl<StaffClass>();
                var output = new List<StaffClass>();
                var datas_add = new List<StaffClass>();
                var datas_update = new List<StaffClass>();

                // === 2. 寫入流程 ===
                foreach (var temp in input)
                {
                    // 檢核必填
                    if (temp.staff_id.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "參數驗證失敗：staff_id 為必填";
                        return returnData.JsonSerializationt();
                    }
                    if (temp.staff_name.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "參數驗證失敗：staff_name 為必填";
                        return returnData.JsonSerializationt();
                    }

                    List<object[]> list_objects = sql_staff.GetRowsByDefult(null, "staff_id", temp.staff_id);

                    if (list_objects.Count == 0)
                    {
                        temp.GUID = Guid.NewGuid().ToString();
                        datas_add.Add(temp);
                    }
                    else
                    {
                        StaffClass staffClass = list_objects[0].SQLToClass<StaffClass>();
                        temp.GUID = staffClass.GUID;
                        datas_update.Add(temp);
                    }
                }

                if (datas_add.Count > 0) sql_staff.AddRows(null, datas_add.ClassToSQL<StaffClass>());
                if (datas_update.Count > 0) sql_staff.UpdateByDefulteExtra(null, datas_update.ClassToSQL<StaffClass>());

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
        /// 刪除 Staff 人員資料
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於刪除指定 <c>Staff</c> 人員資料。  
        /// 系統會依據 <c>staff_id</c> 檢核，若資料存在則刪除，否則忽略。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "delete",
        ///   "Data": [
        ///     {
        ///       "staff_id": "S001"
        ///     },
        ///     {
        ///       "staff_id": "S002"
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
        ///   "Message": "刪除(2)筆資料",
        ///   "TimeTaken": "0.025s",
        ///   "Data": []
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (缺少必要欄位)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Message": "參數驗證失敗：staff_id 為必填"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (傳入格式錯誤)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Message": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (例外狀況)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Message": "Exception message"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - `staff_id` 為必填，用於定位資料。  
        /// - 支援批次刪除，傳入多筆 `staff_id`，系統會逐一檢查並刪除。  
        /// - 若指定的 `staff_id` 不存在，則忽略不處理。  
        /// - `GUID` 不需傳入，刪除邏輯完全依 <c>staff_id</c> 判斷。  
        /// </remarks>
        /// <param name="returnData">前端傳入的 Request 包含 Data 陣列</param>
        /// <returns>回傳標準化 JSON，包含 Code、Message、TimeTaken 與 Data</returns>
        [HttpPost("delete_staffs")]
        public string delete_staffs([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_staffs";

            try
            {
                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<StaffClass> input = returnData.Data.ObjToClass<List<StaffClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var sql_staff = MethodClass.GetSQLControl<StaffClass>();
                var output = new List<StaffClass>();
                var list_delete = new List<object[]>();
                // === 2. 刪除流程 ===
                foreach (var temp in input)
                {
                    // 檢核必填
                    if (temp.staff_id.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "參數驗證失敗：staff_id 為必填";
                        return returnData.JsonSerializationt();
                    }
                    List<object[]> list_objects = sql_staff.GetRowsByDefult(null, "staff_id", temp.staff_id);

                    if (list_objects.Count != 0)
                    {
                        list_delete.Add(list_objects[0]);
                    }
                }

                if (list_delete.Count > 0) sql_staff.DeleteExtra(null, list_delete);

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
        /// 查詢員工清單 (支援關鍵字搜尋、分頁與多表關聯)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 提供查詢人員基本資料與相關屬性（屬性設定、請假紀錄、群組成員、排班歷史）等綜合資訊。  
        /// 支援條件查詢、模糊搜尋、分頁與排序，可用於管理後台或智慧排班系統中。
        ///
        /// ## ⚙️ 功能說明  
        /// 1. 支援查詢條件：  
        ///    - GUID  
        ///    - staff_id  
        ///    - staff_name  
        ///    - role  
        ///    - keyword（模糊搜尋）  
        /// 2. 可控制分頁與排序欄位。  
        /// 3. 自動關聯：
        ///    - <c>StaffAttributesClass</c> 員工屬性  
        ///    - <c>LeaveRequestClass</c> 請假紀錄  
        ///    - <c>ShiftGroupMemberClass</c> 群組成員  
        ///    - <c>StaffScheduleHistoryClass</c> 排班歷史（限 status = '正常'）  
        /// 4. 每筆 <c>StaffClass</c> 會自動執行 <c>RecalculateCounts()</c> 更新統計欄位。  
        ///
        /// ## 🔍 查詢模式  
        /// - **全域模式**：未提供任何條件時，自動載入全部資料並支援 keyword 搜尋。  
        /// - **條件模式**：當提供任一查詢條件時，僅依條件搜尋。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "get_staffs",
        ///   "ValueAry": [
        ///     "staff_name=王小明",
        ///     "role=藥師",
        ///     "page=1",
        ///     "pageSize=50",
        ///     "sortBy=updated_at",
        ///     "sortOrder=desc"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 🔍 支援參數對照表  
        /// | 參數名稱 | 類型 | 必填 | 預設值 | 說明 |
        /// |------------|------|------|---------|------|
        /// | GUID | string | 否 |  | 指定單一員工 |
        /// | staff_id | string | 否 |  | 員工編號 |
        /// | staff_name | string | 否 |  | 員工姓名（支援模糊搜尋） |
        /// | role | string | 否 |  | 員工角色（如 藥師、助理） |
        /// | keyword | string | 否 |  | 全域搜尋字串，匹配 ID、姓名、角色 |
        /// | page | int | 否 | 1 | 查詢頁碼 |
        /// | pageSize | int | 否 | 50 | 每頁筆數 |
        /// | sortBy | string | 否 | updated_at | 排序欄位名稱 |
        /// | sortOrder | string | 否 | desc | 排序方向 asc / desc |
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_staffs",
        ///   "Result": "共取得(2)筆資料",
        ///   "Data": [
        ///     {
        ///       "GUID": "C9F1B3A5-AB21-4A09-A8AA-1FEE4A2B63F3",
        ///       "staff_id": "A001",
        ///       "staff_name": "王小明",
        ///       "role": "藥師",
        ///       "created_at": "2025/01/01 09:00:00",
        ///       "updated_at": "2025/01/10 14:33:00",
        ///       "staffAttributes": {
        ///         "department": "藥劑科",
        ///         "seniority": "5"
        ///       },
        ///       "leaveRequests": [
        ///         { "type": "事假", "start_date": "2025/01/05", "end_date": "2025/01/07" }
        ///       ],
        ///       "shiftGroupMembers": [
        ///         { "group_name": "門診早班", "order_index": "1" }
        ///       ],
        ///       "scheduleHistories": [
        ///         { "date": "2025/01/08", "shift_name": "早班", "status": "正常" }
        ///       ]
        ///     }
        ///   ],
        ///   "Extra": {
        ///     "TotalCount": "2",
        ///     "TotalPages": "1",
        ///     "CurrentPage": "1",
        ///     "PageSize": "50"
        ///   },
        ///   "TimeTaken": "102ms"
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_staffs",
        ///   "Result": "Exception : 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - 當未提供任何條件時，系統自動載入所有員工（可能筆數龐大）。  
        /// - 若使用 keyword 模式，會於記憶體中進行模糊比對，建議搭配分頁。  
        /// - 每筆員工資料會附帶關聯子資料，請注意回傳 JSON 內容大小。  
        /// - 若要僅取特定欄位，建議改用 SQL SELECT 欄位式查詢以提升效能。  
        /// - 此 API 可用於員工清單頁、篩選彈窗或報表匯出前端模組。
        /// </remarks>
        /// <param name="returnData">封裝 API 請求與回應內容，包含 ValueAry、Data、Method 等欄位</param>
        /// <returns>JSON 格式字串，包含員工資料清單、關聯資料及分頁資訊</returns>
        [HttpPost("get_staffs")]
        public string get_staffs([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_staffs";

            try
            {
                // 驗證必填
                //if (returnData.ValueAry == null || returnData.ValueAry.Count == 0)
                //{
                //    returnData.Code = -200;
                //    returnData.Result = "ValueAry 不能為空";
                //    return returnData.JsonSerializationt();
                //}
                var sql_staff = MethodClass.GetSQLControl<StaffClass>();

                (List<StaffClass> staffs, int totalCount, int totalPages, int pageSize , int currentPage) = GetStaffs(returnData.ValueAry);
                returnData.Code = 200;
                returnData.Result = $"共取得({staffs.Count})筆資料";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = staffs;
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
        /// 更新人員屬性 (StaffAttributesClass)
        /// </summary>
        /// <remarks>
        /// 此 API 用於針對既有人員 (Staff) 更新或新增屬性 (StaffAttributesClass)。  
        /// - 若 `staff_guid` 尚未有屬性紀錄，則建立新紀錄。  
        /// - 若已存在，則更新屬性內容。  
        ///
        /// <br/><br/>
        /// 🔑 **必要條件**
        /// - `ValueAry` 必須包含 Staff 查詢條件 (至少需能查到 staff_guid)。  
        /// - `Data` 必須為 `StaffAttributesClass` 格式。  
        ///
        /// <br/><br/>
        /// 📥 **Request JSON 範例**
        /// ```json
        /// {
        ///   "ServerName": "Main",
        ///   "ServerType": "SQL",
        ///   "Method": "update_attributes",
        ///   "ValueAry": [ "staff_id=S001" ],
        ///   "Data": {
        ///     "GUID": "",
        ///     "staff_guid": "",
        ///     "pregnant": "false",
        ///     "breastfeeding": "true",
        ///     "night_only": "false",
        ///     "no_night": "true",
        ///     "fixed_shift": "evening",
        ///     "max_consecutive_days": "10",
        ///     "preferred_days_off": "SAT,SUN",
        ///     "prefer_evening": "true",
        ///     "prefer_weekend": "true",
        ///     "exclude_shifts": "tpn,night",
        ///     "specialty": "oncology",
        ///     "support_role": "true"
        ///   }
        /// }
        /// ```
        ///
        /// <br/><br/>
        /// 📤 **Response JSON 範例 (成功)**
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "update_attributes",
        ///   "Result": "新增(1)筆資料,修改(0)筆資料",
        ///   "TimeTaken": "35ms",
        ///   "Data": {
        ///     "GUID": "A123-456B-789C",
        ///     "staff_id": "S001",
        ///     "staff_name": "王小明",
        ///     "role": "藥師",
        ///     "staffAttributes": {
        ///       "GUID": "B234-567C-890D",
        ///       "staff_guid": "A123-456B-789C",
        ///       "pregnant": "false",
        ///       "breastfeeding": "true",
        ///       "night_only": "false",
        ///       "no_night": "true",
        ///       "fixed_shift": "evening",
        ///       "max_consecutive_days": "10",
        ///       "preferred_days_off": "SAT,SUN",
        ///       "prefer_evening": "true",
        ///       "prefer_weekend": "true",
        ///       "exclude_shifts": "tpn,night",
        ///       "specialty": "oncology",
        ///       "support_role": "true"
        ///     }
        ///   }
        /// }
        /// ```
        ///
        /// <br/><br/>
        /// ❌ **錯誤回傳範例**
        /// - 找不到 Staff：
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "update_attributes",
        ///   "Result": "找不到指定的 staff_guid"
        /// }
        /// ```
        ///
        /// - Data 格式錯誤：
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "update_attributes",
        ///   "Result": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// - 系統錯誤：
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "update_attributes",
        ///   "Result": "Exception message"
        /// }
        /// ```
        /// </remarks>
        /// <param name="returnData">統一封裝的請求與回應物件</param>
        /// <returns>JSON 格式的回應字串</returns>
        [HttpPost("update_attributes")]
        public string update_attributes([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "update_attributes";

            try
            {

                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];


                var sql_staffs = MethodClass.GetSQLControl<StaffClass>();
                var sql_staffAttributes = MethodClass.GetSQLControl<StaffAttributesClass>();
                (List<StaffClass> staffClasses, int totalCount, int totalPages, int pageSize, int currentPage) = GetStaffs( returnData.ValueAry);


                StaffAttributesClass staffAttributes = returnData.Data.ObjToClass<StaffAttributesClass>();
                if (staffAttributes == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }
                if (staffClasses.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "找不到指定的 staff_guid";
                    return returnData.JsonSerializationt();
                }
                StaffClass staffClass = staffClasses[0];
                staffClass.staffAttributes = staffAttributes;
                staffAttributes.staff_guid = staffClass.GUID;
                var datas_add = new List<StaffAttributesClass>();
                var datas_update = new List<StaffAttributesClass>();
                List<object[]> objects = sql_staffAttributes.GetRowsByDefult(null, "staff_guid", staffAttributes.staff_guid);
                if (objects.Count == 0)
                {
                    staffAttributes.GUID = Guid.NewGuid().ToString();
                    datas_add.Add(staffAttributes);
                }
                else
                {
                    StaffAttributesClass oldAttributes = objects[0].SQLToClass<StaffAttributesClass>();
                    staffAttributes.GUID = oldAttributes.GUID;
                    datas_update.Add(staffAttributes);
                }
               

                if (datas_add.Count > 0) sql_staffAttributes.AddRows(null, datas_add.ClassToSQL<StaffAttributesClass>());
                if (datas_update.Count > 0) sql_staffAttributes.UpdateByDefulteExtra(null, datas_update.ClassToSQL<StaffAttributesClass>());

                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Result = $"新增({datas_add.Count})筆資料,修改({datas_update.Count})筆資料";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = staffClass;
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
        /// 匯出 Staff 人員資料為 Excel 檔
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於匯出目前資料庫內的 **Staff 人員清單**，並以 Excel 檔 (xlsx) 下載。  
        /// 匯出的檔案會自動移除系統欄位：`GUID`、`created_at`、`updated_at`。  
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "download_staff_excel",
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 📤 Response 範例 (成功回傳)  
        /// - 以檔案串流形式回傳，Header 包含檔案名稱與編碼資訊：  
        /// ```http
        /// Content-Disposition: attachment; filename="staff.xlsx"; filename*=UTF-8''staff.xlsx  
        /// Content-Type: application/octet-stream  
        /// ```
        /// - 下載的檔案內容為 Excel (XLSX) 格式，檔名固定為 `staff.xlsx`。  
        ///
        /// ## 📤 Response JSON 範例 (失敗回傳)  
        /// ❌ Data 不能為空  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "download_staff_excel",
        ///   "Result": "Data 不能為空"
        /// }
        /// ```
        ///
        /// ❌ staffClass 解析失敗  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "download_staff_excel",
        ///   "Result": "staffCalss 解析失敗"
        /// }
        /// ```
        ///
        /// ❌ 系統錯誤  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "download_staff_excel",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - 檔案格式固定為 Excel (xlsx)。  
        /// - 下載檔名固定為 `staff.xlsx`。  
        /// - Header 中包含 `Content-Disposition`，前端需能解析以正確下載檔案。  
        /// - 跨域下載時，需使用 `Access-Control-Expose-Headers` 以確保前端能讀取 Header。  
        /// </remarks>
        /// <param name="returnData">前端傳入的統一請求物件，需包含 Data 欄位 (可為空物件)</param>
        /// <returns>若成功，回傳 Excel 檔案串流；若失敗，回傳 JSON 格式錯誤訊息</returns>
        [HttpPost("download_staff_excel")]
        public IActionResult download_staff_excel([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "download_staff_excel";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return new JsonResult(returnData);
                }
                SQLControl sQLControl_staff = MethodClass.GetSQLControl<StaffClass>();
                var input = sQLControl_staff.GetAllRows(null).SQLToClass<StaffClass>();
                if (input == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "staffCalss 解析失敗";
                    return new JsonResult(returnData);
                }

                DataTable dataTable = sQLControl_staff.GetAllRows(null).ToDataTable<StaffClass>();

                dataTable.RemoveColumn("GUID");
                dataTable.RemoveColumn("created_at");
                dataTable.RemoveColumn("updated_at");
                dataTable.NPOI_GetBytes();



                byte[] bytes_pdf = dataTable.NPOI_GetBytes(Excel_Type.xlsx);

                Stream stream = new MemoryStream(bytes_pdf);


                string contentType = "application/octet-stream";

                // 原始檔名（可能有中文）
                string originalName = $"staff.xlsx";



                // UTF-8 檔名（正確編碼）
                string utf8FileName = Uri.EscapeDataString(originalName);

                // 設定 Content-Disposition header
                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{originalName}\"; filename*=UTF-8''{utf8FileName}");

                // 確保前端能讀到 header
                Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition, Content-Length, Content-Type");

                return File(stream, contentType);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return new JsonResult(returnData);
            }
        }

        /// <summary>
        /// 上傳並匯入 Staff 人員 Excel 檔
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於上傳 Excel 檔案 (XLSX)，解析內容並批次新增或更新 **Staff 人員資料**。  
        /// 上傳後的資料會自動轉換成 <c>StaffClass</c>，再呼叫 <c>add_and_update_staffs</c> 進行處理。  
        ///
        /// ## 📥 Request 範例  
        /// - 使用 <c>multipart/form-data</c> 格式上傳檔案，欄位名稱為 <c>file</c>。  
        /// ```http
        /// POST /upload_staff_excel
        /// Content-Type: multipart/form-data
        ///
        /// file: staff.xlsx
        /// ```
        ///
        /// ## 📥 Excel 檔案格式 (staff.xlsx)  
        /// - 第 1 列：欄位名稱 (略過)  
        /// - 第 2 列起：人員資料，欄位依序為：  
        ///   1. staff_id (人員編號，必填)  
        ///   2. staff_name (人員姓名，必填)  
        ///   3. role (角色/職務，可選)  
        ///
        /// 範例內容：  
        /// | staff_id | staff_name | role |  
        /// |----------|------------|------|  
        /// | S001     | 王大明     | 藥師 |  
        /// | S002     | 李小華     | 藥助 |  
        ///
        /// ## 📤 Response JSON 範例 (成功)  
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "add_and_update_staffs",
        ///   "Result": "新增(1)筆資料,修改(1)筆資料",
        ///   "TimeTaken": "0.032s",
        ///   "Data": []
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (失敗)  
        /// - 未選擇檔案  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "upload_staff_excel",
        ///   "Result": "未選擇檔案或檔案為空"
        /// }
        /// ```
        ///
        /// - Excel 內沒有有效資料  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "upload_staff_excel",
        ///   "Result": "Excel 內沒有有效資料"
        /// }
        /// ```
        ///
        /// - 系統錯誤  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "upload_staff_excel",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - 檔案格式必須為 Excel (XLSX)。  
        /// - 只會讀取第一個工作表 (Sheet0)。  
        /// - 第 1 列為標題列，會自動略過，從第 2 列開始解析資料。  
        /// - <c>staff_id</c> 與 <c>staff_name</c> 為必填欄位。  
        /// - 上傳後會自動呼叫 <c>add_and_update_staffs</c>，依 staff_id 判斷新增或更新。  
        /// </remarks>
        /// <param name="file">上傳的 Excel 檔案 (IFormFile)，檔名建議為 staff.xlsx</param>
        /// <returns>回傳標準化 JSON，包含 Code、Method、Result、Data 與 TimeTaken</returns>
        [HttpPost("upload_staff_excel")]
        public IActionResult upload_staff_excel(IFormFile file)
        {
            var returnData = new returnData();
            var timer = new MyTimerBasic();
            returnData.Method = "upload_staff_excel";

            try
            {
                if (file == null || file.Length == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "未選擇檔案或檔案為空";
                    return new JsonResult(returnData);
                }

                List<StaffClass> staffs = new List<StaffClass>();
                using (var stream = file.OpenReadStream())
                {
                    IWorkbook workbook = new XSSFWorkbook(stream);
                    ISheet sheet = workbook.GetSheetAt(0);
                    DataTable dataTable = sheet.SheetToDataTable();

                    for(int i = 0; i < dataTable.Rows.Count; i++)
                    {
                        StaffClass staff = new StaffClass
                        {
                            GUID = Guid.NewGuid().ToString(),
                            staff_id = dataTable.Rows[i]["staff_id"]?.ToString().Trim(),
                            staff_name = dataTable.Rows[i]["staff_name"]?.ToString().Trim(),
                            role = dataTable.Rows[i]["role"]?.ToString().Trim(),
                            staff_simple_name = dataTable.Rows[i]["staff_simple_name"]?.ToString().Trim(),
                            created_at = DateTime.Now.ToDateTimeString_6(),
                            updated_at = DateTime.Now.ToDateTimeString_6()
                        };
                        if (!string.IsNullOrEmpty(staff.staff_id) && !string.IsNullOrEmpty(staff.staff_name))
                        {
                            staffs.Add(staff);
                        }
                    }
                    //// 假設第一列是欄位名稱，從第二列開始讀
                    //for (int row = 1; row <= sheet.LastRowNum; row++)
                    //{
                    //    IRow currentRow = sheet.GetRow(row);
                    //    if (currentRow == null) continue;

                    //    StaffClass staff = new StaffClass
                    //    {
                    //        GUID = Guid.NewGuid().ToString(),
                    //        staff_id = currentRow.GetCell(0)?.ToString().Trim(),
                    //        staff_name = currentRow.GetCell(1)?.ToString().Trim(),
                    //        role = currentRow.GetCell(2)?.ToString().Trim(),
                    //        created_at = DateTime.Now.ToDateTimeString_6(),
                    //        updated_at = DateTime.Now.ToDateTimeString_6()
                    //    };

                    //    if (!string.IsNullOrEmpty(staff.staff_id) && !string.IsNullOrEmpty(staff.staff_name))
                    //    {
                    //        staffs.Add(staff);
                    //    }
                    //}
                }
              
                if (staffs.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Excel 內沒有有效資料";
                    return new JsonResult(returnData);
                }
                returnData returnData_add_update = new returnData();
                returnData_add_update.Data = staffs;
                string json_add_update = add_and_update_staffs(returnData_add_update);
                returnData_add_update = json_add_update.JsonDeserializet<returnData>();
                return StatusCode(StatusCodes.Status200OK, returnData_add_update);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"Exception: {ex.Message}";
                return new JsonResult(returnData);
            }
        }


        /// <summary>
        /// 查詢單一人員資料 (依 staff_guid)
        /// </summary>
        /// <param name="staffGuid">人員 GUID</param>
        /// <returns>回傳符合條件的人員 (若無則為 null)</returns>
        public static StaffClass GetStaffByGuid(string staffGuid)
        {
            if (string.IsNullOrWhiteSpace(staffGuid))
                return null;

            // 呼叫多筆版本，但只傳入一個 GUID
            var result = GetStaffsByGuids(new List<string> { staffGuid });

            return result.FirstOrDefault();
        }
        /// <summary>
        /// 同步：依多筆 GUID 查詢人員資料 (用非同步多載包裝)
        /// </summary>
        public static List<StaffClass> GetStaffsByGuids(List<string> guids)
        {
            return GetStaffsByGuidsAsync(guids).GetAwaiter().GetResult();
        }
        /// <summary>
        /// 同步版本：查詢人員資料 (支援全域搜尋/條件搜尋)
        /// </summary>
        public static (List<StaffClass> staffClasses, int totalCount, int totalPages, int pageSize, int currentPage) GetStaffs(List<string> ValueAry)
        {
            return GetStaffsCoreAsync(ValueAry, false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 取得所有人員資料 (包含屬性) - 不帶參數
        /// </summary>
        public static List<StaffClass> GetAllStaffs(bool staffAttributes = true)
        {
            // 直接呼叫非同步方法並阻塞取結果
            return GetAllStaffsAsync(staffAttributes).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 非同步：取得所有人員資料 (包含屬性)
        /// </summary>
        public static async Task<List<StaffClass>> GetAllStaffsAsync(bool staffAttributes = true)
        {
            // 撈取所有資料表
            var (allStaffs, allAttrs, _, _, allstaffScheduleHistory) = await LoadAllTablesAsync();

            // 關聯 Staff 與 StaffAttributes
            foreach (var staff in allStaffs)
            {
                if(staffAttributes) staff.staffAttributes = allAttrs.FirstOrDefault(a => a.staff_guid == staff.GUID);
                staff.scheduleHistories = allstaffScheduleHistory.Where(g => g.staff_guid == staff.GUID).ToList();

            }

            return allStaffs;
        }
        /// <summary>
        /// 核心方法：載入所有表，並回傳整合資料
        /// </summary>
        private static async Task<(List<StaffClass> staffs, List<StaffAttributesClass> attrs, List<LeaveRequestClass> leaves, List<ShiftGroupMemberClass> groupMembers, List<StaffScheduleHistoryClass> staffScheduleHistory)> LoadAllTablesAsync()
        {
            var sql_staff = MethodClass.GetSQLControl<StaffClass>();
            var sql_staffAttr = MethodClass.GetSQLControl<StaffAttributesClass>();
            var sql_leaveRequest = MethodClass.GetSQLControl<LeaveRequestClass>();
            var sql_groupMember = MethodClass.GetSQLControl<ShiftGroupMemberClass>();
            var sql_scheduleHistory = MethodClass.GetSQLControl<StaffScheduleHistoryClass>();

            var staffTask = sql_staff.WtrteCommandAndExecuteReaderAsync($"SELECT * FROM {sql_staff.Database}.{sql_staff.TableName}");
            var attrTask = sql_staffAttr.WtrteCommandAndExecuteReaderAsync($"SELECT * FROM {sql_staffAttr.Database}.{sql_staffAttr.TableName}");
            var leaveTask = sql_leaveRequest.WtrteCommandAndExecuteReaderAsync($"SELECT * FROM {sql_leaveRequest.Database}.{sql_leaveRequest.TableName}");
            var groupTask = sql_groupMember.WtrteCommandAndExecuteReaderAsync($"SELECT * FROM {sql_groupMember.Database}.{sql_groupMember.TableName}");
            var scheduleHistoryTask = sql_scheduleHistory.WtrteCommandAndExecuteReaderAsync($"SELECT * FROM {sql_scheduleHistory.Database}.{sql_scheduleHistory.TableName} WHERE status = '正常'");

            await Task.WhenAll(staffTask, attrTask, leaveTask, groupTask);

            var allStaffs = (staffTask.Result).DataTableToRowList().SQLToClass<StaffClass>() ?? new List<StaffClass>();
            var allAttrs = (attrTask.Result).DataTableToRowList().SQLToClass<StaffAttributesClass>() ?? new List<StaffAttributesClass>();
            var allLeaves = (leaveTask.Result).DataTableToRowList().SQLToClass<LeaveRequestClass>() ?? new List<LeaveRequestClass>();
            var allGroupMembers = (groupTask.Result).DataTableToRowList().SQLToClass<ShiftGroupMemberClass>() ?? new List<ShiftGroupMemberClass>();
            var allscheduleHistorys = (scheduleHistoryTask.Result).DataTableToRowList().SQLToClass<StaffScheduleHistoryClass>() ?? new List<StaffScheduleHistoryClass>();

            return (allStaffs, allAttrs, allLeaves, allGroupMembers, allscheduleHistorys);
        }
   
        /// <summary>
        /// 非同步：依多筆 GUID 查詢人員資料
        /// </summary>
        public static async Task<List<StaffClass>> GetStaffsByGuidsAsync(List<string> guids)
        {
            if (guids == null || guids.Count == 0) return new List<StaffClass>();

            var (allStaffs, allAttrs, allLeaves, allGroupMembers, allscheduleHistorys) = await LoadAllTablesAsync();

            var staffs = allStaffs.Where(s => guids.Contains(s.GUID)).ToList();

            foreach (var staff in staffs)
            {
                staff.staffAttributes = allAttrs.FirstOrDefault(a => a.staff_guid == staff.GUID);
                staff.leaveRequests = allLeaves.Where(l => l.staff_guid == staff.GUID).ToList();
                staff.shiftGroupMembers = allGroupMembers.Where(g => g.staff_guid == staff.GUID).ToList();
                staff.scheduleHistories = allscheduleHistorys.Where(g => g.staff_guid == staff.GUID).ToList();

                staff.RecalculateCounts();

            }

            return staffs;
        }
       

        /// <summary>
        /// 查詢人員資料 (支援全域搜尋/條件搜尋)
        /// 非同步核心邏輯
        /// </summary>
        private static async Task<(List<StaffClass> staffClasses, int totalCount, int totalPages, int pageSize, int currentPage)> GetStaffsCoreAsync(List<string> ValueAry, bool isAsync)
        {
            string Esc(string s) => (s ?? "").Replace("'", "''");
            var sql_staff = MethodClass.GetSQLControl<StaffClass>();
            var sql_staffAttr = MethodClass.GetSQLControl<StaffAttributesClass>();
            var sql_leaveRequest = MethodClass.GetSQLControl<LeaveRequestClass>();
            var sql_groupMember = MethodClass.GetSQLControl<ShiftGroupMemberClass>();
            var sql_scheduleHistory = MethodClass.GetSQLControl<StaffScheduleHistoryClass>();

            // 解析參數
            string GetVal(string key) =>
                ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                ?.Split('=')[1];

            string GUID = GetVal("GUID") ?? "";
            string staff_id = GetVal("staff_id") ?? "";
            string staff_name = GetVal("staff_name") ?? "";
            string role = GetVal("role") ?? "";
            string keyword = GetVal("keyword") ?? ""; // 🔍 支援全域搜尋

            int page = (GetVal("page") ?? "1").StringToInt32();
            int pageSize = (GetVal("pageSize") ?? "200").StringToInt32();
            string sortBy = GetVal("sortBy") ?? "updated_at";
            string sortOrder = (GetVal("sortOrder") ?? "desc").ToUpper();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 200;

            List<StaffClass> staffs = new List<StaffClass>();
            int totalCount = 0, totalPages = 0;

            // === 1. 判斷模式 ===
            bool isGlobalSearch = keyword.StringIsEmpty() && GUID.StringIsEmpty() && staff_id.StringIsEmpty() && staff_name.StringIsEmpty() && role.StringIsEmpty();

            if (isGlobalSearch)
            {
                // === 全域搜尋模式 ===
                string querySql = $@"
            SELECT * FROM {sql_staff.Database}.{sql_staff.TableName}
            ORDER BY {sortBy} {sortOrder}";

                var dtStaffs = isAsync
                    ? await sql_staff.WtrteCommandAndExecuteReaderAsync(querySql)
                    : sql_staff.WtrteCommandAndExecuteReader(querySql);

                var allStaffs = dtStaffs.DataTableToRowList().SQLToClass<StaffClass>() ?? new List<StaffClass>();

                // 撈出所有屬性、請假、群組
                var dtAttr = isAsync
                    ? await sql_staffAttr.WtrteCommandAndExecuteReaderAsync($"SELECT * FROM {sql_staffAttr.Database}.{sql_staffAttr.TableName}")
                    : sql_staffAttr.WtrteCommandAndExecuteReader($"SELECT * FROM {sql_staffAttr.Database}.{sql_staffAttr.TableName}");
                var allAttrs = dtAttr.DataTableToRowList().SQLToClass<StaffAttributesClass>() ?? new List<StaffAttributesClass>();

                var dtLeaves = isAsync
                    ? await sql_leaveRequest.WtrteCommandAndExecuteReaderAsync($"SELECT * FROM {sql_leaveRequest.Database}.{sql_leaveRequest.TableName}")
                    : sql_leaveRequest.WtrteCommandAndExecuteReader($"SELECT * FROM {sql_leaveRequest.Database}.{sql_leaveRequest.TableName}");
                var allLeaves = dtLeaves.DataTableToRowList().SQLToClass<LeaveRequestClass>() ?? new List<LeaveRequestClass>();

                var dtGroupMembers = isAsync
                    ? await sql_groupMember.WtrteCommandAndExecuteReaderAsync($"SELECT * FROM {sql_groupMember.Database}.{sql_groupMember.TableName}")
                    : sql_groupMember.WtrteCommandAndExecuteReader($"SELECT * FROM {sql_groupMember.Database}.{sql_groupMember.TableName}");
                var allGroupMembers = dtGroupMembers.DataTableToRowList().SQLToClass<ShiftGroupMemberClass>() ?? new List<ShiftGroupMemberClass>();


                var dtscheduleHistory = isAsync
                 ? await sql_scheduleHistory.WtrteCommandAndExecuteReaderAsync($"SELECT * FROM {sql_scheduleHistory.Database}.{sql_scheduleHistory.TableName} WHERE status = '正常'" )
                 : sql_scheduleHistory.WtrteCommandAndExecuteReader($"SELECT * FROM {sql_scheduleHistory.Database}.{sql_scheduleHistory.TableName} WHERE status = '正常'");
                var allscheduleHistory = dtscheduleHistory.DataTableToRowList().SQLToClass<StaffScheduleHistoryClass>() ?? new List<StaffScheduleHistoryClass>();

                // 關聯
                foreach (var staff in allStaffs)
                {
                    staff.staffAttributes = allAttrs.FirstOrDefault(x => x.staff_guid == staff.GUID);
                    staff.leaveRequests = allLeaves.Where(x => x.staff_guid == staff.GUID).ToList();
                    staff.shiftGroupMembers = allGroupMembers.Where(x => x.staff_guid == staff.GUID).ToList();
                    staff.scheduleHistories = allscheduleHistory.Where(x => x.staff_guid == staff.GUID).ToList();
                    staff.RecalculateCounts();

                }

                // 🔍 關鍵字搜尋 (記憶體篩選)
                staffs = allStaffs
                    .Where(s =>
                        (s.staff_id?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (s.staff_name?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (s.role?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();

                totalCount = staffs.Count;
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // 分頁
                staffs = staffs.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            }
            else
            {
                // === 條件搜尋模式 ===
                string where = $" WHERE 1=1 ";
                if (!GUID.StringIsEmpty()) where += $" AND GUID = '{Esc(GUID)}' ";
                if (!staff_id.StringIsEmpty()) where += $" AND staff_id = '{Esc(staff_id)}' ";
                if (!staff_name.StringIsEmpty()) where += $" AND staff_name LIKE '%{Esc(staff_name)}%' ";
                if (!role.StringIsEmpty()) where += $" AND role = '{Esc(role)}' ";
                if (!keyword.StringIsEmpty())
                    where += $" AND (staff_id LIKE '%{Esc(keyword)}%' OR staff_name LIKE '%{Esc(keyword)}%') ";

                string orderBy = $" ORDER BY {sortBy} {sortOrder} ";
                int offset = (page - 1) * pageSize;

                // 查詢總數
                string countSql = $"SELECT COUNT(*) AS cnt FROM {sql_staff.Database}.{sql_staff.TableName} {where}";
                var dtCnt = isAsync
                    ? await sql_staff.WtrteCommandAndExecuteReaderAsync(countSql)
                    : sql_staff.WtrteCommandAndExecuteReader(countSql);
                totalCount = dtCnt.Rows[0]["cnt"].ToString().StringToInt32();
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                // 查詢 Staff
                string querySql = $@"
            SELECT * FROM {sql_staff.Database}.{sql_staff.TableName}
            {where}
            {orderBy}
            LIMIT {offset}, {pageSize}";

                var dt = isAsync
                    ? await sql_staff.WtrteCommandAndExecuteReaderAsync(querySql)
                    : sql_staff.WtrteCommandAndExecuteReader(querySql);
                staffs = dt.DataTableToRowList().SQLToClass<StaffClass>() ?? new List<StaffClass>();

                if (staffs.Count > 0)
                {
                    var guids = staffs.Select(s => $"'{Esc(s.GUID)}'").ToList();

                    // 屬性
                    string attrSql = $@"SELECT * FROM {sql_staffAttr.Database}.{sql_staffAttr.TableName}
                                WHERE staff_guid IN ({string.Join(",", guids)})";
                    var dtAttr = isAsync
                        ? await sql_staffAttr.WtrteCommandAndExecuteReaderAsync(attrSql)
                        : sql_staffAttr.WtrteCommandAndExecuteReader(attrSql);
                    var attrs = dtAttr.DataTableToRowList().SQLToClass<StaffAttributesClass>() ?? new List<StaffAttributesClass>();

                    // 請假
                    string leaveSql = $@"SELECT * FROM {sql_leaveRequest.Database}.{sql_leaveRequest.TableName}
                                 WHERE staff_guid IN ({string.Join(",", guids)})";
                    var dtLeaves = isAsync
                        ? await sql_leaveRequest.WtrteCommandAndExecuteReaderAsync(leaveSql)
                        : sql_leaveRequest.WtrteCommandAndExecuteReader(leaveSql);
                    var leaves = dtLeaves.DataTableToRowList().SQLToClass<LeaveRequestClass>() ?? new List<LeaveRequestClass>();

                    // 群組成員
                    string groupSql = $@"SELECT * FROM {sql_groupMember.Database}.{sql_groupMember.TableName}
                                 WHERE staff_guid IN ({string.Join(",", guids)})";
                    var dtGroups = isAsync
                        ? await sql_groupMember.WtrteCommandAndExecuteReaderAsync(groupSql)
                        : sql_groupMember.WtrteCommandAndExecuteReader(groupSql);
                    var groups = dtGroups.DataTableToRowList().SQLToClass<ShiftGroupMemberClass>() ?? new List<ShiftGroupMemberClass>();


                    // 歷史排班
                    string scheduleHistorySql = $@"SELECT * FROM {sql_scheduleHistory.Database}.{sql_scheduleHistory.TableName}
                                 WHERE staff_guid IN ({string.Join(",", guids)}) and status = '正常'";
                    var dtscheduleHistorys = isAsync
                        ? await sql_scheduleHistory.WtrteCommandAndExecuteReaderAsync(scheduleHistorySql)
                        : sql_scheduleHistory.WtrteCommandAndExecuteReader(scheduleHistorySql);
                    var scheduleHistorys = dtscheduleHistorys.DataTableToRowList().SQLToClass<StaffScheduleHistoryClass>() ?? new List<StaffScheduleHistoryClass>();


                    foreach (var staff in staffs)
                    {
                        staff.staffAttributes = attrs.FirstOrDefault(a => a.staff_guid == staff.GUID);
                        staff.leaveRequests = leaves.Where(l => l.staff_guid == staff.GUID).ToList();
                        staff.shiftGroupMembers = groups.Where(g => g.staff_guid == staff.GUID).ToList();
                        staff.scheduleHistories = scheduleHistorys.Where(g => g.staff_guid == staff.GUID).ToList();
                        staff.RecalculateCounts();

                    }
                }
            }

            return (staffs, totalCount, totalPages, pageSize, page);
        }

     
        /// <summary>
        /// 非同步版本：查詢人員資料 (支援全域搜尋/條件搜尋)
        /// </summary>
        public static Task<(List<StaffClass> staffClasses, int totalCount, int totalPages, int pageSize, int currentPage)> GetStaffsAsync(List<string> ValueAry)
        {
            return GetStaffsCoreAsync(ValueAry, true);
        }

    }
}
