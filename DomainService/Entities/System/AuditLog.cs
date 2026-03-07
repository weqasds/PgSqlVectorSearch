using System;
using DomainService.Entities.Core;

namespace DomainService.Entities.System;

public class AuditLog : Entity<Guid>
{
    public string Operation { get; set; } = null!;
    public Guid? UserId { get; set; }
    public string Details { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}