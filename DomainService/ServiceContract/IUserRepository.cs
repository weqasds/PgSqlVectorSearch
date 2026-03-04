using System;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsByUsernameAsync(string username);
    Task<bool> ExistsByEmailAsync(string email);
}