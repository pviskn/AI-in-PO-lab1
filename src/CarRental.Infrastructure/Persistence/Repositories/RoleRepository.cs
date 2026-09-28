using CarRental.Application.Abstractions.Repositories;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _context;

    public RoleRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Role?> GetByRoleTypeAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return _context.Roles
            .FirstOrDefaultAsync(r => r.Name == role, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Roles.OrderBy(role => role.Name).ToListAsync(cancellationToken);
    }
}
