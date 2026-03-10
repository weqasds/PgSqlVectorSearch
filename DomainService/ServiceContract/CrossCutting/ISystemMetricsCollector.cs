using System;
using System.Threading.Tasks;

namespace DomainService.ServiceContract.CrossCutting;

/// <summary>
/// 系统指标收集器接口，用于收集系统性能指标
/// </summary>
public interface ISystemMetricsCollector
{
    /// <summary>
    /// 获取CPU使用率（百分比）
    /// </summary>
    /// <returns>CPU使用率（0-100）</returns>
    double GetCpuUsage();

    /// <summary>
    /// 获取内存使用率（百分比）
    /// </summary>
    /// <returns>内存使用率（0-100）</returns>
    double GetMemoryUsage();

    /// <summary>
    /// 获取活动线程数
    /// </summary>
    /// <returns>活动线程数</returns>
    int GetActiveThreads();

    /// <summary>
    /// 获取磁盘使用率（百分比）
    /// </summary>
    /// <returns>磁盘使用率（0-100）</returns>
    double GetDiskUsage();

    /// <summary>
    /// 获取系统运行时间
    /// </summary>
    /// <returns>系统运行时间</returns>
    TimeSpan GetSystemUptime();

    /// <summary>
    /// 获取应用程序运行时间
    /// </summary>
    /// <returns>应用程序运行时间</returns>
    TimeSpan GetApplicationUptime();

    /// <summary>
    /// 获取数据库连接池状态
    /// </summary>
    /// <returns>数据库连接池信息</returns>
    Task<DatabaseConnectionPoolInfo> GetDatabaseConnectionPoolInfoAsync();

    /// <summary>
    /// 获取HTTP请求统计
    /// </summary>
    /// <returns>HTTP请求统计信息</returns>
    Task<HttpRequestStatistics> GetHttpRequestStatisticsAsync();
}

/// <summary>
/// 数据库连接池信息
/// </summary>
public class DatabaseConnectionPoolInfo
{
    /// <summary>
    /// 活动连接数
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// 空闲连接数
    /// </summary>
    public int IdleConnections { get; set; }

    /// <summary>
    /// 连接池大小
    /// </summary>
    public int PoolSize { get; set; }

    /// <summary>
    /// 等待连接数
    /// </summary>
    public int WaitingConnections { get; set; }

    /// <summary>
    /// 连接超时次数
    /// </summary>
    public int TimeoutCount { get; set; }
}

/// <summary>
/// HTTP请求统计信息
/// </summary>
public class HttpRequestStatistics
{
    /// <summary>
    /// 总请求数
    /// </summary>
    public long TotalRequests { get; set; }

    /// <summary>
    /// 当前活动请求数
    /// </summary>
    public int ActiveRequests { get; set; }

    /// <summary>
    /// 请求失败数
    /// </summary>
    public long FailedRequests { get; set; }

    /// <summary>
    /// 平均响应时间（毫秒）
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// 请求率（请求数/秒）
    /// </summary>
    public double RequestRate { get; set; }
}