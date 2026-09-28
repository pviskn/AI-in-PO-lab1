namespace CarRental.Application.Abstractions.Services;

public interface ICurrentUserService
{
    Guid? UserId { get; }

    string? Username { get; }

    IReadOnlyList<string> Roles { get; }

    bool IsAuthenticated { get; }
}
