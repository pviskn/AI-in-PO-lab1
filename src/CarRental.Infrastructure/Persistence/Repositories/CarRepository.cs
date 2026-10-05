using CarRental.Application.Abstractions.Repositories;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repositories;

public class CarRepository : ICarRepository
{
    private readonly ApplicationDbContext _context;

    public CarRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<Car?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Cars.FirstOrDefaultAsync(car => car.Id == id && car.DeletedAt == null, cancellationToken);
    }

    public Task<Car?> GetByIdIncludingDeletedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Cars.FirstOrDefaultAsync(car => car.Id == id, cancellationToken);
    }

    public Task<Car?> GetByVinAsync(string vin, CancellationToken cancellationToken = default)
    {
        return _context.Cars.FirstOrDefaultAsync(car => car.Vin.Value == vin && car.DeletedAt == null, cancellationToken);
    }

    public async Task<(IReadOnlyList<Car> Items, int TotalCount)> GetPagedAsync(
        CarStatus? status,
        CarCategory? category,
        decimal? minPricePerDay,
        decimal? maxPricePerDay,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Car> query = _context.Cars.Where(c => c.DeletedAt == null);

        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        if (category.HasValue)
            query = query.Where(c => c.Category == category.Value);

        if (minPricePerDay.HasValue)
            query = query.Where(c => c.DailyRate >= minPricePerDay.Value);

        if (maxPricePerDay.HasValue)
            query = query.Where(c => c.DailyRate <= maxPricePerDay.Value);

        int totalCount = await query.CountAsync(cancellationToken);

        List<Car> items = await query
            .OrderBy(c => c.Make)
            .ThenBy(c => c.Model)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> ExistsByVinAsync(string vin, CancellationToken cancellationToken = default)
    {
        string normalizedVin = vin.Trim().ToUpperInvariant();
        return _context.Cars.AnyAsync(car => car.Vin.Value == normalizedVin, cancellationToken);
    }

    public void Add(Car car)
    {
        _context.Cars.Add(car);
    }

    public void Update(Car car)
    {
        _context.Cars.Update(car);
    }
}