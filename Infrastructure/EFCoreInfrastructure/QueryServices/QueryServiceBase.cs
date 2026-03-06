using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.QueryServices;
using DomainService.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCoreInfrastructure.QueryServices;

/// <summary>
/// 通用查询服务基类
/// 实现所有复杂查询操作，不包含数据修改功能
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
public abstract class QueryServiceBase<T> : IQueryService<T> where T : Entity<Guid>
{
    protected readonly DbSet<T> _dbSet;
    protected readonly DbContext _context;

    protected QueryServiceBase(DbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<T>();
    }

    // 基本查询
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    // 条件查询
    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.CountAsync(predicate);
    }

    // 包含导航属性的查询
    public virtual async Task<IEnumerable<T>> IncludesAsync(
        Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet.Where(predicate);

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.ToListAsync();
    }

    // 排序查询
    public virtual async Task<IEnumerable<T>> OrderByAsync(
        Expression<Func<T, bool>> predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        bool asNoTracking = false)
    {
        IQueryable<T> query = _dbSet.Where(predicate);

        query = orderBy(query);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.ToListAsync();
    }

    // 分页查询
    public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
        int pageIndex,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool asNoTracking = false)
    {
        IQueryable<T> query = _dbSet;

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync();

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    // 投影查询（DTO映射）
    public virtual async Task<IEnumerable<TResult>> SelectAsync<TResult>(
        Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool asNoTracking = false)
    {
        IQueryable<T> query = _dbSet;

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        if (orderBy != null)
        {
            query = orderBy(query);
        }

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.Select(selector).ToListAsync();
    }

    // 无跟踪查询（只读场景）
    public virtual async Task<IEnumerable<T>> FindAsNoTrackingAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet
            .Where(predicate)
            .AsNoTracking()
            .ToListAsync();
    }

    // 获取查询构建器（高级场景，慎用）
    public virtual IQueryable<T> GetQueryable()
    {
        return _dbSet.AsQueryable();
    }
}