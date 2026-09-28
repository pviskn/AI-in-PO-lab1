using CarRental.Application.Common;
using CarRental.Application.DTOs;

namespace CarRental.Application.Abstractions.UseCases;

public interface IUserManagementService
{
    Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default);

    Task AssignRoleAsync(Guid userId, AssignRoleDto dto, CancellationToken cancellationToken = default);

    Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
