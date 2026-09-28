using CarRental.Domain.Enums;

namespace CarRental.Application.DTOs;

public sealed record CarFilterDto(
    CarStatus? Status = null,
    CarCategory? Category = null,
    decimal? MinPricePerDay = null,
    decimal? MaxPricePerDay = null,
    int Page = 1,
    int PageSize = 20);
