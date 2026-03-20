using System.Collections.Generic;

namespace Application.Dto;

public class PopularQueriesDto
{
    public int TopN { get; set; }
    public IEnumerable<QueryStat> Queries { get; set; } = null!;
}