using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomainService.ServiceContract;
using Application.ApplicationServiceContract;
using Application.Dto;

namespace Application.ApplicationService;

public class UserAppService : IUserAppService
{
    private readonly IUserRepository _userRepository;
    private readonly IChatService _chatService;

    public UserAppService(IUserRepository userRepository, IChatService chatService)
    {
        _userRepository = userRepository;
        _chatService = chatService;
    }

    public async Task<UserDto> GetUserAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new ArgumentException($"User with id {userId} not found.");
        }

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateUserProfileAsync(Guid userId, UpdateUserProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new ArgumentException($"User with id {userId} not found.");
        }

        // Update user properties
        user.Email = request.Email!;
        user.UserName = request.Username!;
        // Add other fields as needed

        await _userRepository.UpdateAsync(user);

        return MapToDto(user);
    }

    public async Task<IEnumerable<ChatSessionDto>> GetUserSessionsAsync(Guid userId)
    {
        var sessions = await _chatService.GetUserSessionsAsync(userId);
        return sessions.Select(MapToSessionDto);
    }

    private UserDto MapToDto(DomainService.Entities.User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.UserName,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.ModifiedAt // Placeholder for last login time, replace with actual field if available
        };
    }

    private ChatSessionDto MapToSessionDto(DomainService.Entities.ChatSession session)
    {
        // MessageCount could be retrieved from repository; for simplicity we'll set to 0
        return new ChatSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            Title = session.ChatTitle,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.ModifiedAt, // Placeholder for last activity time, replace with actual field if available
            MessageCount = 0
        };
    }
}