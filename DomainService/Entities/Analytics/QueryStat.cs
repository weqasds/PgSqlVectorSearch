using System;

namespace DomainService.Entities.Analytics;

public record QueryStat(string Query, int Count, DateTimeOffset LastQueried);