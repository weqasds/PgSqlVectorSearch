using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Application.ApplicationServiceContract.CrossCutting;
using Infrastructure.EFCoreInfrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Infrastructure.InfrastructureShared.Services;

/// <summary>
/// 系统指标收集器实现
/// </summary>
public class SystemMetricsCollector : ISystemMetricsCollector
{
    private readonly ILogger<SystemMetricsCollector> _logger;
    private readonly Process _currentProcess;
    private readonly DateTime _startTime;
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _memoryCounter;
    private readonly RagDbContext _dbContext;
    private readonly RequestTrackingService _requestTrackingService;

    public SystemMetricsCollector(
        ILogger<SystemMetricsCollector> logger,
        RagDbContext dbContext,
        RequestTrackingService requestTrackingService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _requestTrackingService = requestTrackingService ?? throw new ArgumentNullException(nameof(requestTrackingService));
        _currentProcess = Process.GetCurrentProcess();
        _startTime = DateTime.UtcNow;

        InitializePerformanceCounters();
    }

    private void InitializePerformanceCounters()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                // 在Windows上使用PerformanceCounter
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");

                // 首次读取以初始化计数器
                if (_cpuCounter != null)
                {
                    _cpuCounter.NextValue();
                }

                _logger.LogInformation("Performance counters initialized on Windows platform.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize performance counters on Windows.");
            }
        }
        else
        {
            _logger.LogInformation("Performance counters not available on non-Windows platform.");
        }
    }

    public double GetCpuUsage()
    {
        try
        {
            if (_cpuCounter != null)
            {
                // 获取CPU使用率，需要两次调用间隔
                return Math.Round(_cpuCounter.NextValue(), 2);
            }
            else
            {
                // 模拟数据或使用其他方法
                return GetCpuUsageFallback();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get CPU usage.");
            return 0;
        }
    }

    public double GetMemoryUsage()
    {
        try
        {
            if (_memoryCounter != null && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: 获取可用内存并计算使用率
                var availableMemoryMB = _memoryCounter.NextValue();
                var totalMemoryMB = GetTotalPhysicalMemoryMB();

                if (totalMemoryMB > 0)
                {
                    var usedMemoryMB = totalMemoryMB - availableMemoryMB;
                    return Math.Round((usedMemoryMB / totalMemoryMB) * 100, 2);
                }
            }

            // 回退方案：使用进程内存
            return GetMemoryUsageFallback();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get memory usage.");
            return 0;
        }
    }

    public int GetActiveThreads()
    {
        try
        {
            return _currentProcess.Threads.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active threads.");
            return 0;
        }
    }

    public double GetDiskUsage()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var drive = Environment.SystemDirectory.Substring(0, 1);
                var driveInfo = new System.IO.DriveInfo(drive);

                if (driveInfo.IsReady)
                {
                    var totalSize = driveInfo.TotalSize;
                    var freeSpace = driveInfo.TotalFreeSpace;
                    var usedSpace = totalSize - freeSpace;

                    return Math.Round((usedSpace / (double)totalSize) * 100, 2);
                }
            }

            return 0; // 简化实现
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get disk usage.");
            return 0;
        }
    }

    public TimeSpan GetSystemUptime()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: 使用Environment.TickCount获取系统启动后的毫秒数
                var tickCount = Environment.TickCount;

                // TickCount可能为负数（如果系统运行超过24.9天）
                var uptimeMs = tickCount >= 0 ? tickCount : (int.MaxValue - (int)(-tickCount - 1));

                return TimeSpan.FromMilliseconds(uptimeMs);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Linux: 读取/proc/uptime文件
                var uptimeContent = System.IO.File.ReadAllText("/proc/uptime");
                var parts = uptimeContent.Split(' ');
                if (parts.Length > 0 && double.TryParse(parts[0], out var uptimeSeconds))
                {
                    return TimeSpan.FromSeconds(uptimeSeconds);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // macOS: 使用sysctl命令
                using var uptimeProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "sysctl",
                    Arguments = "-n kern.boottime",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });

                if (uptimeProcess != null)
                {
                    var output = uptimeProcess.StandardOutput.ReadToEnd();
                    uptimeProcess.WaitForExit();

                    // 解析输出格式：{ sec = 123456, usec = 789 }
                    // 简化实现，返回默认值
                }
            }

            // 如果不支持的平台，返回应用程序运行时间作为近似值
            _logger.LogWarning("Unsupported platform for system uptime detection, returning application uptime.");
            return GetApplicationUptime();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system uptime.");
            return GetApplicationUptime(); // 回退到应用程序运行时间
        }
    }

    public TimeSpan GetApplicationUptime()
    {
        return DateTime.UtcNow - _startTime;
    }

    public async Task<DatabaseConnectionPoolInfo> GetDatabaseConnectionPoolInfoAsync()
    {
        try
        {
            // 获取活动连接数（查询pg_stat_activity）
            var activeConnections = await GetActiveDatabaseConnectionsAsync();

            // 尝试获取Npgsql连接池信息
            var poolInfo = await GetNpgsqlConnectionPoolInfoAsync();

            return new DatabaseConnectionPoolInfo
            {
                ActiveConnections = activeConnections,
                IdleConnections = Math.Max(0, poolInfo.PoolSize - activeConnections),
                PoolSize = poolInfo.PoolSize,
                WaitingConnections = poolInfo.WaitingConnections,
                TimeoutCount = poolInfo.TimeoutCount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database connection pool info.");
            // 返回包含默认值的对象，而不是空对象
            return new DatabaseConnectionPoolInfo
            {
                ActiveConnections = 0,
                IdleConnections = 0,
                PoolSize = 0,
                WaitingConnections = 0,
                TimeoutCount = 0
            };
        }
    }

    private async Task<int> GetActiveDatabaseConnectionsAsync()
    {
        try
        {
            // 查询pg_stat_activity视图获取当前数据库的活动连接
            FormattableString sql = $@"
                SELECT COUNT(*) as active_connections
                FROM pg_stat_activity
                WHERE datname = current_database()
                  AND state != 'idle'
                  AND pid != pg_backend_pid()";

            var result = await _dbContext.Database
                .SqlQuery<int>(sql).FirstOrDefaultAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get active database connections, using fallback.");
            return 0;
        }
    }

    private async Task<(int PoolSize, int WaitingConnections, int TimeoutCount)> GetNpgsqlConnectionPoolInfoAsync()
    {
        // Npgsql连接池信息无法直接从运行时获取
        // 这里返回默认值或从配置读取
        // 默认Npgsql连接池大小是100
        const int defaultPoolSize = 100;

        // 等待连接数和超时次数目前无法获取，返回0
        return (defaultPoolSize, 0, 0);
    }

    public async Task<HttpRequestStatistics> GetHttpRequestStatisticsAsync()
    {
        try
        {
            // 从请求跟踪服务获取实时统计数据
            var statistics = _requestTrackingService.GetStatistics();

            return new HttpRequestStatistics
            {
                TotalRequests = statistics.TotalRequests,
                ActiveRequests = statistics.ActiveRequests,
                FailedRequests = statistics.FailedRequests,
                AverageResponseTime = statistics.AverageResponseTime,
                RequestRate = statistics.RequestRate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get HTTP request statistics.");
            return new HttpRequestStatistics();
        }
    }

    private double GetCpuUsageFallback()
    {
        try
        {
            // 使用进程CPU时间计算近似CPU使用率
            var startCpuTime = _currentProcess.TotalProcessorTime;
            var startTime = DateTime.UtcNow;

            // 等待一小段时间
            Task.Delay(500).Wait();

            var endCpuTime = _currentProcess.TotalProcessorTime;
            var endTime = DateTime.UtcNow;

            var cpuUsedMs = (endCpuTime - startCpuTime).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

            return Math.Round(cpuUsageTotal * 100, 2);
        }
        catch
        {
            return 0;
        }
    }

    private double GetMemoryUsageFallback()
    {
        try
        {
            _currentProcess.Refresh();
            var processMemoryMB = _currentProcess.WorkingSet64 / (1024 * 1024);
            var totalMemoryMB = GetTotalPhysicalMemoryMB();

            if (totalMemoryMB > 0)
            {
                return Math.Round((processMemoryMB / totalMemoryMB) * 100, 2);
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private double GetTotalPhysicalMemoryMB()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    var totalMemoryBytes = Convert.ToUInt64(obj["TotalVisibleMemorySize"]) * 1024;
                    return totalMemoryBytes / (1024 * 1024);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var memInfo = System.IO.File.ReadAllText("/proc/meminfo");
                var lines = memInfo.Split('\n');
                foreach (var line in lines)
                {
                    if (line.StartsWith("MemTotal:"))
                    {
                        var parts = line.Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && long.TryParse(parts[1], out var totalMemoryKB))
                        {
                            return totalMemoryKB / 1024.0;
                        }
                    }
                }
            }

            return 8192; // 默认8GB
        }
        catch
        {
            return 8192; // 默认8GB
        }
    }

    public void Dispose()
    {
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
    }
}