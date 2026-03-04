using System;
using System.Collections.Generic;
using System.Text;

namespace DomainService.Entities;

public class Entity<Tkey>
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public Tkey Id { get; set; }
    /// <summary>
    /// 记录创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
    /// <summary>
    /// 软删除标志
    /// </summary>
    public bool IsDeleted { get; set; }
}
