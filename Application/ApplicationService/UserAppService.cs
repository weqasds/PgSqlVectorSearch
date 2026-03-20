using DomainService.ServiceContract.Services;
using DomainService.ServiceContract.Repositories;
using Application.ApplicationServiceContract;
using Application.Dto;
using DomainService.Entities.UserManagement;
using DomainService.Entities.Chat;
using Application.ApplicationServiceContract.CrossCutting;

namespace Application.ApplicationService;

public class UserAppService : IUserAppService
{
    private readonly IUserRepository _userRepository;
    private readonly IChatService _chatService;
    private readonly IUnitOfWork _unitOfWork;

    public UserAppService(IUserRepository userRepository, IChatService chatService, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _chatService = chatService;
        _unitOfWork = unitOfWork;
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

        // Update user properties based on the request
        user.UpdateInfo(request.Email!, request.Username!);
        // Add other fields as needed

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task<IEnumerable<ChatSessionDto>> GetUserSessionsAsync(Guid userId)
    {
        var sessions = await _chatService.GetUserSessionsAsync(userId);

        var sessionDtos = new List<ChatSessionDto>();
        foreach (var session in sessions)
        {
            var dto = await MapToSessionDtoAsync(session);
            sessionDtos.Add(dto);
        }

        return sessionDtos;
    }

    private UserDto MapToDto(User user)
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

    private async Task<ChatSessionDto> MapToSessionDtoAsync(ChatSession session)
    {
        var messages = await _chatService.GetSessionMessagesAsync(session.Id);
        var count = messages?.Count() ?? 0;

        return new ChatSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            Title = session.ChatTitle,
            CreatedAt = session.CreatedAt,
            LastActivityAt = session.ModifiedAt, // Placeholder for last activity time, replace with actual field if available
            MessageCount = count
        };
    }
}