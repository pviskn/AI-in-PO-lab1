using CarRental.Application.Abstractions.Repositories;
using CarRental.Application.Abstractions.Services;
using CarRental.Application.Abstractions.UseCases;
using CarRental.Application.Common;
using CarRental.Application.DTOs;
using CarRental.Application.Mappings;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;

namespace CarRental.Application.Services;

public class CarCatalogService : ICarCatalogService
{
    private readonly ICarRepository _carRepo;

    private readonly IUnitOfWork _uow;

    private readonly IDateTimeProvider _dateTime;

    public CarCatalogService(ICarRepository carRepo, IUnitOfWork uow, IDateTimeProvider dateTime)
    {
        _carRepo = carRepo;
        _uow = uow;
        _dateTime = dateTime;
    }

    public async Task<PagedResult<CarDto>> GetCarsAsync(CarFilterDto filter, CancellationToken cancellationToken = default)
    {
        int page = Math.Max(1, filter.Page);
        int pageSize = Math.Clamp(filter.PageSize, 1, 100);

        (IReadOnlyList<Car> items, int totalCount) = await _carRepo.GetPagedAsync(
            filter.Status,
            filter.Category,
            filter.MinPricePerDay,
            filter.MaxPricePerDay,
            page,
            pageSize,
            cancellationToken);

        return new PagedResult<CarDto>(items.Select(car => car.ToDto()).ToList(), totalCount, page, pageSize);
    }

    public async Task<CarDto?> GetCarByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Car? car = await _carRepo.GetByIdAsync(id, cancellationToken);
        return car?.ToDto();
    }

    public async Task<CarDto> AddCarAsync(CreateCarDto dto, CancellationToken cancellationToken = default)
    {
        if (await _carRepo.ExistsByVinAsync(dto.Vin, cancellationToken))
            throw new DuplicateVinException(dto.Vin);

        var car = new Car(Guid.NewGuid(), dto.Vin, dto.Make, dto.Model, dto.Year, dto.Category, dto.PricePerDay, dto.Mileage);
        _carRepo.Add(car);
        await _uow.SaveChangesAsync(cancellationToken);

        return car.ToDto();
    }

    public async Task ChangeCarStatusAsync(Guid carId, CarStatus newStatus, CancellationToken cancellationToken = default)
    {
        Car? car = await _carRepo.GetByIdAsync(carId, cancellationToken);
        if (car == null)
            throw new KeyNotFoundException("автомобиль не найден");

        if (car.Status == newStatus)
            return;

        switch (newStatus)
        {
            case CarStatus.Available:
                if (car.Status == CarStatus.UnderMaintenance)
                    car.ReturnFromMaintenance();
                else if (car.Status == CarStatus.Rented)
                    car.Complete();
                else
                    throw new InvalidOperationException("нельзя так изменить статус автомобиля");
                break;
            case CarStatus.Rented:
                car.Rent();
                break;
            case CarStatus.UnderMaintenance:
                car.SendToMaintenance();
                break;
            default:
                throw new InvalidOperationException("нельзя так изменить статус автомобиля");
        }

        _carRepo.Update(car);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCarAsync(Guid carId, CancellationToken cancellationToken = default)
    {
        Car car = await _carRepo.GetByIdIncludingDeletedAsync(carId, cancellationToken)
            ?? throw new KeyNotFoundException("автомобиль не найден");

        car.SoftDelete(_dateTime.UtcNow);

        _carRepo.Update(car);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<CarDto> RestoreCarAsync(Guid carId, CancellationToken cancellationToken = default)
    {
        Car car = await _carRepo.GetByIdIncludingDeletedAsync(carId, cancellationToken)
            ?? throw new KeyNotFoundException("автомобиль не найден");

        car.Restore();

        _carRepo.Update(car);
        await _uow.SaveChangesAsync(cancellationToken);

        return car.ToDto();
    }
}
