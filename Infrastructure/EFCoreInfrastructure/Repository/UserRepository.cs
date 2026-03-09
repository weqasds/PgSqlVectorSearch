using System;
using System.Linq;
using System.Threading.Tasks;
using DomainService.Entities.Core;using DomainService.Entities.KnowledgeManagement;using DomainService.Entities.KnowledgeGraph;using DomainService.Entities.Enums;using DomainService.Entities.VectorSearch;using DomainService.Entities.Chat;using DomainService.Entities.UserManagement;using DomainService.Entities.System;using DomainService.Entities.RAG;using DomainService.Entities.Analytics;
using DomainService.ServiceContract.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EFCoreInfrastructure.Repository;

public class UserRepository : RepositoryBase<User>, IUserRepository
{
    public UserRepository(RagDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.UserName == username);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        return await _dbSet.AnyAsync(u => u.UserName == username);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _dbSet.AnyAsync(u => u.Email == email);
    }

    public async Task<int> GetTotalCountAsync()
    {
        return await _dbSet.CountAsync();
    }

    public async Task<int> GetActiveUserCountAsync()
    {
        // Define "active" as users who have logged in within the last 30 days
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        return await _dbSet.CountAsync(u => u.ModifiedAt >= cutoffDate);
    }

    public async Task<int> GetActiveUserCountAsync(DateOnly start, DateOnly end)
    {
        var startDateTime = start.ToDateTime(TimeOnly.MinValue);
        var endDateTime = end.ToDateTime(TimeOnly.MaxValue);
        return await _dbSet.CountAsync(u => u.ModifiedAt >= startDateTime && u.ModifiedAt <= endDateTime);
    }

    public async Task<int> GetActiveUserCountByDateAsync(DateOnly date)
    {
        var startDateTime = date.ToDateTime(TimeOnly.MinValue);
        var endDateTime = date.ToDateTime(TimeOnly.MaxValue);
        return await _dbSet.CountAsync(u => u.ModifiedAt >= startDateTime && u.ModifiedAt <= endDateTime);
    }

    public async Task<User> GetUserWithAccessibleKnowledgeBasesAsync(Guid userId)
    {
        return await _dbSet
            .Include(u => u.AccessibleKnowledgeBases)
                .ThenInclude(akb => akb.KnowledgeBase)
            .FirstOrDefaultAsync(u => u.Id == userId)
            .ContinueWith(task =>
            {
                var user = task.Result;
                if (user == null)
                    throw new KeyNotFoundException($"未找到ID为{userId}的用户");
                return user;
            });
    }

}