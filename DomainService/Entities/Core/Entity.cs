using System;
using System.Collections.Generic;
using System.Text;

namespace DomainService.Entities.Core;

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
    /// <summary>
    /// 记录最后修改时间
    /// </summary>
    public DateTime? ModifiedAt { get; set; }
    public Entity()
    {
        // 如果Tkey是Guid类型，则默认生成一个新的Guid，否则使用默认值
        Id = Guid.NewGuid() is Tkey key ? key : default!;
        IsDeleted = false;
    }

}
