using System;
using System.Collections.Generic;

namespace Application.Dto;

public class UsageStatisticsDto
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalQueries { get; set; }
    public int TotalDocumentsUploaded { get; set; }
    public IEnumerable<DailyUsage> DailyUsages { get; set; } = null!;
}

public class DailyUsage
{
    public DateOnly Date { get; set; }
    public int QueryCount { get; set; }
    public int UserCount { get; set; }
    public int DocumentCount { get; set; }
}