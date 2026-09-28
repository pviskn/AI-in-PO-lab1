using System.ComponentModel.DataAnnotations;

namespace CarRental.Application.DTOs;

public sealed record CompleteRentalDto(
    [Required]
    DateOnly ActualReturnDate,

    [Range(0, double.MaxValue, ErrorMessage = "Штраф за повреждения не может быть отрицательным.")]
    decimal DamageFee = 0);
