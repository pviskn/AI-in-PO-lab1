using CarRental.Domain.Enums;

namespace CarRental.Application.DTOs;

public sealed record RentalRequestDto(
    Guid Id,
    Guid ClientId,
    string ClientUsername,
    Guid CarId,
    string CarVin,
    string CarMake,
    string CarModel,
    DateOnly StartDate,
    DateOnly EndDate,
    RentalRequestStatus Status,
    string? ManagerComment,
    DateTime CreatedAt);
