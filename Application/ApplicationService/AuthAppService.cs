using System;
using System.Threading.Tasks;
using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using DomainService.ServiceContract.CrossCutting;
using Application.ApplicationServiceContract.CrossCutting;
using Application.ApplicationServiceContract;
using Application.Dto;
using DomainService.Entities.UserManagement;
using System.Security.Cryptography;
using System.Text;
using ApplicationService.Dto.Request;

namespace Application.ApplicationService;

public class AuthAppService : IAuthAppService
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthAppService(
        IAuthService authService,
        IUserRepository userRepository,
        ITokenService tokenService,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _authService = authService;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // Authenticate user via domain service
        var user = await _authService.AuthenticateAsync(request.Username, request.Password);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        // Generate JWT token using TokenService
        var token = _tokenService.GenerateAccessToken(user);

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
        // Get current user ID from authentication context
        var userId = _currentUserService.GetCurrentUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await _userRepository.GetByIdAsync(userId.Value);
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
        await _unitOfWork.SaveChangesAsync();
        // Generate JWT token for new user
        var token = _tokenService.GenerateAccessToken(user);

        return new RegisterResponse
        {
            Success = true,
            Message = "Registration successful.",
            Token = token,
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

    public async Task ChangePasswordAsync(ChangePasswordRequest request)
    {
        // Get current user ID from authentication context
        var userId = _currentUserService.GetCurrentUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        await _authService.ChangePasswordAsync(userId.Value, request.OldPassword, request.NewPassword);
        await _unitOfWork.SaveChangesAsync();
        // Log the password change event
        Console.WriteLine($"User {userId} changed password successfully.");
    }

}