using CarRental.Domain.Enums;

namespace CarRental.Application.DTOs;

public sealed record CarDto(
    Guid Id,
    string Vin,
    string Make,
    string Model,
    int Year,
    CarCategory Category,
    CarStatus Status,
    decimal PricePerDay,
    int Mileage);
