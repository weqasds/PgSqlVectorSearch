using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.InfrastructureShared.Services;

/// <summary>
/// HTTP请求跟踪服务
/// 提供线程安全的请求统计收集
/// </summary>
public class RequestTrackingService
{
    private readonly object _lock = new();
    private long _totalRequests;
    private long _failedRequests;
    private double _totalResponseTimeMs;
    private int _activeRequests;
    private DateTime? _firstRequestTime;
    private DateTime? _lastRequestTime;

    /// <summary>
    /// 记录一个HTTP请求
    /// </summary>
    public void RecordRequest(string method, string path, int statusCode, long elapsedMs, DateTime requestTime)
    {
        lock (_lock)
        {
            _totalRequests++;

            if (statusCode >= 400)
            {
                _failedRequests++;
            }

            _totalResponseTimeMs += elapsedMs;
            _firstRequestTime ??= requestTime;
            _lastRequestTime = requestTime;

            // 记录到日志（可选）
            if (elapsedMs > 1000) // 记录慢请求
            {
                Console.WriteLine($"Slow request: {method} {path} took {elapsedMs}ms");
            }
        }
    }

    /// <summary>
    /// 开始处理请求时调用
    /// </summary>
    public void RequestStarted()
    {
        lock (_lock)
        {
            _activeRequests++;
        }
    }

    /// <summary>
    /// 请求处理完成时调用
    /// </summary>
    public void RequestCompleted()
    {
        lock (_lock)
        {
            _activeRequests = Math.Max(0, _activeRequests - 1);
        }
    }

    /// <summary>
    /// 获取当前请求统计
    /// </summary>
    public RequestStatistics GetStatistics()
    {
        lock (_lock)
        {
            var averageResponseTime = _totalRequests > 0 ? _totalResponseTimeMs / _totalRequests : 0;
            var requestRate = 0.0;

            if (_firstRequestTime.HasValue && _lastRequestTime.HasValue)
            {
                var timeSpan = (_lastRequestTime.Value - _firstRequestTime.Value).TotalSeconds;
                if (timeSpan > 0)
                {
                    requestRate = _totalRequests / timeSpan;
                }
            }

            return new RequestStatistics
            {
                TotalRequests = _totalRequests,
                ActiveRequests = _activeRequests,
                FailedRequests = _failedRequests,
                AverageResponseTime = Math.Round(averageResponseTime, 2),
                RequestRate = Math.Round(requestRate, 2)
            };
        }
    }

    /// <summary>
    /// 重置统计（可选，用于测试）
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _totalRequests = 0;
            _failedRequests = 0;
            _totalResponseTimeMs = 0;
            _activeRequests = 0;
            _firstRequestTime = null;
            _lastRequestTime = null;
        }
    }
}

/// <summary>
/// 请求统计信息
/// </summary>
public class RequestStatistics
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