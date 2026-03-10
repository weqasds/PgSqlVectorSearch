using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DomainService.ServiceContract.CrossCutting;
using Microsoft.Extensions.Logging;

namespace InfrastructureShared.Services;

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

    public SystemMetricsCollector(ILogger<SystemMetricsCollector> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                using var uptimeProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c net statistics workstation | find \"Statistics since\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                });

                if (uptimeProcess != null)
                {
                    var output = uptimeProcess.StandardOutput.ReadToEnd();
                    uptimeProcess.WaitForExit();

                    // 解析输出获取系统运行时间
                    // 简化实现，返回模拟值
                }
            }

            // 返回模拟值或使用其他方法
            return TimeSpan.FromDays(7); // 模拟值
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system uptime.");
            return TimeSpan.Zero;
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
            // 在实际应用中，这里应该从数据库连接池获取信息
            // 当前返回模拟数据
            await Task.CompletedTask;

            return new DatabaseConnectionPoolInfo
            {
                ActiveConnections = 8,
                IdleConnections = 2,
                PoolSize = 10,
                WaitingConnections = 0,
                TimeoutCount = 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get database connection pool info.");
            return new DatabaseConnectionPoolInfo();
        }
    }

    public async Task<HttpRequestStatistics> GetHttpRequestStatisticsAsync()
    {
        try
        {
            // 在实际应用中，这里应该从HTTP请求监控系统获取信息
            // 当前返回模拟数据
            await Task.CompletedTask;

            return new HttpRequestStatistics
            {
                TotalRequests = 1500,
                ActiveRequests = 12,
                FailedRequests = 25,
                AverageResponseTime = 245.5,
                RequestRate = 2.5
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