using CarRental.Application.Common;
using CarRental.Application.DTOs;
using CarRental.Domain.Enums;

namespace CarRental.Application.Abstractions.UseCases;

public interface ICarCatalogService
{
    Task<PagedResult<CarDto>> GetCarsAsync(CarFilterDto filter, CancellationToken cancellationToken = default);

    Task<CarDto?> GetCarByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CarDto> AddCarAsync(CreateCarDto dto, CancellationToken cancellationToken = default);

    Task ChangeCarStatusAsync(Guid carId, CarStatus newStatus, CancellationToken cancellationToken = default);
}
