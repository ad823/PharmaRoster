using Basic;
using SQLUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PharmaRosterLib
{
    /// <summary>
    /// 人員登入驗證資料表 (Staff Authentication)
    /// 專責處理帳號、密碼雜湊、登入狀態與安全控管
    /// ⚠️ 不存放明碼密碼，僅存 PasswordHash + PasswordSalt
    /// ⚠️ 與 Staff 表為 1:1 關聯，透過 staff_id 對應
    /// </summary>
    [Description("staff_auth")]
    public class StaffAuthClass
    {
        /// <summary>
        /// 唯一識別碼 (GUID)
        /// 系統內部使用，作為資料表 Primary Key
        /// </summary>
        [Description("VARCHAR,50,PRIMARY")]
        public string GUID { get; set; }

        /// <summary>
        /// 人員編號
        /// 對應 staffs.staff_id
        /// 作為登入身分與人員資料的關聯鍵
        /// </summary>
        [Description("VARCHAR,50,INDEX")]
        public string staff_id { get; set; }

        /// <summary>
        /// 登入帳號
        /// 系統內唯一，不可重複
        /// 供使用者輸入進行登入驗證
        /// </summary>
        [Description("VARCHAR,50,UNIQUE")]
        public string account { get; set; }

        /// <summary>
        /// 密碼雜湊值 (Password Hash)
        /// 由「使用者密碼 + Salt」經安全雜湊演算法產生
        /// 為不可逆資料，無法還原原始密碼
        /// </summary>
        [Description("VARCHAR,255,NONE")]
        public string password_hash { get; set; }

        /// <summary>
        /// 密碼 Salt
        /// 每一帳號獨立產生的隨機字串
        /// 用於防止彩虹表與撞庫攻擊
        /// </summary>
        [Description("VARCHAR,100,NONE")]
        public string password_salt { get; set; }

        /// <summary>
        /// 帳號是否鎖定
        /// Y = 正常
        /// N = 鎖定（連續登入失敗或人工鎖定）
        /// </summary>
        [Description("VARCHAR,10,NONE")]
        public string is_locked { get; set; } = "N";

        /// <summary>
        /// 登入失敗累積次數
        /// 每次登入失敗遞增，成功登入後可歸零
        /// 可搭配 is_locked 作為帳號鎖定依據
        /// </summary>
        [Description("VARCHAR,10,NONE")]
        public string fail_count { get; set; } = "0";

        /// <summary>
        /// 最後一次成功登入時間
        /// 用於資安稽核與使用行為追蹤
        /// </summary>
        [Description("DATETIME,50,NONE")]
        public string last_login_at { get; set; }

        /// <summary>
        /// 最後一次成功登出時間
        /// 用於資安稽核與使用行為追蹤
        /// </summary>
        [Description("DATETIME,50,NONE")]
        public string last_logout_at { get; set; }

        /// <summary>
        /// 建立時間
        /// 帳號首次建立的時間點
        /// </summary>
        [Description("DATETIME,50,NONE")]
        public string created_at { get; set; }

        /// <summary>
        /// 更新時間
        /// 最近一次修改帳號資料（如鎖定、密碼更新）的時間
        /// </summary>
        [Description("DATETIME,50,NONE")]
        public string updated_at { get; set; }
    }
    /// <summary>
    /// 登入成功後回傳的資料（JWT + 使用者資訊）
    /// </summary>
    public class LoginData
    {
        [JsonPropertyName("staff_id")]
        public string StaffId { get; set; }

        [JsonPropertyName("account")]
        public string Account { get; set; }

        [JsonPropertyName("staff_name")]
        public string StaffName { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        // ===== JWT 相關 =====

        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; } // seconds
    }
}
