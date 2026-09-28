using CarRental.Domain.Entities;
using CarRental.Domain.Enums;

namespace CarRental.Application.Abstractions.Repositories;

public interface IRentalRequestRepository
{
    Task<RentalRequest?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<RentalRequest> Items, int TotalCount)> GetPagedAsync(
        Guid? userId,
        RentalRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingRequestsAsync(
        Guid carId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludeRequestId = null,
        CancellationToken cancellationToken = default);

    void Add(RentalRequest request);

    void Update(RentalRequest request);
}
