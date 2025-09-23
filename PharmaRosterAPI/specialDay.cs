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

namespace PharmaRosterAPI
{
    [Route("phar_roster_api/[controller]")]
    public class specialDay : ControllerBase
    {
        /// <summary>
        /// 初始化 ShiftGroup 與 ShiftGroupMember 資料表
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於初始化 **ShiftGroup (班別群組)** 與 **ShiftGroupMember (群組成員)** 資料表。  
        /// 系統會自動檢查資料表是否存在，若不存在則建立。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "init"
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
        ///       "TableName": "shiftGroup",
        ///       "Created": true
        ///     },
        ///     {
        ///       "TableName": "shiftGroupMember",
        ///       "Created": true
        ///     }
        ///   ],
        ///   "TimeTaken": "0.006s"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (例外狀況)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "init",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - 此 API 僅需在系統安裝或初始化階段呼叫一次。  
        /// - 若資料表已存在，不會重新建立，但仍會回傳成功訊息。  
        /// - 回傳的 <c>Data</c> 陣列會列出檢查或建立的資料表狀態。  
        /// </remarks>
        /// <param name="returnData">前端傳入的標準化請求物件</param>
        /// <returns>回傳標準化 JSON，包含 Code、Method、Result、Data 與 TimeTaken</returns>
        [HttpPost("init")]
        public string init([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "init";

            try
            {
                List<Table> tables = new List<Table>();
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<SpecialDayClass>());

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
        /// 查詢 SpecialDay 特殊日期清單
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於依據查詢條件，取得 **特殊日期 (SpecialDay)** 清單。  
        /// 支援依日期、描述等條件查詢，並具備分頁與排序功能。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "get_specialDays",
        ///   "ValueAry": [
        ///     "date=2025-10-10",
        ///     "description=國慶日",
        ///     "page=1",
        ///     "pageSize=20",
        ///     "sortBy=date",
        ///     "sortOrder=asc"
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例
        /// ✅ 成功
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "get_specialDays",
        ///   "Result": "共取得(1)筆資料",
        ///   "Data": [
        ///     {
        ///       "GUID": "A1B2C3D4-5678-9ABC-DEF0-1234567890AB",
        ///       "date": "2025-10-10",
        ///       "description": "國慶日",
        ///       "created_at": "2025-09-20 09:00:00"
        ///     }
        ///   ],
        ///   "TimeTaken": "0.004s",
        ///   "TotalCount": 1,
        ///   "TotalPages": 1,
        ///   "CurrentPage": 1,
        ///   "PageSize": 20
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (缺少必要欄位)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_specialDays",
        ///   "Result": "ValueAry 不能為空"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (例外狀況)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "get_specialDays",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - <c>date</c> 可精準查詢單一日期。  
        /// - <c>description</c> 以模糊比對 (LIKE) 查詢。  
        /// - 支援分頁與排序參數，預設依 <c>date asc</c> 排序。  
        /// </remarks>
        /// <param name="returnData">前端傳入的標準化請求物件，需包含 ValueAry 查詢條件</param>
        /// <returns>回傳標準化 JSON，包含 Code、Method、Result、Data 與分頁資訊</returns>
        [HttpPost("get_specialDays")]
        public string get_specialDays([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_specialDays";

            try
            {
                // 驗證必填
                if (returnData.ValueAry == null || returnData.ValueAry.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "ValueAry 不能為空";
                    return returnData.JsonSerializationt();
                }

                (List<SpecialDayClass> specialDays, int totalCount, int totalPages, int pageSize, int currentPage)
                    = GetSpecialDays(returnData.ValueAry);

                returnData.Code = 200;
                returnData.Result = $"共取得({specialDays.Count})筆資料";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = specialDays;
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
        /// 新增或更新 SpecialDay 特殊日期設定
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於批次新增或更新 **特殊日期 (SpecialDay)**。  
        /// 系統會依據 <c>date</c> 進行檢核：  
        /// - 若資料庫無相同 <c>date</c> → 新增 (自動產生 GUID)。  
        /// - 若資料庫已有相同 <c>date</c> → 更新 (保留既有 GUID)。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "add_and_update_specialDays",
        ///   "Data": [
        ///     {
        ///       "GUID": "",
        ///       "date": "2025-10-10",
        ///       "description": "國慶日",
        ///       "override_required_count": "2",
        ///       "created_at": "2025-09-20 09:00:00"
        ///     },
        ///     {
        ///       "GUID": "",
        ///       "date": "2025-12-25",
        ///       "description": "聖誕節",
        ///       "override_required_count": "0",
        ///       "created_at": "2025-09-20 09:05:00"
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
        ///   "Method": "add_and_update_specialDays",
        ///   "Result": "新增(1)筆資料,修改(1)筆資料",
        ///   "Data": [],
        ///   "TimeTaken": "0.004s"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (缺少必要欄位)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_specialDays",
        ///   "Result": "參數驗證失敗：date 為必填"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (傳入格式錯誤)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_specialDays",
        ///   "Result": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// ❌ 錯誤 (例外狀況)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "add_and_update_specialDays",
        ///   "Result": "Exception: 資料庫連線失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - <c>date</c> 為必填且唯一，系統以此判斷是否新增或更新。  
        /// - <c>override_required_count</c> 預設為 "0"。  
        /// - <c>GUID</c> 由系統自動產生，新增時可傳空字串。  
        /// </remarks>
        /// <param name="returnData">前端傳入的標準化請求物件，內含特殊日期資料陣列</param>
        /// <returns>回傳標準化 JSON，包含 Code、Method、Result、Data 與 TimeTaken</returns>
        [HttpPost("add_and_update_specialDays")]
        public string add_and_update_specialDays([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "add_and_update_specialDays";

            try
            {
                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<SpecialDayClass> input = returnData.Data.ObjToClass<List<SpecialDayClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var sql_specialDay = MethodClass.GetSQLControl<SpecialDayClass>();
                var output = new List<SpecialDayClass>();
                var datas_add = new List<SpecialDayClass>();
                var datas_update = new List<SpecialDayClass>();

                // === 2. 寫入流程 ===
                foreach (var temp in input)
                {
                    // 檢核必填
                    if (temp.date.Check_Date_String() == false)
                    {
                        returnData.Code = -200;
                        returnData.Result = "參數驗證失敗：date 為必填";
                        return returnData.JsonSerializationt();
                    }
          

                    List<object[]> list_objects = sql_specialDay.GetRowsByDefult(null, "date", temp.date);

                    if (list_objects.Count == 0)
                    {
                        temp.GUID = Guid.NewGuid().ToString();
                        temp.created_at = DateTime.Now.ToDateTimeString_6();
                        datas_add.Add(temp);
                    }
                    else
                    {
                        SpecialDayClass specialDay = list_objects[0].SQLToClass<SpecialDayClass>();
                        temp.GUID = specialDay.GUID;
                        temp.created_at = DateTime.Now.ToDateTimeString_6();
                        datas_update.Add(temp);
                    }
                }

                if (datas_add.Count > 0) sql_specialDay.AddRows(null, datas_add.ClassToSQL<SpecialDayClass>());
                if (datas_update.Count > 0) sql_specialDay.UpdateByDefulteExtra(null, datas_update.ClassToSQL<SpecialDayClass>());

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
        /// 刪除特殊日 (SpecialDay) 資料
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於刪除指定的特殊日 (SpecialDayClass) 資料，通常用於移除假日或特殊排班日的設定。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "delete_specialDays",
        ///   "Data": [
        ///     { "date": "2025-01-01" },
        ///     { "date": "2025-02-28" }
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "delete_specialDays",
        ///   "Result": "刪除(2)筆資料",
        ///   "TimeTaken": "32ms",
        ///   "Data": []
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少必要參數：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_specialDays",
        ///   "Result": "參數驗證失敗：date 為必填"
        /// }
        /// ```
        ///
        /// - 傳入格式錯誤：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_specialDays",
        ///   "Result": "Data 格式錯誤或無有效資料"
        /// }
        /// ```
        ///
        /// - 系統例外：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "delete_specialDays",
        ///   "Result": "Exception message"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項
        /// - 傳入的 <c>Data</c> 必須為陣列，每筆至少需包含 <c>date</c> 欄位。  
        /// - 系統會依據 `date` 進行精確比對並刪除，若該日期不存在則不會動作。  
        /// - 建議僅刪除未進行排班運算的特殊日，以免影響排班紀錄。  
        /// </remarks>
        /// <param name="returnData">統一封裝的請求與回應物件，需包含 Data 陣列</param>
        /// <returns>JSON 格式的回應字串，包含刪除筆數與狀態</returns>
        [HttpPost("delete_specialDays")]
        public string delete_specialDays([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_specialDays";

            try
            {
                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                List<SpecialDayClass> input = returnData.Data.ObjToClass<List<SpecialDayClass>>();
                if (input == null || input.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 格式錯誤或無有效資料";
                    return returnData.JsonSerializationt();
                }

                var sql_specialDay = MethodClass.GetSQLControl<SpecialDayClass>();
                var output = new List<SpecialDayClass>();
                var list_delete = new List<object[]>();
                // === 2. 刪除流程 ===
                foreach (var temp in input)
                {
                    // 檢核必填
                    if (temp.date.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "參數驗證失敗：date 為必填";
                        return returnData.JsonSerializationt();
                    }
                    List<object[]> list_objects = sql_specialDay.GetRowsByDefult(null, "date", temp.date);

                    if (list_objects.Count != 0)
                    {
                        list_delete.Add(list_objects[0]);
                    }
                }

                if (list_delete.Count > 0) sql_specialDay.DeleteExtra(null, list_delete);

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

        public static (List<SpecialDayClass> specialDays, int totalCount, int totalPages, int pageSize, int currentPage) GetSpecialDays(List<string> ValueAry)
        {
            string Esc(string s) => (s ?? "").Replace("'", "''");

            var sql_specialDay = MethodClass.GetSQLControl<SpecialDayClass>();

            // 解析參數
            string GetVal(string key) =>
                ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                ?.Split('=')[1];

            string date = GetVal("date") ?? "";
            string description = GetVal("description") ?? "";

            int page = (GetVal("page") ?? "1").StringToInt32();
            int pageSize = (GetVal("pageSize") ?? "50").StringToInt32();
            string sortBy = GetVal("sortBy") ?? "date";
            string sortOrder = (GetVal("sortOrder") ?? "asc").ToUpper();

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            List<SpecialDayClass> specialDays = new List<SpecialDayClass>();
            int totalCount = 0, totalPages = 0;

            // 條件搜尋
            string where = $" WHERE 1=1 ";
            if (!date.StringIsEmpty()) where += $" AND date = '{Esc(date)}' ";
            if (!description.StringIsEmpty()) where += $" AND description LIKE '%{Esc(description)}%' ";

            string orderBy = $" ORDER BY {sortBy} {sortOrder} ";
            int offset = (page - 1) * pageSize;

            // 查詢總數
            string countSql = $"SELECT COUNT(*) AS cnt FROM {sql_specialDay.Database}.{sql_specialDay.TableName} {where}";
            var dtCnt = sql_specialDay.WtrteCommandAndExecuteReader(countSql);
            totalCount = dtCnt.Rows[0]["cnt"].ToString().StringToInt32();
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // 查詢資料
            string querySql = $@"
        SELECT * FROM {sql_specialDay.Database}.{sql_specialDay.TableName}
        {where}
        {orderBy}
        LIMIT {offset}, {pageSize}";
            var dt = sql_specialDay.WtrteCommandAndExecuteReader(querySql);
            specialDays = dt.DataTableToRowList().SQLToClass<SpecialDayClass>() ?? new List<SpecialDayClass>();

            return (specialDays, totalCount, totalPages, pageSize, page);
        }

    }
}
