namespace CarRental.Application.DTOs;

public sealed record RentalContractDto(
    Guid Id,
    Guid RentalRequestId,
    decimal BasePrice,
    decimal LateFee,
    decimal DamageFee,
    decimal TotalPrice,
    DateOnly? ActualReturnDate,
    DateTime CreatedAt);
