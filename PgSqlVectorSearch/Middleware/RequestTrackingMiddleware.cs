using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.InfrastructureShared.Services;
namespace PgSqlVectorSearch.Middleware;

/// <summary>
/// HTTP请求跟踪中间件
/// 用于收集请求统计信息，如请求数、响应时间等
/// </summary>
public class RequestTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestTrackingService _trackingService;

    public RequestTrackingMiddleware(RequestDelegate next, RequestTrackingService trackingService)
    {
        _next = next;
        _trackingService = trackingService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTime.UtcNow;

        try
        {
            await _next(context);
            stopwatch.Stop();

            // 记录成功请求
            _trackingService.RecordRequest(
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                startTime);
        }
        catch (Exception)
        {
            stopwatch.Stop();
            // 记录失败请求
            _trackingService.RecordRequest(
                context.Request.Method,
                context.Request.Path,
                500, // Internal Server Error
                stopwatch.ElapsedMilliseconds,
                startTime);
            throw;
        }
    }
}