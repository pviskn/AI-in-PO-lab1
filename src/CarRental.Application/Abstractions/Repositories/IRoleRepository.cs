using CarRental.Domain.Entities;
using CarRental.Domain.Enums;

namespace CarRental.Application.Abstractions.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByRoleTypeAsync(UserRole role, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);
}
