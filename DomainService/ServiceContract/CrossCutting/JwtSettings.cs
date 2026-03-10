using System;

namespace DomainService.ServiceContract.CrossCutting;

/// <summary>
/// JWT认证配置设置
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// 密钥，用于签名JWT令牌
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// 令牌发行者
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// 令牌受众
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// 访问令牌过期时间（分钟）
    /// </summary>
    public int AccessTokenExpiryMinutes { get; set; } = 60;

    /// <summary>
    /// 刷新令牌过期时间（分钟）
    /// </summary>
    public int RefreshTokenExpiryMinutes { get; set; } = 10080; // 7天

    /// <summary>
    /// 是否验证发行者
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// 是否验证受众
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// 是否验证令牌生存期
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// 时钟偏移（秒），用于处理时间同步问题
    /// </summary>
    public int ClockSkewSeconds { get; set; } = 5;
}