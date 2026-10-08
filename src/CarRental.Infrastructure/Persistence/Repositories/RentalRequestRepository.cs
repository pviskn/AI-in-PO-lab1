using CarRental.Application.Abstractions.Repositories;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repositories;

public class RentalRequestRepository : IRentalRequestRepository
{
    private readonly ApplicationDbContext _context;

    public RentalRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<RentalRequest?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.RentalRequests
            .Include(r => r.User)
            .Include(r => r.Car)
            .Include(r => r.Contract)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<RentalRequest> Items, int TotalCount)> GetPagedAsync(
        Guid? userId,
        RentalRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<RentalRequest> query = _context.RentalRequests
            .Include(r => r.User)
            .Include(r => r.Car);

        if (userId.HasValue)
            query = query.Where(r => r.UserId == userId.Value);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        int totalCount = await query.CountAsync(cancellationToken);

        List<RentalRequest> items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<bool> HasOverlappingRequestsAsync(
        Guid carId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludeRequestId = null,
        CancellationToken cancellationToken = default)
    {
        return _context.RentalRequests
        .AnyAsync(
            r =>
            r.CarId == carId &&
            r.Status != RentalRequestStatus.Rejected &&
            r.Status != RentalRequestStatus.Cancelled &&
            r.StartDate < endDate &&
            r.EndDate > startDate &&
            (excludeRequestId == null || r.Id != excludeRequestId),
            cancellationToken);
    }

    public void Add(RentalRequest request)
    {
        _context.RentalRequests.Add(request);
    }

    public void Update(RentalRequest request)
    {
        _context.RentalRequests.Update(request);
    }
}
