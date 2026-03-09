using System;
using System.Threading.Tasks;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using DomainService.ServiceContract.CrossCutting;
using Application.ApplicationServiceContract;
using Application.Dto;
using DomainService.Entities.UserManagement;
using System.Security.Cryptography;
using System.Text;

namespace Application.ApplicationService;

public class AuthAppService : IAuthAppService
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;

    public AuthAppService(IAuthService authService, IUserRepository userRepository)
    {
        _authService = authService;
        _userRepository = userRepository;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // Authenticate user via domain service
        var user = await _authService.AuthenticateAsync(request.Username, request.Password);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        // Generate token (in a real implementation, this would be done by the auth service)
        // For now, we'll assume the auth service returns a user with a token property
        // or we need to generate a JWT token.
        // Since the domain service doesn't return a token, we'll simulate one.
        var token = GenerateJwtToken(user);

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            User = new UserDto
            {
                Id = user.Id,
                Username = user.UserName,
                Email = user.Email,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.ModifiedAt
            }
        };
    }

    public async Task LogoutAsync(string token)
    {
        // Validate token first
        var isValid = await _authService.ValidateTokenAsync(token);
        if (!isValid)
        {
            throw new UnauthorizedAccessException("Invalid token.");
        }

        // In a real implementation, you might:
        // 1. Add token to a blacklist/revocation list
        // 2. Update user's last logout time
        // 3. Log the logout event

        // For now, we'll just log and mark as handled
        Console.WriteLine($"User logged out, token invalidated: {token.Substring(0, Math.Min(20, token.Length))}...");

        await Task.CompletedTask;
    }

    public async Task<UserDto> GetCurrentUserAsync()
    {
        // This method should retrieve the current user based on the current principal
        // In a real implementation, we would get userId from claims in HttpContext

        // For now, we'll assume a static current user ID for demonstration
        // This should be replaced with actual authentication context
        var userId = new Guid("00000000-0000-0000-0000-000000000001"); // Example user ID

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Current user not found.");
        }

        return new UserDto
        {
            Id = user.Id,
            Username = user.UserName,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.ModifiedAt
        };
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        // Register user via domain service
        var user = await _authService.RegisterAsync(request.Username, request.Email, request.Password);

        // Generate token for new user
        var token = GenerateJwtToken(user);

        return new RegisterResponse
        {
            Success = true,
            Message = "Registration successful.",
        };
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request)
    {
        // This method should identify the current user from context
        // In a real implementation, we would get userId from claims in HttpContext

        // For demonstration, using a static current user ID
        // This should be replaced with actual authentication context
        var userId = new Guid("00000000-0000-0000-0000-000000000001"); // Example user ID

        await _authService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);

        // Log the password change event
        Console.WriteLine($"User {userId} changed password successfully.");
    }

    private string GenerateJwtToken(User user)
    {
        // Placeholder for JWT token generation
        // In a real implementation, use JWT library with secret key
        return $"fake-jwt-token-for-{user.Id}";
    }
}