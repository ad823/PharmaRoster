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


        // ✅ 全域鎖：避免多人同時 init_flow（同一台 API Server 有效）
        private static readonly object _dayoffInitFlowLock = new object();
        /// <summary>
        /// 初始化排休流程：重置指定表單(form_guid)狀態並開放第一組週休，同時強制鎖定其他表單（一次只能有一個表單進入排休流程）。
        /// </summary>
        /// <remarks>
        /// ===============================
        /// 【API 說明】
        /// ===============================
        /// 本 API 用於初始化指定排休表單(form_guid)的排休流程：
        /// 1) 將該表單下所有組別 status 強制設為 "0"（鎖定不可填）
        /// 2) 將該表單下排序第一組(order_index 最小) status 設為 "1"（可填寫週休）
        /// 3) 強制鎖定「其他所有表單」的所有組別 status= "0"
        ///    - 目的：確保系統一次只能有一張表單進入排休流程（避免多表單同時排休導致狀態機錯亂）
        ///
        /// 補充：
        /// - 本 API 會使用全域 lock 防止多人/多請求同時初始化導致流程衝突（同一台 API Server 有效）
        ///
        /// ===============================
        /// 【URL】
        /// ===============================
        /// POST /phar_roster_api/dayOffSchedule/init_flow
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
        /// 【流程規則（重要）】
        /// ===============================
        /// 1) 一次只能有一張表單進入排休流程
        ///    - 若其他表單存在 status="1" 或 status="3"（表示正在填寫週休/特休）
        ///      且 force != true → 直接拒絕初始化
        ///    - 若 force=true → 允許強制重置並搶回控制權
        ///
        /// 2) 初始化後狀態分布：
        ///    - 本次 form_guid：
        ///        - 全部組別 status = "0"
        ///        - 第一組 status = "1"
        ///    - 其他表單：
        ///        - 全部組別 status = "0"
        ///
        /// ===============================
        /// 【force 參數行為說明】
        /// ===============================
        /// force=false（預設）
        /// - 若本表單已進行（存在 status=2/3/4）→ 拒絕
        /// - 若其他表單進行中（存在 status=1/3）→ 拒絕
        ///
        /// force=true
        /// - 強制重置本表單：
        ///    - 清空（重置）本表單的時間欄位為 DateTime.MinValue
        ///      weekly_fill_start_at / weekly_completed_at / annual_fill_start_at / annual_completed_at
        /// - 強制鎖定其他表單（只改 status，不動時間欄位，保留歷史）
        ///
        /// ===============================
        /// 【時間欄位寫入規則】(DATETIME)
        /// ===============================
        /// 任何狀態調整時：
        /// - status_changed_at = now
        /// - updated_at = now
        ///
        /// 本表單第一組開放週休時：
        /// - weekly_fill_start_at：
        ///   - force=true → 一律寫入 now
        ///   - force=false → 若 weekly_fill_start_at 為空或 MinValue 才寫入 now
        ///
        /// 其他表單強制鎖定：
        /// - 僅調整 status / status_changed_at / updated_at
        /// - 不修改 weekly/annual 的開始與完成時間欄位（保留歷史）
        ///
        /// ===============================
        /// 【傳入參數】(ValueAry)
        /// ===============================
        /// form_guid = 排休表單 GUID（必填）
        /// force     = true/false（選填，預設 false）
        ///
        /// ===============================
        /// 【JSON 傳入範例】
        /// ===============================
        /// (1) 正常初始化（不強制）
        /// {
        ///   "ValueAry": [
        ///     "form_guid=FORM_GUID_001",
        ///     "force=false"
        ///   ]
        /// }
        ///
        /// (2) 強制重置初始化（搶回控制權）
        /// {
        ///   "ValueAry": [
        ///     "form_guid=FORM_GUID_001",
        ///     "force=true"
        ///   ]
        /// }
        ///
        /// ===============================
        /// 【成功回傳 JSON 範例】
        /// ===============================
        /// {
        ///   "Code": 200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/init_flow",
        ///   "Result": "流程初始化完成，已開放第一組週休：1（其餘表單已強制鎖定）",
        ///   "Data": [
        ///     {
        ///       "GUID": "GROUP_GUID_001",
        ///       "form_guid": "FORM_GUID_001",
        ///       "order_index": "1",
        ///       "status": "1",
        ///       "weekly_fill_start_at": "2026-01-21 22:30:00",
        ///       "status_changed_at": "2026-01-21 22:30:00",
        ///       "updated_at": "2026-01-21 22:30:00"
        ///     }
        ///   ]
        /// }
        ///
        /// ===============================
        /// 【失敗回傳 JSON 範例】
        /// ===============================
        /// (1) 缺少 form_guid
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/init_flow",
        ///   "Result": "未提供 form_guid",
        ///   "Data": null
        /// }
        ///
        /// (2) 查無組別
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/init_flow",
        ///   "Result": "查無組別資料 form_guid=FORM_GUID_001",
        ///   "Data": null
        /// }
        ///
        /// (3) 其他表單正在排休（存在 status=1/3）且 force=false
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/init_flow",
        ///   "Result": "已有其他表單正在排休流程中(status=1/3)，請先完成或使用 force=true 強制重置",
        ///   "Data": null
        /// }
        ///
        /// (4) 本表單流程已進行（存在 status=2/3/4）且 force=false
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/init_flow",
        ///   "Result": "流程已進行(存在 status=2/3/4)，如需重置請帶入 force=true",
        ///   "Data": null
        /// }
        ///
        /// (5) 例外錯誤
        /// {
        ///   "Code": -500,
        ///   "Method": "/phar_roster_api/dayOffSchedule/init_flow",
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

                // ✅ 用 lock 防止同時 init 造成兩張表單同時 open
                lock (_dayoffInitFlowLock)
                {
                    string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

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
                    // ❷ 防呆：如果其他表單正在排休(status=1 或 3)，除非 force=true 否則拒絕
                    // =========================================================
                    if (!force)
                    {
                        bool otherFormInProgress = allGroups.Any(g =>
                            g.form_guid != form_guid &&
                            (g.status == "1" || g.status == "3"));

                        if (otherFormInProgress)
                        {
                            returnData.Code = -200;
                            returnData.Result = "已有其他表單正在排休流程中(status=1/3)，請先完成或使用 force=true 強制重置";
                            return returnData.JsonSerializationt();
                        }
                    }

                    // =========================================================
                    // ❸ 取得本次表單的 groups
                    // =========================================================
                    List<DayOffGroupClass> groups = allGroups
                        .Where(g => g.form_guid == form_guid)
                        .OrderBy(g => g.order_index.StringToInt32())
                        .ToList();

                    if (groups == null || groups.Count == 0)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"查無組別資料 form_guid={form_guid}";
                        return returnData.JsonSerializationt();
                    }

                    // =========================================================
                    // ❹ 防呆：本表單流程已進行 → force=false 時拒絕
                    // =========================================================
                    if (!force)
                    {
                        bool alreadyStarted = groups.Any(g => g.status == "2" || g.status == "3" || g.status == "4");
                        if (alreadyStarted)
                        {
                            returnData.Code = -200;
                            returnData.Result = "流程已進行(存在 status=2/3/4)，如需重置請帶入 force=true";
                            return returnData.JsonSerializationt();
                        }
                    }

                    // =========================================================
                    // ❺ 本表單：全部鎖定(status=0)
                    //     - force=true 才清空時間欄位
                    // =========================================================
                    foreach (var g in groups)
                    {
                        g.status = "0";
                        g.status_changed_at = now;
                        g.updated_at = now;

                        if (force)
                        {
                            // ✅ force 才清時間（避免歷史失真）
                            g.weekly_fill_start_at = DateTime.MinValue.ToDateTimeString();
                            g.weekly_completed_at = DateTime.MinValue.ToDateTimeString();
                            g.annual_fill_start_at = DateTime.MinValue.ToDateTimeString();
                            g.annual_completed_at = DateTime.MinValue.ToDateTimeString();
                        }

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

                        // ✅ 只有在 force=true 或空值時才寫入開始時間
                        if (force || first.weekly_fill_start_at.StringIsEmpty() || first.weekly_fill_start_at == DateTime.MinValue.ToDateTimeString())
                        {
                            first.weekly_fill_start_at = now;
                        }

                        sql_dayOffGroupClass.UpdateByDefulteExtra(null, first.ClassToSQL<DayOffGroupClass>());
                    }

                    // =========================================================
                    // ❼ 強制鎖定其他所有表單（一次只能一張表單排休）
                    //     - 只動 status / changed_at / updated_at
                    //     - 不動它們的時間欄位（保留歷史）
                    // =========================================================
                    var otherFormGroups = allGroups
                        .Where(g => g.form_guid != form_guid)
                        .ToList();

                    foreach (var og in otherFormGroups)
                    {
                        // 只要不是鎖定狀態就強制鎖定
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
        /// 檢查指定 staff 是否在「目前排休輪次(open group)」中（是否輪到填寫週休/特休）。
        /// </summary>
        /// <remarks>
        /// ===============================
        /// 【API 說明】
        /// ===============================
        /// 本 API 用於前端判斷「使用者是否輪到填寫排休」。
        /// 系統排休流程規則：
        /// 1) 一次只能有一張排休表單進入排休流程（Active Form）
        /// 2) 同一時間只允許存在 1 個 open group：
        ///    - 週休階段：open group 的狀態為 status="1"
        ///    - 特休階段：open group 的狀態為 status="3"
        ///
        /// 本 API 會：
        /// - 找出目前 active 表單（若未提供 form_guid）
        /// - 找出該表單目前 open group（status=1 或 3）
        /// - 查詢 staff_id 是否屬於 open group
        /// - 回傳 can_write/is_in_round 供前端控制填寫權限
        ///
        /// ===============================
        /// 【URL】
        /// ===============================
        /// POST /phar_roster_api/dayOffSchedule/check_staff_in_current_round
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
        /// "1" = 可填寫週休（open group）
        /// "2" = 週休填寫完成
        /// "3" = 可填寫特休（open group）
        /// "4" = 特休填寫完成
        ///
        /// ===============================
        /// 【傳入參數】(ValueAry)
        /// ===============================
        /// staff_id    = 員工識別碼/員工 GUID（必填）
        /// form_guid   = 排休表單 GUID（選填）
        ///              - 有提供：以該表單為判斷依據（不使用全系統 active form 探測）
        ///              - 未提供：自動從全系統組別中尋找唯一 open group（status=1 或 3）
        ///
        /// ===============================
        /// 【流程判斷規則】
        /// ===============================
        /// A) 有提供 form_guid：
        ///    - 僅針對該表單找 open group
        ///    - 若同表單同時存在 status=1 與 status=3 → 視為資料異常，回 -200
        ///    - 若同表單 status=1 或 status=3 超過 1 組 → 視為資料異常，回 -200
        ///
        /// B) 未提供 form_guid：
        ///    - 全系統只允許存在 1 組 status=1 或 1 組 status=3（二擇一）
        ///    - 若同時存在 status=1 與 status=3 → 視為資料異常，回 -200
        ///    - 若 status=1 超過 1 組或 status=3 超過 1 組 → 視為資料異常，回 -200
        ///    - 若全系統不存在 status=1/3 → 視為目前沒有表單排休中，回 Code=200, stage=none
        ///
        /// C) staff 判斷：
        ///    - 以 DayOffGroupMemberClass 的 form_guid + staff_id 尋找 staff 所屬 group_guid
        ///    - 若 staff 不在該表單任何組別 → 回 -200
        ///    - 若 staff 所屬 group_guid == open_group_guid → can_write=true / is_in_round=true
        ///
        /// ===============================
        /// 【回傳資料(Data)欄位說明】
        /// ===============================
        /// staff_id                 : 本次查詢 staff_id
        /// active_form_guid         : 目前 active 表單 GUID（若無則為 null）
        /// stage                    : "none" / "weekly" / "annual"
        /// stage_name               : "無" / "週休" / "特休"
        /// can_write                : bool，是否可填寫（等同 is_in_round）
        /// is_in_round              : bool，是否輪到（staff 是否在 open group）
        /// open_group_guid          : 目前開放組別 GUID（open group）
        /// open_group_order_index   : open group 排序序號（int）
        /// open_group_name          : open group 名稱（若無名稱欄位則為 null）
        /// staff_group_guid         : staff 所屬組別 GUID
        /// staff_group_order_index  : staff 所屬組別序號（int）
        /// staff_group_name         : staff 所屬組別名稱（若無名稱欄位則為 null）
        /// next_group_guid          : 下一組 GUID（若已是最後一組則為 null）
        /// next_group_order_index   : 下一組序號（可能為 null）
        /// next_group_name          : 下一組名稱（若無名稱欄位則為 null）
        /// remain_groups_to_open    : 還差幾組才輪到（0 表示輪到或已超過）
        /// message                  : 完整提示訊息（可直接顯示）
        /// progress_message         : 簡短進度訊息（更口語）
        ///
        /// ===============================
        /// 【JSON 傳入範例】
        /// ===============================
        /// (1) 未指定 form_guid（自動找 active form）
        /// {
        ///   "ValueAry": [
        ///     "staff_id=STAFF_GUID_001"
        ///   ]
        /// }
        ///
        /// (2) 指定某一張表單判斷
        /// {
        ///   "ValueAry": [
        ///     "staff_id=STAFF_GUID_001",
        ///     "form_guid=FORM_GUID_001"
        ///   ]
        /// }
        ///
        /// ===============================
        /// 【成功回傳 JSON 範例】
        /// ===============================
        /// ------------------------------------------------
        /// 情境 A：staff 輪到（在 open group）
        /// ------------------------------------------------
        /// {
        ///   "Code": 200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/check_staff_in_current_round",
        ///   "Result": "staff 已輪到可填寫",
        ///   "Data": {
        ///     "staff_id": "STAFF_GUID_001",
        ///     "active_form_guid": "FORM_GUID_001",
        ///     "stage": "weekly",
        ///     "stage_name": "週休",
        ///     "can_write": true,
        ///     "is_in_round": true,
        ///     "open_group_guid": "GROUP_GUID_002",
        ///     "open_group_order_index": 2,
        ///     "open_group_name": null,
        ///     "staff_group_guid": "GROUP_GUID_002",
        ///     "staff_group_order_index": 2,
        ///     "staff_group_name": null,
        ///     "next_group_guid": "GROUP_GUID_003",
        ///     "next_group_order_index": 3,
        ///     "next_group_name": null,
        ///     "remain_groups_to_open": 0,
        ///     "message": "目前輪到第 2 組(週休)，你屬於第 2 組，可開始填寫",
        ///     "progress_message": "✅ 已輪到你填寫週休。"
        ///   }
        /// }
        ///
        /// ------------------------------------------------
        /// 情境 B：staff 尚未輪到
        /// ------------------------------------------------
        /// {
        ///   "Code": 200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/check_staff_in_current_round",
        ///   "Result": "staff 尚未輪到填寫",
        ///   "Data": {
        ///     "staff_id": "STAFF_GUID_001",
        ///     "active_form_guid": "FORM_GUID_001",
        ///     "stage": "annual",
        ///     "stage_name": "特休",
        ///     "can_write": false,
        ///     "is_in_round": false,
        ///     "open_group_guid": "GROUP_GUID_001",
        ///     "open_group_order_index": 1,
        ///     "staff_group_guid": "GROUP_GUID_003",
        ///     "staff_group_order_index": 3,
        ///     "remain_groups_to_open": 2,
        ///     "message": "目前輪到第 1 組(特休)，你屬於第 3 組，尚未輪到（還差 2 組）",
        ///     "progress_message": "⏳ 尚未輪到你填寫特休，目前進度：第 1 組 / 你在第 3 組。"
        ///   }
        /// }
        ///
        /// ------------------------------------------------
        /// 情境 C：目前沒有任何表單在排休（全系統無 status=1/3）
        /// ------------------------------------------------
        /// {
        ///   "Code": 200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/check_staff_in_current_round",
        ///   "Result": "目前無任何表單正在排休流程中",
        ///   "Data": {
        ///     "staff_id": "STAFF_GUID_001",
        ///     "active_form_guid": null,
        ///     "stage": "none",
        ///     "stage_name": "無",
        ///     "can_write": false,
        ///     "is_in_round": false,
        ///     "message": "目前尚未開始排休流程",
        ///     "progress_message": "目前尚未開始排休流程"
        ///   }
        /// }
        ///
        /// ===============================
        /// 【失敗回傳 JSON 範例】
        /// ===============================
        /// (1) 未提供 staff_id
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/check_staff_in_current_round",
        ///   "Result": "未提供 staff_id",
        ///   "Data": null
        /// }
        ///
        /// (2) 指定 form_guid 但查無組別
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/check_staff_in_current_round",
        ///   "Result": "查無組別資料 form_guid=FORM_GUID_001",
        ///   "Data": null
        /// }
        ///
        /// (3) 流程狀態異常：同時存在週休與特休 open（全系統或同表單）
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/check_staff_in_current_round",
        ///   "Result": "流程狀態異常：同時存在可填週休(status=1)與可填特休(status=3)",
        ///   "Data": null
        /// }
        ///
        /// (4) staff 不在該表單任何組別
        /// {
        ///   "Code": -200,
        ///   "Method": "/phar_roster_api/dayOffSchedule/check_staff_in_current_round",
        ///   "Result": "staff_id=STAFF_GUID_001 不在該排休表單(form_guid=FORM_GUID_001)的任何組別中",
        ///   "Data": null
        /// }
        /// </remarks>
        /// <param name="returnData">returnData 物件，主要使用 ValueAry 作為參數輸入。</param>
        /// <returns>回傳 returnData.JsonSerializationt() 的 JSON 字串。</returns>
        [HttpPost("check_staff_in_current_round")]
        public string check_staff_in_current_round([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "/phar_roster_api/dayOffSchedule/check_staff_in_current_round";

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry?
                    .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string staff_id = GetVal("staff_id");
                string form_guid_param = GetVal("form_guid");

                if (staff_id.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 staff_id";
                    return returnData.JsonSerializationt();
                }

                var sql_dayOffGroupClass = MethodClass.GetSQLControl<DayOffGroupClass>();
                var sql_dayOffGroupMemberClass = MethodClass.GetSQLControl<DayOffGroupMemberClass>();

                // ================================
                // 取全系統組別
                // ================================
                List<DayOffGroupClass> allGroups = sql_dayOffGroupClass.GetAllRows(null).SQLToClass<DayOffGroupClass>();
                if (allGroups == null || allGroups.Count == 0)
                {
                    returnData.Code = 200;
                    returnData.Result = "目前無任何表單正在排休流程中";
                    returnData.Data = new
                    {
                        staff_id,
                        active_form_guid = (string)null,
                        stage = "none",
                        stage_name = "無",
                        can_write = false,
                        is_in_round = false,
                        message = "目前尚未開始排休流程",
                        progress_message = "目前尚未開始排休流程"
                    };
                    return returnData.JsonSerializationt();
                }

                // 小工具：安全抓 group name（若你沒有此欄位，會回 null）
                string GetGroupName(DayOffGroupClass g)
                {
                    if (g == null) return null;

                    // ✅ 如果你有名稱欄位，請在這裡改成你自己的欄位，例如 g.group_name
                    // return g.group_name;

                    // 暫時：若沒有名稱欄位 → 回 null
                    return null;
                }

                // ================================
                // 決定 active form / open group
                // ================================
                string active_form_guid = null;
                string stage = "none";
                DayOffGroupClass openGroup = null;

                if (!form_guid_param.StringIsEmpty())
                {
                    active_form_guid = form_guid_param;

                    var formGroups = allGroups
                        .Where(g => g.form_guid == active_form_guid)
                        .OrderBy(g => g.order_index.StringToInt32())
                        .ToList();

                    if (formGroups.Count == 0)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"查無組別資料 form_guid={active_form_guid}";
                        return returnData.JsonSerializationt();
                    }

                    var openWeekly = formGroups.Where(g => g.status == "1").ToList();
                    var openAnnual = formGroups.Where(g => g.status == "3").ToList();

                    if (openWeekly.Count > 0 && openAnnual.Count > 0)
                    {
                        returnData.Code = -200;
                        returnData.Result = "流程狀態異常：同一表單同時存在 status=1 與 status=3";
                        return returnData.JsonSerializationt();
                    }
                    if (openWeekly.Count > 1 || openAnnual.Count > 1)
                    {
                        returnData.Code = -200;
                        returnData.Result = "流程狀態異常：同一表單 open group 數量異常（status=1 或 status=3 超過 1 組）";
                        return returnData.JsonSerializationt();
                    }

                    if (openWeekly.Count == 1) { stage = "weekly"; openGroup = openWeekly[0]; }
                    else if (openAnnual.Count == 1) { stage = "annual"; openGroup = openAnnual[0]; }
                    else stage = "none";
                }
                else
                {
                    var openWeeklyAll = allGroups.Where(g => g.status == "1").ToList();
                    var openAnnualAll = allGroups.Where(g => g.status == "3").ToList();

                    if (openWeeklyAll.Count > 0 && openAnnualAll.Count > 0)
                    {
                        returnData.Code = -200;
                        returnData.Result = "流程狀態異常：同時存在可填週休(status=1)與可填特休(status=3)";
                        return returnData.JsonSerializationt();
                    }
                    if (openWeeklyAll.Count > 1)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"流程狀態異常：可填週休(status=1)組別數量={openWeeklyAll.Count}，應僅允許 1 組";
                        return returnData.JsonSerializationt();
                    }
                    if (openAnnualAll.Count > 1)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"流程狀態異常：可填特休(status=3)組別數量={openAnnualAll.Count}，應僅允許 1 組";
                        return returnData.JsonSerializationt();
                    }

                    if (openWeeklyAll.Count == 1)
                    {
                        stage = "weekly";
                        openGroup = openWeeklyAll[0];
                        active_form_guid = openGroup.form_guid;
                    }
                    else if (openAnnualAll.Count == 1)
                    {
                        stage = "annual";
                        openGroup = openAnnualAll[0];
                        active_form_guid = openGroup.form_guid;
                    }
                    else
                    {
                        returnData.Code = 200;
                        returnData.Result = "目前無任何表單正在排休流程中";
                        returnData.Data = new
                        {
                            staff_id,
                            active_form_guid = (string)null,
                            stage = "none",
                            stage_name = "無",
                            can_write = false,
                            is_in_round = false,
                            message = "目前尚未開始排休流程",
                            progress_message = "目前尚未開始排休流程"
                        };
                        return returnData.JsonSerializationt();
                    }
                }

                if (stage == "none" || openGroup == null || active_form_guid.StringIsEmpty())
                {
                    returnData.Code = 200;
                    returnData.Result = "該表單目前無 open group（尚未初始化或已結束）";
                    returnData.Data = new DayOffCheckStaffInRoundResponse
                    {
                        staff_id = staff_id,
                        active_form_guid = active_form_guid,
                        stage = "none",
                        stage_name = "無",
                        can_write = false,
                        is_in_round = false,
                        message = "目前無開放可填寫的組別",
                        progress_message = "目前無開放可填寫的組別"
                    };
                    return returnData.JsonSerializationt();
                }

                // ================================
                // 取得 staff 所屬 group（members）
                // ================================
                List<DayOffGroupMemberClass> members = sql_dayOffGroupMemberClass
                    .GetRowsByDefult(null, "form_guid", active_form_guid)
                    .SQLToClass<DayOffGroupMemberClass>();

                var staffMember = members?.FirstOrDefault(m => m.staff_id == staff_id);
                if (staffMember == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"staff_id={staff_id} 不在該排休表單(form_guid={active_form_guid})的任何組別中";
                    return returnData.JsonSerializationt();
                }

                // staff 所屬組別（同一張 form_guid）
                var formGroupsAll = allGroups
                    .Where(g => g.form_guid == active_form_guid)
                    .OrderBy(g => g.order_index.StringToInt32())
                    .ToList();

                DayOffGroupClass staffGroup = formGroupsAll.FirstOrDefault(g => g.GUID == staffMember.group_guid);

                int openOrder = openGroup.order_index.StringToInt32();
                int staffOrder = (staffGroup?.order_index ?? "0").StringToInt32();

                bool isInRound = staffMember.group_guid == openGroup.GUID;
                bool canWrite = isInRound;

                // 下一組資訊（同表單）
                DayOffGroupClass nextGroup = formGroupsAll
                    .FirstOrDefault(g => g.order_index.StringToInt32() == openOrder + 1);

                // 還差幾組
                int remain = Math.Max(0, staffOrder - openOrder);

                string stageName = stage == "weekly" ? "週休" : "特休";
                string message = isInRound
                    ? $"目前輪到第 {openOrder} 組({stageName})，你屬於第 {staffOrder} 組，可開始填寫"
                    : $"目前輪到第 {openOrder} 組({stageName})，你屬於第 {staffOrder} 組，尚未輪到（還差 {remain} 組）";

                string progressMessage = isInRound
                    ? $"✅ 已輪到你填寫{stageName}。"
                    : $"⏳ 尚未輪到你填寫{stageName}，目前進度：第 {openOrder} 組 / 你在第 {staffOrder} 組。";

                // ================================
                // 回傳
                // ================================
                returnData.Code = 200;
                returnData.Result = canWrite ? "staff 已輪到可填寫" : "staff 尚未輪到填寫";
                returnData.Data = new DayOffCheckStaffInRoundResponse
                {
                    // staff
                    staff_id = staff_id,

                    // 流程/表單
                    active_form_guid = active_form_guid,
                    stage = stage,
                    stage_name = stageName,

                    // 權限
                    can_write = canWrite,
                    is_in_round = isInRound,

                    // open group
                    open_group_guid = openGroup.GUID,
                    open_group_order_index = openOrder,
                    open_group_name = GetGroupName(openGroup),

                    // staff group
                    staff_group_guid = staffMember.group_guid,
                    staff_group_order_index = staffOrder,
                    staff_group_name = GetGroupName(staffGroup),

                    // next group
                    next_group_guid = nextGroup?.GUID,
                    next_group_order_index = nextGroup.order_index.StringToInt32(),
                    next_group_name = GetGroupName(nextGroup),

                    // remain / message
                    remain_groups_to_open = remain,
                    message = message,
                    progress_message = progressMessage
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
        /// 取得下一組（依 order_index 排序後）
        /// </summary>
        private DayOffGroupClass GetNextGroup(List<DayOffGroupClass> groups, DayOffGroupClass current)
        {
            int idx = groups.FindIndex(g => g.GUID == current.GUID);
            if (idx < 0) return null;
            if (idx + 1 >= groups.Count) return null;
            return groups[idx + 1];
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
