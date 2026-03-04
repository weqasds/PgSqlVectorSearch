using System;
using System.Threading.Tasks;
using DomainService.ServiceContract;
using Application.ApplicationServiceContract;
using Application.Dto;

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
        // Invalidate token (implementation depends on token storage)
        // For now, just assume token invalidation is handled elsewhere
        await Task.CompletedTask;
    }

    public async Task<UserDto> GetCurrentUserAsync()
    {
        // This method should retrieve the current user based on the current principal
        // For simplicity, we'll throw NotSupportedException; real implementation would use IHttpContextAccessor
        throw new NotSupportedException("GetCurrentUserAsync requires HTTP context integration.");
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
        // For simplicity, we'll assume the request contains UserId
        // In real implementation, get userId from claims
        var userId = request.UserId;

        await _authService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);
    }

    private string GenerateJwtToken(DomainService.Entities.User user)
    {
        // Placeholder for JWT token generation
        // In a real implementation, use JWT library with secret key
        return $"fake-jwt-token-for-{user.Id}";
    }
}