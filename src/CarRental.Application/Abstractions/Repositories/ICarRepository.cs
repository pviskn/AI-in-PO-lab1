using CarRental.Domain.Entities;
using CarRental.Domain.Enums;

namespace CarRental.Application.Abstractions.Repositories;

public interface ICarRepository
{
    Task<Car?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Car?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Car?> GetByVinAsync(string vin, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Car> Items, int TotalCount)> GetPagedAsync(
        CarStatus? status,
        CarCategory? category,
        decimal? minPricePerDay,
        decimal? maxPricePerDay,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByVinAsync(string vin, CancellationToken cancellationToken = default);

    void Add(Car car);

    void Update(Car car);
}
