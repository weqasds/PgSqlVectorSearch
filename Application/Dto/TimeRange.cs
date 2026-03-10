using System;

namespace Application.ApplicationServiceContract;

public class TimeRange
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
}