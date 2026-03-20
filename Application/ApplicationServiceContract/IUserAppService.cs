using Application.Dto;

namespace Application.ApplicationServiceContract;

public interface IUserAppService
{
    Task<UserDto> GetUserAsync(Guid userId);
    Task<UserDto> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request);
    Task<IEnumerable<ChatSessionDto>> GetUserSessionsAsync(Guid userId);
}