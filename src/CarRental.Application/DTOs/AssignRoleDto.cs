using CarRental.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CarRental.Application.DTOs;

public sealed record AssignRoleDto(
    [Required]
    UserRole Role);
