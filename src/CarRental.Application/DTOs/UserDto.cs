namespace CarRental.Application.DTOs;

public sealed record UserDto(
    Guid Id,
    string Username,
    string FirstName,
    string LastName,
    string Email,
    DateOnly DateOfBirth,
    DateOnly LicenseDate,
    IReadOnlyList<string> Roles);
