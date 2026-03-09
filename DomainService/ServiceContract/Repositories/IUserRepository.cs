using System;
using System.Threading.Tasks;
using DomainService.Entities.Core;
using DomainService.Entities.UserManagement;


namespace DomainService.ServiceContract.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsByUsernameAsync(string username);
    Task<bool> ExistsByEmailAsync(string email);

    // Analytics methods
    Task<int> GetTotalCountAsync();
    Task<int> GetActiveUserCountAsync();
    Task<int> GetActiveUserCountAsync(DateOnly start, DateOnly end);
    Task<int> GetActiveUserCountByDateAsync(DateOnly date);

    // User navigation property loading methods
    /// <summary>
    /// 获取用户及其有权限访问的知识库列表
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    Task<User> GetUserWithAccessibleKnowledgeBasesAsync(Guid userId);
}