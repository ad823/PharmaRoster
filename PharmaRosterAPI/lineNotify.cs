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
    public class lineNotify : ControllerBase
    {
        /// <summary>
        /// LINE 訊息推播（指定多位 staff_id）
        /// </summary>
        /// <remarks>
        /// ## 📘 功能說明  
        /// 本 API 用於 LINE 訊息推播，僅支援「指定多位 staff_id」逐一推播。  
        /// 系統會依每位 staff_id 查詢 <c>staff_line_link</c> 取得 <c>line_user_id</c> 後呼叫 LINE Push API。  
        /// 每一次推播（每位收件者）都會寫入 <c>line_push_log</c>（成功/失敗都寫）。  
        /// 
        ///## 🌐 API URL  
        /// `POST /phar_roster_api/lineNotify/push`
        /// 
        /// ## ✅ Input（ValueAry）
        /// - <c>message</c>（必填）
        /// - staff 清單（二選一，至少一種）：
        ///   - 多筆：<c>staff_id=A001</c>、<c>staff_id=A002</c>...
        ///   - 單筆：<c>staff_ids=A001,A002,A003</c>
        ///
        /// ## ⚙️ 行為
        /// 1. 取得 message 與 staff_id 清單
        /// 2. 從 <c>line_settings</c> 取得 <c>channel_access_token</c>
        /// 3. 逐一 staff_id：
        ///    - 查 <c>staff_line_link</c>（is_enabled=1）取得 line_user_id
        ///    - 呼叫 LINE Push API 推播
        ///    - 寫入 <c>line_push_log</c>（成功/失敗都寫）
        ///
        /// ## 📥 Request JSON 範例（多筆 staff_id）
        /// ```json
        /// {
        ///   "Method": "push",
        ///   "ValueAry": [
        ///     "staff_id=A001",
        ///     "staff_id=A002",
        ///     "staff_id=A003",
        ///     "message=請於今日 17:00 前完成排休填寫"
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📥 Request JSON 範例（staff_ids 逗號）
        /// ```json
        /// {
        ///   "Method": "push",
        ///   "ValueAry": [
        ///     "staff_ids=A001,A002,A003",
        ///     "message=本組排休已開放，請儘速填寫"
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 成功回傳範例
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "push",
        ///   "Result": "推播完成，成功 2 筆，失敗 1 筆",
        ///   "Data": {
        ///     "targets": 3,
        ///     "success": 2,
        ///     "fail": 1
        ///   }
        /// }
        /// ```
        ///
        /// ## ❌ 錯誤回傳範例
        /// - message 空：
        /// ```json
        /// { "Code": -200, "Result": "message 不可為空" }
        /// ```
        /// - staff_id 清單為空：
        /// ```json
        /// { "Code": -200, "Result": "staff_id 不可為空（請提供 staff_id 或 staff_ids）" }
        /// ```
        /// - 未設定 channel_access_token：
        /// ```json
        /// { "Code": -200, "Result": "未設定 channel_access_token（line_settings）" }
        /// ```
        /// </remarks>
        [HttpPost("push")]
        public async Task<string> push([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "push";

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry?.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string message = GetVal("message");
                if (message.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "message 不可為空";
                    return returnData.JsonSerializationt();
                }

                // 解析 staff 清單：支援 staff_id 多筆、或 staff_ids=逗號
                List<string> staffIds = ExtractStaffIds(returnData.ValueAry);

                if (staffIds == null || staffIds.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "staff_id 不可為空（請提供 staff_id 或 staff_ids）";
                    return returnData.JsonSerializationt();
                }

                // 取得 channel_access_token
                string channelAccessToken = await GetLineSettingValue("channel_access_token");
                if (channelAccessToken.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未設定 channel_access_token（line_settings）";
                    return returnData.JsonSerializationt();
                }

                int success = 0;
                int fail = 0;

                foreach (var sid in staffIds)
                {
                    var r = await PushToOneStaffAndLog(channelAccessToken, sid, message);
                    if (r.is_success) success++;
                    else fail++;
                }

                returnData.Code = 200;
                returnData.Data = new
                {
                    targets = staffIds.Count,
                    success = success,
                    fail = fail
                };
                returnData.Result = $"推播完成";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }

        // ============================================================
        // 解析 staff 清單
        // ============================================================

        private List<string> ExtractStaffIds(List<string> valueAry)
        {
            var list = new List<string>();
            if (valueAry == null) return list;

            // 1) 收集所有 staff_id=xxx（可多筆）
            foreach (var s in valueAry)
            {
                if (s == null) continue;
                if (s.StartsWith("staff_id=", StringComparison.OrdinalIgnoreCase))
                {
                    var v = s.Split('=')[1]?.Trim();
                    if (v.StringIsEmpty() == false) list.Add(v);
                }
            }

            // 2) staff_ids=A001,A002（可選）
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

            // 去重
            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        // ============================================================
        // 單人推播 + 寫 line_push_log（成功/失敗都寫）
        // ============================================================

        private async Task<(bool is_success, string error_message)> PushToOneStaffAndLog(
            string channelAccessToken,
            string staff_id,
            string message)
        {
            string nowStr = DateTime.Now.ToDateTimeString();

            var sql_link = MethodClass.GetSQLControl<StaffLineLinkClass>();
            var sql_log = MethodClass.GetSQLControl<LinePushLogClass>();

            // 1) 查 line_user_id
            string sql_get_link = $@"
                SELECT *
                FROM {sql_link.Database}.{sql_link.TableName}
                WHERE staff_id = @staff_id
                  AND is_enabled = '1'
                LIMIT 1
            ";
            var p_link = new { staff_id = staff_id };
            List<object[]> rows_link = await sql_link.WriteCommandAsync(sql_get_link, p_link);
            var links = rows_link.SQLToClass<StaffLineLinkClass>();
            var link = links?.FirstOrDefault();

            string line_user_id = link?.line_user_id ?? "";

            // 2) 未綁定：寫 log fail
            if (line_user_id.StringIsEmpty())
            {
                var logFail = new LinePushLogClass
                {
                    GUID = Guid.NewGuid().ToString(),
                    target_type = "staff",
                    target_id = staff_id,
                    staff_id = staff_id,
                    line_user_id = "",
                    message = message,
                    send_status = "fail",
                    http_status = "",
                    error_message = "未找到 staff_line_link（尚未綁定或未啟用）",
                    created_at = nowStr
                };
                sql_log.AddRow(null, logFail.ClassToSQL<LinePushLogClass>());
                return (false, logFail.error_message);
            }

            // 3) Push
            bool isSuccess = false;
            string httpStatus = "";
            string errMsg = "";

            try
            {
                var payload = new
                {
                    to = line_user_id,
                    messages = new[]
                    {
                        new { type = "text", text = message }
                    }
                };

                string json = System.Text.Json.JsonSerializer.Serialize(payload);

                using var client = new HttpClient();
                using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.line.me/v2/bot/message/push");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", channelAccessToken);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using var res = await client.SendAsync(req);
                httpStatus = ((int)res.StatusCode).ToString();

                if (res.IsSuccessStatusCode)
                {
                    isSuccess = true;
                }
                else
                {
                    errMsg = await res.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                errMsg = ex.Message;
            }

            // 4) 寫 log（成功/失敗都寫）
            var log = new LinePushLogClass
            {
                GUID = Guid.NewGuid().ToString(),
                target_type = "staff",
                target_id = staff_id,
                staff_id = staff_id,
                line_user_id = line_user_id,
                message = message,
                send_status = isSuccess ? "success" : "fail",
                http_status = httpStatus,
                error_message = isSuccess ? "" : errMsg,
                created_at = nowStr
            };
            sql_log.AddRow(null, log.ClassToSQL<LinePushLogClass>());

            return (isSuccess, errMsg);
        }

        // ============================================================
        // line_settings：取 channel_access_token
        // ============================================================

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
            var p = new { setting_key = key };
            List<object[]> rows = await sql_setting.WriteCommandAsync(sql, p);
            var list = rows.SQLToClass<LineSettingClass>();
            return list?.FirstOrDefault()?.setting_value ?? "";
        }
    }

}

