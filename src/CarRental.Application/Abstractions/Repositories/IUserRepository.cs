using CarRental.Domain.Entities;

namespace CarRental.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByUsernameWithRolesAsync(string username, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken = default);

    void Add(User user);

    void Update(User user);
}
