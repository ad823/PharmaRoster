using Basic;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using NPOI.SS.Formula.Eval;
using PharmaRosterLib;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace PharmaRosterAPI
{
    [Route("phar_roster_api/[controller]")]
    public class staffLine : ControllerBase
    {
        [HttpPost("init")]
        public string init([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "init";

            try
            {
                List<Table> tables = new List<Table>();
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<StaffLineLinkClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<StaffLineBindCodeClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<LinePushLogClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<LineSettingClass>());

                

                returnData.Code = 200;
                returnData.Data = tables;
                returnData.Result = "初始化 staffLine 資料表完成";
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
        /// 設定/更新 LINE 設定值 (例如 add_friend_url)
        /// </summary>
        /// <remarks>
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "set_line_setting",
        ///   "ValueAry": [
        ///     "setting_key=add_friend_url",
        ///     "setting_value=https://lin.ee/XXXXXXXX",
        ///     "is_enabled=1"
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 成功回傳範例
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "set_line_setting",
        ///   "Result": "設定成功",
        ///   "Data": {
        ///     "GUID": "xxxx",
        ///     "setting_key": "add_friend_url",
        ///     "setting_value": "https://lin.ee/XXXXXXXX",
        ///     "is_enabled": "1",
        ///     "created_at": "2026-02-25 09:00:00",
        ///     "updated_at": "2026-02-25 09:00:00",
        ///     "created_by": "",
        ///     "updated_by": ""
        ///   }
        /// }
        /// ```
        /// </remarks>
        [HttpPost("set_line_setting")]
        public async Task<string> set_line_setting([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "set_line_setting";

            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry?.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];

                string setting_key = GetVal("setting_key");
                string setting_value = GetVal("setting_value");
                string is_enabled = GetVal("is_enabled");

                if (setting_key.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "setting_key 不可為空";
                    return returnData.JsonSerializationt();
                }
                if (setting_value.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "setting_value 不可為空";
                    return returnData.JsonSerializationt();
                }
                if (is_enabled.StringIsEmpty()) is_enabled = "1";

                var sql_setting = MethodClass.GetSQLControl<LineSettingClass>();
                string nowStr = DateTime.Now.ToDateTimeString();

                // 查是否已存在 key
                string sql = $@"
            SELECT *
            FROM {sql_setting.Database}.{sql_setting.TableName}
            WHERE setting_key = @setting_key
            LIMIT 1";
                var parameters = new { setting_key = setting_key };
                List<object[]> rows = await sql_setting.WriteCommandAsync(sql, parameters);
                List<LineSettingClass> list = rows.SQLToClass<LineSettingClass>();
                LineSettingClass dbItem = list?.FirstOrDefault();

                LineSettingClass resultItem;

                if (dbItem == null)
                {
                    // 新增
                    resultItem = new LineSettingClass
                    {
                        GUID = Guid.NewGuid().ToString(),
                        setting_key = setting_key,
                        setting_value = setting_value,
                        is_enabled = is_enabled,
                        created_at = nowStr,
                        updated_at = nowStr,
                        created_by = "",
                        updated_by = ""
                    };

                    sql_setting.AddRows(null, new List<LineSettingClass> { resultItem }.ClassToSQL<LineSettingClass>());
                }
                else
                {
                    // 更新
                    dbItem.setting_value = setting_value;
                    dbItem.is_enabled = is_enabled;
                    dbItem.updated_at = nowStr;
                    dbItem.updated_by = "";

                    resultItem = dbItem;

                    sql_setting.UpdateByDefulteExtra(null, new List<LineSettingClass> { resultItem }.ClassToSQL<LineSettingClass>());
                }

                returnData.Code = 200;
                returnData.Data = resultItem;
                returnData.Result = "設定成功";
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
        /// 讀取 LINE 設定
        /// </summary>
        /// <remarks>
        /// ## 📥 Request JSON 範例（指定 key）
        /// ```json
        /// {
        ///   "Method": "get_line_setting",
        ///   "ValueAry": [ "setting_key=add_friend_url" ]
        /// }
        /// ```
        ///
        /// ## 📥 Request JSON 範例（不指定 key，回全部）
        /// ```json
        /// {
        ///   "Method": "get_line_setting",
        ///   "ValueAry": [ ]
        /// }
        /// ```
        /// </remarks>
        [HttpPost("get_line_setting")]
        public async Task<string> get_line_setting([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_line_setting";

            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry?.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];

                string setting_key = GetVal("setting_key");
                string only_enabled = GetVal("only_enabled"); // 可選: 1/0，預設 0

                if (only_enabled.StringIsEmpty()) only_enabled = "0";

                var sql_setting = MethodClass.GetSQLControl<LineSettingClass>();

                string sql;
                object parameters;

                if (setting_key.StringIsEmpty() == false)
                {
                    sql = $@"
                SELECT *
                FROM {sql_setting.Database}.{sql_setting.TableName}
                WHERE setting_key = @setting_key
                {(only_enabled == "1" ? "AND is_enabled = '1'" : "")}
                LIMIT 1";
                    parameters = new { setting_key = setting_key };

                    List<object[]> rows = await sql_setting.WriteCommandAsync(sql, parameters);
                    List<LineSettingClass> list = rows.SQLToClass<LineSettingClass>();
                    LineSettingClass item = list?.FirstOrDefault();

                    returnData.Code = 200;
                    returnData.Data = item; // 找不到會是 null
                    returnData.Result = item == null ? "查無資料" : "取得設定成功";
                    return returnData.JsonSerializationt(true);
                }
                else
                {
                    sql = $@"
                SELECT *
                FROM {sql_setting.Database}.{sql_setting.TableName}
                {(only_enabled == "1" ? "WHERE is_enabled = '1'" : "")}
            ";
                    parameters = new { };

                    List<object[]> rows = await sql_setting.WriteCommandAsync(sql, parameters);
                    List<LineSettingClass> list = rows.SQLToClass<LineSettingClass>() ?? new List<LineSettingClass>();

                    returnData.Code = 200;
                    returnData.Data = list;
                    returnData.Result = $"取得設定成功,共{list.Count}筆";
                    return returnData.JsonSerializationt(true);
                }
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }

        /// <summary>
        /// 產生 LINE 綁定碼
        /// </summary>
        /// <remarks>
        /// ## 📘 功能說明  
        /// 本 API 用於產生指定人員的 LINE 綁定碼，讓使用者到 LINE 官方帳號輸入「綁定 123456」完成綁定。  
        /// 同時會從資料表 <c>line_settings</c> 讀取 <c>add_friend_url</c>（若存在）回傳給前端，用於引導加入好友。  
        ///
        /// ## ⚙️ 執行流程  
        /// 1. 從 <c>ValueAry</c> 取得 <c>staff_id</c> 與 <c>expire_minutes</c>(可選,預設10)。  
        /// 2. 將同 <c>staff_id</c> 既有「未使用且未過期」的綁定碼批次失效（<c>is_used=1</c>, <c>used_at=now</c>）。  
        /// 3. 產生 6 碼 <c>bind_code</c>，並查詢資料庫避免撞碼（重試最多 30 次）。  
        /// 4. 新增一筆 <c>staff_line_bind_code</c>。  
        /// 5. 從 <c>line_settings</c> 讀取 <c>add_friend_url</c>，寫入回傳的 <c>ValueAry</c>（<c>add_friend_url=...</c>）。  
        /// 6. 回傳 <c>Data = StaffLineBindCodeClass</c>。  
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "create_line_bind_code",
        ///   "ValueAry": [
        ///     "staff_id=A001",
        ///     "expire_minutes=10"
        ///   ]
        /// }
        /// ```
        ///
        /// ## 🔍 參數說明  
        /// | 參數名稱 | 類型 | 必填 | 範例 | 說明 |
        /// |---|---|---|---|---|
        /// | staff_id | string | ✅ | A001 | 員工編號 |
        /// | expire_minutes | string | ❌ | 10 | 綁定碼有效分鐘數（預設 10） |
        ///
        /// ## 📤 成功回傳範例  
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "create_line_bind_code",
        ///   "Result": "產生綁定碼成功",
        ///   "ValueAry": [
        ///     "add_friend_url=https://lin.ee/XXXXXXXX"
        ///   ],
        ///   "Data": {
        ///     "GUID": "xxxx-xxxx",
        ///     "bind_code": "123456",
        ///     "staff_id": "A001",
        ///     "expire_at": "2026-02-25 09:10:00",
        ///     "is_used": "0",
        ///     "used_at": "",
        ///     "used_line_user_id": "",
        ///     "created_at": "2026-02-25 09:00:00",
        ///     "created_by": ""
        ///   }
        /// }
        /// ```
        ///
        /// ## ❌ 錯誤回傳範例  
        /// - staff_id 空：  
        /// ```json
        /// { "Code": -200, "Result": "staff_id 不可為空" }
        /// ```
        /// - 撞碼過多：  
        /// ```json
        /// { "Code": -200, "Result": "產生綁定碼失敗（撞碼過多），請重試" }
        /// ```
        /// </remarks>
        /// <param name="returnData">封裝 API 請求資料的物件（ValueAry 取參數）。</param>
        /// <returns>回傳 JSON 格式字串（Data 為 StaffLineBindCodeClass）。</returns>
        [HttpPost("create_line_bind_code")]
        public async Task<string> create_line_bind_code([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "create_line_bind_code";

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry?.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string staff_id = GetVal("staff_id");
                string expire_minutes_str = GetVal("expire_minutes");

                if (staff_id.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "staff_id 不可為空";
                    return returnData.JsonSerializationt();
                }

                int expireMinutes = 10;
                if (expire_minutes_str.StringIsEmpty() == false)
                {
                    if (int.TryParse(expire_minutes_str, out int m) && m > 0) expireMinutes = m;
                }

                var sql_bindCode = MethodClass.GetSQLControl<StaffLineBindCodeClass>();

                string nowStr = DateTime.Now.ToDateTimeString();
                string expireAtStr = DateTime.Now.AddMinutes(expireMinutes).ToDateTimeString();

                // 1) 先把同 staff_id 舊的「未使用且未過期」綁定碼失效
                {
                    string sql = $@"
                        SELECT *
                        FROM {sql_bindCode.Database}.{sql_bindCode.TableName}
                        WHERE staff_id = @staff_id
                          AND is_used = '0'
                          AND (expire_at IS NULL OR expire_at = '' OR expire_at > @now)
                    ";

                    var parameters = new
                    {
                        staff_id = staff_id,
                        now = nowStr
                    };

                    List<object[]> rows = await sql_bindCode.WriteCommandAsync(sql, parameters);
                    List<StaffLineBindCodeClass> oldCodes = rows.SQLToClass<StaffLineBindCodeClass>();

                    if (oldCodes != null && oldCodes.Count > 0)
                    {
                        foreach (var c in oldCodes)
                        {
                            c.is_used = "1";
                            c.used_at = nowStr;
                        }
                        sql_bindCode.UpdateByDefulteExtra(null, oldCodes.ClassToSQL<StaffLineBindCodeClass>());
                    }
                }

                // 2) 產生 6 碼，避免撞碼
                string bind_code = "";
                const int maxTry = 30;

                for (int i = 0; i < maxTry; i++)
                {
                    string candidate = Generate6DigitCode();

                    string sql_check = $@"
                        SELECT GUID
                        FROM {sql_bindCode.Database}.{sql_bindCode.TableName}
                        WHERE bind_code = @bind_code
                        LIMIT 1
                    ";
                    var parameters_check = new { bind_code = candidate };
                    List<object[]> chk = await sql_bindCode.WriteCommandAsync(sql_check, parameters_check);

                    if (chk == null || chk.Count == 0)
                    {
                        bind_code = candidate;
                        break;
                    }
                }

                if (bind_code.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "產生綁定碼失敗（撞碼過多），請重試";
                    return returnData.JsonSerializationt();
                }

                // 3) 新增資料
                StaffLineBindCodeClass newRow = new StaffLineBindCodeClass
                {
                    GUID = Guid.NewGuid().ToString(),
                    bind_code = bind_code,
                    staff_id = staff_id,
                    expire_at = expireAtStr,
                    is_used = "0",
                    used_at = DateTime.MinValue.ToDateString(),
                    used_line_user_id = "",
                    created_at = nowStr,
                    created_by = ""
                };

                // 你目前使用 AddRow
                sql_bindCode.AddRow(null, newRow.ClassToSQL<StaffLineBindCodeClass>());

                // 4) 從 line_settings 讀取 add_friend_url 回傳到 ValueAry
                {
                    string addFriendUrl = await GetLineSettingValue("add_friend_url");
                    if (addFriendUrl.StringIsEmpty() == false)
                    {
                        if (returnData.ValueAry == null) returnData.ValueAry = new List<string>();
                        returnData.ValueAry.Add($"add_friend_url={addFriendUrl}");
                    }
                }

                // 5) 回傳
                returnData.Code = 200;
                returnData.Data = newRow;
                returnData.Result = "產生綁定碼成功";
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
        /// 查詢 LINE 綁定狀態（回傳 StaffLineBindCodeClass 清單）
        /// </summary>
        /// <remarks>
        /// ## 📘 功能說明
        /// 本 API 用於查詢 staff_line_bind_code 的綁定狀態。
        ///
        /// ✅ 查詢模式：
        /// 1) 指定 staff_id 或 staff_ids：
        ///    - 針對每一位 staff，回傳其「最新一筆」資料（created_at DESC LIMIT 1）
        ///    - 查不到也回空物件（避免 null）
        ///
        /// 2) staff_id / staff_ids 都未提供或為空：
        ///    - ✅ 只回「成功綁定」資料
        ///    - 成功綁定判定：is_used = 1 且 used_line_user_id 不為空
        ///    - 並且每位 staff 只回最新一筆成功綁定（避免回傳同一人多筆）
        ///
        /// ## 📥 Input（ValueAry）
        /// - staff_id=A001
        /// - staff_ids=A001,A002,A003
        /// - （不帶 staff_id / staff_ids 或帶空字串）→ 回傳「已成功綁定」清單（每人最新一筆）
        ///
        /// </remarks>
        /// <param name="returnData">通用傳入物件，使用 ValueAry 作為參數輸入</param>
        /// <returns>序列化後的 returnData JSON 字串</returns>
        [HttpPost("get_bind_status")]
        public async Task<string> get_bind_status([FromBody] returnData returnData)
        {
            returnData.Method = "get_bind_status";
            string reqId = Guid.NewGuid().ToString("N");
            Logger.Log("LINE_BIND", $"[{reqId}] get_bind_status HIT");

            try
            {
                List<string> staffIds = ExtractStaffIds(returnData.ValueAry);

                var sql_bind = MethodClass.GetSQLControl<StaffLineBindCodeClass>();
                List<StaffLineBindCodeClass> result = new List<StaffLineBindCodeClass>();

                // =========================================================
                // A) staff_id 為空 → 只回「成功綁定」且每人最新一筆
                // =========================================================
                if (staffIds == null || staffIds.Count == 0)
                {
                    // MySQL：每位 staff 取最新一筆成功綁定
                    string sql = $@"
SELECT t.*
FROM {sql_bind.Database}.{sql_bind.TableName} t
JOIN (
    SELECT staff_id, MAX(created_at) AS max_created_at
    FROM {sql_bind.Database}.{sql_bind.TableName}
    WHERE is_used = '1'
      AND IFNULL(used_line_user_id,'') <> ''
    GROUP BY staff_id
) x
ON t.staff_id = x.staff_id AND t.created_at = x.max_created_at
ORDER BY t.created_at DESC
";
                    List<object[]> rows = await sql_bind.WriteCommandAsync(sql, null);
                    result = rows.SQLToClass<StaffLineBindCodeClass>() ?? new List<StaffLineBindCodeClass>();

                    returnData.Code = 200;
                    returnData.Data = result;
                    returnData.Result = $"查詢成功(已綁定),共{result.Count}筆";
                    Logger.Log("LINE_BIND", $"[{reqId}] get_bind_status OK (BOUND_ONLY) count={result.Count}");

                    return returnData.JsonSerializationt(true);
                }

                // =========================================================
                // B) 指定 staff_id(s) → 每人回最新一筆（不限定成功/失敗，維持你原邏輯）
                //    若你也想「指定 staff 時只回成功綁定」，我也可以幫你改一行 WHERE
                // =========================================================
                staffIds = staffIds
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var sid in staffIds)
                {
                    string sql = $@"
SELECT *
FROM {sql_bind.Database}.{sql_bind.TableName}
WHERE staff_id = @staff_id
ORDER BY created_at DESC
LIMIT 1
";
                    var p = new { staff_id = sid };
                    List<object[]> rows = await sql_bind.WriteCommandAsync(sql, p);
                    var codes = rows.SQLToClass<StaffLineBindCodeClass>();

                    if (codes != null && codes.Count > 0)
                    {
                        result.Add(codes.First());
                    }
                    else
                    {
                        // 沒資料也回空物件（避免 null）
                        result.Add(new StaffLineBindCodeClass
                        {
                            GUID = "",
                            bind_code = "",
                            staff_id = sid,
                            expire_at = "",
                            is_used = "0",
                            used_at = "",
                            used_line_user_id = "",
                            created_at = "",
                            created_by = ""
                        });
                    }
                }

                returnData.Code = 200;
                returnData.Data = result;
                returnData.Result = $"查詢成功,共{result.Count}筆";
                Logger.Log("LINE_BIND", $"[{reqId}] get_bind_status OK count={result.Count}");

                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                Logger.Log("LINE_BIND", $"[{reqId}] EX: {ex.Message}");
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }

        /// <summary>
        /// 解除 LINE 綁定（回傳 StaffLineBindCodeClass）
        /// </summary>
        [HttpPost("unbind")]
        public async Task<string> unbind([FromBody] returnData returnData)
        {
            returnData.Method = "unbind";
            string reqId = Guid.NewGuid().ToString("N");
            Logger.Log("LINE_BIND", $"[{reqId}] unbind HIT");

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry?.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string staff_id = GetVal("staff_id");
                if (staff_id.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "staff_id 不可為空";
                    return returnData.JsonSerializationt();
                }

                var sql_bind = MethodClass.GetSQLControl<StaffLineBindCodeClass>();
                string nowStr = DateTime.Now.ToDateTimeString();

                string sql = $@"
            SELECT *
            FROM {sql_bind.Database}.{sql_bind.TableName}
            WHERE staff_id = @staff_id
            ORDER BY created_at DESC
            LIMIT 1
        ";

                var p = new { staff_id = staff_id };
                List<object[]> rows = await sql_bind.WriteCommandAsync(sql, p);
                var codes = rows.SQLToClass<StaffLineBindCodeClass>();

                if (codes == null || codes.Count == 0)
                {
                    returnData.Code = 200;
                    returnData.Data = new StaffLineBindCodeClass();
                    returnData.Result = "無綁定資料";
                    return returnData.JsonSerializationt(true);
                }

                var code = codes.First();

                code.is_used = "1";
                code.used_line_user_id = "";
                code.used_at = nowStr;

                sql_bind.UpdateByDefulteExtra(null,
                    new List<StaffLineBindCodeClass> { code }.ClassToSQL<StaffLineBindCodeClass>());

                returnData.Code = 200;
                returnData.Data = code;
                returnData.Result = "解除綁定成功";

                Logger.Log("LINE_BIND", $"[{reqId}] unbind OK staff_id={staff_id}");
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                Logger.Log("LINE_BIND", $"[{reqId}] EX: {ex.Message}");
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }


        /// <summary>
        /// LINE Webhook（綁定碼綁定 + 查詢當下作業表單的排班/排休資訊）
        /// </summary>
        /// <remarks>
        /// ## 📘 功能說明
        /// 本 API 為 LINE Messaging API Webhook 入口，用於處理使用者對官方帳號傳送的訊息事件。
        ///
        /// ✅ 支援功能：
        /// 1) 綁定：
        ///    - 指令：<c>綁定 123456</c> / <c>綁定:123456</c> / <c>123456</c>
        ///    - 功能：完成 staff 與 LINE userId 綁定
        ///
        /// 2) 查詢「排班/排休」（整合成一個指令）：
        ///    - 指令：<c>排班</c> / <c>排休</c> / <c>排班排休</c> / <c>班表</c> / <c>休假</c> / <c>查詢</c>
        ///    - 行為：
        ///      - 只查詢「當下作業表單」（active form）
        ///      - 回傳「整張表單中，有排班的日期+班別+時段」以及「已選休假的日期+上午/下午/全日」
        ///      - 排班資訊來源：<c>DayOffScheduleItemClass</c>（item.workShiftRequirement / item.shift_requirement）
        ///      - 排休資訊來源：<c>StaffDayOffOptionClass</c>（item.option 內 selected_xxx / is_force_ff）
        ///      - ✅ 不讀、不改 <c>option.assigned_shift</c>（依你要求）
        ///
        /// 3) 若傳入內容找不到任何指令：
        ///    - 回傳「指令說明」
        ///
        /// ## ⚙️ 執行流程（每個事件）
        /// 1. 讀取 Header：<c>X-Line-Signature</c>
        /// 2. 讀取原始 Request Body（驗簽必用）
        /// 3. 從資料表 <c>line_settings</c> 取得 <c>channel_secret</c>，以 HMAC-SHA256 驗簽（Base64）
        /// 4. 解析 webhook JSON（events）
        /// 5. 針對每個文字訊息：
        ///    - 先判斷是否為綁定指令（6 碼或「綁定」+6 碼）
        ///    - 否則判斷是否為查詢排班/排休指令
        ///    - 都不是 → 回覆指令說明
        /// 6. 從 <c>line_settings</c> 取得 <c>channel_access_token</c>，用 Reply API 回覆
        /// 7. Webhook 固定快速回應 <c>OK</c>（避免 LINE 重送）
        ///
        /// ## 📥 LINE Webhook JSON 範例（節錄）
        /// ```json
        /// {
        ///   "events": [
        ///     {
        ///       "type": "message",
        ///       "replyToken": "xxxx",
        ///       "source": { "userId": "Uxxxxxxxx" },
        ///       "message": { "type": "text", "text": "排班" }
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 回應
        /// - 固定回傳字串：<c>OK</c>
        ///
        /// ## ❌ 常見錯誤處理
        /// - 驗簽失敗：仍回 OK，但不處理（可加 log）
        /// - 尚未綁定：回覆提示「請先綁定」
        /// - 無當下作業表單：回覆「目前沒有進行中的排休表單」
        ///
        /// ## 📑 注意事項
        /// - 本 API 只回覆文字訊息，不回傳 JSON
        /// - 查詢內容只針對「登入者（由 line_user_id 映射 staff_id）」
        /// - 當下作業表單判定：以 <c>dayoff_group</c> 存在 status=1(週休) 或 status=3(特休) 的 form_guid 為準（取最新一筆）
        /// </remarks>
        /// <returns>固定回傳 OK</returns>
        [HttpPost("webhook")]
        public async Task<string> webhook()
        {
            string reqId = Guid.NewGuid().ToString("N");
            Logger.Log("LINE_WEBHOOK", $"[{reqId}] HIT");

            try
            {
                // 1) 讀取 raw body（驗簽必用）
                Request.EnableBuffering();
                string body = "";
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
                {
                    body = await reader.ReadToEndAsync();
                    Request.Body.Position = 0;
                }

                // 2) signature
                string signature = Request.Headers["X-Line-Signature"].FirstOrDefault() ?? "";

                // 3) channel_secret
                string channelSecret = await GetLineSettingValue("channel_secret");
                if (channelSecret.StringIsEmpty())
                {
                    Logger.Log("LINE_WEBHOOK", $"[{reqId}] channel_secret_missing -> return OK");
                    return "OK";
                }

                // 4) verify signature
                bool sigOk = VerifyLineSignature(channelSecret, body, signature);
                Logger.Log("LINE_WEBHOOK", $"[{reqId}] signature_verify={(sigOk ? "OK" : "FAIL")}");
                if (!sigOk) return "OK";

                // 5) parse json
                LineWebhookRequest req = null;
                try
                {
                    req = JsonSerializer.Deserialize<LineWebhookRequest>(body);
                }
                catch (Exception ex)
                {
                    Logger.Log("LINE_WEBHOOK", $"[{reqId}] json_deserialize_fail: {ex.Message}");
                    return "OK";
                }

                int eventCount = req?.events?.Count ?? 0;
                if (eventCount == 0) return "OK";

                // token 一次拿
                string token = await GetLineSettingValue("channel_access_token");

                foreach (var ev in req.events)
                {
                    if (ev == null) continue;
                    if (ev.type != "message") continue;
                    if (ev.message == null || ev.message.type != "text") continue;

                    string text = ev.message.text ?? "";
                    string replyToken = ev.replyToken ?? "";
                    string userId = ev.source?.userId ?? "";

                    if (text.StringIsEmpty() || replyToken.StringIsEmpty() || userId.StringIsEmpty())
                        continue;

                    // 解析指令
                    string bindCode = ParseBindCode(text); // ✅ 你原本就有：支援 綁定 123456 / 綁定:123456 / 6碼
                    bool isQueryCmd = IsQueryScheduleAndDayOffCommand(text);

                    // (A) 綁定
                    if (!bindCode.StringIsEmpty())
                    {
                        string bindResult = await BindByCode(bindCode, userId);

                        if (token.StringIsEmpty() == false)
                            await ReplyText_WithLog(reqId, token, replyToken, bindResult);

                        continue;
                    }

                    // (B) 查詢排班/排休
                    if (isQueryCmd)
                    {
                        // 1) 取得 staff_id（由 line_user_id → staff_id）
                        string staffId = await GetStaffIdByLineUserId(userId);
                        if (staffId.StringIsEmpty())
                        {
                            string msg = "你尚未綁定帳號，請先輸入 6 位數綁定碼，例如：綁定 123456";
                            if (token.StringIsEmpty() == false) await ReplyText_WithLog(reqId, token, replyToken, msg);
                            continue;
                        }

                        // 2) 找「當下作業表單」
                        var active = await GetActiveFormByDayoffGroupStatus();
                        if (active == null || active.form_name.StringIsEmpty())
                        {
                            string msg = "目前沒有進行中的排休表單。";
                            if (token.StringIsEmpty() == false) await ReplyText_WithLog(reqId, token, replyToken, msg);
                            continue;
                        }

                        // 3) 組文字回覆
                        string reply = await BuildScheduleAndDayOffReplyText(active.form_name, staffId);
                        if (reply.StringIsEmpty()) reply = "目前沒有可顯示的排班或已選休假資料。";

                        if (token.StringIsEmpty() == false)
                            await ReplyText_WithLog(reqId, token, replyToken, reply);

                        continue;
                    }

                    // (C) 都找不到指令 -> 回指令說明
                    if (token.StringIsEmpty() == false)
                    {
                        await ReplyText_WithLog(reqId, token, replyToken, BuildHelpMessage());
                    }
                }

                return "OK";
            }
            catch (Exception ex)
            {
                Logger.Log("LINE_WEBHOOK", $"[{reqId}] EXCEPTION: {ex.Message}");
                return "OK";
            }
            finally
            {
                Logger.LogAddLine("LINE_WEBHOOK");
            }
        }

        #region === 查詢排班/排休：核心 helper（不改資料表、最小改動）===

        /// <summary>
        /// 判斷是否為「排班/排休整合查詢」指令
        /// </summary>
        private bool IsQueryScheduleAndDayOffCommand(string text)
        {
            if (text == null) return false;
            string t = text.Trim();

            // ✅ 你可以依現場習慣再加字
            string[] keys = new[]
            {
        "排班", "排休", "排班排休", "班表", "休假", "查詢"
    };

            return keys.Any(k => string.Equals(t, k, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 指令說明文字
        /// </summary>
        private string BuildHelpMessage()
        {
            var sb = new StringBuilder();
            sb.AppendLine("可用指令：");
            sb.AppendLine("1) 【綁定】：綁定 123456（或直接輸入 6 位數）");
            sb.AppendLine("2) 【查詢排班/排休】：排班、班表");
            return sb.ToString().Trim();
        }

        /// <summary>
        /// 由 line_user_id 取得 staff_id（若找不到回空字串）
        /// </summary>
        /// <remarks>
        /// ✅ 最小假設：你已經有 staff_line_link 資料表可做 line_user_id -> staff_id 對照。
        /// 若你目前是存在其他表（例如 staff_line_bind_code.used_line_user_id 取最新已使用），
        /// 你把表結構貼我，我再幫你改成完全符合你現況。
        /// </remarks>
        private async Task<string> GetStaffIdByLineUserId(string lineUserId)
        {
            if (lineUserId.StringIsEmpty()) return "";

            // === 你若已有 StaffLineLinkClass，請改成你專案實際 class ===
            // [Description("staff_line_link")]
            // public class StaffLineLinkClass { public string GUID; public string staff_id; public string line_user_id; public string updated_at; ... }

            try
            {
                var sql_link = MethodClass.GetSQLControl<StaffLineLinkClass>();

                string sql = $@"
SELECT *
FROM {sql_link.Database}.{sql_link.TableName}
WHERE line_user_id = @line_user_id
ORDER BY updated_at DESC
LIMIT 1
";
                var p = new { line_user_id = lineUserId };
                List<object[]> rows = await sql_link.WriteCommandAsync(sql, p);
                var links = rows.SQLToClass<StaffLineLinkClass>();

                var link = links?.FirstOrDefault();
                if (link == null) return "";

                return link.staff_id ?? "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 當下作業表單（active form）查詢結果
        /// </summary>
        private class ActiveFormInfo
        {
            public string form_guid { get; set; }
            public string form_name { get; set; }
        }

        /// <summary>
        /// 取得「當下作業表單」：以 dayoff_group 存在 status=1 或 status=3 的最新一筆為準
        /// </summary>
        private async Task<ActiveFormInfo> GetActiveFormByDayoffGroupStatus()
        {
            var sql_group = MethodClass.GetSQLControl<DayOffGroupClass>();
            var sql_form = MethodClass.GetSQLControl<DayOffScheduleFormClass>();

            // 1) 先從 group 找最新 active
            string sql = $@"
SELECT *
FROM {sql_group.Database}.{sql_group.TableName}
WHERE status IN ('1','3')
ORDER BY status_changed_at DESC, updated_at DESC
LIMIT 1
";
            List<object[]> rows = await sql_group.WriteCommandAsync(sql, new { });
            var groups = rows.SQLToClass<DayOffGroupClass>();
            var g = groups?.FirstOrDefault();
            if (g == null || g.form_guid.StringIsEmpty()) return null;

            // 2) form_guid -> form_name
            object[] obj_form = sql_form.GetRowsByDefult(null, "GUID", g.form_guid).FirstOrDefault();
            if (obj_form == null) return null;

            var form = obj_form.SQLToClass<DayOffScheduleFormClass>();
            return new ActiveFormInfo
            {
                form_guid = form?.GUID ?? "",
                form_name = form?.form_name ?? ""
            };
        }

        /// <summary>
        /// 建立「排班/排休整合」回覆文字（排班只看 item、排休只看 option）
        /// </summary>
        private async Task<string> BuildScheduleAndDayOffReplyText(string formName, string staffId)
        {
            var form = await GetFormForStaffCore_KeepAllDays(formName, staffId);
            if (form == null) return "";

            var lines = new List<(DateTime dt, int order, string text)>();

            foreach (var day in form.days ?? new List<DayOffScheduleDayClass>())
            {
                DateTime dayDt = day?.date.StringToDateTime() ?? DateTime.MinValue;
                if (dayDt == DateTime.MinValue) continue;

                string dateText = dayDt.ToString("yyyy/MM/dd");

                // 可能一天多 item：全部列出（保守）
                var items = day.items ?? new List<DayOffScheduleItemClass>();
                if (items.Count == 0) continue;

                foreach (var item in items)
                {
                    // ========= A) 排班（只看 item）=========
                    string shiftType = GetShiftTypeFromItem(item).Trim();
                    string shiftTime = GetShiftTimeFromItem(item).Trim();
                    if (shiftType == "swing") shiftType = "小夜";
                    if (shiftType == "midnight") shiftType = "大夜";
                    if (shiftType == "holiday") shiftType = "假日";
                    bool hasSchedule =
                        shiftType.StringIsEmpty() == false
                        && !shiftType.Equals("OFF", StringComparison.OrdinalIgnoreCase)
                        && !shiftType.Equals("WW", StringComparison.OrdinalIgnoreCase)
                        && !shiftType.Equals("FF", StringComparison.OrdinalIgnoreCase);

                    if (hasSchedule)
                    {
                        string s = shiftTime.StringIsEmpty()
                            ? $"{dateText}({GetChineseWeekDay(dateText.StringToDateTime())}) {shiftType}"
                            : $"{dateText}({GetChineseWeekDay(dateText.StringToDateTime())}) {shiftType} {shiftTime}";
                        lines.Add((dayDt.Date, 1, s));
                    }

                    // ========= B) 排休（只看 option）=========
                    string dayoffText = GetDayOffTextFromOption(item.option);
                    if (dayoffText.StringIsEmpty() == false && item.option.date.StringToDateTime() != DateTime.MinValue && item.option.date.StringIsEmpty() == false)
                    {
                        lines.Add((item.option.date.StringToDateTime(), 2, $"{item.option.date.StringToDateTime().ToDateString()}({GetChineseWeekDay(item.option.date.StringToDateTime())}) {dayoffText}"));
                    }
                }
            }

            lines = lines
                .OrderBy(x => x.dt)
                .ThenBy(x => x.order)
                .ThenBy(x => x.text)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine($"【排班/排休】{form.form_name}");
            sb.AppendLine($"工號：{staffId}");
            if (lines.Count == 0)
            {
                sb.AppendLine("目前沒有可顯示的排班或已選休假資料。");
                return sb.ToString().Trim();
            }

            foreach (var x in lines) sb.AppendLine(x.text);
            return sb.ToString().Trim();
        }
        public static string GetChineseWeekDay(DateTime date)
        {
            switch (date.DayOfWeek)
            {
                case DayOfWeek.Sunday: return "日";
                case DayOfWeek.Monday: return "一";
                case DayOfWeek.Tuesday: return "二";
                case DayOfWeek.Wednesday: return "三";
                case DayOfWeek.Thursday: return "四";
                case DayOfWeek.Friday: return "五";
                case DayOfWeek.Saturday: return "六";
                default: return "";
            }
        }
        /// <summary>
        /// 排班：從 item 解析班別（不讀 option.assigned_shift）
        /// </summary>
        private string GetShiftTypeFromItem(DayOffScheduleItemClass item)
        {
            try
            {
                if (item == null) return "";

                if (item.workShiftRequirement != null)
                    return item.workShiftRequirement.shift_type ?? "";

                if (item.shift_requirement.StringIsEmpty()) return "";
                var req = JsonSerializer.Deserialize<WorkShiftRequirementClass>(item.shift_requirement);
                return req?.shift_type ?? "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 排班：從 item 解析時段（不讀 option.assigned_shift）
        /// </summary>
        private string GetShiftTimeFromItem(DayOffScheduleItemClass item)
        {
            try
            {
                if (item == null) return "";

                if (item.workShiftRequirement != null)
                    return item.workShiftRequirement.time ?? "";

                if (item.shift_requirement.StringIsEmpty()) return "";
                var req = JsonSerializer.Deserialize<WorkShiftRequirementClass>(item.shift_requirement);
                return req?.time ?? "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 排休：從 option 判斷（FF/已選），回傳「休假 全日/上午/下午」
        /// </summary>
        private string GetDayOffTextFromOption(StaffDayOffOptionClass opt)
        {
            if (opt == null) return "";

            if (opt.is_force_ff == "true") return "休假 全日";
            if (opt.selected_full == "true") return "休假 全日";
            if (opt.selected_half_am == "true") return "休假 上午";
            if (opt.selected_half_pm == "true") return "休假 下午";

            return "";
        }

        /// <summary>
        /// 取得指定表單(form_name)中，指定登入者(staff_id)的個人資料（保留所有 day，即使那天沒有 item 也要回）
        /// </summary>
        /// <remarks>
        /// ✅ 與你先前 get_form_for_staff 的差異：
        /// - days：回傳整張表單所有 days（排序後）
        /// - 每個 day.items：只放登入者自己的 items（可能是 0 筆）
        /// - item.option：用 item.option_guid 對應 staff_dayoff_option.GUID
        /// </remarks>
        private async Task<DayOffScheduleFormClass> GetFormForStaffCore_KeepAllDays(string form_name, string staff_id)
        {
            // SQL Controls
            var sql_form = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
            var sql_day = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
            var sql_item = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
            var sql_option = MethodClass.GetSQLControl<StaffDayOffOptionClass>();
            var sql_staff = MethodClass.GetSQLControl<StaffClass>();

            // form
            object[] obj_form = sql_form.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();
            if (obj_form == null) return null;
            DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();
            string form_guid = form.GUID;

            // staff_id -> staff_guid
            object[] obj_staff = sql_staff.GetRowsByDefult(null, "staff_id", staff_id).FirstOrDefault();
            if (obj_staff == null) return null;
            StaffClass staff = obj_staff.SQLToClass<StaffClass>();
            string staff_guid = staff.GUID;

            // all days
            List<object[]> obj_days = sql_day.GetRowsByDefult(null, "form_guid", form_guid);
            List<DayOffScheduleDayClass> days_all = obj_days.SQLToClass<DayOffScheduleDayClass>() ?? new List<DayOffScheduleDayClass>();
            days_all = days_all.OrderBy(d => d.date.StringToDateTime()).ToList();

            // all items (then filter staff)
            List<object[]> obj_items = sql_item.GetRowsByDefult(null, "form_guid", form_guid);
            List<DayOffScheduleItemClass> items_all = obj_items.SQLToClass<DayOffScheduleItemClass>() ?? new List<DayOffScheduleItemClass>();

            List<DayOffScheduleItemClass> staff_items = items_all
                .Where(i => i != null && string.Equals(i.staff_guid, staff_guid, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // options (staff only)
            List<object[]> obj_options = sql_option.GetRowsByDefult(null, "form_guid", form_guid);
            List<StaffDayOffOptionClass> options_all = obj_options.SQLToClass<StaffDayOffOptionClass>() ?? new List<StaffDayOffOptionClass>();

            List<StaffDayOffOptionClass> staff_options = options_all
                .Where(o => o != null && string.Equals(o.staff_guid, staff_guid, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var optionDict = staff_options
                .Where(o => o.GUID.StringIsEmpty() == false)
                .GroupBy(o => o.GUID)
                .ToDictionary(g => g.Key, g => g.First());

            // fill item.option (by option_guid)
            foreach (var item in staff_items)
            {
                item.staff = staff;

                if (item.option_guid.StringIsEmpty() == false && optionDict.TryGetValue(item.option_guid, out var opt))
                {
                    opt.NormalizeSelection();
                    item.option = opt;
                }
                else
                {
                    // 保留最小空 option（避免前端 null）
                    item.option = new StaffDayOffOptionClass
                    {
                        GUID = item.option_guid,
                        form_guid = form_guid,
                        staff_guid = staff_guid,
                        item_guid = item.GUID,
                        date = "",
                        suggested_dates = "[]",
                        is_any_date = "false",
                        // ✅ 不讀不改 assigned_shift，這裡也不塞值
                        can_full = "false",
                        can_half_am = "false",
                        can_half_pm = "false",
                        is_forbidden = "false",
                        is_force_ff = "false",
                        force_ff_at = "",
                        selected_full = "false",
                        selected_half_am = "false",
                        selected_half_pm = "false"
                    };
                }
            }

            // bucket items by day_guid
            var bucket = staff_items
                .GroupBy(i => i.day_guid ?? "")
                .ToDictionary(g => g.Key, g => g.ToList());

            // ✅ 保留全部 days：每一天都回（items 可能 0 筆）
            form.days = new List<DayOffScheduleDayClass>();
            foreach (var day in days_all)
            {
                var dayCopy = JsonSerializer.Deserialize<DayOffScheduleDayClass>(JsonSerializer.Serialize(day));
                if (dayCopy.items == null) dayCopy.items = new List<DayOffScheduleItemClass>();

                if (!dayCopy.GUID.StringIsEmpty() && bucket.TryGetValue(dayCopy.GUID, out var items))
                {
                    dayCopy.items = items
                        .OrderBy(i => i.date.StringToDateTime())
                        .ThenBy(i => i.position)
                        .ToList();
                }
                else
                {
                    dayCopy.items = new List<DayOffScheduleItemClass>();
                }

                form.days.Add(dayCopy);
            }

            // AnyDate 統計（只針對該 staff）
            int anyDateOptionCount = staff_options.Count(o => o.is_any_date == "true");
            form.any_date_option_count = anyDateOptionCount.ToString();
            form.any_date_quota_days = anyDateOptionCount.ToString();
            form.any_date_staff_count = anyDateOptionCount > 0 ? "1" : "0";
            form.any_date_used_days = "0";
            form.any_date_remaining_days = anyDateOptionCount.ToString();
            form.any_date_is_full = anyDateOptionCount <= 0 ? "true" : "false";

            return form;
        }

        #endregion

        private string BuildGroupTargetId(string group_guid, string form_guid, string group_num)
        {
            if (group_guid.StringIsEmpty() == false) return group_guid;
            if (form_guid.StringIsEmpty() == false && group_num.StringIsEmpty() == false) return $"{form_guid}:{group_num}";
            return "";
        }

        // ============================================================
        // group -> staff 清單（你可依你實際表結構微調）
        // ============================================================
        private async Task<string> GetGroupGuidByFormAndGroupNum(string form_guid, string group_num)
        {
            var sql_group = MethodClass.GetSQLControl<DayOffGroupClass>();

            string sql = $@"
                SELECT *
                FROM {sql_group.Database}.{sql_group.TableName}
                WHERE form_guid = @form_guid
                  AND group_num = @group_num
                LIMIT 1
            ";
            var parameters = new { form_guid = form_guid, group_num = group_num };
            List<object[]> rows = await sql_group.WriteCommandAsync(sql, parameters);
            var groups = rows.SQLToClass<DayOffGroupClass>();
            return groups?.FirstOrDefault()?.GUID ?? "";
        }

        private async Task<List<string>> GetStaffIdsByGroupGuid(string group_guid)
        {
            var sql_member = MethodClass.GetSQLControl<DayOffGroupMemberClass>();

            string sql = $@"
                SELECT *
                FROM {sql_member.Database}.{sql_member.TableName}
                WHERE group_guid = @group_guid
            ";
            var parameters = new { group_guid = group_guid };
            List<object[]> rows = await sql_member.WriteCommandAsync(sql, parameters);
            var members = rows.SQLToClass<DayOffGroupMemberClass>() ?? new List<DayOffGroupMemberClass>();

            return members
                .Where(x => x != null && x.staff_id.StringIsEmpty() == false)
                .Select(x => x.staff_id)
                .Distinct()
                .ToList();
        }

 

        /// <summary>
        /// 產生 6 碼數字字串
        /// </summary>
        private string Generate6DigitCode()
        {
            int value = System.Security.Cryptography.RandomNumberGenerator.GetInt32(0, 1000000);
            return value.ToString("D6");
        }

        /// <summary>
        /// 使用綁定碼完成 staff_id 與 line_user_id 綁定
        /// </summary>
        private async Task<string> BindByCode(string bindCode, string lineUserId)
        {
            var sql_bindCode = MethodClass.GetSQLControl<StaffLineBindCodeClass>();
            var sql_link = MethodClass.GetSQLControl<StaffLineLinkClass>();

            string nowStr = DateTime.Now.ToDateTimeString();

            // 1) 查 bind_code 是否有效
            string sql = $@"
                SELECT *
                FROM {sql_bindCode.Database}.{sql_bindCode.TableName}
                WHERE bind_code = @bind_code
                  AND is_used = '0'
                  AND (expire_at IS NULL OR expire_at = '' OR expire_at > @now)
                LIMIT 1
            ";
            var parameters = new
            {
                bind_code = bindCode,
                now = nowStr
            };

            List<object[]> rows = await sql_bindCode.WriteCommandAsync(sql, parameters);
            List<StaffLineBindCodeClass> list = rows.SQLToClass<StaffLineBindCodeClass>();
            var item = list?.FirstOrDefault();

            if (item == null)
            {
                return "綁定失敗：綁定碼無效、已使用或已過期。請回系統重新產生綁定碼。";
            }

            if (item.staff_id.StringIsEmpty())
            {
                return "綁定失敗：綁定碼資料異常（staff_id 空）。";
            }

            // 2) 更新 bind_code 為已使用
            item.is_used = "1";
            item.used_at = nowStr;
            item.used_line_user_id = lineUserId;

            sql_bindCode.UpdateByDefulteExtra(null, new List<StaffLineBindCodeClass> { item }.ClassToSQL<StaffLineBindCodeClass>());

            // 3) Upsert staff_line_link（同 staff_id 覆蓋 line_user_id）
            {
                string sql2 = $@"
                    SELECT *
                    FROM {sql_link.Database}.{sql_link.TableName}
                    WHERE staff_id = @staff_id
                    LIMIT 1
                ";
                var parameters2 = new { staff_id = item.staff_id };
                List<object[]> rows2 = await sql_link.WriteCommandAsync(sql2, parameters2);
                List<StaffLineLinkClass> list2 = rows2.SQLToClass<StaffLineLinkClass>();
                var link = list2?.FirstOrDefault();

                if (link == null)
                {
                    link = new StaffLineLinkClass
                    {
                        GUID = Guid.NewGuid().ToString(),
                        staff_id = item.staff_id,
                        line_user_id = lineUserId,
                        is_enabled = "1",
                        linked_at = nowStr,
                        created_at = nowStr,
                        updated_at = nowStr,
                        created_by = "",
                        updated_by = ""
                    };

                    sql_link.AddRow(null, link.ClassToSQL<StaffLineLinkClass>());
                }
                else
                {
                    // 若之前未填 linked_at，可補
                    if (link.linked_at.StringIsEmpty()) link.linked_at = nowStr;

                    link.line_user_id = lineUserId;
                    link.is_enabled = "1";
                    link.updated_at = nowStr;
                    link.updated_by = "";

                    sql_link.UpdateByDefulteExtra(null, new List<StaffLineLinkClass> { link }.ClassToSQL<StaffLineLinkClass>());
                }
            }

            return $"綁定成功：{item.staff_id} 已完成 LINE 綁定。";
        }


        /// <summary>
        /// LINE Reply Message（回覆文字）
        /// </summary>
        private async Task ReplyText(string channelAccessToken, string replyToken, string text)
        {
            try
            {
                var payload = new
                {
                    replyToken = replyToken,
                    messages = new[]
                    {
                new { type = "text", text = text }
            }
                };

                string json = System.Text.Json.JsonSerializer.Serialize(payload);

                using var client = new System.Net.Http.HttpClient();
                using var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, "https://api.line.me/v2/bot/message/reply");
                req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", channelAccessToken);
                req.Content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

                using var res = await client.SendAsync(req);
                // 不強制 throw，避免 webhook 失敗
            }
            catch
            {
                // ignore
            }
        }
        private async Task ReplyText_WithLog(string reqId, string channelAccessToken, string replyToken, string text)
        {
            try
            {
                await ReplyText(channelAccessToken, replyToken, text);
                Logger.Log("LINE_WEBHOOK", $"[{reqId}] reply_ok");
            }
            catch (Exception ex)
            {
                Logger.Log("LINE_WEBHOOK", $"[{reqId}] reply_fail: {ex.Message}");
            }
        }
        private string MaskUserId(string userId)
        {
            if (userId.StringIsEmpty()) return "";
            if (userId.Length <= 6) return "***";
            return userId.Substring(0, 3) + "..." + userId.Substring(userId.Length - 3, 3);
        }

        private string SafeText(string text)
        {
            // 避免 log 太長、也避免把綁定碼全曝光（你可自行調整）
            text = (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
            if (text.Length > 60) text = text.Substring(0, 60) + "...";
            return text;
        }

        // ============================================================
        // Helper：解析 staff_id 清單（支援 staff_id 多筆 + staff_ids csv）
        // ============================================================
        private List<string> ExtractStaffIds(List<string> valueAry)
        {
            var list = new List<string>();
            if (valueAry == null) return list;

            foreach (var s in valueAry)
            {
                if (s == null) continue;
                if (s.StartsWith("staff_id=", StringComparison.OrdinalIgnoreCase))
                {
                    var v = s.Split('=')[1]?.Trim();
                    if (v.StringIsEmpty() == false) list.Add(v);
                }
            }

            var staffIdsCsv = valueAry
                .FirstOrDefault(x => x != null && x.StartsWith("staff_ids=", StringComparison.OrdinalIgnoreCase))
                ?.Split('=')[1];

            if (staffIdsCsv.StringIsEmpty() == false)
            {
                var arr = staffIdsCsv
                    .Split(new[] { ',', '，', ';', '；', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => x.StringIsEmpty() == false);
                list.AddRange(arr);
            }

            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// 讀取 line_settings.setting_value（若不存在則回傳空字串）
        /// </summary>
        private async Task<string> GetLineSettingValue(string key)
        {
            if (key.StringIsEmpty()) return "";

            var sql_setting = MethodClass.GetSQLControl<LineSettingClass>();
            string sql = $@"
                SELECT *
                FROM {sql_setting.Database}.{sql_setting.TableName}
                WHERE setting_key = @setting_key
                  AND is_enabled = '1'
                LIMIT 1
            ";
            var parameters = new { setting_key = key };

            List<object[]> rows = await sql_setting.WriteCommandAsync(sql, parameters);
            List<LineSettingClass> list = rows.SQLToClass<LineSettingClass>();
            return list?.FirstOrDefault()?.setting_value ?? "";
        }

        private bool VerifyLineSignature(string channelSecret, string body, string signature)
        {
            if (signature.StringIsEmpty()) return false;

            byte[] key = Encoding.UTF8.GetBytes(channelSecret);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);

            using var hmac = new HMACSHA256(key);
            byte[] hash = hmac.ComputeHash(bodyBytes);
            string computed = Convert.ToBase64String(hash);

            // 固定時間比較
            return FixedTimeEquals(computed, signature);
        }

        private bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            byte[] ba = Encoding.UTF8.GetBytes(a);
            byte[] bb = Encoding.UTF8.GetBytes(b);
            if (ba.Length != bb.Length) return false;
            return CryptographicOperations.FixedTimeEquals(ba, bb);
        }

        /// <summary>
        /// 解析綁定碼：支援
        /// 1) 綁定 123456
        /// 2) 綁定:123456 / 綁定：123456
        /// 3) 直接輸入 6 位數字：123456
        /// </summary>
        private string ParseBindCode(string text)
        {
            text = (text ?? "").Trim();

            // 直接 6 位數字
            if (text.Length == 6 && text.All(char.IsDigit))
            {
                return text;
            }

            // 綁定 開頭
            if (text.StartsWith("綁定"))
            {
                string s = text.Substring(2).Trim(); // 去掉「綁定」
                s = s.Replace(":", "").Replace("：", "").Trim();
                s = s.Replace(" ", "").Replace("\t", "");

                if (s.Length == 6 && s.All(char.IsDigit))
                {
                    return s;
                }
            }

            return "";
        }


    }
}

