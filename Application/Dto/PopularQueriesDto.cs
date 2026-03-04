using System.Collections.Generic;

namespace Application.ApplicationServiceContract;

public class PopularQueriesDto
{
    public int TopN { get; set; }
    public IEnumerable<QueryStat> Queries { get; set; } = null!;
}