using CarRental.Domain.Enums;

namespace CarRental.Application.DTOs;

public sealed record ChangeCarStatusDto(CarStatus NewStatus);
