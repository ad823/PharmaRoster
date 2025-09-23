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
    public class shiftGroup :ControllerBase
    {

        /// <summary>
        /// 初始化 ShiftGroup 與 ShiftGroupMember 資料表
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於初始化與建立 **排班群組 (ShiftGroup)** 及其 **群組成員 (ShiftGroupMember)** 資料表。  
        /// 通常在系統啟動或第一次執行時呼叫，確保相關資料表存在並能正確進行資料操作。  
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
        ///   "Result": "初始化 shiftGroup 資料表完成",
        ///   "Data": [
        ///     {
        ///       "TableName": "shift_groups",
        ///       "Columns": [
        ///         { "Name": "GUID", "Type": "VARCHAR(50)", "IsPrimary": true },
        ///         { "Name": "group_name", "Type": "VARCHAR(100)" },
        ///         { "Name": "created_at", "Type": "DATETIME" }
        ///       ]
        ///     },
        ///     {
        ///       "TableName": "shift_group_members",
        ///       "Columns": [
        ///         { "Name": "GUID", "Type": "VARCHAR(50)", "IsPrimary": true },
        ///         { "Name": "group_guid", "Type": "VARCHAR(50)" },
        ///         { "Name": "staff_guid", "Type": "VARCHAR(50)" },
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
        /// - 系統會自動檢查並建立 <c>shift_groups</c> 與 <c>shift_group_members</c> 資料表。  
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
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<ShiftGroupClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<ShiftGroupMemberClass>());

                returnData.Code = 200;
                returnData.Data = tables;
                returnData.Result = "初始化 shiftGroup 資料表完成";
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
        /// 查詢排班群組 (ShiftGroup) 及其成員資料
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於查詢排班群組 (ShiftGroup)，並同時回傳每個群組的成員 (ShiftGroupMember)。  
        /// 每位成員會自動補齊其對應的人員資料 (StaffClass)，填入 <c>staff_info</c> 屬性。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "get_shiftGroups",
        ///   "ValueAry": [
        ///     "group_name=小夜固定班",
        ///     "page=1",
        ///     "pageSize=20",
        ///     "sortBy=created_at",
        ///     "sortOrder=desc"
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_shiftGroups",
        ///   "Result": "共取得(1)筆資料",
        ///   "TimeTaken": "35ms",
        ///   "Data": [
        ///     {
        ///       "GUID": "G1234567-89AB-CDEF-0123-456789ABCDEF",
        ///       "group_name": "小夜固定班",
        ///       "description": "此群組為小夜班固定成員，輪替範圍僅限內部。",
        ///       "last_index": "2",
        ///       "work_shifts": "[\"16:00-22:00\",\"22:00-00:00\"]",
        ///       "updated_at": "2025-09-20 10:30:00",
        ///       "created_at": "2025-09-10 09:00:00",
        ///       "Members": [
        ///         {
        ///           "GUID": "M1111111-2222-3333-4444-555555555555",
        ///           "group_guid": "G1234567-89AB-CDEF-0123-456789ABCDEF",
        ///           "staff_guid": "SAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE",
        ///           "created_at": "2025-09-10 09:15:00",
        ///           "staff_info": {
        ///             "GUID": "SAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE",
        ///             "staff_id": "S001",
        ///             "staff_name": "王大明",
        ///             "role": "藥師",
        ///             "created_at": "2025-09-01 08:00:00",
        ///             "updated_at": "2025-09-15 12:00:00"
        ///           }
        ///         }
        ///       ]
        ///     }
        ///   ],
        ///   "TotalCount": "1",
        ///   "TotalPages": "1",
        ///   "CurrentPage": "1",
        ///   "PageSize": "20"
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少必要參數：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_shiftGroups",
        ///   "Result": "ValueAry 不能為空"
        /// }
        /// ```
        ///
        /// - 系統例外：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_shiftGroups",
        ///   "Result": "Exception message"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - 查詢條件 `ValueAry` 至少要提供一個條件 (例如 GUID 或 group_name)。  
        /// - 回傳的成員 (Members) 中，每筆資料會自動補齊 `staff_info`。  
        /// - 分頁相關資訊 (TotalCount, TotalPages, CurrentPage, PageSize) 會以 Extra 屬性動態展開在 JSON 最外層。  
        /// </remarks>
        /// <param name="returnData">前端傳入的請求物件，需包含 ValueAry 查詢條件</param>
        /// <returns>JSON 格式字串，包含群組及其成員資料</returns>
        [HttpPost("get_shiftGroups")]
        public string get_shiftGroups([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_shiftGroups";

            try
            {
                // 驗證必填
                if (returnData.ValueAry == null || returnData.ValueAry.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "ValueAry 不能為空";
                    return returnData.JsonSerializationt();
                }

                // 呼叫靜態函式，直接包含 Members
                (List<ShiftGroupClass> shiftGroups, int totalCount, int totalPages, int pageSize, int currentPage)
                    = GetShiftGroups(returnData.ValueAry);

                // 成功回傳
                returnData.Code = 200;
                returnData.Result = $"共取得({shiftGroups.Count})筆資料";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = shiftGroups;
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
        /// 新增或更新 ShiftGroup 群組資料
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於批次新增或更新 **ShiftGroup 群組資料**。  
        /// 系統會依據 <c>GUID</c> 進行檢核：  
        /// - 若資料庫不存在該 <c>GUID</c> → 新增 (系統自動產生新 GUID)。  
        /// - 若資料庫已存在該 <c>GUID</c> → 更新 (保留既有 GUID，刷新欄位內容)。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "add_and_update_shiftGroups",
        ///   "Data": [
        ///     {
        ///       "GUID": "",
        ///       "group_name": "小夜固定班",
        ///       "description": "此群組為固定小夜人員",
        ///       "work_shifts": "[\"22:00-02:00\"]"
        ///     },
        ///     {
        ///       "GUID": "B8A2E3F1-1234-4F2A-B3A7-0DBB76543210",
        ///       "group_name": "大夜循環",
        ///       "description": "此群組為大夜輪值班",
        ///       "work_shifts": "[\"00:00-08:00\"]"
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
        ///   "Method": "add_and_update_shiftGroups",
        ///   "Result": "新增(1)筆資料,修改(1)筆資料",
        ///   "TimeTaken": "0.028s",
        ///   "Data": []
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (缺少必要欄位)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_shiftGroups",
        ///   "Result": "參數驗證失敗：group_name 為必填"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (傳入格式錯誤)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_shiftGroups",
        ///   "Result": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (例外狀況)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_shiftGroups",
        ///   "Result": "Exception message"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - `GUID` 新增時可傳空字串，系統會自動產生。  
        /// - `group_name` 為必填欄位。  
        /// - `work_shifts` 須以 JSON 陣列字串存放（如 ["08:00-12:00","13:00-17:00"]）。  
        /// - 傳入多筆資料時，系統會逐筆判斷新增或更新。  
        /// </remarks>
        /// <param name="returnData">
        /// 前端傳入的 Request，包含欲新增或更新的 Data 陣列 (ShiftGroupClass)
        /// </param>
        /// <returns>
        /// 回傳標準化 JSON，包含 Code、Method、Result、TimeTaken 與 Data
        /// </returns>
        [HttpPost("add_and_update_shiftGroups")]
        public string add_and_update_shiftGroups([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "add_and_update_shiftGroups";

            try
            {
                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<ShiftGroupClass> input = returnData.Data.ObjToClass<List<ShiftGroupClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var sql_shiftGroup = MethodClass.GetSQLControl<ShiftGroupClass>();
                var output = new List<ShiftGroupClass>();
                var datas_add = new List<ShiftGroupClass>();
                var datas_update = new List<ShiftGroupClass>();

                // === 2. 寫入流程 ===
                foreach (var temp in input)
                {
                    // 檢核必填
                    if (temp.group_name.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "參數驗證失敗：group_name 為必填";
                        return returnData.JsonSerializationt();
                    }
                  

                    List<object[]> list_objects = sql_shiftGroup.GetRowsByDefult(null, "GUID", temp.GUID);

                    if (list_objects.Count == 0)
                    {
                        temp.GUID = Guid.NewGuid().ToString();
                        temp.created_at = DateTime.Now.ToDateTimeString_6();
                        temp.last_index = "0";
                        datas_add.Add(temp);
                    }
                    else
                    {
                        ShiftGroupClass shiftGroup = list_objects[0].SQLToClass<ShiftGroupClass>();
                        temp.GUID = shiftGroup.GUID;
                        temp.updated_at = DateTime.Now.ToDateTimeString_6();
                        datas_update.Add(temp);
                    }
                }

                if (datas_add.Count > 0) sql_shiftGroup.AddRows(null, datas_add.ClassToSQL<ShiftGroupClass>());
                if (datas_update.Count > 0) sql_shiftGroup.UpdateByDefulteExtra(null, datas_update.ClassToSQL<ShiftGroupClass>());

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
        /// 刪除 ShiftGroup 群組資料
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於批次刪除 **ShiftGroup 群組資料**。  
        /// 系統會依據 <c>GUID</c> 檢核是否存在於資料庫：  
        /// - 若存在 → 執行刪除。  
        /// - 若不存在 → 略過不處理。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "delete_shiftGroups",
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
        ///   "Method": "delete_shiftGroups",
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
        ///   "Method": "delete_shiftGroups",
        ///   "Result": "參數驗證失敗：guid 為必填"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (傳入格式錯誤)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_shiftGroups",
        ///   "Result": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (例外狀況)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_shiftGroups",
        ///   "Result": "Exception message"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - `GUID` 為必填欄位，必須指定要刪除的群組。  
        /// - 傳入多筆資料時，系統會逐筆檢核並刪除。  
        /// - 若 GUID 不存在於資料庫，該筆資料將被忽略，不會報錯。  
        /// </remarks>
        /// <param name="returnData">前端傳入的 Request，包含欲刪除的 Data 陣列 (需含 GUID)</param>
        /// <returns>回傳標準化 JSON，包含 Code、Method、Result、TimeTaken 與 Data</returns>
        [HttpPost("delete_shiftGroups")]
        public string delete_shiftGroups([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_shiftGroups";

            try
            {
                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<ShiftGroupClass> input = returnData.Data.ObjToClass<List<ShiftGroupClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var sql_shiftGroup = MethodClass.GetSQLControl<ShiftGroupClass>();
                var output = new List<ShiftGroupClass>();
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
                    List<object[]> list_objects = sql_shiftGroup.GetRowsByDefult(null, "GUID", temp.GUID);

                    if (list_objects.Count != 0)
                    {
                        list_delete.Add(list_objects[0]);
                    }
                }

                if (list_delete.Count > 0) sql_shiftGroup.DeleteExtra(null, list_delete);
                output = list_delete.SQLToClass<ShiftGroupClass>();
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
        /// 新增或更新 ShiftGroup 成員資料
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於批次新增或更新 **ShiftGroup 成員 (ShiftGroupMember)** 資料。  
        /// 系統會依據 <c>group_guid</c> 與 <c>GUID</c> 進行檢核：  
        /// - 若 <c>GUID</c> 不存在 → 新增 (系統自動產生新 GUID)。  
        /// - 若 <c>GUID</c> 已存在 → 更新 (保留既有 GUID，刷新欄位內容)。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "add_and_update_shiftGroupMembers",
        ///   "ValueAry": [ "group_guid=B8A2E3F1-1234-4F2A-B3A7-0DBB76543210" ],
        ///   "Data": [
        ///     {
        ///       "GUID": "",
        ///       "staff_guid": "STAFF-001",
        ///       "created_at": "2025-09-20 09:00:00"
        ///     },
        ///     {
        ///       "GUID": "E2D4F9C1-5678-4AC3-9B1A-0DBB12345678",
        ///       "staff_guid": "STAFF-002",
        ///       "created_at": "2025-09-20 09:10:00"
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
        ///   "Method": "add_and_update_shiftGroupMembers",
        ///   "Result": "新增(1)筆資料,修改(1)筆資料",
        ///   "TimeTaken": "0.031s",
        ///   "Data": []
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (缺少必要參數)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_shiftGroupMembers",
        ///   "Result": "參數驗證失敗：group_guid 為必填"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (群組不存在)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_shiftGroupMembers",
        ///   "Result": "參數驗證失敗：group_guid 不存在"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (staff_guid 缺少)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_shiftGroupMembers",
        ///   "Result": "參數驗證失敗：staff_guid 為必填"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (傳入格式錯誤)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_shiftGroupMembers",
        ///   "Result": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (例外狀況)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_shiftGroupMembers",
        ///   "Result": "Exception message"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - `group_guid` 為必填，必須透過 ValueAry 傳入。  
        /// - `staff_guid` 為必填欄位，必須為已存在的人員。  
        /// - `GUID` 新增時可傳空字串，系統會自動產生。  
        /// - 傳入多筆資料時，系統會逐筆檢核新增或更新。  
        /// - 若 `staff_guid` 不存在於資料庫，該筆資料會被略過，不會報錯。  
        /// </remarks>
        /// <param name="returnData">前端傳入的 Request，包含 ValueAry (group_guid) 與 Data 陣列 (ShiftGroupMemberClass)</param>
        /// <returns>回傳標準化 JSON，包含 Code、Method、Result、TimeTaken 與 Data</returns>
        [HttpPost("add_and_update_shiftGroupMembers")]
        public string add_and_update_shiftGroupMembers([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "add_and_update_shiftGroupMembers";

            try
            {
                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<ShiftGroupMemberClass> input = returnData.Data.ObjToClass<List<ShiftGroupMemberClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }
                // 解析參數
                string GetVal(string key) =>
                    returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string group_guid = GetVal("group_guid") ?? "";
                if (group_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：group_guid 為必填";
                    return returnData.JsonSerializationt();
                }
                var sql_shiftGroup = MethodClass.GetSQLControl<ShiftGroupClass>();
                var sql_shiftGroupMemberClass = MethodClass.GetSQLControl<ShiftGroupMemberClass>();
                var output = new List<ShiftGroupMemberClass>();
                var datas_add = new List<ShiftGroupMemberClass>();
                var datas_update = new List<ShiftGroupMemberClass>();

                List<object[]> objects_group = sql_shiftGroup.GetRowsByDefult(null, "GUID", group_guid);    
              
                if(objects_group.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "參數驗證失敗：group_guid 不存在";
                    return returnData.JsonSerializationt();
                }
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

                    StaffClass staffClass = staff.GetStaffByGuid(temp.staff_guid);
                    if (staffClass != null)
                    {
                        temp.group_guid = group_guid;
                    }
                    else
                    {
                        continue;
                    }
                    List<object[]> list_objects = sql_shiftGroupMemberClass.GetRowsByDefult(null, new string[] { "group_guid", "staff_guid" },new string[] { temp.group_guid, temp.staff_guid });

                    if (list_objects.Count == 0)
                    {
                        temp.GUID = Guid.NewGuid().ToString();
                        temp.created_at = DateTime.Now.ToDateTimeString_6();
                        datas_add.Add(temp);
                    }
                    else
                    {
                        ShiftGroupClass shiftGroup = list_objects[0].SQLToClass<ShiftGroupClass>();
                        temp.GUID = shiftGroup.GUID;
                        temp.created_at = DateTime.Now.ToDateTimeString_6();
                        datas_update.Add(temp);
                    }
                }

                if (datas_add.Count > 0) sql_shiftGroupMemberClass.AddRows(null, datas_add.ClassToSQL<ShiftGroupMemberClass>());
                if (datas_update.Count > 0) sql_shiftGroupMemberClass.UpdateByDefulteExtra(null, datas_update.ClassToSQL<ShiftGroupMemberClass>());

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
        /// 刪除 ShiftGroup 成員資料
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於批次刪除 **ShiftGroup 成員 (ShiftGroupMember)** 資料。  
        /// 系統會依據 <c>GUID</c> 檢核是否存在於資料庫：  
        /// - 若存在 → 執行刪除。  
        /// - 若不存在 → 略過不處理。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "delete_shiftGroupMembers",
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
        ///   "Method": "delete_shiftGroupMembers",
        ///   "Result": "刪除(2)筆資料",
        ///   "Data": [
        ///     {
        ///       "GUID": "C5B2E3D1-9F21-4F2A-B3A7-0DBB12345678",
        ///       "group_guid": "B8A2E3F1-1234-4F2A-B3A7-0DBB76543210",
        ///       "staff_guid": "STAFF-001",
        ///       "created_at": "2025-09-20 09:00:00"
        ///     },
        ///     {
        ///       "GUID": "7A6D12A9-5B10-43E2-B7E1-ABCD98765432",
        ///       "group_guid": "B8A2E3F1-1234-4F2A-B3A7-0DBB76543210",
        ///       "staff_guid": "STAFF-002",
        ///       "created_at": "2025-09-20 09:05:00"
        ///     }
        ///   ],
        ///   "TimeTaken": "0.004s"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (缺少必要欄位)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_shiftGroupMembers",
        ///   "Result": "參數驗證失敗：guid 為必填"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (傳入格式錯誤)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_shiftGroupMembers",
        ///   "Result": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (例外狀況)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_shiftGroupMembers",
        ///   "Result": "Exception message"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - `GUID` 為必填欄位，必須指定要刪除的成員。  
        /// - 傳入多筆資料時，系統會逐筆檢核並刪除。  
        /// - 若指定的 `GUID` 不存在，該筆資料會被忽略，不會報錯。  
        /// - 成功回傳的 `Data` 會包含已刪除的成員資料。  
        /// </remarks>
        /// <param name="returnData">前端傳入的 Request，包含欲刪除的 Data 陣列 (需含 GUID)</param>
        /// <returns>回傳標準化 JSON，包含 Code、Method、Result、TimeTaken 與 Data (刪除的成員資料)</returns>
        [HttpPost("delete_shiftGroupMembers")]
        public string delete_shiftGroupMembers([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_shiftGroupMembers";

            try
            {
                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<ShiftGroupMemberClass> input = returnData.Data.ObjToClass<List<ShiftGroupMemberClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var sql_shiftGroupMember = MethodClass.GetSQLControl<ShiftGroupMemberClass>();
                var output = new List<ShiftGroupMemberClass>();
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
                    List<object[]> list_objects = sql_shiftGroupMember.GetRowsByDefult(null, "GUID", temp.GUID);

                    if (list_objects.Count != 0)
                    {
                        list_delete.Add(list_objects[0]);
                    }
                }

                if (list_delete.Count > 0) sql_shiftGroupMember.DeleteExtra(null, list_delete);
                output = list_delete.SQLToClass<ShiftGroupMemberClass>();
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

        public static List<ShiftGroupClass> GetShiftGroups(bool staff_info = true)
        {
            return GetShiftGroupsAsync().GetAwaiter().GetResult();
        }
        public static async Task<List<ShiftGroupClass>> GetShiftGroupsAsync(bool staff_info = true)
        {
            var result = await GetShiftGroupsAsync(new List<string>(), staff_info);
            return result.shiftGroups;
        }
        public static ShiftGroupClass GetShiftGroups(string GUID, bool staff_info = true)
        {
            return GetShiftGroupsAsync(GUID, staff_info).GetAwaiter().GetResult() ?? null;
        }
        public static async Task<ShiftGroupClass> GetShiftGroupsAsync(string GUID, bool staff_info = true)
        {
            List<string> valueAry = new List<string>();
            valueAry.Add(GUID);
            var result = await GetShiftGroupsAsync(valueAry, staff_info);
            return result.shiftGroups[0] ?? null;
        }
        /// <summary>
        /// 查詢排班群組 (ShiftGroup) 及其成員資料
        /// </summary>
        /// <remarks>
        /// ## 📌 功能說明  
        /// 此靜態方法會依據傳入的 <c>ValueAry</c> 查詢條件，從資料庫撈取 **ShiftGroup** 群組，並一併載入其所屬的成員 (ShiftGroupMember)。  
        /// 每個成員會自動補齊對應的 <c>StaffClass</c> 資料，填入 <c>staff_info</c> 屬性。  
        ///
        /// ## 🔑 支援條件  
        /// - `GUID`：群組唯一識別碼 (精確搜尋)。  
        /// - `group_name`：群組名稱 (模糊搜尋)。  
        /// - `page`：頁碼 (預設 1)。  
        /// - `pageSize`：每頁筆數 (預設 50)。  
        /// - `sortBy`：排序欄位 (預設 created_at)。  
        /// - `sortOrder`：排序方式，asc 或 desc (預設 desc)。  
        ///
        /// ## 📤 回傳內容  
        /// - shiftGroups：符合條件的群組清單 (含 Members)。  
        /// - totalCount：符合條件的總筆數。  
        /// - totalPages：總頁數。  
        /// - pageSize：每頁筆數。  
        /// - currentPage：目前頁碼。  
        ///
        /// ## 📑 注意事項  
        /// - 若 <c>group_name</c> 與 <c>GUID</c> 同時存在，系統會同時套用條件。  
        /// - <c>Members</c> 會自動帶出成員的 <c>staff_info</c>。若找不到對應 Staff，該成員會被忽略。  
        /// - 此方法僅查詢資料，不負責新增或更新。  
        /// </remarks>
        /// <param name="ValueAry">
        /// 查詢條件集合，例如：  
        /// <list type="bullet">
        /// <item><description><c>GUID=xxxx</c></description></item>
        /// <item><description><c>group_name=小夜固定班</c></description></item>
        /// <item><description><c>page=1</c></description></item>
        /// <item><description><c>pageSize=50</c></description></item>
        /// <item><description><c>sortBy=created_at</c></description></item>
        /// <item><description><c>sortOrder=desc</c></description></item>
        /// </list>
        /// </param>
        /// <returns>
        /// Tuple，內容包含：  
        /// - List&lt;ShiftGroupClass&gt; shiftGroups：群組清單 (含成員)。  
        /// - int totalCount：符合條件的總筆數。  
        /// - int totalPages：總頁數。  
        /// - int pageSize：每頁筆數。  
        /// - int currentPage：目前頁碼。  
        /// </returns>
        public static (List<ShiftGroupClass> shiftGroups, int totalCount, int totalPages, int pageSize, int currentPage) GetShiftGroups(List<string> ValueAry, bool staff_info = true)
        {       
            return GetShiftGroupsAsync(ValueAry, staff_info).GetAwaiter().GetResult();
        }
        /// <summary>
        /// 查詢排班群組 (ShiftGroup) 及其成員資料 (非同步)
        /// </summary>
        /// <remarks>
        /// 功能說明同 <c>GetShiftGroups</c>，但採用 async/await，避免阻塞。
        /// </remarks>
        public static async Task<(List<ShiftGroupClass> shiftGroups, int totalCount, int totalPages, int pageSize, int currentPage)> GetShiftGroupsAsync(List<string> ValueAry, bool staff_info = true)
        {
            string Esc(string s) => (s ?? "").Replace("'", "''");

            var sql_shiftGroup = MethodClass.GetSQLControl<ShiftGroupClass>();
            var sql_shiftGroupMember = MethodClass.GetSQLControl<ShiftGroupMemberClass>();
            var sql_staff = MethodClass.GetSQLControl<StaffClass>();

            List<StaffClass> staffs = staff.GetAllStaffs(); // 若有非同步可改成 await staff.GetAllStaffsAsync()
            Dictionary<string, List<StaffClass>> keyValuePairs_staffs = staffs.CoverToDictionaryByGUID();
            List<StaffClass> staffs_buf = new List<StaffClass>();

            // 解析參數
            string GetVal(string key) =>
                ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                ?.Split('=')[1];

            string GUID = GetVal("GUID") ?? "";
            string group_name = GetVal("group_name") ?? "";

            int page = (GetVal("page") ?? "1").StringToInt32();
            int pageSize = (GetVal("pageSize") ?? "50").StringToInt32();
            string sortBy = GetVal("sortBy") ?? "created_at";
            string sortOrder = (GetVal("sortOrder") ?? "desc").ToUpper();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            List<ShiftGroupClass> shiftGroups = new List<ShiftGroupClass>();
            int totalCount = 0, totalPages = 0;

            // 條件搜尋
            string where = $" WHERE 1=1 ";
            if (!GUID.StringIsEmpty()) where += $" AND GUID = '{Esc(GUID)}' ";
            if (!group_name.StringIsEmpty()) where += $" AND group_name LIKE '%{Esc(group_name)}%' ";

            string orderBy = $" ORDER BY {sortBy} {sortOrder} ";
            int offset = (page - 1) * pageSize;

            // 查詢總數
            string countSql = $"SELECT COUNT(*) AS cnt FROM {sql_shiftGroup.Database}.{sql_shiftGroup.TableName} {where}";
            var dtCnt = await sql_shiftGroup.WtrteCommandAndExecuteReaderAsync(countSql);
            totalCount = dtCnt.Rows[0]["cnt"].ToString().StringToInt32();
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // 查詢群組
            string querySql = $@"
        SELECT * FROM {sql_shiftGroup.Database}.{sql_shiftGroup.TableName}
        {where}
        {orderBy}
        LIMIT {offset}, {pageSize}";
            var dt = await sql_shiftGroup.WtrteCommandAndExecuteReaderAsync(querySql);
            shiftGroups = dt.DataTableToRowList().SQLToClass<ShiftGroupClass>() ?? new List<ShiftGroupClass>();

            // === 取得群組成員，填入 Members 屬性 ===
            foreach (var group in shiftGroups)
            {
                string memberSql = $@"
            SELECT * FROM {sql_shiftGroupMember.Database}.{sql_shiftGroupMember.TableName}
            WHERE group_guid = '{group.GUID}'";

                var dtMembers = await sql_shiftGroupMember.WtrteCommandAndExecuteReaderAsync(memberSql);
                var members_temp = dtMembers.DataTableToRowList().SQLToClass<ShiftGroupMemberClass>() ?? new List<ShiftGroupMemberClass>();
                var members = new List<ShiftGroupMemberClass>();
                if(staff_info)
                {
                    foreach (var member in members_temp)
                    {
                        staffs_buf = keyValuePairs_staffs.SortDictionaryByGUID(member.staff_guid);
                        if (staffs_buf.Count > 0)
                        {
                            member.staff_info = staffs_buf[0];
                            members.Add(member);
                        }
                    }
                }
             
                group.Members = members;
            }

            return (shiftGroups, totalCount, totalPages, pageSize, page);
        }

      
    }
}
