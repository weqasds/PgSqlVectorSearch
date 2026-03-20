using System;
using System.Threading.Tasks;
using Application.Dto;
using ApplicationService.Dto.Request;

namespace Application.ApplicationServiceContract;

public interface IAuthAppService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task LogoutAsync(string token);
    Task<UserDto> GetCurrentUserAsync();
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task ChangePasswordAsync(ChangePasswordRequest request);
}