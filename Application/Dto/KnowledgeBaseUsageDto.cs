using System;
using System.Collections.Generic;

namespace Application.Dto;

public class KnowledgeBaseUsageDto
{
    public Guid KnowledgeBaseId { get; set; }
    public string KnowledgeBaseName { get; set; } = null!;
    public int DocumentCount { get; set; }
    public int ChunkCount { get; set; }
    public int QueryCount { get; set; }
    public DateTimeOffset? LastQueryTime { get; set; }
    public IEnumerable<QueryStat> TopQueries { get; set; } = null!;
}

public class QueryStat
{
    public string Query { get; set; } = null!;
    public int Count { get; set; }
    public DateTimeOffset LastQueried { get; set; }
}