namespace Infrastructure.InfrastructureShared.Services;

/// <summary>
/// Redis 基础设施配置。
/// </summary>
public sealed class RedisInfrastructureOptions
{
    public const string SectionName = "Redis";

    /// <summary>
    /// 是否启用 Redis 基础设施。
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Redis 连接字符串。
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// 连接超时（毫秒）。
    /// </summary>
    public int ConnectTimeoutMs { get; set; } = 2000;
}
