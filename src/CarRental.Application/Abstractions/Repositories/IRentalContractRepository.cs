using CarRental.Domain.Entities;

namespace CarRental.Application.Abstractions.Repositories;

public interface IRentalContractRepository
{
    Task<RentalContract?> GetByRentalRequestIdAsync(Guid rentalRequestId, CancellationToken cancellationToken = default);

    Task<RentalContract?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(RentalContract contract);

    void Update(RentalContract contract);
}
