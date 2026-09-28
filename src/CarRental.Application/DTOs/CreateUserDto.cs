using System.ComponentModel.DataAnnotations;

namespace CarRental.Application.DTOs;

public sealed record CreateUserDto(
    [Required, StringLength(50, MinimumLength = 3)]
    string Username,

    [Required, StringLength(100, MinimumLength = 6)]
    string Password,

    [Required, StringLength(100)]
    string FirstName,

    [Required, StringLength(100)]
    string LastName,

    [Required, EmailAddress]
    string Email,

    [Required]
    DateOnly DateOfBirth,

    [Required]
    DateOnly LicenseDate);
