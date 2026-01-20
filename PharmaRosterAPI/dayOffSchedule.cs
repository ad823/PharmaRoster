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
                                                    .Where(x => x.staff_guid == item.staff_guid && x.date.StringToDateTime().ToDateString("-") == item.date.StringToDateTime().ToDateString("-"))
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
        /// 自動計算可休日期（calculate_available_dayoff_dates）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL  
        /// `POST /phar_roster_api/DayOffSchedule/calculate_available_dayoff_dates`
        ///
        /// ## 📘 功能說明  
        /// 根據指定的排休表單 (<c>form_name</c>)，分析每位人員的班表，  
        /// 自動生成「可放假日期建議」(`StaffDayOffOptionClass`)，  
        /// 並更新對應的排休項目（`DayOffScheduleItemClass`）。
        ///
        /// ✅ **主要功能**  
        /// - 依據排班結果（早班、小夜、大夜、假日班等）計算補休日。  
        /// - 若該班別需補休，會建立對應的 <see cref="StaffDayOffOptionClass"/>。  
        /// - 若該人員可任選一天休假，`is_any_date` 會設為 `"true"`。  
        /// - 若系統有預測合適休假日，會填入 `suggested_dates` JSON 陣列。
        ///
        /// ## ⚙️ 執行流程  
        /// 1. 驗證表單名稱 (<c>form_name</c>) 是否存在。  
        /// 2. 取得該表單的所有日期與排班項目。  
        /// 3. 建立 item → staff → date 的索引結構（快速比對班別）。  
        /// 4. 呼叫規則建構函式（`BuildStaffDayOffSpecialDayOption`, `BuildStaffDayOffSwingOption` 等）生成補休日。  
        /// 5. 若該 staff+date 組合尚未存在 option，則建立新紀錄並更新 item.option_guid。  
        /// 6. 寫入 `staff_dayoff_option` 資料表，同時更新 `dayoff_schedule_item`。  
        ///
        /// ## 📥 Request JSON 範例  
        /// ```json
        /// {
        ///   "Method": "calculate_available_dayoff_dates",
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
        /// | form_name | string | ✅ | 一月排休表 | 要計算可休日期的排休表名稱 |
        /// | simple | bool | ❌ | false | 若為 true，僅載入主表結構不進行運算 |
        ///
        /// ## 🧩 回傳資料階層  
        /// ```
        /// DayOffScheduleFormClass
        /// ├─ DayOffScheduleDayClass[]
        /// │  ├─ DayOffScheduleItemClass[]
        /// │  │   ├─ WorkShiftRequirementClass
        /// │  │   └─ StaffDayOffOptionClass
        /// ```
        ///
        /// ## 📤 成功回傳範例  
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Result": "新增排休資料成功,共3筆",
        ///   "Data": {
        ///     "GUID": "6892c33b-d8ba-488f-95c5-e4e2aafe1016",
        ///     "form_name": "一月排休表",
        ///     "days": [
        ///       {
        ///         "date": "2026-01-12",
        ///         "items": [
        ///           {
        ///             "staff_id": "850233",
        ///             "staff_name": "郭佳瓚",
        ///             "workShiftRequirement": {
        ///               "day": "Monday",
        ///               "time": "16:00-23:59",
        ///               "shift_type": "swing",
        ///               "department": "急診"
        ///             },
        ///             "option": {
        ///               "GUID": "be0b01ff-a037-4e31-bfc0-aca6b69e3299",
        ///               "form_guid": "6892c33b-d8ba-488f-95c5-e4e2aafe1016",
        ///               "item_guid": "a909ee8e-99e7-44ab-9d85-5c471887b922",
        ///               "staff_guid": "e8669c12-b0d6-4bc0-b109-c69a9de9bc1e",
        ///               "date": "2026-01-13",
        ///               "suggested_dates": "[\"2026-01-14\"]",
        ///               "is_any_date": "false",
        ///               "assigned_shift": "swing",
        ///               "can_full": "true",
        ///               "can_half_am": "false",
        ///               "can_half_pm": "false",
        ///               "is_forbidden": "false",
        ///               "selected_full": "",
        ///               "selected_half_am": "",
        ///               "selected_half_pm": "",
        ///               "suggested_dates_list": ["2026-01-14"]
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
        /// | date | string | 2026-01-13 | 建議補休日期 |
        /// | suggested_dates | string(JSON) | ["2026-01-14","2026-01-15"] | 系統建議的可休日期清單 |
        /// | **is_any_date** | string | "true" / "false" | 若為 **"true"**，表示該員工可於此週期內任意選擇一天休假；若 "false"，則須依建議日放假 |
        /// | assigned_shift | string | swing | 對應班別（例：day/swing/midnight/holiday） |
        /// | can_full | string | true | 是否可整天休假 |
        /// | can_half_am | string | false | 是否可上午半天休 |
        /// | can_half_pm | string | false | 是否可下午半天休 |
        /// | is_forbidden | string | false | 是否被管理端禁止休假 |
        /// | selected_full | string | "" | 實際選擇全天假狀態 |
        /// | selected_half_am | string | "" | 實際選擇上午半天假狀態 |
        /// | selected_half_pm | string | "" | 實際選擇下午半天假狀態 |
        ///
        /// 🟢 **補充說明：**  
        /// - 當 `is_any_date = "true"` 時，該員工可於整個表單週期內任選一天休假。  
        /// - 若同時提供 `suggested_dates`，代表系統建議的補休日（如夜班後、週日後）。  
        /// - 補休計算規則由內部函式  
        ///   `BuildStaffDayOffSpecialDayOption()`、  
        ///   `BuildStaffDayOffSwingOption()`、  
        ///   `BuildStaffDayOffHolidayOption()`、  
        ///   `BuildStaffDayOffMidnightOption()`  
        ///   決定，會依照班別產生不同邏輯。
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
        /// - URL 為 <c>/phar_roster_api/DayOffSchedule/calculate_available_dayoff_dates</c>。  
        /// - 每次執行只會為尚未建立過的組合（item_guid + staff_guid + date）新增 option。  
        /// - 已存在的資料不會重複生成。  
        /// - 本 API 主要給後端定期運算或人工觸發使用，用以建立系統建議假期。
        /// </remarks>
        /// <param name="returnData">封裝 API 請求內容的物件，包含表單名稱。</param>
        /// <returns>回傳更新後的排休表單結構，含新生成的 StaffDayOffOption 記錄。</returns>
        [HttpPost("calculate_available_dayoff_dates")]
        public string calculate_available_dayoff_dates([FromBody] returnData returnData)
        {
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
                List<object[]> obj_staffDayOffOption = sql_staffDayOffOptionClass.GetRowsByDefult(null, "form_guid", dayOffScheduleForm.GUID);

                List<DayOffScheduleDayClass> dayOffScheduleDayClasses = obj_dayOffScheduleDays.SQLToClass<DayOffScheduleDayClass>();
                List<DayOffScheduleItemClass> dayOffScheduleItemClasses = obj_dayOffScheduleItem.SQLToClass<DayOffScheduleItemClass>();
                List<StaffDayOffOptionClass> staffDayOffOptionClasses = obj_staffDayOffOption.SQLToClass<StaffDayOffOptionClass>();

                // ✅ 已存在 option 的快速索引（避免重複新增）
                // Unique Key: item_guid|staff_guid|date
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

                foreach (var dayOffScheduleDay in dayOffScheduleDayClasses)
                {
                    dayOffScheduleDay.items = dayOffScheduleItemClasses
                                                .Where(x => x.day_guid == dayOffScheduleDay.GUID)
                                                .ToList();
                    foreach (var item in dayOffScheduleDay.items)
                    {
                        item.option = staffDayOffOptionClasses
                                                    .Where(x => x.staff_guid == item.staff_guid && x.date.StringToDateTime().ToDateString("-") == item.date.StringToDateTime().ToDateString("-"))
                                                    .FirstOrDefault();
                    }
                }
                // ✅ 取得 items 後：依 staff 分類
                var staffGroups = dayOffScheduleItemClasses
                    .Where(x => x != null && x.staff_guid.StringIsEmpty() == false)
                    .GroupBy(x => x.staff_guid)
                    .Select(g => new
                    {
                        staff_guid = g.Key,
                        staff_name = g.FirstOrDefault()?.staff_name ?? "", // 若你的 Item 有 staff_name
                        items = g.OrderBy(x => x.date)  // 依日期排序（若 item 有 date 欄位）
                                 .ToList()
                    })
                    .OrderBy(x => x.staff_name)
                    .ToList();
                Dictionary<string, List<DayOffScheduleItemClass>> staffItemDict = dayOffScheduleItemClasses
                    .Where(x => x != null && x.staff_guid.StringIsEmpty() == false)
                    .GroupBy(x => x.staff_guid)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // key: staff_guid|yyyy-MM-dd
                Dictionary<string, List<DayOffScheduleItemClass>> itemIndex =
                    staffItemDict
                        .SelectMany(kv => kv.Value.Select(item => new { staffGuid = kv.Key, item }))
                        .Where(x => x.item != null && x.item.date.StringIsEmpty() == false)
                        .GroupBy(x => $"{x.staffGuid}|{x.item.date.StringToDateTime().ToDateString('-')}")
                        .ToDictionary(g => g.Key, g => g.Select(x => x.item).ToList());
                List<StaffDayOffOptionClass> staffDayOffOptions_add = new List<StaffDayOffOptionClass>();
                List<DayOffScheduleItemClass> dayOffScheduleItems_update = new List<DayOffScheduleItemClass>();
                foreach (var key in staffItemDict.Keys)
                {
                    // ✅ 找 items
                    staffItemDict.TryGetValue(key, out var items);
                    items ??= new List<DayOffScheduleItemClass>();
                    foreach (var item in items)
                    {
                        StaffDayOffOptionClass staffDayOffOptionClass = null;

                        if (staffDayOffOptionClass == null) staffDayOffOptionClass = BuildStaffDayOffSpecialDayOption(item, itemIndex);
                        if (staffDayOffOptionClass == null) staffDayOffOptionClass = BuildStaffDayOffSwingOption(item, itemIndex);
                        if (staffDayOffOptionClass == null) staffDayOffOptionClass = BuildStaffDayOffHolidayOption(item, itemIndex);
                        if (staffDayOffOptionClass == null) staffDayOffOptionClass = BuildStaffDayOffMidnightOption(item, itemIndex);
                        if (staffDayOffOptionClass == null) continue;
                        // ✅ 組合唯一 key
                        string optionDate = staffDayOffOptionClass.date.StringToDateTime().ToDateString('-');
                        string optionKey = $"{staffDayOffOptionClass.item_guid}|{staffDayOffOptionClass.staff_guid}|{optionDate}";

                        // ✅ 已存在就跳過
                        if (existsOptionKeySet.Contains(optionKey)) continue;

                        // ✅ 同一次執行也避免重複新增
                        existsOptionKeySet.Add(optionKey);
                        item.option_guid = staffDayOffOptionClass.GUID;
                        dayOffScheduleItems_update.Add(item);
                        staffDayOffOptions_add.Add(staffDayOffOptionClass);

                    }

                }

                sql_staffDayOffOptionClass.AddRows(null, staffDayOffOptions_add.ClassToSQL<StaffDayOffOptionClass>());
                sql_dayOffScheduleItemClass.UpdateByDefulteExtra(null, dayOffScheduleItems_update.ClassToSQL<DayOffScheduleItemClass>());
                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Data = dayOffScheduleForm;
                returnData.Result = $"新增排休資料成功,共{staffDayOffOptions_add.Count}筆";
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
                if (itemDate.DayOfWeek == DayOfWeek.Saturday) dateTimeSuggestedDate = itemDate.AddDays(5);
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
                    dateTimeSuggestedDate = GetFirstSaturdayOfMonth(itemDate);
                    if (HasWorkShift(itemIndex, item.staff_guid, dateTimeSuggestedDate))
                    {
                        return null;
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
                option.assigned_shift = ShiftTypeEnum.midnight.GetEnumName();
                // 超出月份 → 任選日期
                if (dateTimeSuggestedDate.Month != itemDate.Month)
                {
                    dateTimeSuggestedDate = GetFirstSaturdayOfMonth(itemDate);
                    if (HasWorkShift(itemIndex, item.staff_guid, dateTimeSuggestedDate))
                    {
                        return null;
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
