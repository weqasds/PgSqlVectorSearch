using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using DomainService.Entities;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using DomainService.ServiceContract.CrossCutting;

namespace DomainService.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("用户名不能为空", nameof(username));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("密码不能为空", nameof(password));

        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null || user.IsDeleted)
            throw new UnauthorizedAccessException("用户名或密码错误");

        // 验证密码
        var passwordHash = ComputeHash(password);
        if (user.PasswordHash != passwordHash)
            throw new UnauthorizedAccessException("用户名或密码错误");

        return user;
    }

    public async Task<User> RegisterAsync(string username, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("用户名不能为空", nameof(username));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("邮箱不能为空", nameof(email));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("密码不能为空", nameof(password));

        // 检查用户名是否已存在
        var usernameExists = await _userRepository.ExistsByUsernameAsync(username);
        if (usernameExists)
            throw new InvalidOperationException($"用户名 '{username}' 已存在");

        // 检查邮箱是否已存在
        var emailExists = await _userRepository.ExistsByEmailAsync(email);
        if (emailExists)
            throw new InvalidOperationException($"邮箱 '{email}' 已存在");

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = username,
            Email = email,
            PasswordHash = ComputeHash(password),
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        await _userRepository.AddAsync(user);
        return user;
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        // TODO: 实现JWT令牌验证逻辑
        // 这里应验证令牌签名、过期时间等
        // 临时实现：假设令牌有效
        await Task.CompletedTask;
        return true;
    }

    public async Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(oldPassword))
            throw new ArgumentException("旧密码不能为空", nameof(oldPassword));

        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("新密码不能为空", nameof(newPassword));

        if (oldPassword == newPassword)
            throw new InvalidOperationException("新密码不能与旧密码相同");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.IsDeleted)
            throw new KeyNotFoundException($"未找到ID为 {userId} 的用户");

        // 验证旧密码
        var oldPasswordHash = ComputeHash(oldPassword);
        if (user.PasswordHash != oldPasswordHash)
            throw new UnauthorizedAccessException("旧密码错误");

        // 更新密码
        user.PasswordHash = ComputeHash(newPassword);
        await _userRepository.UpdateAsync(user);
    }

    private string ComputeHash(string input)
    {
        // 使用SHA256计算哈希值（实际应用中应使用更安全的算法如PBKDF2或BCrypt）
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }
    }
}