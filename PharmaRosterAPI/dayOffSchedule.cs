using Basic;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MySql.Data.MySqlClient;
using NPOI.SS.Formula.Eval;
using NPOI.SS.Formula.Functions;
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
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;

namespace PharmaRosterAPI
{
    [Route("phar_roster_api/[controller]")]
    public class dayOffSchedule : ControllerBase
    {      
        [HttpPost("init")]
        public string init([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "init";

            try
            {
                List<Table> tables = new List<Table>();
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<DayOffScheduleFormClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<DayOffScheduleDayClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<DayOffScheduleItemClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<StaffDayOffOptionClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<DayOffGroupClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<DayOffGroupMemberClass>());
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<StaffDayOffOptionLogClass>());

                
                returnData.Code = 200;
                returnData.Data = tables;
                returnData.Result = "初始化 DayOffScheduleClass 資料表完成";
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
        /// 取得所有排休表單清單（list_form）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL  
        /// `POST /phar_roster_api/DayOffSchedule/list_form`
        ///
        /// ## 📘 功能說明  
        /// 取得系統中所有已建立的排休表單清單。  
        /// 回傳每一筆表單的主檔資訊 (<see cref="DayOffScheduleFormClass"/>)，  
        /// 包含表單名稱、是否鎖定、建立時間、更新時間等欄位。  
        ///
        /// 此 API 通常用於「表單管理頁」或「下拉選單」列出所有排休表。
        ///
        /// ## ⚙️ 執行流程  
        /// 1. 從資料表 <c>dayoff_schedule_form</c> 讀取所有紀錄。  
        /// 2. 轉換為 <see cref="DayOffScheduleFormClass"/> 物件清單。  
        /// 3. 回傳表單清單（不含日期與項目資料）。
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "list_form",
        ///   "ValueAry": [],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// 或可附帶搜尋條件：
        /// ```json
        /// {
        ///   "Method": "list_form",
        ///   "ValueAry": [
        ///     "form_name=一月排休表"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 🔍 參數說明  
        /// | 參數名稱 | 類型 | 必填 | 範例 | 說明 |
        /// |------------|------|------|------|------|
        /// | form_name | string | ❌ | 一月排休表 | 可選，用於指定要篩選的表單名稱（目前未使用） |
        /// | simple | string | ❌ | true / false | 保留參數，目前未使用 |
        ///
        /// ## 📤 回傳範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Result": "取得資料成功",
        ///   "Data": [
        ///     {
        ///       "GUID": "F41A...E3B",
        ///       "form_name": "一月排休表",
        ///       "is_locked": "false",
        ///       "created_at": "2026-01-01 08:00:00",
        ///       "updated_at": "2026-01-01 08:00:00"
        ///     },
        ///     {
        ///       "GUID": "C22B...7D2",
        ///       "form_name": "二月排休表",
        ///       "is_locked": "true",
        ///       "created_at": "2026-02-01 08:00:00",
        ///       "updated_at": "2026-02-10 14:30:00"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## ❌ 錯誤回傳範例  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "Exception: 資料庫連線錯誤"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - URL 為 <c>/api/DayOffSchedule/list_form</c>，請以 <c>POST</c> 傳送。  
        /// - 預設會回傳所有表單主檔資料（不含日期與人員項目）。  
        /// - 若資料量龐大，可於前端實作分頁或查詢篩選功能。  
        /// - 回傳的每筆資料為 <see cref="DayOffScheduleFormClass"/> 物件。
        /// </remarks>
        /// <param name="returnData">封裝 API 請求內容的物件，包含查詢參數。</param>
        /// <returns>JSON 格式的排休表單清單。</returns>
        [HttpPost("list_form")]
        public string list_form([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "list_form";
            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];
                string form_name = GetVal("form_name");
                string simple = GetVal("simple");
                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
             

                List<object[]> objs_dayOffScheduleForm = sql_dayOffScheduleFormClass.GetAllRows(null);
                List<DayOffScheduleFormClass> dayOffScheduleForms = objs_dayOffScheduleForm.SQLToClass<DayOffScheduleFormClass>();

                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Data = dayOffScheduleForms;
                returnData.Result = "取得資料成功";
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
        /// 查詢指定的排休表單（get_form）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL  
        /// `POST /phar_roster_api/DayOffSchedule/get_form`
        ///
        /// ## 📘 功能說明  
        /// 根據指定的表單名稱 (<c>form_name</c>)，取得排休表單完整結構。  
        /// 可透過參數 <c>simple=true</c> 選擇是否僅回傳主表資料（不含日期、項目與放假選項）。
        ///
        /// - 當 <c>simple=true</c> 時，只回傳 <see cref="DayOffScheduleFormClass"/>。  
        /// - 當 <c>simple=false</c>（預設）時，會載入：  
        ///   - 所有日期 <see cref="DayOffScheduleDayClass"/>  
        ///   - 每日人員項目 <see cref="DayOffScheduleItemClass"/>  
        ///   - 每項對應的放假選項狀態 <see cref="StaffDayOffOptionClass"/>
        ///
        /// ## ⚙️ 執行流程  
        /// 1. 驗證輸入參數 `form_name`。  
        /// 2. 查詢主表 `dayoff_schedule_form`。  
        /// 3. 若 <c>simple=true</c> → 直接回傳主表資料。  
        /// 4. 若 <c>simple=false</c> → 載入：  
        ///    - 所有日期 (`dayoff_schedule_day`)  
        ///    - 所有人員項目 (`dayoff_schedule_item`)  
        ///    - 放假選項 (`staff_dayoff_option`)  
        /// 5. 以階層方式組合完整資料後回傳。
        ///
        /// ## 🧩 回傳資料階層結構  
        /// ```
        /// DayOffScheduleFormClass
        /// ├─ DayOffScheduleDayClass[]
        /// │  ├─ DayOffScheduleItemClass[]
        /// │  │   ├─ WorkShiftRequirementClass
        /// │  │   └─ StaffDayOffOptionClass
        /// ```
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "get_form",
        ///   "ValueAry": [
        ///     "form_name=一月排休表",
        ///     "simple=false"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 🔍 參數說明  
        /// | 參數名稱 | 類型 | 必填 | 預設值 | 範例 | 說明 |
        /// |------------|------|------|--------|------|------|
        /// | form_name | string | ✅ | — | 一月排休表 | 要查詢的表單名稱 |
        /// | simple | bool | ❌ | false | true / false | 是否僅回傳主表資料 |
        ///
        /// ## 📤 回傳範例（完整模式，含 StaffDayOffOptionClass）
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Result": "取得資料成功",
        ///   "Data": {
        ///     "GUID": "6892c33b-d8ba-488f-95c5-e4e2aafe1016",
        ///     "form_name": "一月排休表",
        ///     "is_locked": "false",
        ///     "created_at": "2026-01-11 22:20:21",
        ///     "updated_at": "2026-01-11 22:20:21",
        ///     "days": [
        ///       {
        ///         "GUID": "052b1703-de2b-41e5-98b4-aeae71c21da1",
        ///         "form_guid": "6892c33b-d8ba-488f-95c5-e4e2aafe1016",
        ///         "date": "2026-01-12 00:00:00",
        ///         "items": [
        ///           {
        ///             "GUID": "a909ee8e-99e7-44ab-9d85-5c471887b922",
        ///             "form_guid": "6892c33b-d8ba-488f-95c5-e4e2aafe1016",
        ///             "day_guid": "052b1703-de2b-41e5-98b4-aeae71c21da1",
        ///             "option_guid": "be0b01ff-a037-4e31-bfc0-aca6b69e3299",
        ///             "date": "2026-01-12 00:00:00",
        ///             "staff_guid": "e8669c12-b0d6-4bc0-b109-c69a9de9bc1e",
        ///             "staff_id": "850233",
        ///             "staff_name": "郭佳瓚",
        ///             "staff_simple_name": "瓚",
        ///             "selected_dayoff_type": "",
        ///             "shift_requirement": "{\"day\":\"Monday\",\"time\":\"16:00-23:59\",\"required_count\":\"2\",\"department\":\"急診\"}",
        ///             "created_at": "2026-01-11 22:20:21",
        ///             "updated_at": "2026-01-11 22:20:21",
        ///             "option": {
        ///               "GUID": "be0b01ff-a037-4e31-bfc0-aca6b69e3299",
        ///               "form_guid": "6892c33b-d8ba-488f-95c5-e4e2aafe1016",
        ///               "item_guid": "a909ee8e-99e7-44ab-9d85-5c471887b922",
        ///               "staff_guid": "e8669c12-b0d6-4bc0-b109-c69a9de9bc1e",
        ///               "date": "2026-01-12 00:00:00",
        ///               "suggested_dates": "[\"2026-01-13\"]",
        ///               "is_any_date": "true",
        ///               "assigned_shift": "swing",
        ///               "can_full": "true",
        ///               "can_half_am": "false",
        ///               "can_half_pm": "false",
        ///               "is_forbidden": "",
        ///               "selected_full": "",
        ///               "selected_half_am": "",
        ///               "selected_half_pm": ""
        ///             },
        ///             "workShiftRequirement": {
        ///               "day": "Monday",
        ///               "time": "16:00-23:59",
        ///               "required_count": "2",
        ///               "department": "急診",
        ///               "disabled": false
        ///             }
        ///           }
        ///         ]
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        ///
        /// ## 🧾 StaffDayOffOptionClass 欄位說明  
        /// | 欄位名稱 | 類型 | 範例 | 說明 |
        /// |------------|------|------|------|
        /// | GUID | string | be0b01ff-a037-... | 放假選項唯一識別碼 |
        /// | form_guid | string | 6892c33b-d8ba-... | 所屬表單 GUID |
        /// | item_guid | string | a909ee8e-99e7-... | 對應的排休項目 GUID |
        /// | staff_guid | string | e8669c12-b0d6-... | 員工唯一識別碼 |
        /// | date | string | 2026-01-12 | 此放假設定對應的日期 |
        /// | suggested_dates | string(JSON) | ["2026-01-13"] | 系統建議的可休日期清單 |
        /// | **is_any_date** | string | "true" / "false" | 若為 **"true"**，代表該員工可於任意日期中選擇休假，常用於夜班或週日補休。 |
        /// | assigned_shift | string | swing | 該員工被分配的班別 |
        /// | can_full | string | true | 可否整天休假 |
        /// | can_half_am | string | false | 可否上午半日休假 |
        /// | can_half_pm | string | false | 可否下午半日休假 |
        /// | is_forbidden | string | false | 是否禁止休假 |
        /// | selected_full | string | "" | 實際選擇整日假狀態 |
        /// | selected_half_am | string | "" | 實際選擇上午半日假狀態 |
        /// | selected_half_pm | string | "" | 實際選擇下午半日假狀態 |
        ///
        /// 🟢 **補充說明：**  
        /// - 當 `is_any_date = "true"` 時，表示該員工可以於此週期內**任選一天休假**，而非固定日期。  
        /// - 若同時提供 `suggested_dates`，則代表系統建議可休日期；若未提供，則代表無限制可選日期。  
        /// - 常見應用：週日值班補休、夜班隔日休等彈性放假制度。
        ///
        /// ## ❌ 錯誤回傳範例  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "找不到表單名稱(一月排休表)"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - URL 為 <c>/phar_roster_api/DayOffSchedule/get_form</c>，請以 <c>POST</c> 傳送。  
        /// - 若 <c>form_name</c> 不存在，回傳錯誤碼 <c>-200</c>。  
        /// - 當 <c>simple=true</c> 時，僅回傳表單主檔，不載入日期、項目與放假選項資料。  
        /// - 當 <c>simple=false</c> 時，會同時載入每位人員的 <see cref="StaffDayOffOptionClass"/> 狀態。  
        /// - 適用於前端顯示完整排休表、人工檢查、或後端 AI 建議假期排程時使用。
        /// </remarks>
        /// <param name="returnData">封裝 API 請求內容的物件，包含表單名稱與查詢模式參數。</param>
        /// <returns>回傳完整或簡易的排休表單 JSON 結構，包含放假選項資料。</returns>
        [HttpPost("get_form")]
        public string get_form([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_form";
            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];
                string form_name = GetVal("form_name");
                string simple = GetVal("simple");
                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

                object[] obj_dayOffScheduleForm = sql_dayOffScheduleFormClass.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();

                if(obj_dayOffScheduleForm == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass dayOffScheduleForm = obj_dayOffScheduleForm.SQLToClass<DayOffScheduleFormClass>();
              
                List<object[]> obj_dayOffScheduleDays = sql_dayOffScheduleDayClass.GetRowsByDefult(null, "form_guid", dayOffScheduleForm.GUID);
                List<object[]> obj_dayOffScheduleItem = sql_dayOffScheduleItemClass.GetRowsByDefult(null, "form_guid", dayOffScheduleForm.GUID);
                List<object[]> obj_staffDayOffOption = sql_staffDayOffOptionClass.GetRowsByDefult(null, "form_guid", dayOffScheduleForm.GUID);

                List<DayOffScheduleDayClass> dayOffScheduleDayClasses = obj_dayOffScheduleDays.SQLToClass<DayOffScheduleDayClass>();
                List<DayOffScheduleItemClass> dayOffScheduleItemClasses = obj_dayOffScheduleItem.SQLToClass<DayOffScheduleItemClass>();
                List<StaffDayOffOptionClass> staffDayOffOptionClasses = obj_staffDayOffOption.SQLToClass<StaffDayOffOptionClass>();

                dayOffScheduleForm.days.LockAdd(dayOffScheduleDayClasses);

                if (simple == true.ToString().ToLower())
                {
                    returnData.Code = 200;
                    returnData.Data = dayOffScheduleForm;
                    returnData.Result = "取得資料成功";
                    return returnData.JsonSerializationt(true);
                }

                foreach (var dayOffScheduleDay in dayOffScheduleDayClasses)
                {
                    dayOffScheduleDay.items = dayOffScheduleItemClasses
                                                .Where(x => x.day_guid == dayOffScheduleDay.GUID)
                                                .ToList();
                    foreach (var item in dayOffScheduleDay.items)
                    {
                        item.option = staffDayOffOptionClasses
                                                    .Where(x => x.staff_guid == item.staff_guid && x.GUID == item.option_guid)
                                                    .FirstOrDefault();                      
                    }
                }
                if(dayOffScheduleForm.enable_annualleave_selection.StringIsEmpty()) dayOffScheduleForm.enable_annualleave_selection = "false";
                if(dayOffScheduleForm.enable_weekoff_selection.StringIsEmpty()) dayOffScheduleForm.enable_weekoff_selection = "false";
                if(dayOffScheduleForm.is_completed_locked.StringIsEmpty()) dayOffScheduleForm.is_completed_locked = "false";
                if(dayOffScheduleForm.annualleave_selection_update_at.StringIsEmpty()) dayOffScheduleForm.annualleave_selection_update_at = DateTime.MinValue.ToDateTimeString();
                if(dayOffScheduleForm.weekoff_selection_update_at.StringIsEmpty()) dayOffScheduleForm.weekoff_selection_update_at = DateTime.MinValue.ToDateTimeString();
                if(dayOffScheduleForm.is_completed_locked_update_at.StringIsEmpty()) dayOffScheduleForm.is_completed_locked_update_at = DateTime.MinValue.ToDateTimeString();

                calculate_available_dayoff_dates(returnData);
                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Data = dayOffScheduleForm;
                returnData.Result = "取得資料成功";
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
        /// 刪除指定的排休表單（delete_form）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL  
        /// `POST /phar_roster_api/DayOffSchedule/delete_form`
        ///
        /// ## 📘 功能說明  
        /// 根據表單名稱 (<c>form_name</c>) 刪除整份排休表單，  
        /// 同時一併刪除其下所有日期資料 (<c>dayoff_schedule_day</c>)  
        /// 以及人員排休項目 (<c>dayoff_schedule_item</c>)。  
        ///
        /// ⚠️ **此操作不可回復，請務必於前端提示使用者確認。**
        ///
        /// ## ⚙️ 執行流程  
        /// 1. 驗證輸入參數 `form_name`。  
        /// 2. 查詢主表紀錄 (`dayoff_schedule_form`)。  
        /// 3. 若無符合資料 → 回傳錯誤。  
        /// 4. 查詢所有對應的日期與項目資料。  
        /// 5. 依序執行刪除：Form → Days → Items。  
        /// 6. 回傳刪除摘要資訊。
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "delete_form",
        ///   "ValueAry": [
        ///     "form_name=一月排休表"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 🔍 參數說明  
        /// | 參數名稱 | 類型 | 必填 | 範例 | 說明 |
        /// |------------|------|------|------|------|
        /// | form_name | string | ✅ | 一月排休表 | 要刪除的表單名稱 |
        /// | simple | string | ❌ | true / false | 保留參數（未使用） |
        ///
        /// ## 📤 回傳範例（成功）
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Result": "刪除資料成功,共31個日期,共620筆Items",
        ///   "Data": {
        ///     "GUID": "F41A...E3B",
        ///     "form_name": "一月排休表",
        ///     "is_locked": "false",
        ///     "created_at": "2026-01-01 08:00:00",
        ///     "updated_at": "2026-01-31 17:00:00"
        ///   }
        /// }
        /// ```
        ///
        /// ## ❌ 錯誤回傳範例  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "找不到表單名稱(一月排休表)"
        /// }
        /// ```
        ///
        /// 或系統例外：
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "Exception: 資料庫連線錯誤"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - URL 為 <c>/api/DayOffSchedule/delete_form</c>，請以 <c>POST</c> 傳送。  
        /// - 此操作會永久刪除主表、日期表與項目表中的所有關聯資料。  
        /// - 建議前端於操作前彈出確認視窗以避免誤刪。  
        /// - 回傳之 <c>Data</c> 為被刪除的主表資訊（僅供紀錄）。  
        /// - 若指定的表單不存在，會回傳錯誤碼 <c>-200</c>。
        /// </remarks>
        /// <param name="returnData">封裝 API 請求內容的物件，包含要刪除的表單名稱。</param>
        /// <returns>回傳 JSON 結果，包含刪除摘要與主表資訊。</returns>
        [HttpPost("delete_form")]
        public string delete_form([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "delete_form";
            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];
                string form_name = GetVal("form_name");
                string simple = GetVal("simple");
                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();

                object[] obj_dayOffScheduleForm = sql_dayOffScheduleFormClass.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();

                if (obj_dayOffScheduleForm == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass dayOffScheduleForm = obj_dayOffScheduleForm.SQLToClass<DayOffScheduleFormClass>();

                List<object[]> obj_dayOffScheduleDays = sql_dayOffScheduleDayClass.GetRowsByDefult(null, "form_guid", dayOffScheduleForm.GUID);
                List<object[]> obj_dayOffScheduleItem = sql_dayOffScheduleItemClass.GetRowsByDefult(null, "form_guid", dayOffScheduleForm.GUID);

                sql_dayOffScheduleFormClass.DeleteExtra(null, obj_dayOffScheduleForm);
                sql_dayOffScheduleDayClass.DeleteExtra(null, obj_dayOffScheduleDays);
                sql_dayOffScheduleItemClass.DeleteExtra(null, obj_dayOffScheduleItem);

                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Data = dayOffScheduleForm;
                returnData.Result = $"刪除資料成功,共{obj_dayOffScheduleDays.Count}個日期,共{obj_dayOffScheduleItem.Count}筆Items";
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
        /// 建立新的排休表單
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL  
        /// `POST /phar_roster_api/DayOffSchedule/creat_form`
        ///
        /// ## 📘 功能說明  
        /// 建立一份新的排休表單 (<see cref="DayOffScheduleFormClass"/>)，  
        /// 系統會依輸入的日期清單自動產生對應的排休日與人員項目。  
        /// 每個日期會對應一組 <see cref="DayOffScheduleDayClass"/>，  
        /// 其中包含各人員的排休列 <see cref="DayOffScheduleItemClass"/>。
        ///
        /// ## ⚙️ 執行流程  
        /// 1. 驗證輸入參數：`form_name`、`dates`。  
        /// 2. 檢查表單名稱是否重複。  
        /// 3. 驗證日期格式並依時間順序排序。  
        /// 4. 取得指定日期的排班資料 (`scheduleDay.GetScheduleDay()`)。  
        /// 5. 依每位人員與班別自動建立 `DayOffScheduleItemClass`。  
        /// 6. 寫入三張資料表：  
        ///    - `dayoff_schedule_form`  
        ///    - `dayoff_schedule_day`  
        ///    - `dayoff_schedule_item`  
        /// 7. 回傳建立完成的表單結構。
        ///
        /// ## 🧩 建立資料結構  
        /// ```
        /// DayOffScheduleFormClass
        /// ├─ DayOffScheduleDayClass[]
        /// │  ├─ DayOffScheduleItemClass[]
        /// │  │   ├─ StaffClass
        /// │  │   └─ WorkShiftRequirementClass
        /// ```
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "creat_form",
        ///   "ValueAry": [
        ///     "form_name=一月排休表",
        ///     "dates=2026-01-01,2026-01-02,2026-01-03"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 🔍 參數說明  
        /// | 參數名稱 | 類型 | 必填 | 範例 | 說明 |
        /// |------------|------|------|------|------|
        /// | form_name | string | ✅ | 一月排休表 | 表單名稱（不可重複） |
        /// | dates | string | ✅ | 2026-01-01,2026-01-02 | 日期清單，格式 yyyy-MM-dd，以逗號分隔 |
        ///
        /// ## 📤 回傳範例 (成功)
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Result": "success",
        ///   "Data": {
        ///     "GUID": "F41A...E3B",
        ///     "form_name": "一月排休表",
        ///     "is_locked": "false",
        ///     "created_at": "2026-01-01 08:00:00",
        ///     "updated_at": "2026-01-01 08:00:00",
        ///     "days": [
        ///       {
        ///         "GUID": "D11B...92C",
        ///         "form_guid": "F41A...E3B",
        ///         "date": "2026-01-01",
        ///         "items": [
        ///           {
        ///             "staff_guid": "E22B...441",
        ///             "staff_id": "P001",
        ///             "workShiftRequirement": {
        ///               "department": "門診",
        ///               "time": "08:00-16:00",
        ///               "shift_type": "day"
        ///             }
        ///           }
        ///         ]
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        ///
        /// ## ❌ 錯誤回傳範例  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "表單名稱(一月排休表)已建立過"
        /// }
        /// ```
        /// 或  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "日期格式錯誤: 2026/13/01"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - URL 為 <c>/api/DayOffSchedule/creat_form</c>，請以 <c>POST</c> 傳送。  
        /// - 表單名稱必須唯一。  
        /// - 日期格式需為 <c>yyyy-MM-dd</c>。  
        /// - 若日期無對應排班資料，該日會略過不生成項目。  
        /// - 回傳的 Data 內含完整表單結構，可直接用於前端呈現。
        /// </remarks>
        /// <param name="returnData">封裝 API 請求內容的物件，包含表單名稱與日期清單。</param>
        /// <returns>JSON 格式的建立結果，含完整表單資料結構。</returns>
        [HttpPost("creat_form")]
        public string creat_form([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "creat_form";

            try
            {
                init(returnData);
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];
                string form_name = GetVal("form_name");
                string str_dates = GetVal("dates");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = $"未輸入表單名稱";
                    return returnData.JsonSerializationt();
                }
                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();

                if (sql_dayOffScheduleFormClass.GetRowsByDefult(null, "form_name", form_name).Count> 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"表單名稱({form_name})已建立過";
                    return returnData.JsonSerializationt();
                }

                List<string> dates = str_dates.Split(',').ToList();
                for(int i = 0; i < dates.Count; i++)
                {
                    dates[i] = dates[i].Trim();
                    if (dates[i].Check_Date_String() == false)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"日期格式錯誤: {dates[i]}";
                        return returnData.JsonSerializationt();
                    }
                }
                dates = dates
                        .OrderBy(d => DateTime.Parse(d))
                        .ToList();

                List<ScheduleDayClass> scheduleDays = scheduleDay.GetScheduleDay(dates.ToArray());
                List<DayOffScheduleItemClass> dayOffScheduleItems_add = new List<DayOffScheduleItemClass>();
                List<StaffClass> staffs = staff.GetAllStaffs();
                DayOffScheduleFormClass dayOffScheduleForm = new DayOffScheduleFormClass()
                {
                    GUID = Guid.NewGuid().ToString(),
                    form_name = form_name,
                    enable_weekoff_selection = "false",
                    enable_annualleave_selection = "false",
                    is_completed_locked = "false",
                    created_at = DateTime.Now.ToDateTimeString(),
                    updated_at = DateTime.Now.ToDateTimeString()
                };
                foreach (string date in dates)
                {
                    DayOffScheduleDayClass dayOffScheduleDay = new DayOffScheduleDayClass()
                    {
                        GUID = Guid.NewGuid().ToString(),
                        form_guid = dayOffScheduleForm.GUID,
                        date = date,
                        created_at = DateTime.Now.ToDateTimeString(),
                        updated_at = DateTime.Now.ToDateTimeString()
                    };
                    dayOffScheduleForm.days.Add(dayOffScheduleDay);

                    ScheduleDayClass scheduleDay = scheduleDays.First(x => x.date.StringToDateTime().ToDateString("-") == date.StringToDateTime().ToDateString("-"));
                    if (scheduleDay == null)
                    {
                        continue;
                    }
                    int index_opd = 0;
                    int index_pher = 0;
                    List<SpecialDayClass> specialDays = specialDay.GetSpecialDays(new List<string>()).specialDays;
                    foreach (var assignedShift in scheduleDay.AssignedShifts)
                    {
                        StaffClass staff = staffs.Where(x => x.GUID == assignedShift.staff_guid).FirstOrDefault();
                        if (staff == null)
                        {
                            continue;
                        }
                        bool isSpecial = specialDays.Where(x => x.date.StringToDateTime().ToDateString("-") == date.StringToDateTime().ToDateString("-")).Any();
                        DayOffScheduleItemClass dayOffScheduleItem = new DayOffScheduleItemClass()
                        {
                            GUID = Guid.NewGuid().ToString(),
                            form_guid = dayOffScheduleForm.GUID,
                            day_guid = dayOffScheduleDay.GUID,
                            date = date,
                            is_special_day = isSpecial.ToString().ToLower(),
                            staff_id = staff.staff_id,
                            staff_guid = staff.GUID,
                            staff_name = staff.staff_name,
                            staff_simple_name = staff.staff_simple_name,
                            workShiftRequirement = assignedShift.workShiftRequirement,
                            created_at = DateTime.Now.ToDateTimeString(),
                            updated_at = DateTime.Now.ToDateTimeString()
                        };
                        if (assignedShift.date.StringToDateTime().DayOfWeek == DayOfWeek.Sunday)
                        {
                            string endStr = assignedShift.workShiftRequirement?.TimeRange.Value.end.ToString() ?? "";

                            if (assignedShift.workShiftRequirement.department == "門診" && endStr.Contains("16:00"))
                            {
                                dayOffScheduleItem.position = index_opd.ToString();
                                index_opd++;
                            }
                            else if (assignedShift.workShiftRequirement.department == "急診" && endStr.Contains("16:00"))
                            {
                                dayOffScheduleItem.position = index_pher.ToString();
                                index_pher++;
                            }
                           

                        }
                        dayOffScheduleDay.items.Add(dayOffScheduleItem);
                        dayOffScheduleItems_add.Add(dayOffScheduleItem);
                    }
                }
                sql_dayOffScheduleFormClass.AddRow(null, dayOffScheduleForm.ClassToSQL<DayOffScheduleFormClass>());
                sql_dayOffScheduleDayClass.AddRows(null, dayOffScheduleForm.days.ClassToSQL<DayOffScheduleDayClass>());
                sql_dayOffScheduleItemClass.AddRows(null, dayOffScheduleItems_add.ClassToSQL<DayOffScheduleItemClass>());


                // === 3. 成功回傳 ===
                returnData.Code = 200;
                //returnData.Result = $"新增({datas_add.Count})筆資料,修改({datas_update.Count})筆資料";
                returnData.TimeTaken = $"{timer}";
                returnData.Data = dayOffScheduleForm;
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
        /// 查詢排休表單「任選一天放假」總天數統計（get_any_date_quota_summary）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL
        /// `POST /phar_roster_api/DayOffSchedule/get_any_date_quota_summary`
        ///
        /// ## 📘 功能說明
        /// 依據指定排休表單名稱 (<c>form_name</c>)，統計該表單在 <c>staff_dayoff_option</c> 中：
        /// - <c>is_any_date = true</c> 的 option 總筆數（不去重）
        ///
        /// ✅ 任選天數定義（你指定的規則）：
        /// - 「任選一天放假沒有去重問題」
        /// - 也就是整張表單中，有多少筆 option 設定為 <c>is_any_date=true</c>
        /// - 不依賴 <c>suggested_dates_list</c>，不以日期池長度當作額度
        ///
        /// ## ✅ 主要用途
        /// - 前端顯示「任選一天」的總額度天數（例如：你有 6 天任選）
        /// - 顯示「有任選額度的人數」(staff_guid 去重) 作為補充資訊（非必要，但常用於 UI）
        ///
        /// ## ⚙️ 執行流程
        /// 1. 從 <c>returnData.ValueAry</c> 解析 <c>form_name</c>
        /// 2. 查詢表單主檔 <c>dayoff_schedule_form</c>，取得 <c>form_guid</c>
        /// 3. 統計 <c>staff_dayoff_option</c> 中 <c>form_guid</c> 且 <c>is_any_date='true'</c> 的筆數
        /// 4. 將統計結果寫入回傳的 <see cref="DayOffScheduleFormClass"/>（非 SQL 欄位）
        ///
        /// ## 📥 Request JSON 範例
        /// {
        ///   "Method": "get_any_date_quota_summary",
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表"
        ///   ],
        ///   "Data": {}
        /// }
        ///
        /// ## 📤 成功回傳 JSON 範例
        /// {
        ///   "Code": 200,
        ///   "Result": "取得任選天數統計成功",
        ///   "Data": {
        ///     "GUID": "FORM_GUID_001",
        ///     "form_name": "2026年03月排休表",
        ///     "any_date_quota_days": "6",
        ///     "any_date_option_count": "6",
        ///     "any_date_staff_count": "6",
        ///     "any_date_used_days": "0",
        ///     "any_date_remaining_days": "6",
        ///     "any_date_is_full": "false"
        ///   }
        /// }
        ///
        /// ## ❌ 錯誤回傳範例
        /// (1) 找不到表單
        /// {
        ///   "Code": -200,
        ///   "Result": "找不到表單名稱(2026年03月排休表)"
        /// }
        ///
        /// (2) 系統例外
        /// {
        ///   "Code": -200,
        ///   "Result": "Exception message ..."
        /// }
        ///
        /// ## 📑 注意事項
        /// - 本 API 統計的是「option 筆數」，不做去重、不做日期池運算。
        /// - <c>any_date_used_days</c> 若你尚未有「任選一天實際選擇」的欄位規則，本 API 先回傳 0；
        ///   未來你若要加入已使用統計，可再延伸此 API。
        /// </remarks>
        /// <param name="returnData">
        /// 封裝 API 請求內容的物件，需在 <c>ValueAry</c> 內包含：
        /// <c>form_name</c>
        /// </param>
        /// <returns>回傳包含任選天數統計結果的 JSON 字串。</returns>
        [HttpPost("get_any_date_quota_summary")]
        public async Task<string> get_any_date_quota_summary([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_any_date_quota_summary";
            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string form_name = GetVal("form_name");

                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

                object[] obj_dayOffScheduleForm = sql_dayOffScheduleFormClass
                    .GetRowsByDefult(null, "form_name", form_name)
                    .FirstOrDefault();

                if (obj_dayOffScheduleForm == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass dayOffScheduleForm = obj_dayOffScheduleForm.SQLToClass<DayOffScheduleFormClass>();

                // ✅ 統計 is_any_date=true 的 option 筆數（不去重）
                // 同時回傳涉及多少人（staff_guid 去重）方便 UI 顯示
                string sql = $@"
            SELECT 
                COUNT(1) AS any_cnt,
                COUNT(DISTINCT staff_guid) AS staff_cnt
            FROM {sql_staffDayOffOptionClass.Database}.{sql_staffDayOffOptionClass.TableName}
            WHERE form_guid = @form_guid
              AND is_any_date = 'true'
        ";

                var parameters = new { form_guid = dayOffScheduleForm.GUID };
                List<object[]> rows = await sql_staffDayOffOptionClass.WriteCommandAsync(sql, parameters);

                long anyCnt = 0;
                long staffCnt = 0;

                if (rows != null && rows.Count > 0 && rows[0] != null)
                {
                    // ⚠️ 依你現有工具的回傳 object[] 順序：0=any_cnt, 1=staff_cnt
                    anyCnt = rows[0][0].ToString().StringToInt64();
                    staffCnt = rows[0][1].ToString().StringToInt64();
                }

                // ✅ 填入表單（非SQL欄位）
                dayOffScheduleForm.any_date_quota_days = anyCnt.ToString();
                dayOffScheduleForm.any_date_option_count = anyCnt.ToString();
                dayOffScheduleForm.any_date_staff_count = staffCnt.ToString();

                // 若你尚未有「任選已使用」的判定規則，先回 0
                dayOffScheduleForm.any_date_used_days = "0";

                long remaining = anyCnt; // remaining = quota - used (used=0)
                dayOffScheduleForm.any_date_remaining_days = remaining.ToString();
                dayOffScheduleForm.any_date_is_full = (remaining <= 0) ? "true" : "false";

                returnData.Code = 200;
                returnData.Data = dayOffScheduleForm;
                returnData.Result = "取得任選天數統計成功";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
            finally
            {
                returnData.Result += timer.ToString();
            }
        }

        /// <summary>
        /// 計算排休表單可用放假日，並建立系統預設/特殊規則選項（含：週日無排班 → 自動建立 FF 強制休假）。
        /// </summary>
        /// <remarks>
        /// ===============================
        /// 【API 說明】
        /// ===============================
        /// 本 API 用於排休表單 DayOffScheduleForm 的「放假選項(option)」計算與補齊，並依規則自動新增 StaffDayOffOption：
        ///
        /// (A) 讀取表單資料：
        /// 1) 依 form_name 取得對應的 DayOffScheduleFormClass
        /// 2) 讀取該表單的 days、items、staff_dayoff_option
        ///
        /// (B) 若 simple=true：
        /// - 直接回傳表單資料（不計算、不新增 option）
        ///
        /// (C) 若 simple=false（預設）：
        /// - 針對每個 item 計算/建立特殊規則 option（例如：特定日、補休、假日、小夜/大夜規則等）
        /// - ✅ 新增功能：若週日無排班 → 自動建立 FF 強制休假 option
        ///
        /// ===============================
        /// 【新增功能：週日無排班 → 自動設定 FF 強制休假】
        /// ===============================
        /// 規則：
        /// 1) 僅處理星期日 (DayOfWeek.Sunday)
        /// 2) 若該 item「有排班需求」→ 不新增 FF
        /// 3) 若該 item「已存在 option」（不論是不是 FF）→ 不覆蓋、不修改
        /// 4) 僅在「該 item 完全沒有 option」時新增 FF option
        ///
        /// 【排班需求判定（依 WorkShiftRequirementClass）】
        /// - item.workShiftRequirement == null → 視為無排班
        /// - req.disabled == true → 視為無排班
        /// - req.RequiredCountBase <= 0 → 視為無排班
        /// - req.shift_type 為空 → 視為無排班
        /// - 其餘情況 → 視為有排班（不新增 FF）
        ///
        /// 【FF option 寫入內容】
        /// - assigned_shift = "OFF"
        /// - can_full = true、can_half_am = false、can_half_pm = false
        /// - selected_full = true（強制整天假）
        /// - is_force_ff = true（新增欄位）
        /// - force_ff_at = now（新增欄位）
        ///
        /// ===============================
        /// 【重要防呆：週日 item 已有 option 不覆蓋】
        /// ===============================
        /// 若 item 已存在 option（包含但不限於：
        /// - item.option != null 且 item.option.GUID 有值
        /// - item.option_guid 不為空
        /// - DB 已存在 option key
        /// ）
        /// 則本 API 不會覆蓋該 item（即使該 option 不是 FF）
        ///
        /// ===============================
        /// 【URL】
        /// ===============================
        /// POST /phar_roster_api/dayOffSchedule/calculate_available_dayoff_dates
        ///
        /// ===============================
        /// 【Method】
        /// ===============================
        /// POST
        ///
        /// ===============================
        /// 【傳入參數】(ValueAry)
        /// ===============================
        /// form_name = 排休表單名稱（必填）
        /// simple    = 是否簡化回傳（選填）
        ///            - true  : 僅回傳 form 及 days/items，不進行 option 新增計算
        ///            - false : 進行 option 計算與新增（預設）
        ///
        /// ===============================
        /// 【JSON 傳入範例】
        /// ===============================
        /// (1) 正常計算（預設）
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年01月排休表"
        ///   ]
        /// }
        ///
        /// (2) 簡化回傳（不新增 option）
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年01月排休表",
        ///     "simple=true"
        ///   ]
        /// }
        ///
        /// ===============================
        /// 【資料新增規則】
        /// ===============================
        /// 本 API 會新增 StaffDayOffOptionClass 至 staff_dayoff_option 資料表：
        /// 1) 特殊規則 option（由下列 builder 產生）
        ///    - BuildStaffDayOffSpecialDayOption()
        ///    - BuildStaffDayOffSwingOption()
        ///    - BuildStaffDayOffHolidayOption()
        ///    - BuildStaffDayOffMidnightOption()
        /// 2) ✅ 週日無排班強制休假 FF option
        ///
        /// 並同步更新對應 DayOffScheduleItemClass.option_guid
        ///
        /// ===============================
        /// 【避免重複新增機制】
        /// ===============================
        /// 使用 existsOptionKeySet HashSet 做唯一性控管：
        /// UniqueKey = item_guid|staff_guid|yyyy-MM-dd
        /// - DB 已存在 option → 不新增
        /// - 同一次執行產生的 option → 不重複新增
        ///
        /// ===============================
        /// 【成功回傳 JSON 範例】
        /// ===============================
        /// {
        ///   "Code": 200,
        ///   "Method": "calculate_available_dayoff_dates",
        ///   "Result": "新增排休資料成功,共12筆(含週日無排班→FF強制)",
        ///   "Data": {
        ///     "GUID": "FORM_GUID_001",
        ///     "form_name": "2026年01月排休表",
        ///     "days": [
        ///       {
        ///         "GUID": "DAY_GUID_001",
        ///         "items": [
        ///           {
        ///             "GUID": "ITEM_GUID_001",
        ///             "staff_guid": "STAFF_GUID_001",
        ///             "date": "2026-01-04",
        ///             "option_guid": "OPTION_GUID_FF_001"
        ///           }
        ///         ]
        ///       }
        ///     ]
        ///   }
        /// }
        ///
        /// ===============================
        /// 【失敗回傳 JSON 範例】
        /// ===============================
        /// (1) 找不到表單
        /// {
        ///   "Code": -200,
        ///   "Method": "calculate_available_dayoff_dates",
        ///   "Result": "找不到表單名稱(2026年01月排休表)",
        ///   "Data": null
        /// }
        ///
        /// (2) 例外錯誤
        /// {
        ///   "Code": -200,
        ///   "Method": "calculate_available_dayoff_dates",
        ///   "Result": "Exception message ...",
        ///   "Data": null
        /// }
        ///
        /// ===============================
        /// 【備註】
        /// ===============================
        /// 1) 本 API 只會新增 option，不會刪除既有 option。
        /// 2) 週日 FF 僅在 item 完全沒有 option 才新增，避免覆蓋人工選擇或既有規則。
        /// 3) 若你未來要允許「週日 option 存在但選擇為空」時仍強制 FF，可以再擴充判斷條件。
        /// </remarks>
        /// <param name="returnData">
        /// returnData 物件，主要使用 ValueAry 作為參數輸入。
        /// </param>
        /// <returns>
        /// 回傳 returnData.JsonSerializationt() 的 JSON 字串。
        /// </returns>
        /// <summary>
        /// 計算排休表單可用放假日，並建立系統預設/特殊規則選項。
        /// </summary>
        /// <remarks>
        /// 規則：
        /// 1. 先計算特殊規則建議休假日
        /// 2. 再補週六、週日 item
        /// 3. 僅對「無 item 且未被特殊規則指定建議休假日」的週六/週日補 item
        /// 4. 補出的 item 同時建立 FF option
        /// </remarks>
        /// <param name="returnData">returnData 物件，主要使用 ValueAry 作為參數輸入。</param>
        /// <returns>回傳 returnData.JsonSerializationt() 的 JSON 字串。</returns>
        [HttpPost("calculate_available_dayoff_dates")]
        public string calculate_available_dayoff_dates([FromBody] returnData returnData)
        {
            init(returnData);
            var timer = new MyTimerBasic();
            returnData.Method = "calculate_available_dayoff_dates";
            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string simple = GetVal("simple");

                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

                object[] obj_dayOffScheduleForm = sql_dayOffScheduleFormClass
                    .GetRowsByDefult(null, "form_name", form_name)
                    .FirstOrDefault();

                if (obj_dayOffScheduleForm == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass dayOffScheduleForm = obj_dayOffScheduleForm.SQLToClass<DayOffScheduleFormClass>();

                List<object[]> obj_dayOffScheduleDays = sql_dayOffScheduleDayClass.GetRowsByDefult(null, "form_guid", dayOffScheduleForm.GUID);
                List<object[]> obj_dayOffScheduleItem = sql_dayOffScheduleItemClass.GetRowsByDefult(null, "form_guid", dayOffScheduleForm.GUID);
                List<object[]> obj_staffDayOffOption = sql_staffDayOffOptionClass.GetRowsByDefult(null, "form_guid", dayOffScheduleForm.GUID);

                List<DayOffScheduleDayClass> dayOffScheduleDayClasses = obj_dayOffScheduleDays.SQLToClass<DayOffScheduleDayClass>();
                List<DayOffScheduleItemClass> dayOffScheduleItemClasses = obj_dayOffScheduleItem.SQLToClass<DayOffScheduleItemClass>();
                List<StaffDayOffOptionClass> staffDayOffOptionClasses = obj_staffDayOffOption.SQLToClass<StaffDayOffOptionClass>();

                HashSet<string> existsOptionKeySet = staffDayOffOptionClasses
                    .Where(x => x != null)
                    .Select(x =>
                    {
                        string dt = x.date.StringToDateTime().ToDateString('-');
                        return $"{x.item_guid}|{x.staff_guid}|{dt}";
                    })
                    .ToHashSet();

                dayOffScheduleForm.days.LockAdd(dayOffScheduleDayClasses);

                if (simple == true.ToString().ToLower())
                {
                    returnData.Code = 200;
                    returnData.Data = dayOffScheduleForm;
                    returnData.Result = "取得資料成功";
                    return returnData.JsonSerializationt(true);
                }

                // =========================================================
                // 先綁既有 option
                // =========================================================
                foreach (var day in dayOffScheduleDayClasses)
                {
                    day.items = dayOffScheduleItemClasses
                        .Where(x => x.day_guid == day.GUID)
                        .ToList();

                    foreach (var item in day.items)
                    {
                        item.option = staffDayOffOptionClasses
                            .Where(x => x.staff_guid == item.staff_guid && x.GUID == item.option_guid)
                            .FirstOrDefault();
                    }
                }

                // =========================================================
                // 建立索引
                // =========================================================
                Dictionary<string, List<DayOffScheduleItemClass>> staffItemDict = dayOffScheduleItemClasses
                    .Where(x => x != null && x.staff_guid.StringIsEmpty() == false)
                    .GroupBy(x => x.staff_guid)
                    .ToDictionary(g => g.Key, g => g.ToList());

                Dictionary<string, DayOffScheduleItemClass> itemKeyIndex = dayOffScheduleItemClasses
                    .Where(x => x != null && x.staff_guid.StringIsEmpty() == false && x.date.StringIsEmpty() == false)
                    .GroupBy(x => $"{x.staff_guid}|{x.date.StringToDateTime().ToDateString('-')}")
                    .ToDictionary(g => g.Key, g => g.First());

                Dictionary<string, List<DayOffScheduleItemClass>> itemIndex =
                    staffItemDict
                        .SelectMany(kv => kv.Value.Select(item => new { staffGuid = kv.Key, item }))
                        .Where(x => x.item != null && x.item.date.StringIsEmpty() == false)
                        .GroupBy(x => $"{x.staffGuid}|{x.item.date.StringToDateTime().ToDateString('-')}")
                        .ToDictionary(g => g.Key, g => g.Select(x => x.item).ToList());

                List<StaffDayOffOptionClass> staffDayOffOptions_add = new List<StaffDayOffOptionClass>();
                List<DayOffScheduleItemClass> dayOffScheduleItems_add = new List<DayOffScheduleItemClass>();
                List<DayOffScheduleItemClass> dayOffScheduleItems_update = new List<DayOffScheduleItemClass>();

                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // =========================================================
                // 特殊規則已保留日期集合
                // key = staff_guid|yyyy-MM-dd
                // =========================================================
                HashSet<string> reservedSuggestedDateSet = new HashSet<string>();

                void AddSuggestedDatesToReservedSet(StaffDayOffOptionClass opt)
                {
                    if (opt == null) return;
                    if (opt.staff_guid.StringIsEmpty()) return;

                    if (opt.suggested_dates_list != null)
                    {
                        foreach (var d in opt.suggested_dates_list)
                        {
                            DateTime dt = d.StringToDateTime();
                            if (dt == DateTime.MinValue) continue;
                            reservedSuggestedDateSet.Add($"{opt.staff_guid}|{dt.ToDateString('-')}");
                        }
                    }

                    DateTime mainDt = opt.date.StringToDateTime();
                    if (mainDt != DateTime.MinValue)
                    {
                        reservedSuggestedDateSet.Add($"{opt.staff_guid}|{mainDt.ToDateString('-')}");
                    }
                }

                bool HasSchedule(DayOffScheduleItemClass item)
                {
                    if (item == null) return false;

                    WorkShiftRequirementClass req = item.workShiftRequirement;
                    if (req == null) return false;
                    if (req.disabled) return false;
                    if (req.RequiredCountBase <= 0) return false;
                    if (req.shift_type.StringIsEmpty()) return false;

                    return true;
                }

             

                StaffDayOffOptionClass BuildForceFFOption(DayOffScheduleItemClass item, DateTime dt)
                {
                    string offDate = dt.ToDateString('-');
                    if (dt.DayOfWeek == DayOfWeek.Saturday) item.shift_requirement = BuildHolidayOffShiftRequirementJson(dt);
                    if (dt.DayOfWeek == DayOfWeek.Sunday) item.shift_requirement = BuildHolidayOffShiftRequirementJson(dt);
                    item.selected_dayoff_type = "FF"; // 若你前端有使用，可留；不需要也可空字串
                                                      // FF 一律整天假        

                    var option = new StaffDayOffOptionClass();
                    option.GUID = Guid.NewGuid().ToString();
                    option.form_guid = item.form_guid;
                    option.item_guid = item.GUID;
                    option.staff_guid = item.staff_guid;
                    option.date = offDate;
                    option.suggested_dates_list = new List<string>() { offDate };
                    option.is_any_date = "false";
                    option.assigned_shift = "OFF";

                    // 週六半天、週日全天
                    if (dt.DayOfWeek == DayOfWeek.Saturday)
                    {
                        option.can_full = "false";
                        option.can_half_am = "true";
                        option.can_half_pm = "false";
                        option.selected_full = "false";
                        option.selected_half_am = "true";
                        option.selected_half_pm = "false";
                        option.is_force_ff = "false";
                    }
                    else
                    {
                        option.can_full = "true";
                        option.can_half_am = "false";
                        option.can_half_pm = "false";
                        option.selected_full = "true";
                        option.selected_half_am = "false";
                        option.selected_half_pm = "false";
                        option.is_force_ff = "true";
                    }

                    option.is_forbidden = "false";
                    option.is_force_ff = "true";
                    option.force_ff_at = now;
                    option.updated_at = now;
                    option.released_at = DateTime.MinValue.ToDateTimeString();
                    return option;
                }

                // =========================================================
                // staff 清單
                // =========================================================
                var staffList = dayOffScheduleItemClasses
                    .Where(x => x != null && x.staff_guid.StringIsEmpty() == false)
                    .GroupBy(x => x.staff_guid)
                    .Select(g => new
                    {
                        staff_guid = g.Key,
                        staff_id = g.First().staff_id,
                        staff_name = g.First().staff_name,
                        staff_simple_name = g.First().staff_simple_name,
                        position = g.First().position
                    })
                    .ToList();

                // =========================================================
                // 先跑特殊規則
                // =========================================================
                foreach (var staffGuid in staffItemDict.Keys.ToList())
                {
                    var staffItems = staffItemDict[staffGuid]
                        .Where(x => x != null && x.date.StringIsEmpty() == false)
                        .OrderBy(x => x.date.StringToDateTime())
                        .ToList();
                
                    foreach (var item in staffItems)
                    {
                        //if (staffGuid != "06faaad9-6ee2-4034-98fd-602710a288b4")
                        //{
                        //    continue;
                        //}
                        if (item == null) continue;

                        if (item.option_guid.StringIsEmpty() == false)
                        {
                            if (item.option != null) AddSuggestedDatesToReservedSet(item.option);
                            continue;
                        }

                        if (!HasSchedule(item)) continue;

                        StaffDayOffOptionClass staffDayOffOptionClass = null;

                        if (staffDayOffOptionClass == null) staffDayOffOptionClass = BuildStaffDayOffSpecialDayOption(item, itemIndex);
                        if (staffDayOffOptionClass == null) staffDayOffOptionClass = BuildStaffDayOffSwingOption(item, itemIndex);
                        if (staffDayOffOptionClass == null) staffDayOffOptionClass = BuildStaffDayOffHolidayOption(item, itemIndex);
                        if (staffDayOffOptionClass == null) staffDayOffOptionClass = BuildStaffDayOffMidnightOption(item, itemIndex);

                        if (staffDayOffOptionClass == null) continue;

                        if (staffDayOffOptionClass.force_ff_at.Check_Date_String() == false) staffDayOffOptionClass.force_ff_at = DateTime.MinValue.ToDateString();

                        string optionDate = staffDayOffOptionClass.date.StringToDateTime().ToDateString('-');
                        string optionKey = $"{staffDayOffOptionClass.item_guid}|{staffDayOffOptionClass.staff_guid}|{optionDate}";

                        if (existsOptionKeySet.Contains(optionKey)) continue;

                        existsOptionKeySet.Add(optionKey);

                        item.option_guid = staffDayOffOptionClass.GUID;
                        item.option = staffDayOffOptionClass;

                        dayOffScheduleItems_update.Add(item);
                        staffDayOffOptions_add.Add(staffDayOffOptionClass);
                        AddSuggestedDatesToReservedSet(staffDayOffOptionClass);
                    }
                }

                // =========================================================
                // 再補週六、週日 item + FF option
                // 條件：
                // 1. 該人該日沒有 item
                // 2. 該人該日沒有被特殊規則指定建議休假日
                // =========================================================
                var holidayDays = dayOffScheduleDayClasses
                    .Where(d =>
                    {
                        DateTime dt = d.date.StringToDateTime();
                        return dt != DateTime.MinValue &&
                               (dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday);
                    })
                    .OrderBy(d => d.date.StringToDateTime())
                    .ToList();

                foreach (var day in holidayDays)
                {
                    DateTime dayDt = day.date.StringToDateTime();
                    if (dayDt == DateTime.MinValue) continue;

                    string dt = dayDt.ToDateString('-');

                    foreach (var staff in staffList)
                    {
                        if (staff.staff_guid.StringIsEmpty()) continue;

                        string keyStaffDate = $"{staff.staff_guid}|{dt}";
                        //if (staff.staff_guid != "06faaad9-6ee2-4034-98fd-602710a288b4")
                        //{
                        //    continue;
                        //}
                        // 已有 item -> 不補
                        if (itemKeyIndex.ContainsKey(keyStaffDate)) continue;

                        // 已被特殊規則指定建議休假日 -> 不補
                        if (reservedSuggestedDateSet.Contains(keyStaffDate)) continue;

                        var newItem = new DayOffScheduleItemClass();
                        newItem.GUID = Guid.NewGuid().ToString();
                        newItem.form_guid = dayOffScheduleForm.GUID;
                        newItem.day_guid = day.GUID;
                        newItem.option_guid = "";
                        newItem.date = dt;
                        newItem.is_special_day = "false";

                        newItem.staff_guid = staff.staff_guid;
                        newItem.staff_id = staff.staff_id;
                        newItem.staff_name = staff.staff_name;
                        newItem.staff_simple_name = staff.staff_simple_name;
                        newItem.position = staff.position;

                        newItem.shift_requirement = BuildHolidayOffShiftRequirementJson(dayDt);
                        newItem.created_at = now;
                        newItem.updated_at = now;

                        var ffOpt = BuildForceFFOption(newItem, dayDt);

                        newItem.option_guid = ffOpt.GUID;
                        newItem.option = ffOpt;

                        dayOffScheduleItems_add.Add(newItem);
                        staffDayOffOptions_add.Add(ffOpt);

                        dayOffScheduleItemClasses.Add(newItem);
                        staffDayOffOptionClasses.Add(ffOpt);
                        day.items.Add(newItem);

                        itemKeyIndex[keyStaffDate] = newItem;

                        if (!staffItemDict.ContainsKey(staff.staff_guid))
                            staffItemDict[staff.staff_guid] = new List<DayOffScheduleItemClass>();
                        staffItemDict[staff.staff_guid].Add(newItem);

                        if (!itemIndex.ContainsKey(keyStaffDate))
                            itemIndex[keyStaffDate] = new List<DayOffScheduleItemClass>();
                        itemIndex[keyStaffDate].Add(newItem);

                        existsOptionKeySet.Add($"{newItem.GUID}|{newItem.staff_guid}|{dt}");
                    }
                }

                // =========================================================
                // DB 寫入
                // =========================================================
                if (dayOffScheduleItems_add.Count > 0)
                {
                    sql_dayOffScheduleItemClass.AddRows(null, dayOffScheduleItems_add.ClassToSQL<DayOffScheduleItemClass>());
                }

                if (staffDayOffOptions_add.Count > 0)
                {
                    sql_staffDayOffOptionClass.AddRows(null, staffDayOffOptions_add.ClassToSQL<StaffDayOffOptionClass>());
                }

                if (dayOffScheduleItems_update.Count > 0)
                {
                    sql_dayOffScheduleItemClass.UpdateByDefulteExtra(null, dayOffScheduleItems_update.ClassToSQL<DayOffScheduleItemClass>());
                }

                // =========================================================
                // 回傳前重新綁定
                // =========================================================
                foreach (var day in dayOffScheduleDayClasses)
                {
                    day.items = dayOffScheduleItemClasses
                        .Where(x => x.day_guid == day.GUID)
                        .OrderBy(x => x.date.StringToDateTime())
                        .ThenBy(x => x.staff_id)
                        .ToList();

                    foreach (var item in day.items)
                    {
                        item.option = staffDayOffOptionClasses
                            .Where(x => x.staff_guid == item.staff_guid && x.GUID == item.option_guid)
                            .FirstOrDefault();
                    }
                }

                dayOffScheduleForm.days = dayOffScheduleDayClasses
                    .OrderBy(x => x.date.StringToDateTime())
                    .ToList();

                returnData.Code = 200;
                returnData.Data = dayOffScheduleForm;
                returnData.Result =
                    $"計算完成，新增 item {dayOffScheduleItems_add.Count} 筆，新增 option {staffDayOffOptions_add.Count} 筆，更新 item {dayOffScheduleItems_update.Count} 筆";
                returnData.TimeTaken = $"{timer}";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                returnData.TimeTaken = $"{timer}";
                return returnData.JsonSerializationt();
            }
        }

        /// <summary>
        /// 取得指定排休表單中「每日可放假名額 / 已選擇名額 / FF 人數 / 剩餘名額 / 建議池 / 建議可再選」統計資料（get_dayoff_day_capacity_summary）
        /// </summary>
        /// <remarks>
        /// ===============================
        /// 【API 說明】
        /// ===============================
        /// 本 API 用於前端顯示「每日放假名額統計」，並將統計結果直接併入 DayOffScheduleDayClass 的非 SQL 欄位：
        ///
        /// (A) Capacity（每日上限名額）
        /// - am_capacity = day.am_max_dayoff_count
        /// - pm_capacity = day.pm_max_dayoff_count
        ///
        /// (B) Selected（已選擇休假名額）
        /// - am_selected：AM 半天 + FF 佔用
        /// - pm_selected：PM 半天 + FF 佔用
        /// - ff_selected：只算 FF 人數（方便前端顯示 badge）
        ///
        /// (C) Remaining（剩餘名額）
        /// - am_remaining = am_capacity - am_selected
        /// - pm_remaining = pm_capacity - pm_selected
        /// - is_am_full / is_pm_full：remaining <= 0
        ///
        /// (D) Suggest（建議池 / 建議可再選名額）
        /// ✅ 注意：suggested_dates_list 通常「不會包含 option 當天」，因此統計方式必須「反向統計」：
        /// - 以整張表單所有 staff_dayoff_option 的 suggested_dates_list 來累加到各日期
        ///
        /// - am_suggest_pool：建議今天可選「上午」的人數（can_full=true 或 can_half_am=true 且 suggested_dates_list 包含今天）
        /// - pm_suggest_pool：建議今天可選「下午」的人數（can_full=true 或 can_half_pm=true 且 suggested_dates_list 包含今天）
        /// - am_suggest_count：今天實際「最多能讓幾個人真的去挑(上午)」= max(0, min(am_remaining, am_suggest_pool))
        /// - pm_suggest_count：今天實際「最多能讓幾個人真的去挑(下午)」= max(0, min(pm_remaining, pm_suggest_pool))
        ///
        /// ===============================
        /// 【URL】
        /// ===============================
        /// POST /phar_roster_api/DayOffSchedule/get_dayoff_day_capacity_summary
        ///
        /// ===============================
        /// 【Method】
        /// ===============================
        /// POST
        ///
        /// ===============================
        /// 【傳入參數】(ValueAry)
        /// ===============================
        /// form_name = 排休表單名稱（必填）
        ///
        /// ===============================
        /// 【資料來源】
        /// ===============================
        /// 1) dayoff_schedule_form：用 form_name 找 form_guid
        /// 2) dayoff_schedule_day：讀取每日上限名額（am_max_dayoff_count / pm_max_dayoff_count）
        /// 3) dayoff_schedule_item：當日有哪些人有 item（用於 selected 統計 fallback）
        /// 4) staff_dayoff_option：
        ///    - 用 item_guid 對應到 option 取得 selected_xxx 統計
        ///    - 用 suggested_dates_list 做反向統計（SuggestPool）
        ///
        /// ===============================
        /// 【統計規則 - Selected】
        /// ===============================
        /// 以 staff_dayoff_option 的選擇欄位為準：
        /// - selected_full = true  => 當天 AM +1、PM +1、FF +1
        /// - selected_half_am = true => 當天 AM +1
        /// - selected_half_pm = true => 當天 PM +1
        ///
        /// 若 item 沒有對應 option（少數情境），則 fallback 參考 item.selected_dayoff_type：
        /// - "FF" => AM +1、PM +1、FF +1
        /// - "AM" => AM +1
        /// - "PM" => PM +1
        ///
        /// ===============================
        /// 【統計規則 - Remaining】
        /// ===============================
        /// am_remaining = am_capacity - am_selected
        /// pm_remaining = pm_capacity - pm_selected
        /// is_am_full = (am_remaining <= 0)
        /// is_pm_full = (pm_remaining <= 0)
        ///
        /// ===============================
        /// 【統計規則 - SuggestPool（反向統計 suggested_dates_list）】
        /// ===============================
        /// 以整張表單 options 逐筆掃描 suggested_dates_list：
        /// 只統計符合條件者：
        /// - opt.is_forbidden != true
        /// - opt 尚未選擇（selected_full/half_am/half_pm 全為 false）
        /// - opt.suggested_dates_list 包含某一天(date)
        ///
        /// 計入方式：
        /// - 若 can_full=true 或 can_half_am=true，則該 staff 會計入該 date 的 am_suggest_pool
        /// - 若 can_full=true 或 can_half_pm=true，則該 staff 會計入該 date 的 pm_suggest_pool
        ///
        /// 去重規則（避免同一 staff 同一天被多筆 option 重複建議而重複計數）：
        /// - am_suggest_pool 以 staff_guid|date 去重
        /// - pm_suggest_pool 以 staff_guid|date 去重
        ///
        /// ===============================
        /// 【統計規則 - SuggestCount】
        /// ===============================
        /// am_suggest_count = max(0, min(am_remaining, am_suggest_pool))
        /// pm_suggest_count = max(0, min(pm_remaining, pm_suggest_pool))
        ///
        /// ===============================
        /// 【回傳資料】
        /// ===============================
        /// 回傳 List&lt;DayOffScheduleDayClass&gt;，每筆 day 會包含以下「非 SQL」計算欄位：
        /// - am_dayoff_count / pm_dayoff_count / ff_dayoff_count
        /// - am_remaining_count / pm_remaining_count
        /// - is_am_full / is_pm_full
        /// - am_suggest_pool / pm_suggest_pool
        /// - am_suggest_count / pm_suggest_count
        ///
        /// ===============================
        /// 【JSON 傳入範例】
        /// ===============================
        /// {
        ///   "Method": "get_dayoff_day_capacity_summary",
        ///   "ValueAry": [
        ///     "form_name=2026年01月排休表"
        ///   ],
        ///   "Data": {}
        /// }
        ///
        /// ===============================
        /// 【成功回傳 JSON 範例】
        /// ===============================
        /// {
        ///   "Code": 200,
        ///   "Method": "get_dayoff_day_capacity_summary",
        ///   "Result": "取得每日名額/已選擇/剩餘/建議統計成功",
        ///   "Data": [
        ///     {
        ///       "GUID": "DAY_GUID_001",
        ///       "form_guid": "FORM_GUID_001",
        ///       "date": "2026-01-04",
        ///       "am_max_dayoff_count": "3",
        ///       "pm_max_dayoff_count": "3",
        ///
        ///       "am_dayoff_count": "2",
        ///       "pm_dayoff_count": "1",
        ///       "ff_dayoff_count": "1",
        ///
        ///       "am_remaining_count": "1",
        ///       "pm_remaining_count": "2",
        ///       "is_am_full": "false",
        ///       "is_pm_full": "false",
        ///
        ///       "am_suggest_pool": "4",
        ///       "pm_suggest_pool": "2",
        ///       "am_suggest_count": "1",
        ///       "pm_suggest_count": "2"
        ///     }
        ///   ]
        /// }
        ///
        /// ===============================
        /// 【失敗回傳 JSON 範例】
        /// ===============================
        /// (1) 找不到表單
        /// {
        ///   "Code": -200,
        ///   "Method": "get_dayoff_day_capacity_summary",
        ///   "Result": "找不到表單名稱(2026年01月排休表)",
        ///   "Data": null
        /// }
        ///
        /// (2) 例外錯誤
        /// {
        ///   "Code": -200,
        ///   "Method": "get_dayoff_day_capacity_summary",
        ///   "Result": "Exception message ...",
        ///   "Data": null
        /// }
        ///
        /// ===============================
        /// 【注意事項】
        /// ===============================
        /// 1) 本 API 只做統計與回傳，不新增/不修改 DB 資料。
        /// 2) 若前端只需要統計，不要 items 明細，可在回傳前將 day.items 清空以降低傳輸量。
        /// 3) 日期字串一律使用 yyyy-MM-dd 比對（透過 StringToDateTime().ToDateString('-') 正規化）。
        /// </remarks>
        /// <param name="returnData">
        /// 封裝 API 請求內容的物件，主要使用 ValueAry 作為參數輸入。
        /// ValueAry 必須包含：
        /// - form_name=表單名稱
        /// </param>
        /// <returns>
        /// 回傳 returnData.JsonSerializationt() 的 JSON 字串。
        /// </returns>
        [HttpPost("get_dayoff_day_capacity_summary")]
        public string get_dayoff_day_capacity_summary([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_dayoff_day_capacity_summary";
            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];

                string form_name = GetVal("form_name");

                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

                object[] objForm = sql_dayOffScheduleFormClass
                    .GetRowsByDefult(null, "form_name", form_name)
                    .FirstOrDefault();

                if (objForm == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass form = objForm.SQLToClass<DayOffScheduleFormClass>();

                List<DayOffScheduleDayClass> days = sql_dayOffScheduleDayClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleDayClass>();

                List<DayOffScheduleItemClass> items = sql_dayOffScheduleItemClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleItemClass>();

                List<StaffDayOffOptionClass> options = sql_staffDayOffOptionClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<StaffDayOffOptionClass>();

                // item 依 day_guid 分組
                Dictionary<string, List<DayOffScheduleItemClass>> itemsByDayGuid = items
                    .Where(x => x != null && x.day_guid.StringIsEmpty() == false)
                    .GroupBy(x => x.day_guid)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // option 依 item_guid 索引（用於 Selected 統計）
                Dictionary<string, StaffDayOffOptionClass> optByItemGuid = options
                    .Where(x => x != null && x.item_guid.StringIsEmpty() == false)
                    .GroupBy(x => x.item_guid)
                    .ToDictionary(g => g.Key, g => g.First());

                // =========================================================
                // ✅ SuggestPool：反向統計 suggested_dates_list
                // key: yyyy-MM-dd
                // =========================================================
                Dictionary<string, int> suggestPoolAmByDate = new Dictionary<string, int>();
                Dictionary<string, int> suggestPoolPmByDate = new Dictionary<string, int>();

                HashSet<string> suggestDedupAm = new HashSet<string>(); // staff_guid|date
                HashSet<string> suggestDedupPm = new HashSet<string>(); // staff_guid|date

                foreach (var opt in options)
                {
                    if (opt == null) continue;
                    if (opt.staff_guid.StringIsEmpty()) continue;

                    // 不統計 forbidden
                    if (opt.is_forbidden.StringToBool()) continue;

                    // 已經選了就不算建議池
                    bool alreadySelected =
                        opt.selected_full.StringToBool() ||
                        opt.selected_half_am.StringToBool() ||
                        opt.selected_half_pm.StringToBool();
                    if (alreadySelected) continue;

                    if (opt.suggested_dates_list == null || opt.suggested_dates_list.Count == 0) continue;

                    bool canFull = opt.can_full.StringToBool();
                    bool canAm = opt.can_half_am.StringToBool();
                    bool canPm = opt.can_half_pm.StringToBool();

                    foreach (var sd in opt.suggested_dates_list)
                    {
                        string sdt = sd.StringToDateTime().ToDateString('-');
                        if (sdt.StringIsEmpty()) continue;

                        // AM pool：can_full 或 can_half_am
                        if (canFull || canAm)
                        {
                            string dedupKey = $"{opt.staff_guid}|{sdt}";
                            if (suggestDedupAm.Add(dedupKey))
                            {
                                if (!suggestPoolAmByDate.ContainsKey(sdt)) suggestPoolAmByDate[sdt] = 0;
                                suggestPoolAmByDate[sdt]++;
                            }
                        }

                        // PM pool：can_full 或 can_half_pm
                        if (canFull || canPm)
                        {
                            string dedupKey = $"{opt.staff_guid}|{sdt}";
                            if (suggestDedupPm.Add(dedupKey))
                            {
                                if (!suggestPoolPmByDate.ContainsKey(sdt)) suggestPoolPmByDate[sdt] = 0;
                                suggestPoolPmByDate[sdt]++;
                            }
                        }
                    }
                }

                // =========================================================
                // ✅ 逐日統計：Selected / Remaining / SuggestCount
                // =========================================================
                foreach (var day in days)
                {
                    if (day == null) continue;

                    // 若你沒有這個方法，請直接把欄位清空即可（見下方 DayOffScheduleDayClass）
                    day.ResetComputedFields();

                    string dt = day.date.StringToDateTime().ToDateString('-');

                    int amCap = day.am_max_dayoff_count.StringToInt32();
                    int pmCap = day.pm_max_dayoff_count.StringToInt32();

                    int amSelected = 0;
                    int pmSelected = 0;
                    int ffSelected = 0;

                    // ---------------------------
                    // Selected：用當天 items + option
                    // ---------------------------
                    if (itemsByDayGuid.TryGetValue(day.GUID, out var dayItems) && dayItems != null)
                    {
                        //day.items = dayItems;

                        foreach (var item in dayItems)
                        {
                            if (item == null) continue;

                            optByItemGuid.TryGetValue(item.GUID, out var opt);

                            if (opt != null)
                            {
                                bool selFull = opt.selected_full.StringToBool();
                                bool selAm = opt.selected_half_am.StringToBool();
                                bool selPm = opt.selected_half_pm.StringToBool();

                                if (selFull)
                                {
                                    ffSelected++;
                                    amSelected++;
                                    pmSelected++;
                                }
                                else if (selAm)
                                {
                                    amSelected++;
                                }
                                else if (selPm)
                                {
                                    pmSelected++;
                                }
                            }
                            else
                            {
                                // fallback：無 option 時，保守用 item.selected_dayoff_type
                                string t = (item.selected_dayoff_type ?? "").Trim().ToUpper();
                                if (t == "FF")
                                {
                                    ffSelected++;
                                    amSelected++;
                                    pmSelected++;
                                }
                                else if (t == "AM")
                                {
                                    amSelected++;
                                }
                                else if (t == "PM")
                                {
                                    pmSelected++;
                                }
                            }
                        }
                    }
                    else
                    {
                        day.items = new List<DayOffScheduleItemClass>();
                    }

                    // ---------------------------
                    // SuggestPool：反向統計結果
                    // ---------------------------
                    int amSuggestPool = suggestPoolAmByDate.TryGetValue(dt, out var v1) ? v1 : 0;
                    int pmSuggestPool = suggestPoolPmByDate.TryGetValue(dt, out var v2) ? v2 : 0;

                    // ---------------------------
                    // Remaining
                    // ---------------------------
                    int amRemain = amCap - amSelected;
                    int pmRemain = pmCap - pmSelected;

                    // ---------------------------
                    // SuggestCount = min(Remaining, SuggestPool)
                    // ---------------------------
                    int amSuggestCount = System.Math.Max(0, System.Math.Min(amRemain, amSuggestPool));
                    int pmSuggestCount = System.Math.Max(0, System.Math.Min(pmRemain, pmSuggestPool));

                    // ---------------------------
                    // 回填（全部字串）
                    // ---------------------------
                    day.am_dayoff_count = amSelected.ToString();
                    day.pm_dayoff_count = pmSelected.ToString();
                    day.ff_dayoff_count = ffSelected.ToString();

                    day.am_remaining_count = amRemain.ToString();
                    day.pm_remaining_count = pmRemain.ToString();

                    day.is_am_full = (amRemain <= 0) ? "true" : "false";
                    day.is_pm_full = (pmRemain <= 0) ? "true" : "false";

                    day.am_suggest_pool = amSuggestPool.ToString();
                    day.pm_suggest_pool = pmSuggestPool.ToString();

                    day.am_suggest_count = amSuggestCount.ToString();
                    day.pm_suggest_count = pmSuggestCount.ToString();

                    // 若你要「回傳更輕量」，可取消明細
                    // day.items.Clear();
                }

                // 日期排序
                days = days.OrderBy(d => d.date.StringToDateTime()).ToList();

                returnData.Code = 200;
                returnData.Data = days;
                returnData.Result = "取得每日名額/已選擇/剩餘/建議統計成功";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
            finally
            {
                returnData.Result += timer.ToString();
            }
        }

    
        /// <summary>
        /// 設定排休日每日最大可休人數（上午／下午）
        /// </summary>
        /// <remarks>
        /// ## 📘 功能說明  
        /// 本 API 用於批次設定指定表單中每日可休假人數上限。  
        /// 系統會根據前端傳入的 <c>DayOffScheduleDayClass</c> 清單資料，  
        /// 更新對應的 <c>am_max_dayoff_count</c> 與 <c>pm_max_dayoff_count</c> 欄位。  
        ///
        /// - 常用於組長於排休表建立後設定每日休假名額上限。  
        /// - 一次可更新多筆日期（例如整個月的日期設定）。  
        ///
        /// ## ⚙️ 執行流程  
        /// 1. 驗證傳入的 `DayOffScheduleDayClass` 清單資料。  
        /// 2. 若清單為 null 或格式錯誤，回傳錯誤。  
        /// 3. 呼叫 `UpdateByDefulteExtra()` 將多筆排休日資料批次更新至資料庫。  
        /// 4. 回傳更新結果與成功筆數。  
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "set_dayoff_schedule_day_max_count",
        ///   "ValueAry": [
        ///     "form_name=一月排休表"
        ///   ],
        ///   "Data": [
        ///     {
        ///       "GUID": "day001",
        ///       "form_guid": "form001",
        ///       "date": "2026-01-05",
        ///       "am_max_dayoff_count": "2",
        ///       "pm_max_dayoff_count": "3"
        ///     },
        ///     {
        ///       "GUID": "day002",
        ///       "form_guid": "form001",
        ///       "date": "2026-01-06",
        ///       "am_max_dayoff_count": "1",
        ///       "pm_max_dayoff_count": "2"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## 🔍 參數說明  
        /// | 參數名稱 | 類型 | 必填 | 範例 | 說明 |
        /// |------------|------|------|------|------|
        /// | form_name | string | ✅ | 一月排休表 | 所屬排休表名稱 |
        /// | Data | List&lt;DayOffScheduleDayClass&gt; | ✅ | — | 批次更新的排休日清單 |
        /// | am_max_dayoff_count | string | ✅ | 2 | 上午可休假人數上限 |
        /// | pm_max_dayoff_count | string | ✅ | 3 | 下午可休假人數上限 |
        ///
        /// ## 📤 成功回傳範例  
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "set_dayoff_schedule_day_max_count",
        ///   "Result": "更新排休日資料成功,共2筆",
        ///   "Data": [
        ///     {
        ///       "GUID": "day001",
        ///       "date": "2026-01-05",
        ///       "am_max_dayoff_count": "2",
        ///       "pm_max_dayoff_count": "3"
        ///     },
        ///     {
        ///       "GUID": "day002",
        ///       "date": "2026-01-06",
        ///       "am_max_dayoff_count": "1",
        ///       "pm_max_dayoff_count": "2"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## ❌ 錯誤回傳範例  
        /// - 傳入資料為 null：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "傳入排休日資料異常"
        /// }
        /// ```
        /// - 系統例外：  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "Exception : 資料庫更新失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - 更新動作僅修改 <c>am_max_dayoff_count</c> 與 <c>pm_max_dayoff_count</c> 欄位。  
        /// - 更新筆數依據傳入 Data 清單長度計算。  
        /// - 若無對應 GUID 資料，系統會忽略該筆更新。  
        /// - 回傳 Data 內容為成功更新的排休日清單。  
        /// </remarks>
        /// <param name="returnData">
        /// 封裝 API 請求資料的物件，包含：
        /// <list type="bullet">
        /// <item><description><c>ValueAry</c>：包含 form_name。</description></item>
        /// <item><description><c>Data</c>：需更新的 DayOffScheduleDayClass 清單。</description></item>
        /// </list>
        /// </param>
        /// <returns>回傳 JSON 格式字串，包含更新筆數與結果訊息。</returns>
        [HttpPost("set_dayoff_schedule_day_max_count")]
        async public Task<string> set_dayoff_schedule_day_max_count([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "set_dayoff_schedule_day_max_count";
            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];

                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();

                List<DayOffScheduleDayClass> dayOffScheduleDays = returnData.Data.ObjToClass<List<DayOffScheduleDayClass>>();


                if (dayOffScheduleDays == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"傳入排休日資料異常";
                    return returnData.JsonSerializationt();
                }
                string sql = $@"
                          SELECT *
                          FROM {sql_dayOffScheduleDayClass.Database}.{sql_dayOffScheduleDayClass.TableName}
                          WHERE GUID IN @guid";
                List<string> GUIDs = dayOffScheduleDays
                    .Where(x => x != null && x.GUID.StringIsEmpty() == false)
                    .Select(x => x.GUID)
                    .ToList();
                var parameters = new
                {
                    guid = GUIDs,
                };
                List<object[]> list_dayOffScheduleDay = await sql_dayOffScheduleDayClass.WriteCommandAsync(sql, parameters);

                List<DayOffScheduleDayClass> dayOffScheduleDayClasses = list_dayOffScheduleDay.SQLToClass<DayOffScheduleDayClass>();
                List<DayOffScheduleDayClass> dayOffScheduleDayClasse_update = new List<DayOffScheduleDayClass>();

                foreach(var dayOffScheduleDay in dayOffScheduleDays)
                {
                    var dbDayOffScheduleDay = dayOffScheduleDayClasses
                        .Where(x => x.GUID == dayOffScheduleDay.GUID)
                        .FirstOrDefault();
                    if (dbDayOffScheduleDay == null) continue;
                    dbDayOffScheduleDay.am_max_dayoff_count = dayOffScheduleDay.am_max_dayoff_count;
                    dbDayOffScheduleDay.pm_max_dayoff_count = dayOffScheduleDay.pm_max_dayoff_count;
                    dbDayOffScheduleDay.updated_at = DateTime.Now.ToDateTimeString();
                    dayOffScheduleDayClasse_update.Add(dbDayOffScheduleDay);
                }

                sql_dayOffScheduleDayClass.UpdateByDefulteExtra(null, dayOffScheduleDayClasse_update.ClassToSQL<DayOffScheduleDayClass>());
                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Data = dayOffScheduleDayClasse_update;
                returnData.Result = $"更新排休日資料成功,共{dayOffScheduleDayClasse_update.Count}筆";
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
        /// 取得排休表單的所有組別與組內成員（get_dayoff_group）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL  
        /// `POST /phar_roster_api/DayOffSchedule/get_dayoff_group`
        ///
        /// ## 📘 功能說明  
        /// 根據指定的表單名稱 (<c>form_name</c>)，  
        /// 查詢該表單底下所有「排休組別」(<see cref="DayOffGroupClass"/>)  
        /// 及其所屬「組別成員」(<see cref="DayOffGroupMemberClass"/>)。
        ///
        /// ✅ **主要用途**  
        /// - 排休通知順序管理。  
        /// - 週休／特休完成狀態檢查。  
        /// - 前端顯示拖曳式組別與成員排序。  
        ///
        /// ## ⚙️ 執行流程  
        /// 1. 驗證輸入參數 `form_name`。  
        /// 2. 取得表單 GUID。  
        /// 3. 查詢對應的 `dayoff_group` 組別資料。  
        /// 4. 查詢對應的 `dayoff_group_member` 組員資料。  
        /// 5. 根據 `group_guid` 將成員歸類至各組別中。  
        /// 6. 回傳包含組別與成員的階層結構。
        ///
        /// ## 🧩 回傳資料階層  
        /// ```
        /// [
        ///   DayOffGroupClass {
        ///     GUID,
        ///     form_guid,
        ///     order_index,
        ///     members: [
        ///       DayOffGroupMemberClass { ... }
        ///     ]
        ///   }
        /// ]
        /// ```
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "get_dayoff_group",
        ///   "ValueAry": [
        ///     "form_name=一月排休表"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 📤 成功回傳範例  
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Result": "取得資料成功",
        ///   "Data": [
        ///     {
        ///       "GUID": "group-001",
        ///       "form_guid": "form-001",
        ///       "order_index": "1",
        ///       "created_at": "2026-01-12 09:00:00",
        ///       "updated_at": "2026-01-12 09:00:00",
        ///       "members": [
        ///         {
        ///           "GUID": "member-001",
        ///           "form_guid": "form-001",
        ///           "group_guid": "group-001",
        ///           "staff_guid": "staff-001",
        ///           "staff_id": "P001",
        ///           "staff_name": "王小明",
        ///           "order_index": "1",
        ///           "is_weekoff_completed": "true",
        ///           "weekoff_completed_at": "2026-01-15 12:00:00",
        ///           "is_annualleave_completed": "false",
        ///           "annualleave_completed_at": "",
        ///           "created_at": "2026-01-12 09:00:00",
        ///           "updated_at": "2026-01-12 09:00:00"
        ///         },
        ///         {
        ///           "GUID": "member-002",
        ///           "staff_id": "P002",
        ///           "staff_name": "林小美",
        ///           "order_index": "2",
        ///           "is_weekoff_completed": "false"
        ///         }
        ///       ]
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## 🧾 DayOffGroupClass 欄位說明  
        /// | 欄位名稱 | 類型 | 範例 | 說明 |
        /// |------------|------|------|------|
        /// | GUID | string | group-001 | 組別唯一識別碼 |
        /// | form_guid | string | form-001 | 所屬排休表單 GUID |
        /// | order_index | string | 1 | 組別顯示／排序序號 |
        /// | created_at | string | 2026-01-12 09:00:00 | 建立時間 |
        /// | updated_at | string | 2026-01-12 09:00:00 | 更新時間 |
        /// | members | List&lt;DayOffGroupMemberClass&gt; | — | 組內成員清單 |
        ///
        /// ## 🧾 DayOffGroupMemberClass 欄位說明  
        /// | 欄位名稱 | 類型 | 範例 | 說明 |
        /// |------------|------|------|------|
        /// | GUID | string | member-001 | 組別成員唯一識別碼 |
        /// | form_guid | string | form-001 | 所屬表單 GUID |
        /// | group_guid | string | group-001 | 所屬組別 GUID |
        /// | staff_guid | string | staff-001 | 人員唯一識別碼 |
        /// | staff_id | string | P001 | 人員編號 |
        /// | staff_name | string | 王小明 | 人員姓名 |
        /// | order_index | string | 1 | 組內排序序號（數字越小越前） |
        /// | is_weekoff_completed | string | true | 是否完成週休排假 |
        /// | weekoff_completed_at | string | 2026-01-15 12:00:00 | 週休排假完成時間 |
        /// | is_annualleave_completed | string | false | 是否完成特休排假 |
        /// | annualleave_completed_at | string | "" | 特休排假完成時間 |
        /// | created_at | string | 2026-01-12 09:00:00 | 建立時間 |
        /// | updated_at | string | 2026-01-12 09:00:00 | 更新時間 |
        ///
        /// 🟢 **補充說明：**  
        /// - `order_index` 可用於前端拖曳排序顯示組別與組員順序。  
        /// - `is_weekoff_completed` 與 `is_annualleave_completed` 可用於顯示排假完成狀態。  
        /// - 一張表單 (<c>form_guid</c>) 下可有多個組別，每組別可包含多名人員。  
        ///
        /// ## ❌ 錯誤回傳範例  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "找不到表單名稱(一月排休表)"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - URL 為 <c>/phar_roster_api/DayOffSchedule/get_dayoff_group</c>，請以 <c>POST</c> 傳送。  
        /// - 若 <c>form_name</c> 不存在，回傳錯誤碼 <c>-200</c>。  
        /// - 回傳結構中 `members` 為非資料表欄位，用於回傳組內人員排序與狀態。  
        /// - 適用於前端組別排序畫面、排假進度顯示與組長管理功能。
        /// </remarks>
        /// <param name="returnData">封裝 API 請求內容的物件，需包含 form_name。</param>
        /// <returns>回傳包含組別與組員清單的 JSON 結構。</returns>
        [HttpPost("get_dayoff_group")]
        public string get_dayoff_group([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_dayoff_group";
            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];
                string form_name = GetVal("form_name");
                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();

                var sql_dayOffGroupClass = MethodClass.GetSQLControl<DayOffGroupClass>();
                var sql_dayOffGroupMemberClass = MethodClass.GetSQLControl<DayOffGroupMemberClass>();

                object[] obj_dayOffScheduleForm = sql_dayOffScheduleFormClass.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();

                if (obj_dayOffScheduleForm == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass dayOffScheduleForm = obj_dayOffScheduleForm.SQLToClass<DayOffScheduleFormClass>();

                List<object[]> obj_dayOffGroup = sql_dayOffGroupClass.GetRowsByDefult(null, "form_guid", dayOffScheduleForm.GUID);
                List<object[]> obj_dayOffGroupMember = sql_dayOffGroupMemberClass.GetRowsByDefult(null, "form_guid", dayOffScheduleForm.GUID);

                List<DayOffGroupClass> dayOffGroupClasses = obj_dayOffGroup.SQLToClass<DayOffGroupClass>();
                List<DayOffGroupMemberClass> dayOffGroupMemberClasses = obj_dayOffGroupMember.SQLToClass<DayOffGroupMemberClass>();

       

                foreach (var dayOffGroupClass in dayOffGroupClasses)
                {   dayOffGroupClass.members = dayOffGroupMemberClasses
                                                .Where(x => x.group_guid == dayOffGroupClass.GUID)
                                                .ToList();
                }
                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Data = dayOffGroupClasses;
                returnData.Result = "取得資料成功";
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
        /// 自動分組並建立排休組別資料（calculate_dayoff_group）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL  
        /// `POST /phar_roster_api/DayOffSchedule/calculate_dayoff_group`
        ///
        /// ## 📘 功能說明  
        /// 依據指定的排休表單 (<c>form_name</c>) 及設定的每組人數 (<c>group_size</c>)，  
        /// 系統會自動建立「排休通知組別」(<see cref="DayOffGroupClass"/>)  
        /// 與「組別成員」(<see cref="DayOffGroupMemberClass"/>) 的完整結構。
        ///
        /// ✅ **主要用途**  
        /// - 自動分群排休人員（依人數平均分配）。  
        /// - 清除原有組別並重新生成。  
        /// - 預設設定每位成員的週休與特休狀態為「未完成」。  
        /// - 可作為「排休通知順序」或「組別抽籤」的基礎資料來源。
        ///
        /// ## ⚙️ 執行流程  
        /// 1. 驗證 `form_name` 與 `group_size` 是否正確。  
        /// 2. 查詢表單主檔 <c>dayoff_schedule_form</c>。  
        /// 3. 刪除舊的 <c>dayoff_group</c> 與 <c>dayoff_group_member</c> 資料。  
        /// 4. 根據排班人員清單建立新組別，並依序分配至各組。  
        /// 5. 寫入新組別與成員資料後回傳成功結果。
        ///
        /// ## 🧩 回傳資料階層  
        /// ```
        /// [
        ///   DayOffGroupClass {
        ///     GUID,
        ///     form_guid,
        ///     order_index,
        ///     members: [
        ///       DayOffGroupMemberClass { ... }
        ///     ]
        ///   }
        /// ]
        /// ```
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "calculate_dayoff_group",
        ///   "ValueAry": [
        ///     "form_name=一月排休表",
        ///     "group_size=4"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 📤 成功回傳範例  
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Result": "新增組別成功",
        ///   "Data": [
        ///     {
        ///       "GUID": "group-001",
        ///       "form_guid": "form-001",
        ///       "order_index": "1",
        ///       "created_at": "2026-01-16 09:00:00",
        ///       "updated_at": "2026-01-16 09:00:00",
        ///       "members": [
        ///         {
        ///           "GUID": "member-001",
        ///           "form_guid": "form-001",
        ///           "group_guid": "group-001",
        ///           "staff_guid": "staff-001",
        ///           "staff_id": "P001",
        ///           "staff_name": "王小明",
        ///           "order_index": "1",
        ///           "is_weekoff_completed": "false",
        ///           "is_annualleave_completed": "false"
        ///         },
        ///         {
        ///           "GUID": "member-002",
        ///           "staff_guid": "staff-002",
        ///           "staff_id": "P002",
        ///           "staff_name": "林小華",
        ///           "order_index": "2",
        ///           "is_weekoff_completed": "false",
        ///           "is_annualleave_completed": "false"
        ///         }
        ///       ]
        ///     },
        ///     {
        ///       "GUID": "group-002",
        ///       "form_guid": "form-001",
        ///       "order_index": "2"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## 🧾 參數說明  
        /// | 參數名稱 | 類型 | 必填 | 範例 | 說明 |
        /// |------------|------|------|------|------|
        /// | form_name | string | ✅ | 一月排休表 | 指定要進行自動分組的排休表單名稱 |
        /// | group_size | int | ✅ | 4 | 每組人數設定，必須為整數 |
        ///
        /// ## 🧾 DayOffGroupClass 欄位說明  
        /// | 欄位名稱 | 類型 | 範例 | 說明 |
        /// |------------|------|------|------|
        /// | GUID | string | group-001 | 組別唯一識別碼 |
        /// | form_guid | string | form-001 | 所屬排休表單 GUID |
        /// | order_index | string | 1 | 組別顯示／排序序號 |
        /// | created_at | string | 2026-01-16 09:00:00 | 建立時間 |
        /// | updated_at | string | 2026-01-16 09:00:00 | 更新時間 |
        ///
        /// ## 🧾 DayOffGroupMemberClass 欄位說明  
        /// | 欄位名稱 | 類型 | 範例 | 說明 |
        /// |------------|------|------|------|
        /// | GUID | string | member-001 | 組員唯一識別碼 |
        /// | form_guid | string | form-001 | 所屬表單 GUID |
        /// | group_guid | string | group-001 | 所屬組別 GUID |
        /// | staff_guid | string | staff-001 | 員工唯一識別碼 |
        /// | staff_id | string | P001 | 員工編號 |
        /// | staff_name | string | 王小明 | 員工姓名 |
        /// | order_index | string | 1 | 組內排序序號 |
        /// | is_weekoff_completed | string | false | 是否完成週休排假 |
        /// | is_annualleave_completed | string | false | 是否完成特休排假 |
        /// | created_at | string | 2026-01-16 09:00:00 | 建立時間 |
        /// | updated_at | string | 2026-01-16 09:00:00 | 更新時間 |
        ///
        /// 🟢 **補充說明：**  
        /// - 執行本 API 時會刪除原有的組別與成員資料。  
        /// - 系統依人員 GUID 排序後，按 <c>group_size</c> 進行分組。  
        /// - 組員順序 (<c>order_index</c>) 會自動從 1 起遞增，可於前端重新排序。  
        /// - 本功能常用於建立排假通知組或抽籤分組。  
        ///
        /// ## ❌ 錯誤回傳範例  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "組別人數設定錯誤"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - URL 為 <c>/phar_roster_api/DayOffSchedule/calculate_dayoff_group</c>，請以 <c>POST</c> 傳送。  
        /// - 若 <c>form_name</c> 不存在或 <c>group_size</c> 非數字，回傳錯誤碼 <c>-200</c>。  
        /// - 此 API 會覆蓋現有組別資料，請在重新分組時使用。
        /// </remarks>
        /// <param name="returnData">封裝 API 請求內容的物件，包含表單名稱與每組人數設定。</param>
        /// <returns>回傳自動分組後的組別與成員清單 JSON 結構。</returns>
        [HttpPost("calculate_dayoff_group")]
        public string calculate_dayoff_group([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "calculate_dayoff_group";
            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];
                string form_name = GetVal("form_name");
                string group_size = GetVal("group_size");
                if(group_size.StringIsInt32() == false)
                {
                    returnData.Code = -200;
                    returnData.Result = $"組別人數設定錯誤";
                    return returnData.JsonSerializationt();
                }
                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();

                var sql_dayOffGroupClass = MethodClass.GetSQLControl<DayOffGroupClass>();
                var sql_dayOffGroupMemberClass = MethodClass.GetSQLControl<DayOffGroupMemberClass>();

                object[] obj_dayOffScheduleForm = sql_dayOffScheduleFormClass.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();

                if (obj_dayOffScheduleForm == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass dayOffScheduleForm = obj_dayOffScheduleForm.SQLToClass<DayOffScheduleFormClass>();

                sql_dayOffGroupClass.DeleteByDefult(null, "form_guid", dayOffScheduleForm.GUID);
                sql_dayOffGroupMemberClass.DeleteByDefult(null, "form_guid", dayOffScheduleForm.GUID);

                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
                List<object[]> obj_dayOffScheduleItem = sql_dayOffScheduleItemClass.GetRowsByDefult(null, "form_guid", dayOffScheduleForm.GUID);
                List<DayOffScheduleItemClass> dayOffScheduleItemClasses = obj_dayOffScheduleItem.SQLToClass<DayOffScheduleItemClass>();
                List<string> staffGuids = dayOffScheduleItemClasses
                    .Where(x => x != null && x.staff_guid.StringIsEmpty() == false)
                    .Select(x => x.staff_guid)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

                List<StaffClass> staffClasses_src = staff.GetStaffs(new List<string>()).staffClasses;

                List<StaffClass> staffClasses = new List<StaffClass>();
                foreach (var staffGuid in staffGuids)
                {
                    var staffClass = staffClasses_src
                        .Where(x => x.GUID == staffGuid)
                        .FirstOrDefault();
                    if (staffClass != null)
                    {
                        staffClasses.Add(staffClass);
                    }
                }
                int groupSize = group_size.StringToInt32();

                List<DayOffGroupClass> dayOffGroupClasses = new List<DayOffGroupClass>();
                DayOffGroupClass currentGroup = null;
                List<DayOffGroupMemberClass> dayOffGroupMemberClasses = new List<DayOffGroupMemberClass>();
                int groupIndex = 1;

                for (int i = 0; i < staffClasses.Count; i++)
                {
                    var staffClass = staffClasses[i];
                    if (i % groupSize == 0)
                    {
                        // 新增組別
                        currentGroup = new DayOffGroupClass();
                        currentGroup.GUID = Guid.NewGuid().ToString();
                        currentGroup.form_guid = dayOffScheduleForm.GUID;
                        currentGroup.order_index = groupIndex.ToString();
                        currentGroup.created_at = DateTime.Now.ToDateTimeString();
                        currentGroup.updated_at = DateTime.Now.ToDateTimeString();
                        dayOffGroupClasses.Add(currentGroup);
                        groupIndex++;
                    }
                    // 新增組員
                    DayOffGroupMemberClass member = new DayOffGroupMemberClass();
                    member.GUID = Guid.NewGuid().ToString();
                    member.form_guid = dayOffScheduleForm.GUID;
                    member.group_guid = currentGroup.GUID;
                    member.staff_guid = staffClass.GUID;
                    member.staff_id = staffClass.staff_id;
                    member.staff_name = staffClass.staff_name;
                    member.order_index = ((i % groupSize) + 1).ToString();
                    member.is_weekoff_completed = "false";
                    member.weekoff_completed_at = DateTime.MinValue.ToDateTimeString();
                    member.is_annualleave_completed = "false";
                    member.annualleave_completed_at = DateTime.MinValue.ToDateTimeString();
                    member.created_at = DateTime.Now.ToDateTimeString();
                    member.updated_at = DateTime.Now.ToDateTimeString();
                    dayOffGroupMemberClasses.Add(member);
                }

                sql_dayOffGroupClass.AddRows(null, dayOffGroupClasses.ClassToSQL<DayOffGroupClass>());
                sql_dayOffGroupMemberClass.AddRows(null, dayOffGroupMemberClasses.ClassToSQL<DayOffGroupMemberClass>());

                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Data = dayOffGroupClasses;
                returnData.Result = "新增組別成功";
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
        /// 更新或設定排休組別資料（set_dayoff_group）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL  
        /// `POST /phar_roster_api/DayOffSchedule/set_dayoff_group`
        ///
        /// ## 📘 功能說明  
        /// 前端在調整排休組別、重新排序組員或修改完成狀態後，  
        /// 透過此 API 將更新後的「組別」(<see cref="DayOffGroupClass"/>) 與「組別成員」(<see cref="DayOffGroupMemberClass"/>) 一併回寫至資料庫。
        ///
        /// ✅ **主要用途**  
        /// - 更新已存在的組別與其成員資料。  
        /// - 可用於手動調整組別排序、成員順序或完成狀態。  
        /// - 同步更新兩張資料表：`dayoff_group` 與 `dayoff_group_member`。
        ///
        /// ## ⚙️ 執行流程  
        /// 1. 從 <c>returnData.Data</c> 解析傳入的組別清單。  
        /// 2. 依據每個組別的成員資料展開成完整的 <see cref="DayOffGroupMemberClass"/> 集合。  
        /// 3. 更新組別主檔（Group）與成員檔（Member）資料表。  
        /// 4. 回傳更新結果摘要（組數與成員筆數）。
        ///
        /// ## 🧩 資料結構範例  
        /// ```
        /// [
        ///   DayOffGroupClass {
        ///     GUID,
        ///     form_guid,
        ///     order_index,
        ///     members: [
        ///       DayOffGroupMemberClass { ... }
        ///     ]
        ///   }
        /// ]
        /// ```
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "set_dayoff_group",
        ///   "ValueAry": [],
        ///   "Data": [
        ///     {
        ///       "GUID": "group-001",
        ///       "form_guid": "form-001",
        ///       "order_index": "1",
        ///       "members": [
        ///         {
        ///           "GUID": "member-001",
        ///           "form_guid": "form-001",
        ///           "group_guid": "group-001",
        ///           "staff_guid": "staff-001",
        ///           "staff_id": "P001",
        ///           "staff_name": "王小明",
        ///           "order_index": "1",
        ///           "is_weekoff_completed": "true",
        ///           "weekoff_completed_at": "2026-01-16 09:00:00",
        ///           "is_annualleave_completed": "false",
        ///           "annualleave_completed_at": ""
        ///         },
        ///         {
        ///           "GUID": "member-002",
        ///           "staff_guid": "staff-002",
        ///           "staff_id": "P002",
        ///           "staff_name": "林小華",
        ///           "order_index": "2",
        ///           "is_weekoff_completed": "false",
        ///           "is_annualleave_completed": "false"
        ///         }
        ///       ]
        ///     },
        ///     {
        ///       "GUID": "group-002",
        ///       "form_guid": "form-001",
        ///       "order_index": "2",
        ///       "members": []
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## 📤 成功回傳範例  
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Result": "更新組別成功,共更新<2>組組別,<6>筆成員",
        ///   "Data": [
        ///     {
        ///       "GUID": "group-001",
        ///       "form_guid": "form-001",
        ///       "order_index": "1",
        ///       "members": [...]
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// ## 🧾 DayOffGroupClass 欄位說明  
        /// | 欄位名稱 | 類型 | 範例 | 說明 |
        /// |------------|------|------|------|
        /// | GUID | string | group-001 | 組別唯一識別碼 |
        /// | form_guid | string | form-001 | 所屬排休表單 GUID |
        /// | order_index | string | 1 | 組別顯示／排序序號 |
        /// | created_at | string | 2026-01-16 09:00:00 | 建立時間 |
        /// | updated_at | string | 2026-01-16 09:00:00 | 更新時間 |
        ///
        /// ## 🧾 DayOffGroupMemberClass 欄位說明  
        /// | 欄位名稱 | 類型 | 範例 | 說明 |
        /// |------------|------|------|------|
        /// | GUID | string | member-001 | 組員唯一識別碼 |
        /// | form_guid | string | form-001 | 所屬表單 GUID |
        /// | group_guid | string | group-001 | 所屬組別 GUID |
        /// | staff_guid | string | staff-001 | 員工唯一識別碼 |
        /// | staff_id | string | P001 | 員工編號 |
        /// | staff_name | string | 王小明 | 員工姓名 |
        /// | order_index | string | 1 | 組內排序序號 |
        /// | is_weekoff_completed | string | true | 是否完成週休排假 |
        /// | weekoff_completed_at | string | 2026-01-16 09:00:00 | 週休排假完成時間 |
        /// | is_annualleave_completed | string | false | 是否完成特休排假 |
        /// | annualleave_completed_at | string | "" | 特休排假完成時間 |
        ///
        /// 🟢 **補充說明：**  
        /// - 本 API 僅更新現有資料，不會新增或刪除組別。  
        /// - 若某組別成員清單為空，仍可保留組別。  
        /// - `group_guid` 會自動補齊對應的組別 GUID。  
        /// - 適用於「手動編輯組別」或「前端排序調整後儲存」的場景。
        ///
        /// ## ❌ 錯誤回傳範例  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "資料格式錯誤或更新失敗"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - URL 為 <c>/phar_roster_api/DayOffSchedule/set_dayoff_group</c>，請以 <c>POST</c> 傳送。  
        /// - 請確保每個組別與成員的 GUID 存在且有效。  
        /// - 若傳入 <c>returnData.Data</c> 為空，將不進行任何更新。
        /// </remarks>
        /// <param name="returnData">封裝組別與組員更新資料的物件。</param>
        /// <returns>回傳更新後的組別與組員清單 JSON 結構。</returns>
        [HttpPost("set_dayoff_group")]
        public string set_dayoff_group([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "set_dayoff_group";
            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];
                
                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffGroupClass = MethodClass.GetSQLControl<DayOffGroupClass>();
                var sql_dayOffGroupMemberClass = MethodClass.GetSQLControl<DayOffGroupMemberClass>();


                List<DayOffGroupClass> dayOffGroupClasses = returnData.Data.ObjToClass<List<DayOffGroupClass>>();
                List<DayOffGroupMemberClass> dayOffGroupMemberClasses = new List<DayOffGroupMemberClass>();
                foreach (var group in dayOffGroupClasses)
                {
                    if (group.members != null && group.members.Count > 0)
                    {
                        foreach(var member in group.members)
                        {
                            member.group_guid = group.GUID;
                            dayOffGroupMemberClasses.Add(member);
                        }
                    }
                }
                sql_dayOffGroupClass.UpdateByDefulteExtra(null, dayOffGroupClasses.ClassToSQL<DayOffGroupClass>());
                sql_dayOffGroupMemberClass.UpdateByDefulteExtra(null, dayOffGroupMemberClasses.ClassToSQL<DayOffGroupMemberClass>());

                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Data = dayOffGroupClasses;
                returnData.Result = $"更新組別成功,共更新<{dayOffGroupClasses.Count}>組組別,<{dayOffGroupMemberClasses.Count}>筆成員";
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
        /// 設定是否開放「週休選擇」流程（set_weekoff_selection）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL  
        /// `POST /phar_roster_api/DayOffSchedule/set_weekoff_selection`
        ///
        /// ## 📘 功能說明  
        /// 依據指定的排休表單名稱 (<c>form_name</c>)，
        /// 將該表單的「週休選擇」開放狀態 (<c>enable_weekoff_selection</c>) 設定為 true/false，
        /// 並更新最後變更時間 (<c>weekoff_selection_update_at</c>)。
        ///
        /// ✅ **主要用途**  
        /// - 組長開啟/關閉「週休選擇」流程入口。  
        /// - 控制前端是否允許進入週休排假頁面。  
        /// - 記錄週休選擇狀態最後更新時間，作為稽核/流程追蹤依據。
        ///
        /// ## ⚙️ 執行流程  
        /// 1. 從 <c>returnData.ValueAry</c> 解析 <c>form_name</c> 與 <c>enable</c>。  
        /// 2. 查詢表單主檔 <c>dayoff_schedule_form</c>。  
        /// 3. 若表單不存在，回傳錯誤。  
        /// 4. 更新欄位：  
        ///    - <c>enable_weekoff_selection</c> = "true" 或 "false"  
        ///    - <c>weekoff_selection_update_at</c> = DateTime.Now  
        /// 5. 寫回資料庫並回傳更新後的表單資料。
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "set_weekoff_selection",
        ///   "ValueAry": [
        ///     "form_name=一月排休表",
        ///     "enable=true"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 📤 成功回傳範例  
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Result": "更新資料成功",
        ///   "Data": {
        ///     "GUID": "form-001",
        ///     "form_name": "一月排休表",
        ///     "enable_weekoff_selection": "true",
        ///     "weekoff_selection_update_at": "2026-01-21 10:20:30",
        ///     "enable_annualleave_selection": "false",
        ///     "annualleave_selection_update_at": "0001-01-01 00:00:00",
        ///     "is_completed_locked": "false",
        ///     "created_at": "2026-01-10 09:00:00",
        ///     "updated_at": "2026-01-21 10:20:30"
        ///   }
        /// }
        /// ```
        ///
        /// ## 🧾 參數說明  
        /// | 參數名稱 | 類型 | 必填 | 範例 | 說明 |
        /// |------------|------|------|------|------|
        /// | form_name | string | ✅ | 一月排休表 | 指定要更新的排休表單名稱 |
        /// | enable | bool | ✅ | true | true 開啟週休選擇 / false 關閉週休選擇 |
        ///
        /// ## ❌ 錯誤回傳範例  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "找不到表單名稱(一月排休表)"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - URL 為 <c>/phar_roster_api/DayOffSchedule/set_weekoff_selection</c>，請以 <c>POST</c> 傳送。  
        /// - <c>enable</c> 會以 <c>StringToBool()</c> 解析，建議傳入 true/false。  
        /// - 更新成功後，會同步更新 <c>weekoff_selection_update_at</c>。  
        /// - 若表單名稱不存在，回傳錯誤碼 <c>-200</c>。
        /// </remarks>
        /// <param name="returnData">
        /// 封裝 API 請求內容的物件，
        /// 需在 <c>ValueAry</c> 內包含：
        /// <c>form_name</c>、<c>enable</c>。
        /// </param>
        /// <returns>回傳更新後的表單資料 JSON 結構。</returns>
        [HttpPost("set_weekoff_selection")]
        public string set_weekoff_selection([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "set_weekoff_selection";
            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];
                string form_name = GetVal("form_name");
                string enable = GetVal("enable");
                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();

                object[] obj_dayOffScheduleForm = sql_dayOffScheduleFormClass.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();

                if (obj_dayOffScheduleForm == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass dayOffScheduleForm = obj_dayOffScheduleForm.SQLToClass<DayOffScheduleFormClass>();
                dayOffScheduleForm.enable_weekoff_selection = enable.ToLower() == "true" ? "true" : "false";
                dayOffScheduleForm.weekoff_selection_update_at = DateTime.Now.ToDateTimeString();

                if (dayOffScheduleForm.enable_annualleave_selection.StringIsEmpty()) dayOffScheduleForm.enable_annualleave_selection = "false";
                if (dayOffScheduleForm.enable_weekoff_selection.StringIsEmpty()) dayOffScheduleForm.enable_weekoff_selection = "false";
                if (dayOffScheduleForm.is_completed_locked.StringIsEmpty()) dayOffScheduleForm.is_completed_locked = "false";
                if (dayOffScheduleForm.annualleave_selection_update_at.StringIsEmpty()) dayOffScheduleForm.annualleave_selection_update_at = DateTime.MinValue.ToDateTimeString();
                if (dayOffScheduleForm.weekoff_selection_update_at.StringIsEmpty()) dayOffScheduleForm.weekoff_selection_update_at = DateTime.MinValue.ToDateTimeString();
                if (dayOffScheduleForm.is_completed_locked_update_at.StringIsEmpty()) dayOffScheduleForm.is_completed_locked_update_at = DateTime.MinValue.ToDateTimeString();

                sql_dayOffScheduleFormClass.UpdateByDefulteExtra(null, dayOffScheduleForm.ClassToSQL<DayOffScheduleFormClass>());


                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Data = dayOffScheduleForm;
                returnData.Result = $"更新資料成功";
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
        /// 設定是否開放「特休選擇」流程（set_annualleave_selection）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL  
        /// `POST /phar_roster_api/DayOffSchedule/set_annualleave_selection`
        ///
        /// ## 📘 功能說明  
        /// 依據指定的排休表單名稱 (<c>form_name</c>)，
        /// 將該表單的「特休選擇」開放狀態 (<c>enable_annualleave_selection</c>) 設定為 true/false，
        /// 並更新最後變更時間 (<c>annualleave_selection_update_at</c>)。
        ///
        /// ✅ **主要用途**  
        /// - 組長開啟/關閉「特休選擇」流程入口。  
        /// - 控制前端是否允許進入特休排假頁面。  
        /// - 記錄特休選擇狀態最後更新時間，作為稽核/流程追蹤依據。
        ///
        /// ## ⚙️ 執行流程  
        /// 1. 從 <c>returnData.ValueAry</c> 解析 <c>form_name</c> 與 <c>enable</c>。  
        /// 2. 查詢表單主檔 <c>dayoff_schedule_form</c>。  
        /// 3. 若表單不存在，回傳錯誤。  
        /// 4. 更新欄位：  
        ///    - <c>enable_annualleave_selection</c> = "true" 或 "false"  
        ///    - <c>annualleave_selection_update_at</c> = DateTime.Now  
        /// 5. 寫回資料庫並回傳更新後的表單資料。
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "set_annualleave_selection",
        ///   "ValueAry": [
        ///     "form_name=一月排休表",
        ///     "enable=true"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 📤 成功回傳範例  
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Result": "更新資料成功",
        ///   "Data": {
        ///     "GUID": "form-001",
        ///     "form_name": "一月排休表",
        ///     "enable_weekoff_selection": "true",
        ///     "weekoff_selection_update_at": "2026-01-21 10:20:30",
        ///     "enable_annualleave_selection": "true",
        ///     "annualleave_selection_update_at": "2026-01-21 10:25:00",
        ///     "is_completed_locked": "false",
        ///     "created_at": "2026-01-10 09:00:00",
        ///     "updated_at": "2026-01-21 10:25:00"
        ///   }
        /// }
        /// ```
        ///
        /// ## 🧾 參數說明  
        /// | 參數名稱 | 類型 | 必填 | 範例 | 說明 |
        /// |------------|------|------|------|------|
        /// | form_name | string | ✅ | 一月排休表 | 指定要更新的排休表單名稱 |
        /// | enable | bool | ✅ | true | true 開啟特休選擇 / false 關閉特休選擇 |
        ///
        /// ## ❌ 錯誤回傳範例  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "找不到表單名稱(一月排休表)"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - URL 為 <c>/phar_roster_api/DayOffSchedule/set_annualleave_selection</c>，請以 <c>POST</c> 傳送。  
        /// - <c>enable</c> 會以 <c>StringToBool()</c> 解析，建議傳入 true/false。  
        /// - 更新成功後，會同步更新 <c>annualleave_selection_update_at</c>。  
        /// - 若表單名稱不存在，回傳錯誤碼 <c>-200</c>。
        /// </remarks>
        /// <param name="returnData">
        /// 封裝 API 請求內容的物件，
        /// 需在 <c>ValueAry</c> 內包含：
        /// <c>form_name</c>、<c>enable</c>。
        /// </param>
        /// <returns>回傳更新後的表單資料 JSON 結構。</returns>
        [HttpPost("set_annualleave_selection")]
        public string set_annualleave_selection([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "set_annualleave_selection";
            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];
                string form_name = GetVal("form_name");
                string enable = GetVal("enable");
                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();

                object[] obj_dayOffScheduleForm = sql_dayOffScheduleFormClass.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();

                if (obj_dayOffScheduleForm == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass dayOffScheduleForm = obj_dayOffScheduleForm.SQLToClass<DayOffScheduleFormClass>();
                dayOffScheduleForm.enable_annualleave_selection = enable.ToLower() == "true" ? "true" : "false";
                dayOffScheduleForm.annualleave_selection_update_at = DateTime.Now.ToDateTimeString();

                if (dayOffScheduleForm.enable_annualleave_selection.StringIsEmpty()) dayOffScheduleForm.enable_annualleave_selection = "false";
                if (dayOffScheduleForm.enable_weekoff_selection.StringIsEmpty()) dayOffScheduleForm.enable_weekoff_selection = "false";
                if (dayOffScheduleForm.is_completed_locked.StringIsEmpty()) dayOffScheduleForm.is_completed_locked = "false";
                if (dayOffScheduleForm.annualleave_selection_update_at.StringIsEmpty()) dayOffScheduleForm.annualleave_selection_update_at = DateTime.MinValue.ToDateTimeString();
                if (dayOffScheduleForm.weekoff_selection_update_at.StringIsEmpty()) dayOffScheduleForm.weekoff_selection_update_at = DateTime.MinValue.ToDateTimeString();
                if (dayOffScheduleForm.is_completed_locked_update_at.StringIsEmpty()) dayOffScheduleForm.is_completed_locked_update_at = DateTime.MinValue.ToDateTimeString();

                sql_dayOffScheduleFormClass.UpdateByDefulteExtra(null, dayOffScheduleForm.ClassToSQL<DayOffScheduleFormClass>());


                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Data = dayOffScheduleForm;
                returnData.Result = $"更新資料成功";
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
        /// 設定排休表單是否「完成鎖定」（set_is_completed_locked）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL  
        /// `POST /phar_roster_api/DayOffSchedule/set_is_completed_locked`
        ///
        /// ## 📘 功能說明  
        /// 依據指定的排休表單名稱 (<c>form_name</c>)，
        /// 將該表單的「完成鎖定」狀態 (<c>is_completed_locked</c>) 設定為 true/false，
        /// 並更新最後變更時間 (<c>is_completed_locked_update_at</c>)。
        ///
        /// ✅ **主要用途**  
        /// - 當週休/特休流程完成後，將表單設為「完成鎖定」，避免後續再異動。  
        /// - 控制前端是否允許修改：週休/特休選擇、組別排序、可休名額等。  
        /// - 記錄「完成鎖定」狀態最後更新時間，作為稽核/流程追蹤依據。
        ///
        /// ## ⚙️ 執行流程  
        /// 1. 從 <c>returnData.ValueAry</c> 解析 <c>form_name</c> 與 <c>enable</c>。  
        /// 2. 查詢表單主檔 <c>dayoff_schedule_form</c>。  
        /// 3. 若表單不存在，回傳錯誤。  
        /// 4. 更新欄位：  
        ///    - <c>is_completed_locked</c> = "true" 或 "false"  
        ///    - <c>is_completed_locked_update_at</c> = DateTime.Now  
        /// 5. 寫回資料庫並回傳更新後的表單資料。
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "set_is_completed_locked",
        ///   "ValueAry": [
        ///     "form_name=一月排休表",
        ///     "enable=true"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ## 📤 成功回傳範例  
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Result": "更新資料成功",
        ///   "Data": {
        ///     "GUID": "form-001",
        ///     "form_name": "一月排休表",
        ///     "enable_weekoff_selection": "true",
        ///     "weekoff_selection_update_at": "2026-01-21 10:20:30",
        ///     "enable_annualleave_selection": "true",
        ///     "annualleave_selection_update_at": "2026-01-21 10:25:00",
        ///     "is_completed_locked": "true",
        ///     "is_completed_locked_update_at": "2026-01-21 10:40:10",
        ///     "created_at": "2026-01-10 09:00:00",
        ///     "updated_at": "2026-01-21 10:40:10"
        ///   }
        /// }
        /// ```
        ///
        /// ## 🧾 參數說明  
        /// | 參數名稱 | 類型 | 必填 | 範例 | 說明 |
        /// |------------|------|------|------|------|
        /// | form_name | string | ✅ | 一月排休表 | 指定要更新的排休表單名稱 |
        /// | enable | bool | ✅ | true | true 設為完成鎖定 / false 解除完成鎖定 |
        ///
        /// ## ❌ 錯誤回傳範例  
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "找不到表單名稱(一月排休表)"
        /// }
        /// ```
        ///
        /// ## 📑 注意事項  
        /// - URL 為 <c>/phar_roster_api/DayOffSchedule/set_is_completed_locked</c>，請以 <c>POST</c> 傳送。  
        /// - <c>enable</c> 會以 <c>StringToBool()</c> 解析，建議傳入 true/false。  
        /// - 更新成功後，會同步更新 <c>is_completed_locked_update_at</c>。  
        /// - 若表單名稱不存在，回傳錯誤碼 <c>-200</c>。  
        /// - 建議前端於「完成鎖定=true」時，全面鎖住所有可編輯功能（拖曳排序、名額設定、週休/特休選擇）。  
        /// </remarks>
        /// <param name="returnData">
        /// 封裝 API 請求內容的物件，
        /// 需在 <c>ValueAry</c> 內包含：
        /// <c>form_name</c>、<c>enable</c>。
        /// </param>
        /// <returns>回傳更新後的表單資料 JSON 結構。</returns>
        [HttpPost("set_is_completed_locked")]
        public string set_is_completed_locked([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "set_is_completed_locked";
            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];
                string form_name = GetVal("form_name");
                string enable = GetVal("enable");
                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();

                object[] obj_dayOffScheduleForm = sql_dayOffScheduleFormClass.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();

                if (obj_dayOffScheduleForm == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass dayOffScheduleForm = obj_dayOffScheduleForm.SQLToClass<DayOffScheduleFormClass>();
                dayOffScheduleForm.is_completed_locked = enable.ToLower() == "true" ? "true" : "false";
                dayOffScheduleForm.is_completed_locked_update_at = DateTime.Now.ToDateTimeString();

                if (dayOffScheduleForm.enable_annualleave_selection.StringIsEmpty()) dayOffScheduleForm.enable_annualleave_selection = "false";
                if (dayOffScheduleForm.enable_weekoff_selection.StringIsEmpty()) dayOffScheduleForm.enable_weekoff_selection = "false";
                if (dayOffScheduleForm.is_completed_locked.StringIsEmpty()) dayOffScheduleForm.is_completed_locked = "false";
                if (dayOffScheduleForm.annualleave_selection_update_at.StringIsEmpty()) dayOffScheduleForm.annualleave_selection_update_at = DateTime.MinValue.ToDateTimeString();
                if (dayOffScheduleForm.weekoff_selection_update_at.StringIsEmpty()) dayOffScheduleForm.weekoff_selection_update_at = DateTime.MinValue.ToDateTimeString();
                if (dayOffScheduleForm.is_completed_locked_update_at.StringIsEmpty()) dayOffScheduleForm.is_completed_locked_update_at = DateTime.MinValue.ToDateTimeString();

                sql_dayOffScheduleFormClass.UpdateByDefulteExtra(null, dayOffScheduleForm.ClassToSQL<DayOffScheduleFormClass>());


                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Data = dayOffScheduleForm;
                returnData.Result = $"更新資料成功";
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }

        // ✅ 全域鎖：避免多人同時 init_flow（同一台 API Server 有效）
        private static readonly object _dayoffInitFlowLock = new object();

        /// <summary>
        /// 初始化排休流程：重置指定表單(form_guid)狀態並開放第一組週休，同時強制鎖定其他表單（一次只能有一個表單進入排休流程）。
        /// </summary>
        /// <remarks>
        /// （你的原註解可保留；本版本行為重點如下）
        /// force=false：維持原流程（檢查其他表單進行中→擋下；重置本表單→開第一組→鎖其他表單）
        /// force=true ：只重置此表單（status=0 + 清時間），不開第一組、不鎖其他表單、不做擋下檢查
        /// 另外：weekly_fill_start_at 若為非法日期字串，一律自動修正為 MinValue 字串（force=false 不清時間，只修正非法字串）
        /// </remarks>
        /// <param name="returnData">returnData 物件，主要使用 ValueAry 作為參數輸入。</param>
        /// <returns>回傳 JSON 字串。</returns>
        [HttpPost("init_flow")]
        public string init_flow([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "/phar_roster_api/dayOffSchedule/init_flow";

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry?
                    .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string form_guid = GetVal("form_guid");
                string forceStr = GetVal("force");

                bool force = false;
                if (!forceStr.StringIsEmpty())
                {
                    force = forceStr.Equals("true", StringComparison.OrdinalIgnoreCase) || forceStr.Equals("1");
                }

                if (form_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 form_guid";
                    return returnData.JsonSerializationt();
                }

                var sql_dayOffGroupClass = MethodClass.GetSQLControl<DayOffGroupClass>();

                lock (_dayoffInitFlowLock)
                {
                    string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    string min = DateTime.MinValue.ToDateTimeString();

                    // =========================================================
                    // ❶ 先抓取所有組別（用於檢查是否已有其他表單進行中）
                    // =========================================================
                    List<DayOffGroupClass> allGroups = sql_dayOffGroupClass
                        .GetAllRows(null)
                        .SQLToClass<DayOffGroupClass>();

                    if (allGroups == null || allGroups.Count == 0)
                    {
                        returnData.Code = -200;
                        returnData.Result = "查無任何組別資料 (dayoff_group)";
                        return returnData.JsonSerializationt();
                    }

                    // =========================================================
                    // ❷ force=false：如果其他表單正在排休(status=1 或 3) → 擋下
                    //     force=true ：不擋（救援/解除用）
                    // =========================================================
                    if (!force)
                    {
                        bool otherFormInProgress = allGroups.Any(g =>
                            g != null &&
                            g.form_guid != form_guid &&
                            (g.status == "1" || g.status == "3"));

                        if (otherFormInProgress)
                        {
                            returnData.Code = -200;
                            returnData.Result = "已有其他表單正在排休流程中(status=1/3)，請先完成或使用 force=true 強制解除";
                            return returnData.JsonSerializationt();
                        }
                    }

                    // =========================================================
                    // ❸ 取得本次表單的 groups
                    // =========================================================
                    List<DayOffGroupClass> groups = allGroups
                        .Where(g => g != null && g.form_guid == form_guid)
                        .OrderBy(g => g.order_index.StringToInt32())
                        .ToList();

                    if (groups == null || groups.Count == 0)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"查無組別資料,請先建立組別 form_guid={form_guid}";
                        return returnData.JsonSerializationt();
                    }

                    // =========================================================
                    // ✅ 先做資料防呆：weekly_fill_start_at 非法日期 → 修正為 MinValue（不清時間）
                    //    （force=false / force=true 都做，避免後續流程讀到壞字串卡死）
                    // =========================================================
                    foreach (var g in groups)
                    {
                        if (g == null) continue;

                        g.weekly_fill_start_at = NormalizeDateTimeOrMin(g.weekly_fill_start_at);
                        g.status_changed_at = NormalizeDateTimeOrMin(g.status_changed_at);

                        // 若你只想修 weekly_fill_start_at，不想動其他欄位，就保留這一行即可
                        // 但通常壞資料可能也在其他欄位，這裡不強制修正它們（依你的要求「只修 weekly_fill_start_at」）
                        g.weekly_completed_at = NormalizeDateTimeOrMin(g.weekly_completed_at);
                        g.annual_fill_start_at = NormalizeDateTimeOrMin(g.annual_fill_start_at);
                        g.annual_completed_at = NormalizeDateTimeOrMin(g.annual_completed_at);

                        // ✅ 這一步是「不清時間」：只要有修正（或你希望每次都寫回），就更新資料庫
                        // 為了簡化且一致，這裡直接寫回（不新增欄位、不做額外比對）
                        g.updated_at = now;
                        sql_dayOffGroupClass.UpdateByDefulteExtra(null, g.ClassToSQL<DayOffGroupClass>());
                    }

                    // =========================================================
                    // ✅ force=true：單純解除/重置此表單（不進入流程、不鎖其他表單）
                    // =========================================================
                    if (force)
                    {
                        foreach (var g in allGroups)
                        {
                            if (g == null) continue;

                            g.status = "0";
                            g.status_changed_at = now;
                            g.updated_at = now;

                            // force=true 才清時間
                            g.weekly_fill_start_at = min;
                            g.weekly_completed_at = min;
                            g.annual_fill_start_at = min;
                            g.annual_completed_at = min;

                            sql_dayOffGroupClass.UpdateByDefulteExtra(null, g.ClassToSQL<DayOffGroupClass>());
                        }
                        foreach (var g in groups)
                        {
                            if (g == null) continue;

                            g.status = "0";
                            g.status_changed_at = now;
                            g.updated_at = now;

                            // force=true 才清時間
                            g.weekly_fill_start_at = min;
                            g.weekly_completed_at = min;
                            g.annual_fill_start_at = min;
                            g.annual_completed_at = min;

                            sql_dayOffGroupClass.UpdateByDefulteExtra(null, g.ClassToSQL<DayOffGroupClass>());
                        }
                        returnData.Code = 200;
                        returnData.Result = $"已強制解除：此表單已全重置 status=0 並清空時間欄位（未開第一組、未鎖定其他表單） form_guid={form_guid}";
                        returnData.Data = null;
                        return returnData.JsonSerializationt();
                    }

                    // =========================================================
                    // ❹ force=false：本表單流程已進行 → 拒絕
                    // =========================================================
                    bool alreadyStarted = groups.Any(g => g.status == "2" || g.status == "3" || g.status == "4");
                    if (alreadyStarted)
                    {
                        returnData.Code = -200;
                        returnData.Result = "流程已進行(存在 status=2/3/4)，如需解除請帶入 force=true";
                        return returnData.JsonSerializationt();
                    }

                    // =========================================================
                    // ❺ 本表單：全部鎖定(status=0)（force=false 不清時間）
                    // =========================================================
                    foreach (var g in groups)
                    {
                        if (g == null) continue;

                        g.status = "0";
                        g.status_changed_at = now;
                        g.updated_at = now;

                        // force=false：不清時間（你要求）
                        sql_dayOffGroupClass.UpdateByDefulteExtra(null, g.ClassToSQL<DayOffGroupClass>());
                    }

                    // =========================================================
                    // ❻ 本表單：開放第一組週休(status=1)
                    // =========================================================
                    DayOffGroupClass first = groups.FirstOrDefault();
                    if (first != null)
                    {
                        first.status = "1";
                        first.status_changed_at = now;
                        first.updated_at = now;

                        // force=false：若 weekly_fill_start_at 為空或 MinValue 才寫 now
                        if (first.weekly_fill_start_at.StringIsEmpty() || first.weekly_fill_start_at == min)
                        {
                            first.weekly_fill_start_at = now;
                        }

                        sql_dayOffGroupClass.UpdateByDefulteExtra(null, first.ClassToSQL<DayOffGroupClass>());
                    }

                    // =========================================================
                    // ❼ force=false：強制鎖定其他所有表單（一次只能一張表單排休）
                    // =========================================================
                    var otherFormGroups = allGroups
                        .Where(g => g != null && g.form_guid != form_guid)
                        .ToList();

                    foreach (var og in otherFormGroups)
                    {
                        if (og == null) continue;

                        if (og.status != "0")
                        {
                            og.status = "0";
                            og.status_changed_at = now;
                            og.updated_at = now;

                            sql_dayOffGroupClass.UpdateByDefulteExtra(null, og.ClassToSQL<DayOffGroupClass>());
                        }
                    }

                    // =========================================================
                    // ❽ 回傳
                    // =========================================================
                    returnData.Code = 200;
                    returnData.Result = $"流程初始化完成，已開放第一組週休：{first?.order_index}（其餘表單已強制鎖定）";
                    returnData.Data = new List<DayOffGroupClass>() { first };
                    return returnData.JsonSerializationt();
                }
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
            finally
            {
                returnData.Result += timer.ToString();
            }
        }

        /// <summary>
        /// 完成某組別的填寫階段（週休 / 特休），並自動推進下一組別狀態（含防呆檢查）。
        /// </summary>
        /// <remarks>
        /// ===============================
        /// 【API 說明】
        /// ===============================
        /// 本 API 用於排休流程的「一組一組填寫」推進機制：
        /// 1) 週休流程：
        ///    - 第一組 status="1"(可填週休)
        ///    - 完成後 current 變為 status="2"(週休完成)
        ///    - 系統自動開放下一組 next status="1"
        ///    - 直到所有組別週休完成
        ///
        /// 2) 特休流程：
        ///    - 當且僅當「所有組別週休完成」後，系統開放第一組特休 status="3"(可填特休)
        ///    - 完成後 current 變為 status="4"(特休完成)
        ///    - 系統自動開放下一組 next status="3"
        ///    - 直到所有組別特休完成後流程結束
        ///
        /// ※ 本 API 只負責「完成」與「推進」，不負責「初始化」。
        /// ※ 初始化建議請呼叫 init_flow：第一組 status="1"，其餘組 status="0"。
        ///
        /// ===============================
        /// 【URL】
        /// ===============================
        /// POST /phar_roster_api/dayOffSchedule/complete_stage
        ///
        /// ===============================
        /// 【Method】
        /// ===============================
        /// POST
        ///
        /// ===============================
        /// 【狀態碼(status)定義】(VARCHAR)
        /// ===============================
        /// "0" = 未輪到（鎖定不可填）
        /// "1" = 可填寫週休
        /// "2" = 週休填寫完成
        /// "3" = 可填寫特休
        /// "4" = 特休填寫完成
        ///
        /// ===============================
        /// 【stage 參數說明】
        /// ===============================
        /// stage = "weekly"：完成週休（必須 status="1" 才允許）
        /// stage = "annual"：完成特休（必須 status="3" 才允許）
        ///
        /// ===============================
        /// 【時間欄位寫入規則】(DATETIME)
        /// ===============================
        /// 任何狀態變更：
        /// - status_changed_at = now
        /// - updated_at = now
        ///
        /// stage=weekly：
        /// - current.weekly_completed_at 若空 → 寫入 now
        /// - next.weekly_fill_start_at 若空 → 寫入 now（當 next 從 "0" 變成 "1"）
        ///
        /// stage=annual：
        /// - current.annual_completed_at 若空 → 寫入 now
        /// - next.annual_fill_start_at 若空 → 寫入 now（當 next 從 "2" 變成 "3"）
        ///
        /// ===============================
        /// 【防呆規則】
        /// ===============================
        /// 1) 流程資料唯一性（避免流程亂掉）
        ///    - 週休可填狀態(status="1")同時間只能存在 1 組
        ///    - 特休可填狀態(status="3")同時間只能存在 1 組
        ///    - 不允許同時存在 status="1" 與 status="3"
        ///
        /// 2) 階段不可跳關
        ///    - stage=annual 時，必須所有組別週休完成（不得存在 status="0"/"1"）
        ///    - 若已進入特休階段（存在 status="3"/"4"），禁止再執行 stage=weekly
        ///
        /// 3) 只能由「目前輪到的那一組」完成
        ///    - stage=weekly 時，groups 中唯一 status="1" 必須是 current
        ///    - stage=annual 時，groups 中唯一 status="3" 必須是 current
        ///
        /// 4) next 狀態一致性
        ///    - 週休推進：下一組若存在，next.status 必須為 "0" 才能開放週休
        ///    - 特休推進：下一組若存在，next.status 必須為 "2" 才能開放特休
        ///    - 若 next.status 不符合，代表資料被人為更動或流程異常，將回傳錯誤 (-200)
        ///
        /// ===============================
        /// 【傳入參數】(ValueAry)
        /// ===============================
        /// form_guid   = 排休表單 GUID（必填）
        /// group_guid  = 本次完成的組別 GUID（必填）
        /// stage       = weekly / annual（必填）
        ///
        /// ===============================
        /// 【JSON 傳入範例】
        /// ===============================
        /// (1) 完成週休：
        /// {
        ///   "ValueAry": [
        ///     "form_guid=FORM_GUID_001",
        ///     "group_guid=GROUP_GUID_001",
        ///     "stage=weekly"
        ///   ]
        /// }
        ///
        /// (2) 完成特休：
        /// {
        ///   "ValueAry": [
        ///     "form_guid=FORM_GUID_001",
        ///     "group_guid=GROUP_GUID_001",
        ///     "stage=annual"
        ///   ]
        /// }
        ///
        /// ===============================
        /// 【成功回傳 JSON 範例】
        /// ===============================
        /// ※ 下列 Data 僅為示意（欄位依你的 DayOffGroupClass 為準）
        ///
        /// ------------------------------------------------
        /// 情境 A：完成週休，並開放下一組週休
        /// ------------------------------------------------
        /// {
        ///   "Code": 200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/complete_stage",
        ///   "Result": "週休完成：1，已開放下一組週休：2",
        ///   "Data": [
        ///     {
        ///       "GUID": "GROUP_GUID_001",
        ///       "form_guid": "FORM_GUID_001",
        ///       "order_index": "1",
        ///       "status": "2",
        ///       "weekly_completed_at": "2026-01-21 22:30:00",
        ///       "status_changed_at": "2026-01-21 22:30:00",
        ///       "updated_at": "2026-01-21 22:30:00"
        ///     },
        ///     {
        ///       "GUID": "GROUP_GUID_002",
        ///       "form_guid": "FORM_GUID_001",
        ///       "order_index": "2",
        ///       "status": "1",
        ///       "weekly_fill_start_at": "2026-01-21 22:30:00",
        ///       "status_changed_at": "2026-01-21 22:30:00",
        ///       "updated_at": "2026-01-21 22:30:00"
        ///     }
        ///   ]
        /// }
        ///
        /// ------------------------------------------------
        /// 情境 B：最後一組週休完成，且全部週休完成 → 自動切換到特休並開放第一組特休
        /// ------------------------------------------------
        /// {
        ///   "Code": 200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/complete_stage",
        ///   "Result": "所有組別週休已完成，已切換至特休填寫並開放第一組：1",
        ///   "Data": [
        ///     {
        ///       "GUID": "GROUP_GUID_LAST",
        ///       "order_index": "N",
        ///       "status": "2",
        ///       "weekly_completed_at": "2026-01-21 22:40:00"
        ///     },
        ///     {
        ///       "GUID": "GROUP_GUID_001",
        ///       "order_index": "1",
        ///       "status": "3",
        ///       "annual_fill_start_at": "2026-01-21 22:40:00"
        ///     }
        ///   ]
        /// }
        ///
        /// ------------------------------------------------
        /// 情境 C：完成特休，並開放下一組特休
        /// ------------------------------------------------
        /// {
        ///   "Code": 200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/complete_stage",
        ///   "Result": "特休完成：1，已開放下一組特休：2",
        ///   "Data": [
        ///     {
        ///       "GUID": "GROUP_GUID_001",
        ///       "order_index": "1",
        ///       "status": "4",
        ///       "annual_completed_at": "2026-01-21 23:10:00"
        ///     },
        ///     {
        ///       "GUID": "GROUP_GUID_002",
        ///       "order_index": "2",
        ///       "status": "3",
        ///       "annual_fill_start_at": "2026-01-21 23:10:00"
        ///     }
        ///   ]
        /// }
        ///
        /// ------------------------------------------------
        /// 情境 D：最後一組特休完成，且全部特休完成 → 流程結束
        /// ------------------------------------------------
        /// {
        ///   "Code": 200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/complete_stage",
        ///   "Result": "所有組別特休已完成，流程結束",
        ///   "Data": null
        /// }
        ///
        /// ===============================
        /// 【失敗回傳 JSON 範例】
        /// ===============================
        /// (1) 缺少參數：
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/complete_stage",
        ///   "Result": "未提供 form_guid",
        ///   "Data": null
        /// }
        ///
        /// (2) 目前輪到的組別不是此組：
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/complete_stage",
        ///   "Result": "目前輪到填寫週休的組別非此組，請先取得目前開放組別(status=1)再完成",
        ///   "Data": null
        /// }
        ///
        /// (3) 階段跳關：週休未完成就進特休：
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/complete_stage",
        ///   "Result": "週休尚未全部完成，禁止進入特休階段 (仍存在 status=0 或 status=1)",
        ///   "Data": null
        /// }
        ///
        /// (4) 流程狀態異常：可填狀態同時多組/同時存在週休與特休：
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/complete_stage",
        ///   "Result": "流程狀態異常：同時存在可填週休(status=1)與可填特休(status=3)，請修正資料後再操作",
        ///   "Data": null
        /// }
        ///
        /// (5) next 狀態不符：
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/complete_stage",
        ///   "Result": "流程狀態異常：下一組(order_index=2) status 應為 0 才能開放週休，但實際為 2",
        ///   "Data": null
        /// }
        ///
        /// (6) stage 不支援：
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/complete_stage",
        ///   "Result": "stage 不支援：xxx，僅支援 weekly / annual",
        ///   "Data": null
        /// }
        ///
        /// (7) 例外錯誤：
        /// {
        ///   "Code": -500,
        ///   "Method": "/phar_roster_api/dayOffSchedule/complete_stage",
        ///   "Result": "Exception message ...",
        ///   "Data": null
        /// }
        /// </remarks>
        /// <param name="returnData">
        /// returnData 物件，主要使用 ValueAry 作為參數輸入。
        /// </param>
        /// <returns>
        /// 回傳 returnData.JsonSerializationt() 的 JSON 字串。
        /// </returns>
        [HttpPost("complete_stage")]
        public string complete_stage([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "/phar_roster_api/dayOffSchedule/complete_stage";

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry?
                    .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string form_guid = GetVal("form_guid");
                string group_guid = GetVal("group_guid");
                string stage = GetVal("stage"); // weekly / annual

                if (form_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 form_guid";
                    return returnData.JsonSerializationt();
                }
                if (group_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 group_guid";
                    return returnData.JsonSerializationt();
                }
                if (stage.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 stage (weekly/annual)";
                    return returnData.JsonSerializationt();
                }

                var sql_dayOffGroupClass = MethodClass.GetSQLControl<DayOffGroupClass>();

                // 取得此表單所有組別
                List<DayOffGroupClass> groups = sql_dayOffGroupClass
                    .GetRowsByDefult(null, "form_guid", form_guid)
                    .SQLToClass<DayOffGroupClass>();

                if (groups == null || groups.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"查無組別資料 form_guid={form_guid}";
                    return returnData.JsonSerializationt();
                }

                // 排序（注意 order_index 為 VARCHAR）
                groups = groups
                    .OrderBy(x => x.order_index.StringToInt32())
                    .ToList();

                DayOffGroupClass current = groups.FirstOrDefault(x => x.GUID == group_guid);
                if (current == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"查無 group_guid={group_guid}";
                    return returnData.JsonSerializationt();
                }

                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // =========================
                // ⭐ 防呆：狀態合法性檢查
                // =========================
                // 只能存在單一週休可填（status=1）或單一特休可填（status=3）
                var openWeekly = groups.Where(g => g.status == "1").ToList();
                var openAnnual = groups.Where(g => g.status == "3").ToList();

                // 若同時存在 1 與 3，代表流程資料異常
                if (openWeekly.Count > 0 && openAnnual.Count > 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "流程狀態異常：同時存在可填週休(status=1)與可填特休(status=3)，請修正資料後再操作";
                    return returnData.JsonSerializationt();
                }

                // 若週休可填組超過1
                if (openWeekly.Count > 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"流程狀態異常：可填週休(status=1)組別數量={openWeekly.Count}，應僅允許 1 組";
                    return returnData.JsonSerializationt();
                }

                // 若特休可填組超過1
                if (openAnnual.Count > 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"流程狀態異常：可填特休(status=3)組別數量={openAnnual.Count}，應僅允許 1 組";
                    return returnData.JsonSerializationt();
                }

                // stage=annual 防呆：必須先全部週休完成才能進入特休
                // 也就是不能有 status=0 或 status=1 存在
                bool weeklyAllDone = groups.All(g => g.status != "0" && g.status != "1");

                // 依 stage 決定狀態流轉
                if (stage.Equals("weekly", StringComparison.OrdinalIgnoreCase))
                {
                    // 防呆：如果已進入特休階段（存在 status=3 或 4），不可再完成週休
                    bool annualStarted = groups.Any(g => g.status == "3" || g.status == "4");
                    if (annualStarted)
                    {
                        returnData.Code = -200;
                        returnData.Result = "流程已進入特休階段（存在 status=3/4），不可再完成週休";
                        return returnData.JsonSerializationt();
                    }

                    // 防呆：必須由目前開放那一組完成（唯一 status=1）
                    if (openWeekly.Count != 1 || openWeekly[0].GUID != current.GUID)
                    {
                        returnData.Code = -200;
                        returnData.Result = "目前輪到填寫週休的組別非此組，請先取得目前開放組別(status=1)再完成";
                        return returnData.JsonSerializationt();
                    }

                    // 合法性檢查：必須是可填週休(1) 才能完成
                    if (current.status != "1")
                    {
                        returnData.Code = -200;
                        returnData.Result = $"該組別狀態不允許完成週休 (status={current.status})，必須為 1=可填週休";
                        return returnData.JsonSerializationt();
                    }

                    // 更新當前組：週休完成(2)
                    current.status = "2";
                    current.status_changed_at = now;
                    if (current.weekly_completed_at.StringIsEmpty()) current.weekly_completed_at = now;
                    current.updated_at = now;

                    // 尋找下一組（週休階段下一個要開放的）
                    DayOffGroupClass next = GetNextGroup(groups, current);

                    if (next != null)
                    {
                        // 開放下一組填週休：未輪到(0) -> 可填週休(1)
                        if (next.status == "0")
                        {
                            next.status = "1";
                            next.status_changed_at = now;
                            if (next.weekly_fill_start_at.StringIsEmpty()) next.weekly_fill_start_at = now;
                            next.updated_at = now;

                            sql_dayOffGroupClass.UpdateByDefulteExtra(null, current.ClassToSQL<DayOffGroupClass>());
                            sql_dayOffGroupClass.UpdateByDefulteExtra(null, next.ClassToSQL<DayOffGroupClass>());

                            returnData.Code = 200;
                            returnData.Result = $"週休完成：{current.order_index}，已開放下一組週休：{next.order_index}";
                            returnData.Data = new List<DayOffGroupClass>() { current, next };
                            return returnData.JsonSerializationt();
                        }
                        else
                        {
                            // 防呆：下一組不是 0，代表資料不一致，回傳異常資訊
                            sql_dayOffGroupClass.UpdateByDefulteExtra(null, current.ClassToSQL<DayOffGroupClass>());

                            returnData.Code = -200;
                            returnData.Result = $"流程狀態異常：下一組(order_index={next.order_index}) status 應為 0 才能開放週休，但實際為 {next.status}";
                            return returnData.JsonSerializationt();
                        }
                    }

                    // next == null 代表本次完成的是最後一組週休
                    // 若「全部週休完成」→ 進入特休階段，開放第一組特休(3)
                    bool allWeeklyDone = groups.All(g => g.status == "2" || g.status == "3" || g.status == "4");
                    sql_dayOffGroupClass.UpdateByDefulteExtra(null, current.ClassToSQL<DayOffGroupClass>());

                    if (allWeeklyDone)
                    {
                        DayOffGroupClass first = groups.FirstOrDefault();
                        if (first != null && first.status == "2")
                        {
                            first.status = "3";
                            first.status_changed_at = now;
                            if (first.annual_fill_start_at.StringIsEmpty()) first.annual_fill_start_at = now;
                            first.updated_at = now;

                            sql_dayOffGroupClass.UpdateByDefulteExtra(null, first.ClassToSQL<DayOffGroupClass>());

                            returnData.Code = 200;
                            returnData.Result = $"所有組別週休已完成，已切換至特休填寫並開放第一組：{first.order_index}";
                            returnData.Data = new List<DayOffGroupClass>() { current, first };
                            return returnData.JsonSerializationt();
                        }

                        returnData.Code = -200;
                        returnData.Result = $"流程狀態異常：所有週休完成但第一組狀態非 2，無法自動開放特休 (first.status={first?.status})";
                        return returnData.JsonSerializationt();
                    }

                    returnData.Code = 200;
                    returnData.Result = "週休完成：最後一組已完成，但尚未達成全部週休完成條件";
                    return returnData.JsonSerializationt();
                }
                else if (stage.Equals("annual", StringComparison.OrdinalIgnoreCase))
                {
                    // 防呆：特休必須在週休全完成後才能執行
                    if (!weeklyAllDone)
                    {
                        returnData.Code = -200;
                        returnData.Result = "週休尚未全部完成，禁止進入特休階段 (仍存在 status=0 或 status=1)";
                        return returnData.JsonSerializationt();
                    }

                    // 防呆：必須由目前開放那一組完成（唯一 status=3）
                    if (openAnnual.Count != 1 || openAnnual[0].GUID != current.GUID)
                    {
                        returnData.Code = -200;
                        returnData.Result = "目前輪到填寫特休的組別非此組，請先取得目前開放組別(status=3)再完成";
                        return returnData.JsonSerializationt();
                    }

                    // 合法性檢查：必須是可填特休(3) 才能完成
                    if (current.status != "3")
                    {
                        returnData.Code = -200;
                        returnData.Result = $"該組別狀態不允許完成特休 (status={current.status})，必須為 3=可填特休";
                        return returnData.JsonSerializationt();
                    }

                    // 更新當前組：特休完成(4)
                    current.status = "4";
                    current.status_changed_at = now;
                    if (current.annual_completed_at.StringIsEmpty()) current.annual_completed_at = now;
                    current.updated_at = now;

                    // 開放下一組特休：2 -> 3
                    DayOffGroupClass next = GetNextGroup(groups, current);
                    if (next != null)
                    {
                        if (next.status == "2")
                        {
                            next.status = "3";
                            next.status_changed_at = now;
                            if (next.annual_fill_start_at.StringIsEmpty()) next.annual_fill_start_at = now;
                            next.updated_at = now;

                            sql_dayOffGroupClass.UpdateByDefulteExtra(null, current.ClassToSQL<DayOffGroupClass>());
                            sql_dayOffGroupClass.UpdateByDefulteExtra(null, next.ClassToSQL<DayOffGroupClass>());

                            returnData.Code = 200;
                            returnData.Result = $"特休完成：{current.order_index}，已開放下一組特休：{next.order_index}";
                            returnData.Data = new List<DayOffGroupClass>() { current, next };
                            return returnData.JsonSerializationt();
                        }
                        else
                        {
                            sql_dayOffGroupClass.UpdateByDefulteExtra(null, current.ClassToSQL<DayOffGroupClass>());

                            returnData.Code = -200;
                            returnData.Result = $"流程狀態異常：下一組(order_index={next.order_index}) status 應為 2 才能開放特休，但實際為 {next.status}";
                            return returnData.JsonSerializationt();
                        }
                    }

                    // next == null → 最後一組特休完成
                    sql_dayOffGroupClass.UpdateByDefulteExtra(null, current.ClassToSQL<DayOffGroupClass>());

                    bool allAnnualDone = groups.All(g => g.status == "4");
                    if (allAnnualDone)
                    {
                        returnData.Code = 200;
                        returnData.Result = "所有組別特休已完成，流程結束";
                        return returnData.JsonSerializationt();
                    }

                    returnData.Code = 200;
                    returnData.Result = "特休完成：最後一組已完成，但仍有組別未達成全數完成狀態";
                    return returnData.JsonSerializationt();
                }
                else
                {
                    returnData.Code = -200;
                    returnData.Result = $"stage 不支援：{stage}，僅支援 weekly / annual";
                    return returnData.JsonSerializationt();
                }
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
            finally
            {
                returnData.Result += timer.ToString();
            }
        }

        //填寫者使用的API
        /// <summary>
        /// 取得目前輪到哪一組填寫（週休/特休）的「開放組別」。
        /// </summary>
        /// <remarks>
        /// ===============================
        /// 【API 說明】
        /// ===============================
        /// 本 API 用於前端判斷目前流程進行到哪個階段，以及「目前輪到哪一組」可填寫：
        /// - 若存在 status="1" → 表示目前為週休階段，回傳 stage="weekly" 且 open_group=status=1 之組別
        /// - 若存在 status="3" → 表示目前為特休階段，回傳 stage="annual" 且 open_group=status=3 之組別
        /// - 若兩者皆不存在 → 表示尚未初始化或流程已結束（stage="none"）
        ///
        /// ===============================
        /// 【URL】
        /// ===============================
        /// POST /phar_roster_api/dayOffSchedule/get_current_open_group
        ///
        /// ===============================
        /// 【Method】
        /// ===============================
        /// POST
        ///
        /// ===============================
        /// 【狀態碼(status)定義】(VARCHAR)
        /// ===============================
        /// "0" = 未輪到（鎖定不可填）
        /// "1" = 可填寫週休
        /// "2" = 週休填寫完成
        /// "3" = 可填寫特休
        /// "4" = 特休填寫完成
        ///
        /// ===============================
        /// 【傳入參數】(ValueAry)
        /// ===============================
        /// form_guid = 排休表單 GUID（必填）
        ///
        /// ===============================
        /// 【JSON 傳入範例】
        /// ===============================
        /// {
        ///   "ValueAry": [
        ///     "form_guid=FORM_GUID_001"
        ///   ]
        /// }
        ///
        /// ===============================
        /// 【成功回傳 JSON 範例】
        /// ===============================
        /// ------------------------------------------------
        /// 情境 A：目前為週休階段（存在 status=1）
        /// ------------------------------------------------
        /// {
        ///   "Code": 200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/get_current_open_group",
        ///   "Result": "目前為週休階段，已取得開放組別",
        ///   "Data": {
        ///     "stage": "weekly",
        ///     "open_group": {
        ///       "GUID": "GROUP_GUID_002",
        ///       "form_guid": "FORM_GUID_001",
        ///       "order_index": "2",
        ///       "status": "1",
        ///       "weekly_fill_start_at": "2026-01-21 22:30:00"
        ///     },
        ///     "groups_status_summary": {
        ///       "status_0": 5,
        ///       "status_1": 1,
        ///       "status_2": 1,
        ///       "status_3": 0,
        ///       "status_4": 0
        ///     }
        ///   }
        /// }
        ///
        /// ------------------------------------------------
        /// 情境 B：目前為特休階段（存在 status=3）
        /// ------------------------------------------------
        /// {
        ///   "Code": 200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/get_current_open_group",
        ///   "Result": "目前為特休階段，已取得開放組別",
        ///   "Data": {
        ///     "stage": "annual",
        ///     "open_group": {
        ///       "GUID": "GROUP_GUID_001",
        ///       "form_guid": "FORM_GUID_001",
        ///       "order_index": "1",
        ///       "status": "3",
        ///       "annual_fill_start_at": "2026-01-21 23:10:00"
        ///     },
        ///     "groups_status_summary": {
        ///       "status_0": 0,
        ///       "status_1": 0,
        ///       "status_2": 6,
        ///       "status_3": 1,
        ///       "status_4": 0
        ///     }
        ///   }
        /// }
        ///
        /// ------------------------------------------------
        /// 情境 C：尚未初始化或流程已結束（不存在 status=1/3）
        /// ------------------------------------------------
        /// {
        ///   "Code": 200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/get_current_open_group",
        ///   "Result": "目前無開放組別（尚未初始化或流程已結束）",
        ///   "Data": {
        ///     "stage": "none",
        ///     "open_group": null,
        ///     "groups_status_summary": {
        ///       "status_0": 0,
        ///       "status_1": 0,
        ///       "status_2": 0,
        ///       "status_3": 0,
        ///       "status_4": 7
        ///     }
        ///   }
        /// }
        ///
        /// ===============================
        /// 【失敗回傳 JSON 範例】
        /// ===============================
        /// (1) 缺少 form_guid：
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/get_current_open_group",
        ///   "Result": "未提供 form_guid",
        ///   "Data": null
        /// }
        ///
        /// (2) 查無組別：
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/get_current_open_group",
        ///   "Result": "查無組別資料 form_guid=FORM_GUID_001",
        ///   "Data": null
        /// }
        ///
        /// (3) 流程資料異常（同時存在 status=1 與 status=3 或多組可填）：
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/get_current_open_group",
        ///   "Result": "流程狀態異常：同時存在可填週休(status=1)與可填特休(status=3)，請修正資料後再查詢",
        ///   "Data": null
        /// }
        /// </remarks>
        /// <param name="returnData">
        /// returnData 物件，主要使用 ValueAry 作為參數輸入。
        /// </param>
        /// <returns>
        /// 回傳 returnData.JsonSerializationt() 的 JSON 字串。
        /// </returns>
        [HttpPost("get_current_open_group")]
        public string get_current_open_group([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "/phar_roster_api/dayOffSchedule/get_current_open_group";

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry?
                    .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string form_guid = GetVal("form_guid");
                if (form_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 form_guid";
                    return returnData.JsonSerializationt();
                }

                var sql_dayOffGroupClass = MethodClass.GetSQLControl<DayOffGroupClass>();

                List<DayOffGroupClass> groups = sql_dayOffGroupClass
                    .GetRowsByDefult(null, "form_guid", form_guid)
                    .SQLToClass<DayOffGroupClass>();

                if (groups == null || groups.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"查無組別資料 form_guid={form_guid}";
                    return returnData.JsonSerializationt();
                }

                // 排序（注意 order_index 為 VARCHAR）
                groups = groups
                    .OrderBy(x => x.order_index.StringToInt32())
                    .ToList();

                // 取得目前 open 組（週休 status=1 / 特休 status=3）
                var openWeekly = groups.Where(g => g.status == "1").ToList();
                var openAnnual = groups.Where(g => g.status == "3").ToList();

                // 防呆：不允許同時存在週休與特休 open
                if (openWeekly.Count > 0 && openAnnual.Count > 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "流程狀態異常：同時存在可填週休(status=1)與可填特休(status=3)，請修正資料後再查詢";
                    return returnData.JsonSerializationt();
                }
                // 防呆：open 不允許多組
                if (openWeekly.Count > 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"流程狀態異常：可填週休(status=1)組別數量={openWeekly.Count}，應僅允許 1 組";
                    return returnData.JsonSerializationt();
                }
                if (openAnnual.Count > 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"流程狀態異常：可填特休(status=3)組別數量={openAnnual.Count}，應僅允許 1 組";
                    return returnData.JsonSerializationt();
                }

                // 狀態統計
                var summary = new Dictionary<string, int>()
        {
            { "status_0", groups.Count(x => x.status == "0") },
            { "status_1", groups.Count(x => x.status == "1") },
            { "status_2", groups.Count(x => x.status == "2") },
            { "status_3", groups.Count(x => x.status == "3") },
            { "status_4", groups.Count(x => x.status == "4") },
        };

                // 組裝回傳資料
                string stage = "none";
                DayOffGroupClass openGroup = null;

                if (openWeekly.Count == 1)
                {
                    stage = "weekly";
                    openGroup = openWeekly[0];
                }
                else if (openAnnual.Count == 1)
                {
                    stage = "annual";
                    openGroup = openAnnual[0];
                }

                var data = new DayOffCurrentOpenGroupResponse
                {
                    stage = stage,
                    open_group = openGroup,
                    groups_status_summary = summary
                };

                returnData.Code = 200;
                if (stage == "weekly") returnData.Result = "目前為週休階段，已取得開放組別";
                else if (stage == "annual") returnData.Result = "目前為特休階段，已取得開放組別";
                else returnData.Result = "目前無開放組別（尚未初始化或流程已結束）";

                returnData.Data = data;
                string json = returnData.JsonSerializationt();
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
            finally
            {
                returnData.Result += timer.ToString();
            }
        }

        /// <summary>
        /// 取得排休流程進度（週休/特休完成比例、目前階段、目前輪到組別等）。
        /// </summary>
        /// <remarks>
        /// ===============================
        /// 【API 說明】
        /// ===============================
        /// 本 API 提供前端顯示排休流程進度用，回傳內容包含：
        /// 1) 目前流程階段（weekly / annual / none / finished）
        /// 2) 目前開放可填寫的組別（週休 status=1 或 特休 status=3）
        /// 3) 各狀態組別數量統計（status_0~status_4）
        /// 4) 週休完成進度(%)、特休完成進度(%)、整體進度(%)（週休50% + 特休50%）
        ///
        /// ===============================
        /// 【URL】
        /// ===============================
        /// POST /phar_roster_api/dayOffSchedule/get_flow_progress
        ///
        /// ===============================
        /// 【Method】
        /// ===============================
        /// POST
        ///
        /// ===============================
        /// 【狀態碼(status)定義】(VARCHAR)
        /// ===============================
        /// "0" = 未輪到（鎖定不可填）
        /// "1" = 可填寫週休
        /// "2" = 週休填寫完成
        /// "3" = 可填寫特休
        /// "4" = 特休填寫完成
        ///
        /// ===============================
        /// 【傳入參數】(ValueAry)
        /// ===============================
        /// form_guid = 排休表單 GUID（必填）
        ///
        /// ===============================
        /// 【JSON 傳入範例】
        /// ===============================
        /// {
        ///   "ValueAry": [
        ///     "form_guid=FORM_GUID_001"
        ///   ]
        /// }
        ///
        /// ===============================
        /// 【回傳資料欄位說明】
        /// ===============================
        /// stage：
        /// - "none"     ：尚未初始化（無 status=1 與 status=3）
        /// - "weekly"   ：週休階段（存在 status=1）
        /// - "annual"   ：特休階段（存在 status=3）
        /// - "finished" ：流程結束（所有組別 status=4）
        ///
        /// weekly_progress_percent：週休完成比例（0~100）
        /// annual_progress_percent：特休完成比例（0~100）
        /// overall_progress_percent：整體流程完成比例（0~100），計算：
        /// - weekly_progress_percent * 0.5 + annual_progress_percent * 0.5
        ///
        /// ===============================
        /// 【成功回傳 JSON 範例】
        /// ===============================
        /// {
        ///   "Code": 200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/get_flow_progress",
        ///   "Result": "已取得流程進度",
        ///   "Data": {
        ///     "form_guid": "FORM_GUID_001",
        ///     "stage": "weekly",
        ///     "open_group": {
        ///       "GUID": "GROUP_GUID_002",
        ///       "order_index": "2",
        ///       "status": "1"
        ///     },
        ///     "total_groups": 7,
        ///     "groups_status_summary": {
        ///       "status_0": 5,
        ///       "status_1": 1,
        ///       "status_2": 1,
        ///       "status_3": 0,
        ///       "status_4": 0
        ///     },
        ///     "weekly_progress_percent": 14.2857,
        ///     "annual_progress_percent": 0,
        ///     "overall_progress_percent": 7.1428
        ///   }
        /// }
        ///
        /// ===============================
        /// 【失敗回傳 JSON 範例】
        /// ===============================
        /// (1) 缺少 form_guid：
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/get_flow_progress",
        ///   "Result": "未提供 form_guid",
        ///   "Data": null
        /// }
        ///
        /// (2) 查無組別：
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/get_flow_progress",
        ///   "Result": "查無組別資料 form_guid=FORM_GUID_001",
        ///   "Data": null
        /// }
        ///
        /// (3) 流程狀態異常（同時存在 status=1 與 status=3 或多組可填）：
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/get_flow_progress",
        ///   "Result": "流程狀態異常：同時存在可填週休(status=1)與可填特休(status=3)，請修正資料後再查詢",
        ///   "Data": null
        /// }
        /// </remarks>
        /// <param name="returnData">
        /// returnData 物件，主要使用 ValueAry 作為參數輸入。
        /// </param>
        /// <returns>
        /// 回傳 returnData.JsonSerializationt() 的 JSON 字串。
        /// </returns>
        [HttpPost("get_flow_progress")]
        public string get_flow_progress([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "/phar_roster_api/dayOffSchedule/get_flow_progress";

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry?
                    .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string form_guid = GetVal("form_guid");
                if (form_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 form_guid";
                    return returnData.JsonSerializationt();
                }

                var sql_dayOffGroupClass = MethodClass.GetSQLControl<DayOffGroupClass>();

    
                List<object[]> objects = sql_dayOffGroupClass.GetRowsByDefult(null, "form_guid", form_guid);
                List<DayOffGroupClass> groups = objects.SQLToClass<DayOffGroupClass>();
                if (groups == null || groups.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"查無組別資料 form_guid={form_guid}";
                    return returnData.JsonSerializationt();
                }

                // 排序（order_index 為 VARCHAR）
                groups = groups.OrderBy(x => x.order_index.StringToInt32()).ToList();

                var openWeekly = groups.Where(g => g.status == "1").ToList();
                var openAnnual = groups.Where(g => g.status == "3").ToList();

                // 防呆：同時存在週休與特休 open
                if (openWeekly.Count > 0 && openAnnual.Count > 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "流程狀態異常：同時存在可填週休(status=1)與可填特休(status=3)，請修正資料後再查詢";
                    return returnData.JsonSerializationt();
                }
                // 防呆：open 不允許多組
                if (openWeekly.Count > 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"流程狀態異常：可填週休(status=1)組別數量={openWeekly.Count}，應僅允許 1 組";
                    return returnData.JsonSerializationt();
                }
                if (openAnnual.Count > 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"流程狀態異常：可填特休(status=3)組別數量={openAnnual.Count}，應僅允許 1 組";
                    return returnData.JsonSerializationt();
                }

                int total = groups.Count;
                int count0 = groups.Count(x => x.status == "0");
                int count1 = groups.Count(x => x.status == "1");
                int count2 = groups.Count(x => x.status == "2");
                int count3 = groups.Count(x => x.status == "3");
                int count4 = groups.Count(x => x.status == "4");

                var summary = new Dictionary<string, int>()
        {
            { "status_0", count0 },
            { "status_1", count1 },
            { "status_2", count2 },
            { "status_3", count3 },
            { "status_4", count4 },
        };

                // 階段判斷
                string stage = "none";
                DayOffGroupClass openGroup = null;

                bool finished = groups.All(g => g.status == "4");
                if (finished)
                {
                    stage = "finished";
                }
                else if (openWeekly.Count == 1)
                {
                    stage = "weekly";
                    openGroup = openWeekly[0];
                }
                else if (openAnnual.Count == 1)
                {
                    stage = "annual";
                    openGroup = openAnnual[0];
                }
                else
                {
                    stage = "none";
                }

                // 週休完成比例：status >= 2 視為週休完成
                // (2/3/4 都代表週休完成)
                int weeklyDone = groups.Count(g => g.status == "2" || g.status == "3" || g.status == "4");
                double weeklyPercent = total == 0 ? 0 : (weeklyDone * 100.0 / total);

                // 特休完成比例：status=4 視為特休完成
                int annualDone = groups.Count(g => g.status == "4");
                double annualPercent = total == 0 ? 0 : (annualDone * 100.0 / total);

                // 整體：週休 50% + 特休 50%
                double overallPercent = weeklyPercent * 0.5 + annualPercent * 0.5;

                var data = new DayOffFlowProgressResponse
                {
                    form_guid = form_guid,
                    stage = stage,
                    open_group = openGroup,
                    total_groups = total,
                    groups_status_summary = summary,
                    weekly_progress_percent = Math.Round(weeklyPercent, 4),
                    annual_progress_percent = Math.Round(annualPercent, 4),
                    overall_progress_percent = Math.Round(overallPercent, 4)
                };

                returnData.Code = 200;
                returnData.Result = "已取得流程進度";
                returnData.Data = data;
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
            finally
            {
                returnData.Result += timer.ToString();
            }
        }

        /// <summary>
        /// 查詢目前「正在排休流程」的表單（全系統一次只能有一張表單進入排休）。
        /// </summary>
        /// <remarks>
        /// ===============================
        /// 【API 說明】
        /// ===============================
        /// 本 API 用於查詢目前是否存在正在進行排休流程的表單（Active Form）。
        /// 系統規則為「一次只能有一張表單進入排休」，因此：
        /// - 若存在 status="1"（可填週休）→ 代表某表單正在週休階段
        /// - 若存在 status="3"（可填特休）→ 代表某表單正在特休階段
        /// - 若皆不存在 → 代表目前沒有任何表單正在排休（尚未初始化或已結束）
        ///
        /// 回傳內容包含：
        /// - active_form_guid：目前正在排休的 form_guid
        /// - stage：weekly / annual / none
        /// - open_group：目前開放可填寫的組別
        /// - groups_status_summary：該 form_guid 下各狀態統計
        ///
        /// ===============================
        /// 【URL】
        /// ===============================
        /// POST /phar_roster_api/dayOffSchedule/get_current_active_form
        ///
        /// ===============================
        /// 【Method】
        /// ===============================
        /// POST
        ///
        /// ===============================
        /// 【狀態碼(status)定義】(VARCHAR)
        /// ===============================
        /// "0" = 未輪到（鎖定不可填）
        /// "1" = 可填寫週休
        /// "2" = 週休填寫完成
        /// "3" = 可填寫特休
        /// "4" = 特休填寫完成
        ///
        /// ===============================
        /// 【資料一致性規則（防呆）】
        /// ===============================
        /// 1) 不允許同時存在 status="1" 與 status="3"
        /// 2) status="1" 全系統不允許超過 1 組
        /// 3) status="3" 全系統不允許超過 1 組
        /// 4) 若 active_form_guid 找到，但該表單內無 open_group，回傳 stage="none"
        ///
        /// ===============================
        /// 【JSON 傳入範例】
        /// ===============================
        /// { }
        ///
        /// ===============================
        /// 【成功回傳 JSON 範例】
        /// ===============================
        /// (1) 週休階段
        /// {
        ///   "Code": 200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/get_current_active_form",
        ///   "Result": "目前有表單正在週休階段排休",
        ///   "Data": {
        ///     "active_form_guid": "FORM_GUID_001",
        ///     "stage": "weekly",
        ///     "open_group": {
        ///       "GUID": "GROUP_GUID_002",
        ///       "form_guid": "FORM_GUID_001",
        ///       "order_index": "2",
        ///       "status": "1"
        ///     },
        ///     "groups_status_summary": {
        ///       "status_0": 5,
        ///       "status_1": 1,
        ///       "status_2": 1,
        ///       "status_3": 0,
        ///       "status_4": 0
        ///     }
        ///   }
        /// }
        ///
        /// (2) 無任何表單排休中
        /// {
        ///   "Code": 200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/get_current_active_form",
        ///   "Result": "目前無任何表單正在排休流程中",
        ///   "Data": {
        ///     "active_form_guid": null,
        ///     "stage": "none",
        ///     "open_group": null
        ///   }
        /// }
        ///
        /// ===============================
        /// 【失敗回傳 JSON 範例】
        /// ===============================
        /// (1) 狀態異常：同時存在 status=1 與 status=3
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/get_current_active_form",
        ///   "Result": "流程狀態異常：同時存在可填週休(status=1)與可填特休(status=3)，請修正資料後再查詢",
        ///   "Data": null
        /// }
        ///
        /// (2) 狀態異常：可填週休超過 1 組
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/get_current_active_form",
        ///   "Result": "流程狀態異常：可填週休(status=1)組別數量=2，應僅允許 1 組",
        ///   "Data": null
        /// }
        /// </remarks>
        /// <param name="returnData">returnData 物件（本 API 無需 ValueAry 參數）。</param>
        /// <returns>回傳 returnData.JsonSerializationt() 的 JSON 字串。</returns>
        [HttpPost("get_current_active_form")]
        public string get_current_active_form([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "/phar_roster_api/dayOffSchedule/get_current_active_form";

            try
            {
                var sql_dayOffGroupClass = MethodClass.GetSQLControl<DayOffGroupClass>();

                // 取得全系統所有組別
                List<DayOffGroupClass> allGroups = sql_dayOffGroupClass
                    .GetAllRows(null)
                    .SQLToClass<DayOffGroupClass>();

                if (allGroups == null || allGroups.Count == 0)
                {
                    returnData.Code = 200;
                    returnData.Result = "目前無任何表單正在排休流程中";
                    returnData.Data = new
                    {
                        active_form_guid = (string)null,
                        stage = "none",
                        open_group = (object)null
                    };
                    return returnData.JsonSerializationt();
                }

                // 找出 open group（週休 status=1 / 特休 status=3）
                var openWeekly = allGroups.Where(g => g.status == "1").ToList();
                var openAnnual = allGroups.Where(g => g.status == "3").ToList();

                // 防呆：不允許同時存在週休與特休 open
                if (openWeekly.Count > 0 && openAnnual.Count > 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "流程狀態異常：同時存在可填週休(status=1)與可填特休(status=3)，請修正資料後再查詢";
                    return returnData.JsonSerializationt();
                }

                // 防呆：open 不允許多組
                if (openWeekly.Count > 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"流程狀態異常：可填週休(status=1)組別數量={openWeekly.Count}，應僅允許 1 組";
                    return returnData.JsonSerializationt();
                }
                if (openAnnual.Count > 1)
                {
                    returnData.Code = -200;
                    returnData.Result = $"流程狀態異常：可填特休(status=3)組別數量={openAnnual.Count}，應僅允許 1 組";
                    return returnData.JsonSerializationt();
                }

                string active_form_guid = null;
                string stage = "none";
                DayOffGroupClass openGroup = null;

                if (openWeekly.Count == 1)
                {
                    stage = "weekly";
                    openGroup = openWeekly[0];
                    active_form_guid = openGroup.form_guid;
                }
                else if (openAnnual.Count == 1)
                {
                    stage = "annual";
                    openGroup = openAnnual[0];
                    active_form_guid = openGroup.form_guid;
                }
                else
                {
                    // 無任何 open group
                    returnData.Code = 200;
                    returnData.Result = "目前無任何表單正在排休流程中";
                    returnData.Data = new
                    {
                        active_form_guid = (string)null,
                        stage = "none",
                        open_group = (object)null
                    };
                    return returnData.JsonSerializationt();
                }

                // 取得該表單底下所有 groups，做狀態統計
                var activeFormGroups = allGroups
                    .Where(g => g.form_guid == active_form_guid)
                    .OrderBy(g => g.order_index.StringToInt32())
                    .ToList();

                var summary = DayOffGroupStatusSummary.FromGroups(activeFormGroups);
 
                returnData.Code = 200;
                if (stage == "weekly") returnData.Result = "目前有表單正在週休階段排休";
                else returnData.Result = "目前有表單正在特休階段排休";

                returnData.Data = new DayOffActiveFormResponse
                {
                    active_form_guid = active_form_guid,
                    stage = stage,
                    open_group = openGroup,
                    groups_status_summary = summary
                };

                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
            finally
            {
                returnData.Result += timer.ToString();
            }
        }

        /// <summary>
        /// 登入後查詢：用「DayOffGroupClass」回傳目前輪到填寫與完成狀態。
        /// </summary>
        /// <remarks>
        /// ===============================
        /// ✅ 功能說明
        /// ===============================
        /// 這支 API 讓前端用「同一個結構 DayOffGroupClass」取得排休流程資訊，
        /// 並同時滿足兩種情境：
        ///
        /// (A) staff_id 有值（登入者查詢）
        /// - 回傳「登入者所屬 DayOffGroupClass」
        /// - members 只回登入者自己的 DayOffGroupMemberClass（精簡）
        /// - can_write 代表「是否輪到登入者填寫」
        /// - 是否已完成週休/特休：由 members[0].is_weekoff_completed / is_annualleave_completed 判斷
        ///
        /// (B) staff_id 空值或未提供（管理端/看整組用）
        /// - 回傳「目前開放的 open group」
        /// - members 回傳整組所有成員（依 order_index 排序）
        /// - 供前端顯示整組進度、或管理端看目前輪到哪組
        ///
        /// ===============================
        /// ✅ 參數來源（returnData.ValueAry）
        /// ===============================
        /// - staff_id  : 登入者工號/帳號（可空）
        /// - form_name : 表單名稱（可空）
        ///   - 若未提供 form_name，系統會自動抓「目前 active 的排休表單」
        ///
        /// ===============================
        /// ✅ stage / open_group 判定規則
        /// ===============================
        /// - stage：
        ///   * form 下存在任何 group.status="1" → stage=weekly
        ///   * 否則若存在任何 group.status="3" → stage=annual
        ///   * 否則 stage=none
        ///
        /// - open_group：
        ///   * weekly → status="1" 且 order_index 最小者
        ///   * annual → status="3" 且 order_index 最小者
        ///
        /// - can_write（只在 staff_id 有值時才有意義）：
        ///   * staff_group.GUID == open_group.GUID → can_write=true
        ///
        /// ===============================
        /// ✅ 回傳資料結構（returnData.Data）
        /// ===============================
        /// - DayOffGroupClass
        ///   - members：依情境回「登入者」或「整組」
        ///   - 並會額外填入「非 SQL 欄位」（你需先加到 DayOffGroupClass）：
        ///     stage / stage_name / open_group_guid / open_group_order_index
        ///     is_open_group / can_write / remain_groups_to_open
        ///     message / progress_message
        ///
        /// ===============================
        /// ✅ Request JSON 範例
        /// ===============================
        /// (1) 登入者查詢
        /// {
        ///   "ValueAry": [
        ///     "staff_id=A12345",
        ///     "form_name=2026年03月排休表"
        ///   ]
        /// }
        ///
        /// (2) staff_id 空 → 回整組（目前開放組別）
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表"
        ///   ]
        /// }
        ///
        /// (3) staff_id 空 + form_name 空 → 回整組（active form 的 open group）
        /// {
        ///   "ValueAry": []
        /// }
        /// </remarks>
        /// <param name="returnData">通用傳入物件，使用 returnData.ValueAry 帶參數</param>
        /// <returns>序列化後的 returnData JSON 字串</returns>
        [HttpPost("get_staff_group_status")]
        public string get_staff_group_status([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_staff_group_status";

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry?
                        .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                        ?.Split('=')[1];

                string staff_id = GetVal("staff_id");   // ✅ 可空
                string form_name = GetVal("form_name"); // ✅ 可空

                var sql_staff = MethodClass.GetSQLControl<StaffClass>();
                var sql_form = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_group = MethodClass.GetSQLControl<DayOffGroupClass>();
                var sql_member = MethodClass.GetSQLControl<DayOffGroupMemberClass>();

                // ===============================
                // 1) form（優先 form_name；否則抓 active）
                // ===============================
                DayOffScheduleFormClass form = null;

                if (form_name.StringIsEmpty() == false)
                {
                    object[] obj_form = sql_form.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();
                    if (obj_form == null)
                    {
                        returnData.Code = -404;
                        returnData.Result = $"找不到表單 form_name={form_name}";
                        return returnData.JsonSerializationt();
                    }
                    form = obj_form.SQLToClass<DayOffScheduleFormClass>();
                }
                else
                {
                    form = TryGetActiveForm(sql_form, sql_group);
                }

                if (form == null || form.GUID.StringIsEmpty())
                {
                    // 沒有 active form：回空 group（仍用 DayOffGroupClass）
                    DayOffGroupClass empty = new DayOffGroupClass
                    {
                        GUID = "",
                        form_guid = "",
                        order_index = "0",
                        status = "0",
                        members = new List<DayOffGroupMemberClass>()
                    };

                    SetExtraFields(empty,
                        stage: "none",
                        stageName: "無",
                        openGuid: "",
                        openOrder: "0",
                        isOpenGroup: false,
                        canWrite: false,
                        remainGroups: 0,
                        message: "目前沒有進行中的排休表單",
                        progress: "尚未開放"
                    );

                    returnData.Code = 200;
                    returnData.Result = "success";
                    returnData.Data = empty;
                    return returnData.JsonSerializationt(true);
                }

                string form_guid = form.GUID;

                // ===============================
                // 2) 取 groups
                // ===============================
                List<object[]> obj_groups = sql_group.GetRowsByDefult(null, "form_guid", form_guid);
                List<DayOffGroupClass> groups = obj_groups.SQLToClass<DayOffGroupClass>() ?? new List<DayOffGroupClass>();

                // stage / open_group
                DayOffGroupClass openWeekly = groups
                    .Where(g => g.status == "1")
                    .OrderBy(g => g.order_index.StringToInt32())
                    .FirstOrDefault();

                DayOffGroupClass openAnnual = groups
                    .Where(g => g.status == "3")
                    .OrderBy(g => g.order_index.StringToInt32())
                    .FirstOrDefault();

                string stage = "none";
                string stageName = "無";
                DayOffGroupClass openGroup = null;

                if (openWeekly != null)
                {
                    stage = "weekly";
                    stageName = "週休";
                    openGroup = openWeekly;
                }
                else if (openAnnual != null)
                {
                    stage = "annual";
                    stageName = "特休";
                    openGroup = openAnnual;
                }

                // ===============================
                // 3) staff_id 空：回整組（open group）
                // ===============================
                if (staff_id.StringIsEmpty())
                {
                    // 若目前沒有 open group（stage=none），回空 group
                    if (openGroup == null)
                    {
                        DayOffGroupClass emptyOpen = new DayOffGroupClass
                        {
                            GUID = "",
                            form_guid = form_guid,
                            order_index = "0",
                            status = "0",
                            members = new List<DayOffGroupMemberClass>()
                        };

                        SetExtraFields(emptyOpen,
                            stage: stage,
                            stageName: stageName,
                            openGuid: "",
                            openOrder: "0",
                            isOpenGroup: false,
                            canWrite: false,
                            remainGroups: 0,
                            message: "目前未開放填寫",
                            progress: "尚未開放"
                        );

                        returnData.Code = 200;
                        returnData.Result = "success";
                        returnData.Data = emptyOpen;
                        return returnData.JsonSerializationt(true);
                    }

                    // 取 open group 全員
                    List<object[]> obj_members = sql_member.GetRowsByDefult(null, "form_guid", form_guid);
                    List<DayOffGroupMemberClass> members_all = obj_members.SQLToClass<DayOffGroupMemberClass>() ?? new List<DayOffGroupMemberClass>();

                    openGroup.members = members_all
                        .Where(m => string.Equals(m.group_guid, openGroup.GUID, StringComparison.OrdinalIgnoreCase))
                        .OrderBy(m => m.order_index.StringToInt32())
                        .ToList();

                    SetExtraFields(openGroup,
                        stage: stage,
                        stageName: stageName,
                        openGuid: openGroup.GUID,
                        openOrder: openGroup.order_index ?? "0",
                        isOpenGroup: true,
                        canWrite: false,
                        remainGroups: 0,
                        message: $"目前開放{stageName}填寫：{(openGroup.order_index.StringIsEmpty() ? "" : $"第{openGroup.order_index}組")}",
                        progress: "開放中"
                    );

                    returnData.Code = 200;
                    returnData.Result = "success";
                    returnData.Data = openGroup;
                    return returnData.JsonSerializationt(true);
                }

                // ===============================
                // 4) staff_id 有值：回登入者所屬組別（members 只回登入者）
                // ===============================

                // staff_id -> staff_guid
                object[] obj_staff = sql_staff.GetRowsByDefult(null, "staff_id", staff_id).FirstOrDefault();
                if (obj_staff == null)
                {
                    returnData.Code = -404;
                    returnData.Result = $"找不到 staff_id={staff_id}";
                    return returnData.JsonSerializationt();
                }
                StaffClass staff = obj_staff.SQLToClass<StaffClass>();
                string staff_guid = staff.GUID;

                // 找登入者 member（member 表有 form_guid + staff_guid）
                List<object[]> obj_members2 = sql_member.GetRowsByDefult(null, "form_guid", form_guid);
                List<DayOffGroupMemberClass> members_all2 = obj_members2.SQLToClass<DayOffGroupMemberClass>() ?? new List<DayOffGroupMemberClass>();

                DayOffGroupMemberClass staffMember = members_all2
                    .FirstOrDefault(m => string.Equals(m.staff_guid, staff_guid, StringComparison.OrdinalIgnoreCase));

                if (staffMember == null || staffMember.group_guid.StringIsEmpty())
                {
                    returnData.Code = -404;
                    returnData.Result = $"找不到此 staff 在表單({form.form_name})的 group_member 記錄";
                    return returnData.JsonSerializationt();
                }

                // staff 所屬 group
                DayOffGroupClass staffGroup = groups
                    .FirstOrDefault(g => string.Equals(g.GUID, staffMember.group_guid, StringComparison.OrdinalIgnoreCase));

                if (staffGroup == null)
                {
                    returnData.Code = -404;
                    returnData.Result = $"找不到 staff 所屬 group_guid={staffMember.group_guid}";
                    return returnData.JsonSerializationt();
                }

                // members：只回登入者
                staffGroup.members = new List<DayOffGroupMemberClass> { staffMember };

                // 是否輪到
                bool canWrite = (openGroup != null &&
                                 string.Equals(openGroup.GUID, staffGroup.GUID, StringComparison.OrdinalIgnoreCase));

                // 還差幾組
                int remain = 0;
                if (openGroup != null)
                {
                    remain = staffGroup.order_index.StringToInt32() - openGroup.order_index.StringToInt32();
                    if (remain < 0) remain = 0;
                }

                string msg = BuildMessage(stageName, canWrite, remain, stage);
                string prog = BuildProgressMessage(canWrite, remain, stage);

                SetExtraFields(staffGroup,
                    stage: stage,
                    stageName: stageName,
                    openGuid: openGroup?.GUID ?? "",
                    openOrder: openGroup?.order_index ?? "0",
                    isOpenGroup: canWrite,
                    canWrite: canWrite,
                    remainGroups: remain,
                    message: msg,
                    progress: prog
                );

                returnData.Code = 200;
                returnData.Result = "success";
                returnData.Data = staffGroup;
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = $"Exception : {ex.Message}";
                return returnData.JsonSerializationt();
            }
            finally
            {
                returnData.TimeTaken = timer.ToString();
            }
        }

        /// <summary>
        /// 儲存登入者單筆放假選擇（寫入 staff_dayoff_option），並寫入異動歷程（StaffDayOffOptionLogClass）；不異動 assigned_shift。
        /// </summary>
        /// <remarks>
        /// ===============================
        /// ✅ 功能說明
        /// ===============================
        /// 前端在「週休/特休填寫畫面」中，使用此 API 儲存使用者對某一個 option 的選擇：
        /// - FULL：整天
        /// - HALF_AM：上午半天
        /// - HALF_PM：下午半天
        /// - CANCEL：取消選擇（清空）
        ///
        /// 寫入資料表：staff_dayoff_option
        /// - 只更新：
        ///   selected_full / selected_half_am / selected_half_pm / date / updated_at
        /// - ✅ assigned_shift 不異動
        ///
        /// ✅ 同時寫入 staff_dayoff_option_log（StaffDayOffOptionLogClass）
        /// - 不改資料表欄位的最小修正：
        ///   - action=CANCEL 時，log.off_date 會記錄「取消前的 option.date」，避免 off_date 空白
        ///
        /// ===============================
        /// ✅ 權限/流程檢核
        /// ===============================
        /// 1) 依 form_name 找表單
        /// 2) 依 staff_id 找 staff（轉 staff_guid）
        /// 3) 依 option_guid 找 option
        /// 4) 檢核：
        ///    - option.form_guid == form.GUID
        ///    - option.staff_guid == staff.GUID
        ///    - 必須「輪到該 staff 所屬組別」才能寫（open group）
        ///    - option.is_force_ff == "true" → 不允許變更
        ///    - option.is_forbidden == "true" → 不允許變更
        ///    - option.is_any_date == "false" → off_date 必須在 suggested_dates_list 內
        ///    - 依 can_full / can_half_am / can_half_pm 限制選擇
        ///
        /// ===============================
        /// ✅ 參數（returnData.ValueAry）
        /// ===============================
        /// - form_name  : 表單名稱（必填）
        /// - staff_id   : 登入者工號（必填）
        /// - option_guid: 要寫入的 option GUID（必填）
        /// - select_type: FULL / HALF_AM / HALF_PM / CANCEL（必填）
        /// - off_date   : 選擇日期（yyyy-MM-dd 或 yyyy-MM-dd HH:mm:ss）
        ///              - CANCEL 可不填
        ///
        /// ===============================
        /// ✅ Request JSON 範例
        /// ===============================
        /// (1) 選整天
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "staff_id=A12345",
        ///     "option_guid=OPTION_GUID_001",
        ///     "select_type=FULL",
        ///     "off_date=2026-03-05"
        ///   ]
        /// }
        ///
        /// (2) 取消（不需 off_date）
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "staff_id=A12345",
        ///     "option_guid=OPTION_GUID_001",
        ///     "select_type=CANCEL"
        ///   ]
        /// }
        /// </remarks>
        /// <param name="returnData">通用傳入物件（ValueAry 帶參數）</param>
        /// <returns>序列化後的 returnData JSON 字串</returns>
        [HttpPost("set_staff_dayoff_selection")]
        public string set_staff_dayoff_selection([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "set_staff_dayoff_selection";

            try
            {
                // ===============================
                // 0) 取參數
                // ===============================
                string GetVal(string key) =>
                    returnData.ValueAry?
                        .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                        ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string staff_id = GetVal("staff_id");
                string option_guid = GetVal("option_guid");
                string select_type = GetVal("select_type");
                string off_date = GetVal("off_date");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 form_name";
                    return returnData.JsonSerializationt();
                }
                if (staff_id.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 staff_id";
                    return returnData.JsonSerializationt();
                }
                if (option_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 option_guid";
                    return returnData.JsonSerializationt();
                }
                if (select_type.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 select_type";
                    return returnData.JsonSerializationt();
                }

                select_type = select_type.Trim().ToUpperInvariant();
                bool isCancel = (select_type == "CANCEL");

                if (!isCancel)
                {
                    if (select_type != "FULL" && select_type != "HALF_AM" && select_type != "HALF_PM")
                    {
                        returnData.Code = -200;
                        returnData.Result = "select_type 必須為 FULL / HALF_AM / HALF_PM / CANCEL";
                        return returnData.JsonSerializationt();
                    }

                    if (off_date.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "未輸入 off_date";
                        return returnData.JsonSerializationt();
                    }

                    DateTime dt = off_date.StringToDateTime();
                    if (dt == DateTime.MinValue)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"off_date 格式錯誤: {off_date}";
                        return returnData.JsonSerializationt();
                    }
                    off_date = dt.ToDateString('-'); // yyyy-MM-dd
                }

                // ===============================
                // 1) SQL Controls
                // ===============================
                var sql_form = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_staff = MethodClass.GetSQLControl<StaffClass>();
                var sql_group = MethodClass.GetSQLControl<DayOffGroupClass>();
                var sql_member = MethodClass.GetSQLControl<DayOffGroupMemberClass>();
                var sql_option = MethodClass.GetSQLControl<StaffDayOffOptionClass>();
                var sql_log = MethodClass.GetSQLControl<StaffDayOffOptionLogClass>(); // ✅ Log

                // ===============================
                // 2) form
                // ===============================
                object[] obj_form = sql_form.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();
                if (obj_form == null)
                {
                    returnData.Code = -404;
                    returnData.Result = $"找不到表單 form_name={form_name}";
                    return returnData.JsonSerializationt();
                }
                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();
                string form_guid = form.GUID;

                // ===============================
                // 3) staff_id -> staff_guid
                // ===============================
                object[] obj_staff = sql_staff.GetRowsByDefult(null, "staff_id", staff_id).FirstOrDefault();
                if (obj_staff == null)
                {
                    returnData.Code = -404;
                    returnData.Result = $"找不到 staff_id={staff_id}";
                    return returnData.JsonSerializationt();
                }
                StaffClass staff = obj_staff.SQLToClass<StaffClass>();
                string staff_guid = staff.GUID;

                // ===============================
                // 4) option_guid -> option
                // ===============================
                object[] obj_option = sql_option.GetRowsByDefult(null, "GUID", option_guid).FirstOrDefault();
                if (obj_option == null)
                {
                    returnData.Code = -404;
                    returnData.Result = $"找不到 option_guid={option_guid}";
                    return returnData.JsonSerializationt();
                }
                StaffDayOffOptionClass option = obj_option.SQLToClass<StaffDayOffOptionClass>();

                // form / staff 對應檢核
                if (!string.Equals(option.form_guid, form_guid, StringComparison.OrdinalIgnoreCase))
                {
                    returnData.Code = -200;
                    returnData.Result = "此 option 不屬於該 form_name";
                    return returnData.JsonSerializationt();
                }
                if (!string.Equals(option.staff_guid, staff_guid, StringComparison.OrdinalIgnoreCase))
                {
                    returnData.Code = -403;
                    returnData.Result = "此 option 不屬於登入者";
                    return returnData.JsonSerializationt();
                }

                // ===============================
                // 5) 流程檢核：必須輪到該 staff 所屬組別才能寫
                // ===============================
                List<object[]> obj_groups = sql_group.GetRowsByDefult(null, "form_guid", form_guid);
                List<DayOffGroupClass> groups = obj_groups.SQLToClass<DayOffGroupClass>() ?? new List<DayOffGroupClass>();

                DayOffGroupClass openWeekly = groups
                    .Where(g => g.status == "1")
                    .OrderBy(g => g.order_index.StringToInt32())
                    .FirstOrDefault();

                DayOffGroupClass openAnnual = groups
                    .Where(g => g.status == "3")
                    .OrderBy(g => g.order_index.StringToInt32())
                    .FirstOrDefault();

                string stage = "none";
                DayOffGroupClass openGroup = null;
                if (openWeekly != null)
                {
                    stage = "weekly";
                    openGroup = openWeekly;
                }
                else if (openAnnual != null)
                {
                    stage = "annual";
                    openGroup = openAnnual;
                }

                if (openGroup == null)
                {
                    returnData.Code = -403;
                    returnData.Result = "目前未開放任何組別填寫";
                    return returnData.JsonSerializationt();
                }

                List<object[]> obj_members = sql_member.GetRowsByDefult(null, "form_guid", form_guid);
                List<DayOffGroupMemberClass> members = obj_members.SQLToClass<DayOffGroupMemberClass>() ?? new List<DayOffGroupMemberClass>();

                DayOffGroupMemberClass staffMember = members
                    .FirstOrDefault(m => string.Equals(m.staff_guid, staff_guid, StringComparison.OrdinalIgnoreCase));

                if (staffMember == null || staffMember.group_guid.StringIsEmpty())
                {
                    returnData.Code = -404;
                    returnData.Result = "找不到此 staff 在該表單的 group_member";
                    return returnData.JsonSerializationt();
                }

                bool canWrite = string.Equals(staffMember.group_guid, openGroup.GUID, StringComparison.OrdinalIgnoreCase);
                if (!canWrite)
                {
                    returnData.Code = -403;
                    returnData.Result = "尚未輪到你的組別，禁止儲存";
                    return returnData.JsonSerializationt();
                }

                // ===============================
                // 6) option 狀態檢核（FF / forbidden）
                // ===============================
                if (option.is_force_ff == "true")
                {
                    returnData.Code = -403;
                    returnData.Result = "此放假為系統強制(FF)，不可更改";
                    return returnData.JsonSerializationt();
                }
                if (option.is_forbidden == "true")
                {
                    returnData.Code = -403;
                    returnData.Result = "此放假選項已被管理端禁止";
                    return returnData.JsonSerializationt();
                }

                // ===============================
                // ✅ 7) BEFORE 快照（最小改動：用於 CANCEL log.off_date）
                // ===============================
                string before_date = option.date ?? "";
                string before_selected_full = option.selected_full ?? "false";
                string before_selected_half_am = option.selected_half_am ?? "false";
                string before_selected_half_pm = option.selected_half_pm ?? "false";

                // ===============================
                // 8) 寫入選擇（不改 assigned_shift）
                // ===============================
                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                if (isCancel)
                {
                    // ✅ 取消：清空選擇（option.date 會變空）
                    option.ClearSelection();
                    option.updated_at = now;
                }
                else
                {
                    // is_any_date=false 時，off_date 必須落在 suggested_dates
                    if (option.is_any_date != "true")
                    {
                        var list = option.suggested_dates_list ?? new List<string>();
                        var set = list
                            .Select(x => x.StringToDateTime().ToDateString('-'))
                            .Where(x => x.StringIsEmpty() == false)
                            .ToHashSet();

                        if (!set.Contains(off_date))
                        {
                            returnData.Code = -200;
                            returnData.Result = "off_date 不在建議可選日期中";
                            return returnData.JsonSerializationt();
                        }
                    }

                    string err;
                    bool ok = false;

                    if (select_type == "FULL")
                        ok = option.TrySelectFullDay(off_date, out err);
                    else if (select_type == "HALF_AM")
                        ok = option.TrySelectHalfAM(off_date, out err);
                    else
                        ok = option.TrySelectHalfPM(off_date, out err);

                    if (!ok)
                    {
                        returnData.Code = -200;
                        returnData.Result = err;
                        return returnData.JsonSerializationt();
                    }

                    option.NormalizeSelection();
                    option.updated_at = now;
                }

                // ===============================
                // 9) 更新 DB（只更新 option 本身）
                // ===============================
                sql_option.UpdateByDefulteExtra(null, new List<object[]>
        {
            option.ClassToSQL<StaffDayOffOptionClass>()
        });

                // ===============================
                // ✅ 10) 寫入 Log（最小改動核心：CANCEL 時 off_date 記 before_date）
                // ===============================
                try
                {
                    var log = new StaffDayOffOptionLogClass();

                    // 必填欄位（依你資料表實際欄位調整）
                    log.GUID = Guid.NewGuid().ToString();
                    log.form_guid = form_guid;
                    log.option_guid = option.GUID;
                    log.staff_guid = staff_guid;

                    // 若你 log 表有 staff_id / stage / action / off_date
                    log.staff_id = staff_id;
                    log.stage = stage;
                    log.action = select_type;

                    // ✅ 最小改動修正點：
                    // CANCEL 時 option.date 已清空，所以用 before_date 記錄「取消哪一天」
                    // 非 CANCEL 則記錄這次選擇後的 option.date（等同 off_date）
                    log.off_date = (select_type == "CANCEL") ? before_date : (option.date ?? "");

                    // 如果你的 log 表有 before/after 欄位（有就寫、沒有也不影響你可刪掉）
                    // ↓↓↓ 若你沒有這些欄位，請直接刪除這段，避免編譯錯
                    log.before_selected_full = before_selected_full;
                    log.before_selected_half_am = before_selected_half_am;
                    log.before_selected_half_pm = before_selected_half_pm;

                    log.after_selected_full = option.selected_full ?? "false";
                    log.after_selected_half_am = option.selected_half_am ?? "false";
                    log.after_selected_half_pm = option.selected_half_pm ?? "false";

                    log.created_at = now;

                    sql_log.AddRows(null, new List<object[]>
            {
                log.ClassToSQL<StaffDayOffOptionLogClass>()
            });
                }
                catch
                {
                    // ✅ log 寫入失敗不阻擋主流程（避免影響使用者儲存）
                    // 你若想嚴格：可改成 throw
                }

                // ===============================
                // 11) 回傳
                // ===============================
                returnData.Code = 200;
                returnData.Result = "success";
                returnData.Data = option;
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = $"Exception : {ex.Message}";
                return returnData.JsonSerializationt();
            }
            finally
            {
                returnData.TimeTaken = timer.ToString();
            }
        }

        /// <summary>
        /// 查詢 staff_dayoff_option 的異動歷程（StaffDayOffOptionLogClass）。
        /// </summary>
        /// <remarks>
        /// ===============================
        /// ✅ 功能說明
        /// ===============================
        /// 前端用此 API 取得「放假選擇歷程」清單，可用於：
        /// - 使用者自查：我什麼時候選了哪一天、改了幾次
        /// - 管理端稽核：某張表單的所有修改紀錄
        /// - 追蹤問題：某個 option_guid 的完整操作軌跡
        ///
        /// 本 API 只查詢 staff_dayoff_option_log（不異動任何資料）。
        ///
        /// ===============================
        /// ✅ 參數（returnData.ValueAry）
        /// ===============================
        /// - form_name   : 表單名稱（必填）
        /// - staff_id    : 工號（選填；空字串或未帶 → 回整張表單的所有 log）
        /// - option_guid : option GUID（選填）
        /// - date_start  : 起始時間（選填；yyyy-MM-dd 或 yyyy-MM-dd HH:mm:ss）
        /// - date_end    : 結束時間（選填；yyyy-MM-dd 或 yyyy-MM-dd HH:mm:ss）
        /// - top         : 最多回傳筆數（選填；預設 500，最大 5000）
        ///
        /// ===============================
        /// ✅ 重要行為
        /// ===============================
        /// 1) staff_id 空 → 回整組（整張表單所有 staff 的 log）
        /// 2) 會依 created_at 由新到舊排序（最新在前）
        /// 3) date_start/date_end 若只給日期（yyyy-MM-dd），會自動補：
        ///    - start：00:00:00
        ///    - end  ：23:59:59
        ///
        /// ===============================
        /// ✅ Request JSON 範例
        /// ===============================
        /// (1) 查整張表單（管理端）
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表"
        ///   ]
        /// }
        ///
        /// (2) 查某員工（使用者）
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "staff_id=A12345"
        ///   ]
        /// }
        ///
        /// (3) 查某 option 的完整軌跡
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "option_guid=OPTION_GUID_001"
        ///   ]
        /// }
        ///
        /// (4) 加日期區間 + 限制筆數
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "staff_id=A12345",
        ///     "date_start=2026-03-01",
        ///     "date_end=2026-03-10",
        ///     "top=200"
        ///   ]
        /// }
        ///
        /// ===============================
        /// ✅ Response JSON（示意）
        /// ===============================
        /// {
        ///   "Code": 200,
        ///   "Result": "success",
        ///   "Data": [
        ///     {
        ///       "GUID": "...",
        ///       "form_guid": "...",
        ///       "option_guid": "...",
        ///       "staff_id": "A12345",
        ///       "stage": "weekly",
        ///       "action": "FULL",
        ///       "off_date": "2026-03-05",
        ///       "before_selected_full": "false",
        ///       "after_selected_full": "true",
        ///       "created_at": "2026-02-24 17:12:33"
        ///     }
        ///   ]
        /// }
        /// </remarks>
        /// <param name="returnData">通用傳入物件（ValueAry 帶參數）</param>
        /// <returns>序列化後的 returnData JSON 字串</returns>
        [HttpPost("get_staff_dayoff_option_logs")]
        public string get_staff_dayoff_option_logs([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_staff_dayoff_option_logs";

            try
            {
                // ===============================
                // 0) 取參數
                // ===============================
                string GetVal(string key) =>
                    returnData.ValueAry?
                        .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                        ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string staff_id = GetVal("staff_id");
                string option_guid = GetVal("option_guid");
                string date_start = GetVal("date_start");
                string date_end = GetVal("date_end");
                string topStr = GetVal("top");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 form_name";
                    return returnData.JsonSerializationt();
                }

                int top = 500;
                if (!topStr.StringIsEmpty())
                {
                    int t = topStr.StringToInt32();
                    if (t > 0) top = t;
                }
                if (top > 5000) top = 5000;

                // ===============================
                // 1) SQL Controls
                // ===============================
                var sql_form = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_staff = MethodClass.GetSQLControl<StaffClass>();
                var sql_log = MethodClass.GetSQLControl<StaffDayOffOptionLogClass>();

                // ===============================
                // 2) form_name -> form_guid
                // ===============================
                object[] obj_form = sql_form.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();
                if (obj_form == null)
                {
                    returnData.Code = -404;
                    returnData.Result = $"找不到表單 form_name={form_name}";
                    return returnData.JsonSerializationt();
                }
                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();
                string form_guid = form.GUID;

                // ===============================
                // 3) staff_id -> staff_guid（若 staff_id 有填才需要）
                // ===============================
                string staff_guid = "";
                string staff_name = "";
                if (!staff_id.StringIsEmpty())
                {
                    object[] obj_staff = sql_staff.GetRowsByDefult(null, "staff_id", staff_id).FirstOrDefault();
                    if (obj_staff == null)
                    {
                        returnData.Code = -404;
                        returnData.Result = $"找不到 staff_id={staff_id}";
                        return returnData.JsonSerializationt();
                    }
                    StaffClass staff = obj_staff.SQLToClass<StaffClass>();
                    staff_guid = staff.GUID;
                    staff_name = staff.staff_name;
                }

                // ===============================
                // 4) 解析時間區間
                // ===============================
                DateTime dtStart = DateTime.MinValue;
                DateTime dtEnd = DateTime.MaxValue;

                if (!date_start.StringIsEmpty())
                {
                    // 若只給 yyyy-MM-dd，補 00:00:00
                    if (date_start.Length <= 10) date_start = date_start.Trim() + " 00:00:00";
                    dtStart = date_start.StringToDateTime();
                    if (dtStart == DateTime.MinValue)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"date_start 格式錯誤: {GetVal("date_start")}";
                        return returnData.JsonSerializationt();
                    }
                }
                if (!date_end.StringIsEmpty())
                {
                    // 若只給 yyyy-MM-dd，補 23:59:59
                    if (date_end.Length <= 10) date_end = date_end.Trim() + " 23:59:59";
                    dtEnd = date_end.StringToDateTime();
                    if (dtEnd == DateTime.MinValue)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"date_end 格式錯誤: {GetVal("date_end")}";
                        return returnData.JsonSerializationt();
                    }
                }

                // ===============================
                // 5) 取 logs（先用 form_guid 限縮，再用 LINQ 過濾）
                // ===============================
                List<object[]> obj_logs = sql_log.GetRowsByDefult(null, "form_guid", form_guid);
                List<StaffDayOffOptionLogClass> logs = obj_logs.SQLToClass<StaffDayOffOptionLogClass>() ?? new List<StaffDayOffOptionLogClass>();

                // form_guid 防呆（避免資料異常）
                logs = logs
                    .Where(x => x != null && string.Equals(x.form_guid, form_guid, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // staff 篩選（staff_id 空就不篩）
                if (!staff_guid.StringIsEmpty())
                {
                    logs = logs
                        .Where(x => string.Equals(x.staff_guid, staff_guid, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // option 篩選
                if (!option_guid.StringIsEmpty())
                {
                    logs = logs
                        .Where(x => string.Equals(x.option_guid, option_guid, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // 日期區間（用 created_at）
                logs = logs
                    .Where(x =>
                    {
                        DateTime c = x.created_at.StringToDateTime();
                        if (c == DateTime.MinValue) return false;
                        return c >= dtStart && c <= dtEnd;
                    })
                    .ToList();

                // 排序：最新在前
                logs = logs
                    .OrderByDescending(x => x.created_at.StringToDateTime())
                    .ThenByDescending(x => x.GUID) // 同秒穩定排序
                    .Take(top)
                    .ToList();

                // ===============================
                // 6) 回傳
                // ===============================
                returnData.Code = 200;
                returnData.Result = "success";
                returnData.Data = logs;
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = $"Exception : {ex.Message}";
                return returnData.JsonSerializationt();
            }
            finally
            {
                returnData.TimeTaken = timer.ToString();
            }
        }

        /// <summary>
        /// 取得指定表單(form_name)中，指定登入者(staff_id)的排休/排班資訊（只回登入者自己的 items，且所有 day 都回傳，即使該日沒有 item 也顯示）。
        /// </summary>
        /// <remarks>
        /// ===============================
        /// ✅ 功能說明
        /// ===============================
        /// 前端登入後，用此 API 取得該登入者在某張排休表單中的「個人可視資料」：
        /// - 回傳整張表單所有 DayOffScheduleDayClass（days 全部日期都回）
        /// - 每個 day.items 只包含登入者自己的 DayOffScheduleItemClass（不包含其他人）
        /// - 若該日登入者沒有 item，則 day.items 為空清單
        /// - item.option 會使用 item.option_guid 對應 staff_dayoff_option.GUID 並填入
        /// - 前端可用來顯示：
        ///   1) 已被排的 FF（option.is_force_ff=true）
        ///   2) 已選擇休假（selected_full / selected_half_am / selected_half_pm）
        ///   3) 未選擇但可建議日期（suggested_dates）
        ///   4) 被排到的班別（assigned_shift / item.shift_requirement）
        ///
        /// ===============================
        /// ✅ 參數來源（returnData.ValueAry）
        /// ===============================
        /// - form_name : 表單名稱（對應 dayoff_schedule_form.form_name）
        /// - staff_id  : 登入者工號/帳號（對應 staff.staff_id）
        ///
        /// ===============================
        /// ✅ 回傳資料結構
        /// ===============================
        /// returnData.Data 會放：
        /// - DayOffScheduleFormClass（days 為全日期，items 只含登入者）
        ///
        /// ===============================
        /// ✅ Request JSON 範例
        /// ===============================
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026-03 月排休",
        ///     "staff_id=A12345"
        ///   ]
        /// }
        /// </remarks>
        /// <param name="returnData">通用傳入物件，使用 returnData.ValueAry 帶參數</param>
        /// <returns>序列化後的 returnData JSON 字串</returns>
        [HttpPost("get_form_for_staff")]
        public string get_form_for_staff([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_form_for_staff";

            try
            {
                // ===============================
                // 0) 取參數（ValueAry）
                // ===============================
                string GetVal(string key) =>
                    returnData.ValueAry?
                        .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                        ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string staff_id = GetVal("staff_id");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 form_name";
                    returnData.TimeTaken = timer.ToString();
                    return returnData.JsonSerializationt();
                }
                if (staff_id.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 staff_id";
                    returnData.TimeTaken = timer.ToString();
                    return returnData.JsonSerializationt();
                }

                // ===============================
                // 1) SQL Controls
                // ===============================
                var sql_form = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_day = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_item = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
                var sql_option = MethodClass.GetSQLControl<StaffDayOffOptionClass>();
                var sql_staff = MethodClass.GetSQLControl<StaffClass>();

                // ===============================
                // 2) form_name → form_guid
                // ===============================
                object[] obj_form = sql_form.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();
                if (obj_form == null)
                {
                    returnData.Code = -404;
                    returnData.Result = $"找不到表單 form_name={form_name}";
                    returnData.TimeTaken = timer.ToString();
                    return returnData.JsonSerializationt();
                }
                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();
                string form_guid = form.GUID;

                // ===============================
                // 3) staff_id → staff_guid（staff_dayoff_option.staff_guid 存 staff.GUID）
                // ===============================
                object[] obj_staff = sql_staff.GetRowsByDefult(null, "staff_id", staff_id).FirstOrDefault();
                if (obj_staff == null)
                {
                    returnData.Code = -404;
                    returnData.Result = $"找不到 staff_id={staff_id}";
                    returnData.TimeTaken = timer.ToString();
                    return returnData.JsonSerializationt();
                }
                StaffClass staff = obj_staff.SQLToClass<StaffClass>();
                string staff_guid = staff.GUID;

                // ===============================
                // 4) 取該表單全部 days（全部要回傳）
                // ===============================
                List<object[]> obj_days = sql_day.GetRowsByDefult(null, "form_guid", form_guid);
                List<DayOffScheduleDayClass> days_all = obj_days.SQLToClass<DayOffScheduleDayClass>();

                days_all = days_all
                    .OrderBy(d => d.date.StringToDateTime())
                    .ToList();

                // ===============================
                // 5) 只取登入者自己的 items（dayoff_schedule_item.staff_guid）
                // ===============================
                List<object[]> obj_items = sql_item.GetRowsByDefult(null, "form_guid", form_guid);
                List<DayOffScheduleItemClass> items_all = obj_items.SQLToClass<DayOffScheduleItemClass>();

                List<DayOffScheduleItemClass> staff_items = items_all
                    .Where(i => string.Equals(i.staff_guid, staff_guid, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                // 依 day_guid 分桶（給 day 填 items 用）
                var staffItemBucketByDayGuid = staff_items
                    .GroupBy(i => i.day_guid ?? "")
                    .ToDictionary(g => g.Key, g => g.ToList());

                // ===============================
                // 6) 用 item.option_guid 對應 StaffDayOffOptionClass.GUID
                //    並填入 item.option（找不到也補空 option，避免前端 null）
                // ===============================
                List<object[]> obj_options = sql_option.GetRowsByDefult(null, "form_guid", form_guid);
                List<StaffDayOffOptionClass> options_all = obj_options.SQLToClass<StaffDayOffOptionClass>();

                List<StaffDayOffOptionClass> staff_options = options_all
                    .Where(o => string.Equals(o.staff_guid, staff_guid, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var optionDict = staff_options
                    .Where(o => !o.GUID.StringIsEmpty())
                    .GroupBy(o => o.GUID)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var item in staff_items)
                {
                    // 填入 staff（前端若要顯示姓名/工號）
                    item.staff = staff;

                    if (!item.option_guid.StringIsEmpty() && optionDict.TryGetValue(item.option_guid, out var opt))
                    {
                        opt.NormalizeSelection();
                        item.option = opt;
                    }
                    else
                    {
                        // 找不到 option（資料不一致或尚未建立），補空 option
                        item.option = new StaffDayOffOptionClass
                        {
                            GUID = item.option_guid,
                            form_guid = form_guid,
                            staff_guid = staff_guid,
                            item_guid = item.GUID,
                            date = "",
                            suggested_dates = "[]",
                            is_any_date = "false",
                            assigned_shift = "",
                            can_full = "false",
                            can_half_am = "false",
                            can_half_pm = "false",
                            is_forbidden = "false",
                            is_force_ff = "false",
                            force_ff_at = DateTime.MinValue.ToDateTimeString(),
                            selected_full = "false",
                            selected_half_am = "false",
                            selected_half_pm = "false"
                        };
                    }
                }

                // ===============================
                // 7) ✅ 回傳所有 days（就算沒有 item 也顯示）
                //    - day.items：只有登入者 items（沒有就空清單）
                // ===============================
                form.days = new List<DayOffScheduleDayClass>();

                foreach (var day in days_all)
                {
                    // 重要：避免 computed 欄位殘留
                    day.ResetComputedFields();

                    if (day.items == null) day.items = new List<DayOffScheduleItemClass>();
                    day.items.Clear();

                    // 只塞該 day 的登入者 items
                    if (!day.GUID.StringIsEmpty() && staffItemBucketByDayGuid.TryGetValue(day.GUID, out var items))
                    {
                        // items 排序：日期越早越前，再用 position
                        items = items
                            .OrderBy(i => i.date.StringToDateTime())
                            .ThenBy(i => i.position)
                            .ToList();

                        day.items = items;
                    }
                    else
                    {
                        // 沒有 item 也要回傳空清單
                        day.items = new List<DayOffScheduleItemClass>();
                    }

                    form.days.Add(day);
                }

                // days 已排序過，這裡保守再排一次
                form.days = form.days
                    .OrderBy(d => d.date.StringToDateTime())
                    .ToList();

                // ===============================
                // 8) 任選日期（Any Date）統計（依你原 class 欄位）
                //    這裡針對「該 staff」計算：is_any_date=true 的 option 筆數
                // ===============================
                int anyDateOptionCount = staff_options.Count(o => o.is_any_date == "true");
                form.any_date_option_count = anyDateOptionCount.ToString();
                form.any_date_quota_days = anyDateOptionCount.ToString();
                form.any_date_staff_count = anyDateOptionCount > 0 ? "1" : "0";
                form.any_date_used_days = "0";
                form.any_date_remaining_days = (anyDateOptionCount - 0).ToString();
                form.any_date_is_full = (anyDateOptionCount - 0) <= 0 ? "true" : "false";

                // ===============================
                // 9) 回傳
                // ===============================
                returnData.Code = 200;
                returnData.Result = "success";
                returnData.Data = form;

                returnData.TimeTaken = timer.ToString();
                return returnData.JsonSerializationt();
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = $"Exception : {ex.Message}";
                returnData.TimeTaken = timer.ToString();
                return returnData.JsonSerializationt();
            }
        }

        /// <summary>
        /// 登入者完成送出目前階段（週休/特休）：
        /// 寫入 dayoff_group_member 的完成狀態，並在整組都完成時自動推進 group 狀態與開放下一組。
        /// </summary>
        /// <remarks>
        /// ===============================
        /// ✅ 功能說明
        /// ===============================
        /// 前端「填寫者」在週休/特休畫面按下「完成送出」時呼叫：
        /// 1) 找出目前表單 active 的 open group（status=1 或 status=3）
        /// 2) 檢核登入者是否屬於 open group
        /// 3) 將 dayoff_group_member 對應欄位標記完成：
        ///    - 週休階段(stage=weekly)：is_weekoff_completed=true、weekoff_completed_at=now
        ///    - 特休階段(stage=annual)：is_annualleave_completed=true、annualleave_completed_at=now
        /// 4) 若該 open group 內「全部成員」都完成：
        ///    - weekly：group.status 1→2，並開下一組 0→1
        ///    - annual：group.status 3→4，並開下一組 2→3
        ///
        /// ===============================
        /// ✅ stage 判定規則
        /// ===============================
        /// 若未傳入 stage：
        /// - 優先找 status=1 的 group → stage=weekly
        /// - 否則找 status=3 的 group → stage=annual
        /// - 都沒有 → 表示目前沒有開放填寫
        ///
        /// 若有傳入 stage（weekly/annual）：
        /// - 會強制使用該 stage 去找對應 open group（weekly=status1, annual=status3）
        ///
        /// ===============================
        /// ✅ 參數（returnData.ValueAry）
        /// ===============================
        /// - form_name : 表單名稱（必填）
        /// - staff_id  : 登入者工號（必填）
        /// - stage     : weekly / annual（選填，不填則自動判定）
        ///
        /// ===============================
        /// ✅ Request JSON 範例
        /// ===============================
        /// (1) 自動判定目前階段
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "staff_id=A12345"
        ///   ]
        /// }
        ///
        /// (2) 指定週休完成
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "staff_id=A12345",
        ///     "stage=weekly"
        ///   ]
        /// }
        ///
        /// ===============================
        /// ✅ Response（示意）
        /// ===============================
        /// {
        ///   "Code": 200,
        ///   "Result": "success",
        ///   "Data": {
        ///     "stage": "weekly",
        ///     "open_group": { ... DayOffGroupClass ... },
        ///     "next_group": { ... DayOffGroupClass ... },
        ///     "open_group_completed": true
        ///   }
        /// }
        /// </remarks>
        /// <param name="returnData">通用傳入物件（ValueAry 帶參數）</param>
        /// <returns>序列化後的 returnData JSON 字串</returns>
        [HttpPost("complete_staff_stage")]
        public string complete_staff_stage([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "complete_staff_stage";

            try
            {
                // ===============================
                // 0) 取參數
                // ===============================
                string GetVal(string key) =>
                    returnData.ValueAry?
                        .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                        ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string staff_id = GetVal("staff_id");
                string stage = GetVal("stage"); // weekly / annual

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 form_name";
                    return returnData.JsonSerializationt();
                }
                if (staff_id.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 staff_id";
                    return returnData.JsonSerializationt();
                }

                stage = (stage ?? "").Trim().ToLower();

                // ===============================
                // 1) SQL Controls
                // ===============================
                var sql_form = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_staff = MethodClass.GetSQLControl<StaffClass>();
                var sql_group = MethodClass.GetSQLControl<DayOffGroupClass>();
                var sql_member = MethodClass.GetSQLControl<DayOffGroupMemberClass>();

                // ===============================
                // 2) form
                // ===============================
                object[] obj_form = sql_form.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();
                if (obj_form == null)
                {
                    returnData.Code = -404;
                    returnData.Result = $"找不到表單 form_name={form_name}";
                    return returnData.JsonSerializationt();
                }
                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();
                string form_guid = form.GUID;

                // ===============================
                // 3) staff_id -> staff_guid
                // ===============================
                object[] obj_staff = sql_staff.GetRowsByDefult(null, "staff_id", staff_id).FirstOrDefault();
                if (obj_staff == null)
                {
                    returnData.Code = -404;
                    returnData.Result = $"找不到 staff_id={staff_id}";
                    return returnData.JsonSerializationt();
                }
                StaffClass staff = obj_staff.SQLToClass<StaffClass>();
                string staff_guid = staff.GUID;

                // ===============================
                // 4) 取 groups / members
                // ===============================
                List<DayOffGroupClass> groups =
                    sql_group.GetRowsByDefult(null, "form_guid", form_guid)
                    .SQLToClass<DayOffGroupClass>() ?? new List<DayOffGroupClass>();

                List<DayOffGroupMemberClass> members =
                    sql_member.GetRowsByDefult(null, "form_guid", form_guid)
                    .SQLToClass<DayOffGroupMemberClass>() ?? new List<DayOffGroupMemberClass>();

                // ===============================
                // 5) 找 open group + 判定 stage
                // ===============================
                DayOffGroupClass openWeekly = groups
                    .Where(g => g.status == "1")
                    .OrderBy(g => g.order_index.StringToInt32())
                    .FirstOrDefault();

                DayOffGroupClass openAnnual = groups
                    .Where(g => g.status == "3")
                    .OrderBy(g => g.order_index.StringToInt32())
                    .FirstOrDefault();

                DayOffGroupClass openGroup = null;

                if (stage == "weekly")
                {
                    openGroup = openWeekly;
                }
                else if (stage == "annual")
                {
                    openGroup = openAnnual;
                }
                else
                {
                    // 自動判定
                    if (openWeekly != null)
                    {
                        stage = "weekly";
                        openGroup = openWeekly;
                    }
                    else if (openAnnual != null)
                    {
                        stage = "annual";
                        openGroup = openAnnual;
                    }
                    else
                    {
                        stage = "none";
                    }
                }

                if (openGroup == null)
                {
                    returnData.Code = -403;
                    returnData.Result = "目前未開放任何組別填寫";
                    return returnData.JsonSerializationt();
                }

                // ===============================
                // 6) 找 staff member，必須在 open group
                // ===============================
                DayOffGroupMemberClass staffMember = members
                    .FirstOrDefault(m =>
                        string.Equals(m.staff_guid, staff_guid, StringComparison.OrdinalIgnoreCase));

                if (staffMember == null)
                {
                    returnData.Code = -404;
                    returnData.Result = "找不到此 staff 在該表單的 group_member";
                    return returnData.JsonSerializationt();
                }

                if (!string.Equals(staffMember.group_guid, openGroup.GUID, StringComparison.OrdinalIgnoreCase))
                {
                    returnData.Code = -403;
                    returnData.Result = "尚未輪到你的組別，禁止完成送出";
                    return returnData.JsonSerializationt();
                }

                // ===============================
                // 7) 寫入 member 完成欄位
                // ===============================
                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                if (stage == "weekly")
                {
                    staffMember.is_weekoff_completed = "true";
                    if (staffMember.weekoff_completed_at.StringIsEmpty())
                        staffMember.weekoff_completed_at = now;
                }
                else if (stage == "annual")
                {
                    staffMember.is_annualleave_completed = "true";
                    if (staffMember.annualleave_completed_at.StringIsEmpty())
                        staffMember.annualleave_completed_at = now;
                }
                else
                {
                    returnData.Code = -200;
                    returnData.Result = "stage 不正確（weekly/annual）";
                    return returnData.JsonSerializationt();
                }

                staffMember.updated_at = now;

                // 更新 member（注意你專案 UpdateByDefulteExtra 的參數型態）
                // ✅ 通常是：UpdateByDefulteExtra(null, List<object[]> rows)
                sql_member.UpdateByDefulteExtra(null, new List<object[]> { staffMember.ClassToSQL<DayOffGroupMemberClass>() });

                // ===============================
                // 8) 若整組都完成 → 推進 group 狀態、開下一組
                // ===============================
                bool openGroupCompleted = false;
                DayOffGroupClass nextGroup = null;

                List<DayOffGroupMemberClass> openGroupMembers = members
                    .Where(m => string.Equals(m.group_guid, openGroup.GUID, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (stage == "weekly")
                {
                    openGroupCompleted = openGroupMembers.All(m => m.is_weekoff_completed == "true");
                    if (openGroupCompleted)
                    {
                        // openGroup: 1 -> 2
                        if (openGroup.status == "1")
                        {
                            openGroup.SetStatusWithTime("2");
                            sql_group.UpdateByDefulteExtra(null, new List<object[]> { openGroup.ClassToSQL<DayOffGroupClass>() });
                        }

                        // 開下一組：找 order_index > current 的最小組
                        int cur = openGroup.order_index.StringToInt32();
                        nextGroup = groups
                            .Where(g => g.order_index.StringToInt32() > cur)
                            .OrderBy(g => g.order_index.StringToInt32())
                            .FirstOrDefault();

                        if (nextGroup != null)
                        {
                            // 週休階段：通常 nextGroup.status 應該是 0（未輪到）
                            if (nextGroup.status == "0")
                            {
                                nextGroup.SetStatusWithTime("1");
                                sql_group.UpdateByDefulteExtra(null, new List<object[]> { nextGroup.ClassToSQL<DayOffGroupClass>() });
                            }
                        }
                    }
                }
                else // annual
                {
                    openGroupCompleted = openGroupMembers.All(m => m.is_annualleave_completed == "true");
                    if (openGroupCompleted)
                    {
                        // openGroup: 3 -> 4
                        if (openGroup.status == "3")
                        {
                            openGroup.SetStatusWithTime("4");
                            sql_group.UpdateByDefulteExtra(null, new List<object[]> { openGroup.ClassToSQL<DayOffGroupClass>() });
                        }

                        // 開下一組：找 order_index > current 的最小組
                        int cur = openGroup.order_index.StringToInt32();
                        nextGroup = groups
                            .Where(g => g.order_index.StringToInt32() > cur)
                            .OrderBy(g => g.order_index.StringToInt32())
                            .FirstOrDefault();

                        if (nextGroup != null)
                        {
                            // 特休階段：通常 nextGroup.status 應該是 2（週休完成待特休）
                            if (nextGroup.status == "2")
                            {
                                nextGroup.SetStatusWithTime("3");
                                sql_group.UpdateByDefulteExtra(null, new List<object[]> { nextGroup.ClassToSQL<DayOffGroupClass>() });
                            }
                        }
                    }
                }

                // ===============================
                // 9) 回傳（用 DayOffGroupClass 呈現）
                // ===============================
                returnData.Code = 200;
                returnData.Result = "success";
                returnData.Data = new
                {
                    stage = stage,
                    open_group = openGroup,
                    next_group = nextGroup,
                    open_group_completed = openGroupCompleted
                };

                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = $"Exception : {ex.Message}";
                return returnData.JsonSerializationt();
            }
            finally
            {
                returnData.TimeTaken = timer.ToString();
            }
        }


        /// <summary>
        /// 將時間字串正規化：空值/MinValue/非法字串 → MinValue 字串；合法字串 → 原樣回傳
        /// </summary>
        private string NormalizeDateTimeOrMin(string dt)
        {
            string min = DateTime.MinValue.ToDateTimeString();
            if (dt.StringIsEmpty()) return min;
            if (dt == min) return min;

            DateTime tmp;
            if (!DateTime.TryParse(dt, out tmp)) return min;

            return dt;
        }


        // =========================
        // ✅ Helper：依 option 計算 day 的登入者視角統計
        // （注意：這裡是「單一 staff」計數，不是全體統計）
        // =========================
        private static void ApplyDayCountersFromOption(DayOffScheduleDayClass day, StaffDayOffOptionClass opt)
        {
            if (day == null || opt == null) return;

            // FF 視為 FULL（且不可改）
            if (opt.is_force_ff == "true")
            {
                day.ff_dayoff_count = (day.ff_dayoff_count.StringToInt32() + 1).ToString();
                return;
            }

            if (opt.selected_full == "true")
            {
                day.ff_dayoff_count = (day.ff_dayoff_count.StringToInt32() + 1).ToString();
                return;
            }

            if (opt.selected_half_am == "true")
            {
                day.am_dayoff_count = (day.am_dayoff_count.StringToInt32() + 1).ToString();
                return;
            }

            if (opt.selected_half_pm == "true")
            {
                day.pm_dayoff_count = (day.pm_dayoff_count.StringToInt32() + 1).ToString();
                return;
            }
        }

        // =========================
        // ✅ Helper：任選日期統計（依你 class 註解）
        // =========================
        private static void ApplyAnyDateSummary(DayOffScheduleFormClass form, List<StaffDayOffOptionClass> staff_options)
        {
            if (form == null) return;
            if (staff_options == null) staff_options = new List<StaffDayOffOptionClass>();

            var anyOptions = staff_options.Where(o => o.is_any_date == "true").ToList();

            int quota = anyOptions.Count; // 依你註解：不去重
            int staffCount = quota > 0 ? 1 : 0; // 這支 API 只回單一 staff
            int used = anyOptions.Count(o => !string.IsNullOrWhiteSpace(o.date)); // 先用「有填 date」當作已使用

            int remaining = quota - used;
            bool isFull = remaining <= 0;

            form.any_date_quota_days = quota.ToString();
            form.any_date_option_count = quota.ToString();
            form.any_date_staff_count = staffCount.ToString();
            form.any_date_used_days = used.ToString();
            form.any_date_remaining_days = remaining.ToString();
            form.any_date_is_full = isFull ? "true" : "false";
        }

        // =========================================================
        // Helper：取得 active form（保守版）
        // 規則：dayoff_group 內存在 status=1 或 status=3 的 form_guid 視為 active
        // =========================================================
        private DayOffScheduleFormClass TryGetActiveForm(SQLControl sql_form, SQLControl sql_group)
        {
            try
            {
                List<object[]> obj_groups_all = sql_group.GetAllRows(null);
                var groups_all = obj_groups_all.SQLToClass<DayOffGroupClass>() ?? new List<DayOffGroupClass>();

                string activeFormGuid = groups_all
                    .Where(g => g.status == "1" || g.status == "3")
                    .OrderByDescending(g => g.status_changed_at.StringToDateTime())
                    .Select(g => g.form_guid)
                    .FirstOrDefault();

                if (activeFormGuid.StringIsEmpty()) return null;

                object[] obj_form = sql_form.GetRowsByDefult(null, "GUID", activeFormGuid).FirstOrDefault();
                return obj_form?.SQLToClass<DayOffScheduleFormClass>();
            }
            catch
            {
                return null;
            }
        }

        // =========================================================
        // Helper：把「非SQL欄位」塞回 DayOffGroupClass
        // ⚠️ 你需要先把這些欄位加到 DayOffGroupClass（非SQL欄位）
        // =========================================================
        private void SetExtraFields(
            DayOffGroupClass g,
            string stage,
            string stageName,
            string openGuid,
            string openOrder,
            bool isOpenGroup,
            bool canWrite,
            int remainGroups,
            string message,
            string progress)
        {
            g.stage = stage;
            g.stage_name = stageName;

            g.open_group_guid = openGuid;
            g.open_group_order_index = openOrder;

            g.is_open_group = isOpenGroup ? "true" : "false";
            g.can_write = canWrite ? "true" : "false";
            g.remain_groups_to_open = remainGroups.ToString();

            g.message = message;
            g.progress_message = progress;
        }

        private string BuildMessage(string stageName, bool canWrite, int remain, string stage)
        {
            if (stage == "none") return "目前未開放填寫";
            if (canWrite) return $"目前開放{stageName}填寫：輪到你";
            return $"目前開放{stageName}填寫：尚未輪到你（還差 {remain} 組）";
        }

        private string BuildProgressMessage(bool canWrite, int remain, string stage)
        {
            if (stage == "none") return "尚未開放";
            if (canWrite) return "輪到你";
            if (remain > 0) return $"還差{remain}組";
            return "未輪到";
        }


        /// <summary>
        /// 取得下一組（依 order_index 排序後）
        /// </summary>
        private DayOffGroupClass GetNextGroup(List<DayOffGroupClass> groups, DayOffGroupClass current)
        {
            int idx = groups.FindIndex(g => g.GUID == current.GUID);
            if (idx < 0) return null;
            if (idx + 1 >= groups.Count) return null;
            return groups[idx + 1];
        }

        // =========================================================
        // ✅ 建立 FF option（date + suggested_dates 都是週日當天）
        // =========================================================
        private StaffDayOffOptionClass BuildForceFFOption(DayOffScheduleItemClass item, string dt)
        {
            var opt = new StaffDayOffOptionClass();
            opt.GUID = Guid.NewGuid().ToString();
            opt.form_guid = item.form_guid;
            opt.item_guid = item.GUID;
            opt.staff_guid = item.staff_guid;

            opt.date = dt;

            opt.suggested_dates_list = new List<string>() { dt };
            if (dt.StringToDateTime().DayOfWeek == DayOfWeek.Sunday)
            {
                item.shift_requirement = BuildHolidayOffShiftRequirementJson(dt.StringToDateTime());
                item.selected_dayoff_type = "FF"; // 若你前端有使用，可留；不需要也可空字串
                                                  // FF 一律整天假        
                opt.can_full = "true";
                opt.can_half_am = "false";
                opt.can_half_pm = "false";
                // ✅ 強制選擇整天假
                opt.selected_full = "true";
                opt.selected_half_am = "false";
                opt.selected_half_pm = "false";
                opt.is_any_date = "false";


                // ✅ 你新增的 FF 欄位（請確保 class 已加上）
                opt.is_force_ff = "true";
            }
            if (dt.StringToDateTime().DayOfWeek == DayOfWeek.Saturday)
            {
                item.shift_requirement = BuildHolidayOffShiftRequirementJson(dt.StringToDateTime());
                item.selected_dayoff_type = "FF"; // 若你前端有使用，可留；不需要也可空字串
                                                  // FF 一律整天假        
                opt.can_full = "false";
                opt.can_half_am = "true";
                opt.can_half_pm = "true";
                // ✅ 強制選擇整天假
                opt.selected_full = "false";
                opt.selected_half_am = "false";
                opt.selected_half_pm = "false";
                opt.is_any_date = "true";

                opt.is_force_ff = "false";
            }

            opt.is_forbidden = "false";
            opt.assigned_shift = "OFF";
            opt.force_ff_at = DateTime.Now.ToDateTimeString_6();

         

            opt.NormalizeSelection();
            return opt;
        }
        private string BuildHolidayOffShiftRequirementJson(DateTime dt)
        {
            string dayCode = dt.DayOfWeek == DayOfWeek.Saturday ? "Saturday" :
                             dt.DayOfWeek == DayOfWeek.Sunday ? "Sunday" : "";

            var req = new WorkShiftRequirementClass()
            {
                day = dayCode,
                time = "",
                shift_type = "",
                required_count = "0",
                assigned_count = "0",
                department = "",
                hdr = "",
                disabled = true
            };
            return JsonSerializer.Serialize(req);
        }


        private StaffDayOffOptionClass BuildStaffDayOffSpecialDayOption(DayOffScheduleItemClass item, Dictionary<string, List<DayOffScheduleItemClass>> itemIndex)
        {
            if (item == null) return null;
            if (item.is_special_day != "true") return null;

            // ===== 觸發條件：結束時間符合 =====
            string staffGuid = item.staff_guid;
            string endStr = item.workShiftRequirement?.TimeRange.Value.end.ToString() ?? "";
            if(endStr.Contains("08:00"))return null;
            // item日期
            DateTime itemDate = item.date.StringToDateTime();

            DateTime dateTimeSuggestedDate = new DateTime();
            StaffDayOffOptionClass option = new StaffDayOffOptionClass();
            option.GUID = Guid.NewGuid().ToString();
            option.form_guid = item.form_guid;
            option.item_guid = item.GUID;
            option.staff_guid = staffGuid;
            option.date = item.date;

            option.can_full = "false";
            option.can_half_pm = "false";
            option.can_half_am = "false";
            if (endStr.Contains("12:00"))
            {
                option.can_half_pm = "true";
                option.can_half_am = "true";
            }
            else
            {
                option.can_full = "true";
            }
            option.assigned_shift = ShiftTypeEnum.none.GetEnumName();
            option.is_any_date = "true";
            option.NormalizeSelection();
            return option;

            return null;
        }
        private StaffDayOffOptionClass BuildStaffDayOffSwingOption(DayOffScheduleItemClass item, Dictionary<string, List<DayOffScheduleItemClass>> itemIndex)
        {
            if (item == null) return null;

            // ===== 觸發條件：結束時間符合 =====
            string staffGuid = item.staff_guid;
            string endStr = item.workShiftRequirement?.TimeRange.Value.end.ToString() ?? "";
            bool isLateEnd = endStr.Contains("23:00:00") || endStr.Contains("23:59");
            if (!isLateEnd) return null;

            // item日期
            DateTime itemDate = item.date.StringToDateTime();

            // =========================================================
            // ✅ 規則A：當日為星期六
            // =========================================================
            if (itemDate.DayOfWeek == DayOfWeek.Saturday || itemDate.DayOfWeek == DayOfWeek.Sunday)
            {
                DateTime dateTimeSuggestedDate = new DateTime();
                if (itemDate.DayOfWeek == DayOfWeek.Saturday) dateTimeSuggestedDate = itemDate.AddDays(7);
                if (itemDate.DayOfWeek == DayOfWeek.Sunday) dateTimeSuggestedDate = itemDate.AddDays(4);
            
                StaffDayOffOptionClass option = new StaffDayOffOptionClass();
                option.GUID = Guid.NewGuid().ToString();
                option.form_guid = item.form_guid;
                option.item_guid = item.GUID;
                option.staff_guid = staffGuid;
                option.date = item.date;
                option.can_full = "true";
                option.can_half_pm = "false";
                option.can_half_am = "false";
       
                option.assigned_shift = ShiftTypeEnum.swing.GetEnumName();
                // 超出月份 → 任選日期
                if (dateTimeSuggestedDate.Month != itemDate.Month)
                {
                    option.is_any_date = "true";
                    if (itemDate.DayOfWeek == DayOfWeek.Saturday)
                    {
                        option.can_full = "false";
                        option.can_half_pm = "false";
                        option.can_half_am = "true";
                    }
                      
                }
                else
                {
                    if (HasWorkShift(itemIndex, item.staff_guid, dateTimeSuggestedDate))
                    {
                        return null;
                    }
                    if (dateTimeSuggestedDate.DayOfWeek == DayOfWeek.Saturday)
                    {
                        option.can_full = "false";
                        option.can_half_pm = "false";
                        option.can_half_am = "true";
                    }
                    option.suggested_dates = (new List<string>() { dateTimeSuggestedDate.ToDateString('-') }).JsonSerializationt();
                }

                option.NormalizeSelection();
                return option;
            }
        
            // =========================================================
            // ✅ 規則B：前一天小夜班，且後一天沒有上班
            // =========================================================
            DateTime prevDate = itemDate.AddDays(-1);
            string prevKey = $"{staffGuid}|{prevDate.ToDateString('-')}";

            bool hasPrevEvening = false;
            if (itemIndex.TryGetValue(prevKey, out var prevItems) && prevItems != null && prevItems.Count > 0)
            {
                hasPrevEvening = prevItems.Any(x =>
                {
                    string time = x.workShiftRequirement?.time ?? "";
                    if (time.StringIsEmpty()) return false;

                    return time == "12:00-20:00"
                        || time == "12:30-21:00"
                        || time == "13:30-22:00"
                        || time == "14:30-23:00"
                        || time == "15:30-23:59"
                        || time == "16:00-23:59";
                });
            }

            DateTime nextDate = itemDate.AddDays(+1);
            string nextKey = $"{staffGuid}|{nextDate.ToDateString('-')}";

            bool hasNextWork = itemIndex.TryGetValue(nextKey, out var nextItems)
                               && nextItems != null
                               && nextItems.Count > 0;

            if (hasPrevEvening && !hasNextWork)
            {
                DateTime dateTimeSuggestedDate = itemDate.AddDays(1);
                if (HasWorkShift(itemIndex, item.staff_guid, dateTimeSuggestedDate))
                {
                    return null;
                }
                StaffDayOffOptionClass option = new StaffDayOffOptionClass();
                option.GUID = Guid.NewGuid().ToString();
                option.form_guid = item.form_guid;
                option.item_guid = item.GUID;
                option.staff_guid = staffGuid;
                option.date = item.date;
                option.suggested_dates = (new List<string>() { dateTimeSuggestedDate.ToDateString('-') }).JsonSerializationt();
                option.can_full = "true";
                option.can_half_pm = "false";
                option.can_half_am = "false";
                option.assigned_shift = ShiftTypeEnum.swing.GetEnumName();
                if (dateTimeSuggestedDate.DayOfWeek == DayOfWeek.Saturday)
                {
                    option.can_full = "false";
                    option.can_half_pm = "false";
                    option.can_half_am = "true";
                }
                option.NormalizeSelection();

                return option;
            }

            return null;
        }
        private StaffDayOffOptionClass BuildStaffDayOffHolidayOption(DayOffScheduleItemClass item, Dictionary<string, List<DayOffScheduleItemClass>> itemIndex)
        {
            if (item == null) return null;

            // ===== 觸發條件：結束時間符合 =====
            string staffGuid = item.staff_guid;
            string endStr = item.workShiftRequirement?.TimeRange.Value.end.ToString() ?? "";
         

            // item日期
            DateTime itemDate = item.date.StringToDateTime();

            // =========================================================
            // ✅ 規則A：當日為星期六
            // =========================================================
            if (itemDate.DayOfWeek == DayOfWeek.Saturday && endStr.Contains("16:00"))
            {
    
                DateTime dateTimeSuggestedDate = new DateTime();
                dateTimeSuggestedDate = itemDate.AddDays(7);
             
                StaffDayOffOptionClass option = new StaffDayOffOptionClass();
                option.GUID = Guid.NewGuid().ToString();
                option.form_guid = item.form_guid;
                option.item_guid = item.GUID;
                option.staff_guid = staffGuid;
                option.date = item.date;
                option.can_full = "true";
                option.can_half_pm = "false";
                option.can_half_am = "false";
                  
                option.assigned_shift = ShiftTypeEnum.holiday.GetEnumName();
                // 超出月份 → 任選日期
                if (dateTimeSuggestedDate.Month != itemDate.Month)
                {
                 
                    option.is_any_date = "true";
                    option.can_full = "false";
                    option.can_half_pm = "false";
                    option.can_half_am = "true";
          
                    option.suggested_dates = (new List<string>() { dateTimeSuggestedDate.ToDateString('-') }).JsonSerializationt();
                }
                else
                {
                    if (HasWorkShift(itemIndex, item.staff_guid, dateTimeSuggestedDate))
                    {
                        return null;
                    }
                    if (dateTimeSuggestedDate.DayOfWeek == DayOfWeek.Saturday)
                    {
                        option.can_full = "false";
                        option.can_half_pm = "false";
                        option.can_half_am = "true";
                    }
                    option.suggested_dates = (new List<string>() { dateTimeSuggestedDate.ToDateString('-') }).JsonSerializationt();
                }

                option.NormalizeSelection();
                return option;
            }
            // =========================================================
            // ✅ 規則B：當日為星期日化療
            // =========================================================
            if (itemDate.DayOfWeek == DayOfWeek.Sunday && endStr.Contains("12:00"))
            {
                DateTime dateTimeSuggestedDate = new DateTime();
                dateTimeSuggestedDate = itemDate.AddDays(6);
     
                StaffDayOffOptionClass option = new StaffDayOffOptionClass();
                option.GUID = Guid.NewGuid().ToString();
                option.form_guid = item.form_guid;
                option.item_guid = item.GUID;
                option.staff_guid = staffGuid;
                option.date = item.date;

                option.can_full = "true";
                option.can_half_pm = "false";
                option.can_half_am = "false";
                option.assigned_shift = ShiftTypeEnum.holiday.GetEnumName();
                if (dateTimeSuggestedDate.Month != itemDate.Month)
                {
                    dateTimeSuggestedDate = itemDate.AddDays(-1);
                    if (HasWorkShift(itemIndex, item.staff_guid, dateTimeSuggestedDate))
                    {
                        return null;
                    }
                    if (dateTimeSuggestedDate.DayOfWeek == DayOfWeek.Saturday)
                    {
                        option.can_full = "false";
                        option.can_half_pm = "false";
                        option.can_half_am = "true";
                    }
                    option.suggested_dates = (new List<string>() { dateTimeSuggestedDate.ToDateString('-') }).JsonSerializationt();
                }
                else
                {
                    if (HasWorkShift(itemIndex, item.staff_guid, dateTimeSuggestedDate))
                    {
                        return null;
                    }
                    if (dateTimeSuggestedDate.DayOfWeek == DayOfWeek.Saturday)
                    {
                        option.can_full = "false";
                        option.can_half_pm = "false";
                        option.can_half_am = "true";
                    }
                    option.suggested_dates = (new List<string>() { dateTimeSuggestedDate.ToDateString('-') }).JsonSerializationt();
                }

                option.NormalizeSelection();
                return option;
            }
            // =========================================================
            // ✅ 規則C：當日為星期日一般班
            // =========================================================
            if (itemDate.DayOfWeek == DayOfWeek.Sunday && endStr.Contains("16:00"))
            {
                if(item.workShiftRequirement.department == "急診")
                {
                    StaffDayOffOptionClass option = new StaffDayOffOptionClass();
                    DateTime dateTimeSuggestedDate = DateTime.MinValue;

                    option.GUID = Guid.NewGuid().ToString();
                    option.form_guid = item.form_guid;
                    option.item_guid = item.GUID;
                    option.staff_guid = staffGuid;
                    option.date = item.date;
                    option.can_full = "true";
                    option.can_half_pm = "false";
                    option.can_half_am = "false";
                    option.assigned_shift = ShiftTypeEnum.holiday.GetEnumName();
                    if (item.position == "0")
                    {
                        option.is_any_date = "true";
                    }
                    if (item.position == "1")
                    {
                        dateTimeSuggestedDate = itemDate.AddDays(1);
                        if (HasWorkShift(itemIndex, item.staff_guid, dateTimeSuggestedDate))
                        {
                            return null;
                        }
                        if (dateTimeSuggestedDate.DayOfWeek == DayOfWeek.Saturday)
                        {
                            option.can_full = "false";
                            option.can_half_pm = "false";
                            option.can_half_am = "true";
                        }
                        option.suggested_dates = (new List<string>() { dateTimeSuggestedDate.ToDateString('-') }).JsonSerializationt();

                    }

                    option.NormalizeSelection();
                    return option;

                }
                if (item.workShiftRequirement.department == "門診")
                {
                    StaffDayOffOptionClass option = new StaffDayOffOptionClass();
                    DateTime dateTimeSuggestedDate = DateTime.MinValue;
                    if(item.position.StringIsInt32() == false)
                    {
                        return null;
                    }
                
                    option.GUID = Guid.NewGuid().ToString();
                    option.form_guid = item.form_guid;
                    option.item_guid = item.GUID;
                    option.staff_guid = staffGuid;
                    option.date = item.date;
                    option.can_full = "true";
                    option.can_half_pm = "false";
                    option.can_half_am = "false";
                    option.assigned_shift = ShiftTypeEnum.holiday.GetEnumName();

                    dateTimeSuggestedDate = itemDate.AddDays(2 + item.position.StringToInt32());

                    if (dateTimeSuggestedDate.Month != itemDate.Month)
                    {
                        option.is_any_date = "true";
                    }
                    else
                    {
                        if (HasWorkShift(itemIndex, item.staff_guid, dateTimeSuggestedDate))
                        {
                            return null;
                        }
                        if (dateTimeSuggestedDate.DayOfWeek == DayOfWeek.Saturday)
                        {
                            option.can_full = "false";
                            option.can_half_pm = "false";
                            option.can_half_am = "true";
                        }
                        option.suggested_dates = (new List<string>() { dateTimeSuggestedDate.ToDateString('-') }).JsonSerializationt();
                    }
                    option.NormalizeSelection();
                    return option;

           

                }
            }
            return null;
        }
        private StaffDayOffOptionClass BuildStaffDayOffMidnightOption(DayOffScheduleItemClass item, Dictionary<string, List<DayOffScheduleItemClass>> itemIndex)
        {
            if (item == null) return null;

            // ===== 觸發條件：結束時間符合 =====
            string staffGuid = item.staff_guid;
            string endStr = item.workShiftRequirement?.TimeRange.Value.end.ToString() ?? "";
            bool isLateEnd = endStr.Contains("08:00");
            if (!isLateEnd) return null;

            // item日期
            DateTime itemDate = item.date.StringToDateTime();


            if (itemDate.DayOfWeek == DayOfWeek.Sunday)
            {
                DateTime dateTimeSuggestedDate = new DateTime();
                dateTimeSuggestedDate = itemDate.AddDays(5);

                StaffDayOffOptionClass option = new StaffDayOffOptionClass();
                option.GUID = Guid.NewGuid().ToString();
                option.form_guid = item.form_guid;
                option.item_guid = item.GUID;
                option.staff_guid = staffGuid;
                option.date = item.date;

                option.can_full = "true";
                option.can_half_pm = "false";
                option.can_half_am = "false";
                option.selected_full = "false";
                option.selected_half_am = "false";
                option.selected_half_pm = "false";
                option.assigned_shift = ShiftTypeEnum.midnight.GetEnumName();
                // 超出月份 → 任選日期
                if (dateTimeSuggestedDate.Month != itemDate.Month)
                {
                    dateTimeSuggestedDate = GetFirstSaturdayOfMonth(itemDate);
                    if (HasWorkShift(itemIndex, item.staff_guid, dateTimeSuggestedDate))
                    {
                        return null;
                    }
                    if (dateTimeSuggestedDate.DayOfWeek == DayOfWeek.Saturday)
                    {
                        option.can_full = "false";
                        option.can_half_pm = "false";
                        option.can_half_am = "true";
                    }
                    option.suggested_dates = (new List<string>() { dateTimeSuggestedDate.ToDateString('-') }).JsonSerializationt();
                }
                else
                {
                    if (HasWorkShift(itemIndex, item.staff_guid, dateTimeSuggestedDate))
                    {
                        return null;
                    }
                    option.suggested_dates = (new List<string>() { dateTimeSuggestedDate.ToDateString('-') }).JsonSerializationt();
                }

                option.NormalizeSelection();
                return option;
            }
            else
            {
                DateTime dateTimeSuggestedDate = itemDate.AddDays(-1);
                if (HasWorkShift(itemIndex, item.staff_guid, dateTimeSuggestedDate))
                {
                    return null;
                }
                StaffDayOffOptionClass option = new StaffDayOffOptionClass();
                option.GUID = Guid.NewGuid().ToString();
                option.form_guid = item.form_guid;
                option.item_guid = item.GUID;
                option.staff_guid = staffGuid;
                option.date = item.date;
                option.can_full = "true";
                option.can_half_pm = "false";
                option.can_half_am = "false";
                option.selected_full = "false";
                option.selected_half_am = "false";
                option.selected_half_pm = "false";
                option.assigned_shift = ShiftTypeEnum.midnight.GetEnumName();
                // 超出月份 → 任選日期
                if (dateTimeSuggestedDate.Month != itemDate.Month)
                {
                    dateTimeSuggestedDate = GetFirstSaturdayOfMonth(itemDate);
                    if (HasWorkShift(itemIndex, item.staff_guid, dateTimeSuggestedDate))
                    {
                        return null;
                    }
                    if (dateTimeSuggestedDate.DayOfWeek == DayOfWeek.Saturday)
                    {
                        option.can_full = "false";
                        option.can_half_pm = "false";
                        option.can_half_am = "true";
                    }
                    option.suggested_dates = (new List<string>() { dateTimeSuggestedDate.ToDateString('-') }).JsonSerializationt();
                }
                else
                {
                    if (HasWorkShift(itemIndex, item.staff_guid, dateTimeSuggestedDate))
                    {
                        return null;
                    }
                    option.suggested_dates = (new List<string>() { dateTimeSuggestedDate.ToDateString('-') }).JsonSerializationt();
                }
                option.NormalizeSelection();
                return option;
            }
          


            return null;
        }
        /// <summary>
        /// 檢查指定日期，該人員是否有排到任何班
        /// </summary>
        /// <param name="itemIndex">資料索引：key = staffGuid|yyyy-MM-dd</param>
        /// <param name="staffGuid">人員 GUID</param>
        /// <param name="date">要檢查日期</param>
        /// <returns>有排班回傳 true</returns>
        public static bool HasWorkShift(Dictionary<string, List<DayOffScheduleItemClass>> itemIndex, string staffGuid, DateTime date)
        {
            if (itemIndex == null) return false;
            if (staffGuid.StringIsEmpty()) return false;

            string key = $"{staffGuid}|{date.ToDateString('-')}";

            if (!itemIndex.TryGetValue(key, out var items)) return false;
            if (items == null || items.Count == 0) return false;

            // 有 workShiftRequirement.time 就視為有班
            return items.Any(x => !(x.workShiftRequirement?.time ?? "").StringIsEmpty());
        }

        public static DateTime GetFirstSaturdayOfMonth(DateTime date)
        {
            // 當月 1 號
            DateTime firstDay = new DateTime(date.Year, date.Month, 1);

            // 計算要往後加幾天，才會到星期六
            int offset = ((int)DayOfWeek.Saturday - (int)firstDay.DayOfWeek + 7) % 7;

            return firstDay.AddDays(offset);
        }

    }
}
