using System.Collections.Generic;

namespace Application.ApplicationServiceContract;

public class SystemMetricsDto
{
    public IEnumerable<RequestMetric> RequestMetrics { get; set; } = null!;
    public IEnumerable<ProcessingMetric> ProcessingMetrics { get; set; } = null!;
    public DatabaseMetrics DatabaseMetrics { get; set; } = new DatabaseMetrics();
}

public class RequestMetric
{
    public string Endpoint { get; set; } = null!;
    public int RequestCount { get; set; }
    public double AverageResponseTime { get; set; }
    public int ErrorCount { get; set; }
}

public class ProcessingMetric
{
    public string Operation { get; set; } = null!;
    public int Count { get; set; }
    public double AverageDuration { get; set; }
}

public class DatabaseMetrics
{
    public int ConnectionCount { get; set; }
    public long QueryCount { get; set; }
    public double CacheHitRatio { get; set; }
}