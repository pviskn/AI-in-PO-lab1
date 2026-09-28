using CarRental.Application.Abstractions.Repositories;
using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public Task<User?> GetByUsernameWithRolesAsync(string username, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = _context.Users;

        int totalCount = await query.CountAsync(cancellationToken);

        List<User> items = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return _context.Users.AnyAsync(u => u.Username == username, cancellationToken);
    }

    public void Add(User user)
    {
        _context.Users.Add(user);
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }
}
