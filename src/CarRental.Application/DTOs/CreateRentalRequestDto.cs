using System.ComponentModel.DataAnnotations;

namespace CarRental.Application.DTOs;

public sealed record CreateRentalRequestDto(
    [Required]
    Guid CarId,

    [Required]
    DateOnly StartDate,

    [Required]
    DateOnly EndDate);
