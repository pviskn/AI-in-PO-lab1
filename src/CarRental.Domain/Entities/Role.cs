using CarRental.Domain.Enums;

namespace CarRental.Domain.Entities;

public class Role
{
    private Role() { }

    public Guid Id { get; private set; }

    public UserRole Name { get; private set; }

    private readonly List<User> _users = new();

    public IReadOnlyCollection<User> Users => _users.AsReadOnly();

    public Role(Guid id, UserRole name)
    {
        Id = id;
        Name = name;
    }
}
