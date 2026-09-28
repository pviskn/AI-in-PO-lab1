using System.ComponentModel.DataAnnotations;

namespace CarRental.Application.DTOs;

public sealed record RejectRentalRequestDto(
    [Required, StringLength(500)]
    string Reason);
