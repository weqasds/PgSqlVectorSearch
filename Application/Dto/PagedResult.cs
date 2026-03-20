using System.Collections.Generic;

namespace Application.Dto;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = null!;
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}