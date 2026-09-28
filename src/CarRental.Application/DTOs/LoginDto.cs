using System.ComponentModel.DataAnnotations;

namespace CarRental.Application.DTOs;

public sealed record LoginDto(
    [Required]
    string Username,

    [Required]
    string Password);
