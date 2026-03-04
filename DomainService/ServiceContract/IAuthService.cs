using System;
using System.Threading.Tasks;
using DomainService.Entities;

namespace DomainService.ServiceContract;

public interface IAuthService
{
    Task<User> AuthenticateAsync(string username, string password);
    Task<User> RegisterAsync(string username, string email, string password);
    Task<bool> ValidateTokenAsync(string token);
    Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
}