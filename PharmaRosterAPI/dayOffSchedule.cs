using Basic;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyOffice;
using MySql.Data.MySqlClient;
using NPOI.HSSF.Util;
using NPOI.SS.Formula.Eval;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PharmaRosterLib;
using PharmaRosterLib.Helpers.ImportSchedule;
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
using System.Threading;
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
                tables.Add(PharmaRosterLib.MethodClass.CheckCreatTable<DayOffReleasePoolClass>());


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

        private static readonly SemaphoreSlim _calculateAvailableDayoffDatesSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 計算可用休假日期，並自動補入週日 FF 與特殊日 NH
        /// </summary>
        /// <remarks>
        /// ===============================
        /// 【API 說明】
        /// ===============================
        /// 本 API 用於依據指定排休表單 form_name，計算並產生人員可用休假資料。
        ///
        /// 原有功能：
        /// 1. 依排班資料計算特殊規則休假 option。
        /// 2. 依特殊日、擺班、假日、夜班等規則產生 StaffDayOffOptionClass。
        /// 3. 週日若人員沒有排班，系統自動補入 FF 強制休假。
        ///
        /// 本版新增：
        /// 1. 若日期為特殊日 / 國定假日，且人員當日沒有排班，系統自動補入 NH。
        /// 2. NH 效果等同 FF，屬於強制休假一日。
        /// 3. 若某日同時為週日與特殊日，優先補 NH。
        ///
        ///
        /// ===============================
        /// 【自動補休規則】
        /// ===============================
        /// 一、週日無排班
        /// - 自動新增 DayOffScheduleItemClass
        /// - selected_dayoff_type = "FF"
        /// - 自動新增 StaffDayOffOptionClass
        /// - selected_full = "true"
        /// - is_force_ff = "true"
        ///
        /// 二、特殊日 / 國定假日無排班
        /// - 自動新增 DayOffScheduleItemClass
        /// - selected_dayoff_type = "NH"
        /// - is_special_day = "true"
        /// - 自動新增 StaffDayOffOptionClass
        /// - selected_full = "true"
        /// - is_force_ff = "true"
        /// - dayoff_source_type = "NATIONAL_HOLIDAY"
        ///
        /// 三、特殊日與週日重疊
        /// - 優先產生 NH
        /// - 不再產生 FF
        ///
        /// 四、已存在 item 的日期
        /// - 不再自動補 FF / NH
        ///
        /// 五、已被預留休建議日期占用
        /// - 不再自動補 FF / NH
        ///
        ///
        /// ===============================
        /// 【傳入參數】ValueAry
        /// ===============================
        /// form_name = 排休表單名稱（必填）
        /// simple    = true / false（選填）
        ///
        ///
        /// ===============================
        /// 【Request JSON 範例】
        /// ===============================
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026-03",
        ///     "simple=false"
        ///   ]
        /// }
        ///
        ///
        /// ===============================
        /// 【成功回傳 JSON 範例】
        /// ===============================
        /// {
        ///   "Code": 200,
        ///   "Method": "calculate_available_dayoff_dates",
        ///   "Result": "計算完成，新增 item 10 筆，新增 option 10 筆，更新 item 5 筆",
        ///   "Data": {
        ///     "GUID": "FORM_GUID",
        ///     "form_name": "2026-03",
        ///     "days": []
        ///   }
        /// }
        ///
        ///
        /// ===============================
        /// 【錯誤回傳 JSON 範例】
        /// ===============================
        /// {
        ///   "Code": -200,
        ///   "Method": "calculate_available_dayoff_dates",
        ///   "Result": "找不到表單名稱(2026-03)"
        /// }
        ///
        ///
        /// ===============================
        /// 【注意事項】
        /// ===============================
        /// 1. 本 API 會寫入資料庫。
        /// 2. 本 API 有單機 SemaphoreSlim 鎖，一次只允許一個使用者執行。
        /// 3. NH 與 FF 都屬於強制休假，不應再被使用者手動選擇或釋出。
        /// 4. 前端若要顯示 NH，可依：
        ///    - item.selected_dayoff_type == "NH"
        ///    - 或 option.dayoff_source_type == "NATIONAL_HOLIDAY"
        ///    判斷。
        /// </remarks>
        /// <param name="returnData">returnData 物件，使用 ValueAry 傳入 form_name / simple。</param>
        /// <returns>回傳計算後的排休表單資料。</returns>
        [HttpPost("calculate_available_dayoff_dates")]
        public string calculate_available_dayoff_dates([FromBody] returnData returnData)
        {
            init(returnData);
            var timer = new MyTimerBasic();
            returnData.Method = "calculate_available_dayoff_dates";

            bool entered = _calculateAvailableDayoffDatesSemaphore.Wait(0);
            if (!entered)
            {
                returnData.Code = -200;
                returnData.Result = "目前已有使用者正在執行計算，請稍後再試";
                returnData.TimeTaken = $"{timer}";
                return returnData.JsonSerializationt();
            }

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
                var sql_specialDayClass = MethodClass.GetSQLControl<SpecialDayClass>();

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

                List<SpecialDayClass> specialDayClasses = sql_specialDayClass
                    .GetAllRows(null)
                    .SQLToClass<SpecialDayClass>();

                HashSet<string> specialDaySet = specialDayClasses
                    .Where(x => x != null && x.date.StringIsEmpty() == false)
                    .Select(x => x.date.StringToDateTime().ToDateString('-'))
                    .Where(x => x.StringIsEmpty() == false)
                    .ToHashSet();

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

                    return true;
                }

                StaffDayOffOptionClass BuildForceFFOption(DayOffScheduleItemClass item, DateTime dt)
                {
                    string offDate = dt.ToDateString('-');

                    if (dt.DayOfWeek == DayOfWeek.Saturday) item.shift_requirement = BuildHolidayOffShiftRequirementJson(dt);
                    if (dt.DayOfWeek == DayOfWeek.Sunday) item.shift_requirement = BuildHolidayOffShiftRequirementJson(dt);

                    item.selected_dayoff_type = "FF";

                    var option = new StaffDayOffOptionClass();
                    option.GUID = Guid.NewGuid().ToString();
                    option.form_guid = item.form_guid;
                    option.item_guid = item.GUID;
                    option.staff_guid = item.staff_guid;
                    option.date = offDate;
                    option.suggested_dates_list = new List<string>() { offDate };
                    option.is_any_date = "false";
                    option.assigned_shift = "OFF";

                    if (dt.DayOfWeek == DayOfWeek.Saturday)
                    {
                        option.can_full = "false";
                        option.can_half_am = "true";
                        option.can_half_pm = "false";

                        option.selected_full = "false";
                        option.selected_half_am = "true";
                        option.selected_half_pm = "false";
                    }
                    else
                    {
                        option.can_full = "true";
                        option.can_half_am = "false";
                        option.can_half_pm = "false";

                        option.selected_full = "true";
                        option.selected_half_am = "false";
                        option.selected_half_pm = "false";
                    }

                    option.is_forbidden = "false";
                    option.is_force_ff = "true";
                    option.force_ff_at = now;
                    option.updated_at = now;
                    option.released_at = DateTime.MinValue.ToDateTimeString();

                    if (option.dayoff_source_type.StringIsEmpty())
                        option.dayoff_source_type = "FORCE_FF";

                    return option;
                }

                StaffDayOffOptionClass BuildForceNHOption(DayOffScheduleItemClass item, DateTime dt)
                {
                    string offDate = dt.ToDateString('-');

                    item.shift_requirement = BuildHolidayOffShiftRequirementJson(dt);
                    item.selected_dayoff_type = "NH";
                    item.is_special_day = "true";

                    var option = new StaffDayOffOptionClass();
                    option.GUID = Guid.NewGuid().ToString();
                    option.form_guid = item.form_guid;
                    option.item_guid = item.GUID;
                    option.staff_guid = item.staff_guid;
                    option.date = offDate;
                    option.suggested_dates_list = new List<string>() { offDate };
                    option.is_any_date = "false";
                    option.assigned_shift = "OFF";

                    option.can_full = "true";
                    option.can_half_am = "false";
                    option.can_half_pm = "false";

                    option.selected_full = "true";
                    option.selected_half_am = "false";
                    option.selected_half_pm = "false";

                    option.is_forbidden = "false";
                    option.is_force_ff = "true";
                    option.force_ff_at = now;
                    option.updated_at = now;
                    option.released_at = DateTime.MinValue.ToDateTimeString();
                    option.dayoff_source_type = "NATIONAL_HOLIDAY";

                    return option;
                }

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

                foreach (var staffGuid in staffItemDict.Keys.ToList())
                {
                    var staffItems = staffItemDict[staffGuid]
                        .Where(x => x != null && x.date.StringIsEmpty() == false)
                        .OrderBy(x => x.date.StringToDateTime())
                        .ToList();

                    foreach (var item in staffItems)
                    {
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

                        if (staffDayOffOptionClass.force_ff_at.Check_Date_String() == false)
                            staffDayOffOptionClass.force_ff_at = DateTime.MinValue.ToDateString();

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

                var forceOffDays = dayOffScheduleDayClasses
                    .Where(d =>
                    {
                        DateTime dt = d.date.StringToDateTime();
                        if (dt == DateTime.MinValue) return false;

                        string dateKey = dt.ToDateString('-');

                        return dt.DayOfWeek == DayOfWeek.Sunday || specialDaySet.Contains(dateKey);
                    })
                    .OrderBy(d => d.date.StringToDateTime())
                    .ToList();

                foreach (var day in forceOffDays)
                {
                    DateTime dayDt = day.date.StringToDateTime();
                    if (dayDt == DateTime.MinValue) continue;

                    string dt = dayDt.ToDateString('-');
                    bool isSpecialDay = specialDaySet.Contains(dt);

                    foreach (var staff in staffList)
                    {
                        if (staff.staff_guid.StringIsEmpty()) continue;

                        string keyStaffDate = $"{staff.staff_guid}|{dt}";

                        if (itemKeyIndex.ContainsKey(keyStaffDate)) continue;
                        if (reservedSuggestedDateSet.Contains(keyStaffDate)) continue;

                        var newItem = new DayOffScheduleItemClass();
                        newItem.GUID = Guid.NewGuid().ToString();
                        newItem.form_guid = dayOffScheduleForm.GUID;
                        newItem.day_guid = day.GUID;
                        newItem.option_guid = "";
                        newItem.date = dt;
                        newItem.is_special_day = isSpecialDay ? "true" : "false";

                        newItem.staff_guid = staff.staff_guid;
                        newItem.staff_id = staff.staff_id;
                        newItem.staff_name = staff.staff_name;
                        newItem.staff_simple_name = staff.staff_simple_name;
                        newItem.position = staff.position;

                        newItem.shift_requirement = BuildHolidayOffShiftRequirementJson(dayDt);
                        newItem.created_at = now;
                        newItem.updated_at = now;

                        StaffDayOffOptionClass forceOpt;

                        if (isSpecialDay)
                        {
                            forceOpt = BuildForceNHOption(newItem, dayDt);
                        }
                        else
                        {
                            forceOpt = BuildForceFFOption(newItem, dayDt);
                        }

                        newItem.option_guid = forceOpt.GUID;
                        newItem.option = forceOpt;

                        dayOffScheduleItems_add.Add(newItem);
                        staffDayOffOptions_add.Add(forceOpt);

                        dayOffScheduleItemClasses.Add(newItem);
                        staffDayOffOptionClasses.Add(forceOpt);
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
            finally
            {
                if (entered)
                {
                    _calculateAvailableDayoffDatesSemaphore.Release();
                }
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
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();
                var sql_dayOffReleasePool = MethodClass.GetSQLControl<DayOffReleasePoolClass>();

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
                List<object[]> obj_dayOffReleasePool = sql_dayOffReleasePool.GetRowsByDefult(null, "form_guid", dayOffScheduleForm.GUID);

                sql_dayOffScheduleFormClass.DeleteExtra(null, obj_dayOffScheduleForm);
                sql_dayOffScheduleDayClass.DeleteExtra(null, obj_dayOffScheduleDays);
                sql_dayOffScheduleItemClass.DeleteExtra(null, obj_dayOffScheduleItem);
                sql_staffDayOffOptionClass.DeleteExtra(null, obj_staffDayOffOption);
                sql_dayOffReleasePool.DeleteExtra(null, obj_dayOffReleasePool);
                // === 3. 成功回傳 ===
                returnData.Code = 200;
                returnData.Data = dayOffScheduleForm;
                returnData.Result = $"刪除資料成功,共{obj_dayOffScheduleDays.Count}個日期,共{obj_dayOffScheduleItem.Count}筆Items,共{obj_staffDayOffOption.Count}筆Options";
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
        /// 確認匯入班表 Excel，並建立新的排班表單
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於將已完成預覽檢查的班表 Excel 正式匯入系統，並建立一份新的排班表單。
        ///
        /// 匯入來源為「班表匯入空白模板」填寫完成的 Excel。
        /// 本 API 會：
        /// 1. 驗證 Excel 內容是否合法
        /// 2. 驗證 form_name 是否重複
        /// 3. 依 year_month 建立整個月份所有日期的 DayOffScheduleDayClass
        /// 4. 依 Excel 內容建立 DayOffScheduleItemClass
        /// 5. 寫入：
        ///    - dayoff_schedule_form
        ///    - dayoff_schedule_day
        ///    - dayoff_schedule_item
        ///
        /// 本 API 不會沿用既有表單，
        /// 而是依 Excel 建立一份全新的排班表單。
        ///
        /// ---
        ///
        /// ## 🌐 URL
        /// ```text
        /// /phar_roster_api/dayOffSchedule/confirm_import_schedule_excel
        /// ```
        ///
        /// ## Method
        /// ```text
        /// POST
        /// ```
        ///
        /// ## Content-Type
        /// ```text
        /// multipart/form-data
        /// ```
        ///
        /// ---
        ///
        /// ## 📥 上傳欄位
        /// | 欄位名稱 | 型別 | 必填 | 說明 |
        /// |------|------|------|------|
        /// | file | IFormFile | ✅ | 要匯入的 Excel 檔案（僅支援 .xlsx） |
        /// | form_name | string | ✅ | 新建立的排班表單名稱，不可重複 |
        /// | year_month | string | ✅ | 年月，格式 yyyy-MM，例如 2026-05 |
        ///
        /// ---
        ///
        /// ## 📑 Excel 模板前提
        /// 本 API 預設使用者上傳的是由「班表匯入空白模板」填寫完成的 Excel。
        ///
        /// 模板規則如下：
        /// 1. 第 1 列為標題列
        /// 2. A1 = 班別類型
        /// 3. B1 = 時段
        /// 4. C1 ~ AG1 = 日期欄（01 ~ 31）
        /// 5. 第 2 列開始為固定班別列
        ///
        /// 固定班別列定義如下：
        ///
        /// | 班別類型 | 時段 |
        /// |------|------|
        /// | 國定假日 | 08:00-12:00 |
        /// | 假日門診 | 07:30-16:00 |
        /// | 假日門診 | 08:00-16:00 |
        /// | 假日急診 | 08:00-16:00 |
        /// | 化療 | 08:00-12:00 |
        /// | TPN | 08:00-16:00 |
        /// | 中藥局 | 12:30-21:00 |
        /// | 小夜門診 | 12:30-21:00 |
        /// | 小夜門診 | 13:30-22:00 |
        /// | 小夜門診 | 14:30-23:00 |
        /// | 小夜門診 | 15:30-23:59 |
        /// | 小夜急診 | 16:00-23:59 |
        /// | 小夜其他 | 12:30-21:00 |
        /// | 大夜門診 | 00:00-08:00 |
        /// | 大夜急診 | 00:00-08:00 |
        ///
        /// ---
        ///
        /// ## 📝 儲存格填寫規則
        /// 日期欄中的每個儲存格，代表：
        /// 「該日期、該班別、該時段」的人員簡名內容。
        ///
        /// ### 規則
        /// 1. 一個字代表一位人員簡名
        /// 2. 不使用任何分隔符號
        /// 3. 不加空白
        /// 4. 不加中括號
        /// 5. 沒有人就留空
        ///
        /// ### 合法範例
        /// ```text
        /// 亭庭璇詩
        /// 均甄
        /// 品
        /// 曼能
        /// ```
        ///
        /// ### 不合法範例
        /// ```text
        /// 亭、庭、璇、詩
        /// [品]陳媚松顏靖
        /// 亭 庭 璇 詩
        /// ```
        ///
        /// ---
        ///
        /// ## ✅ 驗證規則
        /// 本 API 匯入前會重新做完整驗證：
        ///
        /// ### 1. 檔案檢查
        /// - 必須有上傳檔案
        /// - 副檔名必須為 `.xlsx`
        ///
        /// ### 2. 參數檢查
        /// - form_name 必填
        /// - year_month 必填
        /// - year_month 格式必須為 yyyy-MM
        /// - form_name 不可與既有表單重複
        ///
        /// ### 3. Excel 基本結構檢查
        /// - 必須至少有一張 Sheet
        /// - 第一張 Sheet 必須可讀取
        /// - 第 1 列必須存在
        /// - A1 必須為 `班別類型`
        /// - B1 必須為 `時段`
        ///
        /// ### 4. 固定班別列檢查
        /// - 第 2 列到第 16 列必須符合固定班別定義
        ///
        /// ### 5. 日期欄檢查
        /// - 日期欄必須為 01 ~ 31
        ///
        /// ### 6. 儲存格內容格式檢查
        /// - 不可包含空白
        /// - 不可包含全形空白
        /// - 不可包含逗號
        /// - 不可包含頓號
        /// - 不可包含中括號
        /// - 不可包含換行
        /// - 不可包含 Tab
        ///
        /// ### 7. 簡名解析檢查
        /// - 一個字代表一位人
        /// - 同一格不可有重複簡名
        /// - 每個簡名都必須能找到 staff
        /// - 每個簡名必須唯一對應到一位 staff
        ///
        /// ### 8. 同日跨班別重複檢查
        /// - 同一天同一人不可出現在多個班別
        /// - 若重複，整份不匯入
        ///
        /// ---
        ///
        /// ## 🏗️ 建立規則
        ///
        /// ### 1. 建立表單
        /// 建立新的 DayOffScheduleFormClass，欄位初始值：
        /// - enable_weekoff_selection = false
        /// - enable_annualleave_selection = false
        /// - is_completed_locked = false
        ///
        /// ### 2. 建立日期
        /// 依 year_month 建立整個月份所有日期的 DayOffScheduleDayClass。
        /// 即使某天沒有排班，也會建立 day。
        ///
        /// ### 3. 建立 item
        /// 依 Excel 有填值的儲存格建立 DayOffScheduleItemClass。
        ///
        /// ### 4. is_special_day 規則
        /// 只要當天「國定假日 08:00-12:00」那格有人，
        /// 該日期所有 item 的 is_special_day = true。
        ///
        /// ### 5. position 規則
        /// 完全沿用既有 creat_form 邏輯：
        /// - 星期日
        /// - department = 門診 且 time 結束為 16:00 → position 依 index_opd 累加
        /// - department = 急診 且 time 結束為 16:00 → position 依 index_pher 累加
        ///
        /// ### 6. WorkShiftRequirementClass 建立規則
        /// day = 星期幾英文，例如 Monday / Sunday
        /// required_count = 1
        /// assigned_count = 1
        /// hdr = ""
        /// disabled = false
        ///
        /// shift_type / department mapping：
        ///
        /// | Excel 班別 | department | shift_type |
        /// |------|------|------|
        /// | 國定假日 | 國定假日 | holiday |
        /// | 假日門診 | 門診 | holiday |
        /// | 假日急診 | 急診 | holiday |
        /// | 化療 | 化療 | holiday |
        /// | TPN | TPN | holiday |
        /// | 中藥局 | 中藥局 | swing |
        /// | 小夜門診 | 門診 | swing |
        /// | 小夜急診 | 急診 | swing |
        /// | 小夜其他 | 其他 | swing |
        /// | 大夜門診 | 門診 | midnight |
        /// | 大夜急診 | 急診 | midnight |
        ///
        /// ---
        ///
        /// ## 📤 成功回傳 JSON 範例
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Method": "confirm_import_schedule_excel",
        ///   "Result": "匯入成功，建立表單(五月排班表)，建立日期(31)筆，建立排班項目(128)筆",
        ///   "Data": {
        ///     "GUID": "FORM_GUID",
        ///     "form_name": "五月排班表",
        ///     "days": [
        ///       {
        ///         "GUID": "DAY_GUID",
        ///         "date": "2026-05-01",
        ///         "items": [
        ///           {
        ///             "staff_id": "A001",
        ///             "staff_name": "王小明",
        ///             "staff_simple_name": "亭"
        ///           }
        ///         ]
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        ///
        /// ---
        ///
        /// ## ❌ 錯誤回傳 JSON 範例
        ///
        /// ### 表單名稱重複
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "confirm_import_schedule_excel",
        ///   "Result": "表單名稱(五月排班表)已建立過"
        /// }
        /// ```
        ///
        /// ### year_month 格式錯誤
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "confirm_import_schedule_excel",
        ///   "Result": "year_month 格式錯誤，需為 yyyy-MM"
        /// }
        /// ```
        ///
        /// ### 匯入驗證失敗
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "confirm_import_schedule_excel",
        ///   "Result": "匯入驗證失敗：同一天同一人不可出現在多個班別：亭，已出現在 假日門診 07:30-16:00"
        /// }
        /// ```
        ///
        /// ---
        ///
        /// ## 📌 注意事項
        /// - 本 API 會寫入資料庫。
        /// - 若任一格驗證失敗，整份不匯入。
        /// - 本 API 不會覆蓋既有表單，而是建立新表單。
        /// - form_name 不可重複。
        /// </remarks>
        /// <param name="file">上傳的 Excel 檔案（.xlsx）</param>
        /// <param name="form_name">新建立的表單名稱</param>
        /// <param name="year_month">年月，格式 yyyy-MM，例如 2026-05</param>
        /// <returns>成功時回傳建立完成的表單資料，失敗時回傳 JSON 錯誤訊息。</returns>
        [HttpPost("confirm_import_schedule_excel")]
        public string confirm_import_schedule_excel(IFormFile file, string form_name, string year_month)
        {
            var timer = new MyTimerBasic();
            returnData returnData = new returnData();
            returnData.Method = "confirm_import_schedule_excel";

            try
            {
                init(returnData);

                if (file == null || file.Length == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "未收到上傳檔案";
                    return returnData.JsonSerializationt();
                }

                string ext = Path.GetExtension(file.FileName)?.ToLower();
                if (ext != ".xlsx")
                {
                    returnData.Code = -200;
                    returnData.Result = "僅支援 .xlsx Excel 檔案";
                    return returnData.JsonSerializationt();
                }

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 form_name";
                    return returnData.JsonSerializationt();
                }

                DateTime ym;
                if (!DateTime.TryParse($"{year_month}-01", out ym))
                {
                    returnData.Code = -200;
                    returnData.Result = "year_month 格式錯誤，需為 yyyy-MM";
                    return returnData.JsonSerializationt();
                }

                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();

                if (sql_dayOffScheduleFormClass.GetRowsByDefult(null, "form_name", form_name).Count > 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"表單名稱({form_name})已建立過";
                    return returnData.JsonSerializationt();
                }

                List<ImportScheduleTemplateRow> templateRows = ImportScheduleTemplateDefinition.GetRows();

                List<StaffClass> staffClasses = staff.GetStaffs(new List<string>() { "pageSize=10000" }).staffClasses;
                if (staffClasses == null) staffClasses = new List<StaffClass>();

                Dictionary<string, List<StaffClass>> simpleNameMap =
                    ImportScheduleStaffHelper.BuildSimpleNameMap(staffClasses);

                Dictionary<string, string> dayStaffUsedMap = new Dictionary<string, string>();

                // key = yyyy-MM-dd，value = true/false 是否國定假日
                HashSet<string> specialDateSet = new HashSet<string>();

                // 暫存匯入解析結果
                Dictionary<string, List<ImportScheduleResolvedStaff>> importedData =
                    new Dictionary<string, List<ImportScheduleResolvedStaff>>();
                // key = yyyy-MM-dd|ShiftType|ShiftTime

                using (var stream = file.OpenReadStream())
                {
                    IWorkbook workbook = new XSSFWorkbook(stream);
                    ISheet sheet = workbook.GetSheetAt(0);

                    if (sheet == null)
                    {
                        returnData.Code = -200;
                        returnData.Result = "Excel 沒有可讀取的 Sheet";
                        return returnData.JsonSerializationt();
                    }

                    IRow headerRow = sheet.GetRow(0);
                    if (headerRow == null)
                    {
                        returnData.Code = -200;
                        returnData.Result = "Excel 標題列不存在";
                        return returnData.JsonSerializationt();
                    }

                    string headerA = ImportScheduleExcelHelper.GetCellString(headerRow.GetCell(0));
                    string headerB = ImportScheduleExcelHelper.GetCellString(headerRow.GetCell(1));

                    if (headerA != "班別類型" || headerB != "時段")
                    {
                        returnData.Code = -200;
                        returnData.Result = "Excel 標題格式錯誤，前兩欄必須為「班別類型 / 時段」";
                        return returnData.JsonSerializationt();
                    }

                    Dictionary<int, string> dateColumnMap =
                        ImportScheduleExcelHelper.BuildDateColumnMap(headerRow, 2, 32);

                    for (int rowIndex = 1; rowIndex <= templateRows.Count; rowIndex++)
                    {
                        IRow row = sheet.GetRow(rowIndex);
                        ImportScheduleTemplateRow expected = templateRows[rowIndex - 1];

                        if (row == null)
                        {
                            returnData.Code = -200;
                            returnData.Result = $"匯入驗證失敗：第 {rowIndex + 1} 列不存在，應為固定班別列：{expected.ShiftType} / {expected.ShiftTime}";
                            return returnData.JsonSerializationt();
                        }

                        string shiftType = ImportScheduleExcelHelper.GetCellString(row.GetCell(0));
                        string shiftTime = ImportScheduleExcelHelper.GetCellString(row.GetCell(1));

                        if (!ImportScheduleExcelHelper.IsTemplateRowMatched(shiftType, shiftTime, expected))
                        {
                            returnData.Code = -200;
                            returnData.Result = $"匯入驗證失敗：第 {rowIndex + 1} 列班別定義錯誤，應為「{expected.ShiftType} / {expected.ShiftTime}」";
                            return returnData.JsonSerializationt();
                        }

                        for (int col = 2; col <= 32; col++)
                        {
                            string rawText = ImportScheduleExcelHelper.GetCellString(row.GetCell(col));
                            if (rawText.StringIsEmpty()) continue;

                            string dayText = dateColumnMap.ContainsKey(col) ? dateColumnMap[col] : "";
                            if (dayText.StringIsEmpty() || !ImportScheduleExcelHelper.IsValidDayHeader(dayText))
                            {
                                returnData.Code = -200;
                                returnData.Result = $"匯入驗證失敗：第 1 列第 {col + 1} 欄日期標題錯誤，應為 01~31";
                                return returnData.JsonSerializationt();
                            }

                            DateTime targetDate = new DateTime(ym.Year, ym.Month, int.Parse(dayText));
                            string dateKey = targetDate.ToString("yyyy-MM-dd");

                            if (ImportScheduleExcelHelper.ContainsInvalidCharacters(rawText))
                            {
                                returnData.Code = -200;
                                returnData.Result = $"匯入驗證失敗：{dateKey} {shiftType} {shiftTime} 內容格式錯誤，不可包含空白、逗號、頓號、中括號或換行";
                                return returnData.JsonSerializationt();
                            }

                            List<string> simpleNames = ImportScheduleExcelHelper.ParseSimpleNames(rawText);
                            List<string> duplicatedSimpleNames = ImportScheduleExcelHelper.GetDuplicatedSimpleNames(simpleNames);

                            if (duplicatedSimpleNames.Count > 0)
                            {
                                returnData.Code = -200;
                                returnData.Result = $"匯入驗證失敗：{dateKey} {shiftType} {shiftTime} 同一格不可有重複簡名：{string.Join(",", duplicatedSimpleNames)}";
                                return returnData.JsonSerializationt();
                            }

                            ImportScheduleResolveResult resolveResult =
                                ImportScheduleStaffHelper.ResolveSimpleNames(simpleNameMap, simpleNames);

                            if (!resolveResult.IsSuccess)
                            {
                                returnData.Code = -200;
                                returnData.Result = $"匯入驗證失敗：{dateKey} {shiftType} {shiftTime} {resolveResult.ErrorMessage}";
                                return returnData.JsonSerializationt();
                            }

                            foreach (ImportScheduleResolvedStaff resolvedStaff in resolveResult.Staffs)
                            {
                                string currentShiftInfo = $"{shiftType} {shiftTime}";
                                bool ok = ImportScheduleStaffHelper.TryCheckAndRegisterDailyDuplicate(
                                    dayStaffUsedMap,
                                    dateKey,
                                    resolvedStaff,
                                    currentShiftInfo,
                                    out string duplicateError);

                                if (!ok)
                                {
                                    returnData.Code = -200;
                                    returnData.Result = $"匯入驗證失敗：{duplicateError}";
                                    return returnData.JsonSerializationt();
                                }
                            }

                            string importKey = $"{dateKey}|{shiftType}|{shiftTime}";
                            importedData[importKey] = resolveResult.Staffs;

                            // 國定假日該格有人 => 當天所有 item is_special_day=true
                            if (shiftType == "國定假日" && shiftTime == "08:00-12:00" && resolveResult.Staffs.Count > 0)
                            {
                                specialDateSet.Add(dateKey);
                            }
                        }
                    }
                }

                // ===== 建立 form =====
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

                List<DayOffScheduleDayClass> daysToAdd = new List<DayOffScheduleDayClass>();
                List<DayOffScheduleItemClass> itemsToAdd = new List<DayOffScheduleItemClass>();

                int daysInMonth = DateTime.DaysInMonth(ym.Year, ym.Month);

                for (int day = 1; day <= daysInMonth; day++)
                {
                    DateTime currentDate = new DateTime(ym.Year, ym.Month, day);
                    string dateKey = currentDate.ToString("yyyy-MM-dd");

                    DayOffScheduleDayClass dayOffScheduleDay = new DayOffScheduleDayClass()
                    {
                        GUID = Guid.NewGuid().ToString(),
                        form_guid = dayOffScheduleForm.GUID,
                        date = dateKey,
                        am_max_dayoff_count = "0",
                        pm_max_dayoff_count = "0",
                        created_at = DateTime.Now.ToDateTimeString(),
                        updated_at = DateTime.Now.ToDateTimeString(),
                        items = new List<DayOffScheduleItemClass>()
                    };

                    dayOffScheduleForm.days.Add(dayOffScheduleDay);
                    daysToAdd.Add(dayOffScheduleDay);

                    int index_opd = 0;
                    int index_pher = 0;

                    foreach (ImportScheduleTemplateRow templateRow in templateRows)
                    {
                        string importKey = $"{dateKey}|{templateRow.ShiftType}|{templateRow.ShiftTime}";
                        if (!importedData.ContainsKey(importKey)) continue;

                        List<ImportScheduleResolvedStaff> resolvedStaffs = importedData[importKey];
                        if (resolvedStaffs == null || resolvedStaffs.Count == 0) continue;

                        foreach (ImportScheduleResolvedStaff resolvedStaff in resolvedStaffs)
                        {
                            DayOffScheduleItemClass dayOffScheduleItem = new DayOffScheduleItemClass()
                            {
                                GUID = Guid.NewGuid().ToString(),
                                form_guid = dayOffScheduleForm.GUID,
                                day_guid = dayOffScheduleDay.GUID,
                                option_guid = "",
                                date = dateKey,
                                is_special_day = specialDateSet.Contains(dateKey).ToString().ToLower(),
                                staff_guid = resolvedStaff.GUID,
                                staff_id = resolvedStaff.staff_id,
                                staff_name = resolvedStaff.staff_name,
                                staff_simple_name = resolvedStaff.staff_simple_name,
                                selected_dayoff_type = "",
                                position = "",
                                created_at = DateTime.Now.ToDateTimeString(),
                                updated_at = DateTime.Now.ToDateTimeString()
                            };

                            WorkShiftRequirementClass requirement = BuildWorkShiftRequirementForImport(
                                currentDate,
                                templateRow.ShiftType,
                                templateRow.ShiftTime);

                            dayOffScheduleItem.workShiftRequirement = requirement;

                            // 沿用 creat_form 的週日 position 規則
                            if (currentDate.DayOfWeek == DayOfWeek.Sunday)
                            {
                                string endStr = requirement?.TimeRange?.end.ToString() ?? "";

                                if (requirement != null && requirement.department == "門診" && endStr.Contains("16:00"))
                                {
                                    dayOffScheduleItem.position = index_opd.ToString();
                                    index_opd++;
                                }
                                else if (requirement != null && requirement.department == "急診" && endStr.Contains("16:00"))
                                {
                                    dayOffScheduleItem.position = index_pher.ToString();
                                    index_pher++;
                                }
                            }

                            dayOffScheduleDay.items.Add(dayOffScheduleItem);
                            itemsToAdd.Add(dayOffScheduleItem);
                        }
                    }
                }

                // ===== 寫入資料庫 =====
                sql_dayOffScheduleFormClass.AddRow(null, dayOffScheduleForm.ClassToSQL<DayOffScheduleFormClass>());
                sql_dayOffScheduleDayClass.AddRows(null, daysToAdd.ClassToSQL<DayOffScheduleDayClass>());
                sql_dayOffScheduleItemClass.AddRows(null, itemsToAdd.ClassToSQL<DayOffScheduleItemClass>());

                returnData.Code = 200;
                returnData.Result = $"匯入成功，建立表單({dayOffScheduleForm.form_name})，建立日期({daysToAdd.Count})筆，建立排班項目({itemsToAdd.Count})筆";
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
        /// 建立匯入用 WorkShiftRequirementClass
        /// </summary>
        private WorkShiftRequirementClass BuildWorkShiftRequirementForImport(DateTime date, string shiftType, string shiftTime)
        {
            string department = "";
            string shiftTypeValue = "";

            switch (shiftType)
            {
                case "國定假日":
                    department = "國定假日";
                    shiftTypeValue = "holiday";
                    break;
                case "假日門診":
                    department = "門診";
                    shiftTypeValue = "holiday";
                    break;
                case "假日急診":
                    department = "急診";
                    shiftTypeValue = "holiday";
                    break;
                case "化療":
                    department = "化療";
                    shiftTypeValue = "holiday";
                    break;
                case "TPN":
                    department = "TPN";
                    shiftTypeValue = "holiday";
                    break;
                case "中藥局":
                    department = "中藥局";
                    shiftTypeValue = "swing";
                    break;
                case "小夜門診":
                    department = "門診";
                    shiftTypeValue = "swing";
                    break;
                case "小夜急診":
                    department = "急診";
                    shiftTypeValue = "swing";
                    break;
                case "小夜其他":
                    department = "其他";
                    shiftTypeValue = "swing";
                    break;
                case "大夜門診":
                    department = "門診";
                    shiftTypeValue = "midnight";
                    break;
                case "大夜急診":
                    department = "急診";
                    shiftTypeValue = "midnight";
                    break;
                default:
                    department = shiftType;
                    shiftTypeValue = "";
                    break;
            }

            return new WorkShiftRequirementClass
            {
                day = date.DayOfWeek.ToString(),
                time = shiftTime,
                shift_type = shiftTypeValue,
                required_count = "1",
                assigned_count = "1",
                department = department,
                hdr = "",
                disabled = false
            };
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
        /// 查詢單一人員於指定排休表單中的排休狀況摘要（公平機制版 get_staff_dayoff_status_summary）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL
        /// POST /phar_roster_api/dayOffSchedule/get_staff_dayoff_status_summary
        ///
        /// ## 📘 功能說明
        /// 查詢單一人員於指定排休表單中的：
        /// 1. 應休 / 已休 / 剩餘應休
        /// 2. 週六未排班次數
        /// 3. 任選假次數
        /// 4. 釋出全日 / 半日次數
        /// 5. 週六排休已使用次數與上限（公平機制）
        /// 6. 下午半日排休已使用次數與上限（公平機制）
        /// 7. 每日明細
        ///
        /// ## 同步最新規則
        /// 1. 選擇休假本身不影響應休
        /// 2. 應休只看：
        ///    - 每個週六未上班 +0.5
        ///    - is_any_date = true +1
        ///    - 釋出 FULL +1
        ///    - 釋出 HALF_AM / HALF_PM +0.5
        ///
        /// 3. 已休只看：
        ///    - selected_full = true → +1
        ///    - selected_half_am = true → +0.5
        ///    - selected_half_pm = true → +0.5
        ///
        /// 4. 公平機制統計：
        ///    - pm_selected_count 只算 is_quota_dayoff=true 且 quota_dayoff_type=WEEKDAY_HALF_PM
        ///    - weekend_selected_count 只算 is_quota_dayoff=true 且 quota_dayoff_type=SATURDAY_HALF_AM
        ///    - pm_selected_limit 預設 2
        ///    - weekend_selected_limit 預設 1；若有週六/週日補休來源（暫以週末釋出判定）則 +1
        /// </remarks>
        /// <param name="returnData">returnData 物件，主要使用 ValueAry 作為參數輸入。</param>
        /// <returns>回傳 JSON 字串。</returns>
        [HttpPost("get_staff_dayoff_status_summary")]
        public string get_staff_dayoff_status_summary([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "/phar_roster_api/dayOffSchedule/get_staff_dayoff_status_summary";

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry?
                    .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string staff_guid = GetVal("staff_guid");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 form_name";
                    return returnData.JsonSerializationt();
                }

                if (staff_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 staff_guid";
                    return returnData.JsonSerializationt();
                }

                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

                object[] obj_form = sql_dayOffScheduleFormClass
                    .GetRowsByDefult(null, "form_name", form_name)
                    .FirstOrDefault();

                if (obj_form == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();

                List<DayOffScheduleDayClass> days = sql_dayOffScheduleDayClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleDayClass>()
                    .OrderBy(x => x.date.StringToDateTime())
                    .ToList();

                List<DayOffScheduleItemClass> allItems = sql_dayOffScheduleItemClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleItemClass>();

                List<StaffDayOffOptionClass> allOptions = sql_staffDayOffOptionClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<StaffDayOffOptionClass>();

                List<DayOffScheduleItemClass> items = allItems
                    .Where(x => x.staff_guid == staff_guid)
                    .ToList();

                List<StaffDayOffOptionClass> options = allOptions
                    .Where(x => x.staff_guid == staff_guid)
                    .ToList();

                Dictionary<string, DayOffScheduleItemClass> itemByDate = items
                    .Where(x => x != null && x.date.StringIsEmpty() == false)
                    .GroupBy(x => x.date.StringToDateTime().ToDateString('-'))
                    .ToDictionary(g => g.Key, g => g.First());

                Dictionary<string, StaffDayOffOptionClass> optionByDate = options
                    .Where(x => x != null && x.date.StringIsEmpty() == false)
                    .GroupBy(x => x.date.StringToDateTime().ToDateString('-'))
                    .ToDictionary(g => g.Key, g => g.First());

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

                double quota = 0;
                double used = 0;

                int saturdayNoScheduleCount = 0;
                int anyDateCount = 0;
                int releaseFullCount = 0;
                int releaseHalfCount = 0;
                int weekendSelectedCount = 0;
                int weekendSelectedLimit = 1;
                int pmSelectedCount = 0;
                int pmSelectedLimit = 2;

                StaffDayoffStatusSummaryDto dto = new StaffDayoffStatusSummaryDto();
                dto.form_guid = form.GUID;
                dto.form_name = form.form_name;
                dto.staff_guid = staff_guid;

                var firstItem = items.FirstOrDefault();
                if (firstItem != null)
                {
                    dto.staff_id = firstItem.staff_id;
                    dto.staff_name = firstItem.staff_name;
                }

                // ============================================
                // 額外週六上限（簡化版）
                // 若有週六/週日釋出來源，週六上限 +1
                // ============================================
                foreach (var op in options)
                {
                    if (op == null) continue;

                    DateTime opDate = op.date.StringToDateTime();
                    if (opDate == DateTime.MinValue) continue;

                    if ((op.is_released == "true" || op.is_any_date == "true") &&
                        (opDate.DayOfWeek == DayOfWeek.Saturday || opDate.DayOfWeek == DayOfWeek.Sunday))
                    {
                        weekendSelectedLimit += 1;
                        break; // 目前規則只額外 +1 次
                    }
                }

                foreach (var day in days)
                {
                    string dt = day.date.StringToDateTime().ToDateString('-');

                    itemByDate.TryGetValue(dt, out var item);
                    optionByDate.TryGetValue(dt, out var option);

                    DateTime dayDate = day.date.StringToDateTime();
                    bool isSaturday = dayDate.DayOfWeek == DayOfWeek.Saturday;
                    bool isSunday = dayDate.DayOfWeek == DayOfWeek.Sunday;
                    bool isWeekend = isSaturday || isSunday;
                    bool hasSchedule = HasSchedule(item);

                    double quotaDelta = 0;
                    double usedDelta = 0;

                    string sourceType = "";
                    string isAnyDate = "false";
                    string selectedFull = "false";
                    string selectedHalfAm = "false";
                    string selectedHalfPm = "false";
                    string isReleased = "false";
                    string releasedDayoffType = "";
                    string isHoleFill = "false";
                    string isWeekendSelectedCounted = "false";
                    string isPmSelectedCounted = "false";

                    if (option != null)
                    {
                        option.NormalizeSelection();

                        sourceType = option.dayoff_source_type ?? "";
                        isAnyDate = option.is_any_date ?? "false";
                        selectedFull = option.selected_full ?? "false";
                        selectedHalfAm = option.selected_half_am ?? "false";
                        selectedHalfPm = option.selected_half_pm ?? "false";
                        isReleased = option.is_released ?? "false";
                        releasedDayoffType = option.released_dayoff_type ?? "";
                        isHoleFill = ((sourceType ?? "").Trim().ToUpper() == "HOLE_FILL") ? "true" : "false";

                        // 1. is_any_date = true → 應休 +1
                        if (option.is_any_date == "true")
                        {
                            quotaDelta += 1;
                            anyDateCount++;
                        }

                        // 2. 釋出 → 依 released_dayoff_type 計算應休
                        if (option.is_released == "true")
                        {
                            string releasedType = (option.released_dayoff_type ?? "").Trim().ToUpper();

                            if (releasedType == "FULL")
                            {
                                quotaDelta += 1;
                                releaseFullCount++;
                            }
                            else if (releasedType == "HALF_AM" || releasedType == "HALF_PM")
                            {
                                quotaDelta += 0.5;
                                releaseHalfCount++;
                            }
                        }

                        // 3. 已休只看選擇（修正 bug：selected_full 要算 usedDelta）
                        if (option.selected_full == "true")
                        {
                            if (option.is_force_ff != "true")
                            {
                                //usedDelta += 1;
                            }
                        }
                        else
                        {
                            if(option.dayoff_source_type != "RELEASED_SOURCE")
                            {
                                if (option.selected_half_am == "true") usedDelta += 0.5;
                                if (option.selected_half_pm == "true") usedDelta += 0.5;
                            }
                         
                        }

                        // 4. 公平機制：週六已使用次數
                        if (option.is_quota_dayoff == "true" &&
                            (option.quota_dayoff_type ?? "").Trim().ToUpper() == "SATURDAY_HALF_AM")
                        {
                            weekendSelectedCount++;
                            if (isSaturday) isWeekendSelectedCounted = "true";
                        }

                        // 5. 公平機制：下午半日已使用次數
                        if (option.is_quota_dayoff == "true" &&
                            (option.quota_dayoff_type ?? "").Trim().ToUpper() == "WEEKDAY_HALF_PM")
                        {
                            pmSelectedCount++;
                            isPmSelectedCounted = "true";
                        }
                    }

                    // 6. 每個週六未上班 → 應休 +0.5
                    if (isSaturday && !hasSchedule)
                    {
                        quotaDelta += 0.5;
                        saturdayNoScheduleCount++;
                    }

                    quota += quotaDelta;
                    used += usedDelta;

                    StaffDayoffDailyStatusDto daily = new StaffDayoffDailyStatusDto();
                    daily.date = dt;
                    daily.is_saturday = isSaturday ? "true" : "false";
                    daily.is_sunday = isSunday ? "true" : "false";
                    daily.is_weekend = isWeekend ? "true" : "false";
                    daily.has_schedule = hasSchedule ? "true" : "false";
                    daily.assigned_shift = item?.workShiftRequirement?.shift_type ?? "";
                    daily.dayoff_source_type = sourceType;
                    daily.is_any_date = isAnyDate;
                    daily.selected_full = selectedFull;
                    daily.selected_half_am = selectedHalfAm;
                    daily.selected_half_pm = selectedHalfPm;
                    daily.is_released = isReleased;
                    daily.released_dayoff_type = releasedDayoffType;
                    daily.is_hole_fill = isHoleFill;
                    daily.is_weekend_selected_counted = isWeekendSelectedCounted;
                    daily.is_pm_selected_counted = isPmSelectedCounted;
                    daily.quota_delta = quotaDelta.ToString("0.##");
                    daily.used_delta = usedDelta.ToString("0.##");

                    dto.daily_status.Add(daily);
                }

                dto.quota_dayoff = quota.ToString("0.##");
                dto.used_dayoff = used.ToString("0.##");
                dto.remaining_dayoff = (quota - used).ToString("0.##");
                dto.saturday_no_schedule_count = saturdayNoScheduleCount.ToString();
                dto.any_date_count = anyDateCount.ToString();
                dto.release_full_count = releaseFullCount.ToString();
                dto.release_half_count = releaseHalfCount.ToString();
                dto.weekend_selected_count = weekendSelectedCount.ToString();
                dto.weekend_selected_limit = weekendSelectedLimit.ToString();
                dto.pm_selected_count = pmSelectedCount.ToString();
                dto.pm_selected_limit = pmSelectedLimit.ToString();

                returnData.Code = 200;
                returnData.Result = "取得人員排休狀況成功";
                returnData.Data = dto;
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }

        /// <summary>
        /// 查詢指定表單中某一天的休假整體狀況（get_dayoff_date_status_summary）
        /// </summary>
        /// <remarks>
        /// ===============================
        /// 【API 說明】
        /// ===============================
        /// 本 API 用於依據指定表單名稱(form_name)與日期(date)，
        /// 查詢該日的整體休假狀況，包含：
        /// 1. 當日休假額度（上午 / 下午）
        /// 2. 當日有多少人及有誰釋出額度
        /// 3. 當日有多少人及有誰已選擇休假（上午 / 下午 / 整日）
        /// 4. 當日有多少人及有誰有預留休但尚未選擇
        ///
        ///
        /// ===============================
        /// 【主要用途】
        /// ===============================
        /// 1. 前端顯示某日休假全貌
        /// 2. 主管檢視當日排休 / 釋出 / 預留狀況
        /// 3. 作為當日排休檢視頁、看板或彈窗資料來源
        ///
        ///
        /// ===============================
        /// 【資料來源】
        /// ===============================
        /// 1. DayOffScheduleDayClass
        ///    - am_max_dayoff_count
        ///    - pm_max_dayoff_count
        ///
        /// 2. StaffDayOffOptionClass
        ///    - is_released
        ///    - released_dayoff_type
        ///    - selected_full
        ///    - selected_half_am
        ///    - selected_half_pm
        ///    - is_any_date
        ///    - suggested_dates_list
        ///    - dayoff_source_type
        ///
        /// 3. DayOffScheduleItemClass
        ///    - staff_guid
        ///    - staff_id
        ///    - staff_name
        ///
        ///
        /// ===============================
        /// 【統計規則】
        /// ===============================
        /// 一、當日休假額度
        /// - 上午休假額度 = DayOffScheduleDayClass.am_max_dayoff_count
        /// - 下午休假額度 = DayOffScheduleDayClass.pm_max_dayoff_count
        ///
        /// 二、當日釋出額度
        /// 符合以下條件者列入：
        /// - option.date = 指定日期
        /// - option.is_released = "true"
        ///
        /// 並依 released_dayoff_type 分類：
        /// - FULL
        /// - HALF_AM
        /// - HALF_PM
        ///
        /// 三、當日已選休假
        /// 符合以下條件者列入：
        /// - option.date = 指定日期
        /// - selected_full / selected_half_am / selected_half_pm 為 true
        ///
        /// 四、當日有預留休但尚未選擇
        /// 符合以下條件者列入：
        /// 1. option.date = 指定日期
        ///    或 suggested_dates_list 包含指定日期
        /// 2. option.dayoff_source_type != "HOLE_FILL"
        /// 3. option.selected_full != "true"
        /// 4. option.selected_half_am != "true"
        /// 5. option.selected_half_pm != "true"
        /// 6. option.is_released != "true"
        ///
        ///
        /// ===============================
        /// 【URL】
        /// ===============================
        /// POST /phar_roster_api/dayOffSchedule/get_dayoff_date_status_summary
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
        /// date      = 指定日期（必填，yyyy-MM-dd 或 yyyy-MM-dd HH:mm:ss）
        ///
        ///
        /// ===============================
        /// 【JSON 傳入範例】
        /// ===============================
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "date=2026-03-10"
        ///   ]
        /// }
        ///
        ///
        /// ===============================
        /// 【成功回傳 JSON 範例】
        /// ===============================
        /// {
        ///   "Code": 200,
        ///   "Method": "get_dayoff_date_status_summary",
        ///   "Result": "取得當日休假狀況成功",
        ///   "Data": {
        ///     "form_guid": "FORM_GUID_001",
        ///     "form_name": "2026年03月排休表",
        ///     "date": "2026-03-10",
        ///     "am_max_dayoff_count": "3",
        ///     "pm_max_dayoff_count": "3",
        ///     "selected_full_count": "1",
        ///     "selected_half_am_count": "2",
        ///     "selected_half_pm_count": "1",
        ///     "selected_total_count": "4",
        ///     "released_full_count": "0",
        ///     "released_half_am_count": "1",
        ///     "released_half_pm_count": "2",
        ///     "released_total_count": "3",
        ///     "reserved_not_selected_count": "5",
        ///     "released_list": [
        ///       {
        ///         "staff_guid": "STAFF_GUID_001",
        ///         "staff_id": "P001",
        ///         "staff_name": "王小明",
        ///         "status_type": "RELEASED",
        ///         "dayoff_type": "HALF_PM",
        ///         "is_any_date": "false",
        ///         "dayoff_source_type": "RELEASED_SOURCE",
        ///         "option_guid": "OPTION_GUID_001",
        ///         "item_guid": "ITEM_GUID_001"
        ///       }
        ///     ],
        ///     "selected_full_list": [],
        ///     "selected_half_am_list": [],
        ///     "selected_half_pm_list": [],
        ///     "reserved_not_selected_list": []
        ///   }
        /// }
        ///
        ///
        /// ===============================
        /// 【失敗回傳 JSON 範例】
        /// ===============================
        /// (1) 未提供參數
        /// {
        ///   "Code": -200,
        ///   "Method": "get_dayoff_date_status_summary",
        ///   "Result": "未提供 date",
        ///   "Data": null
        /// }
        ///
        /// (2) 找不到表單
        /// {
        ///   "Code": -200,
        ///   "Method": "get_dayoff_date_status_summary",
        ///   "Result": "找不到表單名稱(2026年03月排休表)",
        ///   "Data": null
        /// }
        ///
        /// (3) 找不到日期
        /// {
        ///   "Code": -200,
        ///   "Method": "get_dayoff_date_status_summary",
        ///   "Result": "找不到日期資料(2026-03-10)",
        ///   "Data": null
        /// }
        ///
        /// (4) 例外錯誤
        /// {
        ///   "Code": -500,
        ///   "Method": "get_dayoff_date_status_summary",
        ///   "Result": "Exception message ...",
        ///   "Data": null
        /// }
        /// </remarks>
        /// <param name="returnData">returnData 物件，主要使用 ValueAry 作為參數輸入。</param>
        /// <returns>回傳 JSON 字串。</returns>
        [HttpPost("get_dayoff_date_status_summary")]
        public string get_dayoff_date_status_summary([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_dayoff_date_status_summary";

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry?
                    .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string date = GetVal("date");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 form_name";
                    return returnData.JsonSerializationt();
                }

                if (date.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 date";
                    return returnData.JsonSerializationt();
                }

                DateTime targetDateTime = date.StringToDateTime();
                if (targetDateTime == DateTime.MinValue)
                {
                    returnData.Code = -200;
                    returnData.Result = $"日期格式錯誤({date})";
                    return returnData.JsonSerializationt();
                }

                string targetDate = targetDateTime.ToDateString('-');

                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

                object[] obj_form = sql_dayOffScheduleFormClass
                    .GetRowsByDefult(null, "form_name", form_name)
                    .FirstOrDefault();

                if (obj_form == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();

                DayOffScheduleDayClass day = sql_dayOffScheduleDayClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleDayClass>()
                    .Where(x => x.date.StringToDateTime().ToDateString('-') == targetDate)
                    .FirstOrDefault();

                if (day == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到日期資料({targetDate})";
                    return returnData.JsonSerializationt();
                }

                List<DayOffScheduleItemClass> items = sql_dayOffScheduleItemClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleItemClass>();

                List<StaffDayOffOptionClass> options = sql_staffDayOffOptionClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<StaffDayOffOptionClass>();

                // 方便從 option 找回 staff_id / staff_name
                Dictionary<string, DayOffScheduleItemClass> itemByGuid = items
                    .Where(x => x != null && x.GUID.StringIsEmpty() == false)
                    .GroupBy(x => x.GUID)
                    .ToDictionary(g => g.Key, g => g.First());

                Dictionary<string, DayOffScheduleItemClass> firstItemByStaffGuid = items
                    .Where(x => x != null && x.staff_guid.StringIsEmpty() == false)
                    .GroupBy(x => x.staff_guid)
                    .ToDictionary(g => g.Key, g => g.First());

                DayoffDatePersonStatusDto BuildPersonDto(StaffDayOffOptionClass option, string statusType, string dayoffType)
                {
                    DayOffScheduleItemClass item = null;

                    if (option != null && !option.item_guid.StringIsEmpty() && itemByGuid.ContainsKey(option.item_guid))
                    {
                        item = itemByGuid[option.item_guid];
                    }
                    else if (option != null && !option.staff_guid.StringIsEmpty() && firstItemByStaffGuid.ContainsKey(option.staff_guid))
                    {
                        item = firstItemByStaffGuid[option.staff_guid];
                    }

                    return new DayoffDatePersonStatusDto
                    {
                        staff_guid = option?.staff_guid ?? "",
                        staff_id = item?.staff_id ?? "",
                        staff_name = item?.staff_name ?? "",
                        status_type = statusType,
                        dayoff_type = dayoffType,
                        is_any_date = option?.is_any_date ?? "false",
                        dayoff_source_type = option?.dayoff_source_type ?? "",
                        option_guid = option?.GUID ?? "",
                        item_guid = option?.item_guid ?? ""
                    };
                }

                bool MatchTargetDate(StaffDayOffOptionClass option, string dt)
                {
                    if (option == null) return false;

                    if (option.date.StringToDateTime().ToDateString('-') == dt) return true;

                    if (option.suggested_dates_list != null &&
                        option.suggested_dates_list.Any(x => x.StringToDateTime().ToDateString('-') == dt))
                    {
                        return true;
                    }

                    return false;
                }

                DayoffDateStatusSummaryDto dto = new DayoffDateStatusSummaryDto();
                dto.form_guid = form.GUID;
                dto.form_name = form.form_name;
                dto.date = targetDate;
                dto.am_max_dayoff_count = day.am_max_dayoff_count ?? "0";
                dto.pm_max_dayoff_count = day.pm_max_dayoff_count ?? "0";
                if (dto.reserved_not_selected_count.StringIsEmpty()) dto.reserved_not_selected_count = "0";
                if (dto.selected_full_count.StringIsEmpty()) dto.selected_full_count = "0";
                if (dto.selected_half_am_count.StringIsEmpty()) dto.selected_half_am_count = "0";
                if (dto.selected_half_pm_count.StringIsEmpty()) dto.selected_half_pm_count = "0";

                if (dto.released_full_count.StringIsEmpty()) dto.released_full_count = "0";
                if (dto.released_half_am_count.StringIsEmpty()) dto.released_half_am_count = "0";
                if (dto.released_half_pm_count.StringIsEmpty()) dto.released_half_pm_count = "0";
                foreach (var option in options)
                {
                    if (option == null) continue;

                    option.NormalizeSelection();

                    bool matchMainDate = option.date.StringToDateTime().ToDateString('-') == targetDate;
                    bool matchReserveDate = MatchTargetDate(option, targetDate);

                    // =========================
                    // 1. 釋出名單
                    // =========================
                    if (matchMainDate && option.is_released == "true")
                    {
                        string releasedType = (option.released_dayoff_type ?? "").Trim().ToUpper();

                        if (releasedType == "FULL")
                        {
                            dto.released_full_count = (dto.released_full_count.StringToInt32() + 1).ToString();
                            dto.released_list.Add(BuildPersonDto(option, "RELEASED", "FULL"));
                        }
                        else if (releasedType == "HALF_AM")
                        {
                            dto.released_half_am_count = (dto.released_half_am_count.StringToInt32() + 1).ToString();
                            dto.released_list.Add(BuildPersonDto(option, "RELEASED", "HALF_AM"));
                        }
                        else if (releasedType == "HALF_PM")
                        {
                            dto.released_half_pm_count = (dto.released_half_pm_count.StringToInt32() + 1).ToString();
                            dto.released_list.Add(BuildPersonDto(option, "RELEASED", "HALF_PM"));
                        }
                    }

                    // =========================
                    // 2. 已選休假名單
                    // =========================
                    if (matchMainDate && option.selected_full == "true")
                    {
                        dto.selected_full_count = (dto.selected_full_count.StringToInt32() + 1).ToString();
                        dto.selected_full_list.Add(BuildPersonDto(option, "SELECTED", "FULL"));
                    }

                    if (matchMainDate && option.selected_half_am == "true")
                    {
                        dto.selected_half_am_count = (dto.selected_half_am_count.StringToInt32() + 1).ToString();
                        dto.selected_half_am_list.Add(BuildPersonDto(option, "SELECTED", "HALF_AM"));
                    }

                    if (matchMainDate && option.selected_half_pm == "true")
                    {
                        dto.selected_half_pm_count = (dto.selected_half_pm_count.StringToInt32() + 1).ToString();
                        dto.selected_half_pm_list.Add(BuildPersonDto(option, "SELECTED", "HALF_PM"));
                    }

                    // =========================
                    // 3. 預留休未選名單
                    // =========================
                    bool noSelection =
                        option.selected_full != "true" &&
                        option.selected_half_am != "true" &&
                        option.selected_half_pm != "true";

                    bool isReserved =
                        (option.dayoff_source_type ?? "").Trim().ToUpper() != "HOLE_FILL";

                    bool notReleased = option.is_released != "true";

                    if (matchReserveDate && noSelection && isReserved && notReleased)
                    {
              
                        dto.reserved_not_selected_count = (dto.reserved_not_selected_count.StringToInt32() + 1).ToString();
                        dto.reserved_not_selected_list.Add(BuildPersonDto(option, "RESERVED_NOT_SELECTED", "NONE"));
                    }
                }

                dto.selected_total_count =
                    (dto.selected_full_count.StringToInt32() +
                     dto.selected_half_am_count.StringToInt32() +
                     dto.selected_half_pm_count.StringToInt32()).ToString();

                dto.released_total_count =
                    (dto.released_full_count.StringToInt32() +
                     dto.released_half_am_count.StringToInt32() +
                     dto.released_half_pm_count.StringToInt32()).ToString();

                returnData.Code = 200;
                returnData.Result = "取得當日休假狀況成功";
                returnData.Data = dto;
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
            finally
            {
                returnData.TimeTaken = timer.ToString();
            }
        }

        /// <summary>
        /// 統一處理休假選擇與釋出控制的操作入口（最終版 set_staff_dayoff_and_release_action）
        /// </summary>
        /// <remarks>
        /// ===============================
        /// ✅ 功能說明
        /// ===============================
        /// 本 API 為前端統一操作入口，用於整合：
        /// 1. 選擇整日休假
        /// 2. 選擇上午半日休假
        /// 3. 選擇下午半日休假
        /// 4. 取消休假選擇
        /// 5. 手動整日釋出
        /// 6. 取消釋出
        ///
        /// 前端只需呼叫本 API，依 action_type 分派到對應內部功能。
        ///
        ///
        /// ===============================
        /// ✅ action_type 定義
        /// ===============================
        /// - SELECT_FULL
        /// - SELECT_HALF_AM
        /// - SELECT_HALF_PM
        /// - CANCEL_SELECTION
        /// - RELEASE_FULL
        /// - CANCEL_RELEASE
        ///
        ///
        /// ===============================
        /// ✅ 規則說明
        /// ===============================
        /// 1. SELECT_FULL
        ///    - 呼叫 set_staff_dayoff_selection(select_type=FULL)
        ///
        /// 2. SELECT_HALF_AM
        ///    - 呼叫 set_staff_dayoff_selection(select_type=HALF_AM)
        ///    - 後端會自動釋出 HALF_PM
        ///
        /// 3. SELECT_HALF_PM
        ///    - 呼叫 set_staff_dayoff_selection(select_type=HALF_PM)
        ///    - 後端會自動釋出 HALF_AM
        ///
        /// 4. CANCEL_SELECTION
        ///    - 呼叫 set_staff_dayoff_selection(select_type=CANCEL)
        ///
        /// 5. RELEASE_FULL
        ///    - 呼叫 release_dayoff_option(release_dayoff_type=FULL)
        ///
        /// 6. CANCEL_RELEASE
        ///    - 呼叫 cancel_release_dayoff_option()
        ///
        ///
        /// ===============================
        /// ✅ 業務限制
        /// ===============================
        /// - 不允許「完全不選休，直接釋出半日」
        /// - 半日釋出只能由 SELECT_HALF_AM / SELECT_HALF_PM 自動產生
        /// - 若要手動開洞，只允許整日釋出（RELEASE_FULL）
        ///
        ///
        /// ===============================
        /// ✅ 參數（returnData.ValueAry）
        /// ===============================
        /// - form_name   : 表單名稱（必填）
        /// - option_guid : option GUID（必填）
        /// - action_type : 操作類型（必填）
        /// - staff_id    : 選休假 / 取消休假時必填
        /// - off_date    : SELECT_FULL / SELECT_HALF_AM / SELECT_HALF_PM 時必填
        ///
        ///
        /// ===============================
        /// ✅ Request JSON 範例
        /// ===============================
        /// (1) 選整日
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "staff_id=A12345",
        ///     "option_guid=OPTION_GUID_001",
        ///     "action_type=SELECT_FULL",
        ///     "off_date=2026-03-05"
        ///   ]
        /// }
        ///
        /// (2) 選上午
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "staff_id=A12345",
        ///     "option_guid=OPTION_GUID_001",
        ///     "action_type=SELECT_HALF_AM",
        ///     "off_date=2026-03-05"
        ///   ]
        /// }
        ///
        /// (3) 取消休假
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "staff_id=A12345",
        ///     "option_guid=OPTION_GUID_001",
        ///     "action_type=CANCEL_SELECTION"
        ///   ]
        /// }
        ///
        /// (4) 手動整日釋出
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "option_guid=OPTION_GUID_001",
        ///     "action_type=RELEASE_FULL"
        ///   ]
        /// }
        ///
        /// (5) 取消釋出
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "option_guid=OPTION_GUID_001",
        ///     "action_type=CANCEL_RELEASE"
        ///   ]
        /// }
        /// </remarks>
        /// <param name="returnData">通用傳入物件（ValueAry 帶參數）</param>
        /// <returns>序列化後的 returnData JSON 字串</returns>
        [HttpPost("set_staff_dayoff_and_release_action")]
        public string set_staff_dayoff_and_release_action([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "set_staff_dayoff_and_release_action";

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry?
                        .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                        ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string staff_id = GetVal("staff_id");
                string option_guid = GetVal("option_guid");
                string action_type = GetVal("action_type");
                string off_date = GetVal("off_date");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 form_name";
                    return returnData.JsonSerializationt();
                }
                if (option_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 option_guid";
                    return returnData.JsonSerializationt();
                }
                if (action_type.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 action_type";
                    return returnData.JsonSerializationt();
                }

                action_type = action_type.Trim().ToUpperInvariant();

                HashSet<string> validActions = new HashSet<string>()
        {
            "SELECT_FULL",
            "SELECT_HALF_AM",
            "SELECT_HALF_PM",
            "CANCEL_SELECTION",
            "RELEASE_FULL",
            "CANCEL_RELEASE"
        };

                if (!validActions.Contains(action_type))
                {
                    returnData.Code = -200;
                    returnData.Result = "action_type 必須為 SELECT_FULL / SELECT_HALF_AM / SELECT_HALF_PM / CANCEL_SELECTION / RELEASE_FULL / CANCEL_RELEASE";
                    return returnData.JsonSerializationt();
                }

                // ============================
                // 選擇休假相關：必須要 staff_id
                // ============================
                if (action_type == "SELECT_FULL" ||
                    action_type == "SELECT_HALF_AM" ||
                    action_type == "SELECT_HALF_PM" ||
                    action_type == "CANCEL_SELECTION")
                {
                    if (staff_id.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "此操作必須提供 staff_id";
                        return returnData.JsonSerializationt();
                    }
                }

                // ============================
                // 選擇休假：必須 off_date
                // ============================
                if (action_type == "SELECT_FULL" ||
                    action_type == "SELECT_HALF_AM" ||
                    action_type == "SELECT_HALF_PM")
                {
                    if (off_date.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "此操作必須提供 off_date";
                        return returnData.JsonSerializationt();
                    }
                }

                returnData innerReturnData = new returnData();
                innerReturnData.ValueAry = new List<string>();

                void AddVal(string key, string value)
                {
                    if (value.StringIsEmpty()) return;
                    innerReturnData.ValueAry.Add($"{key}={value}");
                }

                AddVal("form_name", form_name);
                AddVal("staff_id", staff_id);
                AddVal("option_guid", option_guid);
                AddVal("off_date", off_date);

                string json;

                if (action_type == "SELECT_FULL")
                {
                    AddVal("select_type", "FULL");
                    json = set_staff_dayoff_selection(innerReturnData);
                }
                else if (action_type == "SELECT_HALF_AM")
                {
                    AddVal("select_type", "HALF_AM");
                    json = set_staff_dayoff_selection(innerReturnData);
                }
                else if (action_type == "SELECT_HALF_PM")
                {
                    AddVal("select_type", "HALF_PM");
                    json = set_staff_dayoff_selection(innerReturnData);
                }
                else if (action_type == "CANCEL_SELECTION")
                {
                    AddVal("select_type", "CANCEL");
                    cancel_release_dayoff_option(innerReturnData);
                    AddVal("release_dayoff_type", "FULL");
                    json = set_staff_dayoff_selection(innerReturnData);

                }
                else if (action_type == "RELEASE_FULL")
                {
                    AddVal("release_dayoff_type", "FULL");
                    json = release_dayoff_option(innerReturnData);
                }
                else // CANCEL_RELEASE
                {
                    json = cancel_release_dayoff_option(innerReturnData);
                }

                return json;
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
        /// 儲存登入者單筆放假選擇（升級版 set_staff_dayoff_selection）
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
        /// 本升級版已同步最新規則：
        /// 1. 休假選擇與釋出規則同步
        /// 2. FULL：不可保留整日釋出
        /// 3. HALF_AM：自動確保對側 HALF_PM 釋出存在
        /// 4. HALF_PM：自動確保對側 HALF_AM 釋出存在
        /// 5. CANCEL：僅清空休假選擇，不自動取消釋出
        ///
        ///
        /// ===============================
        /// ✅ 功能內容
        /// ===============================
        /// 1) 驗證 form / staff / option / group 權限
        /// 2) 驗證 is_force_ff / is_forbidden
        /// 3) 儲存休假選擇
        /// 4) 同步建立 / 關閉釋出池 DayOffReleasePoolClass
        /// 5) 寫入 staff_dayoff_option_log
        ///
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
        ///
        /// ===============================
        /// ✅ 規則說明
        /// ===============================
        /// - FULL：
        ///   - 設定整日休假
        ///   - 若存在未被接手的 OPEN 釋出池，會自動關閉
        ///   - 若已有被接手之 pool，禁止改為 FULL
        ///
        /// - HALF_AM：
        ///   - 設定上午休假
        ///   - 自動建立 / 保留 HALF_PM 的釋出池
        ///   - 若原本存在不同方向的 OPEN pool，且未被接手，則自動關閉並重建
        ///   - 若原本 pool 已被接手，禁止改方向
        ///
        /// - HALF_PM：
        ///   - 設定下午休假
        ///   - 自動建立 / 保留 HALF_AM 的釋出池
        ///
        /// - CANCEL：
        ///   - 只清空 selected_full / selected_half_am / selected_half_pm
        ///   - 不自動取消既有釋出
        ///
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
        /// (2) 選上午（會自動釋出下午）
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "staff_id=A12345",
        ///     "option_guid=OPTION_GUID_001",
        ///     "select_type=HALF_AM",
        ///     "off_date=2026-03-05"
        ///   ]
        /// }
        ///
        /// (3) 取消（不需 off_date）
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
                    off_date = dt.ToDateString('-');
                }

                // ===============================
                // 1) SQL Controls
                // ===============================
                var sql_form = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_staff = MethodClass.GetSQLControl<StaffClass>();
                var sql_group = MethodClass.GetSQLControl<DayOffGroupClass>();
                var sql_member = MethodClass.GetSQLControl<DayOffGroupMemberClass>();
                var sql_option = MethodClass.GetSQLControl<StaffDayOffOptionClass>();
                var sql_log = MethodClass.GetSQLControl<StaffDayOffOptionLogClass>();
                var sql_releasePool = MethodClass.GetSQLControl<DayOffReleasePoolClass>();

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
                // 5) 流程檢核
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
                // 6) option 狀態檢核
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
                // 7) 快照
                // ===============================
                string before_date = option.date ?? "";
                string before_selected_full = option.selected_full ?? "false";
                string before_selected_half_am = option.selected_half_am ?? "false";
                string before_selected_half_pm = option.selected_half_pm ?? "false";
                string before_is_released = option.is_released ?? "false";
                string before_released_dayoff_type = option.released_dayoff_type ?? "";
                string before_release_pool_guid = option.release_pool_guid ?? "";

                string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // ===============================
                // 共用：抓目前 option 對應 pool
                // ===============================
                DayOffReleasePoolClass currentPool = null;
                if (!option.release_pool_guid.StringIsEmpty())
                {
                    currentPool = sql_releasePool
                        .GetRowsByDefult(null, "GUID", option.release_pool_guid)
                        .SQLToClass<DayOffReleasePoolClass>()
                        .FirstOrDefault();
                }

                List<DayOffReleasePoolClass> optionPools = sql_releasePool
                    .GetRowsByDefult(null, "source_option_guid", option.GUID)
                    .SQLToClass<DayOffReleasePoolClass>() ?? new List<DayOffReleasePoolClass>();

                DayOffReleasePoolClass openPool = optionPools
                    .Where(x => x.status == "OPEN")
                    .OrderByDescending(x => x.created_at.StringToDateTime())
                    .FirstOrDefault();

                bool PoolHasClaimed(DayOffReleasePoolClass pool)
                {
                    if (pool == null) return false;
                    return pool.claimed_slots.StringToInt32() > 0;
                }

                void ClosePool(DayOffReleasePoolClass pool)
                {
                    if (pool == null) return;
                    pool.status = "CANCELLED";
                    pool.updated_at = now;
                    pool.version_no = (pool.version_no.StringToInt32() + 1).ToString();
                    sql_releasePool.UpdateByDefulteExtra(null, pool.ClassToSQL<DayOffReleasePoolClass>());
                }

                DayOffReleasePoolClass CreatePool(string releaseType)
                {
                    DayOffReleasePoolClass pool = new DayOffReleasePoolClass();
                    pool.GUID = Guid.NewGuid().ToString();
                    pool.form_guid = option.form_guid;
                    pool.source_option_guid = option.GUID;
                    pool.source_item_guid = option.item_guid;
                    pool.source_staff_guid = option.staff_guid;
                    pool.date = option.date;
                    pool.release_dayoff_type = releaseType;
                    pool.total_slots = "1";
                    pool.claimed_slots = "0";
                    pool.remaining_slots = "1";
                    pool.status = "OPEN";
                    pool.version_no = "1";
                    pool.created_at = now;
                    pool.updated_at = now;

                    sql_releasePool.AddRows(null, new List<object[]>() { pool.ClassToSQL<DayOffReleasePoolClass>() });
                    return pool;
                }

                void EnsurePool(string releaseType)
                {
                    // 已有同方向 OPEN pool -> 直接沿用
                    if (openPool != null &&
                        string.Equals(openPool.release_dayoff_type, releaseType, StringComparison.OrdinalIgnoreCase) &&
                        openPool.status == "OPEN")
                    {
                        option.is_released = "true";
                        option.released_at = now;
                        option.released_dayoff_type = releaseType;
                        option.release_pool_guid = openPool.GUID;
                        option.dayoff_source_type = "RELEASED_SOURCE";
                        return;
                    }

                    // 若有不同方向 OPEN pool
                    if (openPool != null &&
                        !string.Equals(openPool.release_dayoff_type, releaseType, StringComparison.OrdinalIgnoreCase))
                    {
                        if (PoolHasClaimed(openPool))
                        {
                            throw new Exception($"目前已有已被接手的釋出池({openPool.release_dayoff_type})，不可改變休假方向");
                        }

                        ClosePool(openPool);
                    }

                    DayOffReleasePoolClass newPool = CreatePool(releaseType);

                    option.is_released = "true";
                    option.released_at = now;
                    option.released_dayoff_type = releaseType;
                    option.release_pool_guid = newPool.GUID;
                    option.dayoff_source_type = "RELEASED_SOURCE";
                }

                void ClearReleaseIfPossible()
                {
                    if (openPool != null)
                    {
                        if (PoolHasClaimed(openPool))
                        {
                            throw new Exception("目前已有已被接手的釋出池，不可改為整日休假");
                        }

                        ClosePool(openPool);
                    }

                    option.is_released = "false";
                    option.released_at = DateTime.MinValue.ToDateTimeString();
                    option.released_dayoff_type = "";
                    option.release_pool_guid = "";
                    if ((option.dayoff_source_type ?? "").Trim().ToUpper() == "RELEASED_SOURCE")
                    {
                        option.dayoff_source_type = "";
                    }
                }

                // ===============================
                // 8) 寫入選擇 + 同步釋出
                // ===============================
                if (isCancel)
                {
                    // 取消只清空選擇，不動釋出
                    option.ClearSelection();
                    option.updated_at = now;
                    option.NormalizeSelection();
                }
                else
                {
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

                    lock (_dayoffReleasePoolLock)
                    {
                        if (select_type == "FULL")
                        {
                            // FULL：不可保留 OPEN 釋出池
                            ClearReleaseIfPossible();
                        }
                        else if (select_type == "HALF_AM")
                        {
                            // 上午休 -> 自動釋出下午
                            EnsurePool("HALF_PM");
                        }
                        else if (select_type == "HALF_PM")
                        {
                            // 下午休 -> 自動釋出上午
                            EnsurePool("HALF_AM");
                        }
                    }

                    option.updated_at = now;
                    option.NormalizeSelection();
                }

                // ===============================
                // 9) 更新 DB
                // ===============================
                sql_option.UpdateByDefulteExtra(null, new List<object[]>
        {
            option.ClassToSQL<StaffDayOffOptionClass>()
        });

                // ===============================
                // 10) 寫入 Log
                // ===============================
                try
                {
                    var log = new StaffDayOffOptionLogClass();

                    log.GUID = Guid.NewGuid().ToString();
                    log.form_guid = form_guid;
                    log.option_guid = option.GUID;
                    log.staff_guid = staff_guid;
                    log.staff_id = staff_id;
                    log.stage = stage;
                    log.action = select_type;
                    log.off_date = (select_type == "CANCEL") ? before_date : (option.date ?? "");

                    // 若你的 log class 沒有這些欄位，請刪掉
                    log.before_selected_full = before_selected_full;
                    log.before_selected_half_am = before_selected_half_am;
                    log.before_selected_half_pm = before_selected_half_pm;

                    log.after_selected_full = option.selected_full ?? "false";
                    log.after_selected_half_am = option.selected_half_am ?? "false";
                    log.after_selected_half_pm = option.selected_half_pm ?? "false";

                    //// 若你的 log class 沒有這些欄位，請刪掉
                    //log.before_is_released = before_is_released;
                    //log.before_released_dayoff_type = before_released_dayoff_type;
                    //log.before_release_pool_guid = before_release_pool_guid;

                    //log.after_is_released = option.is_released ?? "false";
                    //log.after_released_dayoff_type = option.released_dayoff_type ?? "";
                    //log.after_release_pool_guid = option.release_pool_guid ?? "";

                    log.created_at = now;

                    sql_log.AddRows(null, new List<object[]>
            {
                log.ClassToSQL<StaffDayOffOptionLogClass>()
            });
                }
                catch
                {
                    // log 失敗不阻擋主流程
                }

                // ===============================
                // 11) 回傳
                // ===============================
                DayOffReleasePoolClass poolAfter = null;
                if (!option.release_pool_guid.StringIsEmpty())
                {
                    poolAfter = sql_releasePool
                        .GetRowsByDefult(null, "GUID", option.release_pool_guid)
                        .SQLToClass<DayOffReleasePoolClass>()
                        .FirstOrDefault();
                }

                returnData.Code = 200;
                returnData.Result = "success";
                returnData.Data = new
                {
                    option,
                    release_pool = poolAfter
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
        /// 使用應休額度進行排休選擇（含公平機制與當日名額限制版 set_staff_quota_dayoff_selection）
        /// </summary>
        /// <remarks>
        /// ===============================
        /// 【API 說明】
        /// ===============================
        /// 本 API 用於讓單一人員使用自己的應休額度進行排休。
        ///
        /// 本版已同時加入：
        /// 1. 個人應休總額度限制
        /// 2. 下午半日次數限制
        /// 3. 週六排休次數限制
        /// 4. 當日 AM / PM 休假名額上限限制
        ///
        ///
        /// ===============================
        /// 【功能用途】
        /// ===============================
        /// 1. 平日整日休假
        /// 2. 平日上午半日休假
        /// 3. 平日下午半日休假
        /// 4. 週六上午半日休假
        /// 5. 週日整日休假
        /// 6. 取消已選的應休排休
        ///
        ///
        /// ===============================
        /// 【排休規則】
        /// ===============================
        /// 一、平日休假
        /// - FULL      → 消耗 1 日應休額度
        /// - HALF_AM   → 消耗 0.5 日應休額度
        /// - HALF_PM   → 消耗 0.5 日應休額度
        ///
        /// 二、週六休假
        /// - 只允許 HALF_AM
        /// - 消耗 0.5 日應休額度
        /// - 不允許 FULL
        /// - 不允許 HALF_PM
        ///
        /// 三、週日休假
        /// - 只允許 FULL
        /// - 消耗 1 日應休額度
        /// - 不允許 HALF_AM
        /// - 不允許 HALF_PM
        ///
        /// 四、CANCEL
        /// - 取消該筆應休排休
        /// - 清空 selected_full / selected_half_am / selected_half_pm
        /// - is_quota_dayoff = false
        /// - quota_used = 0
        /// - quota_dayoff_type = 空字串
        ///
        ///
        /// ===============================
        /// 【公平機制限制】
        /// ===============================
        /// 一、下午半日排休限制
        /// - 只統計 is_quota_dayoff = true 且 quota_dayoff_type = WEEKDAY_HALF_PM
        /// - 每人最多 2 次
        ///
        /// 二、週六排休限制
        /// - 只統計 is_quota_dayoff = true 且 quota_dayoff_type = SATURDAY_HALF_AM
        /// - 每人預設最多 1 次
        /// - 若有額外補休來源（目前簡化版：存在週六/週日釋出來源），則上限 +1 次
        ///
        /// 三、總額度限制
        /// - 已使用應休額度 + 本次消耗
        /// - 不可超過該人員的總應休額度
        ///
        ///
        /// ===============================
        /// 【當日名額限制】
        /// ===============================
        /// 本 API 除了檢查個人額度與公平機制，
        /// 還會檢查該日整體休假名額上限：
        ///
        /// 1. 若本次選 FULL：
        ///    - 需同時檢查上午名額與下午名額
        ///    - 上午不可超過 am_max_dayoff_count
        ///    - 下午不可超過 pm_max_dayoff_count
        ///
        /// 2. 若本次選 HALF_AM：
        ///    - 需檢查上午名額
        ///    - 上午不可超過 am_max_dayoff_count
        ///
        /// 3. 若本次選 HALF_PM：
        ///    - 需檢查下午名額
        ///    - 下午不可超過 pm_max_dayoff_count
        ///
        ///
        /// ===============================
        /// 【名額占用規則】
        /// ===============================
        /// 一、上午名額占用
        /// 符合以下任一即占用上午名額：
        /// - selected_full = true
        /// - selected_half_am = true
        ///
        /// 二、下午名額占用
        /// 符合以下任一即占用下午名額：
        /// - selected_full = true
        /// - selected_half_pm = true
        ///
        /// 三、名額統計以整張表單所有人為準
        /// 並非只計算單一人員
        ///
        /// 四、若本次為修改同一筆 option
        /// 系統會先扣除該筆原本已占用的名額，再檢查新選擇是否超限
        ///
        ///
        /// ===============================
        /// 【不可操作的 option】
        /// ===============================
        /// 以下 option 不可用於應休排休：
        /// 1. 系統強制放假（is_force_ff = true）
        /// 2. 被禁止操作（is_forbidden = true）
        /// 3. 填洞休假（dayoff_source_type = HOLE_FILL）
        /// 4. 已釋出中的 option（is_released = true）
        ///
        ///
        /// ===============================
        /// 【quota_dayoff_type 定義】
        /// ===============================
        /// - WEEKDAY_FULL
        /// - WEEKDAY_HALF_AM
        /// - WEEKDAY_HALF_PM
        /// - SATURDAY_HALF_AM
        /// - SUNDAY_FULL
        ///
        ///
        /// ===============================
        /// 【URL】
        /// ===============================
        /// POST /phar_roster_api/dayOffSchedule/set_staff_quota_dayoff_selection
        ///
        /// ===============================
        /// 【Method】
        /// ===============================
        /// POST
        ///
        /// ===============================
        /// 【傳入參數】(ValueAry)
        /// ===============================
        /// form_name   = 表單名稱（必填）
        /// staff_id    = 人員工號（必填）
        /// option_guid = option GUID（必填）
        /// select_type = FULL / HALF_AM / HALF_PM / CANCEL（必填）
        /// off_date    = 日期（選擇 FULL / HALF_AM / HALF_PM 時必填）
        ///
        ///
        /// ===============================
        /// 【JSON 傳入範例】
        /// ===============================
        /// (1) 平日整日休假
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "staff_id=A12345",
        ///     "option_guid=OPTION_GUID_001",
        ///     "select_type=FULL",
        ///     "off_date=2026-03-12"
        ///   ]
        /// }
        ///
        /// (2) 平日下午半日休假
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "staff_id=A12345",
        ///     "option_guid=OPTION_GUID_002",
        ///     "select_type=HALF_PM",
        ///     "off_date=2026-03-11"
        ///   ]
        /// }
        ///
        /// (3) 週六上午半日休假
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "staff_id=A12345",
        ///     "option_guid=OPTION_GUID_003",
        ///     "select_type=HALF_AM",
        ///     "off_date=2026-03-14"
        ///   ]
        /// }
        ///
        /// (4) 取消應休排休
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "staff_id=A12345",
        ///     "option_guid=OPTION_GUID_001",
        ///     "select_type=CANCEL"
        ///   ]
        /// }
        ///
        ///
        /// ===============================
        /// 【成功回傳 JSON 範例】
        /// ===============================
        /// {
        ///   "Code": 200,
        ///   "Method": "set_staff_quota_dayoff_selection",
        ///   "Result": "success",
        ///   "Data": {
        ///     "option": {
        ///       "GUID": "OPTION_GUID_001",
        ///       "staff_guid": "STAFF_GUID_001",
        ///       "selected_full": "true",
        ///       "selected_half_am": "false",
        ///       "selected_half_pm": "false",
        ///       "is_quota_dayoff": "true",
        ///       "quota_used": "1",
        ///       "quota_dayoff_type": "WEEKDAY_FULL"
        ///     },
        ///     "quota_summary": {
        ///       "staff_guid": "STAFF_GUID_001",
        ///       "quota_total": "5.5",
        ///       "quota_used_total": "2",
        ///       "quota_remaining": "3.5"
        ///     },
        ///     "rule_summary": {
        ///       "quota_total": "5.5",
        ///       "quota_used_total": "2",
        ///       "quota_remaining": "3.5",
        ///       "pm_half_used_count": "1",
        ///       "saturday_used_count": "1",
        ///       "pm_half_limit_count": "2",
        ///       "saturday_limit_count": "2",
        ///       "has_extra_saturday_limit": "true"
        ///     },
        ///     "date_quota_summary": {
        ///       "date": "2026-03-12",
        ///       "am_max_dayoff_count": "3",
        ///       "pm_max_dayoff_count": "3",
        ///       "am_used_count": "2",
        ///       "pm_used_count": "2",
        ///       "am_remaining_count": "1",
        ///       "pm_remaining_count": "1"
        ///     }
        ///   }
        /// }
        ///
        ///
        /// ===============================
        /// 【失敗回傳 JSON 範例】
        /// ===============================
        /// (1) 週六錯誤選擇
        /// {
        ///   "Code": -200,
        ///   "Method": "set_staff_quota_dayoff_selection",
        ///   "Result": "週六只允許 HALF_AM",
        ///   "Data": null
        /// }
        ///
        /// (2) 週日錯誤選擇
        /// {
        ///   "Code": -200,
        ///   "Method": "set_staff_quota_dayoff_selection",
        ///   "Result": "週日只允許 FULL",
        ///   "Data": null
        /// }
        ///
        /// (3) 下午半日已達上限
        /// {
        ///   "Code": -200,
        ///   "Method": "set_staff_quota_dayoff_selection",
        ///   "Result": "下午半日排休已達上限 2 次",
        ///   "Data": null
        /// }
        ///
        /// (4) 週六排休已達上限
        /// {
        ///   "Code": -200,
        ///   "Method": "set_staff_quota_dayoff_selection",
        ///   "Result": "週六排休已達上限 1 次",
        ///   "Data": null
        /// }
        ///
        /// (5) 當日上午休假名額已滿
        /// {
        ///   "Code": -200,
        ///   "Method": "set_staff_quota_dayoff_selection",
        ///   "Result": "當日上午休假名額已滿",
        ///   "Data": null
        /// }
        ///
        /// (6) 當日下午休假名額已滿
        /// {
        ///   "Code": -200,
        ///   "Method": "set_staff_quota_dayoff_selection",
        ///   "Result": "當日下午休假名額已滿",
        ///   "Data": null
        /// }
        ///
        /// (7) 整日休假但上午名額不足
        /// {
        ///   "Code": -200,
        ///   "Method": "set_staff_quota_dayoff_selection",
        ///   "Result": "當日上午休假名額已滿，無法選擇整日休假",
        ///   "Data": null
        /// }
        ///
        /// (8) 整日休假但下午名額不足
        /// {
        ///   "Code": -200,
        ///   "Method": "set_staff_quota_dayoff_selection",
        ///   "Result": "當日下午休假名額已滿，無法選擇整日休假",
        ///   "Data": null
        /// }
        ///
        /// (9) 應休額度不足
        /// {
        ///   "Code": -200,
        ///   "Method": "set_staff_quota_dayoff_selection",
        ///   "Result": "剩餘應休額度不足",
        ///   "Data": null
        /// }
        ///
        /// (10) 例外錯誤
        /// {
        ///   "Code": -500,
        ///   "Method": "set_staff_quota_dayoff_selection",
        ///   "Result": "Exception message ...",
        ///   "Data": null
        /// }
        /// </remarks>
        /// <param name="returnData">returnData 物件，主要使用 ValueAry 作為參數輸入。</param>
        /// <returns>回傳 JSON 字串。</returns>
        /// <summary>
        /// 使用應休額度進行排休選擇（支援虛擬 option、含公平機制與當日名額限制）
        /// </summary>
        [HttpPost("set_staff_quota_dayoff_selection")]
        public string set_staff_quota_dayoff_selection([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "set_staff_quota_dayoff_selection";

            try
            {
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

                if (select_type.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 select_type";
                    return returnData.JsonSerializationt();
                }

                select_type = select_type.Trim().ToUpperInvariant();
                bool isCancel = select_type == "CANCEL";

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

                    DateTime checkDate = off_date.StringToDateTime();
                    if (checkDate == DateTime.MinValue)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"off_date 格式錯誤: {off_date}";
                        return returnData.JsonSerializationt();
                    }

                    off_date = checkDate.ToDateString('-');
                }
                else
                {
                    if (option_guid.StringIsEmpty())
                    {
                        returnData.Code = -200;
                        returnData.Result = "取消應休排休必須提供 option_guid";
                        return returnData.JsonSerializationt();
                    }
                }

                var sql_form = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_staff = MethodClass.GetSQLControl<StaffClass>();
                var sql_option = MethodClass.GetSQLControl<StaffDayOffOptionClass>();
                var sql_item = MethodClass.GetSQLControl<DayOffScheduleItemClass>();

                object[] obj_form = sql_form.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();
                if (obj_form == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();

                object[] obj_staff = sql_staff.GetRowsByDefult(null, "staff_id", staff_id).FirstOrDefault();
                if (obj_staff == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到 staff_id={staff_id}";
                    return returnData.JsonSerializationt();
                }

                StaffClass staff = obj_staff.SQLToClass<StaffClass>();

                StaffDayOffOptionClass option = null;

                // =========================================================
                // 取得或建立 option
                // - 有 option_guid：更新既有 option
                // - 無 option_guid：依 staff + off_date 查找，找不到才建立 quota option
                // =========================================================
                if (option_guid.StringIsEmpty() == false)
                {
                    object[] obj_option = sql_option.GetRowsByDefult(null, "GUID", option_guid).FirstOrDefault();
                    if (obj_option == null)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"找不到 option_guid={option_guid}";
                        return returnData.JsonSerializationt();
                    }

                    option = obj_option.SQLToClass<StaffDayOffOptionClass>();
                }
                else
                {
                    string targetDate = off_date.StringToDateTime().ToDateString('-');

                    option = sql_option.GetRowsByDefult(null, "form_guid", form.GUID)
                        .SQLToClass<StaffDayOffOptionClass>()
                        .Where(x =>
                            x != null &&
                            x.staff_guid == staff.GUID &&
                            x.date.StringToDateTime().ToDateString('-') == targetDate)
                        .FirstOrDefault();

                    if (option == null)
                    {
                        string nowCreate = DateTime.Now.ToDateTimeString();

                        option = new StaffDayOffOptionClass();
                        option.GUID = Guid.NewGuid().ToString();
                        option.form_guid = form.GUID;
                        option.staff_guid = staff.GUID;
                        option.item_guid = "";
                        option.date = targetDate;
                        option.suggested_dates_list = new List<string>() { targetDate };

                        option.can_full = "true";
                        option.can_half_am = "true";
                        option.can_half_pm = "true";

                        option.selected_full = "false";
                        option.selected_half_am = "false";
                        option.selected_half_pm = "false";

                        option.is_any_date = "false";
                        option.is_forbidden = "false";
                        option.is_released = "false";
                        option.is_force_ff = "false";
                        option.force_ff_at = DateTime.MinValue.ToDateTimeString();
                        option.released_at = DateTime.MinValue.ToDateTimeString();

                        option.is_quota_dayoff = "false";
                        option.quota_used = "0";
                        option.quota_dayoff_type = "";

                        option.dayoff_source_type = "QUOTA_DAYOFF";
                        option.updated_at = nowCreate;

                        sql_option.AddRows(null, new List<object[]>
                {
                    option.ClassToSQL<StaffDayOffOptionClass>()
                });
                    }
                }

                if (option == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "取得 option 失敗";
                    return returnData.JsonSerializationt();
                }

                if (option.form_guid != form.GUID)
                {
                    returnData.Code = -200;
                    returnData.Result = "此 option 不屬於該 form_name";
                    return returnData.JsonSerializationt();
                }

                if (option.staff_guid != staff.GUID)
                {
                    returnData.Code = -200;
                    returnData.Result = "此 option 不屬於該 staff_id";
                    return returnData.JsonSerializationt();
                }

                option.NormalizeSelection();

                if (option.is_force_ff == "true")
                {
                    returnData.Code = -200;
                    returnData.Result = "系統強制放假(FF)不可操作";
                    return returnData.JsonSerializationt();
                }

                if (option.is_forbidden == "true")
                {
                    returnData.Code = -200;
                    returnData.Result = "此 option 已被禁止操作";
                    return returnData.JsonSerializationt();
                }

                if ((option.dayoff_source_type ?? "").Trim().ToUpper() == "HOLE_FILL")
                {
                    returnData.Code = -200;
                    returnData.Result = "填洞休假不可作為應休排休操作";
                    return returnData.JsonSerializationt();
                }

                if (option.is_released == "true")
                {
                    returnData.Code = -200;
                    returnData.Result = "已釋出中的 option 不可作為應休排休操作";
                    return returnData.JsonSerializationt();
                }

                string now = DateTime.Now.ToDateTimeString();

                // =========================================================
                // CANCEL：取消該筆應休排休
                // =========================================================
                if (isCancel)
                {
                    if (option.is_quota_dayoff != "true")
                    {
                        returnData.Code = -200;
                        returnData.Result = "此筆不是應休排休，無法取消";
                        return returnData.JsonSerializationt();
                    }

                    option.ClearSelection();
                    option.is_quota_dayoff = "false";
                    option.quota_used = "0";
                    option.quota_dayoff_type = "";
                    option.updated_at = now;
                    option.NormalizeSelection();

                    sql_option.UpdateByDefulteExtra(null, option.ClassToSQL<StaffDayOffOptionClass>());

                    var quotaSummaryAfterCancel = GetStaffRemainingQuotaDayoff(form.GUID, staff.GUID);
                    var ruleSummaryAfterCancel = GetStaffQuotaDayoffRuleSummary(form.GUID, staff.GUID);

                    DayOffDateQuotaUsageSummary dateSummaryAfterCancel = null;
                    if (!option.date.StringIsEmpty())
                    {
                        dateSummaryAfterCancel = GetDayOffDateQuotaUsageSummary(
                            form.GUID,
                            option.date.StringToDateTime().ToDateString('-')
                        );
                    }

                    returnData.Code = 200;
                    returnData.Result = "success";
                    returnData.Data = new
                    {
                        option,
                        quota_summary = quotaSummaryAfterCancel,
                        rule_summary = ruleSummaryAfterCancel,
                        date_quota_summary = dateSummaryAfterCancel
                    };
                    return returnData.JsonSerializationt(true);
                }

                DateTime dttargetDate = off_date.StringToDateTime();
                if (dttargetDate == DateTime.MinValue)
                {
                    returnData.Code = -200;
                    returnData.Result = $"off_date 格式錯誤: {off_date}";
                    return returnData.JsonSerializationt();
                }

                // =========================================================
                // 排除有排班的日期
                // =========================================================
                List<DayOffScheduleItemClass> items = sql_item
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleItemClass>()
                    .Where(x =>
                        x != null &&
                        x.staff_guid == staff.GUID &&
                        x.date.StringToDateTime().ToDateString('-') == off_date)
                    .ToList();

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

                if (items.Any(x => HasSchedule(x)))
                {
                    returnData.Code = -200;
                    returnData.Result = "該日已有排班，不可選擇應休排休";
                    return returnData.JsonSerializationt();
                }

                double consume = 0;
                string quotaDayoffType = "";

                // =========================================================
                // 日期規則
                // =========================================================
                if (dttargetDate.DayOfWeek == DayOfWeek.Saturday)
                {
                    if (select_type != "HALF_AM")
                    {
                        returnData.Code = -200;
                        returnData.Result = "週六只允許 HALF_AM";
                        return returnData.JsonSerializationt();
                    }

                    consume = 0.5;
                    quotaDayoffType = "SATURDAY_HALF_AM";
                }
                else if (dttargetDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    if (select_type != "FULL")
                    {
                        returnData.Code = -200;
                        returnData.Result = "週日只允許 FULL";
                        return returnData.JsonSerializationt();
                    }

                    consume = 1;
                    quotaDayoffType = "SUNDAY_FULL";
                }
                else
                {
                    if (select_type == "FULL")
                    {
                        consume = 1;
                        quotaDayoffType = "WEEKDAY_FULL";
                    }
                    else if (select_type == "HALF_AM")
                    {
                        consume = 0.5;
                        quotaDayoffType = "WEEKDAY_HALF_AM";
                    }
                    else if (select_type == "HALF_PM")
                    {
                        consume = 0.5;
                        quotaDayoffType = "WEEKDAY_HALF_PM";
                    }
                }

                // =========================================================
                // 原本已使用額度與類型
                // 編輯同一筆時，需補回原本已扣額度與次數
                // =========================================================
                double originalQuotaUsed = 0;
                string originalQuotaType = "";

                if (option.is_quota_dayoff == "true")
                {
                    originalQuotaUsed = option.quota_used.StringToDouble();
                    originalQuotaType = (option.quota_dayoff_type ?? "").Trim().ToUpper();
                }

                // =========================================================
                // 公平機制檢查
                // =========================================================
                StaffQuotaDayoffRuleSummary ruleSummary = GetStaffQuotaDayoffRuleSummary(form.GUID, staff.GUID);

                int pmHalfUsedCount = ruleSummary.pm_half_used_count.StringToInt32();
                int pmHalfLimitCount = ruleSummary.pm_half_limit_count.StringToInt32();

                int saturdayUsedCount = ruleSummary.saturday_used_count.StringToInt32();
                int saturdayLimitCount = ruleSummary.saturday_limit_count.StringToInt32();

                if (originalQuotaType == "WEEKDAY_HALF_PM")
                {
                    pmHalfUsedCount -= 1;
                }

                if (originalQuotaType == "SATURDAY_HALF_AM")
                {
                    saturdayUsedCount -= 1;
                }

                if (quotaDayoffType == "WEEKDAY_HALF_PM")
                {
                    if (pmHalfUsedCount >= pmHalfLimitCount)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"下午半日排休已達上限 {pmHalfLimitCount} 次";
                        return returnData.JsonSerializationt();
                    }
                }

                if (quotaDayoffType == "SATURDAY_HALF_AM")
                {
                    if (saturdayUsedCount >= saturdayLimitCount)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"週六排休已達上限 {saturdayLimitCount} 次";
                        return returnData.JsonSerializationt();
                    }
                }

                // =========================================================
                // 當日 AM / PM 名額檢查
                // =========================================================
                DayOffDateQuotaUsageSummary dateSummary = GetDayOffDateQuotaUsageSummary(form.GUID, off_date);
                if (dateSummary == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到日期資料({off_date})";
                    return returnData.JsonSerializationt();
                }

                int amUsed = dateSummary.am_used_count.StringToInt32();
                int pmUsed = dateSummary.pm_used_count.StringToInt32();
                int amMax = dateSummary.am_max_dayoff_count.StringToInt32();
                int pmMax = dateSummary.pm_max_dayoff_count.StringToInt32();

                // 扣掉自己原本占用，避免修改時誤擋
                if (option.selected_full == "true")
                {
                    amUsed -= 1;
                    pmUsed -= 1;
                }
                else
                {
                    if (option.selected_half_am == "true") amUsed -= 1;
                    if (option.selected_half_pm == "true") pmUsed -= 1;
                }

                if (select_type == "FULL")
                {
                    if (amUsed + 1 > amMax)
                    {
                        returnData.Code = -200;
                        returnData.Result = "當日上午休假名額已滿，無法選擇整日休假";
                        return returnData.JsonSerializationt();
                    }

                    if (pmUsed + 1 > pmMax)
                    {
                        returnData.Code = -200;
                        returnData.Result = "當日下午休假名額已滿，無法選擇整日休假";
                        return returnData.JsonSerializationt();
                    }
                }
                else if (select_type == "HALF_AM")
                {
                    if (amUsed + 1 > amMax)
                    {
                        returnData.Code = -200;
                        returnData.Result = "當日上午休假名額已滿";
                        return returnData.JsonSerializationt();
                    }
                }
                else if (select_type == "HALF_PM")
                {
                    if (pmUsed + 1 > pmMax)
                    {
                        returnData.Code = -200;
                        returnData.Result = "當日下午休假名額已滿";
                        return returnData.JsonSerializationt();
                    }
                }

                // =========================================================
                // 總應休額度檢查
                // =========================================================
                GetStaffRemainingQuotaDayoffResponse quotaSummary = GetStaffRemainingQuotaDayoff(form.GUID, staff.GUID);

                double remaining = quotaSummary.quota_remaining.StringToDouble();
                double availableForThisEdit = remaining + originalQuotaUsed;

                if (availableForThisEdit < consume)
                {
                    returnData.Code = -200;
                    returnData.Result = "剩餘應休額度不足";
                    return returnData.JsonSerializationt();
                }

                // =========================================================
                // 寫入選擇
                // =========================================================
                string err = "";
                bool ok = false;

                if (select_type == "FULL")
                {
                    ok = option.TrySelectFullDay(off_date, out err);
                }
                else if (select_type == "HALF_AM")
                {
                    ok = option.TrySelectHalfAM(off_date, out err);
                }
                else if (select_type == "HALF_PM")
                {
                    ok = option.TrySelectHalfPM(off_date, out err);
                }

                if (!ok)
                {
                    returnData.Code = -200;
                    returnData.Result = err;
                    return returnData.JsonSerializationt();
                }

                option.is_quota_dayoff = "true";
                option.quota_used = consume.ToString("0.##");
                option.quota_dayoff_type = quotaDayoffType;

                // 若原本是虛擬候選選上來的，正式標示為 QUOTA_DAYOFF
                if (option.dayoff_source_type.StringIsEmpty() ||
                    option.dayoff_source_type == "QUOTA_CANDIDATE")
                {
                    option.dayoff_source_type = "QUOTA_DAYOFF";
                }

                option.updated_at = now;
                option.NormalizeSelection();

                sql_option.UpdateByDefulteExtra(null, option.ClassToSQL<StaffDayOffOptionClass>());

                GetStaffRemainingQuotaDayoffResponse quotaSummaryAfter = GetStaffRemainingQuotaDayoff(form.GUID, staff.GUID);
                StaffQuotaDayoffRuleSummary ruleSummaryAfter = GetStaffQuotaDayoffRuleSummary(form.GUID, staff.GUID);
                DayOffDateQuotaUsageSummary dateSummaryAfter = GetDayOffDateQuotaUsageSummary(form.GUID, off_date);

                returnData.Code = 200;
                returnData.Result = "success";
                returnData.Data = new
                {
                    option,
                    quota_summary = quotaSummaryAfter,
                    rule_summary = ruleSummaryAfter,
                    date_quota_summary = dateSummaryAfter
                };
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
            finally
            {
                returnData.TimeTaken = timer.ToString();
            }
        }

        /// <summary>
        /// 查詢單一人員可用於應休排休的 option 清單（get_staff_quota_dayoff_available_options）
        /// </summary>
        /// <remarks>
        /// ===============================
        /// 【API 說明】
        /// ===============================
        /// 本 API 用於查詢單一人員在指定排休表單中：
        /// 1. 哪些 option 可用來做「應休額度排休」
        /// 2. 哪些 option 已經被選為「應休額度排休」
        /// 3. 每筆 option 目前可選擇的休假類型（FULL / HALF_AM / HALF_PM）
        /// 4. 每筆 option 是否可取消
        /// 5. 應休額度統計與公平機制統計
        ///
        /// 本 API 的主要用途是提供前端：
        /// - 取得應休排休用的 option_guid
        /// - 顯示可選清單
        /// - 顯示已選清單（可取消）
        /// - 直接知道每一天目前可不可選，以及不可選原因
        ///
        ///
        /// ===============================
        /// 【核心規則】
        /// ===============================
        /// 一、應休排休與預留休無關
        /// - 本 API 不再以「預留休是否完成」作為應休排休的前置條件
        /// - 使用者是否可以做應休排休，只看：
        ///   1. 日期規則
        ///   2. 個人應休額度是否足夠
        ///   3. 公平機制是否超限
        ///   4. 當日 AM / PM 名額是否已滿
        ///
        /// 二、option_guid 來源
        /// - option_guid 不由前端自行推算
        /// - 前端應直接使用本 API 回傳的 available_options / selected_options 中的 option_guid
        ///
        /// 三、取消操作來源
        /// - selected_options 內每筆資料皆提供 option_guid
        /// - 前端可直接以該筆 option_guid 呼叫取消 API
        ///
        ///
        /// ===============================
        /// 【available_options（可用清單）定義】
        /// ===============================
        /// 會列出該人員在此表單下，所有可作為「應休排休候選」的 option。
        ///
        /// 基本資料來源條件：
        /// 1. 屬於指定 form_guid + staff_guid
        /// 2. 具有有效日期
        ///
        /// 以下 option 不可作為應休排休：
        /// 1. dayoff_source_type = HOLE_FILL
        /// 2. is_released = true
        /// 3. is_force_ff = true
        /// 4. is_forbidden = true
        ///
        /// 但即使列在 available_options 中，實際能不能選，仍需再看：
        /// - 當日是平日 / 週六 / 週日
        /// - 個人應休剩餘額度
        /// - 公平機制限制
        /// - 當日 AM / PM 剩餘名額
        ///
        ///
        /// ===============================
        /// 【selected_options（已選清單）定義】
        /// ===============================
        /// 符合以下條件的 option 會列入已選清單：
        /// 1. is_quota_dayoff = true
        /// 2. selected_full / selected_half_am / selected_half_pm 至少一個為 true
        ///
        /// 主要用途：
        /// - 顯示目前已選的應休排休
        /// - 提供 option_guid 給前端逐筆取消
        ///
        ///
        /// ===============================
        /// 【日期可選規則】
        /// ===============================
        /// 一、平日
        /// - 可選 FULL
        /// - 可選 HALF_AM
        /// - 可選 HALF_PM
        ///
        /// 二、週六
        /// - 只允許 HALF_AM
        /// - 不允許 FULL
        /// - 不允許 HALF_PM
        ///
        /// 三、週日
        /// - 只允許 FULL
        /// - 不允許 HALF_AM
        /// - 不允許 HALF_PM
        ///
        ///
        /// ===============================
        /// 【個人應休額度規則】
        /// ===============================
        /// 本 API 會依 quota_summary 判斷：
        /// - quota_total
        /// - quota_used_total
        /// - quota_remaining
        ///
        /// 若剩餘應休額度不足：
        /// - FULL 不可選
        /// - HALF_AM / HALF_PM 不可選
        ///
        /// 並會將對應的 can_select_xxx 設為 false
        ///
        ///
        /// ===============================
        /// 【公平機制規則】
        /// ===============================
        /// 一、下午半日限制
        /// - 只統計 is_quota_dayoff = true 且 quota_dayoff_type = WEEKDAY_HALF_PM
        /// - 每人最多 2 次
        ///
        /// 二、週六限制
        /// - 只統計 is_quota_dayoff = true 且 quota_dayoff_type = SATURDAY_HALF_AM
        /// - 每人預設最多 1 次
        /// - 若有額外週六資格，則可增加為 2 次
        ///
        /// 三、本 API 會回傳 rule_summary：
        /// - pm_half_used_count
        /// - saturday_used_count
        /// - pm_half_limit_count
        /// - saturday_limit_count
        /// - has_extra_saturday_limit
        ///
        /// 並依此決定該 option 的 can_select_half_pm / can_select_half_am 是否可用
        ///
        ///
        /// ===============================
        /// 【當日名額限制】
        /// ===============================
        /// 本 API 會再依指定日期的整體休假名額做判斷：
        /// - am_max_dayoff_count
        /// - pm_max_dayoff_count
        ///
        /// 名額占用規則：
        /// 一、上午名額占用
        /// - selected_full = true
        /// - selected_half_am = true
        ///
        /// 二、下午名額占用
        /// - selected_full = true
        /// - selected_half_pm = true
        ///
        /// 三、名額判斷以整張表單所有人為準，不只看單一人員
        ///
        /// 四、若這筆 option 原本已經有選擇，系統會先扣除自己原本占用的名額，再判斷是否還能修改
        ///
        /// 例如：
        /// - 平日 FULL 需同時有上午與下午名額
        /// - HALF_AM 需有上午名額
        /// - HALF_PM 需有下午名額
        ///
        ///
        /// ===============================
        /// 【欄位說明】
        /// ===============================
        /// 一、available_options 每筆重點欄位
        /// - option_guid
        /// - date
        /// - day_type
        /// - can_select_full
        /// - can_select_half_am
        /// - can_select_half_pm
        /// - can_select
        /// - block_reason
        ///
        /// 二、selected_options 每筆重點欄位
        /// - option_guid
        /// - selected_type
        /// - quota_used
        /// - quota_dayoff_type
        /// - can_cancel
        ///
        /// 三、前端建議用法
        /// - available_options：做新增 / 修改選擇
        /// - selected_options：做已選列表與取消操作
        ///
        ///
        /// ===============================
        /// 【URL】
        /// ===============================
        /// POST /phar_roster_api/dayOffSchedule/get_staff_quota_dayoff_available_options
        ///
        /// ===============================
        /// 【Method】
        /// ===============================
        /// POST
        ///
        /// ===============================
        /// 【傳入參數】(ValueAry)
        /// ===============================
        /// form_name = 表單名稱（必填）
        /// staff_id  = 人員工號（必填）
        ///
        ///
        /// ===============================
        /// 【JSON 傳入範例】
        /// ===============================
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "staff_id=A12345"
        ///   ]
        /// }
        ///
        ///
        /// ===============================
        /// 【成功回傳 JSON 範例】
        /// ===============================
        /// {
        ///   "Code": 200,
        ///   "Method": "get_staff_quota_dayoff_available_options",
        ///   "Result": "取得應休排休可用清單成功",
        ///   "Data": {
        ///     "form_guid": "FORM_GUID_001",
        ///     "form_name": "2026年03月排休表",
        ///     "staff_guid": "STAFF_GUID_001",
        ///     "staff_id": "A12345",
        ///     "staff_name": "王小明",
        ///     "is_reserved_completed": "true",
        ///     "quota_summary": {
        ///       "staff_guid": "STAFF_GUID_001",
        ///       "quota_total": "5.5",
        ///       "quota_used_total": "1.5",
        ///       "quota_remaining": "4"
        ///     },
        ///     "rule_summary": {
        ///       "quota_total": "5.5",
        ///       "quota_used_total": "1.5",
        ///       "quota_remaining": "4",
        ///       "pm_half_used_count": "1",
        ///       "saturday_used_count": "1",
        ///       "pm_half_limit_count": "2",
        ///       "saturday_limit_count": "2",
        ///       "has_extra_saturday_limit": "true"
        ///     },
        ///     "available_options": [
        ///       {
        ///         "option_guid": "OPTION_GUID_001",
        ///         "item_guid": "ITEM_GUID_001",
        ///         "staff_guid": "STAFF_GUID_001",
        ///         "staff_id": "A12345",
        ///         "staff_name": "王小明",
        ///         "date": "2026-03-12",
        ///         "week_day": "Wed",
        ///         "day_type": "WEEKDAY",
        ///         "can_select_full": "true",
        ///         "can_select_half_am": "true",
        ///         "can_select_half_pm": "false",
        ///         "is_quota_dayoff": "false",
        ///         "selected_type": "NONE",
        ///         "quota_used": "0",
        ///         "quota_dayoff_type": "",
        ///         "can_cancel": "false",
        ///         "can_select": "true",
        ///         "block_reason": ""
        ///       }
        ///     ],
        ///     "selected_options": [
        ///       {
        ///         "option_guid": "OPTION_GUID_002",
        ///         "item_guid": "ITEM_GUID_002",
        ///         "staff_guid": "STAFF_GUID_001",
        ///         "staff_id": "A12345",
        ///         "staff_name": "王小明",
        ///         "date": "2026-03-14",
        ///         "week_day": "Sat",
        ///         "day_type": "SATURDAY",
        ///         "can_select_full": "false",
        ///         "can_select_half_am": "true",
        ///         "can_select_half_pm": "false",
        ///         "is_quota_dayoff": "true",
        ///         "selected_type": "HALF_AM",
        ///         "quota_used": "0.5",
        ///         "quota_dayoff_type": "SATURDAY_HALF_AM",
        ///         "can_cancel": "true",
        ///         "can_select": "true",
        ///         "block_reason": ""
        ///       }
        ///     ]
        ///   }
        /// }
        ///
        ///
        /// ===============================
        /// 【失敗回傳 JSON 範例】
        /// ===============================
        /// (1) 未提供 form_name
        /// {
        ///   "Code": -200,
        ///   "Method": "get_staff_quota_dayoff_available_options",
        ///   "Result": "未輸入 form_name",
        ///   "Data": null
        /// }
        ///
        /// (2) 未提供 staff_id
        /// {
        ///   "Code": -200,
        ///   "Method": "get_staff_quota_dayoff_available_options",
        ///   "Result": "未輸入 staff_id",
        ///   "Data": null
        /// }
        ///
        /// (3) 找不到表單
        /// {
        ///   "Code": -200,
        ///   "Method": "get_staff_quota_dayoff_available_options",
        ///   "Result": "找不到表單名稱(2026年03月排休表)",
        ///   "Data": null
        /// }
        ///
        /// (4) 找不到人員
        /// {
        ///   "Code": -200,
        ///   "Method": "get_staff_quota_dayoff_available_options",
        ///   "Result": "找不到 staff_id=A12345",
        ///   "Data": null
        /// }
        ///
        /// (5) 例外錯誤
        /// {
        ///   "Code": -500,
        ///   "Method": "get_staff_quota_dayoff_available_options",
        ///   "Result": "Exception message ...",
        ///   "Data": null
        /// }
        /// </remarks>
        /// <param name="returnData">returnData 物件，主要使用 ValueAry 作為參數輸入。</param>
        /// <returns>回傳 JSON 字串。</returns>
        [HttpPost("get_staff_quota_dayoff_available_options")]
        public string get_staff_quota_dayoff_available_options([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_staff_quota_dayoff_available_options";

            try
            {
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
                    return returnData.JsonSerializationt();
                }

                if (staff_id.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 staff_id";
                    return returnData.JsonSerializationt();
                }

                var sql_form = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_staff = MethodClass.GetSQLControl<StaffClass>();
                var sql_day = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_option = MethodClass.GetSQLControl<StaffDayOffOptionClass>();
                var sql_item = MethodClass.GetSQLControl<DayOffScheduleItemClass>();

                object[] obj_form = sql_form.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();
                if (obj_form == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();

                object[] obj_staff = sql_staff.GetRowsByDefult(null, "staff_id", staff_id).FirstOrDefault();
                if (obj_staff == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到 staff_id={staff_id}";
                    return returnData.JsonSerializationt();
                }

                StaffClass staff = obj_staff.SQLToClass<StaffClass>();

                List<DayOffScheduleDayClass> days = sql_day
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleDayClass>()
                    .OrderBy(x => x.date.StringToDateTime())
                    .ToList();

                List<StaffDayOffOptionClass> options = sql_option
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<StaffDayOffOptionClass>()
                    .Where(x => x != null && x.staff_guid == staff.GUID)
                    .OrderBy(x => x.date.StringToDateTime())
                    .ToList();

                List<DayOffScheduleItemClass> items = sql_item
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleItemClass>()
                    .Where(x => x != null && x.staff_guid == staff.GUID)
                    .ToList();

                Dictionary<string, DayOffScheduleItemClass> itemByDate = items
                    .Where(x => x != null && x.date.StringIsEmpty() == false)
                    .GroupBy(x => x.date.StringToDateTime().ToDateString('-'))
                    .ToDictionary(g => g.Key, g => g.First());

                Dictionary<string, StaffDayOffOptionClass> optionByDate = options
                    .Where(x => x != null && x.date.StringIsEmpty() == false)
                    .GroupBy(x => x.date.StringToDateTime().ToDateString('-'))
                    .ToDictionary(g => g.Key, g => g.First());

                var quotaSummary = GetStaffRemainingQuotaDayoff(form.GUID, staff.GUID);
                var ruleSummary = GetStaffQuotaDayoffRuleSummary(form.GUID, staff.GUID);

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

                string WeekDayText(DateTime dt)
                {
                    if (dt.DayOfWeek == DayOfWeek.Monday) return "Mon";
                    if (dt.DayOfWeek == DayOfWeek.Tuesday) return "Tue";
                    if (dt.DayOfWeek == DayOfWeek.Wednesday) return "Wed";
                    if (dt.DayOfWeek == DayOfWeek.Thursday) return "Thu";
                    if (dt.DayOfWeek == DayOfWeek.Friday) return "Fri";
                    if (dt.DayOfWeek == DayOfWeek.Saturday) return "Sat";
                    return "Sun";
                }

                string DayType(DateTime dt)
                {
                    if (dt.DayOfWeek == DayOfWeek.Saturday) return "SATURDAY";
                    if (dt.DayOfWeek == DayOfWeek.Sunday) return "SUNDAY";
                    return "WEEKDAY";
                }

                string SelectedType(StaffDayOffOptionClass option)
                {
                    if (option == null) return "NONE";
                    if (option.selected_full == "true") return "FULL";
                    if (option.selected_half_am == "true") return "HALF_AM";
                    if (option.selected_half_pm == "true") return "HALF_PM";
                    return "NONE";
                }

                StaffDayOffOptionClass BuildVirtualOption(DateTime dt)
                {
                    string date = dt.ToDateString('-');

                    return new StaffDayOffOptionClass
                    {
                        GUID = "",
                        form_guid = form.GUID,
                        item_guid = "",
                        staff_guid = staff.GUID,
                        date = date,

                        can_full = "true",
                        can_half_am = "true",
                        can_half_pm = "true",

                        selected_full = "false",
                        selected_half_am = "false",
                        selected_half_pm = "false",

                        is_any_date = "false",
                        is_forbidden = "false",
                        is_released = "false",
                        is_force_ff = "false",

                        is_quota_dayoff = "false",
                        quota_used = "0",
                        quota_dayoff_type = "",

                        dayoff_source_type = "QUOTA_CANDIDATE"
                    };
                }

                StaffQuotaDayoffAvailableOptionDto BuildDto(StaffDayOffOptionClass option)
                {
                    option.NormalizeSelection();

                    DateTime dt = option.date.StringToDateTime();
                    string dayType = DayType(dt);

                    bool canFull = false;
                    bool canHalfAm = false;
                    bool canHalfPm = false;
                    bool canSelect = true;
                    string blockReason = "";

                    string selectedType = SelectedType(option);

                    if ((option.dayoff_source_type ?? "").Trim().ToUpper() == "HOLE_FILL")
                    {
                        canSelect = false;
                        blockReason = "填洞休假不可作為應休排休操作";
                    }
                    else if (option.is_released == "true")
                    {
                        canSelect = false;
                        blockReason = "已釋出中的 option 不可作為應休排休操作";
                    }
                    else if (option.is_force_ff == "true")
                    {
                        canSelect = false;
                        blockReason = "系統強制放假(FF)不可操作";
                    }
                    else if (option.is_forbidden == "true")
                    {
                        canSelect = false;
                        blockReason = "此 option 已被禁止操作";
                    }

                    if (dayType == "WEEKDAY")
                    {
                        canFull = true;
                        canHalfAm = true;
                        canHalfPm = true;
                    }
                    else if (dayType == "SATURDAY")
                    {
                        canFull = false;
                        canHalfAm = true;
                        canHalfPm = false;
                    }
                    else if (dayType == "SUNDAY")
                    {
                        canFull = true;
                        canHalfAm = false;
                        canHalfPm = false;
                    }

                    int pmHalfUsedCount = ruleSummary.pm_half_used_count.StringToInt32();
                    int pmHalfLimitCount = ruleSummary.pm_half_limit_count.StringToInt32();

                    int saturdayUsedCount = ruleSummary.saturday_used_count.StringToInt32();
                    int saturdayLimitCount = ruleSummary.saturday_limit_count.StringToInt32();

                    double quotaRemaining = quotaSummary.quota_remaining.StringToDouble();

                    string originalQuotaType = (option.quota_dayoff_type ?? "").Trim().ToUpper();
                    double originalQuotaUsed = 0;

                    if (option.is_quota_dayoff == "true")
                    {
                        originalQuotaUsed = option.quota_used.StringToDouble();

                        if (originalQuotaType == "WEEKDAY_HALF_PM")
                            pmHalfUsedCount -= 1;

                        if (originalQuotaType == "SATURDAY_HALF_AM")
                            saturdayUsedCount -= 1;
                    }

                    double availableForThisEdit = quotaRemaining + originalQuotaUsed;

                    DayOffDateQuotaUsageSummary dateSummary = GetDayOffDateQuotaUsageSummary(form.GUID, option.date);

                    int amUsed = 0;
                    int pmUsed = 0;
                    int amMax = 0;
                    int pmMax = 0;

                    if (dateSummary != null)
                    {
                        amUsed = dateSummary.am_used_count.StringToInt32();
                        pmUsed = dateSummary.pm_used_count.StringToInt32();
                        amMax = dateSummary.am_max_dayoff_count.StringToInt32();
                        pmMax = dateSummary.pm_max_dayoff_count.StringToInt32();

                        if (option.selected_full == "true")
                        {
                            amUsed -= 1;
                            pmUsed -= 1;
                        }
                        else
                        {
                            if (option.selected_half_am == "true") amUsed -= 1;
                            if (option.selected_half_pm == "true") pmUsed -= 1;
                        }
                    }

                    bool AllowByAM(int add) => (amUsed + add) <= amMax;
                    bool AllowByPM(int add) => (pmUsed + add) <= pmMax;

                    if (!canSelect)
                    {
                        canFull = false;
                        canHalfAm = false;
                        canHalfPm = false;
                    }
                    else
                    {
                        if (dayType == "WEEKDAY")
                        {
                            canFull = canFull && availableForThisEdit >= 1 && AllowByAM(1) && AllowByPM(1);
                            canHalfAm = canHalfAm && availableForThisEdit >= 0.5 && AllowByAM(1);
                            canHalfPm = canHalfPm && availableForThisEdit >= 0.5 && AllowByPM(1) && pmHalfUsedCount < pmHalfLimitCount;
                        }
                        else if (dayType == "SATURDAY")
                        {
                            canFull = false;
                            canHalfPm = false;
                            canHalfAm = canHalfAm &&
                                        availableForThisEdit >= 0.5 &&
                                        AllowByAM(1) &&
                                        saturdayUsedCount < saturdayLimitCount;
                        }
                        else if (dayType == "SUNDAY")
                        {
                            canHalfAm = false;
                            canHalfPm = false;
                            canFull = canFull && availableForThisEdit >= 1 && AllowByAM(1) && AllowByPM(1);
                        }

                        if (!canFull && !canHalfAm && !canHalfPm)
                        {
                            if (availableForThisEdit < 0.5)
                            {
                                blockReason = "剩餘應休額度不足";
                            }
                            else if (dayType == "SATURDAY" && saturdayUsedCount >= saturdayLimitCount)
                            {
                                blockReason = $"週六排休已達上限 {saturdayLimitCount} 次";
                            }
                            else if (dayType == "WEEKDAY" && pmHalfUsedCount >= pmHalfLimitCount && AllowByAM(1) == false)
                            {
                                blockReason = "當日上午休假名額已滿";
                            }
                            else if (dayType == "WEEKDAY" && pmHalfUsedCount >= pmHalfLimitCount)
                            {
                                blockReason = $"下午半日排休已達上限 {pmHalfLimitCount} 次";
                            }
                            else if (!AllowByAM(1) && !AllowByPM(1))
                            {
                                blockReason = "當日休假名額已滿";
                            }
                            else if (!AllowByAM(1))
                            {
                                blockReason = "當日上午休假名額已滿";
                            }
                            else if (!AllowByPM(1))
                            {
                                blockReason = "當日下午休假名額已滿";
                            }

                            canSelect = false;
                        }
                    }

                    return new StaffQuotaDayoffAvailableOptionDto
                    {
                        option_guid = option.GUID ?? "",
                        item_guid = option.item_guid ?? "",
                        staff_guid = option.staff_guid ?? "",
                        staff_id = staff.staff_id ?? "",
                        staff_name = staff.staff_name ?? "",
                        date = dt == DateTime.MinValue ? "" : dt.ToDateString('-'),
                        week_day = dt == DateTime.MinValue ? "" : WeekDayText(dt),
                        day_type = dt == DateTime.MinValue ? "" : dayType,

                        can_select_full = canFull ? "true" : "false",
                        can_select_half_am = canHalfAm ? "true" : "false",
                        can_select_half_pm = canHalfPm ? "true" : "false",

                        is_quota_dayoff = option.is_quota_dayoff ?? "false",
                        selected_type = selectedType,
                        quota_used = option.quota_used ?? "0",
                        quota_dayoff_type = option.quota_dayoff_type ?? "",

                        can_cancel = (option.is_quota_dayoff == "true" && selectedType != "NONE") ? "true" : "false",
                        can_select = canSelect ? "true" : "false",
                        block_reason = blockReason,
                        is_virtual = option.GUID.StringIsEmpty() ? "true" : "false"
                    };
                }

                StaffQuotaDayoffAvailableOptionsResponse response = new StaffQuotaDayoffAvailableOptionsResponse();
                response.form_guid = form.GUID;
                response.form_name = form.form_name;
                response.staff_guid = staff.GUID;
                response.staff_id = staff.staff_id;
                response.staff_name = staff.staff_name;
                response.is_reserved_completed = "true";
                response.quota_summary = quotaSummary;
                response.rule_summary = ruleSummary;

                foreach (var day in days)
                {
                    DateTime dt = day.date.StringToDateTime();
                    if (dt == DateTime.MinValue) continue;

                    string dateKey = dt.ToDateString('-');

                    itemByDate.TryGetValue(dateKey, out var item);
                    optionByDate.TryGetValue(dateKey, out var option);

                    // 有排班，排除在應休候選之外
                    if (HasSchedule(item)) continue;

                    if (option == null)
                    {
                        var virtualOption = BuildVirtualOption(dt);
                        response.available_options.Add(BuildDto(virtualOption));
                        continue;
                    }

                    option.NormalizeSelection();

                    bool isSelectedQuota = option.is_quota_dayoff == "true" &&
                                           (option.selected_full == "true" ||
                                            option.selected_half_am == "true" ||
                                            option.selected_half_pm == "true");

                    if (isSelectedQuota)
                    {
                        response.selected_options.Add(BuildDto(option));
                        continue;
                    }

                    // 不可作為應休候選的既有 option 直接排除
                    if ((option.dayoff_source_type ?? "").Trim().ToUpper() == "HOLE_FILL") continue;
                    if (option.is_released == "true") continue;
                    if (option.is_force_ff == "true") continue;
                    if (option.is_forbidden == "true") continue;

                    response.available_options.Add(BuildDto(option));
                }

                response.available_options = response.available_options
                    .OrderBy(x => x.date.StringToDateTime())
                    .ThenBy(x => x.option_guid)
                    .ToList();

                response.selected_options = response.selected_options
                    .OrderBy(x => x.date.StringToDateTime())
                    .ThenBy(x => x.option_guid)
                    .ToList();

                returnData.Code = 200;
                returnData.Result = "取得應休排休可用清單成功";
                returnData.Data = response;
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
            finally
            {
                returnData.TimeTaken = timer.ToString();
            }
        }

        /// <summary>
        /// 查詢應休排休總表（批次優化版 get_quota_dayoff_roster_overview）
        /// </summary>
        /// <remarks>
        /// ===============================
        /// 【API 說明】
        /// ===============================
        /// 本 API 專門提供「應休排休總表」使用。
        ///
        /// 本 API 不更動原本 get_form。
        ///
        /// get_form：
        /// - 維持原本排班、預留休、釋出、強制休假等資料顯示。
        ///
        /// get_quota_dayoff_roster_overview：
        /// - 專門提供多人 × 日期的應休排休總表。
        /// - 回傳每位人員每日是否可選應休。
        /// - 回傳已選應休。
        /// - 回傳不可選原因。
        /// - 回傳應休額度、公平機制、週六次數、下午半日次數。
        ///
        /// set_staff_quota_dayoff_selection：
        /// - 實際新增、修改、取消應休排休。
        ///
        ///
        /// ===============================
        /// 【效能優化說明】
        /// ===============================
        /// 舊版容易慢的原因：
        ///
        /// 1. 每一格重複計算當日 AM / PM 名額。
        /// 2. 每一位人員重複呼叫 GetStaffRemainingQuotaDayoff。
        /// 3. 每一位人員重複呼叫 GetStaffQuotaDayoffRuleSummary。
        ///
        /// 本版改為：
        ///
        /// 1. 一次讀取整張表單 days。
        /// 2. 一次讀取整張表單 items。
        /// 3. 一次讀取整張表單 options。
        /// 4. 一次建立 dateQuotaDict。
        /// 5. 一次建立 quotaSummaryDict。
        /// 6. 一次建立 ruleSummaryDict。
        /// 7. 主迴圈只查 Dictionary，不再重複查詢與重算。
        ///
        ///
        /// ===============================
        /// 【URL】
        /// ===============================
        /// POST /phar_roster_api/dayOffSchedule/get_quota_dayoff_roster_overview
        ///
        ///
        /// ===============================
        /// 【傳入參數 ValueAry】
        /// ===============================
        /// form_name = 表單名稱（必填）
        /// page      = 頁碼（選填，預設 1）
        /// page_size = 每頁人員數（選填，預設 20）
        /// staff_ids = 指定人員工號清單（選填，多筆以逗號分隔）
        ///
        ///
        /// ===============================
        /// 【Request 範例】
        /// ===============================
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026-03",
        ///     "page=1",
        ///     "page_size=20"
        ///   ]
        /// }
        ///
        ///
        /// ===============================
        /// 【Request 範例：指定人員】
        /// ===============================
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026-03",
        ///     "staff_ids=1120468,1130614"
        ///   ]
        /// }
        ///
        ///
        /// ===============================
        /// 【display_type 定義】
        /// ===============================
        /// SCHEDULE
        /// - 該日已有排班
        ///
        /// FORCE_FF
        /// - 系統強制休假
        ///
        /// RELEASED
        /// - 已釋出
        ///
        /// HOLE_FILL
        /// - 填洞休假
        ///
        /// QUOTA_SELECTED
        /// - 已選應休排休
        ///
        /// AVAILABLE
        /// - 可選應休排休
        ///
        /// BLOCKED
        /// - 不可選
        ///
        /// EMPTY
        /// - 空白
        ///
        ///
        /// ===============================
        /// 【可選判斷規則】
        /// ===============================
        /// 平日：
        /// - FULL：需要剩餘應休 >= 1，且上午、下午皆有名額。
        /// - HALF_AM：需要剩餘應休 >= 0.5，且上午有名額。
        /// - HALF_PM：需要剩餘應休 >= 0.5，且下午有名額，且未超過下午半日上限。
        ///
        /// 週六：
        /// - 只允許 HALF_AM。
        /// - 需要剩餘應休 >= 0.5。
        /// - 上午有名額。
        /// - 未超過週六上限。
        ///
        /// 週日：
        /// - 只允許 FULL。
        /// - 需要剩餘應休 >= 1。
        /// - 上午、下午皆有名額。
        ///
        ///
        /// ===============================
        /// 【前端使用建議】
        /// ===============================
        /// 1. 應休排休總表使用本 API。
        /// 2. 原本排班表仍使用 get_form。
        /// 3. 前端不要自行計算額度、名額、次數。
        /// 4. 前端只看 can_select_xxx、display_type、block_reason。
        /// 5. 若 option_guid="" 且 is_virtual="true"，仍可呼叫 set_staff_quota_dayoff_selection。
        /// 6. 每次新增、修改、取消成功後，重新呼叫本 API。
        ///
        /// </remarks>
        /// <param name="returnData">returnData 物件，使用 ValueAry 傳入 form_name / page / page_size / staff_ids。</param>
        /// <returns>回傳應休排休總表 JSON。</returns>
        [HttpPost("get_quota_dayoff_roster_overview")]
        public string get_quota_dayoff_roster_overview([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "get_quota_dayoff_roster_overview";

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry?
                        .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                        ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string staff_ids_text = GetVal("staff_ids");
                string page_text = GetVal("page");
                string page_size_text = GetVal("page_size");

                int page = page_text.StringToInt32();
                int pageSize = page_size_text.StringToInt32();

                if (page <= 0) page = 1;
                if (pageSize <= 0) pageSize = 20;

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 form_name";
                    return returnData.JsonSerializationt();
                }

                var sql_form = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_day = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_item = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
                var sql_option = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

                object[] obj_form = sql_form.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();
                if (obj_form == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();

                List<DayOffScheduleDayClass> days = sql_day
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleDayClass>()
                    .Where(x => x != null)
                    .OrderBy(x => x.date.StringToDateTime())
                    .ToList();

                List<DayOffScheduleItemClass> allItems = sql_item
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleItemClass>();

                List<StaffDayOffOptionClass> allOptions = sql_option
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<StaffDayOffOptionClass>();

                allItems = allItems ?? new List<DayOffScheduleItemClass>();
                allOptions = allOptions ?? new List<StaffDayOffOptionClass>();

                List<string> staffIdFilter = new List<string>();
                if (!staff_ids_text.StringIsEmpty())
                {
                    staffIdFilter = staff_ids_text
                        .Split(',')
                        .Select(x => x.Trim())
                        .Where(x => x.StringIsEmpty() == false)
                        .ToList();
                }

                var allStaffs = allItems
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
                    .OrderBy(x => x.staff_id)
                    .ToList();

                if (staffIdFilter.Count > 0)
                {
                    allStaffs = allStaffs
                        .Where(x => staffIdFilter.Contains(x.staff_id))
                        .ToList();
                }

                int totalCount = allStaffs.Count;
                int totalPage = (int)Math.Ceiling(totalCount / (double)pageSize);

                var pageStaffs = allStaffs
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                List<string> dateKeys = days
                    .Select(x => x.date.StringToDateTime().ToDateString('-'))
                    .Where(x => x.StringIsEmpty() == false)
                    .ToList();

                Dictionary<string, DayOffDateQuotaUsageSummary> dateQuotaDict =
                    BuildDateQuotaUsageSummaryDict(days, allOptions);

                Dictionary<string, DayOffScheduleItemClass> itemsByStaffDate = allItems
                    .Where(x =>
                        x != null &&
                        x.staff_guid.StringIsEmpty() == false &&
                        x.date.StringIsEmpty() == false)
                    .GroupBy(x => $"{x.staff_guid}|{x.date.StringToDateTime().ToDateString('-')}")
                    .ToDictionary(g => g.Key, g => g.First());

                Dictionary<string, StaffDayOffOptionClass> optionsByStaffDate = allOptions
                    .Where(x =>
                        x != null &&
                        x.staff_guid.StringIsEmpty() == false &&
                        x.date.StringIsEmpty() == false)
                    .GroupBy(x => $"{x.staff_guid}|{x.date.StringToDateTime().ToDateString('-')}")
                    .ToDictionary(g => g.Key, g => g.First());

                // 批次建立每人應休額度統計
                Dictionary<string, GetStaffRemainingQuotaDayoffResponse> quotaSummaryDict =
                    BuildStaffQuotaSummaryDict(form.GUID, days, allItems, allOptions);

                // 批次建立每人公平機制統計
                Dictionary<string, StaffQuotaDayoffRuleSummary> ruleSummaryDict =
                    BuildStaffRuleSummaryDict(form.GUID, allOptions, quotaSummaryDict);

                QuotaDayoffRosterOverviewResponse response = new QuotaDayoffRosterOverviewResponse();
                response.form_guid = form.GUID;
                response.form_name = form.form_name;
                response.total_count = totalCount.ToString();
                response.page = page.ToString();
                response.page_size = pageSize.ToString();
                response.total_page = totalPage.ToString();
                response.dates = dateKeys;

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

                string WeekDayText(DateTime dt)
                {
                    if (dt.DayOfWeek == DayOfWeek.Monday) return "Mon";
                    if (dt.DayOfWeek == DayOfWeek.Tuesday) return "Tue";
                    if (dt.DayOfWeek == DayOfWeek.Wednesday) return "Wed";
                    if (dt.DayOfWeek == DayOfWeek.Thursday) return "Thu";
                    if (dt.DayOfWeek == DayOfWeek.Friday) return "Fri";
                    if (dt.DayOfWeek == DayOfWeek.Saturday) return "Sat";
                    return "Sun";
                }

                string DayType(DateTime dt)
                {
                    if (dt.DayOfWeek == DayOfWeek.Saturday) return "SATURDAY";
                    if (dt.DayOfWeek == DayOfWeek.Sunday) return "SUNDAY";
                    return "WEEKDAY";
                }

                string SelectedType(StaffDayOffOptionClass option)
                {
                    if (option == null) return "NONE";
                    if (option.selected_full == "true") return "FULL";
                    if (option.selected_half_am == "true") return "HALF_AM";
                    if (option.selected_half_pm == "true") return "HALF_PM";
                    return "NONE";
                }

                foreach (var staff in pageStaffs)
                {
                    string staffGuid = staff.staff_guid;

                    quotaSummaryDict.TryGetValue(staffGuid, out var quotaSummary);
                    ruleSummaryDict.TryGetValue(staffGuid, out var ruleSummary);

                    if (quotaSummary == null)
                    {
                        quotaSummary = new GetStaffRemainingQuotaDayoffResponse
                        {
                            staff_guid = staffGuid,
                            quota_total = "0",
                            quota_used_total = "0",
                            quota_remaining = "0"
                        };
                    }

                    if (ruleSummary == null)
                    {
                        ruleSummary = new StaffQuotaDayoffRuleSummary
                        {
                            quota_total = quotaSummary.quota_total,
                            quota_used_total = quotaSummary.quota_used_total,
                            quota_remaining = quotaSummary.quota_remaining,
                            pm_half_used_count = "0",
                            saturday_used_count = "0",
                            pm_half_limit_count = "2",
                            saturday_limit_count = "1",
                            has_extra_saturday_limit = "false"
                        };
                    }

                    double quotaRemaining = quotaSummary.quota_remaining.StringToDouble();

                    int pmHalfUsedCount = ruleSummary.pm_half_used_count.StringToInt32();
                    int pmHalfLimitCount = ruleSummary.pm_half_limit_count.StringToInt32();

                    int saturdayUsedCount = ruleSummary.saturday_used_count.StringToInt32();
                    int saturdayLimitCount = ruleSummary.saturday_limit_count.StringToInt32();

                    QuotaDayoffRosterStaffRowDto row = new QuotaDayoffRosterStaffRowDto();
                    row.staff_guid = staff.staff_guid;
                    row.staff_id = staff.staff_id;
                    row.staff_name = staff.staff_name;
                    row.staff_simple_name = staff.staff_simple_name;
                    row.position = staff.position;
                    row.quota_summary = quotaSummary;
                    row.rule_summary = ruleSummary;

                    foreach (var day in days)
                    {
                        DateTime dt = day.date.StringToDateTime();
                        if (dt == DateTime.MinValue) continue;

                        string dateKey = dt.ToDateString('-');
                        string staffDateKey = $"{staffGuid}|{dateKey}";
                        string dayType = DayType(dt);

                        itemsByStaffDate.TryGetValue(staffDateKey, out var item);
                        optionsByStaffDate.TryGetValue(staffDateKey, out var option);
                        dateQuotaDict.TryGetValue(dateKey, out var dateSummary);

                        QuotaDayoffRosterCellDto cell = new QuotaDayoffRosterCellDto();
                        cell.date = dateKey;
                        cell.week_day = WeekDayText(dt);
                        cell.day_type = dayType;
                        cell.option_guid = "";
                        cell.is_virtual = "true";
                        cell.selected_type = "NONE";
                        cell.quota_used = "0";
                        cell.quota_dayoff_type = "";
                        cell.can_select_full = "false";
                        cell.can_select_half_am = "false";
                        cell.can_select_half_pm = "false";
                        cell.can_select = "false";
                        cell.can_cancel = "false";
                        cell.block_reason = "";
                        cell.display_type = "EMPTY";
                        cell.display_text = "-";

                        if (HasSchedule(item))
                        {
                            cell.display_type = "SCHEDULE";
                            cell.display_text = item?.workShiftRequirement?.shift_type ?? "班";
                            cell.block_reason = "該日已有排班，不可選擇應休排休";
                            row.cells.Add(cell);
                            continue;
                        }

                        if (option != null)
                        {
                            option.NormalizeSelection();

                            string selectedType = SelectedType(option);

                            cell.option_guid = option.GUID;
                            cell.is_virtual = "false";
                            cell.selected_type = selectedType;
                            cell.quota_used = option.quota_used ?? "0";
                            cell.quota_dayoff_type = option.quota_dayoff_type ?? "";
                            cell.can_cancel =
                                (option.is_quota_dayoff == "true" && selectedType != "NONE")
                                ? "true"
                                : "false";

                            if (option.is_force_ff == "true")
                            {
                                cell.display_type = "FORCE_FF";
                                cell.display_text = "FF";
                                cell.block_reason = "系統強制放假";
                                row.cells.Add(cell);
                                continue;
                            }

                            if (option.is_released == "true")
                            {
                                cell.display_type = "RELEASED";
                                cell.display_text = option.released_dayoff_type ?? "釋";
                                cell.block_reason = "已釋出中的 option 不可作為應休排休操作";
                                row.cells.Add(cell);
                                continue;
                            }

                            if ((option.dayoff_source_type ?? "").Trim().ToUpper() == "HOLE_FILL")
                            {
                                cell.display_type = "HOLE_FILL";
                                cell.display_text = selectedType == "NONE" ? "洞" : selectedType;
                                cell.block_reason = "填洞休假不可作為應休排休操作";
                                row.cells.Add(cell);
                                continue;
                            }

                            if (option.is_quota_dayoff == "true" && selectedType != "NONE")
                            {
                                cell.display_type = "QUOTA_SELECTED";
                                cell.display_text = selectedType;
                                row.cells.Add(cell);
                                continue;
                            }

                            if (option.is_forbidden == "true")
                            {
                                cell.display_type = "BLOCKED";
                                cell.display_text = "-";
                                cell.block_reason = "此 option 已被禁止操作";
                                row.cells.Add(cell);
                                continue;
                            }
                        }

                        int amUsed = 0;
                        int pmUsed = 0;
                        int amMax = 0;
                        int pmMax = 0;

                        if (dateSummary != null)
                        {
                            amUsed = dateSummary.am_used_count.StringToInt32();
                            pmUsed = dateSummary.pm_used_count.StringToInt32();
                            amMax = dateSummary.am_max_dayoff_count.StringToInt32();
                            pmMax = dateSummary.pm_max_dayoff_count.StringToInt32();
                        }

                        bool AllowByAM(int add) => (amUsed + add) <= amMax;
                        bool AllowByPM(int add) => (pmUsed + add) <= pmMax;

                        bool canFull = false;
                        bool canHalfAM = false;
                        bool canHalfPM = false;

                        if (dayType == "WEEKDAY")
                        {
                            canFull = quotaRemaining >= 1 && AllowByAM(1) && AllowByPM(1);
                            canHalfAM = quotaRemaining >= 0.5 && AllowByAM(1);
                            canHalfPM = quotaRemaining >= 0.5 && AllowByPM(1) && pmHalfUsedCount < pmHalfLimitCount;
                        }
                        else if (dayType == "SATURDAY")
                        {
                            canFull = false;
                            canHalfPM = false;
                            canHalfAM =
                                quotaRemaining >= 0.5 &&
                                AllowByAM(1) &&
                                saturdayUsedCount < saturdayLimitCount;
                        }
                        else if (dayType == "SUNDAY")
                        {
                            canFull = quotaRemaining >= 1 && AllowByAM(1) && AllowByPM(1);
                            canHalfAM = false;
                            canHalfPM = false;
                        }

                        cell.can_select_full = canFull ? "true" : "false";
                        cell.can_select_half_am = canHalfAM ? "true" : "false";
                        cell.can_select_half_pm = canHalfPM ? "true" : "false";

                        bool anyCanSelect = canFull || canHalfAM || canHalfPM;
                        cell.can_select = anyCanSelect ? "true" : "false";

                        if (anyCanSelect)
                        {
                            cell.display_type = "AVAILABLE";
                            cell.display_text = "";
                        }
                        else
                        {
                            cell.display_type = "BLOCKED";
                            cell.display_text = "-";

                            if (quotaRemaining < 0.5)
                            {
                                cell.block_reason = "剩餘應休額度不足";
                            }
                            else if (dayType == "SATURDAY" && saturdayUsedCount >= saturdayLimitCount)
                            {
                                cell.block_reason = $"週六排休已達上限 {saturdayLimitCount} 次";
                            }
                            else if (dayType == "WEEKDAY" && pmHalfUsedCount >= pmHalfLimitCount && !AllowByAM(1))
                            {
                                cell.block_reason = "當日上午休假名額已滿";
                            }
                            else if (dayType == "WEEKDAY" && pmHalfUsedCount >= pmHalfLimitCount)
                            {
                                cell.block_reason = $"下午半日排休已達上限 {pmHalfLimitCount} 次";
                            }
                            else if (!AllowByAM(1) && !AllowByPM(1))
                            {
                                cell.block_reason = "當日休假名額已滿";
                            }
                            else if (!AllowByAM(1))
                            {
                                cell.block_reason = "當日上午休假名額已滿";
                            }
                            else if (!AllowByPM(1))
                            {
                                cell.block_reason = "當日下午休假名額已滿";
                            }
                        }

                        row.cells.Add(cell);
                    }

                    response.rows.Add(row);
                }

                returnData.Code = 200;
                returnData.Result = "取得應休排休總表成功";
                returnData.Data = response;
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
            finally
            {
                returnData.TimeTaken = timer.ToString();
            }
        }

        /// <summary>
        /// 下載排休月曆 PDF
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於產生「排休月曆 PDF」。
        /// 版型為月曆格式，一格代表一天，格內顯示當日休假人員。
        ///
        /// ## 顯示規則
        /// - 整日休假：顯示簡名，例如：邵
        /// - 上午休假：顯示簡名 + AM，例如：邵AM
        /// - 下午休假：顯示簡名 + PM，例如：邵PM
        /// - 同一天多人休假以「、」分隔，例如：邵AM、李PM、呂、湯AM
        ///
        /// ## 排除規則
        /// 以下資料不顯示：
        /// - is_released = true
        /// - is_force_ff = true
        /// - dayoff_source_type = NATIONAL_HOLIDAY
        /// - dayoff_source_type = FORCE_FF
        /// - item.selected_dayoff_type = FF
        /// - item.selected_dayoff_type = NH
        ///
        /// ## Request JSON 範例
        /// ```json
        /// {
        ///   "ValueAry": [
        ///     "form_name=2026-04"
        ///   ]
        /// }
        /// ```
        ///
        /// ## 成功回傳
        /// PDF 檔案串流。
        ///
        /// </remarks>
        /// <param name="returnData">returnData，ValueAry 需包含 form_name。</param>
        /// <returns>PDF 檔案串流。</returns>
        [HttpPost("download_dayoff_calendar_pdf")]
        public IActionResult download_dayoff_calendar_pdf([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "download_dayoff_calendar_pdf";

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry?
                        .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                        ?.Split('=')[1];

                string form_name = GetVal("form_name") ?? "";

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 form_name";
                    return new JsonResult(returnData);
                }

                var sql_form = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_day = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_item = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
                var sql_option = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

                object[] obj_form = sql_form.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();
                if (obj_form == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return new JsonResult(returnData);
                }

                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();

                List<DayOffScheduleDayClass> days = sql_day
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleDayClass>()
                    .Where(x => x != null)
                    .OrderBy(x => x.date.StringToDateTime())
                    .ToList();

                if (days.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"表單({form_name})沒有日期資料";
                    return new JsonResult(returnData);
                }

                List<DayOffScheduleItemClass> items = sql_item
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleItemClass>()
                    .Where(x => x != null)
                    .ToList();

                List<StaffDayOffOptionClass> options = sql_option
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<StaffDayOffOptionClass>()
                    .Where(x => x != null)
                    .ToList();

                DateTime firstFormDate = days.First().date.StringToDateTime();
                int year = firstFormDate.Year;
                int month = firstFormDate.Month;

                DateTime firstDayOfMonth = new DateTime(year, month, 1);
                DateTime lastDayOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month));

                Dictionary<string, DayOffScheduleItemClass> itemByGuid = items
                    .Where(x => x != null && !x.GUID.StringIsEmpty())
                    .GroupBy(x => x.GUID)
                    .ToDictionary(g => g.Key, g => g.First());

                Dictionary<string, string> staffSimpleNameDict = items
                    .Where(x => x != null && !x.staff_guid.StringIsEmpty())
                    .GroupBy(x => x.staff_guid)
                    .ToDictionary(
                        g => g.Key,
                        g =>
                        {
                            string simple = g.First().staff_simple_name;
                            if (!simple.StringIsEmpty()) return simple;

                            string name = g.First().staff_name;
                            if (!name.StringIsEmpty()) return name.Substring(name.Length - 1, 1);

                            return "";
                        });

                try
                {
                    List<StaffClass> staffClasses = staff.GetStaffs(new List<string>() { "pageSize=10000" }).staffClasses;

                    foreach (var st in staffClasses)
                    {
                        if (st == null) continue;
                        if (st.GUID.StringIsEmpty()) continue;
                        if (staffSimpleNameDict.ContainsKey(st.GUID)) continue;

                        string simple = st.staff_simple_name;
                        if (simple.StringIsEmpty() && !st.staff_name.StringIsEmpty())
                        {
                            simple = st.staff_name.Substring(st.staff_name.Length - 1, 1);
                        }

                        staffSimpleNameDict[st.GUID] = simple;
                    }
                }
                catch
                {
                }

                bool IsTrue(string value)
                {
                    return (value ?? "").Trim().ToLower() == "true";
                }

                bool HasSelectedDayoff(StaffDayOffOptionClass opt)
                {
                    if (opt == null) return false;

                    opt.NormalizeSelection();

                    return IsTrue(opt.selected_full) ||
                           IsTrue(opt.selected_half_am) ||
                           IsTrue(opt.selected_half_pm);
                }

                string GetDayoffSuffix(StaffDayOffOptionClass opt)
                {
                    if (opt == null) return "";

                    opt.NormalizeSelection();

                    if (IsTrue(opt.selected_full)) return "";
                    if (IsTrue(opt.selected_half_am)) return "AM";
                    if (IsTrue(opt.selected_half_pm)) return "PM";

                    return "";
                }

                bool ShouldExcludeOption(StaffDayOffOptionClass opt)
                {
                    if (opt == null) return true;

                    string sourceType = (opt.dayoff_source_type ?? "").Trim().ToUpper();

                    if (IsTrue(opt.is_released)) return true;
                    if (IsTrue(opt.is_force_ff)) return true;

                    if (sourceType == "NATIONAL_HOLIDAY") return true;
                    if (sourceType == "FORCE_FF") return true;

                    if (!opt.item_guid.StringIsEmpty() && itemByGuid.TryGetValue(opt.item_guid, out var item))
                    {
                        string selectedDayoffType = (item.selected_dayoff_type ?? "").Trim().ToUpper();

                        if (selectedDayoffType == "FF") return true;
                        if (selectedDayoffType == "NH") return true;
                    }

                    return false;
                }

                Dictionary<string, List<string>> dayoffTextByDate = new Dictionary<string, List<string>>();

                foreach (var opt in options)
                {
                    if (opt == null) continue;
                    if (!HasSelectedDayoff(opt)) continue;
                    if (ShouldExcludeOption(opt)) continue;

                    DateTime optDate = opt.date.StringToDateTime();
                    if (optDate == DateTime.MinValue) continue;
                    if (optDate < firstDayOfMonth || optDate > lastDayOfMonth) continue;

                    staffSimpleNameDict.TryGetValue(opt.staff_guid, out string simpleName);

                    if (simpleName.StringIsEmpty())
                    {
                        simpleName = "未知";
                    }

                    string displayText = $"{simpleName}{GetDayoffSuffix(opt)}";
                    string dateKey = optDate.ToDateString('-');

                    if (!dayoffTextByDate.ContainsKey(dateKey))
                    {
                        dayoffTextByDate[dateKey] = new List<string>();
                    }

                    if (!dayoffTextByDate[dateKey].Contains(displayText))
                    {
                        dayoffTextByDate[dateKey].Add(displayText);
                    }
                }

                foreach (var key in dayoffTextByDate.Keys.ToList())
                {
                    dayoffTextByDate[key] = dayoffTextByDate[key]
                        .OrderBy(x => x)
                        .ToList();
                }

                int offsetToMonday = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;
                DateTime firstMonday = firstDayOfMonth.AddDays(-offsetToMonday);

                int offsetToSunday = 7 - ((int)lastDayOfMonth.DayOfWeek + 6) % 7 - 1;
                DateTime lastSunday = lastDayOfMonth.AddDays(offsetToSunday);

                SheetClass sheet = new SheetClass();

                if ((lastSunday - firstMonday).Days <= 35)
                {
                    sheet = monthly_shift_schedule_5_week_excel.xlsx.JsonDeserializet<SheetClass>();
                }
                else
                {
                    sheet = monthly_shift_schedule_6_week_excel.xlsx.JsonDeserializet<SheetClass>();
                }

                sheet.Rows[0].Cell[0].Text = $"{year}-{month}月排休表";

                int weekIndex = 1;

                for (DateTime d = firstMonday; d <= lastSunday; d = d.AddDays(1))
                {
                    int dayOfWeek = ((int)d.DayOfWeek + 6) % 7 + 1;

                    if (dayOfWeek == 1 && d > firstMonday)
                    {
                        weekIndex++;
                    }

                    int baseRow = 2 + (weekIndex - 1) * 7;
                    int col = dayOfWeek - 1;

                    string dateKey = d.ToDateString('-');

                    string headerText = d.Day.ToString();

                    if (d.Month != month)
                    {
                        sheet.Rows[baseRow].Cell[col].Text = headerText;
                        ClearCalendarBodyRows(sheet, baseRow, col);
                        continue;
                    }

                    sheet.Rows[baseRow].Cell[col].Text = headerText;

                    List<string> names = new List<string>();
                    if (dayoffTextByDate.TryGetValue(dateKey, out var list))
                    {
                        names = list;
                    }

                    string bodyText = names.Count > 0 ? string.Join("、", names) : "";

                    ClearCalendarBodyRows(sheet, baseRow, col);

                    // 節省空間：全部寫在同一格，用「、」分隔
                    sheet.Rows[baseRow + 1].Cell[col].Text = bodyText;
                }

                byte[] bytes_pdf = sheet.SaveToPDF(PdfSharp.PageSize.A4, PdfSharp.PageOrientation.Landscape);

                Stream stream = new MemoryStream(bytes_pdf);
                string contentType = "application/octet-stream";
                string originalName = $"dayoff_calendar_{form_name}.pdf";
                string utf8FileName = Uri.EscapeDataString(originalName);

                Response.Headers.Add("Content-Disposition", $"attachment; filename=\"{originalName}\"; filename*=UTF-8''{utf8FileName}");
                Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition, Content-Length, Content-Type");

                return File(stream, contentType);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"例外：{ex.Message}";
                return new JsonResult(returnData);
            }
        }
        /// <summary>
        /// 清空月曆日期格內的內容列，只保留日期列。
        /// baseRow 為日期列，例如第 2, 9, 16... 列。
        /// </summary>
        private void ClearCalendarBodyRows(SheetClass sheet, int baseRow, int col)
        {
            if (sheet == null) return;

            for (int r = baseRow + 1; r <= baseRow + 6; r++)
            {
                if (r < 0) continue;
                if (r >= sheet.Rows.Count) continue;
                if (col < 0) continue;
                if (col >= sheet.Rows[r].Cell.Count) continue;

                sheet.Rows[r].Cell[col].Text = "";
            }
        }

        #region export_dayoff_status_excel
        /// <summary>
        /// 匯出整個排休表的排休狀態 Excel
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於匯出指定排休表單的「整體排休狀態總表 Excel」。
        ///
        /// 匯出格式為「人員 × 日期」總表：
        /// - 每列代表一位人員
        /// - 每欄代表一個日期
        /// - 日期前會附加摘要欄位
        ///
        /// 可用於：
        /// - 主管檢視整體排休狀態
        /// - 排休核對
        /// - 匯出留存
        ///
        /// ---
        ///
        /// ## 🌐 URL
        /// ```text
        /// /phar_roster_api/dayOffSchedule/export_dayoff_status_excel
        /// ```
        ///
        /// ## Method
        /// ```text
        /// POST
        /// ```
        ///
        /// ## Content-Type
        /// ```text
        /// application/json
        /// ```
        ///
        /// ---
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "export_dayoff_status_excel",
        ///   "ValueAry": [
        ///     "form_name=2026-05 排休表"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ---
        ///
        /// ## 🔍 參數說明
        /// | 參數名稱 | 類型 | 必填 | 說明 |
        /// |------|------|------|------|
        /// | form_name | string | ✅ | 排休表單名稱 |
        ///
        /// ---
        ///
        /// ## 📑 匯出內容
        /// 匯出格式為「人員 × 日期」總表，欄位如下：
        ///
        /// | 工號 | 姓名 | 簡名 | 應休總額度 | 已用應休額度 | 剩餘應休額度 | 週六已用次數 | 下午已用次數 | 01 | 02 | 03 | ... |
        /// |------|------|------|------|------|------|------|------|------|------|------|------|
        ///
        /// ---
        ///
        /// ## 📝 每格顯示規則
        /// 若同一人同一天有多種狀態，顯示優先順序如下：
        ///
        /// 1. 強制休假
        ///    - FF
        ///    - NH
        /// 2. 已釋出
        ///    - 釋出-整日
        ///    - 釋出-AM
        ///    - 釋出-PM
        /// 3. 應休排休
        ///    - QUOTA-FULL
        ///    - QUOTA-AM
        ///    - QUOTA-PM
        /// 4. 一般已選休假
        ///    - 整日
        ///    - 上午
        ///    - 下午
        ///
        /// 若該日無任何休假狀態，顯示空白。
        ///
        /// ---
        ///
        /// ## 📌 匯出範圍規則
        /// 1. 僅匯出該表單內 `dayoff_schedule_item` 出現過的人員。
        /// 2. 狀態計算同時納入：
        ///    - item 對應 option
        ///    - 額外 quota option（即使沒有 item）
        /// 3. 已釋出狀態顯示在原持有人當天格子。
        /// 4. 週六已用次數、下午已用次數僅統計 QUOTA 類型。
        /// 5. 日期表頭會標示：
        ///    - 星期六：`dd(六)`
        ///    - 星期日：`dd(日)`
        ///    - 國定假日：`dd(國)`
        ///    - 若同時為週末與國定假日，以國定假日優先顯示。
        ///
        /// ---
        ///
        /// ## 📤 Response 說明（成功）
        /// 成功時回傳 Excel 檔案串流。
        ///
        /// ### 檔名格式
        /// ```text
        /// 排休狀態總表_{form_name}.xlsx
        /// ```
        ///
        /// ### Header 範例
        /// ```text
        /// Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
        /// Content-Disposition: attachment; filename="dayoff_status.xlsx"; filename*=UTF-8''%E6%8E%92%E4%BC%91%E7%8B%80%E6%85%8B%E7%B8%BD%E8%A1%A8_2026-05%20%E6%8E%92%E4%BC%91%E8%A1%A8.xlsx
        /// ```
        ///
        /// ---
        ///
        /// ## ❌ Response JSON 範例（錯誤）
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "export_dayoff_status_excel",
        ///   "Result": "找不到表單名稱(2026-05 排休表)"
        /// }
        /// ```
        ///
        /// ---
        ///
        /// ## 📌 注意事項
        /// - 本 API 只做匯出，不修改資料。
        /// - 若 form_name 不存在，直接回傳錯誤。
        /// - 匯出資料以表單內資料為準。
        /// </remarks>
        /// <param name="returnData">封裝 API 請求內容，需於 ValueAry 傳入 form_name。</param>
        /// <returns>成功時回傳 Excel 檔案串流，失敗時回傳 JSON 錯誤訊息。</returns>
        [HttpPost("export_dayoff_status_excel")]
        public IActionResult export_dayoff_status_excel([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "export_dayoff_status_excel";

            try
            {
                init(returnData);

                string GetVal(string key) =>
                    returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string form_name = GetVal("form_name");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 form_name";
                    return new JsonResult(returnData);
                }

                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

                object[] obj_form = sql_dayOffScheduleFormClass.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();
                if (obj_form == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return new JsonResult(returnData);
                }

                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();

                List<DayOffScheduleDayClass> days = sql_dayOffScheduleDayClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleDayClass>()
                    .Where(x => x != null)
                    .OrderBy(x => x.date.StringToDateTime())
                    .ToList();

                List<DayOffScheduleItemClass> items = sql_dayOffScheduleItemClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleItemClass>()
                    .Where(x => x != null)
                    .ToList();

                List<StaffDayOffOptionClass> options = sql_staffDayOffOptionClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<StaffDayOffOptionClass>()
                    .Where(x => x != null)
                    .ToList();

                // 只列出這張表 item 內出現過的人
                var staffRows = items
                    .Where(x => !x.staff_guid.StringIsEmpty())
                    .GroupBy(x => x.staff_guid)
                    .Select(g =>
                    {
                        DayOffScheduleItemClass first = g.First();
                        return new
                        {
                            staff_guid = g.Key,
                            staff_id = first.staff_id ?? "",
                            staff_name = first.staff_name ?? "",
                            staff_simple_name = first.staff_simple_name ?? ""
                        };
                    })
                    .OrderBy(x => x.staff_id)
                    .ThenBy(x => x.staff_name)
                    .ToList();

                Dictionary<string, List<DayOffScheduleItemClass>> itemsByStaffDate = items
                    .GroupBy(x => $"{x.staff_guid}|{x.date.StringToDateTime().ToString("yyyy-MM-dd")}")
                    .ToDictionary(g => g.Key, g => g.ToList());

                Dictionary<string, List<StaffDayOffOptionClass>> optionsByStaffDate = options
                    .GroupBy(x => $"{x.staff_guid}|{x.date.StringToDateTime().ToString("yyyy-MM-dd")}")
                    .ToDictionary(g => g.Key, g => g.ToList());

                XSSFWorkbook workbook = new XSSFWorkbook();
                ISheet sheet = workbook.CreateSheet("排休狀態總表");

                ICellStyle titleStyle = CreateExportTitleStyle(workbook);
                ICellStyle headerStyle = CreateExportHeaderStyle(workbook);
                ICellStyle saturdayHeaderStyle = CreateExportColoredHeaderStyle(workbook, HSSFColor.LightCornflowerBlue.Index);
                ICellStyle sundayHeaderStyle = CreateExportColoredHeaderStyle(workbook, HSSFColor.Rose.Index);
                ICellStyle holidayHeaderStyle = CreateExportColoredHeaderStyle(workbook, HSSFColor.LightYellow.Index);
                ICellStyle normalStyle = CreateExportNormalStyle(workbook);
                ICellStyle centerStyle = CreateExportCenterStyle(workbook);

                int rowIndex = 0;

                // 標題列
                IRow titleRow = sheet.CreateRow(rowIndex++);
                titleRow.HeightInPoints = 24;
                ICell titleCell = titleRow.CreateCell(0);
                titleCell.SetCellValue($"排休狀態總表 - {form.form_name}");
                titleCell.CellStyle = titleStyle;

                int totalColumns = 8 + days.Count;
                sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, totalColumns - 1));

                // 表頭列
                IRow headerRow = sheet.CreateRow(rowIndex++);
                string[] fixedHeaders = new string[]
                {
            "工號",
            "姓名",
            "簡名",
            "應休總額度",
            "已用應休額度",
            "剩餘應休額度",
            "週六已用次數",
            "下午已用次數"
                };

                for (int i = 0; i < fixedHeaders.Length; i++)
                {
                    ICell cell = headerRow.CreateCell(i);
                    cell.SetCellValue(fixedHeaders[i]);
                    cell.CellStyle = headerStyle;
                }

                for (int i = 0; i < days.Count; i++)
                {
                    DateTime dt = days[i].date.StringToDateTime();
                    ICell cell = headerRow.CreateCell(i + fixedHeaders.Length);

                    bool isHoliday = IsNationalHolidayColumn(days[i], items, options);
                    bool isSaturday = dt.DayOfWeek == DayOfWeek.Saturday;
                    bool isSunday = dt.DayOfWeek == DayOfWeek.Sunday;

                    string headerText = dt.ToString("dd");

                    if (isHoliday)
                    {
                        headerText += "(國)";
                        cell.CellStyle = holidayHeaderStyle;
                    }
                    else if (isSunday)
                    {
                        headerText += "(日)";
                        cell.CellStyle = sundayHeaderStyle;
                    }
                    else if (isSaturday)
                    {
                        headerText += "(六)";
                        cell.CellStyle = saturdayHeaderStyle;
                    }
                    else
                    {
                        cell.CellStyle = headerStyle;
                    }

                    cell.SetCellValue(headerText);
                }

                // 資料列
                foreach (var staffRow in staffRows)
                {
                    IRow row = sheet.CreateRow(rowIndex++);

                    StaffQuotaExportSummary summary = BuildStaffQuotaExportSummary(staffRow.staff_guid, options);

                    SetCell(row, 0, staffRow.staff_id, centerStyle);
                    SetCell(row, 1, staffRow.staff_name, normalStyle);
                    SetCell(row, 2, staffRow.staff_simple_name, centerStyle);
                    SetCell(row, 3, summary.quota_total, centerStyle);
                    SetCell(row, 4, summary.quota_used_total, centerStyle);
                    SetCell(row, 5, summary.quota_remaining, centerStyle);
                    SetCell(row, 6, summary.saturday_used_count, centerStyle);
                    SetCell(row, 7, summary.pm_used_count, centerStyle);

                    for (int i = 0; i < days.Count; i++)
                    {
                        string dateKey = days[i].date.StringToDateTime().ToString("yyyy-MM-dd");
                        string key = $"{staffRow.staff_guid}|{dateKey}";

                        List<DayOffScheduleItemClass> dayItems =
                            itemsByStaffDate.ContainsKey(key) ? itemsByStaffDate[key] : new List<DayOffScheduleItemClass>();

                        List<StaffDayOffOptionClass> dayOptions =
                            optionsByStaffDate.ContainsKey(key) ? optionsByStaffDate[key] : new List<StaffDayOffOptionClass>();

                        string displayText = ResolveDayoffExportDisplayText(dayItems, dayOptions);
                        SetCell(row, i + fixedHeaders.Length, displayText, centerStyle);
                    }
                }

                // 欄寬
                sheet.SetColumnWidth(0, 12 * 256); // 工號
                sheet.SetColumnWidth(1, 16 * 256); // 姓名
                sheet.SetColumnWidth(2, 10 * 256); // 簡名
                sheet.SetColumnWidth(3, 12 * 256); // 應休總額度
                sheet.SetColumnWidth(4, 12 * 256); // 已用應休額度
                sheet.SetColumnWidth(5, 12 * 256); // 剩餘應休額度
                sheet.SetColumnWidth(6, 12 * 256); // 週六已用次數
                sheet.SetColumnWidth(7, 12 * 256); // 下午已用次數

                for (int i = 0; i < days.Count; i++)
                {
                    sheet.SetColumnWidth(i + 8, 12 * 256);
                }

                sheet.CreateFreezePane(8, 2);

                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    workbook.Write(ms);
                    bytes = ms.ToArray();
                }

                var stream = new MemoryStream(bytes);
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                string downloadFileName = "dayoff_status.xlsx";
                string displayFileName = $"排休狀態總表_{form.form_name}.xlsx";
                string utf8FileName = Uri.EscapeDataString(displayFileName);

                Response.Headers["Content-Disposition"] =
                    $"attachment; filename=\"{downloadFileName}\"; filename*=UTF-8''{utf8FileName}";
                Response.Headers["Access-Control-Expose-Headers"] =
                    "Content-Disposition, Content-Length, Content-Type";

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
        /// 匯出用：單一人員摘要
        /// </summary>
        private class StaffQuotaExportSummary
        {
            public string quota_total { get; set; } = "0";
            public string quota_used_total { get; set; } = "0";
            public string quota_remaining { get; set; } = "0";
            public string saturday_used_count { get; set; } = "0";
            public string pm_used_count { get; set; } = "0";
        }
        /// <summary>
        /// 建立單一人員的 quota 摘要
        /// </summary>
        private StaffQuotaExportSummary BuildStaffQuotaExportSummary(string staff_guid, List<StaffDayOffOptionClass> options)
        {
            StaffQuotaExportSummary result = new StaffQuotaExportSummary();

            if (staff_guid.StringIsEmpty()) return result;
            if (options == null) return result;

            double quotaTotal = 0;
            double quotaUsedTotal = 0;
            int saturdayUsedCount = 0;
            int pmUsedCount = 0;

            List<StaffDayOffOptionClass> staffOptions = options
                .Where(x => x != null && x.staff_guid == staff_guid)
                .ToList();

            foreach (StaffDayOffOptionClass option in staffOptions)
            {
                option.NormalizeSelection();

                if (option.is_released == "true")
                {
                    string releasedType = (option.released_dayoff_type ?? "").Trim().ToUpper();
                    if (releasedType == "FULL") quotaTotal += 1;
                    else if (releasedType == "HALF_AM" || releasedType == "HALF_PM") quotaTotal += 0.5;
                }

                if (option.is_any_date == "true")
                {
                    quotaTotal += 1;
                }

                if (option.is_quota_dayoff == "true")
                {
                    double used = 0;
                    double.TryParse(option.quota_used, out used);
                    quotaUsedTotal += used;

                    string quotaType = (option.quota_dayoff_type ?? "").Trim().ToUpper();
                    if (quotaType == "SATURDAY_HALF_AM")
                    {
                        saturdayUsedCount++;
                    }
                    if (quotaType == "WEEKDAY_HALF_PM")
                    {
                        pmUsedCount++;
                    }
                }
            }

            double remaining = quotaTotal - quotaUsedTotal;
            if (remaining < 0) remaining = 0;

            result.quota_total = quotaTotal.ToString("0.##");
            result.quota_used_total = quotaUsedTotal.ToString("0.##");
            result.quota_remaining = remaining.ToString("0.##");
            result.saturday_used_count = saturdayUsedCount.ToString();
            result.pm_used_count = pmUsedCount.ToString();

            return result;
        }
        /// <summary>
        /// 解析單一人員單一天的匯出顯示文字
        /// 優先順序：強制休假 > 釋出 > QUOTA > 一般已選
        /// </summary>
        private string ResolveDayoffExportDisplayText(
            List<DayOffScheduleItemClass> dayItems,
            List<StaffDayOffOptionClass> dayOptions)
        {
            if (dayItems == null) dayItems = new List<DayOffScheduleItemClass>();
            if (dayOptions == null) dayOptions = new List<StaffDayOffOptionClass>();

            foreach (DayOffScheduleItemClass item in dayItems)
            {
                string selectedType = (item.selected_dayoff_type ?? "").Trim().ToUpper();
                if (selectedType == "FF") return "FF";
                if (selectedType == "NH") return "NH";
            }

            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                string sourceType = (option.dayoff_source_type ?? "").Trim().ToUpper();
                if (option.is_force_ff == "true")
                {
                    if (sourceType == "NATIONAL_HOLIDAY") return "NH";
                    return "FF";
                }
                if (sourceType == "NATIONAL_HOLIDAY") return "NH";
            }

            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                if (option.is_released == "true")
                {
                    string releasedType = (option.released_dayoff_type ?? "").Trim().ToUpper();
                    if (releasedType == "FULL") return "釋出-整日";
                    if (releasedType == "HALF_AM") return "釋出-AM";
                    if (releasedType == "HALF_PM") return "釋出-PM";
                    return "釋出";
                }
            }

            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                if (option.is_quota_dayoff == "true")
                {
                    if (option.selected_full == "true") return "應休-整日";
                    if (option.selected_half_am == "true") return "應休-AM";
                    if (option.selected_half_pm == "true") return "應休-PM";
                }
            }

            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                if (option.selected_full == "true") return "整日";
                if (option.selected_half_am == "true") return "上午";
                if (option.selected_half_pm == "true") return "下午";
            }

            return "";
        }
        /// <summary>
        /// 判斷該日期欄是否為國定假日
        /// 規則：只要當天任一人有 NH，或 option 的 dayoff_source_type = NATIONAL_HOLIDAY，即視為國定假日
        /// </summary>
        private bool IsNationalHolidayColumn(
            DayOffScheduleDayClass day,
            List<DayOffScheduleItemClass> items,
            List<StaffDayOffOptionClass> options)
        {
            if (day == null) return false;

            string dateKey = day.date.StringToDateTime().ToString("yyyy-MM-dd");

            bool hasNHItem = items.Any(x =>
                x != null &&
                x.date.StringToDateTime().ToString("yyyy-MM-dd") == dateKey &&
                (x.selected_dayoff_type ?? "").Trim().ToUpper() == "NH");

            if (hasNHItem) return true;

            bool hasNHOption = options.Any(x =>
                x != null &&
                x.date.StringToDateTime().ToString("yyyy-MM-dd") == dateKey &&
                ((x.dayoff_source_type ?? "").Trim().ToUpper() == "NATIONAL_HOLIDAY"));

            return hasNHOption;
        }
        /// <summary>
        /// 設定儲存格內容
        /// </summary>
        private void SetCell(IRow row, int colIndex, string text, ICellStyle style)
        {
            ICell cell = row.GetCell(colIndex) ?? row.CreateCell(colIndex);
            cell.SetCellValue(text ?? "");
            if (style != null) cell.CellStyle = style;
        }
        /// <summary>
        /// 建立標題樣式
        /// </summary>
        private ICellStyle CreateExportTitleStyle(IWorkbook workbook)
        {
            IFont font = workbook.CreateFont();
            font.FontName = "微軟正黑體";
            font.FontHeightInPoints = 12;
            font.IsBold = true;

            ICellStyle style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.SetFont(font);

            return style;
        }
        /// <summary>
        /// 建立表頭樣式
        /// </summary>
        private ICellStyle CreateExportHeaderStyle(IWorkbook workbook)
        {
            IFont font = workbook.CreateFont();
            font.FontName = "微軟正黑體";
            font.FontHeightInPoints = 10;
            font.IsBold = true;

            ICellStyle style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.WrapText = true;
            style.FillForegroundColor = HSSFColor.Grey25Percent.Index;
            style.FillPattern = FillPattern.SolidForeground;
            style.SetFont(font);

            return style;
        }
        /// <summary>
        /// 建立有底色的表頭樣式
        /// </summary>
        private ICellStyle CreateExportColoredHeaderStyle(IWorkbook workbook, short fillColor)
        {
            IFont font = workbook.CreateFont();
            font.FontName = "微軟正黑體";
            font.FontHeightInPoints = 10;
            font.IsBold = true;

            ICellStyle style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.WrapText = true;
            style.FillForegroundColor = fillColor;
            style.FillPattern = FillPattern.SolidForeground;
            style.SetFont(font);

            return style;
        }
        /// <summary>
        /// 建立一般文字樣式
        /// </summary>
        private ICellStyle CreateExportNormalStyle(IWorkbook workbook)
        {
            IFont font = workbook.CreateFont();
            font.FontName = "微軟正黑體";
            font.FontHeightInPoints = 10;

            ICellStyle style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Left;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.WrapText = true;
            style.SetFont(font);

            return style;
        }
        /// <summary>
        /// 建立置中樣式
        /// </summary>
        private ICellStyle CreateExportCenterStyle(IWorkbook workbook)
        {
            IFont font = workbook.CreateFont();
            font.FontName = "微軟正黑體";
            font.FontHeightInPoints = 10;

            ICellStyle style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.WrapText = true;
            style.SetFont(font);

            return style;
        }
        #endregion

        #region export_dayoff_status_pdf

        /// <summary>
        /// 匯出整個排休表的排休狀態 PDF
        /// </summary>
        /// <remarks>
        /// 本 API 用於匯出指定排休表單的排休狀態總表 PDF。
        /// 採 A4 橫式,日期整月同頁優先顯示;若人員過多則僅做人員垂直分頁。
        /// </remarks>
        /// <param name="returnData">需於 ValueAry 傳入 form_name。</param>
        /// <returns>成功時回傳 PDF 檔案串流,失敗時回傳 JSON 錯誤訊息。</returns>
        [HttpPost("export_dayoff_status_pdf")]
        public IActionResult export_dayoff_status_pdf([FromBody] returnData returnData)
        {
            returnData.Method = "export_dayoff_status_pdf";

            try
            {
                //CustomPdfSharpFontResolver.EnsurePdfSharpFontResolver();
                GlobalFontSettings.FontResolver = new CustomFontResolver();
                init(returnData);
                string GetVal(string key) =>
                    returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string form_name = GetVal("form_name");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 form_name";
                    return new JsonResult(returnData);
                }

                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

                object[] obj_form = sql_dayOffScheduleFormClass.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();
                if (obj_form == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return new JsonResult(returnData);
                }

                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();

                List<DayOffScheduleDayClass> days = sql_dayOffScheduleDayClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleDayClass>()
                    .Where(x => x != null)
                    .OrderBy(x => x.date.StringToDateTime())
                    .ToList();

                List<DayOffScheduleItemClass> items = sql_dayOffScheduleItemClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleItemClass>()
                    .Where(x => x != null)
                    .ToList();

                List<StaffDayOffOptionClass> options = sql_staffDayOffOptionClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<StaffDayOffOptionClass>()
                    .Where(x => x != null)
                    .ToList();

                var staffRows = items
                    .Where(x => !x.staff_guid.StringIsEmpty())
                    .GroupBy(x => x.staff_guid)
                    .Select(g =>
                    {
                        DayOffScheduleItemClass first = g.First();
                        return new ExportDayoffStaffRowPdf
                        {
                            staff_guid = g.Key,
                            staff_id = first.staff_id ?? "",
                            staff_name = first.staff_name ?? ""
                        };
                    })
                    .OrderBy(x => x.staff_id)
                    .ThenBy(x => x.staff_name)
                    .ToList();

                Dictionary<string, List<DayOffScheduleItemClass>> itemsByStaffDate = items
                    .GroupBy(x => $"{x.staff_guid}|{x.date.StringToDateTime():yyyy-MM-dd}")
                    .ToDictionary(g => g.Key, g => g.ToList());

                Dictionary<string, List<StaffDayOffOptionClass>> optionsByStaffDate = options
                    .GroupBy(x => $"{x.staff_guid}|{x.date.StringToDateTime():yyyy-MM-dd}")
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (ExportDayoffStaffRowPdf staffRow in staffRows)
                {
                    foreach (DayOffScheduleDayClass day in days)
                    {
                        string dateKey = day.date.StringToDateTime().ToString("yyyy-MM-dd");
                        string key = $"{staffRow.staff_guid}|{dateKey}";

                        List<DayOffScheduleItemClass> dayItems =
                            itemsByStaffDate.ContainsKey(key) ? itemsByStaffDate[key] : new List<DayOffScheduleItemClass>();

                        List<StaffDayOffOptionClass> dayOptions =
                            optionsByStaffDate.ContainsKey(key) ? optionsByStaffDate[key] : new List<StaffDayOffOptionClass>();

                        staffRow.dayoffTextByDate[dateKey] = ResolveDayoffExportDisplayTextPdf(dayItems, dayOptions);
                    }
                }

                PdfDocument document = new PdfDocument();
                document.Info.Title = $"排休狀態總表_{form.form_name}";

                XFont titleFont = new XFont("Noto Sans TC", 11, XFontStyleEx.Bold);
                XFont infoFont = new XFont("Noto Sans TC", 7, XFontStyleEx.Regular);
                XFont legendFont = new XFont("Noto Sans TC", 7, XFontStyleEx.Regular);

                XFont headerTopFont = new XFont("Noto Sans TC", 7, XFontStyleEx.Bold);
                XFont headerBottomFont = new XFont("Noto Sans TC", 7, XFontStyleEx.Bold);

                XFont nameFont = new XFont("Noto Sans TC", 7, XFontStyleEx.Regular);
                XFont idFont = new XFont("Noto Sans TC", 6.5, XFontStyleEx.Regular);
                XFont statusFont = new XFont("Noto Sans TC", 7, XFontStyleEx.Bold);

                double marginLeft = 10;
                double marginRight = 8;
                double marginTop = 10;
                double marginBottom = 10;

                double titleHeight = 16;
                double infoHeight = 11;
                double legendHeight = 11;
                double headerHeight = 22;
                double rowHeight = 16;

                double staffColWidth = 75;

                PdfPage measurePage = document.AddPage();
                measurePage.Size = PdfSharp.PageSize.A4;
                measurePage.Orientation = PdfSharp.PageOrientation.Landscape;

                double pageWidth = measurePage.Width.Point;
                double pageHeight = measurePage.Height.Point;

                double availableWidth = pageWidth - marginLeft - marginRight - staffColWidth;
                double dateColWidth = availableWidth / Math.Max(1, days.Count);

                if (dateColWidth < 16)
                    dateColWidth = 16;

                double totalDateWidthMin = 16 * days.Count;
                if (totalDateWidthMin > availableWidth)
                {
                    dateColWidth = availableWidth / Math.Max(1, days.Count);
                }

                int datesPerPage = days.Count;

                double usedTopHeight = titleHeight + infoHeight + legendHeight + headerHeight + 6;
                int rowsPerPage = Math.Max(1, (int)Math.Floor((pageHeight - marginTop - marginBottom - usedTopHeight) / rowHeight));
                int rowPages = Math.Max(1, (int)Math.Ceiling(staffRows.Count / (double)rowsPerPage));

                document.Pages.RemoveAt(document.Pages.Count - 1);

                for (int rowPageIndex = 0; rowPageIndex < rowPages; rowPageIndex++)
                {
                    PdfPage page = document.AddPage();
                    page.Size = PdfSharp.PageSize.A4;
                    page.Orientation = PdfSharp.PageOrientation.Landscape;

                    XGraphics gfx = XGraphics.FromPdfPage(page);

                    double x = marginLeft;
                    double y = marginTop;

                    // 標題
                    gfx.DrawString($"排休狀態總表 - {form.form_name}", titleFont, XBrushes.Black,
                        new XRect(marginLeft, y, page.Width.Point - marginLeft - marginRight, titleHeight),
                        XStringFormats.CenterLeft);
                    y += titleHeight;

                    // 資訊列
                    gfx.DrawString($"匯出時間：{DateTime.Now:yyyy-MM-dd HH:mm:ss}", infoFont, XBrushes.Black,
                        new XRect(marginLeft, y, 240, infoHeight), XStringFormats.CenterLeft);

                    gfx.DrawString($"頁碼：{rowPageIndex + 1}/{rowPages}", infoFont, XBrushes.Black,
                        new XRect(page.Width.Point - marginRight - 80, y, 80, infoHeight), XStringFormats.CenterRight);
                    y += infoHeight;

                    // 圖例
                    gfx.DrawString("圖例 FF=強制休 NH=國定假 釋整/釋上/釋下=已釋出 應整/應上/應下=應休 整/上/下=一般已選",
                        legendFont, XBrushes.Black,
                        new XRect(marginLeft, y, page.Width.Point - marginLeft - marginRight, legendHeight),
                        XStringFormats.CenterLeft);
                    y += legendHeight + 3;

                    double tableStartX = marginLeft;
                    double tableStartY = y;

                    List<ExportDayoffStaffRowPdf> currentStaffRows = staffRows
                        .Skip(rowPageIndex * rowsPerPage)
                        .Take(rowsPerPage)
                        .ToList();

                    // ========= 第一階段:先畫所有格子的背景與框線 =========

                    // 左上表頭
                    DrawPdfCellBackgroundAndBorder(gfx, tableStartX, tableStartY, staffColWidth, headerHeight, XBrushes.LightGray, true);

                    // 日期表頭背景
                    x = tableStartX + staffColWidth;
                    foreach (DayOffScheduleDayClass day in days)
                    {
                        DateTime dt = day.date.StringToDateTime();
                        bool isHoliday = IsNationalHolidayColumnPdf(day, items, options);

                        XBrush bg = XBrushes.LightGray;
                        if (isHoliday) bg = XBrushes.LightGoldenrodYellow;
                        else if (dt.DayOfWeek == DayOfWeek.Sunday) bg = XBrushes.MistyRose;
                        else if (dt.DayOfWeek == DayOfWeek.Saturday) bg = XBrushes.LightBlue;

                        DrawPdfCellBackgroundAndBorder(gfx, x, tableStartY, dateColWidth, headerHeight, bg, true);
                        x += dateColWidth;
                    }

                    // 資料列背景與框線
                    double rowY = tableStartY + headerHeight;
                    foreach (ExportDayoffStaffRowPdf staffRow in currentStaffRows)
                    {
                        DrawPdfCellBackgroundAndBorder(gfx, tableStartX, rowY, staffColWidth, rowHeight, XBrushes.White, true);

                        x = tableStartX + staffColWidth;
                        foreach (DayOffScheduleDayClass day in days)
                        {
                            string dateKey = day.date.StringToDateTime().ToString("yyyy-MM-dd");
                            string text = staffRow.dayoffTextByDate.ContainsKey(dateKey)
                                ? staffRow.dayoffTextByDate[dateKey]
                                : "";

                            XBrush cellBg = XBrushes.White;
                            if (text == "FF") cellBg = XBrushes.Gainsboro;
                            else if (text == "NH") cellBg = XBrushes.LightGoldenrodYellow;
                            else if (text.StartsWith("釋")) cellBg = XBrushes.Moccasin;
                            else if (text.StartsWith("應")) cellBg = XBrushes.LightCyan;

                            DrawPdfCellBackgroundAndBorder(gfx, x, rowY, dateColWidth, rowHeight, cellBg, true);
                            x += dateColWidth;
                        }

                        rowY += rowHeight;
                    }

                    // ========= 第二階段:再畫文字 =========

                    // 左上表頭文字
                    DrawPdfCellTwoLineText(
                        gfx,
                        tableStartX,
                        tableStartY,
                        staffColWidth,
                        headerHeight,
                        "姓名",
                        "工號",
                        headerTopFont,
                        headerBottomFont);

                    // 日期表頭文字
                    x = tableStartX + staffColWidth;
                    foreach (DayOffScheduleDayClass day in days)
                    {
                        DateTime dt = day.date.StringToDateTime();
                        bool isHoliday = IsNationalHolidayColumnPdf(day, items, options);

                        string topText = dt.ToString("dd");
                        string bottomText = BuildPdfDayHeaderShortText(dt, isHoliday);

                        DrawPdfCellTwoLineText(
                            gfx,
                            x,
                            tableStartY,
                            dateColWidth,
                            headerHeight,
                            topText,
                            bottomText,
                            headerTopFont,
                            headerBottomFont);

                        x += dateColWidth;
                    }

                    // 資料文字
                    rowY = tableStartY + headerHeight;
                    foreach (ExportDayoffStaffRowPdf staffRow in currentStaffRows)
                    {
                        DrawPdfCellTwoLineText(
                            gfx,
                            tableStartX,
                            rowY,
                            staffColWidth,
                            rowHeight,
                            staffRow.staff_name,
                            staffRow.staff_id,
                            nameFont,
                            idFont);

                        x = tableStartX + staffColWidth;
                        foreach (DayOffScheduleDayClass day in days)
                        {
                            string dateKey = day.date.StringToDateTime().ToString("yyyy-MM-dd");
                            string text = staffRow.dayoffTextByDate.ContainsKey(dateKey)
                                ? staffRow.dayoffTextByDate[dateKey]
                                : "";

                            DrawPdfCellSingleLineText(
                                gfx,
                                x,
                                rowY,
                                dateColWidth,
                                rowHeight,
                                text,
                                statusFont);

                            x += dateColWidth;
                        }

                        rowY += rowHeight;
                    }
                }

                byte[] pdfBytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    document.Save(ms, false);
                    pdfBytes = ms.ToArray();
                }

                Stream stream = new MemoryStream(pdfBytes);
                string contentType = "application/pdf";

                string downloadFileName = "dayoff_status.pdf";
                string displayFileName = $"排休狀態總表_{form.form_name}.pdf";
                string utf8FileName = Uri.EscapeDataString(displayFileName);

                Response.Headers["Content-Disposition"] =
                    $"attachment; filename=\"{downloadFileName}\"; filename*=UTF-8''{utf8FileName}";
                Response.Headers["Access-Control-Expose-Headers"] =
                    "Content-Disposition, Content-Length, Content-Type";

                return File(stream, contentType);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return new JsonResult(returnData);
            }
        }

        private class ExportDayoffStaffRowPdf
        {
            public string staff_guid { get; set; }
            public string staff_id { get; set; }
            public string staff_name { get; set; }
            public Dictionary<string, string> dayoffTextByDate { get; set; } = new Dictionary<string, string>();
        }

        private string ResolveDayoffExportDisplayTextPdf(
            List<DayOffScheduleItemClass> dayItems,
            List<StaffDayOffOptionClass> dayOptions)
        {
            if (dayItems == null) dayItems = new List<DayOffScheduleItemClass>();
            if (dayOptions == null) dayOptions = new List<StaffDayOffOptionClass>();

            foreach (DayOffScheduleItemClass item in dayItems)
            {
                string selectedType = (item.selected_dayoff_type ?? "").Trim().ToUpper();
                if (selectedType == "FF") return "FF";
                if (selectedType == "NH") return "NH";
            }

            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                string sourceType = (option.dayoff_source_type ?? "").Trim().ToUpper();

                if (option.is_force_ff == "true")
                {
                    if (sourceType == "NATIONAL_HOLIDAY") return "NH";
                    return "FF";
                }

                if (sourceType == "NATIONAL_HOLIDAY") return "NH";
            }

            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                if (option.is_released == "true")
                {
                    string releasedType = (option.released_dayoff_type ?? "").Trim().ToUpper();
                    if (releasedType == "FULL") return "釋整";
                    if (releasedType == "HALF_AM") return "釋上";
                    if (releasedType == "HALF_PM") return "釋下";
                    return "釋整";
                }
            }

            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                if (option.is_quota_dayoff == "true")
                {
                    if (option.selected_full == "true") return "應整";
                    if (option.selected_half_am == "true") return "應上";
                    if (option.selected_half_pm == "true") return "應下";
                    return "應整";
                }
            }

            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                if (option.selected_full == "true") return "整";
                if (option.selected_half_am == "true") return "上";
                if (option.selected_half_pm == "true") return "下";
            }

            return "";
        }

        private bool IsNationalHolidayColumnPdf(
            DayOffScheduleDayClass day,
            List<DayOffScheduleItemClass> items,
            List<StaffDayOffOptionClass> options)
        {
            if (day == null) return false;

            string dateKey = day.date.StringToDateTime().ToString("yyyy-MM-dd");

            bool hasNHItem = items.Any(x =>
                x != null &&
                x.date.StringToDateTime().ToString("yyyy-MM-dd") == dateKey &&
                (x.selected_dayoff_type ?? "").Trim().ToUpper() == "NH");

            if (hasNHItem) return true;

            bool hasNHOption = options.Any(x =>
                x != null &&
                x.date.StringToDateTime().ToString("yyyy-MM-dd") == dateKey &&
                ((x.dayoff_source_type ?? "").Trim().ToUpper() == "NATIONAL_HOLIDAY"));

            return hasNHOption;
        }

        private string BuildPdfDayHeaderShortText(DateTime dt, bool isHoliday)
        {
            if (isHoliday) return "國";

            switch (dt.DayOfWeek)
            {
                case DayOfWeek.Monday: return "一";
                case DayOfWeek.Tuesday: return "二";
                case DayOfWeek.Wednesday: return "三";
                case DayOfWeek.Thursday: return "四";
                case DayOfWeek.Friday: return "五";
                case DayOfWeek.Saturday: return "六";
                case DayOfWeek.Sunday: return "日";
                default: return "";
            }
        }

        private void DrawPdfCellBackgroundAndBorder(
            XGraphics gfx,
            double x,
            double y,
            double width,
            double height,
            XBrush backgroundBrush,
            bool drawBorder)
        {
            if (backgroundBrush != null)
            {
                gfx.DrawRectangle(backgroundBrush, x, y, width, height);
            }

            if (drawBorder)
            {
                gfx.DrawRectangle(XPens.Black, x, y, width, height);
            }
        }

        // =====================================================
        // 最終確定的解法
        // =====================================================
        // 經過診斷確認:
        // - 字體完全正常 (font_debug.pdf 視覺上看得到)
        // - FontResolver 正確 (Bold/Regular 各自載入正確檔案)
        // - 但 XRect + XStringFormats.Center 在 PDFsharp + .otf
        //   組合下會導致 glyph 渲染失敗 (寫入文字層但不可見)
        //
        // 修法:helper 改用 DrawString(text, font, brush, x, y) 座標版
        // 用 MeasureString 自己算置中位置,完全避開 XStringFormats.Center
        // =====================================================

        private void DrawPdfCellSingleLineText(
            XGraphics gfx,
            double x,
            double y,
            double width,
            double height,
            string text,
            XFont font)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            XSize size = gfx.MeasureString(text, font);

            // 水平置中
            double drawX = x + (width - size.Width) / 2.0;
            // 垂直置中:DrawString 的 y 是 baseline,需做基線修正
            // baseline 在格子底端往上一點(避免文字貼底)
            double drawY = y + (height - size.Height) / 2.0 + size.Height * 0.78;

            gfx.DrawString(text, font, XBrushes.Black, drawX, drawY);
        }

        private void DrawPdfCellTwoLineText(
            XGraphics gfx,
            double x,
            double y,
            double width,
            double height,
            string topText,
            string bottomText,
            XFont topFont,
            XFont bottomFont)
        {
            double halfHeight = height / 2.0;

            if (!string.IsNullOrWhiteSpace(topText))
            {
                XSize size = gfx.MeasureString(topText, topFont);
                double drawX = x + (width - size.Width) / 2.0;
                double drawY = y + (halfHeight - size.Height) / 2.0 + size.Height * 0.78;
                gfx.DrawString(topText, topFont, XBrushes.Black, drawX, drawY);
            }

            if (!string.IsNullOrWhiteSpace(bottomText))
            {
                XSize size = gfx.MeasureString(bottomText, bottomFont);
                double drawX = x + (width - size.Width) / 2.0;
                double drawY = y + halfHeight + (halfHeight - size.Height) / 2.0 + size.Height * 0.78;
                gfx.DrawString(bottomText, bottomFont, XBrushes.Black, drawX, drawY);
            }
        }

        #endregion

        #region export_staff_dayoff_status_excel

        /// <summary>
        /// 匯出單一成員的排休狀態 Excel
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於匯出指定排休表單中，單一成員的排休狀態 Excel。
        ///
        /// 匯出內容包含：
        /// 1. 上方摘要資訊
        /// 2. 下方排休狀態明細清單
        ///
        /// 僅列出「有狀態的日期」，不列出純上班或完全空白日期。
        ///
        /// ---
        ///
        /// ## 🌐 URL
        /// ```text
        /// /phar_roster_api/dayOffSchedule/export_staff_dayoff_status_excel
        /// ```
        ///
        /// ## Method
        /// ```text
        /// POST
        /// ```
        ///
        /// ## Content-Type
        /// ```text
        /// application/json
        /// ```
        ///
        /// ---
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "export_staff_dayoff_status_excel",
        ///   "ValueAry": [
        ///     "form_name=2026-04",
        ///     "staff_id=1120468"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ---
        ///
        /// ## 🔍 參數說明
        /// | 參數名稱 | 類型 | 必填 | 說明 |
        /// |------|------|------|------|
        /// | form_name | string | ✅ | 排休表單名稱 |
        /// | staff_id | string | ✅ | 員工工號 |
        ///
        /// ---
        ///
        /// ## 📑 匯出內容
        /// ### 一、摘要區
        /// - 表單名稱
        /// - 工號
        /// - 姓名
        /// - 簡名
        /// - 應休總額度
        /// - 已用應休額度
        /// - 剩餘應休額度
        /// - 週六已用次數
        /// - 下午已用次數
        ///
        /// ### 二、明細區
        /// 欄位如下：
        ///
        /// | 日期 | 星期 | 日別 | 狀態 | 類型 | 來源 | 備註 |
        /// |------|------|------|------|------|------|------|
        ///
        /// ---
        ///
        /// ## 📝 狀態定義
        /// ### 狀態
        /// - 強制休假
        /// - 已釋出
        /// - 應休排休
        /// - 已選休假
        ///
        /// ### 類型
        /// - FF
        /// - NH
        /// - 整日
        /// - 上午
        /// - 下午
        ///
        /// ### 來源
        /// - 強制休假
        /// - 國定假日
        /// - 釋出
        /// - 應休
        /// - 一般選擇
        ///
        /// ### 日別
        /// - 平日
        /// - 星期六
        /// - 星期日
        /// - 國定假日
        ///
        /// 若同時為週末與國定假日，日別以「國定假日」優先。
        ///
        /// ---
        ///
        /// ## 📌 狀態優先順序
        /// 同一人同一天若有多種狀態，顯示優先順序如下：
        ///
        /// 1. 強制休假
        ///    - FF
        ///    - NH
        /// 2. 已釋出
        /// 3. 應休排休
        /// 4. 已選休假
        ///
        /// ---
        ///
        /// ## 📌 匯出規則
        /// 1. 僅匯出指定 staff_id 的資料。
        /// 2. 只列出有狀態的日期。
        /// 3. 已釋出狀態顯示在原持有人資料中。
        /// 4. 明細資料依日期升冪排序。
        /// 5. 備註欄位第一版先保留空白，供未來擴充。
        ///
        /// ---
        ///
        /// ## 📤 Response 說明（成功）
        /// 成功時回傳 Excel 檔案串流。
        ///
        /// ### 檔名格式
        /// ```text
        /// 單人成員排休狀態_{form_name}_{staff_id}.xlsx
        /// ```
        ///
        /// ---
        ///
        /// ## ❌ Response JSON 範例（錯誤）
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "export_staff_dayoff_status_excel",
        ///   "Result": "未輸入 form_name"
        /// }
        /// ```
        ///
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "export_staff_dayoff_status_excel",
        ///   "Result": "找不到 staff_id(1120468) 對應的表單資料"
        /// }
        /// ```
        /// </remarks>
        /// <param name="returnData">封裝 API 請求內容，需於 ValueAry 傳入 form_name、staff_id。</param>
        /// <returns>成功時回傳 Excel 檔案串流，失敗時回傳 JSON 錯誤訊息。</returns>
        [HttpPost("export_staff_dayoff_status_excel")]
        public IActionResult export_staff_dayoff_status_excel([FromBody] returnData returnData)
        {
            returnData.Method = "export_staff_dayoff_status_excel";

            try
            {
                init(returnData);

                string GetVal(string key) =>
                    returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string staff_id = GetVal("staff_id");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 form_name";
                    return new JsonResult(returnData);
                }

                if (staff_id.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 staff_id";
                    return new JsonResult(returnData);
                }

                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

                object[] obj_form = sql_dayOffScheduleFormClass.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();
                if (obj_form == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return new JsonResult(returnData);
                }

                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();

                List<DayOffScheduleDayClass> days = sql_dayOffScheduleDayClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleDayClass>()
                    .Where(x => x != null)
                    .OrderBy(x => x.date.StringToDateTime())
                    .ToList();

                List<DayOffScheduleItemClass> items = sql_dayOffScheduleItemClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleItemClass>()
                    .Where(x => x != null)
                    .ToList();

                List<StaffDayOffOptionClass> options = sql_staffDayOffOptionClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<StaffDayOffOptionClass>()
                    .Where(x => x != null)
                    .ToList();

                List<DayOffScheduleItemClass> staffItems = items
                    .Where(x => x.staff_id == staff_id)
                    .ToList();

                if (staffItems.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到 staff_id({staff_id}) 對應的表單資料";
                    return new JsonResult(returnData);
                }

                DayOffScheduleItemClass staffBase = staffItems.First();
                string staff_guid = staffBase.staff_guid ?? "";
                string staff_name = staffBase.staff_name ?? "";
                string staff_simple_name = staffBase.staff_simple_name ?? "";

                Dictionary<string, List<DayOffScheduleItemClass>> itemsByDate = staffItems
                    .GroupBy(x => x.date.StringToDateTime().ToString("yyyy-MM-dd"))
                    .ToDictionary(g => g.Key, g => g.ToList());

                List<StaffDayOffOptionClass> staffOptions = options
                    .Where(x => x.staff_guid == staff_guid)
                    .ToList();

                Dictionary<string, List<StaffDayOffOptionClass>> optionsByDate = staffOptions
                    .GroupBy(x => x.date.StringToDateTime().ToString("yyyy-MM-dd"))
                    .ToDictionary(g => g.Key, g => g.ToList());

                StaffQuotaExportSummaryExcel summary = BuildStaffQuotaExportSummaryExcel(staff_guid, staffOptions);

                List<ExportStaffDayoffDetailRowExcel> detailRows = new List<ExportStaffDayoffDetailRowExcel>();

                foreach (DayOffScheduleDayClass day in days)
                {
                    DateTime dt = day.date.StringToDateTime();
                    if (dt == DateTime.MinValue) continue;

                    string dateKey = dt.ToString("yyyy-MM-dd");

                    List<DayOffScheduleItemClass> dayItems =
                        itemsByDate.ContainsKey(dateKey) ? itemsByDate[dateKey] : new List<DayOffScheduleItemClass>();

                    List<StaffDayOffOptionClass> dayOptions =
                        optionsByDate.ContainsKey(dateKey) ? optionsByDate[dateKey] : new List<StaffDayOffOptionClass>();

                    ExportStaffDayoffDetailRowExcel row = ResolveStaffDayoffDetailRowExcel(dt, dayItems, dayOptions, items, options);
                    if (row != null)
                    {
                        detailRows.Add(row);
                    }
                }

                detailRows = detailRows
                    .OrderBy(x => x.date)
                    .ToList();

                IWorkbook workbook = new XSSFWorkbook();
                ISheet sheet = workbook.CreateSheet("排休狀態");

                ICellStyle titleStyle = CreateStaffDayoffExcelTitleStyle(workbook);
                ICellStyle labelStyle = CreateStaffDayoffExcelLabelStyle(workbook);
                ICellStyle valueStyle = CreateStaffDayoffExcelValueStyle(workbook);
                ICellStyle headerStyle = CreateStaffDayoffExcelHeaderStyle(workbook);
                ICellStyle normalStyle = CreateStaffDayoffExcelNormalStyle(workbook);

                int rowIndex = 0;

                // Title
                IRow rowTitle = sheet.CreateRow(rowIndex++);
                rowTitle.HeightInPoints = 24;
                CreateCell(rowTitle, 0, $"單人成員排休狀態 - {form.form_name}", titleStyle);
                sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(0, 0, 0, 6));

                // Summary
                rowIndex = WriteStaffDayoffSummaryRow(sheet, rowIndex, "表單名稱", form.form_name, labelStyle, valueStyle);
                rowIndex = WriteStaffDayoffSummaryRow(sheet, rowIndex, "工號", staff_id, labelStyle, valueStyle);
                rowIndex = WriteStaffDayoffSummaryRow(sheet, rowIndex, "姓名", staff_name, labelStyle, valueStyle);
                rowIndex = WriteStaffDayoffSummaryRow(sheet, rowIndex, "簡名", staff_simple_name, labelStyle, valueStyle);
                rowIndex = WriteStaffDayoffSummaryRow(sheet, rowIndex, "應休總額度", summary.quota_total, labelStyle, valueStyle);
                rowIndex = WriteStaffDayoffSummaryRow(sheet, rowIndex, "已用應休額度", summary.quota_used_total, labelStyle, valueStyle);
                rowIndex = WriteStaffDayoffSummaryRow(sheet, rowIndex, "剩餘應休額度", summary.quota_remaining, labelStyle, valueStyle);
                rowIndex = WriteStaffDayoffSummaryRow(sheet, rowIndex, "週六已用次數", summary.saturday_used_count, labelStyle, valueStyle);
                rowIndex = WriteStaffDayoffSummaryRow(sheet, rowIndex, "下午已用次數", summary.pm_used_count, labelStyle, valueStyle);

                rowIndex++;

                // Detail header
                IRow rowHeader = sheet.CreateRow(rowIndex++);
                rowHeader.HeightInPoints = 20;
                CreateCell(rowHeader, 0, "日期", headerStyle);
                CreateCell(rowHeader, 1, "星期", headerStyle);
                CreateCell(rowHeader, 2, "日別", headerStyle);
                CreateCell(rowHeader, 3, "狀態", headerStyle);
                CreateCell(rowHeader, 4, "類型", headerStyle);
                CreateCell(rowHeader, 5, "來源", headerStyle);
                CreateCell(rowHeader, 6, "備註", headerStyle);

                // Detail rows
                foreach (ExportStaffDayoffDetailRowExcel rowData in detailRows)
                {
                    IRow row = sheet.CreateRow(rowIndex++);
                    row.HeightInPoints = 20;

                    CreateCell(row, 0, rowData.date.ToString("yyyy-MM-dd"), normalStyle);
                    CreateCell(row, 1, rowData.week_text, normalStyle);
                    CreateCell(row, 2, rowData.day_type, normalStyle);
                    CreateCell(row, 3, rowData.status_text, normalStyle);
                    CreateCell(row, 4, rowData.type_text, normalStyle);
                    CreateCell(row, 5, rowData.source_text, normalStyle);
                    CreateCell(row, 6, rowData.note_text, normalStyle);
                }

                // Column widths
                sheet.SetColumnWidth(0, 16 * 256);
                sheet.SetColumnWidth(1, 10 * 256);
                sheet.SetColumnWidth(2, 14 * 256);
                sheet.SetColumnWidth(3, 16 * 256);
                sheet.SetColumnWidth(4, 12 * 256);
                sheet.SetColumnWidth(5, 16 * 256);
                sheet.SetColumnWidth(6, 24 * 256);

                byte[] excelBytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    workbook.Write(ms);
                    excelBytes = ms.ToArray();
                }

                Stream stream = new MemoryStream(excelBytes);
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                string downloadFileName = "staff_dayoff_status.xlsx";
                string displayFileName = $"單人成員排休狀態_{form.form_name}_{staff_id}.xlsx";
                string utf8FileName = Uri.EscapeDataString(displayFileName);

                Response.Headers["Content-Disposition"] =
                    $"attachment; filename=\"{downloadFileName}\"; filename*=UTF-8''{utf8FileName}";
                Response.Headers["Access-Control-Expose-Headers"] =
                    "Content-Disposition, Content-Length, Content-Type";

                return File(stream, contentType);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return new JsonResult(returnData);
            }
        }

        private class ExportStaffDayoffDetailRowExcel
        {
            public DateTime date { get; set; }
            public string week_text { get; set; }
            public string day_type { get; set; }
            public string status_text { get; set; }
            public string type_text { get; set; }
            public string source_text { get; set; }
            public string note_text { get; set; }
        }

        private class StaffQuotaExportSummaryExcel
        {
            public string quota_total { get; set; } = "0";
            public string quota_used_total { get; set; } = "0";
            public string quota_remaining { get; set; } = "0";
            public string saturday_used_count { get; set; } = "0";
            public string pm_used_count { get; set; } = "0";
        }

        private StaffQuotaExportSummaryExcel BuildStaffQuotaExportSummaryExcel(string staff_guid, List<StaffDayOffOptionClass> staffOptions)
        {
            StaffQuotaExportSummaryExcel result = new StaffQuotaExportSummaryExcel();

            if (staff_guid.StringIsEmpty()) return result;
            if (staffOptions == null) return result;

            double quotaTotal = 0;
            double quotaUsedTotal = 0;
            int saturdayUsedCount = 0;
            int pmUsedCount = 0;

            foreach (StaffDayOffOptionClass option in staffOptions)
            {
                option.NormalizeSelection();

                if (option.is_released == "true")
                {
                    string releasedType = (option.released_dayoff_type ?? "").Trim().ToUpper();
                    if (releasedType == "FULL") quotaTotal += 1;
                    else if (releasedType == "HALF_AM" || releasedType == "HALF_PM") quotaTotal += 0.5;
                }

                if (option.is_any_date == "true")
                {
                    quotaTotal += 1;
                }

                if (option.is_quota_dayoff == "true")
                {
                    double used = 0;
                    double.TryParse(option.quota_used, out used);
                    quotaUsedTotal += used;

                    string quotaType = (option.quota_dayoff_type ?? "").Trim().ToUpper();
                    if (quotaType == "SATURDAY_HALF_AM") saturdayUsedCount++;
                    if (quotaType == "WEEKDAY_HALF_PM") pmUsedCount++;
                }
            }

            double remaining = quotaTotal - quotaUsedTotal;
            if (remaining < 0) remaining = 0;

            result.quota_total = quotaTotal.ToString("0.##");
            result.quota_used_total = quotaUsedTotal.ToString("0.##");
            result.quota_remaining = remaining.ToString("0.##");
            result.saturday_used_count = saturdayUsedCount.ToString();
            result.pm_used_count = pmUsedCount.ToString();

            return result;
        }

        private ExportStaffDayoffDetailRowExcel ResolveStaffDayoffDetailRowExcel(
            DateTime dt,
            List<DayOffScheduleItemClass> dayItems,
            List<StaffDayOffOptionClass> dayOptions,
            List<DayOffScheduleItemClass> allItems,
            List<StaffDayOffOptionClass> allOptions)
        {
            if (dayItems == null) dayItems = new List<DayOffScheduleItemClass>();
            if (dayOptions == null) dayOptions = new List<StaffDayOffOptionClass>();
            if (allItems == null) allItems = new List<DayOffScheduleItemClass>();
            if (allOptions == null) allOptions = new List<StaffDayOffOptionClass>();

            string weekText = GetChineseWeekShortExcel(dt);
            string dayType = GetDayTypeTextExcel(dt, allItems, allOptions);

            // 1. 強制休假：item
            foreach (DayOffScheduleItemClass item in dayItems)
            {
                string selectedType = (item.selected_dayoff_type ?? "").Trim().ToUpper();
                if (selectedType == "FF")
                {
                    return new ExportStaffDayoffDetailRowExcel
                    {
                        date = dt,
                        week_text = weekText,
                        day_type = dayType,
                        status_text = "強制休假",
                        type_text = "FF",
                        source_text = "強制休假",
                        note_text = ""
                    };
                }
                if (selectedType == "NH")
                {
                    return new ExportStaffDayoffDetailRowExcel
                    {
                        date = dt,
                        week_text = weekText,
                        day_type = dayType,
                        status_text = "強制休假",
                        type_text = "NH",
                        source_text = "國定假日",
                        note_text = ""
                    };
                }
            }

            // 2. 強制休假：option
            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                string sourceType = (option.dayoff_source_type ?? "").Trim().ToUpper();
                if (option.is_force_ff == "true")
                {
                    if (sourceType == "NATIONAL_HOLIDAY")
                    {
                        return new ExportStaffDayoffDetailRowExcel
                        {
                            date = dt,
                            week_text = weekText,
                            day_type = dayType,
                            status_text = "強制休假",
                            type_text = "NH",
                            source_text = "國定假日",
                            note_text = ""
                        };
                    }

                    return new ExportStaffDayoffDetailRowExcel
                    {
                        date = dt,
                        week_text = weekText,
                        day_type = dayType,
                        status_text = "強制休假",
                        type_text = "FF",
                        source_text = "強制休假",
                        note_text = ""
                    };
                }

                if (sourceType == "NATIONAL_HOLIDAY")
                {
                    return new ExportStaffDayoffDetailRowExcel
                    {
                        date = dt,
                        week_text = weekText,
                        day_type = dayType,
                        status_text = "強制休假",
                        type_text = "NH",
                        source_text = "國定假日",
                        note_text = ""
                    };
                }
            }

            // 3. 已釋出
            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                if (option.is_released == "true")
                {
                    return new ExportStaffDayoffDetailRowExcel
                    {
                        date = dt,
                        week_text = weekText,
                        day_type = dayType,
                        status_text = "已釋出",
                        type_text = GetHalfDayTypeTextExcel(option.released_dayoff_type),
                        source_text = "釋出",
                        note_text = ""
                    };
                }
            }

            // 4. 應休排休
            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                if (option.is_quota_dayoff == "true")
                {
                    return new ExportStaffDayoffDetailRowExcel
                    {
                        date = dt,
                        week_text = weekText,
                        day_type = dayType,
                        status_text = "應休排休",
                        type_text = GetSelectedTypeTextExcel(option),
                        source_text = "應休",
                        note_text = ""
                    };
                }
            }

            // 5. 一般已選休假
            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                if (option.selected_full == "true" || option.selected_half_am == "true" || option.selected_half_pm == "true")
                {
                    return new ExportStaffDayoffDetailRowExcel
                    {
                        date = dt,
                        week_text = weekText,
                        day_type = dayType,
                        status_text = "已選休假",
                        type_text = GetSelectedTypeTextExcel(option),
                        source_text = "一般選擇",
                        note_text = ""
                    };
                }
            }

            return null;
        }

        private string GetChineseWeekShortExcel(DateTime dt)
        {
            switch (dt.DayOfWeek)
            {
                case DayOfWeek.Monday: return "一";
                case DayOfWeek.Tuesday: return "二";
                case DayOfWeek.Wednesday: return "三";
                case DayOfWeek.Thursday: return "四";
                case DayOfWeek.Friday: return "五";
                case DayOfWeek.Saturday: return "六";
                case DayOfWeek.Sunday: return "日";
                default: return "";
            }
        }

        private string GetDayTypeTextExcel(DateTime dt, List<DayOffScheduleItemClass> allItems, List<StaffDayOffOptionClass> allOptions)
        {
            string dateKey = dt.ToString("yyyy-MM-dd");

            bool isHoliday = allItems.Any(x =>
                x != null &&
                x.date.StringToDateTime().ToString("yyyy-MM-dd") == dateKey &&
                (x.selected_dayoff_type ?? "").Trim().ToUpper() == "NH");

            if (!isHoliday)
            {
                isHoliday = allOptions.Any(x =>
                    x != null &&
                    x.date.StringToDateTime().ToString("yyyy-MM-dd") == dateKey &&
                    ((x.dayoff_source_type ?? "").Trim().ToUpper() == "NATIONAL_HOLIDAY"));
            }

            if (isHoliday) return "國定假日";
            if (dt.DayOfWeek == DayOfWeek.Saturday) return "星期六";
            if (dt.DayOfWeek == DayOfWeek.Sunday) return "星期日";
            return "平日";
        }

        private string GetHalfDayTypeTextExcel(string sourceType)
        {
            string type = (sourceType ?? "").Trim().ToUpper();

            if (type == "FULL") return "整日";
            if (type == "HALF_AM") return "上午";
            if (type == "HALF_PM") return "下午";

            return "整日";
        }

        private string GetSelectedTypeTextExcel(StaffDayOffOptionClass option)
        {
            option.NormalizeSelection();

            if (option.selected_full == "true") return "整日";
            if (option.selected_half_am == "true") return "上午";
            if (option.selected_half_pm == "true") return "下午";

            return "整日";
        }

        private int WriteStaffDayoffSummaryRow(
            ISheet sheet,
            int rowIndex,
            string labelText,
            string valueText,
            ICellStyle labelStyle,
            ICellStyle valueStyle)
        {
            IRow row = sheet.CreateRow(rowIndex);
            row.HeightInPoints = 20;

            CreateCell(row, 0, labelText, labelStyle);
            CreateCell(row, 1, valueText ?? "", valueStyle);
            sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 1, 3));

            return rowIndex + 1;
        }

        private ICell CreateCell(IRow row, int columnIndex, string text, ICellStyle style)
        {
            ICell cell = row.CreateCell(columnIndex);
            cell.SetCellValue(text ?? "");
            cell.CellStyle = style;
            return cell;
        }

        private ICellStyle CreateStaffDayoffExcelTitleStyle(IWorkbook workbook)
        {
            IFont font = workbook.CreateFont();
            font.FontName = "微軟正黑體";
            font.FontHeightInPoints = 14;
            font.IsBold = true;

            ICellStyle style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Left;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.SetFont(font);

            return style;
        }

        private ICellStyle CreateStaffDayoffExcelLabelStyle(IWorkbook workbook)
        {
            IFont font = workbook.CreateFont();
            font.FontName = "微軟正黑體";
            font.FontHeightInPoints = 10;
            font.IsBold = true;

            ICellStyle style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.FillForegroundColor = IndexedColors.Grey25Percent.Index;
            style.FillPattern = FillPattern.SolidForeground;
            style.SetFont(font);

            return style;
        }

        private ICellStyle CreateStaffDayoffExcelValueStyle(IWorkbook workbook)
        {
            IFont font = workbook.CreateFont();
            font.FontName = "微軟正黑體";
            font.FontHeightInPoints = 10;

            ICellStyle style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Left;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.SetFont(font);

            return style;
        }

        private ICellStyle CreateStaffDayoffExcelHeaderStyle(IWorkbook workbook)
        {
            IFont font = workbook.CreateFont();
            font.FontName = "微軟正黑體";
            font.FontHeightInPoints = 10;
            font.IsBold = true;

            ICellStyle style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.FillForegroundColor = IndexedColors.Grey25Percent.Index;
            style.FillPattern = FillPattern.SolidForeground;
            style.SetFont(font);

            return style;
        }

        private ICellStyle CreateStaffDayoffExcelNormalStyle(IWorkbook workbook)
        {
            IFont font = workbook.CreateFont();
            font.FontName = "微軟正黑體";
            font.FontHeightInPoints = 10;

            ICellStyle style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.WrapText = true;
            style.SetFont(font);

            return style;
        }

        #endregion

        #region export_staff_dayoff_status_pdf

        /// <summary>
        /// 匯出單一成員的排休狀態 PDF
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於匯出指定排休表單中，單一成員的排休狀態 PDF。
        ///
        /// 匯出內容包含：
        /// 1. 上方摘要資訊
        /// 2. 下方排休狀態明細清單
        ///
        /// 僅列出「有狀態的日期」，不列出純上班或完全空白日期。
        ///
        /// ---
        ///
        /// ## 🌐 URL
        /// ```text
        /// /phar_roster_api/dayOffSchedule/export_staff_dayoff_status_pdf
        /// ```
        ///
        /// ## Method
        /// ```text
        /// POST
        /// ```
        ///
        /// ## Content-Type
        /// ```text
        /// application/json
        /// ```
        ///
        /// ---
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "export_staff_dayoff_status_pdf",
        ///   "ValueAry": [
        ///     "form_name=2026-04",
        ///     "staff_id=1120468"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// ---
        ///
        /// ## 🔍 參數說明
        /// | 參數名稱 | 類型 | 必填 | 說明 |
        /// |------|------|------|------|
        /// | form_name | string | ✅ | 排休表單名稱 |
        /// | staff_id | string | ✅ | 員工工號 |
        ///
        /// ---
        ///
        /// ## 📑 匯出內容
        /// ### 一、摘要區
        /// - 表單名稱
        /// - 工號
        /// - 姓名
        /// - 簡名
        /// - 應休總額度
        /// - 已用應休額度
        /// - 剩餘應休額度
        /// - 週六已用次數
        /// - 下午已用次數
        ///
        /// ### 二、明細區
        /// 欄位如下：
        ///
        /// | 日期 | 星期 | 日別 | 狀態 | 類型 | 來源 | 備註 |
        /// |------|------|------|------|------|------|------|
        ///
        /// ---
        ///
        /// ## 📝 狀態定義
        /// ### 狀態
        /// - 強制休假
        /// - 已釋出
        /// - 應休排休
        /// - 已選休假
        ///
        /// ### 類型
        /// - FF
        /// - NH
        /// - 整日
        /// - 上午
        /// - 下午
        ///
        /// ### 來源
        /// - 強制休假
        /// - 國定假日
        /// - 釋出
        /// - 應休
        /// - 一般選擇
        ///
        /// ### 日別
        /// - 平日
        /// - 星期六
        /// - 星期日
        /// - 國定假日
        ///
        /// 若同時為週末與國定假日，日別以「國定假日」優先。
        ///
        /// ---
        ///
        /// ## 📌 狀態優先順序
        /// 同一人同一天若有多種狀態，顯示優先順序如下：
        ///
        /// 1. 強制休假
        ///    - FF
        ///    - NH
        /// 2. 已釋出
        /// 3. 應休排休
        /// 4. 已選休假
        ///
        /// ---
        ///
        /// ## 📌 匯出規則
        /// 1. 僅匯出指定 staff_id 的資料。
        /// 2. 只列出有狀態的日期。
        /// 3. 已釋出狀態顯示在原持有人資料中。
        /// 4. 明細資料依日期升冪排序。
        /// 5. 備註欄位第一版先保留空白，供未來擴充。
        ///
        /// ---
        ///
        /// ## 📤 Response 說明（成功）
        /// 成功時回傳 PDF 檔案串流。
        ///
        /// ### 檔名格式
        /// ```text
        /// 單人成員排休狀態_{form_name}_{staff_id}.pdf
        /// ```
        ///
        /// ---
        ///
        /// ## ❌ Response JSON 範例（錯誤）
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "export_staff_dayoff_status_pdf",
        ///   "Result": "未輸入 form_name"
        /// }
        /// ```
        ///
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "export_staff_dayoff_status_pdf",
        ///   "Result": "找不到 staff_id(1120468) 對應的表單資料"
        /// }
        /// ```
        /// </remarks>
        /// <param name="returnData">封裝 API 請求內容，需於 ValueAry 傳入 form_name、staff_id。</param>
        /// <returns>成功時回傳 PDF 檔案串流，失敗時回傳 JSON 錯誤訊息。</returns>
        [HttpPost("export_staff_dayoff_status_pdf")]
        public IActionResult export_staff_dayoff_status_pdf([FromBody] returnData returnData)
        {
            returnData.Method = "export_staff_dayoff_status_pdf";

            try
            {
                CustomFontResolver.EnsurePdfSharpFontResolver();
                init(returnData);

                string GetVal(string key) =>
                    returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string staff_id = GetVal("staff_id");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 form_name";
                    return new JsonResult(returnData);
                }

                if (staff_id.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未輸入 staff_id";
                    return new JsonResult(returnData);
                }

                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

                object[] obj_form = sql_dayOffScheduleFormClass.GetRowsByDefult(null, "form_name", form_name).FirstOrDefault();
                if (obj_form == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return new JsonResult(returnData);
                }

                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();

                List<DayOffScheduleDayClass> days = sql_dayOffScheduleDayClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleDayClass>()
                    .Where(x => x != null)
                    .OrderBy(x => x.date.StringToDateTime())
                    .ToList();

                List<DayOffScheduleItemClass> items = sql_dayOffScheduleItemClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleItemClass>()
                    .Where(x => x != null)
                    .ToList();

                List<StaffDayOffOptionClass> options = sql_staffDayOffOptionClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<StaffDayOffOptionClass>()
                    .Where(x => x != null)
                    .ToList();

                List<DayOffScheduleItemClass> staffItems = items
                    .Where(x => x.staff_id == staff_id)
                    .ToList();

                if (staffItems.Count == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到 staff_id({staff_id}) 對應的表單資料";
                    return new JsonResult(returnData);
                }

                DayOffScheduleItemClass staffBase = staffItems.First();
                string staff_guid = staffBase.staff_guid ?? "";
                string staff_name = staffBase.staff_name ?? "";
                string staff_simple_name = staffBase.staff_simple_name ?? "";

                Dictionary<string, List<DayOffScheduleItemClass>> itemsByDate = staffItems
                    .GroupBy(x => x.date.StringToDateTime().ToString("yyyy-MM-dd"))
                    .ToDictionary(g => g.Key, g => g.ToList());

                List<StaffDayOffOptionClass> staffOptions = options
                    .Where(x => x.staff_guid == staff_guid)
                    .ToList();

                Dictionary<string, List<StaffDayOffOptionClass>> optionsByDate = staffOptions
                    .GroupBy(x => x.date.StringToDateTime().ToString("yyyy-MM-dd"))
                    .ToDictionary(g => g.Key, g => g.ToList());

                StaffQuotaExportSummaryPdfSingle summary = BuildStaffQuotaExportSummaryPdfSingle(staff_guid, staffOptions);

                List<ExportStaffDayoffDetailRowPdf> detailRows = new List<ExportStaffDayoffDetailRowPdf>();

                foreach (DayOffScheduleDayClass day in days)
                {
                    DateTime dt = day.date.StringToDateTime();
                    if (dt == DateTime.MinValue) continue;

                    string dateKey = dt.ToString("yyyy-MM-dd");

                    List<DayOffScheduleItemClass> dayItems =
                        itemsByDate.ContainsKey(dateKey) ? itemsByDate[dateKey] : new List<DayOffScheduleItemClass>();

                    List<StaffDayOffOptionClass> dayOptions =
                        optionsByDate.ContainsKey(dateKey) ? optionsByDate[dateKey] : new List<StaffDayOffOptionClass>();

                    ExportStaffDayoffDetailRowPdf row = ResolveStaffDayoffDetailRowPdf(dt, dayItems, dayOptions, items, options);
                    if (row != null)
                    {
                        detailRows.Add(row);
                    }
                }

                detailRows = detailRows
                    .OrderBy(x => x.date)
                    .ToList();

                PdfDocument document = new PdfDocument();
                document.Info.Title = $"單人成員排休狀態_{form.form_name}_{staff_id}";

                PdfPage page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;
                page.Orientation = PdfSharp.PageOrientation.Portrait;

                XGraphics gfx = XGraphics.FromPdfPage(page);

                XFont titleFont = new XFont("Noto Sans TC", 14, XFontStyleEx.Bold);
                XFont subFont = new XFont("Noto Sans TC", 8, XFontStyleEx.Regular);
                XFont labelFont = new XFont("Noto Sans TC", 9, XFontStyleEx.Bold);
                XFont valueFont = new XFont("Noto Sans TC", 9, XFontStyleEx.Regular);
                XFont headerFont = new XFont("Noto Sans TC", 9, XFontStyleEx.Bold);
                XFont rowFont = new XFont("Noto Sans TC", 9, XFontStyleEx.Regular);

                double marginLeft = 28;
                double marginTop = 28;
                double marginRight = 28;
                double marginBottom = 28;

                double pageWidth = page.Width.Point;
                double pageHeight = page.Height.Point;
                double contentWidth = pageWidth - marginLeft - marginRight;

                double y = marginTop;

                // Title
                gfx.DrawString($"單人成員排休狀態 - {form.form_name}", titleFont, XBrushes.Black,
                    new XRect(marginLeft, y, contentWidth, 20), XStringFormats.CenterLeft);

                gfx.DrawString($"匯出時間：{DateTime.Now:yyyy-MM-dd HH:mm:ss}", subFont, XBrushes.Black,
                    new XRect(marginLeft, y + 4, contentWidth, 20), XStringFormats.CenterRight);

                y += 28;

                // Summary block
                List<(string label, string value)> summaryRows = new List<(string label, string value)>
        {
            ("表單名稱", form.form_name),
            ("工號", staff_id),
            ("姓名", staff_name),
            ("簡名", staff_simple_name),
            ("應休總額度", summary.quota_total),
            ("已用應休額度", summary.quota_used_total),
            ("剩餘應休額度", summary.quota_remaining),
            ("週六已用次數", summary.saturday_used_count),
            ("下午已用次數", summary.pm_used_count)
        };

                double summaryLabelWidth = 90;
                double summaryValueWidth = contentWidth - summaryLabelWidth;
                double summaryRowHeight = 20;

                foreach ((string label, string value) in summaryRows)
                {
                    DrawPdfRect(gfx, marginLeft, y, summaryLabelWidth, summaryRowHeight, XBrushes.LightGray, true);
                    DrawPdfRect(gfx, marginLeft + summaryLabelWidth, y, summaryValueWidth, summaryRowHeight, XBrushes.White, true);

                    DrawPdfText(gfx, label, labelFont, XBrushes.Black,
                        new XRect(marginLeft, y, summaryLabelWidth, summaryRowHeight), XStringFormats.Center);

                    DrawPdfText(gfx, value ?? "", valueFont, XBrushes.Black,
                        new XRect(marginLeft + summaryLabelWidth + 4, y, summaryValueWidth - 8, summaryRowHeight), XStringFormats.CenterLeft);

                    y += summaryRowHeight;
                }

                y += 16;

                // Table header
                double[] colWidths = new double[]
                {
            78, // 日期
            40, // 星期
            70, // 日別
            78, // 狀態
            55, // 類型
            70, // 來源
            contentWidth - (78 + 40 + 70 + 78 + 55 + 70) // 備註
                };

                string[] headers = new string[]
                {
            "日期", "星期", "日別", "狀態", "類型", "來源", "備註"
                };

                double tableHeaderHeight = 22;
                double rowHeight = 22;

                void DrawTableHeader()
                {
                    double x = marginLeft;
                    for (int i = 0; i < headers.Length; i++)
                    {
                        DrawPdfRect(gfx, x, y, colWidths[i], tableHeaderHeight, XBrushes.LightGray, true);
                        DrawPdfText(gfx, headers[i], headerFont, XBrushes.Black,
                            new XRect(x, y, colWidths[i], tableHeaderHeight), XStringFormats.Center);
                        x += colWidths[i];
                    }
                    y += tableHeaderHeight;
                }

                DrawTableHeader();

                // Detail rows
                foreach (ExportStaffDayoffDetailRowPdf row in detailRows)
                {
                    if (y + rowHeight > pageHeight - marginBottom)
                    {
                        page = document.AddPage();
                        page.Size = PdfSharp.PageSize.A4;
                        page.Orientation = PdfSharp.PageOrientation.Portrait;
                        gfx = XGraphics.FromPdfPage(page);
                        y = marginTop;

                        gfx.DrawString($"單人成員排休狀態 - {form.form_name}", titleFont, XBrushes.Black,
                            new XRect(marginLeft, y, contentWidth, 20), XStringFormats.CenterLeft);

                        gfx.DrawString($"工號：{staff_id}  姓名：{staff_name}", subFont, XBrushes.Black,
                            new XRect(marginLeft, y + 4, contentWidth, 20), XStringFormats.CenterRight);

                        y += 28;
                        DrawTableHeader();
                    }

                    string[] rowTexts = new string[]
                    {
                row.date.ToString("yyyy-MM-dd"),
                row.week_text,
                row.day_type,
                row.status_text,
                row.type_text,
                row.source_text,
                row.note_text
                    };

                    double x = marginLeft;
                    for (int i = 0; i < rowTexts.Length; i++)
                    {
                        DrawPdfRect(gfx, x, y, colWidths[i], rowHeight, XBrushes.White, true);

                        XStringFormat format = i == 6 ? XStringFormats.CenterLeft : XStringFormats.Center;
                        XRect textRect = i == 6
                            ? new XRect(x + 4, y, colWidths[i] - 8, rowHeight)
                            : new XRect(x, y, colWidths[i], rowHeight);

                        DrawPdfText(gfx, rowTexts[i] ?? "", rowFont, XBrushes.Black, textRect, format);

                        x += colWidths[i];
                    }

                    y += rowHeight;
                }

                byte[] pdfBytes;
                using (MemoryStream ms = new MemoryStream())
                {
                    document.Save(ms, false);
                    pdfBytes = ms.ToArray();
                }

                Stream stream = new MemoryStream(pdfBytes);
                string contentType = "application/pdf";

                string downloadFileName = "staff_dayoff_status.pdf";
                string displayFileName = $"單人成員排休狀態_{form.form_name}_{staff_id}.pdf";
                string utf8FileName = Uri.EscapeDataString(displayFileName);

                Response.Headers["Content-Disposition"] =
                    $"attachment; filename=\"{downloadFileName}\"; filename*=UTF-8''{utf8FileName}";
                Response.Headers["Access-Control-Expose-Headers"] =
                    "Content-Disposition, Content-Length, Content-Type";

                return File(stream, contentType);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = ex.Message;
                return new JsonResult(returnData);
            }
        }

        private class ExportStaffDayoffDetailRowPdf
        {
            public DateTime date { get; set; }
            public string week_text { get; set; }
            public string day_type { get; set; }
            public string status_text { get; set; }
            public string type_text { get; set; }
            public string source_text { get; set; }
            public string note_text { get; set; }
        }

        private class StaffQuotaExportSummaryPdfSingle
        {
            public string quota_total { get; set; } = "0";
            public string quota_used_total { get; set; } = "0";
            public string quota_remaining { get; set; } = "0";
            public string saturday_used_count { get; set; } = "0";
            public string pm_used_count { get; set; } = "0";
        }

        private StaffQuotaExportSummaryPdfSingle BuildStaffQuotaExportSummaryPdfSingle(string staff_guid, List<StaffDayOffOptionClass> staffOptions)
        {
            StaffQuotaExportSummaryPdfSingle result = new StaffQuotaExportSummaryPdfSingle();

            if (staff_guid.StringIsEmpty()) return result;
            if (staffOptions == null) return result;

            double quotaTotal = 0;
            double quotaUsedTotal = 0;
            int saturdayUsedCount = 0;
            int pmUsedCount = 0;

            foreach (StaffDayOffOptionClass option in staffOptions)
            {
                option.NormalizeSelection();

                if (option.is_released == "true")
                {
                    string releasedType = (option.released_dayoff_type ?? "").Trim().ToUpper();
                    if (releasedType == "FULL") quotaTotal += 1;
                    else if (releasedType == "HALF_AM" || releasedType == "HALF_PM") quotaTotal += 0.5;
                }

                if (option.is_any_date == "true")
                {
                    quotaTotal += 1;
                }

                if (option.is_quota_dayoff == "true")
                {
                    double used = 0;
                    double.TryParse(option.quota_used, out used);
                    quotaUsedTotal += used;

                    string quotaType = (option.quota_dayoff_type ?? "").Trim().ToUpper();
                    if (quotaType == "SATURDAY_HALF_AM") saturdayUsedCount++;
                    if (quotaType == "WEEKDAY_HALF_PM") pmUsedCount++;
                }
            }

            double remaining = quotaTotal - quotaUsedTotal;
            if (remaining < 0) remaining = 0;

            result.quota_total = quotaTotal.ToString("0.##");
            result.quota_used_total = quotaUsedTotal.ToString("0.##");
            result.quota_remaining = remaining.ToString("0.##");
            result.saturday_used_count = saturdayUsedCount.ToString();
            result.pm_used_count = pmUsedCount.ToString();

            return result;
        }

        private ExportStaffDayoffDetailRowPdf ResolveStaffDayoffDetailRowPdf(
            DateTime dt,
            List<DayOffScheduleItemClass> dayItems,
            List<StaffDayOffOptionClass> dayOptions,
            List<DayOffScheduleItemClass> allItems,
            List<StaffDayOffOptionClass> allOptions)
        {
            if (dayItems == null) dayItems = new List<DayOffScheduleItemClass>();
            if (dayOptions == null) dayOptions = new List<StaffDayOffOptionClass>();
            if (allItems == null) allItems = new List<DayOffScheduleItemClass>();
            if (allOptions == null) allOptions = new List<StaffDayOffOptionClass>();

            string weekText = GetChineseWeekShortPdf(dt);
            string dayType = GetDayTypeTextPdf(dt, allItems, allOptions);

            // 1. 強制休假：item
            foreach (DayOffScheduleItemClass item in dayItems)
            {
                string selectedType = (item.selected_dayoff_type ?? "").Trim().ToUpper();
                if (selectedType == "FF")
                {
                    return new ExportStaffDayoffDetailRowPdf
                    {
                        date = dt,
                        week_text = weekText,
                        day_type = dayType,
                        status_text = "強制休假",
                        type_text = "FF",
                        source_text = "強制休假",
                        note_text = ""
                    };
                }
                if (selectedType == "NH")
                {
                    return new ExportStaffDayoffDetailRowPdf
                    {
                        date = dt,
                        week_text = weekText,
                        day_type = dayType,
                        status_text = "強制休假",
                        type_text = "NH",
                        source_text = "國定假日",
                        note_text = ""
                    };
                }
            }

            // 2. 強制休假：option
            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                string sourceType = (option.dayoff_source_type ?? "").Trim().ToUpper();
                if (option.is_force_ff == "true")
                {
                    if (sourceType == "NATIONAL_HOLIDAY")
                    {
                        return new ExportStaffDayoffDetailRowPdf
                        {
                            date = dt,
                            week_text = weekText,
                            day_type = dayType,
                            status_text = "強制休假",
                            type_text = "NH",
                            source_text = "國定假日",
                            note_text = ""
                        };
                    }

                    return new ExportStaffDayoffDetailRowPdf
                    {
                        date = dt,
                        week_text = weekText,
                        day_type = dayType,
                        status_text = "強制休假",
                        type_text = "FF",
                        source_text = "強制休假",
                        note_text = ""
                    };
                }

                if (sourceType == "NATIONAL_HOLIDAY")
                {
                    return new ExportStaffDayoffDetailRowPdf
                    {
                        date = dt,
                        week_text = weekText,
                        day_type = dayType,
                        status_text = "強制休假",
                        type_text = "NH",
                        source_text = "國定假日",
                        note_text = ""
                    };
                }
            }

            // 3. 已釋出
            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                if (option.is_released == "true")
                {
                    return new ExportStaffDayoffDetailRowPdf
                    {
                        date = dt,
                        week_text = weekText,
                        day_type = dayType,
                        status_text = "已釋出",
                        type_text = GetHalfDayTypeTextPdf(option.released_dayoff_type),
                        source_text = "釋出",
                        note_text = ""
                    };
                }
            }

            // 4. 應休排休
            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                if (option.is_quota_dayoff == "true")
                {
                    return new ExportStaffDayoffDetailRowPdf
                    {
                        date = dt,
                        week_text = weekText,
                        day_type = dayType,
                        status_text = "應休排休",
                        type_text = GetSelectedTypeTextPdf(option),
                        source_text = "應休",
                        note_text = ""
                    };
                }
            }

            // 5. 一般已選休假
            foreach (StaffDayOffOptionClass option in dayOptions)
            {
                option.NormalizeSelection();

                if (option.selected_full == "true" || option.selected_half_am == "true" || option.selected_half_pm == "true")
                {
                    return new ExportStaffDayoffDetailRowPdf
                    {
                        date = dt,
                        week_text = weekText,
                        day_type = dayType,
                        status_text = "已選休假",
                        type_text = GetSelectedTypeTextPdf(option),
                        source_text = "一般選擇",
                        note_text = ""
                    };
                }
            }

            return null;
        }

        private string GetChineseWeekShortPdf(DateTime dt)
        {
            switch (dt.DayOfWeek)
            {
                case DayOfWeek.Monday: return "一";
                case DayOfWeek.Tuesday: return "二";
                case DayOfWeek.Wednesday: return "三";
                case DayOfWeek.Thursday: return "四";
                case DayOfWeek.Friday: return "五";
                case DayOfWeek.Saturday: return "六";
                case DayOfWeek.Sunday: return "日";
                default: return "";
            }
        }

        private string GetDayTypeTextPdf(DateTime dt, List<DayOffScheduleItemClass> allItems, List<StaffDayOffOptionClass> allOptions)
        {
            string dateKey = dt.ToString("yyyy-MM-dd");

            bool isHoliday = allItems.Any(x =>
                x != null &&
                x.date.StringToDateTime().ToString("yyyy-MM-dd") == dateKey &&
                (x.selected_dayoff_type ?? "").Trim().ToUpper() == "NH");

            if (!isHoliday)
            {
                isHoliday = allOptions.Any(x =>
                    x != null &&
                    x.date.StringToDateTime().ToString("yyyy-MM-dd") == dateKey &&
                    ((x.dayoff_source_type ?? "").Trim().ToUpper() == "NATIONAL_HOLIDAY"));
            }

            if (isHoliday) return "國定假日";
            if (dt.DayOfWeek == DayOfWeek.Saturday) return "星期六";
            if (dt.DayOfWeek == DayOfWeek.Sunday) return "星期日";
            return "平日";
        }

        private string GetHalfDayTypeTextPdf(string sourceType)
        {
            string type = (sourceType ?? "").Trim().ToUpper();

            if (type == "FULL") return "整日";
            if (type == "HALF_AM") return "上午";
            if (type == "HALF_PM") return "下午";

            return "整日";
        }

        private void DrawPdfRect(
            XGraphics gfx,
            double x,
            double y,
            double width,
            double height,
            XBrush backgroundBrush,
            bool drawBorder)
        {
            if (backgroundBrush != null)
            {
                gfx.DrawRectangle(backgroundBrush, x, y, width, height);
            }

            if (drawBorder)
            {
                gfx.DrawRectangle(XPens.Black, x, y, width, height);
            }
        }

        private void DrawPdfText(
            XGraphics gfx,
            string text,
            XFont font,
            XBrush brush,
            XRect rect,
            XStringFormat format)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            gfx.DrawString(text, font, brush, rect, format);
        }
        private string GetSelectedTypeTextPdf(StaffDayOffOptionClass option)
        {
            if (option == null) return "整日";

            option.NormalizeSelection();

            if (option.selected_full == "true") return "整日";
            if (option.selected_half_am == "true") return "上午";
            if (option.selected_half_pm == "true") return "下午";

            return "整日";
        }
        #endregion

        private Dictionary<string, DayOffDateQuotaUsageSummary> BuildDateQuotaUsageSummaryDict(List<DayOffScheduleDayClass> days, List<StaffDayOffOptionClass> allOptions)
        {
            var dict = new Dictionary<string, DayOffDateQuotaUsageSummary>();

            days = days ?? new List<DayOffScheduleDayClass>();
            allOptions = allOptions ?? new List<StaffDayOffOptionClass>();

            var optionByDate = allOptions
                .Where(x => x != null && !x.date.StringIsEmpty())
                .GroupBy(x => x.date.StringToDateTime().ToDateString('-'))
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var day in days)
            {
                DateTime dt = day.date.StringToDateTime();
                if (dt == DateTime.MinValue) continue;

                string dateKey = dt.ToDateString('-');
                if (dateKey.StringIsEmpty()) continue;

                int amUsed = 0;
                int pmUsed = 0;

                if (optionByDate.TryGetValue(dateKey, out var options))
                {
                    foreach (var option in options)
                    {
                        if (option == null) continue;

                        option.NormalizeSelection();

                        if (option.selected_full == "true")
                        {
                            amUsed++;
                            pmUsed++;
                        }
                        else
                        {
                            if (option.selected_half_am == "true") amUsed++;
                            if (option.selected_half_pm == "true") pmUsed++;
                        }
                    }
                }

                int amMax = day.am_max_dayoff_count.StringToInt32();
                int pmMax = day.pm_max_dayoff_count.StringToInt32();

                dict[dateKey] = new DayOffDateQuotaUsageSummary
                {
                    date = dateKey,
                    am_max_dayoff_count = amMax.ToString(),
                    pm_max_dayoff_count = pmMax.ToString(),
                    am_used_count = amUsed.ToString(),
                    pm_used_count = pmUsed.ToString(),
                    am_remaining_count = (amMax - amUsed).ToString(),
                    pm_remaining_count = (pmMax - pmUsed).ToString()
                };
            }

            return dict;
        }
        private Dictionary<string, GetStaffRemainingQuotaDayoffResponse> BuildStaffQuotaSummaryDict( string form_guid,  List<DayOffScheduleDayClass> days, List<DayOffScheduleItemClass> allItems,  List<StaffDayOffOptionClass> allOptions)
        {
            var dict = new Dictionary<string, GetStaffRemainingQuotaDayoffResponse>();

            days = days ?? new List<DayOffScheduleDayClass>();
            allItems = allItems ?? new List<DayOffScheduleItemClass>();
            allOptions = allOptions ?? new List<StaffDayOffOptionClass>();

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

            var staffGuids = allItems
                .Where(x => x != null && !x.staff_guid.StringIsEmpty())
                .Select(x => x.staff_guid)
                .Union(allOptions.Where(x => x != null && !x.staff_guid.StringIsEmpty()).Select(x => x.staff_guid))
                .Distinct()
                .ToList();

            var itemsByStaffDate = allItems
                .Where(x => x != null && !x.staff_guid.StringIsEmpty() && !x.date.StringIsEmpty())
                .GroupBy(x => $"{x.staff_guid}|{x.date.StringToDateTime().ToDateString('-')}")
                .ToDictionary(g => g.Key, g => g.First());

            var optionsByStaff = allOptions
                .Where(x => x != null && !x.staff_guid.StringIsEmpty())
                .GroupBy(x => x.staff_guid)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var staffGuid in staffGuids)
            {
                double quotaTotal = 0;
                double quotaUsed = 0;

                // 1. 週六沒有排班，強制應休 +0.5
                foreach (var day in days)
                {
                    DateTime dt = day.date.StringToDateTime();
                    if (dt == DateTime.MinValue) continue;

                    if (dt.DayOfWeek != DayOfWeek.Saturday) continue;

                    string dateKey = dt.ToDateString('-');
                    string key = $"{staffGuid}|{dateKey}";

                    itemsByStaffDate.TryGetValue(key, out var item);

                    if (!HasSchedule(item))
                    {
                        quotaTotal += 0.5;
                    }
                }

                if (optionsByStaff.TryGetValue(staffGuid, out var opts))
                {
                    foreach (var opt in opts)
                    {
                        if (opt == null) continue;

                        opt.NormalizeSelection();

                        // 2. 任選假 is_any_date=true，應休 +1
                        if (opt.is_any_date == "true")
                        {
                            quotaTotal += 1;
                        }

                        // 3. 釋出增加應休額度
                        if (opt.is_released == "true")
                        {
                            string releaseType = (opt.released_dayoff_type ?? "").Trim().ToUpper();

                            if (releaseType == "FULL")
                            {
                                quotaTotal += 1;
                            }
                            else if (releaseType == "HALF_AM" || releaseType == "HALF_PM")
                            {
                                quotaTotal += 0.5;
                            }
                        }

                        // 4. 已使用應休額度
                        if (opt.is_quota_dayoff == "true")
                        {
                            quotaUsed += opt.quota_used.StringToDouble();
                        }
                    }
                }

                double quotaRemaining = quotaTotal - quotaUsed;
                if (quotaRemaining < 0) quotaRemaining = 0;

                dict[staffGuid] = new GetStaffRemainingQuotaDayoffResponse
                {
                    staff_guid = staffGuid,
                    quota_total = quotaTotal.ToString("0.##"),
                    quota_used_total = quotaUsed.ToString("0.##"),
                    quota_remaining = quotaRemaining.ToString("0.##")
                };
            }

            return dict;
        }
        private Dictionary<string, StaffQuotaDayoffRuleSummary> BuildStaffRuleSummaryDict( string form_guid, List<StaffDayOffOptionClass> allOptions, Dictionary<string, GetStaffRemainingQuotaDayoffResponse> quotaSummaryDict)
        {
            var dict = new Dictionary<string, StaffQuotaDayoffRuleSummary>();

            allOptions = allOptions ?? new List<StaffDayOffOptionClass>();
            quotaSummaryDict = quotaSummaryDict ?? new Dictionary<string, GetStaffRemainingQuotaDayoffResponse>();

            var staffGuids = quotaSummaryDict.Keys
                .Union(allOptions.Where(x => x != null && !x.staff_guid.StringIsEmpty()).Select(x => x.staff_guid))
                .Distinct()
                .ToList();

            var optionsByStaff = allOptions
                .Where(x => x != null && !x.staff_guid.StringIsEmpty())
                .GroupBy(x => x.staff_guid)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var staffGuid in staffGuids)
            {
                int pmHalfUsedCount = 0;
                int saturdayUsedCount = 0;

                int pmHalfLimitCount = 2;
                int saturdayLimitCount = 1;

                bool hasExtraSaturdayLimit = false;

                if (optionsByStaff.TryGetValue(staffGuid, out var opts))
                {
                    foreach (var opt in opts)
                    {
                        if (opt == null) continue;

                        opt.NormalizeSelection();

                        string quotaType = (opt.quota_dayoff_type ?? "").Trim().ToUpper();

                        if (opt.is_quota_dayoff == "true")
                        {
                            if (quotaType == "WEEKDAY_HALF_PM")
                            {
                                pmHalfUsedCount++;
                            }

                            if (quotaType == "SATURDAY_HALF_AM")
                            {
                                saturdayUsedCount++;
                            }
                        }

                        if (opt.is_released == "true")
                        {
                            DateTime dt = opt.date.StringToDateTime();

                            if (dt != DateTime.MinValue &&
                                (dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday))
                            {
                                hasExtraSaturdayLimit = true;
                            }
                        }
                    }
                }

                if (hasExtraSaturdayLimit)
                {
                    saturdayLimitCount += 1;
                }

                quotaSummaryDict.TryGetValue(staffGuid, out var quotaSummary);

                if (quotaSummary == null)
                {
                    quotaSummary = new GetStaffRemainingQuotaDayoffResponse
                    {
                        staff_guid = staffGuid,
                        quota_total = "0",
                        quota_used_total = "0",
                        quota_remaining = "0"
                    };
                }

                dict[staffGuid] = new StaffQuotaDayoffRuleSummary
                {
                    quota_total = quotaSummary.quota_total,
                    quota_used_total = quotaSummary.quota_used_total,
                    quota_remaining = quotaSummary.quota_remaining,

                    pm_half_used_count = pmHalfUsedCount.ToString(),
                    saturday_used_count = saturdayUsedCount.ToString(),

                    pm_half_limit_count = pmHalfLimitCount.ToString(),
                    saturday_limit_count = saturdayLimitCount.ToString(),

                    has_extra_saturday_limit = hasExtraSaturdayLimit ? "true" : "false"
                };
            }

            return dict;
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

        private static readonly object _dayoffReleasePoolLock = new object();

        /// <summary>
        /// 釋出指定放假選項並建立釋出池（升級版 release_dayoff_option）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL
        /// POST /phar_roster_api/DayOffSchedule/release_dayoff_option
        ///
        /// ## 📘 功能說明
        /// 將指定的 StaffDayOffOptionClass 標記為釋出，並同步建立一筆 DayOffReleasePoolClass，
        /// 供其他人後續填洞接手。
        ///
        /// ## 升級版規則
        /// 1. 釋出與休假選擇互斥
        /// 2. 若 selected_full=true，則不可釋出 FULL
        /// 3. 若 selected_half_am=true，則自動釋出 HALF_PM
        /// 4. 若 selected_half_pm=true，則自動釋出 HALF_AM
        /// 5. 若目前沒有任何休假選擇，才允許由前端指定 FULL / HALF_AM / HALF_PM
        ///
        /// ## 注意
        /// - 本 API 不允許重複釋出
        /// - 本 API 不處理接手者資料
        /// </remarks>
        [HttpPost("release_dayoff_option")]
        public string release_dayoff_option([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "release_dayoff_option";
            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string option_guid = GetVal("option_guid");
                string release_dayoff_type = GetVal("release_dayoff_type");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 form_name";
                    return returnData.JsonSerializationt();
                }
                if (option_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 option_guid";
                    return returnData.JsonSerializationt();
                }

                release_dayoff_type = (release_dayoff_type ?? "").Trim().ToUpper();

                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();
                var sql_dayOffReleasePoolClass = MethodClass.GetSQLControl<DayOffReleasePoolClass>();

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

                object[] obj_option = sql_staffDayOffOptionClass
                    .GetRowsByDefult(null, "GUID", option_guid)
                    .FirstOrDefault();

                if (obj_option == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到放假選項(option_guid={option_guid})";
                    return returnData.JsonSerializationt();
                }

                StaffDayOffOptionClass option = obj_option.SQLToClass<StaffDayOffOptionClass>();

                if (option.form_guid != dayOffScheduleForm.GUID)
                {
                    returnData.Code = -200;
                    returnData.Result = "option 不屬於指定表單";
                    return returnData.JsonSerializationt();
                }

                if (option.is_force_ff == "true")
                {
                    returnData.Code = -200;
                    returnData.Result = "系統強制放假(FF)不可釋出";
                    return returnData.JsonSerializationt();
                }

                if (option.is_released == "true")
                {
                    returnData.Code = -200;
                    returnData.Result = "此放假選項已釋出，不可重複操作";
                    return returnData.JsonSerializationt();
                }

                option.NormalizeSelection();

                bool isFull = option.selected_full == "true";
                bool isHalfAm = option.selected_half_am == "true";
                bool isHalfPm = option.selected_half_pm == "true";

                // =========================================================
                // 自動決定合法釋出類型
                // =========================================================
                if (isFull)
                {
                    returnData.Code = -200;
                    returnData.Result = "已選擇全日休假時不可釋出整日";
                    return returnData.JsonSerializationt();
                }
                else if (isHalfAm)
                {
                    // 選上午休 → 自動釋出下午
                    release_dayoff_type = "HALF_PM";
                }
                else if (isHalfPm)
                {
                    // 選下午休 → 自動釋出上午
                    release_dayoff_type = "HALF_AM";
                }
                else
                {
                    // 沒有休假選擇時，才接受前端指定
                    if (release_dayoff_type != "FULL" &&
                        release_dayoff_type != "HALF_AM" &&
                        release_dayoff_type != "HALF_PM")
                    {
                        returnData.Code = -200;
                        returnData.Result = "未選擇休假時，release_dayoff_type 只允許 FULL / HALF_AM / HALF_PM";
                        return returnData.JsonSerializationt();
                    }
                }

                string now = DateTime.Now.ToDateTimeString();

                lock (_dayoffReleasePoolLock)
                {
                    // 防止重複建 pool
                    bool existsPool = sql_dayOffReleasePoolClass
                        .GetRowsByDefult(null, "source_option_guid", option.GUID)
                        .SQLToClass<DayOffReleasePoolClass>()
                        .Any(x => x.status == "OPEN");

                    if (existsPool)
                    {
                        returnData.Code = -200;
                        returnData.Result = "此 option 已存在 OPEN 狀態的釋出池";
                        return returnData.JsonSerializationt();
                    }

                    DayOffReleasePoolClass pool = new DayOffReleasePoolClass();
                    pool.GUID = Guid.NewGuid().ToString();
                    pool.form_guid = option.form_guid;
                    pool.source_option_guid = option.GUID;
                    pool.source_item_guid = option.item_guid;
                    pool.source_staff_guid = option.staff_guid;
                    pool.date = option.date;
                    pool.release_dayoff_type = release_dayoff_type;
                    pool.total_slots = "1";
                    pool.claimed_slots = "0";
                    pool.remaining_slots = "1";
                    pool.status = "OPEN";
                    pool.version_no = "1";
                    pool.created_at = now;
                    pool.updated_at = now;

                    // 釋出不再改動目前已選休假
                    option.is_released = "true";
                    option.released_at = now;
                    option.released_dayoff_type = release_dayoff_type;
                    option.release_pool_guid = pool.GUID;
                    option.dayoff_source_type = "RELEASED_SOURCE";
                    option.updated_at = now;

                    option.NormalizeSelection();

                    sql_dayOffReleasePoolClass.AddRows(null, new List<object[]>() { pool.ClassToSQL<DayOffReleasePoolClass>() });
                    sql_staffDayOffOptionClass.UpdateByDefulteExtra(null, option.ClassToSQL<StaffDayOffOptionClass>());

                    returnData.Code = 200;
                    returnData.Data = new
                    {
                        option,
                        release_pool = pool
                    };
                    returnData.Result = "釋出成功";
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
        /// 取消指定放假選項的釋出狀態（同步最新規則版 cancel_release_dayoff_option）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL  
        /// `POST /phar_roster_api/DayOffSchedule/cancel_release_dayoff_option`
        ///
        /// ## 📘 功能說明  
        /// 依據指定的放假選項 GUID (<c>option_guid</c>)，
        /// 將該筆原始釋出 option 的釋出狀態取消，並同步關閉對應的釋出池。
        ///
        ///
        /// ## ✅ 同步最新規則
        /// 1. 取消釋出時：
        ///    - 只清除釋出狀態
        ///    - 不恢復、不改動原本的 selected_full / selected_half_am / selected_half_pm
        ///
        /// 2. 若該 option 有對應 DayOffReleasePoolClass，
        ///    且 pool.claimed_slots > 0
        ///    → 不可取消釋出（因為已有人接手）
        ///
        /// 3. 若 pool.claimed_slots = 0
        ///    → 可取消釋出：
        ///    - option.is_released = false
        ///    - option.released_dayoff_type = ""
        ///    - option.released_at = MinValue
        ///    - option.release_pool_guid = ""
        ///    - 若 dayoff_source_type = RELEASED_SOURCE，則清空
        ///    - pool.status = CANCELLED
        ///
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "cancel_release_dayoff_option",
        ///   "ValueAry": [
        ///     "form_name=2026年03月排休表",
        ///     "option_guid=OPTION_GUID_001"
        ///   ],
        ///   "Data": {}
        /// }
        /// ```
        ///
        ///
        /// ## 📤 成功回傳範例
        /// ```json
        /// {
        ///   "Code": 200,
        ///   "Result": "取消釋出成功",
        ///   "Data": {
        ///     "option": {
        ///       "GUID": "OPTION_GUID_001",
        ///       "selected_full": "false",
        ///       "selected_half_am": "true",
        ///       "selected_half_pm": "false",
        ///       "is_released": "false",
        ///       "released_dayoff_type": "",
        ///       "release_pool_guid": ""
        ///     },
        ///     "release_pool": {
        ///       "GUID": "POOL_GUID_001",
        ///       "status": "CANCELLED"
        ///     }
        ///   }
        /// }
        /// ```
        ///
        ///
        /// ## ❌ 錯誤回傳範例
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Result": "此釋出名額已被接手，不可取消釋出"
        /// }
        /// ```
        /// </remarks>
        /// <param name="returnData">封裝 API 請求內容的物件。</param>
        /// <returns>回傳 JSON 字串。</returns>
        [HttpPost("cancel_release_dayoff_option")]
        public string cancel_release_dayoff_option([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "cancel_release_dayoff_option";

            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string option_guid = GetVal("option_guid");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 form_name";
                    return returnData.JsonSerializationt();
                }
                if (option_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 option_guid";
                    return returnData.JsonSerializationt();
                }

                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();
                var sql_dayOffReleasePoolClass = MethodClass.GetSQLControl<DayOffReleasePoolClass>();

                object[] obj_form = sql_dayOffScheduleFormClass
                    .GetRowsByDefult(null, "form_name", form_name)
                    .FirstOrDefault();

                if (obj_form == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();

                object[] obj_option = sql_staffDayOffOptionClass
                    .GetRowsByDefult(null, "GUID", option_guid)
                    .FirstOrDefault();

                if (obj_option == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到放假選項(option_guid={option_guid})";
                    return returnData.JsonSerializationt();
                }

                StaffDayOffOptionClass option = obj_option.SQLToClass<StaffDayOffOptionClass>();

                if (option.form_guid != form.GUID)
                {
                    returnData.Code = -200;
                    returnData.Result = "option 不屬於指定表單";
                    return returnData.JsonSerializationt();
                }

                if (option.is_force_ff == "true")
                {
                    returnData.Code = -200;
                    returnData.Result = "系統強制放假(FF)不可取消釋出";
                    return returnData.JsonSerializationt();
                }

                if (option.is_from_release == "true")
                {
                    returnData.Code = -200;
                    returnData.Result = "此放假選項為接手釋出名額，不可使用本 API 取消";
                    return returnData.JsonSerializationt();
                }

                if (option.is_released != "true")
                {
                    returnData.Code = -200;
                    returnData.Result = "此放假選項尚未釋出，無法取消";
                    return returnData.JsonSerializationt();
                }

                DayOffReleasePoolClass pool = null;
                if (!option.release_pool_guid.StringIsEmpty())
                {
                    pool = sql_dayOffReleasePoolClass
                        .GetRowsByDefult(null, "GUID", option.release_pool_guid)
                        .SQLToClass<DayOffReleasePoolClass>()
                        .FirstOrDefault();
                }

                if (pool == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到對應釋出池(release_pool_guid={option.release_pool_guid})";
                    return returnData.JsonSerializationt();
                }

                if (pool.claimed_slots.StringToInt32() > 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "此釋出名額已被接手，不可取消釋出";
                    return returnData.JsonSerializationt();
                }

                string now = DateTime.Now.ToDateTimeString();

                // ✅ 同步最新規則：取消釋出只清除釋出狀態，不改動目前 selected_xxx
                option.is_released = "false";
                option.released_at = DateTime.MinValue.ToDateTimeString();
                option.released_dayoff_type = "";
                option.release_pool_guid = "";
                if ((option.dayoff_source_type ?? "").Trim().ToUpper() == "RELEASED_SOURCE")
                {
                    option.dayoff_source_type = "";
                }
                option.updated_at = now;
                option.NormalizeSelection();

                pool.status = "CANCELLED";
                pool.updated_at = now;
                pool.version_no = (pool.version_no.StringToInt32() + 1).ToString();

                sql_staffDayOffOptionClass.UpdateByDefulteExtra(null, option.ClassToSQL<StaffDayOffOptionClass>());
                sql_dayOffReleasePoolClass.UpdateByDefulteExtra(null, pool.ClassToSQL<DayOffReleasePoolClass>());

                returnData.Code = 200;
                returnData.Data = new
                {
                    option,
                    release_pool = pool
                };
                returnData.Result = "取消釋出成功";
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
        /// 接手指定釋出池名額作為填洞休假（claim_released_dayoff_option）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL
        /// POST /phar_roster_api/DayOffSchedule/claim_released_dayoff_option
        ///
        /// ## 📘 功能說明
        /// 讓指定人員接手某筆 OPEN 狀態的 DayOffReleasePoolClass。
        ///
        /// ## 併發保護
        /// 1. API lock
        /// 2. DB 條件更新（remaining_slots > 0 且 version_no 比對）
        ///
        /// ## 注意
        /// - 一個池第一版只允許一人接手
        /// - 必須有對應 item
        /// - 不可接自己釋出的池
        /// </remarks>
        /// <param name="returnData">封裝 API 請求內容的物件。</param>
        /// <returns>回傳 JSON 字串。</returns>
        [HttpPost("claim_released_dayoff_option")]
        public async Task<string> claim_released_dayoff_option([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "claim_released_dayoff_option";

            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string staff_guid = GetVal("staff_guid");
                string release_pool_guid = GetVal("release_pool_guid");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 form_name";
                    return returnData.JsonSerializationt();
                }
                if (staff_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 staff_guid";
                    return returnData.JsonSerializationt();
                }
                if (release_pool_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 release_pool_guid";
                    return returnData.JsonSerializationt();
                }

                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();
                var sql_dayOffReleasePoolClass = MethodClass.GetSQLControl<DayOffReleasePoolClass>();

                object[] obj_form = sql_dayOffScheduleFormClass
                    .GetRowsByDefult(null, "form_name", form_name)
                    .FirstOrDefault();

                if (obj_form == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();

                lock (_dayoffReleasePoolLock)
                {
                    object[] obj_pool = sql_dayOffReleasePoolClass
                        .GetRowsByDefult(null, "GUID", release_pool_guid)
                        .FirstOrDefault();

                    if (obj_pool == null)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"找不到釋出池(release_pool_guid={release_pool_guid})";
                        return returnData.JsonSerializationt();
                    }

                    DayOffReleasePoolClass pool = obj_pool.SQLToClass<DayOffReleasePoolClass>();

                    if (pool.form_guid != form.GUID)
                    {
                        returnData.Code = -200;
                        returnData.Result = "釋出池不屬於指定表單";
                        return returnData.JsonSerializationt();
                    }

                    if (pool.status != "OPEN")
                    {
                        returnData.Code = -200;
                        returnData.Result = $"釋出池目前不可接手(status={pool.status})";
                        return returnData.JsonSerializationt();
                    }

                    if (pool.remaining_slots.StringToInt32() <= 0)
                    {
                        returnData.Code = -200;
                        returnData.Result = "釋出池名額不足";
                        return returnData.JsonSerializationt();
                    }

                    if (pool.source_staff_guid == staff_guid)
                    {
                        returnData.Code = -200;
                        returnData.Result = "不可接手自己釋出的名額";
                        return returnData.JsonSerializationt();
                    }

                    List<StaffDayOffOptionClass> allOptions = sql_staffDayOffOptionClass
                        .GetRowsByDefult(null, "form_guid", form.GUID)
                        .SQLToClass<StaffDayOffOptionClass>();

                    List<DayOffScheduleItemClass> allItems = sql_dayOffScheduleItemClass
                        .GetRowsByDefult(null, "form_guid", form.GUID)
                        .SQLToClass<DayOffScheduleItemClass>();

                    List<DayOffScheduleDayClass> allDays = sql_dayOffScheduleDayClass
                        .GetRowsByDefult(null, "form_guid", form.GUID)
                        .SQLToClass<DayOffScheduleDayClass>();

                    bool alreadyClaimed = allOptions.Any(x =>
                        x != null &&
                        x.is_from_release == "true" &&
                        x.release_pool_guid == release_pool_guid);

                    if (alreadyClaimed)
                    {
                        returnData.Code = -200;
                        returnData.Result = "此釋出池已被其他人接手";
                        return returnData.JsonSerializationt();
                    }

                    string targetDate = pool.date.StringToDateTime().ToDateString('-');

                    DayOffScheduleDayClass day = allDays
                        .Where(x => x.date.StringToDateTime().ToDateString('-') == targetDate)
                        .FirstOrDefault();

                    if (day == null)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"找不到日期資料({targetDate})";
                        return returnData.JsonSerializationt();
                    }

                    DayOffScheduleItemClass targetItem = allItems
                        .Where(x =>
                            x.staff_guid == staff_guid &&
                            x.date.StringToDateTime().ToDateString('-') == targetDate)
                        .FirstOrDefault();

                    if (targetItem == null)
                    {
                        returnData.Code = -200;
                        returnData.Result = "接手者於該日無排休資料(item)，不可填洞";
                        return returnData.JsonSerializationt();
                    }

                    StaffDayOffOptionClass targetOption = allOptions
                        .Where(x =>
                            x.staff_guid == staff_guid &&
                            x.date.StringToDateTime().ToDateString('-') == targetDate)
                        .FirstOrDefault();

                    if (targetOption != null)
                    {
                        targetOption.NormalizeSelection();

                        bool hasExistingSelection =
                            targetOption.selected_full == "true" ||
                            targetOption.selected_half_am == "true" ||
                            targetOption.selected_half_pm == "true";

                        if (hasExistingSelection)
                        {
                            returnData.Code = -200;
                            returnData.Result = "接手者該日已有休假選擇，不可再接手填洞";
                            return returnData.JsonSerializationt();
                        }

                        if (targetOption.is_force_ff == "true")
                        {
                            returnData.Code = -200;
                            returnData.Result = "接手者該日為系統強制放假(FF)，不可填洞";
                            return returnData.JsonSerializationt();
                        }
                    }

                    List<StaffDayOffOptionClass> dateOptions = allOptions
                        .Where(x => x != null && x.date.StringToDateTime().ToDateString('-') == targetDate)
                        .ToList();

                    int amSelected = 0;
                    int pmSelected = 0;

                    foreach (var option in dateOptions)
                    {
                        option.NormalizeSelection();

                        if (option.selected_full == "true")
                        {
                            amSelected++;
                            pmSelected++;
                        }
                        else
                        {
                            if (option.selected_half_am == "true") amSelected++;
                            if (option.selected_half_pm == "true") pmSelected++;
                        }
                    }

                    int amCapacity = day.am_max_dayoff_count.StringToInt32();
                    int pmCapacity = day.pm_max_dayoff_count.StringToInt32();

                    int amRemaining = amCapacity - amSelected;
                    int pmRemaining = pmCapacity - pmSelected;

                    string releaseType = (pool.release_dayoff_type ?? "").Trim().ToUpper();

                    if (releaseType == "FULL")
                    {
                        if (!(amRemaining > 0 && pmRemaining > 0))
                        {
                            returnData.Code = -200;
                            returnData.Result = "整天名額不足";
                            return returnData.JsonSerializationt();
                        }
                    }
                    else if (releaseType == "HALF_AM")
                    {
                        if (!(amRemaining > 0))
                        {
                            returnData.Code = -200;
                            returnData.Result = "上午名額不足";
                            return returnData.JsonSerializationt();
                        }
                    }
                    else if (releaseType == "HALF_PM")
                    {
                        if (!(pmRemaining > 0))
                        {
                            returnData.Code = -200;
                            returnData.Result = "下午名額不足";
                            return returnData.JsonSerializationt();
                        }
                    }
                    else
                    {
                        returnData.Code = -200;
                        returnData.Result = $"釋出類型異常({pool.release_dayoff_type})";
                        return returnData.JsonSerializationt();
                    }

                    // DB 條件更新搶名額（樂觀鎖）
                    string oldVersion = pool.version_no;
                    string now = DateTime.Now.ToDateTimeString();
                    int newClaimed = pool.claimed_slots.StringToInt32() + 1;
                    int newRemaining = pool.remaining_slots.StringToInt32() - 1;
                    int newVersion = pool.version_no.StringToInt32() + 1;
                    string newStatus = newRemaining <= 0 ? "CLOSED" : "OPEN";

                    string sql = $@"
UPDATE {sql_dayOffReleasePoolClass.Database}.{sql_dayOffReleasePoolClass.TableName}
SET claimed_slots = @claimed_slots,
    remaining_slots = @remaining_slots,
    version_no = @version_no,
    status = @status,
    updated_at = @updated_at
WHERE GUID = @guid
  AND status = 'OPEN'
  AND remaining_slots > 0
  AND version_no = @old_version_no;
";
                    var parameters = new
                    {
                        claimed_slots = newClaimed.ToString(),
                        remaining_slots = newRemaining.ToString(),
                        version_no = newVersion.ToString(),
                        status = newStatus,
                        updated_at = now,
                        guid = pool.GUID,
                        old_version_no = oldVersion
                    };

                    int affectCount = sql_dayOffReleasePoolClass.ExecuteNonQuery(sql, parameters);
                    if (affectCount <= 0)
                    {
                        returnData.Code = -200;
                        returnData.Result = "搶洞失敗，可能已被其他人先接手";
                        return returnData.JsonSerializationt();
                    }

                    if (targetOption == null)
                    {
                        targetOption = new StaffDayOffOptionClass();
                        targetOption.GUID = Guid.NewGuid().ToString();
                        targetOption.form_guid = form.GUID;
                        targetOption.item_guid = targetItem.GUID;
                        targetOption.staff_guid = staff_guid;
                        targetOption.date = pool.date;
                        targetOption.suggested_dates_list = new List<string>() { targetDate };
                        targetOption.is_any_date = "false";
                        targetOption.assigned_shift = "OFF";
                        targetOption.can_full = "false";
                        targetOption.can_half_am = "false";
                        targetOption.can_half_pm = "false";
                        targetOption.is_forbidden = "false";
                        targetOption.is_force_ff = "false";
                        targetOption.force_ff_at = DateTime.MinValue.ToDateTimeString();
                        targetOption.is_released = "false";
                        targetOption.released_at = DateTime.MinValue.ToDateTimeString();
                        targetOption.released_dayoff_type = "";
                        targetOption.updated_at = now;
                    }

                    targetOption.ClearSelection();

                    if (releaseType == "FULL")
                    {
                        targetOption.can_full = "true";
                        targetOption.can_half_am = "false";
                        targetOption.can_half_pm = "false";
                        targetOption.SelectFullDay(pool.date);
                    }
                    else if (releaseType == "HALF_AM")
                    {
                        targetOption.can_full = "false";
                        targetOption.can_half_am = "true";
                        targetOption.can_half_pm = "false";
                        targetOption.SelectHalfAM(pool.date);
                    }
                    else if (releaseType == "HALF_PM")
                    {
                        targetOption.can_full = "false";
                        targetOption.can_half_am = "false";
                        targetOption.can_half_pm = "true";
                        targetOption.SelectHalfPM(pool.date);
                    }

                    targetOption.is_from_release = "true";
                    targetOption.source_option_guid = pool.source_option_guid;
                    targetOption.release_pool_guid = pool.GUID;
                    targetOption.dayoff_source_type = "HOLE_FILL";
                    targetOption.updated_at = now;
                    targetOption.NormalizeSelection();

                    targetItem.option_guid = targetOption.GUID;
                    targetItem.updated_at = now;

                    if (allOptions.Any(x => x.GUID == targetOption.GUID))
                    {
                        sql_staffDayOffOptionClass.UpdateByDefulteExtra(null, targetOption.ClassToSQL<StaffDayOffOptionClass>());
                    }
                    else
                    {
                        sql_staffDayOffOptionClass.AddRows(null, new List<object[]>() { targetOption.ClassToSQL<StaffDayOffOptionClass>() });
                    }

                    sql_dayOffScheduleItemClass.UpdateByDefulteExtra(null, targetItem.ClassToSQL<DayOffScheduleItemClass>());

                    // 回讀 pool 最新狀態
                    DayOffReleasePoolClass poolAfter = sql_dayOffReleasePoolClass
                        .GetRowsByDefult(null, "GUID", pool.GUID)
                        .SQLToClass<DayOffReleasePoolClass>()
                        .FirstOrDefault();

                    returnData.Code = 200;
                    returnData.Data = new
                    {
                        claimed_option = targetOption,
                        release_pool = poolAfter
                    };
                    returnData.Result = "接手釋出名額成功";
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
        /// 取消接手釋出名額（cancel_claim_released_dayoff_option）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL
        /// POST /phar_roster_api/DayOffSchedule/cancel_claim_released_dayoff_option
        ///
        /// ## 📘 功能說明
        /// 讓接手者取消一筆 HOLE_FILL 類型的休假，並歸還釋出池名額。
        ///
        /// ## 功能內容
        /// 1. 驗證 claim option 是否存在
        /// 2. 驗證此 option 為 is_from_release=true
        /// 3. 回補 release_pool：
        ///    - claimed_slots -1
        ///    - remaining_slots +1
        ///    - status = OPEN
        /// 4. 清空 claim option 選擇
        /// 5. 清空 source_option_guid / release_pool_guid / is_from_release
        /// </remarks>
        /// <param name="returnData">封裝 API 請求內容的物件。</param>
        /// <returns>回傳 JSON 字串。</returns>
        [HttpPost("cancel_claim_released_dayoff_option")]
        public string cancel_claim_released_dayoff_option([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "cancel_claim_released_dayoff_option";

            try
            {
                string GetVal(string key) =>
                  returnData.ValueAry.FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                  ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string option_guid = GetVal("option_guid");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 form_name";
                    return returnData.JsonSerializationt();
                }
                if (option_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 option_guid";
                    return returnData.JsonSerializationt();
                }

                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();
                var sql_dayOffReleasePoolClass = MethodClass.GetSQLControl<DayOffReleasePoolClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();

                object[] obj_form = sql_dayOffScheduleFormClass
                    .GetRowsByDefult(null, "form_name", form_name)
                    .FirstOrDefault();

                if (obj_form == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();

                object[] obj_option = sql_staffDayOffOptionClass
                    .GetRowsByDefult(null, "GUID", option_guid)
                    .FirstOrDefault();

                if (obj_option == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到接手 option(option_guid={option_guid})";
                    return returnData.JsonSerializationt();
                }

                StaffDayOffOptionClass option = obj_option.SQLToClass<StaffDayOffOptionClass>();

                if (option.form_guid != form.GUID)
                {
                    returnData.Code = -200;
                    returnData.Result = "option 不屬於指定表單";
                    return returnData.JsonSerializationt();
                }

                if (option.is_from_release != "true" || option.release_pool_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "此 option 非接手釋出名額，不可取消接手";
                    return returnData.JsonSerializationt();
                }

                lock (_dayoffReleasePoolLock)
                {
                    object[] obj_pool = sql_dayOffReleasePoolClass
                        .GetRowsByDefult(null, "GUID", option.release_pool_guid)
                        .FirstOrDefault();

                    if (obj_pool == null)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"找不到釋出池(release_pool_guid={option.release_pool_guid})";
                        return returnData.JsonSerializationt();
                    }

                    DayOffReleasePoolClass pool = obj_pool.SQLToClass<DayOffReleasePoolClass>();

                    string now = DateTime.Now.ToDateTimeString();

                    int claimed = pool.claimed_slots.StringToInt32();
                    int remain = pool.remaining_slots.StringToInt32();

                    if (claimed > 0) claimed--;
                    remain++;

                    pool.claimed_slots = claimed.ToString();
                    pool.remaining_slots = remain.ToString();
                    pool.status = "OPEN";
                    pool.version_no = (pool.version_no.StringToInt32() + 1).ToString();
                    pool.updated_at = now;

                    option.ClearSelection();
                    option.is_from_release = "false";
                    option.source_option_guid = "";
                    option.release_pool_guid = "";
                    option.dayoff_source_type = "";
                    option.updated_at = now;
                    option.NormalizeSelection();

                    sql_dayOffReleasePoolClass.UpdateByDefulteExtra(null, pool.ClassToSQL<DayOffReleasePoolClass>());
                    sql_staffDayOffOptionClass.UpdateByDefulteExtra(null, option.ClassToSQL<StaffDayOffOptionClass>());

                    object[] obj_item = sql_dayOffScheduleItemClass
                        .GetRowsByDefult(null, "GUID", option.item_guid)
                        .FirstOrDefault();

                    if (obj_item != null)
                    {
                        DayOffScheduleItemClass item = obj_item.SQLToClass<DayOffScheduleItemClass>();
                        item.option_guid = option.GUID;
                        item.updated_at = now;
                        sql_dayOffScheduleItemClass.UpdateByDefulteExtra(null, item.ClassToSQL<DayOffScheduleItemClass>());
                    }

                    returnData.Code = 200;
                    returnData.Data = new
                    {
                        cancelled_option = option,
                        release_pool = pool
                    };
                    returnData.Result = "取消接手成功";
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
        /// 查詢指定人員於指定日期可接手的填洞休假名額（升級版 get_hole_fill_available_options）
        /// </summary>
        /// <remarks>
        /// ## 🌐 API URL
        /// POST /phar_roster_api/dayOffSchedule/get_hole_fill_available_options
        ///
        /// ## 📘 功能說明
        /// 依據指定表單、指定人員、指定日期，查詢當日所有可接手的釋出池名額。
        ///
        /// ## 升級版規則
        /// 候選來源改由 DayOffReleasePoolClass 提供，僅列出：
        /// - status = OPEN
        /// - remaining_slots > 0
        /// - date = 指定日期
        ///
        /// 再進一步判斷：
        /// - 不可接自己釋出的名額
        /// - 當日時段容量是否足夠
        /// - 該池是否已被接滿
        ///
        /// ## 注意
        /// - 本 API 只回傳候選清單，不會執行接手
        /// </remarks>
        /// <param name="returnData">returnData 物件，主要使用 ValueAry 作為參數輸入。</param>
        /// <returns>回傳 JSON 字串。</returns>
        [HttpPost("get_hole_fill_available_options")]
        public string get_hole_fill_available_options([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "/phar_roster_api/dayOffSchedule/get_hole_fill_available_options";

            try
            {
                string GetVal(string key) =>
                    returnData.ValueAry?
                    .FirstOrDefault(x => x.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                    ?.Split('=')[1];

                string form_name = GetVal("form_name");
                string staff_guid = GetVal("staff_guid");
                string date = GetVal("date");

                if (form_name.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 form_name";
                    return returnData.JsonSerializationt();
                }

                if (staff_guid.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 staff_guid";
                    return returnData.JsonSerializationt();
                }

                if (date.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = "未提供 date";
                    return returnData.JsonSerializationt();
                }

                string targetDate = date.StringToDateTime().ToDateString('-');
                if (targetDate.StringIsEmpty())
                {
                    returnData.Code = -200;
                    returnData.Result = $"日期格式錯誤({date})";
                    return returnData.JsonSerializationt();
                }

                var sql_dayOffScheduleFormClass = MethodClass.GetSQLControl<DayOffScheduleFormClass>();
                var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
                var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
                var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();
                var sql_dayOffReleasePoolClass = MethodClass.GetSQLControl<DayOffReleasePoolClass>();

                object[] obj_form = sql_dayOffScheduleFormClass
                    .GetRowsByDefult(null, "form_name", form_name)
                    .FirstOrDefault();

                if (obj_form == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到表單名稱({form_name})";
                    return returnData.JsonSerializationt();
                }

                DayOffScheduleFormClass form = obj_form.SQLToClass<DayOffScheduleFormClass>();

                DayOffScheduleDayClass day = sql_dayOffScheduleDayClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffScheduleDayClass>()
                    .Where(x => x.date.StringToDateTime().ToDateString('-') == targetDate)
                    .FirstOrDefault();

                if (day == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"找不到日期資料({targetDate})";
                    return returnData.JsonSerializationt();
                }

                List<StaffDayOffOptionClass> allOptions = sql_staffDayOffOptionClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<StaffDayOffOptionClass>();

                List<DayOffReleasePoolClass> allPools = sql_dayOffReleasePoolClass
                    .GetRowsByDefult(null, "form_guid", form.GUID)
                    .SQLToClass<DayOffReleasePoolClass>();

                // 當日全部 option，計算容量
                List<StaffDayOffOptionClass> dateOptions = allOptions
                    .Where(x => x != null && x.date.StringToDateTime().ToDateString('-') == targetDate)
                    .ToList();

                int amSelected = 0;
                int pmSelected = 0;

                foreach (var option in dateOptions)
                {
                    option.NormalizeSelection();

                    if (option.selected_full == "true")
                    {
                        amSelected++;
                        pmSelected++;
                    }
                    else
                    {
                        if (option.selected_half_am == "true") amSelected++;
                        if (option.selected_half_pm == "true") pmSelected++;
                    }
                }

                int amCapacity = day.am_max_dayoff_count.StringToInt32();
                int pmCapacity = day.pm_max_dayoff_count.StringToInt32();

                int amRemaining = amCapacity - amSelected;
                int pmRemaining = pmCapacity - pmSelected;

                // 當日可接手的 pool
                List<DayOffReleasePoolClass> candidatePools = allPools
                    .Where(x =>
                        x != null &&
                        x.date.StringToDateTime().ToDateString('-') == targetDate &&
                        x.status == "OPEN" &&
                        x.remaining_slots.StringToInt32() > 0)
                    .ToList();

                // 若已有 claim option，也可用來輔助判斷是否已被接滿
                HashSet<string> claimedPoolGuids = allOptions
                    .Where(x =>
                        x != null &&
                        x.is_from_release == "true" &&
                        x.release_pool_guid.StringIsEmpty() == false)
                    .Select(x => x.release_pool_guid)
                    .ToHashSet();

                List<HoleFillAvailableOptionDto> result = new List<HoleFillAvailableOptionDto>();

                foreach (var pool in candidatePools)
                {
                    HoleFillAvailableOptionDto dto = new HoleFillAvailableOptionDto();
                    dto.release_pool_guid = pool.GUID;
                    dto.source_option_guid = pool.source_option_guid;
                    dto.source_item_guid = pool.source_item_guid;
                    dto.source_staff_guid = pool.source_staff_guid;
                    dto.source_staff_id = "";
                    dto.source_staff_name = "";
                    dto.date = targetDate;
                    dto.release_dayoff_type = pool.release_dayoff_type;
                    dto.total_slots = pool.total_slots;
                    dto.claimed_slots = pool.claimed_slots;
                    dto.remaining_slots = pool.remaining_slots;
                    dto.am_remaining_count = amRemaining.ToString();
                    dto.pm_remaining_count = pmRemaining.ToString();
                    dto.can_claim = "true";
                    dto.block_reason = "";

                    // 1. 不可接自己釋出的名額
                    if (pool.source_staff_guid == staff_guid)
                    {
                        dto.can_claim = "false";
                        dto.block_reason = "不可接手自己釋出的名額";
                    }

                    // 2. 保守判斷：若已被 claim 過，直接擋
                    if (dto.can_claim == "true" && claimedPoolGuids.Contains(pool.GUID))
                    {
                        dto.can_claim = "false";
                        dto.block_reason = "此釋出池已被其他人接手";
                    }

                    // 3. 依釋出類型判斷容量
                    if (dto.can_claim == "true")
                    {
                        string releaseType = (pool.release_dayoff_type ?? "").Trim().ToUpper();

                        if (releaseType == "FULL")
                        {
                            if (!(amRemaining > 0 && pmRemaining > 0))
                            {
                                dto.can_claim = "false";
                                dto.block_reason = "整天名額不足";
                            }
                        }
                        else if (releaseType == "HALF_AM")
                        {
                            if (!(amRemaining > 0))
                            {
                                dto.can_claim = "false";
                                dto.block_reason = "上午名額不足";
                            }
                        }
                        else if (releaseType == "HALF_PM")
                        {
                            if (!(pmRemaining > 0))
                            {
                                dto.can_claim = "false";
                                dto.block_reason = "下午名額不足";
                            }
                        }
                        else
                        {
                            dto.can_claim = "false";
                            dto.block_reason = $"釋出類型異常({pool.release_dayoff_type})";
                        }
                    }

                    result.Add(dto);
                }

                returnData.Code = 200;
                returnData.Result = "取得可填洞名額成功";
                returnData.Data = result;
                return returnData.JsonSerializationt(true);
            }
            catch (Exception ex)
            {
                returnData.Code = -500;
                returnData.Result = ex.Message;
                return returnData.JsonSerializationt();
            }
        }

        /// <summary>
        /// 下載班表匯入空白模板 Excel
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於下載「班表匯入空白模板 Excel」。
        ///
        /// 使用者可參考既有排班 PDF 或原始 Excel，
        /// 將對應班別與日期的人員簡名，直接填入模板中，
        /// 後續再由系統進行預覽檢查與正式匯入。
        ///
        /// 此模板設計目標：
        /// - 不需要一筆一筆新增班表資料
        /// - 可讓使用者直接複製 Excel 內容貼入
        /// - 保持未來班表匯入格式可擴充性
        ///
        /// ---
        ///
        /// ## 🌐 URL
        /// ```text
        /// /phar_roster_api/dayOffSchedule/download_schedule_import_template_excel
        /// ```
        ///
        /// ## 🧩 模板設計概念
        /// 模板採用「班別列、日期欄」方式：
        ///
        /// - A 欄：班別類型
        /// - B 欄：時段
        /// - C 欄之後：日期欄（01 ~ 31）
        ///
        /// 每一格代表：
        /// 「某一天、某班別、某時段」的人員簡名內容。
        ///
        /// 使用者可依照排班 PDF 或原始 Excel，將對應內容貼入儲存格。
        ///
        /// ---
        ///
        /// ## 📥 Request JSON 範例
        /// ```json
        /// {
        ///   "Method": "download_schedule_import_template_excel",
        ///   "ValueAry": [],
        ///   "Data": {}
        /// }
        /// ```
        ///
        /// > 本 API 不需要 year_month，也不需要其他參數。
        ///
        /// ---
        ///
        /// ## 📤 Response 說明（成功）
        /// 成功時回傳 Excel 檔案串流。
        ///
        /// ### Response Header 範例
        /// ```text
        /// Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
        /// Content-Disposition: attachment; filename="schedule_import_template.xlsx"; filename*=UTF-8''%E7%8F%AD%E8%A1%A8%E5%8C%AF%E5%85%A5%E7%A9%BA%E7%99%BD%E6%A8%A1%E6%9D%BF.xlsx
        /// Access-Control-Expose-Headers: Content-Disposition, Content-Length, Content-Type
        /// ```
        ///
        /// ### 檔名
        /// ```text
        /// 班表匯入空白模板.xlsx
        /// ```
        ///
        /// ---
        ///
        /// ## ❌ Response JSON 範例（錯誤）
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "download_schedule_import_template_excel",
        ///   "Result": "例外：Excel 模板產生失敗"
        /// }
        /// ```
        ///
        /// ---
        ///
        /// ## 📑 Excel 模板結構
        ///
        /// ### Sheet 名稱
        /// ```text
        /// 班表匯入
        /// ```
        ///
        /// ### 第一列欄位
        /// | 欄位 | 說明 |
        /// |------|------|
        /// | A1 | 班別類型 |
        /// | B1 | 時段 |
        /// | C1 ~ AG1 | 日期欄（01 ~ 31） |
        ///
        /// ### 日期欄說明
        /// 本模板固定提供 31 天欄位，
        /// 由使用者依實際月份自行填寫或對照使用。
        ///
        /// 欄位顯示為：
        /// ```text
        /// 01, 02, 03, 04 ... 31
        /// ```
        ///
        /// ---
        ///
        /// ## 📋 固定班別列順序
        /// 模板內班別列固定如下：
        ///
        /// | 班別類型 | 時段 |
        /// |------|------|
        /// | 國定假日 | 08:00-12:00 |
        /// | 假日門診 | 07:30-16:00 |
        /// | 假日門診 | 08:00-16:00 |
        /// | 假日急診 | 08:00-16:00 |
        /// | 化療 | 08:00-12:00 |
        /// | TPN | 08:00-16:00 |
        /// | 中藥局 | 12:30-21:00 |
        /// | 小夜門診 | 12:30-21:00 |
        /// | 小夜門診 | 13:30-22:00 |
        /// | 小夜門診 | 14:30-23:00 |
        /// | 小夜門診 | 15:30-23:59 |
        /// | 小夜急診 | 16:00-23:59 |
        /// | 小夜其他 | 12:30-21:00 |
        /// | 大夜門診 | 00:00-08:00 |
        /// | 大夜急診 | 00:00-08:00 |
        ///
        /// ---
        ///
        /// ## 📝 儲存格填寫規則
        /// 日期欄中的每個儲存格，代表：
        /// 「該日期、該班別、該時段」的人員簡名內容。
        ///
        /// ### 規則
        /// 1. 一個字代表一位人員簡名。
        /// 2. 不使用任何分隔符號。
        /// 3. 不加空白。
        /// 4. 不加中括號。
        /// 5. 沒有人就留空。
        /// 6. 國定假日不需另外設定日期，只看「國定假日 08:00-12:00」那列是否有人。
        ///
        /// ### 合法範例
        /// ```text
        /// 亭庭璇詩
        /// 均甄
        /// 品
        /// 曼能
        /// ```
        ///
        /// ### 不合法範例
        /// ```text
        /// 亭、庭、璇、詩
        /// [品]陳媚松顏靖
        /// 亭 庭 璇 詩
        /// ```
        ///
        /// ---
        ///
        /// ## 📊 範例表格
        /// 下列為模板範例：
        ///
        /// | 班別類型 | 時段 | 01 | 02 | 03 | 04 | 05 |
        /// |------|------|------|------|------|------|------|
        /// | 國定假日 | 08:00-12:00 |  |  |  |  |  |
        /// | 假日門診 | 07:30-16:00 |  |  |  |  |  |
        /// | 假日門診 | 08:00-16:00 |  |  |  |  |  |
        /// | 假日急診 | 08:00-16:00 |  |  |  |  |  |
        /// | 化療 | 08:00-12:00 |  |  |  |  |  |
        /// | TPN | 08:00-16:00 |  |  |  |  |  |
        /// | 中藥局 | 12:30-21:00 |  |  |  |  |  |
        /// | 小夜門診 | 12:30-21:00 |  |  |  |  |  |
        /// | 小夜門診 | 13:30-22:00 |  |  |  |  |  |
        /// | 小夜門診 | 14:30-23:00 |  |  |  |  |  |
        /// | 小夜門診 | 15:30-23:59 |  |  |  |  |  |
        /// | 小夜急診 | 16:00-23:59 |  |  |  |  |  |
        /// | 小夜其他 | 12:30-21:00 |  |  |  |  |  |
        /// | 大夜門診 | 00:00-08:00 |  |  |  |  |  |
        /// | 大夜急診 | 00:00-08:00 |  |  |  |  |  |
        ///
        /// ---
        ///
        /// ## 🚀 簡易操作方式
        ///
        /// ### 方式一：直接手動填寫
        /// 1. 下載空白模板。
        /// 2. 依照班別與日期，在對應儲存格填入人員簡名。
        /// 3. 若該班別當天沒有人，留空即可。
        ///
        /// ### 方式二：參考原始 Excel 或排班資料複製貼上
        /// 例如：
        /// - 使用者看到 5/1 的「假日門診 07:30-16:00」是 `亭庭璇詩`
        /// - 可直接將 `亭庭璇詩` 貼入該列 01 欄位
        ///
        /// 再例如：
        /// - 使用者看到 5/1 的「假日急診 08:00-16:00」是 `均甄`
        /// - 可直接將 `均甄` 貼入該列 01 欄位
        ///
        /// ### 操作重點
        /// - 一格就是一個班別 + 一天
        /// - 一格內直接貼整串簡名
        /// - 不需逐筆輸入人員資料
        ///
        /// ---
        ///
        /// ## ⚙️ 工程實作要求
        ///
        /// ### 1. 本 API 不需要任何參數
        /// - 不帶 year_month
        /// - 不依月份變動欄位數
        /// - 固定產出 31 天欄位
        ///
        /// ### 2. 固定輸出內容
        /// - 1 張 Sheet
        /// - Sheet 名稱為 `班表匯入`
        /// - 固定 15 列班別
        /// - 固定日期欄 01 ~ 31
        ///
        /// ### 3. 樣式建議
        /// - 第一列粗體
        /// - 第一列底色淡灰
        /// - 所有儲存格加框線
        /// - A/B 欄固定寬度
        /// - 日期欄置中
        /// - 凍結前 2 欄與第 1 列
        ///
        /// ### 4. 欄寬建議
        /// - A 欄（班別類型）：18
        /// - B 欄（時段）：16
        /// - 日期欄：10 ~ 12
        ///
        /// ---
        ///
        /// ## 📌 注意事項
        /// - 本 API 僅負責下載空白模板，不做資料驗證。
        /// - 實際匯入驗證（例如簡名重複、找不到人員、格式異常）
        ///   應由後續的 preview / confirm 匯入 API 處理。
        /// - 國定假日是否有人上班，直接填在 `國定假日 08:00-12:00` 那列，不需額外提供國定假日清單。
        /// - 中藥局、TPN、化療這些列只填人員簡名，不加前綴。
        /// </remarks>
        /// <param name="returnData">統一封裝的請求物件，本 API 不需額外參數。</param>
        /// <returns>成功時回傳 Excel 檔案串流，失敗時回傳 JSON 錯誤訊息。</returns>
        [HttpPost("download_schedule_import_template_excel")]
        public IActionResult download_schedule_import_template_excel([FromBody] returnData returnData)
        {
            var timer = new MyTimerBasic();
            returnData.Method = "download_schedule_import_template_excel";

            try
            {
                var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
                var sheet = workbook.CreateSheet("班表匯入");

                var rows = new List<(string ShiftType, string Time)>
        {
            ("國定假日", "08:00-12:00"),
            ("假日門診", "07:30-16:00"),
            ("假日門診", "08:00-16:00"),
            ("假日急診", "08:00-16:00"),
            ("化療", "08:00-12:00"),
            ("TPN", "08:00-16:00"),
            ("中藥局", "12:30-21:00"),
            ("小夜門診", "12:30-21:00"),
            ("小夜門診", "13:30-22:00"),
            ("小夜門診", "14:30-23:00"),
            ("小夜門診", "15:30-23:59"),
            ("小夜急診", "16:00-23:59"),
            ("小夜其他", "12:30-21:00"),
            ("大夜門診", "00:00-08:00"),
            ("大夜急診", "00:00-08:00"),
        };

                // ===== 字型 =====
                var fontHeader = workbook.CreateFont();
                fontHeader.FontName = "微軟正黑體";
                fontHeader.FontHeightInPoints = 11;
                fontHeader.IsBold = true;

                var fontNormal = workbook.CreateFont();
                fontNormal.FontName = "微軟正黑體";
                fontNormal.FontHeightInPoints = 10;

                // ===== 標題樣式 =====
                var styleHeader = workbook.CreateCellStyle();
                styleHeader.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                styleHeader.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                styleHeader.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                styleHeader.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
                styleHeader.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
                styleHeader.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
                styleHeader.SetFont(fontHeader);
                styleHeader.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
                styleHeader.FillPattern = NPOI.SS.UserModel.FillPattern.SolidForeground;

                // ===== 一般儲存格樣式 =====
                var styleCell = workbook.CreateCellStyle();
                styleCell.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                styleCell.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;
                styleCell.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                styleCell.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
                styleCell.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
                styleCell.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
                styleCell.WrapText = true;
                styleCell.SetFont(fontNormal);

                // ===== 說明區樣式 =====
                var styleNote = workbook.CreateCellStyle();
                styleNote.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;
                styleNote.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Top;
                styleNote.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                styleNote.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
                styleNote.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
                styleNote.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
                styleNote.WrapText = true;
                styleNote.SetFont(fontNormal);

                // ===== 第 1 列 =====
                var headerRow = sheet.CreateRow(0);
                headerRow.HeightInPoints = 22;

                var cellA1 = headerRow.CreateCell(0);
                cellA1.SetCellValue("班別類型");
                cellA1.CellStyle = styleHeader;

                var cellB1 = headerRow.CreateCell(1);
                cellB1.SetCellValue("時段");
                cellB1.CellStyle = styleHeader;

                for (int i = 1; i <= 31; i++)
                {
                    var cell = headerRow.CreateCell(i + 1);
                    cell.SetCellValue(i.ToString("00"));
                    cell.CellStyle = styleHeader;
                }

                // ===== 固定班別列 =====
                int rowIndex = 1;
                foreach (var rowData in rows)
                {
                    var row = sheet.CreateRow(rowIndex);
                    row.HeightInPoints = 20;

                    var cell0 = row.CreateCell(0);
                    cell0.SetCellValue(rowData.ShiftType);
                    cell0.CellStyle = styleCell;

                    var cell1 = row.CreateCell(1);
                    cell1.SetCellValue(rowData.Time);
                    cell1.CellStyle = styleCell;

                    for (int i = 1; i <= 31; i++)
                    {
                        var cell = row.CreateCell(i + 1);
                        cell.SetCellValue("");
                        cell.CellStyle = styleCell;
                    }

                    rowIndex++;
                }

                // ===== 說明區 =====
                rowIndex += 1;
                var noteRow = sheet.CreateRow(rowIndex);
                noteRow.HeightInPoints = 130;

                var noteCell = noteRow.CreateCell(0);
                noteCell.SetCellValue(
                    "【填寫說明】\n" +
                    "1. 每個字代表一位人員簡名。\n" +
                    "2. 同一格不可有重複簡名。\n" +
                    "3. 不可輸入空白、逗號、中括號等特殊符號。\n" +
                    "4. 沒有人請留空。\n" +
                    "5. 國定假日是否有人上班，直接填在「國定假日 08:00-12:00」那列。\n" +
                    "6. 中藥局、TPN、化療只填人員簡名，不加前綴。\n" +
                    "7. 第一列日期可由使用者自行改成 01~31 或實際日期。"
                );
                noteCell.CellStyle = styleNote;

                // 合併說明區
                sheet.AddMergedRegion(new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 0, 32));
                for (int i = 1; i <= 32; i++)
                {
                    var c = noteRow.CreateCell(i);
                    c.CellStyle = styleNote;
                }

                // ===== 欄寬 =====
                sheet.SetColumnWidth(0, 18 * 256); // 班別類型
                sheet.SetColumnWidth(1, 16 * 256); // 時段
                for (int i = 2; i <= 32; i++)
                {
                    sheet.SetColumnWidth(i, 11 * 256); // 日期
                }

                // ===== 凍結窗格 =====
                sheet.CreateFreezePane(2, 1);

                // ===== 輸出 =====
                byte[] bytes;
                using (var ms = new MemoryStream())
                {
                    workbook.Write(ms);
                    bytes = ms.ToArray();
                }

                var stream = new MemoryStream(bytes);
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                // 英文 fallback，避免某些瀏覽器 filename 亂碼
                string downloadFileName = "schedule_import_template.xlsx";
                // 中文檔名放在 filename*
                string displayFileName = "班表匯入空白模板.xlsx";
                string utf8FileName = Uri.EscapeDataString(displayFileName);

                Response.Headers["Content-Disposition"] =
                    $"attachment; filename=\"{downloadFileName}\"; filename*=UTF-8''{utf8FileName}";
                Response.Headers["Access-Control-Expose-Headers"] =
                    "Content-Disposition, Content-Length, Content-Type";

                return File(stream, contentType);
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"例外：{ex.Message}";
                return new JsonResult(returnData);
            }
        }

        /// <summary>
        /// 預覽匯入班表 Excel，並回傳附有驗證結果標記的 Excel
        /// </summary>
        /// <remarks>
        /// ## 📌 用途
        /// 本 API 用於預覽「班表匯入 Excel」內容，僅進行解析與驗證，不會寫入資料庫。
        ///
        /// 使用者上傳由「班表匯入空白模板」填寫完成的 Excel 後，
        /// 系統會逐格解析班別與日期內容，並檢查格式與人員簡名是否正確。
        ///
        /// 驗證完成後，系統會回傳一份新的 Excel：
        /// 1. 原始 Sheet 內，將有內容的儲存格依驗證結果上色
        /// 2. 驗證成功格子標記為綠色
        /// 3. 驗證失敗格子標記為紅色
        /// 4. 固定班別列錯誤時，A/B 欄位標記為紅色
        /// 5. 每格會加上批註，顯示解析結果或錯誤原因
        /// 6. 額外新增 `預覽結果` Sheet，列出所有驗證結果明細
        ///
        /// ---
        ///
        /// ## 🌐 URL
        /// ```text
        /// /phar_roster_api/dayOffSchedule/preview_import_schedule_excel
        /// ```
        ///
        /// ## Method
        /// ```text
        /// POST
        /// ```
        ///
        /// ## Content-Type
        /// ```text
        /// multipart/form-data
        /// ```
        ///
        /// ---
        ///
        /// ## 📥 上傳欄位
        /// | 欄位名稱 | 型別 | 必填 | 說明 |
        /// |------|------|------|------|
        /// | file | IFormFile | ✅ | 要預覽的 Excel 檔案（僅支援 .xlsx） |
        ///
        /// ---
        ///
        /// ## 📑 Excel 模板前提
        /// 本 API 預設使用者上傳的是由「班表匯入空白模板」填寫完成的 Excel。
        ///
        /// 模板規則如下：
        /// 1. 第 1 列為標題列
        /// 2. A1 = 班別類型
        /// 3. B1 = 時段
        /// 4. C1 ~ AG1 = 日期欄（01 ~ 31）
        /// 5. 第 2 列開始為固定班別列
        ///
        /// 固定班別列定義如下：
        ///
        /// | 班別類型 | 時段 |
        /// |------|------|
        /// | 國定假日 | 08:00-12:00 |
        /// | 假日門診 | 07:30-16:00 |
        /// | 假日門診 | 08:00-16:00 |
        /// | 假日急診 | 08:00-16:00 |
        /// | 化療 | 08:00-12:00 |
        /// | TPN | 08:00-16:00 |
        /// | 中藥局 | 12:30-21:00 |
        /// | 小夜門診 | 12:30-21:00 |
        /// | 小夜門診 | 13:30-22:00 |
        /// | 小夜門診 | 14:30-23:00 |
        /// | 小夜門診 | 15:30-23:59 |
        /// | 小夜急診 | 16:00-23:59 |
        /// | 小夜其他 | 12:30-21:00 |
        /// | 大夜門診 | 00:00-08:00 |
        /// | 大夜急診 | 00:00-08:00 |
        ///
        /// ---
        ///
        /// ## 📝 儲存格填寫規則
        /// 日期欄中的每個儲存格，代表：
        /// 「該日期、該班別、該時段」的人員簡名內容。
        ///
        /// ### 規則
        /// 1. 一個字代表一位人員簡名
        /// 2. 不使用任何分隔符號
        /// 3. 不加空白
        /// 4. 不加中括號
        /// 5. 沒有人就留空
        ///
        /// ### 合法範例
        /// ```text
        /// 亭庭璇詩
        /// 均甄
        /// 品
        /// 曼能
        /// ```
        ///
        /// ### 不合法範例
        /// ```text
        /// 亭、庭、璇、詩
        /// [品]陳媚松顏靖
        /// 亭 庭 璇 詩
        /// ```
        ///
        /// ---
        ///
        /// ## ✅ 驗證規則
        /// 本 API 會做以下檢查：
        ///
        /// ### 1. 檔案檢查
        /// - 必須有上傳檔案
        /// - 副檔名必須為 `.xlsx`
        ///
        /// ### 2. Excel 基本結構檢查
        /// - 必須至少有一張 Sheet
        /// - 第一張 Sheet 必須可讀取
        /// - 第 1 列必須存在
        /// - A1 必須為 `班別類型`
        /// - B1 必須為 `時段`
        ///
        /// ### 3. 固定班別列檢查
        /// - 第 2 列到第 16 列必須符合固定班別定義
        /// - 若班別類型或時段不符，該列視為錯誤
        ///
        /// ### 4. 日期欄檢查
        /// - 日期欄必須為 01 ~ 31
        /// - 若欄位標題不是合法日期欄，對應格子會視為錯誤
        ///
        /// ### 5. 儲存格內容格式檢查
        /// - 不可包含空白
        /// - 不可包含全形空白
        /// - 不可包含逗號
        /// - 不可包含頓號
        /// - 不可包含中括號
        /// - 不可包含換行
        /// - 不可包含 Tab
        ///
        /// ### 6. 簡名解析檢查
        /// - 一個字代表一位人
        /// - 同一格不可有重複簡名
        /// - 每個簡名都必須能找到 staff
        /// - 每個簡名必須唯一對應到一位 staff
        ///
        /// ### 7. 同日跨班別重複檢查
        /// - 同一天同一人不可出現在多個班別
        /// - 若重複，會回傳錯誤
        ///
        /// ---
        ///
        /// ## 📤 Response 說明
        ///
        /// ### 成功
        /// 成功時不回傳 JSON，而是直接回傳「驗證後 Excel 檔案」。
        ///
        /// 驗證後 Excel 內容：
        /// 1. 原始匯入 Sheet：
        ///    - 成功格子：綠色
        ///    - 錯誤格子：紅色
        ///    - 每格附加批註
        /// 2. 預覽結果 Sheet：
        ///    - 顯示總格數、成功格數、失敗格數
        ///    - 列出每一格的解析結果
        ///
        /// ### 檔名
        /// ```text
        /// schedule_import_preview_result.xlsx
        /// ```
        ///
        /// ### 下載時顯示名稱
        /// ```text
        /// 班表匯入預覽結果.xlsx
        /// ```
        ///
        /// ---
        ///
        /// ## ❌ 錯誤回傳 JSON 範例
        ///
        /// ### 未收到上傳檔案
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "preview_import_schedule_excel",
        ///   "Result": "未收到上傳檔案"
        /// }
        /// ```
        ///
        /// ### 檔案格式錯誤
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "preview_import_schedule_excel",
        ///   "Result": "僅支援 .xlsx Excel 檔案"
        /// }
        /// ```
        ///
        /// ### Excel 標題列錯誤
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "preview_import_schedule_excel",
        ///   "Result": "Excel 標題格式錯誤，前兩欄必須為「班別類型 / 時段」"
        /// }
        /// ```
        ///
        /// ### 系統例外
        /// ```json
        /// {
        ///   "Code": -200,
        ///   "Method": "preview_import_schedule_excel",
        ///   "Result": "例外：Object reference not set to an instance of an object."
        /// }
        /// ```
        ///
        /// ---
        ///
        /// ## 🚀 使用流程
        /// 1. 先下載 `download_schedule_import_template_excel` 空白模板
        /// 2. 在 Excel 內填入班表資料
        /// 3. 上傳到本 API 預覽
        /// 4. 系統回傳附顏色與批註的 Excel
        /// 5. 使用者直接查看哪格成功、哪格錯誤
        /// 6. 修正後再重新上傳
        ///
        /// ---
        ///
        /// ## 📌 注意事項
        /// - 本 API 僅做預覽與驗證，不會寫入資料庫。
        /// - 本 API 依賴 Staff 主檔資料，用於比對簡名是否合法。
        /// - Staff 簡名必須唯一，否則該簡名會判定為錯誤。
        /// - 本 API 目前僅支援單字簡名規則。
        /// - 若同一天同一人出現在多個班別，會回傳錯誤。
        /// </remarks>
        /// <param name="file">上傳的 Excel 檔案（.xlsx）</param>
        /// <returns>成功時回傳附驗證結果標記的 Excel 檔案，失敗時回傳 JSON 錯誤訊息。</returns>
        [HttpPost("preview_import_schedule_excel")]
        public IActionResult preview_import_schedule_excel(IFormFile file)
        {
            var timer = new MyTimerBasic();
            returnData returnData = new returnData();
            returnData.Method = "preview_import_schedule_excel";

            try
            {
                if (file == null || file.Length == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = "未收到上傳檔案";
                    return new JsonResult(returnData);
                }

                string ext = Path.GetExtension(file.FileName)?.ToLower();
                if (ext != ".xlsx")
                {
                    returnData.Code = -200;
                    returnData.Result = "僅支援 .xlsx Excel 檔案";
                    return new JsonResult(returnData);
                }

                List<ImportScheduleTemplateRow> templateRows = ImportScheduleTemplateDefinition.GetRows();

                List<StaffClass> staffClasses = staff.GetStaffs(new List<string>() { "pageSize=10000" }).staffClasses;
                if (staffClasses == null) staffClasses = new List<StaffClass>();

                Dictionary<string, List<StaffClass>> simpleNameMap =
                    ImportScheduleStaffHelper.BuildSimpleNameMap(staffClasses);

                PreviewImportScheduleExcelResponse preview = new PreviewImportScheduleExcelResponse();
                preview.file_name = file.FileName;

                Dictionary<string, string> dayStaffUsedMap = new Dictionary<string, string>();

                using (var stream = file.OpenReadStream())
                {
                    XSSFWorkbook workbook = new XSSFWorkbook(stream);
                    ISheet sourceSheet = workbook.GetSheetAt(0);

                    if (sourceSheet == null)
                    {
                        returnData.Code = -200;
                        returnData.Result = "Excel 沒有可讀取的 Sheet";
                        return new JsonResult(returnData);
                    }

                    int oldPreviewSheetIndex = workbook.GetSheetIndex("預覽結果");
                    if (oldPreviewSheetIndex >= 0)
                    {
                        workbook.RemoveSheetAt(oldPreviewSheetIndex);
                    }

                    IRow headerRow = sourceSheet.GetRow(0);
                    if (headerRow == null)
                    {
                        returnData.Code = -200;
                        returnData.Result = "Excel 標題列不存在";
                        return new JsonResult(returnData);
                    }

                    string headerA = ImportScheduleExcelHelper.GetCellString(headerRow.GetCell(0));
                    string headerB = ImportScheduleExcelHelper.GetCellString(headerRow.GetCell(1));

                    if (headerA != "班別類型" || headerB != "時段")
                    {
                        returnData.Code = -200;
                        returnData.Result = "Excel 標題格式錯誤，前兩欄必須為「班別類型 / 時段」";
                        return new JsonResult(returnData);
                    }

                    Dictionary<int, string> dateColumnMap =
                        ImportScheduleExcelHelper.BuildDateColumnMap(headerRow, 2, 32);

                    ICellStyle successStyle = CreatePreviewCellStyle(workbook, HSSFColor.LightGreen.Index);
                    ICellStyle errorStyle = CreatePreviewCellStyle(workbook, HSSFColor.Rose.Index);
                    ICellStyle warningStyle = CreatePreviewCellStyle(workbook, HSSFColor.LightYellow.Index);
                    ICellStyle resultHeaderStyle = CreatePreviewHeaderStyle(workbook);
                    ICellStyle resultCellStyle = CreatePreviewNormalStyle(workbook);

                    IDrawing drawing = sourceSheet.CreateDrawingPatriarch();

                    int totalCells = 0;
                    int successCells = 0;
                    int errorCells = 0;

                    for (int rowIndex = 1; rowIndex <= templateRows.Count; rowIndex++)
                    {
                        IRow row = sourceSheet.GetRow(rowIndex);
                        ImportScheduleTemplateRow expected = templateRows[rowIndex - 1];

                        if (row == null)
                        {
                            IRow createdRow = sourceSheet.CreateRow(rowIndex);
                            ICell aCell = createdRow.CreateCell(0);
                            ICell bCell = createdRow.CreateCell(1);
                            aCell.SetCellValue("");
                            bCell.SetCellValue("");
                            aCell.CellStyle = errorStyle;
                            bCell.CellStyle = errorStyle;

                            AddCellComment(drawing, aCell, $"第 {rowIndex + 1} 列不存在，應為固定班別列：{expected.ShiftType} / {expected.ShiftTime}");
                            AddCellComment(drawing, bCell, $"第 {rowIndex + 1} 列不存在，應為固定班別列：{expected.ShiftType} / {expected.ShiftTime}");

                            PreviewImportScheduleExcelCellResult rowErr = new PreviewImportScheduleExcelCellResult
                            {
                                row_index = (rowIndex + 1).ToString(),
                                column_index = "1",
                                date_text = "",
                                shift_type = "",
                                shift_time = "",
                                raw_text = "",
                                parsed_simple_names = "",
                                parsed_staff_ids = "",
                                parsed_staff_names = "",
                                is_success = "false",
                                error_message = $"第 {rowIndex + 1} 列不存在，應為固定班別列：{expected.ShiftType} / {expected.ShiftTime}"
                            };

                            preview.results.Add(rowErr);
                            errorCells++;
                            continue;
                        }

                        string shiftType = ImportScheduleExcelHelper.GetCellString(row.GetCell(0));
                        string shiftTime = ImportScheduleExcelHelper.GetCellString(row.GetCell(1));

                        if (!ImportScheduleExcelHelper.IsTemplateRowMatched(shiftType, shiftTime, expected))
                        {
                            ICell shiftTypeCell = row.GetCell(0) ?? row.CreateCell(0);
                            ICell shiftTimeCell = row.GetCell(1) ?? row.CreateCell(1);

                            shiftTypeCell.CellStyle = errorStyle;
                            shiftTimeCell.CellStyle = errorStyle;

                            string rowErrorMessage = $"第 {rowIndex + 1} 列班別定義錯誤，應為「{expected.ShiftType} / {expected.ShiftTime}」";

                            AddCellComment(drawing, shiftTypeCell, rowErrorMessage);
                            AddCellComment(drawing, shiftTimeCell, rowErrorMessage);

                            PreviewImportScheduleExcelCellResult rowErr = new PreviewImportScheduleExcelCellResult
                            {
                                row_index = (rowIndex + 1).ToString(),
                                column_index = "1",
                                date_text = "",
                                shift_type = shiftType,
                                shift_time = shiftTime,
                                raw_text = "",
                                parsed_simple_names = "",
                                parsed_staff_ids = "",
                                parsed_staff_names = "",
                                is_success = "false",
                                error_message = rowErrorMessage
                            };

                            preview.results.Add(rowErr);
                            errorCells++;
                        }

                        for (int col = 2; col <= 32; col++)
                        {
                            ICell cell = row.GetCell(col);
                            string rawText = ImportScheduleExcelHelper.GetCellString(cell);
                            if (rawText.StringIsEmpty()) continue;

                            totalCells++;

                            string dateText = dateColumnMap.ContainsKey(col) ? dateColumnMap[col] : "";

                            PreviewImportScheduleExcelCellResult result = new PreviewImportScheduleExcelCellResult
                            {
                                row_index = (rowIndex + 1).ToString(),
                                column_index = (col + 1).ToString(),
                                date_text = dateText,
                                shift_type = shiftType,
                                shift_time = shiftTime,
                                raw_text = rawText,
                                parsed_simple_names = "",
                                parsed_staff_ids = "",
                                parsed_staff_names = "",
                                is_success = "false",
                                error_message = ""
                            };

                            if (dateText.StringIsEmpty() || !ImportScheduleExcelHelper.IsValidDayHeader(dateText))
                            {
                                result.error_message = $"第 1 列第 {col + 1} 欄日期標題錯誤，應為 01~31";
                                if (cell != null)
                                {
                                    cell.CellStyle = errorStyle;
                                    AddCellComment(drawing, cell, result.error_message);
                                }
                                preview.results.Add(result);
                                errorCells++;
                                continue;
                            }

                            if (ImportScheduleExcelHelper.ContainsInvalidCharacters(rawText))
                            {
                                result.error_message = "內容格式錯誤，不可包含空白、逗號、頓號、中括號或換行";
                                if (cell != null)
                                {
                                    cell.CellStyle = errorStyle;
                                    AddCellComment(drawing, cell, result.error_message);
                                }
                                preview.results.Add(result);
                                errorCells++;
                                continue;
                            }

                            List<string> simpleNames = ImportScheduleExcelHelper.ParseSimpleNames(rawText);

                            List<string> duplicatedSimpleNames =
                                ImportScheduleExcelHelper.GetDuplicatedSimpleNames(simpleNames);

                            if (duplicatedSimpleNames.Count > 0)
                            {
                                result.error_message = $"同一格不可有重複簡名：{string.Join(",", duplicatedSimpleNames)}";
                                if (cell != null)
                                {
                                    cell.CellStyle = errorStyle;
                                    AddCellComment(drawing, cell, result.error_message);
                                }
                                preview.results.Add(result);
                                errorCells++;
                                continue;
                            }

                            ImportScheduleResolveResult resolveResult =
                                ImportScheduleStaffHelper.ResolveSimpleNames(simpleNameMap, simpleNames);

                            if (!resolveResult.IsSuccess)
                            {
                                result.error_message = resolveResult.ErrorMessage;
                                if (cell != null)
                                {
                                    cell.CellStyle = errorStyle;
                                    AddCellComment(drawing, cell, result.error_message);
                                }
                                preview.results.Add(result);
                                errorCells++;
                                continue;
                            }

                            bool hasDuplicateError = false;
                            foreach (ImportScheduleResolvedStaff resolvedStaff in resolveResult.Staffs)
                            {
                                string currentShiftInfo = $"{shiftType} {shiftTime}";
                                bool ok = ImportScheduleStaffHelper.TryCheckAndRegisterDailyDuplicate(
                                    dayStaffUsedMap,
                                    dateText,
                                    resolvedStaff,
                                    currentShiftInfo,
                                    out string duplicateError);

                                if (!ok)
                                {
                                    result.error_message = duplicateError;
                                    hasDuplicateError = true;
                                    break;
                                }
                            }

                            if (hasDuplicateError)
                            {
                                if (cell != null)
                                {
                                    cell.CellStyle = errorStyle;
                                    AddCellComment(drawing, cell, result.error_message);
                                }
                                preview.results.Add(result);
                                errorCells++;
                                continue;
                            }

                            result.parsed_simple_names = ImportScheduleStaffHelper.JoinSimpleNames(resolveResult.Staffs);
                            result.parsed_staff_ids = ImportScheduleStaffHelper.JoinStaffIds(resolveResult.Staffs);
                            result.parsed_staff_names = ImportScheduleStaffHelper.JoinStaffNames(resolveResult.Staffs);
                            result.is_success = "true";
                            result.error_message = "";

                            if (cell != null)
                            {
                                cell.CellStyle = successStyle;
                                AddCellComment(
                                    drawing,
                                    cell,
                                    $"驗證成功\n簡名：{result.parsed_simple_names}\n工號：{result.parsed_staff_ids}\n姓名：{result.parsed_staff_names}"
                                );
                            }

                            preview.results.Add(result);
                            successCells++;
                        }
                    }

                    preview.total_cells = totalCells.ToString();
                    preview.success_cells = successCells.ToString();
                    preview.error_cells = errorCells.ToString();

                    ISheet resultSheet = workbook.CreateSheet("預覽結果");

                    int resultRowIndex = 0;

                    IRow summaryTitleRow = resultSheet.CreateRow(resultRowIndex++);
                    ICell summaryTitleCell = summaryTitleRow.CreateCell(0);
                    summaryTitleCell.SetCellValue("班表匯入預覽結果");
                    summaryTitleCell.CellStyle = resultHeaderStyle;
                    resultSheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, 8));

                    IRow summaryRow = resultSheet.CreateRow(resultRowIndex++);
                    SetResultCell(summaryRow, 0, "檔名", resultHeaderStyle);
                    SetResultCell(summaryRow, 1, preview.file_name, resultCellStyle);
                    SetResultCell(summaryRow, 2, "總格數", resultHeaderStyle);
                    SetResultCell(summaryRow, 3, preview.total_cells, resultCellStyle);
                    SetResultCell(summaryRow, 4, "成功格數", resultHeaderStyle);
                    SetResultCell(summaryRow, 5, preview.success_cells, resultCellStyle);
                    SetResultCell(summaryRow, 6, "失敗格數", resultHeaderStyle);
                    SetResultCell(summaryRow, 7, preview.error_cells, resultCellStyle);

                    resultRowIndex++;

                    IRow resultHeaderRow = resultSheet.CreateRow(resultRowIndex++);
                    string[] headers = new string[]
                    {
                "列號",
                "欄號",
                "日期",
                "班別類型",
                "時段",
                "原始內容",
                "解析簡名",
                "解析工號",
                "解析姓名",
                "是否成功",
                "錯誤訊息"
                    };

                    for (int i = 0; i < headers.Length; i++)
                    {
                        SetResultCell(resultHeaderRow, i, headers[i], resultHeaderStyle);
                    }

                    foreach (PreviewImportScheduleExcelCellResult item in preview.results)
                    {
                        IRow rr = resultSheet.CreateRow(resultRowIndex++);
                        SetResultCell(rr, 0, item.row_index, resultCellStyle);
                        SetResultCell(rr, 1, item.column_index, resultCellStyle);
                        SetResultCell(rr, 2, item.date_text, resultCellStyle);
                        SetResultCell(rr, 3, item.shift_type, resultCellStyle);
                        SetResultCell(rr, 4, item.shift_time, resultCellStyle);
                        SetResultCell(rr, 5, item.raw_text, resultCellStyle);
                        SetResultCell(rr, 6, item.parsed_simple_names, resultCellStyle);
                        SetResultCell(rr, 7, item.parsed_staff_ids, resultCellStyle);
                        SetResultCell(rr, 8, item.parsed_staff_names, resultCellStyle);
                        SetResultCell(rr, 9, item.is_success, item.is_success == "true" ? successStyle : errorStyle);
                        SetResultCell(rr, 10, item.error_message, resultCellStyle);
                    }

                    for (int i = 0; i <= 10; i++)
                    {
                        resultSheet.SetColumnWidth(i, 18 * 256);
                    }
                    resultSheet.SetColumnWidth(5, 24 * 256);
                    resultSheet.SetColumnWidth(10, 40 * 256);

                    byte[] bytes;
                    using (var ms = new MemoryStream())
                    {
                        workbook.Write(ms);
                        bytes = ms.ToArray();
                    }

                    var outputStream = new MemoryStream(bytes);
                    string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                    string downloadFileName = "schedule_import_preview_result.xlsx";
                    string displayFileName = "班表匯入預覽結果.xlsx";
                    string utf8FileName = Uri.EscapeDataString(displayFileName);

                    Response.Headers["Content-Disposition"] =
                        $"attachment; filename=\"{downloadFileName}\"; filename*=UTF-8''{utf8FileName}";
                    Response.Headers["Access-Control-Expose-Headers"] =
                        "Content-Disposition, Content-Length, Content-Type";

                    return File(outputStream, contentType);
                }
            }
            catch (Exception ex)
            {
                returnData.Code = -200;
                returnData.Result = $"例外：{ex.Message}";
                return new JsonResult(returnData);
            }
        }

        /// <summary>
        /// 建立預覽用儲存格樣式（底色）
        /// </summary>
        private ICellStyle CreatePreviewCellStyle(IWorkbook workbook, short fillColor)
        {
            IFont font = workbook.CreateFont();
            font.FontName = "微軟正黑體";
            font.FontHeightInPoints = 10;

            ICellStyle style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.WrapText = true;
            style.FillForegroundColor = fillColor;
            style.FillPattern = FillPattern.SolidForeground;
            style.SetFont(font);

            return style;
        }
        /// <summary>
        /// 建立結果 Sheet 標題樣式
        /// </summary>
        private ICellStyle CreatePreviewHeaderStyle(IWorkbook workbook)
        {
            IFont font = workbook.CreateFont();
            font.FontName = "微軟正黑體";
            font.FontHeightInPoints = 11;
            font.IsBold = true;

            ICellStyle style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Center;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.WrapText = true;
            style.FillForegroundColor = HSSFColor.Grey25Percent.Index;
            style.FillPattern = FillPattern.SolidForeground;
            style.SetFont(font);

            return style;
        }
        /// <summary>
        /// 建立結果 Sheet 一般樣式
        /// </summary>
        private ICellStyle CreatePreviewNormalStyle(IWorkbook workbook)
        {
            IFont font = workbook.CreateFont();
            font.FontName = "微軟正黑體";
            font.FontHeightInPoints = 10;

            ICellStyle style = workbook.CreateCellStyle();
            style.Alignment = HorizontalAlignment.Left;
            style.VerticalAlignment = VerticalAlignment.Center;
            style.BorderTop = BorderStyle.Thin;
            style.BorderBottom = BorderStyle.Thin;
            style.BorderLeft = BorderStyle.Thin;
            style.BorderRight = BorderStyle.Thin;
            style.WrapText = true;
            style.SetFont(font);

            return style;
        }
        /// <summary>
        /// 新增或更新儲存格批註
        /// </summary>
        private void AddCellComment(IDrawing drawing, ICell cell, string commentText)
        {
            if (cell == null || drawing == null) return;
            if (commentText == null) commentText = "";

            IWorkbook workbook = cell.Sheet.Workbook;
            ICreationHelper factory = workbook.GetCreationHelper();

            IClientAnchor anchor = factory.CreateClientAnchor();
            anchor.Col1 = cell.ColumnIndex;
            anchor.Col2 = cell.ColumnIndex + 3;
            anchor.Row1 = cell.RowIndex;
            anchor.Row2 = cell.RowIndex + 4;

            IComment comment = drawing.CreateCellComment(anchor);
            comment.String = factory.CreateRichTextString(commentText);
            comment.Author = "System";
            cell.CellComment = comment;
        }
        /// <summary>
        /// 設定結果 Sheet 儲存格內容與樣式
        /// </summary>
        private void SetResultCell(IRow row, int colIndex, string text, ICellStyle style)
        {
            ICell cell = row.GetCell(colIndex) ?? row.CreateCell(colIndex);
            cell.SetCellValue(text ?? "");
            if (style != null) cell.CellStyle = style;
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
            //if (dt.StringToDateTime().DayOfWeek == DayOfWeek.Saturday)
            //{
            //    item.shift_requirement = BuildHolidayOffShiftRequirementJson(dt.StringToDateTime());
            //    item.selected_dayoff_type = "FF"; // 若你前端有使用，可留；不需要也可空字串
            //                                      // FF 一律整天假        
            //    opt.can_full = "false";
            //    opt.can_half_am = "true";
            //    opt.can_half_pm = "true";
            //    // ✅ 強制選擇整天假
            //    opt.selected_full = "false";
            //    opt.selected_half_am = "false";
            //    opt.selected_half_pm = "false";
            //    opt.is_any_date = "true";

            //    opt.is_force_ff = "false";
            //}

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
                if (itemDate.DayOfWeek == DayOfWeek.Sunday) dateTimeSuggestedDate = itemDate.AddDays(1);
            
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
          
                if (item.workShiftRequirement.department == "急診")
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
                        option.is_any_date = "true";
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
                            option.is_any_date = "true";
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
        /// <summary>
        /// 判斷指定人員在指定表單中的預留休是否已全部處理完成
        /// </summary>
        /// <param name="form_guid">表單 GUID</param>
        /// <param name="staff_guid">人員 GUID</param>
        /// <returns>true：已完成 / false：尚有預留休未處理</returns>
        private bool IsStaffReservedDayoffCompleted(string form_guid, string staff_guid)
        {
            var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

            List<StaffDayOffOptionClass> options = sql_staffDayOffOptionClass
                .GetRowsByDefult(null, "form_guid", form_guid)
                .SQLToClass<StaffDayOffOptionClass>()
                .Where(x => x != null && x.staff_guid == staff_guid)
                .ToList();

            foreach (var option in options)
            {
                if (option == null) continue;

                option.NormalizeSelection();

                // 排除填洞休假
                if ((option.dayoff_source_type ?? "").Trim().ToUpper() == "HOLE_FILL") continue;

                // 排除應休額度排休
                if (option.is_quota_dayoff == "true") continue;

                // 系統 FF 視為已完成
                if (option.is_force_ff == "true") continue;

                // 已釋出視為已完成
                if (option.is_released == "true") continue;

                // 已選擇視為已完成
                bool hasSelected =
                    option.selected_full == "true" ||
                    option.selected_half_am == "true" ||
                    option.selected_half_pm == "true";

                if (hasSelected) continue;

                // 仍有預留休未處理
                return false;
            }

            return true;
        }
        /// <summary>
        /// 取得指定人員於指定表單中的應休額度統計
        /// </summary>
        /// <param name="form_guid">表單 GUID</param>
        /// <param name="staff_guid">人員 GUID</param>
        /// <returns>應休總額、已使用額度、剩餘額度</returns>
        private GetStaffRemainingQuotaDayoffResponse GetStaffRemainingQuotaDayoff(string form_guid, string staff_guid)
        {
            var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
            var sql_dayOffScheduleItemClass = MethodClass.GetSQLControl<DayOffScheduleItemClass>();
            var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

            List<DayOffScheduleDayClass> days = sql_dayOffScheduleDayClass
                .GetRowsByDefult(null, "form_guid", form_guid)
                .SQLToClass<DayOffScheduleDayClass>()
                .OrderBy(x => x.date.StringToDateTime())
                .ToList();

            List<DayOffScheduleItemClass> items = sql_dayOffScheduleItemClass
                .GetRowsByDefult(null, "form_guid", form_guid)
                .SQLToClass<DayOffScheduleItemClass>()
                .Where(x => x != null && x.staff_guid == staff_guid)
                .ToList();

            List<StaffDayOffOptionClass> options = sql_staffDayOffOptionClass
                .GetRowsByDefult(null, "form_guid", form_guid)
                .SQLToClass<StaffDayOffOptionClass>()
                .Where(x => x != null && x.staff_guid == staff_guid)
                .ToList();

            Dictionary<string, DayOffScheduleItemClass> itemByDate = items
                .Where(x => x != null && x.date.StringIsEmpty() == false)
                .GroupBy(x => x.date.StringToDateTime().ToDateString('-'))
                .ToDictionary(g => g.Key, g => g.First());

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

            double quotaTotal = 0;
            double quotaUsedTotal = 0;

            // 1. 每個週六未上班 +0.5
            foreach (var day in days)
            {
                DateTime dt = day.date.StringToDateTime();
                if (dt == DateTime.MinValue) continue;

                if (dt.DayOfWeek == DayOfWeek.Saturday)
                {
                    string key = dt.ToDateString('-');
                    itemByDate.TryGetValue(key, out var item);

                    if (!HasSchedule(item))
                    {
                        quotaTotal += 0.5;
                    }
                }
            }

            // 2. option 貢獻的 quota
            foreach (var option in options)
            {
                if (option == null) continue;

                option.NormalizeSelection();

                if (option.is_any_date == "true")
                {
                    quotaTotal += 1;
                }

                if (option.is_released == "true")
                {
                    string releasedType = (option.released_dayoff_type ?? "").Trim().ToUpper();

                    if (releasedType == "FULL")
                    {
                        quotaTotal += 1;
                    }
                    else if (releasedType == "HALF_AM" || releasedType == "HALF_PM")
                    {
                        quotaTotal += 0.5;
                    }
                }

                if (option.is_quota_dayoff == "true")
                {
                    quotaUsedTotal += option.quota_used.StringToDouble();
                }
            }

            return new GetStaffRemainingQuotaDayoffResponse
            {
                staff_guid = staff_guid,
                quota_total = quotaTotal.ToString("0.##"),
                quota_used_total = quotaUsedTotal.ToString("0.##"),
                quota_remaining = (quotaTotal - quotaUsedTotal).ToString("0.##")
            };
        }
        /// <summary>
        /// 取得單一人員在指定表單中的應休排休公平機制統計
        /// </summary>
        /// <param name="form_guid">表單 GUID</param>
        /// <param name="staff_guid">人員 GUID</param>
        /// <returns>公平機制統計結果</returns>
        private StaffQuotaDayoffRuleSummary GetStaffQuotaDayoffRuleSummary(string form_guid, string staff_guid)
        {
            var quotaSummary = GetStaffRemainingQuotaDayoff(form_guid, staff_guid);

            var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

            List<StaffDayOffOptionClass> options = sql_staffDayOffOptionClass
                .GetRowsByDefult(null, "form_guid", form_guid)
                .SQLToClass<StaffDayOffOptionClass>()
                .Where(x => x != null && x.staff_guid == staff_guid)
                .ToList();

            int pmHalfUsedCount = 0;
            int saturdayUsedCount = 0;
            int pmHalfLimitCount = 2;
            int saturdayLimitCount = 1;
            bool hasExtraSaturdayLimit = false;

            foreach (var option in options)
            {
                if (option == null) continue;

                string quotaType = (option.quota_dayoff_type ?? "").Trim().ToUpper();

                if (option.is_quota_dayoff == "true")
                {
                    if (quotaType == "WEEKDAY_HALF_PM")
                    {
                        pmHalfUsedCount++;
                    }

                    if (quotaType == "SATURDAY_HALF_AM")
                    {
                        saturdayUsedCount++;
                    }
                }

                // 簡化版規則：
                // 若此人存在「週六或週日的釋出來源」，給額外 1 次週六排休機會
                DateTime dt = option.date.StringToDateTime();
                if ((option.is_released == "true" || option.is_any_date == "true") &&
                    dt != DateTime.MinValue &&
                    (dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday))
                {
                    hasExtraSaturdayLimit = true;
                }
            }

            if (hasExtraSaturdayLimit)
            {
                saturdayLimitCount += 1;
            }

            return new StaffQuotaDayoffRuleSummary
            {
                quota_total = quotaSummary.quota_total,
                quota_used_total = quotaSummary.quota_used_total,
                quota_remaining = quotaSummary.quota_remaining,
                pm_half_used_count = pmHalfUsedCount.ToString(),
                saturday_used_count = saturdayUsedCount.ToString(),
                pm_half_limit_count = pmHalfLimitCount.ToString(),
                saturday_limit_count = saturdayLimitCount.ToString(),
                has_extra_saturday_limit = hasExtraSaturdayLimit ? "true" : "false"
            };
        }
        /// <summary>
        /// 取得指定表單、指定日期的休假名額使用統計
        /// </summary>
        /// <param name="form_guid">表單 GUID</param>
        /// <param name="date">指定日期 yyyy-MM-dd</param>
        /// <returns>當日 AM / PM 名額使用狀況</returns>
        private DayOffDateQuotaUsageSummary GetDayOffDateQuotaUsageSummary(string form_guid, string date)
        {
            var sql_dayOffScheduleDayClass = MethodClass.GetSQLControl<DayOffScheduleDayClass>();
            var sql_staffDayOffOptionClass = MethodClass.GetSQLControl<StaffDayOffOptionClass>();

            string targetDate = date.StringToDateTime().ToDateString('-');

            DayOffScheduleDayClass day = sql_dayOffScheduleDayClass
                .GetRowsByDefult(null, "form_guid", form_guid)
                .SQLToClass<DayOffScheduleDayClass>()
                .Where(x => x.date.StringToDateTime().ToDateString('-') == targetDate)
                .FirstOrDefault();

            if (day == null)
            {
                return null;
            }

            List<StaffDayOffOptionClass> options = sql_staffDayOffOptionClass
                .GetRowsByDefult(null, "form_guid", form_guid)
                .SQLToClass<StaffDayOffOptionClass>()
                .Where(x => x != null && x.date.StringToDateTime().ToDateString('-') == targetDate)
                .ToList();

            int amUsed = 0;
            int pmUsed = 0;

            foreach (var option in options)
            {
                if (option == null) continue;

                option.NormalizeSelection();

                // 系統 FF / 預留休 / 應休排休 / 填洞
                // 只要已經選了，就占用當日名額
                if (option.selected_full == "true")
                {
                    amUsed += 1;
                    pmUsed += 1;
                    continue;
                }

                if (option.selected_half_am == "true")
                {
                    amUsed += 1;
                }

                if (option.selected_half_pm == "true")
                {
                    pmUsed += 1;
                }
            }

            int amMax = day.am_max_dayoff_count.StringToInt32();
            int pmMax = day.pm_max_dayoff_count.StringToInt32();

            return new DayOffDateQuotaUsageSummary
            {
                date = targetDate,
                am_max_dayoff_count = amMax.ToString(),
                pm_max_dayoff_count = pmMax.ToString(),
                am_used_count = amUsed.ToString(),
                pm_used_count = pmUsed.ToString(),
                am_remaining_count = (amMax - amUsed).ToString(),
                pm_remaining_count = (pmMax - pmUsed).ToString()
            };
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
