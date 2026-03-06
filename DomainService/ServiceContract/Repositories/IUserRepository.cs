using System;
using System.Threading.Tasks;
using DomainService.Entities;

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
}