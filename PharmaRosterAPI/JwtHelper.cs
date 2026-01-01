using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// JWT Token 產生工具（.NET 5.0）
/// 
/// 功能說明：
/// 1. 使用 HMAC-SHA256 產生 JWT Access Token
/// 2. Token 內包含使用者識別資訊（Claims）
/// 3. 提供給前端 / 桌面程式 / 裝置，作為 API 呼叫的身分驗證依據
/// 
/// 使用時機：
/// - 使用者登入成功後
/// - 由 Server 產生並回傳給 Client
/// </summary>
public static class JwtHelper
{
    /// <summary>
    /// JWT 簽章用 Secret Key
    /// 
    /// 說明：
    /// - 用於 JWT 簽章與驗證
    /// - 長度建議至少 32 字元以上（HmacSha256 安全需求）
    /// - ⚠️ 僅 Server 端可知，嚴禁洩漏
    /// 
    /// 實務建議：
    /// - 正式環境請放在 appsettings.json 或環境變數
    /// - 不要硬編碼在程式碼中（此處為示範）
    /// </summary>
    public static readonly string SecretKey =
        "Qp7sN8F2KxZc9VtD6LwEJmA4RHy0Gf5b3S2C1uXKZPqY9r8WnM6eA0dB";

    /// <summary>
    /// JWT Issuer（簽發者）
    /// 
    /// 說明：
    /// - 表示「是誰發出這張 Token」
    /// - 驗證 Token 時會比對此值
    /// - 通常設定為公司或系統名稱
    /// </summary>
    public static readonly string Issuer = "Kutech.Auth";

    /// <summary>
    /// JWT Audience（接收者）
    /// 
    /// 說明：
    /// - 表示「哪些系統可以使用這張 Token」
    /// - 驗證 Token 時會比對此值
    /// - 常設定為 API 名稱或系統代碼
    /// </summary>
    public static readonly string Audience = "Kutech.API";

    /// <summary>
    /// 產生 JWT Access Token
    /// </summary>
    /// <param name="staffId">
    /// 人員編號  
    /// 用於後續 API 辨識登入者（核心身分）
    /// </param>
    /// <param name="account">
    /// 登入帳號  
    /// 方便除錯與稽核追蹤
    /// </param>
    /// <param name="role">
    /// 角色或權限代碼  
    /// 可用於 API 權限控管（如 Pharmacist / Admin）
    /// </param>
    /// <param name="expireMinutes">
    /// Token 有效時間（分鐘）  
    /// 預設 60 分鐘
    /// </param>
    /// <returns>
    /// JWT Token 字串（Base64 編碼）
    /// </returns>
    public static string GenerateToken(string staffId, string account, string role = "", int expireMinutes = 60)
    {
        // === 1. 將 SecretKey 轉成對稱式安全金鑰 ===
        // JWT 使用此金鑰進行簽章與驗證
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(SecretKey)
        );

        // === 2. 建立簽章憑證 ===
        // 使用 HmacSha256 演算法
        var signingCredentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256
        );

        // === 3. 建立 JWT Claims（Payload） ===
        // Claims 是 Token 內實際攜帶的身分資料
        Claim[] claims = new[]
        {
            // 人員編號（API 辨識使用者的主鍵）
            new Claim("staff_id", staffId),

            // 登入帳號（輔助資訊）
            new Claim("account", account),

            // 角色 / 權限
            new Claim("role", role),

            // JWT 唯一識別碼（防止 Token 重放）
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // === 4. 建立 JWT Token 本體 ===
        JwtSecurityToken token = new JwtSecurityToken(
            issuer: Issuer,                       // 簽發者
            audience: Audience,                   // 接收者
            claims: claims,                       // Payload
            notBefore: DateTime.UtcNow,           // Token 生效時間（UTC）
            expires: DateTime.UtcNow.AddMinutes(expireMinutes), // 到期時間
            signingCredentials: signingCredentials // 簽章設定
        );

        // === 5. 將 Token 轉為字串並回傳 ===
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
