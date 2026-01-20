using Basic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyOffice;
using MySql.Data.MySqlClient;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using PharmaRosterLib;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PharmaRosterAPI
{
    [Route("phar_roster_api/[controller]")]
    [ApiController]
    public class auth : ControllerBase
    {
        /// <summary>
        /// 初始化系統資料表 (建立 StaffAuthClass 結構)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 本 API 用於系統初始設定階段，檢查並自動建立必要的資料表結構。  
        /// 目前僅包含：  
        /// - <c>StaffAuthClass</c>：人員帳號授權表 (帳號、密碼雜湊、登入狀態等)  
        ///
        /// ## ⚙️ 功能說明  
        /// - 會呼叫 <c>PharmaRosterLib.MethodClass.CheckCreatTable&lt;T&gt;()</c> 檢查資料表是否存在。  
        /// - 若資料表不存在，系統將自動建立對應結構。  
        /// - 結果會以 Table 結構清單的方式回傳。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "init",
        ///   "ValueAry": [],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "init",
        ///   "Result": "初始化 StaffAuthClass 資料表完成",
        ///   "TimeTaken": "38ms",
        ///   "Data": [
        ///     {
        ///       "TableName": "staff_auth",
        ///       "Columns": [
        ///         { "Name": "GUID", "Type": "VARCHAR(50)", "Key": "PRIMARY" },
        ///         { "Name": "account", "Type": "VARCHAR(50)", "Key": "UNIQUE" },
        ///         { "Name": "password_hash", "Type": "VARCHAR(255)" },
        ///         ...
        ///       ]
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "init",
        ///   "Result": "Exception: 無法連線至資料庫"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - 僅用於系統第一次部署或資料庫結構更新時執行。  
        /// - 不會刪除或覆寫既有資料。  
        /// - 回傳的 Data 內含建立或驗證過的資料表資訊。  
        /// - 執行時間依資料庫連線狀況而定。  
        /// </remarks>
        /// <param name="returnData">封裝請求與回應的標準物件，包含 Method、Data 等欄位</param>
        /// <returns>JSON 格式回傳，包含初始化結果與資料表結構資訊</returns>
        [HttpPost("init")]
        public string init([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "init";

            try
            {
                List<Table> tables = new List<Table>();
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<StaffAuthClass>());


                returnData.Code = 200;
                returnData.Data = tables;
                returnData.Result = "初始化 StaffAuthClass 資料表完成";
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
        /// 使用者註冊 (建立 StaffAuthClass 帳號授權資料)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 用於建立新使用者登入帳號，系統會驗證人員資料是否存在、檢查帳號與 staff_id 是否重複，  
        /// 並自動產生密碼雜湊與 Salt 儲存至 <c>StaffAuthClass</c> 資料表中。
        ///
        /// ## ⚙️ 功能說明  
        /// 1. 驗證輸入參數：`staff_id`、`account`、`password`  
        /// 2. 檢查 <c>StaffClass</c> 是否存在對應人員資料。  
        /// 3. 檢查帳號與員工 ID 是否已被註冊。  
        /// 4. 使用 <c>PasswordHashHelper.GenerateSalt()</c> 產生 Salt，並以 SHA256 雜湊儲存。  
        /// 5. 寫入 <c>StaffAuthClass</c> 資料表，建立新登入帳號。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "register",
        ///   "ValueAry": [
        ///     "staff_id=1101",
        ///     "account=pharma01",
        ///     "password=abc12345"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 🔍 參數說明  
        /// | 參數名稱 | 類型 | 必填 | 限制 | 說明 |
        /// |------------|------|------|------|------|
        /// | staff_id | string | ✅ | 不可空白 | 對應 <c>StaffClass</c> 的員工代號 |
        /// | account | string | ✅ | 不可空白、長度 < 50 | 登入帳號 |
        /// | password | string | ✅ | 不可空白、長度 < 50 | 登入密碼 (系統將自動雜湊後儲存) |
        ///
        /// ## 🔐 加密機制說明  
        /// 系統會於註冊時：
        /// - 產生亂數 `salt`  
        /// - 將密碼與 salt 經由 <c>PasswordHashHelper.HashPassword(password, salt)</c> 產生安全雜湊值  
        /// - 寫入資料表欄位：
        ///   - `password_hash` → 雜湊後密碼  
        ///   - `password_salt` → 對應的 Salt 值  
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "register",
        ///   "Result": "註冊成功",
        ///   "TimeTaken": "58ms"
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少參數：  
        /// ```json
        /// { "Code": -200, "Result": "Data 不能為空" }
        /// ```
        ///
        /// - 欄位無效：  
        /// ```json
        /// { "Code": -200, "Result": "account :  ,資料不合法" }
        /// ```
        ///
        /// - 超過長度限制：  
        /// ```json
        /// { "Code": -200, "Result": "password 長度不可超過 50 個字元" }
        /// ```
        ///
        /// - 人員不存在：  
        /// ```json
        /// { "Code": -200, "Result": "staff_id : 1101 ,資料不存在，請先建立人員資料" }
        /// ```
        ///
        /// - 帳號已存在：  
        /// ```json
        /// { "Code": -200, "Result": "account : pharma01 此帳號已經註冊，無法重複註冊" }
        /// ```
        ///
        /// - 系統例外：  
        /// ```json
        /// { "Code": -200, "Result": "Exception : 資料庫連線逾時" }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - 註冊前必須確保該員工已存在於 <c>StaffClass</c>。  
        /// - 帳號與員工代碼不可重複。  
        /// - 密碼僅會以雜湊值儲存，不會明碼保存。  
        /// - <c>last_login_at</c>、<c>created_at</c>、<c>updated_at</c> 皆於建立時自動填入。  
        /// </remarks>
        /// <param name="returnData">封裝 API 請求內容與回應物件，包含 ValueAry 與 Data</param>
        /// <returns>回傳 JSON 格式字串，描述註冊成功或錯誤資訊</returns>
        [HttpPost]
        [Route("register")]
        public string register(returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "register";

            try
            {
                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                string GetVal(string key) =>
                       returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                       ?.Split('=')[1];

                string staff_id = GetVal("staff_id") ?? "";
                string account = GetVal("account") ?? "";
                string password = GetVal("password") ?? "";

                if(staff_id.StringIsEmpty() == true)
                {
                    returnData.Code = -200;
                    returnData.Result = $"staff_id : {staff_id} ,資料不合法";
                    return returnData.JsonSerializationt();
                }
                if (account.StringIsEmpty() == true)
                {
                    returnData.Code = -200;
                    returnData.Result = $"account : {account} ,資料不合法";
                    return returnData.JsonSerializationt();
                }
                if (password.StringIsEmpty() == true)
                {
                    returnData.Code = -200;
                    returnData.Result = $"password : {password} ,資料不合法";
                    return returnData.JsonSerializationt();
                }
                if(password.Length >= 50)
                {
                    returnData.Code = -200;
                    returnData.Result = $"password 長度不可超過 50 個字元";
                    return returnData.JsonSerializationt();
                }
                if (account.Length >= 50)
                {
                    returnData.Code = -200;
                    returnData.Result = $"account 長度不可超過 50 個字元";
                    return returnData.JsonSerializationt();
                }
                string salt = PasswordHashHelper.GenerateSalt();
                string hash = PasswordHashHelper.HashPassword(password, salt);

                StaffClass staffClass = staff.GetStaffs(new List<string>() { $"staff_id={staff_id}" }).staffClasses.FirstOrDefault();
                if(staffClass == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"staff_id : {staff_id} ,資料不存在，請先建立人員資料";
                    return returnData.JsonSerializationt();
                }

                var sql_staffAuthClass = MethodClass.GetSQLControl<StaffAuthClass>();

                DataTable dataTable = sql_staffAuthClass.WtrteCommandAndExecuteReader($"select * from {sql_staffAuthClass.Database}.{sql_staffAuthClass.TableName} where staff_id = '{staff_id}'");
                StaffAuthClass authClass = dataTable.SQLToClass<StaffAuthClass>().FirstOrDefault();
                if (authClass != null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"staff_id : {staff_id} 此ID已經註冊，無法重複註冊";
                    return returnData.JsonSerializationt();
                }
                dataTable = sql_staffAuthClass.WtrteCommandAndExecuteReader($"select * from {sql_staffAuthClass.Database}.{sql_staffAuthClass.TableName} where account = '{account}'");
                authClass = dataTable.SQLToClass<StaffAuthClass>().FirstOrDefault();
                if (authClass != null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"account : {account} 此帳號已經註冊，無法重複註冊";
                    return returnData.JsonSerializationt();
                }


                StaffAuthClass staffAuthClass = new StaffAuthClass()
                {
                    GUID = Guid.NewGuid().ToString(),
                    staff_id = staff_id,
                    account = account,
                    password_hash = hash,
                    password_salt = salt,
                    is_locked = "N",
                    fail_count = "0",
                    last_login_at = DateTime.Now.ToDateTimeString(),
                    last_logout_at = DateTime.Now.ToDateTimeString(),
                    created_at = DateTime.Now.ToDateTimeString(),
                    updated_at = DateTime.Now.ToDateTimeString(),
                };
                sql_staffAuthClass.AddRow(null, staffAuthClass.ClassToSQL<StaffAuthClass>());
                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Result = "註冊成功";
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
        /// 使用者登入驗證 (含帳號鎖定與 JWT 權杖產生)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 驗證使用者帳號與密碼，並依安全機制產生 JWT 權杖 (Token)。  
        /// 同時具備錯誤次數鎖定、自動解鎖、密碼雜湊比對與登入紀錄更新功能。
        ///
        /// ## ⚙️ 功能說明  
        /// 1. 驗證輸入參數：`account`、`password`。  
        /// 2. 查詢 <c>StaffAuthClass</c> 取得對應帳號。  
        /// 3. 若帳號鎖定則檢查是否已超過 3 分鐘 — 未滿則提示剩餘秒數，滿 3 分鐘自動解鎖。  
        /// 4. 使用 <c>PasswordHashHelper.VerifyPassword()</c> 進行密碼雜湊驗證。  
        /// 5. 若密碼錯誤，累積 `fail_count`，達 5 次即自動鎖定帳號。  
        /// 6. 登入成功後：
        ///     - 重設錯誤次數
        ///     - 更新最後登入時間 (`last_login_at`)
        ///     - 產生 JWT Token (含使用者角色與有效時間)
        /// 7. 回傳登入結果與 Token 給前端。
        ///
        /// ## 🔐 安全機制  
        /// - 密碼採用 Salt + SHA256 雜湊驗證，不以明碼儲存。  
        /// - 錯誤 5 次 → 帳號鎖定 3 分鐘。  
        /// - 3 分鐘後自動解鎖 (`updated_at` 時間為依據)。  
        /// - JWT Token 有效時間預設 3600 秒 (1 小時)。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "login",
        ///   "ValueAry": [
        ///     "account=pharma01",
        ///     "password=abc12345"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 🔍 參數說明  
        /// | 參數名稱 | 類型 | 必填 | 限制 | 說明 |
        /// |------------|------|------|------|------|
        /// | account | string | ✅ | 不可空白、長度 < 50 | 登入帳號 |
        /// | password | string | ✅ | 不可空白、長度 < 50 | 登入密碼 (明文傳入，由伺服端進行雜湊驗證) |
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "login",
        ///   "Result": "登入成功",
        ///   "Token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        ///   "Expires_in": "3600",
        ///   "TimeTaken": "72ms"
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少參數：  
        /// ```json
        /// { "Code": -200, "Result": "Data 不能為空" }
        /// ```
        ///
        /// - 帳號不存在：  
        /// ```json
        /// { "Code": -200, "Result": "account : pharma01 帳號不存在" }
        /// ```
        ///
        /// - 密碼錯誤：  
        /// ```json
        /// { "Code": -200, "Result": "密碼錯誤 (3/5)" }
        /// ```
        ///
        /// - 帳號被鎖定 (剩餘秒數)：  
        /// ```json
        /// { "Code": -200, "Result": "帳號已鎖定，請 85 秒後再試" }
        /// ```
        ///
        /// - 系統例外：  
        /// ```json
        /// { "Code": -200, "Result": "Exception : 無法連線至資料庫" }
        /// ```
        ///
        /// ## 🧩 JWT 權杖內容範例 (解碼後 Payload)
        /// ```json
        /// {
        ///   "staff_id": "1101",
        ///   "account": "pharma01",
        ///   "role": "藥師",
        ///   "exp": 1735678891
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - 錯誤次數達 5 次後帳號會鎖定 3 分鐘。  
        /// - Token 有效時間 (`Expires_in`) 為秒數，預設 3600。  
        /// - 請於前端儲存 Token 並於後續 API 呼叫中以 Header 傳遞：  
        ///   ```
        ///   Authorization: Bearer <Token>
        ///   ```  
        /// - 每次登入成功都會更新 `last_login_at` 與 `updated_at`。  
        /// </remarks>
        /// <param name="returnData">封裝 API 請求與回應內容，包含 ValueAry、Data、Token 等欄位</param>
        /// <returns>JSON 格式字串，包含登入狀態、JWT Token 與到期時間</returns>
        [HttpPost]
        [Route("login")]
        public string login(returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "login";

            try
            {
                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                string GetVal(string key) =>
                    returnData.ValueAry
                        .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                        ?.Split('=')[1];

                string account = GetVal("account") ?? "";
                string password = GetVal("password") ?? "";

                if (account.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = $"account : {account} ,資料不合法";
                    return returnData.JsonSerializationt();
                }

                if (password.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = $"password 資料不合法";
                    return returnData.JsonSerializationt();
                }

                // === 2. 取得 Auth 資料 ===
                var sql_staffAuthClass = MethodClass.GetSQLControl<StaffAuthClass>();
                string commacd = $"select * from {sql_staffAuthClass.Database}.{sql_staffAuthClass.TableName} where account = '{account}'";
                DataTable dataTable = sql_staffAuthClass
                    .WtrteCommandAndExecuteReader(
                        commacd
                    );

                StaffAuthClass authClass = dataTable.SQLToClass<StaffAuthClass>().FirstOrDefault();

                if (authClass == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"account : {account} 帳號不存在";
                    return returnData.JsonSerializationt();
                }

                // === 3. 檢查是否鎖定（含 3 分鐘自動解鎖） ===
                if (authClass.is_locked == "Y")
                {
                    DateTime lockTime = authClass.updated_at.StringToDateTime();
                    TimeSpan lockSpan = DateTime.Now - lockTime;

                    if (lockSpan.TotalMinutes >= 3)
                    {
                        authClass.is_locked = "N";
                        authClass.fail_count = "0";
                        authClass.updated_at = DateTime.Now.ToDateTimeString();

                        sql_staffAuthClass.UpdateByDefulteExtra(
                            null,
                            authClass.ClassToSQL<StaffAuthClass>()
                        );
                    }
                    else
                    {
                        int remainSeconds = (int)(180 - lockSpan.TotalSeconds);
                        if (remainSeconds < 0) remainSeconds = 0;

                        returnData.Code = -200;
                        returnData.Result = $"帳號已鎖定，請 {remainSeconds} 秒後再試";
                        return returnData.JsonSerializationt();
                    }
                }

                // === 4. 驗證密碼 ===
                bool isPasswordValid = PasswordHashHelper.VerifyPassword(
                    password,
                    authClass.password_salt,
                    authClass.password_hash
                );

                if (!isPasswordValid)
                {
                    int failCount = authClass.fail_count.StringToInt32() + 1;
                    authClass.fail_count = failCount.ToString();

                    if (failCount >= 5)
                    {
                        authClass.is_locked = "Y";
                    }

                    authClass.updated_at = DateTime.Now.ToDateTimeString();

                    sql_staffAuthClass.UpdateByDefulteExtra(
                        null,
                        authClass.ClassToSQL<StaffAuthClass>()
                    );

                    returnData.Code = -200;
                    returnData.Result = $"密碼錯誤 ({failCount}/5)";
                    return returnData.JsonSerializationt();
                }

                // === 5. 登入成功：更新狀態 ===
                authClass.fail_count = "0";
                authClass.last_login_at = DateTime.Now.ToDateTimeString();
                authClass.last_logout_at = DateTime.Now.ToDateTimeString();
                authClass.updated_at = DateTime.Now.ToDateTimeString();

                sql_staffAuthClass.UpdateByDefulteExtra(
                    null,
                    authClass.ClassToSQL<StaffAuthClass>()
                );

                // === 6. 取得 Staff 資料 ===
                StaffClass staffClass =
                    staff.GetStaffs(new List<string>() { $"staff_id={authClass.staff_id}" })
                         .staffClasses
                         .FirstOrDefault();

                // === 7. 產生 JWT Token ===
                string jwtToken = JwtHelper.GenerateToken(
                    staffId: authClass.staff_id,
                    account: authClass.account,
                    role: staffClass?.role ?? ""
                );

                // === 8. 回傳登入成功 + JWT ===
                returnData.Code = 200;
                returnData.Result = "登入成功";
                returnData.Token = jwtToken;
                returnData.Expires_in = "3600";
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
        /// 使用者修改密碼 (需 JWT 驗證)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 允許已登入使用者修改登入密碼。  
        /// 系統會驗證舊密碼是否正確，並使用新的 Salt + Hash 重新加密儲存。  
        /// 修改成功後，舊的 JWT Token 將自動失效，使用者需重新登入。
        ///
        /// ## ⚙️ 功能說明  
        /// 1. 驗證輸入參數：`old_password`、`new_password`。  
        /// 2. 從 JWT 權杖中解析出 `staff_id`。  
        /// 3. 查詢 <c>StaffAuthClass</c> 取得對應使用者的帳號資訊。  
        /// 4. 使用 <c>PasswordHashHelper.VerifyPassword()</c> 驗證舊密碼是否正確。  
        /// 5. 若正確 → 產生新 Salt 與 Hash，更新資料庫。  
        /// 6. 更新完成後強制登出，需重新登入以啟用新密碼。  
        ///
        /// ## 🔐 安全設計  
        /// - 採用 <c>[Authorize]</c> 屬性，僅限持有效 JWT Token 的使用者呼叫。  
        /// - 密碼皆採用 Salt + SHA256 雜湊，資料庫不存明碼。  
        /// - 每次修改密碼會更新：
        ///   - `password_salt`  
        ///   - `password_hash`  
        ///   - `last_login_at`  
        ///   - `updated_at`  
        ///   - 並重置 `fail_count` 與解除鎖定狀態。  
        /// - 修改成功後，原 Token 即視為失效。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "change_password",
        ///   "ValueAry": [
        ///     "old_password=abc12345",
        ///     "new_password=newPass789!"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 🔍 參數說明  
        /// | 參數名稱 | 類型 | 必填 | 限制 | 說明 |
        /// |------------|------|------|------|------|
        /// | old_password | string | ✅ | 不可空白 | 使用者目前密碼 |
        /// | new_password | string | ✅ | 不可空白、長度 &lt; 50 | 新密碼 (系統將自動雜湊後儲存) |
        ///
        /// ## 🔑 Header 範例  
        /// 此 API 需傳入 JWT Token：  
        /// ```
        /// Authorization: Bearer &lt;JWT Token&gt;
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "change_password",
        ///   "Result": "密碼修改成功，請重新登入",
        ///   "TimeTaken": "65ms"
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 缺少參數：  
        /// ```json
        /// { "Code": -200, "Result": "Data 不能為空" }
        /// ```
        ///
        /// - 舊密碼錯誤：  
        /// ```json
        /// { "Code": -200, "Result": "原密碼錯誤" }
        /// ```
        ///
        /// - 新密碼太長：  
        /// ```json
        /// { "Code": -200, "Result": "new_password 長度不可超過 50" }
        /// ```
        ///
        /// - JWT 無法解析：  
        /// ```json
        /// { "Code": -200, "Result": "JWT 無法解析 staff_id" }
        /// ```
        ///
        /// - 授權資料不存在：  
        /// ```json
        /// { "Code": -200, "Result": "帳號授權資料不存在" }
        /// ```
        ///
        /// - 系統例外：  
        /// ```json
        /// { "Code": -200, "Result": "Exception : 資料庫連線失敗" }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - 修改密碼後舊 Token 會立刻失效。  
        /// - 修改後必須重新登入以獲取新的 JWT Token。  
        /// - 新密碼不可與舊密碼相同（可於前端或後端自行加上驗證）。  
        /// - 系統會自動解除鎖定並重設錯誤計數。  
        /// </remarks>
        /// <param name="returnData">封裝 API 請求與回應內容，包含 ValueAry、Data 與 JWT Token</param>
        /// <returns>JSON 格式字串，描述修改密碼的成功或錯誤結果</returns>
        [Authorize]
        [HttpPost]
        [Route("change_password")]
        public string change_password(returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "change_password";

            try
            {
                // === 1. 基本檢核 ===
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                string GetVal(string key) =>
                    returnData.ValueAry
                        .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                        ?.Split('=')[1];

                string oldPassword = GetVal("old_password") ?? "";
                string newPassword = GetVal("new_password") ?? "";

                if (oldPassword.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "old_password 不可為空";
                    return returnData.JsonSerializationt();
                }

                if (newPassword.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "new_password 不可為空";
                    return returnData.JsonSerializationt();
                }

                if (newPassword.Length >= 50)
                {
                    returnData.Code = -200;
                    returnData.Result = "new_password 長度不可超過 50";
                    return returnData.JsonSerializationt();
                }

                // === 2. 從 JWT 取 staff_id ===
                string staffId = User.Claims
                    .FirstOrDefault(c => c.Type == "staff_id")?.Value;

                if (staffId.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "JWT 無法解析 staff_id";
                    return returnData.JsonSerializationt();
                }

                // === 3. 取得 StaffAuth ===
                var sql = MethodClass.GetSQLControl<StaffAuthClass>();

                DataTable dt = sql.WtrteCommandAndExecuteReader(
                    $"select * from {sql.Database}.{sql.TableName} where staff_id = '{staffId}'"
                );

                StaffAuthClass auth = dt.SQLToClass<StaffAuthClass>().FirstOrDefault();

                if (auth == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "帳號授權資料不存在";
                    return returnData.JsonSerializationt();
                }

                // === 4. 驗證原密碼 ===
                bool oldPwdValid = PasswordHashHelper.VerifyPassword(
                    oldPassword,
                    auth.password_salt,
                    auth.password_hash
                );

                if (!oldPwdValid)
                {
                    returnData.Code = -200;
                    returnData.Result = "原密碼錯誤";
                    return returnData.JsonSerializationt();
                }

                // === 5. 產生新 Salt + Hash ===
                string newSalt = PasswordHashHelper.GenerateSalt();
                string newHash = PasswordHashHelper.HashPassword(newPassword, newSalt);

                auth.password_salt = newSalt;
                auth.password_hash = newHash;

                // 🔒 強制舊 JWT 全失效
                auth.last_login_at = DateTime.Now.ToDateTimeString();
                auth.updated_at = DateTime.Now.ToDateTimeString();
                auth.fail_count = "0";
                auth.is_locked = "N";

                sql.UpdateByDefulteExtra(null, auth.ClassToSQL<StaffAuthClass>());

                // === 6. 回傳成功（前端需重新登入） ===
                returnData.Code = 200;
                returnData.Result = "密碼修改成功，請重新登入";
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
        /// 管理者重設使用者密碼 (需 JWT 授權)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 由授權使用者（例如管理員或系統帳號）重設指定員工的登入密碼。  
        /// 系統會自動產生一組臨時密碼 (temporary password)，並更新資料庫雜湊與 Salt。  
        /// 目的是協助使用者忘記密碼或帳號鎖定時重新登入。
        ///
        /// ## ⚙️ 功能說明  
        /// 1. 驗證呼叫者 JWT Token 是否有效。  
        /// 2. 接收查詢條件 (`staff_id` 或 `account`)。  
        /// 3. 查詢 <c>StaffAuthClass</c> 取得授權資料。  
        /// 4. 產生隨機臨時密碼，並以新 Salt + Hash 儲存至資料庫。  
        /// 5. 重設後自動解除鎖定 (`is_locked = N`)、清除錯誤計數 (`fail_count = 0`)、更新時間。  
        /// 6. 回傳臨時密碼，使用者應於首次登入後強制修改密碼。  
        ///
        /// ## 🔐 安全機制  
        /// - 僅限具有 JWT 權杖的授權人員呼叫 (通常為管理員)。  
        /// - 所有密碼皆以 Salt + SHA256 雜湊儲存。  
        /// - 系統不回傳原密碼，僅提供隨機產生的臨時密碼。  
        /// - 呼叫後原 Token 會失效，需重新登入。  
        /// - 建議搭配操作日誌紀錄 (Audit Trail)。
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "reset_password",
        ///   "ValueAry": [
        ///     "staff_id=1101"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        /// 或使用帳號查詢：
        /// ```json
        /// {
        ///   "Method": "reset_password",
        ///   "ValueAry": [
        ///     "account=pharma01"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 🔍 參數說明  
        /// | 參數名稱 | 類型 | 必填 | 限制 | 說明 |
        /// |------------|------|------|------|------|
        /// | staff_id | string | 否 | 不可同時空白 | 員工代號 |
        /// | account | string | 否 | 不可同時空白 | 登入帳號 |
        ///
        /// **備註：** `staff_id` 與 `account` 至少需擇一提供。  
        ///
        /// ## 🔑 Header 範例  
        /// ```
        /// Authorization: Bearer <JWT Token>
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "reset_password",
        ///   "Result": "密碼已重設",
        ///   "Data": {
        ///     "staff_id": "1101",
        ///     "account": "pharma01",
        ///     "temp_password": "YH72xR9b"
        ///   },
        ///   "TimeTaken": "85ms"
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - 未提供任何條件：  
        /// ```json
        /// { "Code": -200, "Result": "請提供 staff_id 或 account" }
        /// ```
        ///
        /// - 查無帳號：  
        /// ```json
        /// { "Code": -200, "Result": "帳號授權資料不存在" }
        /// ```
        ///
        /// - 系統例外：  
        /// ```json
        /// { "Code": -200, "Result": "Exception : 資料庫連線失敗" }
        /// ```
        ///
        /// ## 🔒 安全建議  
        /// - 建議管理員於重設後通知使用者儘快修改臨時密碼。  
        /// - 若系統設有郵件或簡訊模組，可自動發送臨時密碼。  
        /// - 為提高安全性，臨時密碼可設置時效，例如 24 小時內必須更換。  
        /// - 此動作應納入「帳號異動紀錄」或「安全稽核表」中。  
        /// </remarks>
        /// <param name="returnData">封裝 API 請求與回應內容，包含 JWT Token、查詢條件與回傳結果</param>
        /// <returns>JSON 格式字串，包含密碼重設結果與臨時密碼</returns>
        [Authorize]
        [HttpPost]
        [Route("reset_password")]
        public string reset_password(returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "reset_password";

            try
            {
                if (returnData.Data == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "Data 不能為空";
                    return returnData.JsonSerializationt();
                }

                string GetVal(string key) =>
                    returnData.ValueAry
                        .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                        ?.Split('=')[1];

                string staffId = GetVal("staff_id");
                string account = GetVal("account");

                if (staffId.StringIsEmpty() && account.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "請提供 staff_id 或 account";
                    return returnData.JsonSerializationt();
                }

                var sql = MethodClass.GetSQLControl<StaffAuthClass>();

                string where = !staffId.StringIsEmpty()
                    ? $"staff_id = '{staffId}'"
                    : $"account = '{account}'";

                DataTable dt = sql.WtrteCommandAndExecuteReader(
                    $"select * from {sql.Database}.{sql.TableName} where {where}"
                );

                StaffAuthClass auth = dt.SQLToClass<StaffAuthClass>().FirstOrDefault();

                if (auth == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "帳號授權資料不存在";
                    return returnData.JsonSerializationt();
                }

                // === 產生臨時密碼 ===
                string tempPassword = GenerateTempPassword();

                string salt = PasswordHashHelper.GenerateSalt();
                string hash = PasswordHashHelper.HashPassword(tempPassword, salt);

                auth.password_salt = salt;
                auth.password_hash = hash;

                // 強制登出
                auth.last_login_at = DateTime.Now.ToDateTimeString();
                auth.updated_at = DateTime.Now.ToDateTimeString();
                auth.fail_count = "0";
                auth.is_locked = "N";

                sql.UpdateByDefulteExtra(null, auth.ClassToSQL<StaffAuthClass>());

                returnData.Code = 200;
                returnData.Result = "密碼已重設";
                returnData.Data = new
                {
                    staff_id = auth.staff_id,
                    account = auth.account,
                    temp_password = tempPassword
                };
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
        /// 使用者登出 (需 JWT 驗證)
        /// </summary>
        /// <remarks>
        /// ## 📌 用途  
        /// 讓已登入的使用者安全登出系統，並更新資料庫登出時間。  
        /// 前端在登出後需自行刪除 JWT Token，避免後續請求仍帶有舊權杖。
        ///
        /// ## ⚙️ 功能說明  
        /// 1. 使用 <c>[Authorize]</c> 屬性驗證 JWT Token 合法性。  
        /// 2. 由 Token 中解析出使用者的 `staff_id`。  
        /// 3. 查詢對應的 <c>StaffAuthClass</c> 資料。  
        /// 4. 更新以下欄位：
        ///    - `last_logout_at` → 登出時間  
        ///    - `updated_at` → 資料更新時間  
        /// 5. 回傳「登出成功」訊息，前端應同步清除 Token。
        ///
        /// ## 🔐 安全設計  
        /// - 此 API 必須攜帶有效 JWT 權杖才能呼叫。  
        /// - 伺服端僅記錄登出時間，不主動銷毀 Token（JWT 為無狀態機制）。  
        /// - 建議搭配 Token Blacklist 或 Redis 快取進行進階控管。  
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "logout",
        ///   "ValueAry": [],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 🔑 Header 範例  
        /// ```
        /// Authorization: Bearer <JWT Token>
        /// ```
        ///
        /// ## 📤 Response JSON 範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "logout",
        ///   "Result": "登出成功",
        ///   "TimeTaken": "35ms"
        /// }
        /// ```
        ///
        /// ## ❌ Response JSON 範例 (錯誤)
        /// - Token 無法解析：  
        /// ```json
        /// { "Code": -200, "Result": "JWT 無法解析 staff_id" }
        /// ```
        ///
        /// - 系統例外：  
        /// ```json
        /// { "Code": -200, "Result": "Exception : 資料庫連線失敗" }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - 前端登出後必須手動清除本地 JWT Token。  
        /// - 若系統採用黑名單控管，建議同步將此 Token 記錄為失效狀態。  
        /// - 若有多裝置登入需求，可記錄每個 Token 的簽發時間 (iat) 以供管理。  
        /// - 欄位 `last_logout_at` 可用於稽核或安全追蹤。  
        /// </remarks>
        /// <param name="returnData">封裝 API 請求與回應內容，包含 JWT Token、Method 與資料庫操作結果</param>
        /// <returns>JSON 格式字串，包含登出狀態與執行時間</returns>
        [Authorize]
        [HttpPost]
        [Route("logout")]
        public string logout(returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "logout";

            try
            {
                // 1️⃣ 從 JWT 取出 staff_id
                string staffId = User.Claims
                    .FirstOrDefault(c => c.Type == "staff_id")?.Value;

                if (staffId.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "JWT 無法解析 staff_id";
                    return returnData.JsonSerializationt();
                }

                // 2️⃣ 更新 staff_auth（視為此 Token 失效）
                var sql = MethodClass.GetSQLControl<StaffAuthClass>();

                DataTable dt = sql.WtrteCommandAndExecuteReader(
                    $"select * from {sql.Database}.{sql.TableName} where staff_id = '{staffId}'"
                );

                StaffAuthClass auth = dt.SQLToClass<StaffAuthClass>().FirstOrDefault();

                if (auth != null)
                {
                    auth.last_logout_at = DateTime.Now.ToDateTimeString();
                    auth.updated_at = DateTime.Now.ToDateTimeString();

                    sql.UpdateByDefulteExtra(null, auth.ClassToSQL<StaffAuthClass>());
                }

                // 3️⃣ 回傳成功（Client 要自行刪 Token）
                returnData.Code = 200;
                returnData.Result = "登出成功";
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

        private string GenerateTempPassword()
        {
            // 例：8 碼，含大小寫 + 數字
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            var rng = new Random();
            return new string(
                Enumerable.Range(0, 8)
                    .Select(_ => chars[rng.Next(chars.Length)])
                    .ToArray()
            );
        }

    }

}
