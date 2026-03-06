using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DomainService.Entities;

namespace Application.QueryServices;

/// <summary>
/// 通用查询服务接口，负责所有复杂查询操作
/// 遵循CQRS原则，将查询职责与仓储的写职责分离
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
public interface IQueryService<T> where T : Entity<Guid>
{
    // 基本查询
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    // 条件查询
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);

    // 包含导航属性的查询
    Task<IEnumerable<T>> IncludesAsync(
        Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] includes);

    // 排序查询
    Task<IEnumerable<T>> OrderByAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        bool asNoTracking = false);

    // 分页查询
    Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool asNoTracking = false);

    // 投影查询（DTO映射）
    Task<IEnumerable<TResult>> SelectAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool asNoTracking = false);

    // 无跟踪查询（只读场景）
    Task<IEnumerable<T>> FindAsNoTrackingAsync(Expression<Func<T, bool>> predicate);

    // 获取查询构建器（高级场景，慎用）
    IQueryable<T> GetQueryable();
}