using System;

namespace DomainService.Entities;

public record QueryStat(string Query, int Count, DateTime LastQueried);