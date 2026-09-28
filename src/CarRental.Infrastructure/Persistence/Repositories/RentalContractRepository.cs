using CarRental.Application.Abstractions.Repositories;
using CarRental.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Infrastructure.Persistence.Repositories;

public class RentalContractRepository : IRentalContractRepository
{
    private readonly ApplicationDbContext _context;

    public RentalContractRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<RentalContract?> GetByRentalRequestIdAsync(Guid rentalRequestId, CancellationToken cancellationToken = default)
    {
        return _context.RentalContracts
            .FirstOrDefaultAsync(c => c.RentalRequestId == rentalRequestId, cancellationToken);
    }

    public Task<RentalContract?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.RentalContracts
            .Include(c => c.RentalRequest)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public void Add(RentalContract contract)
    {
        _context.RentalContracts.Add(contract);
    }

    public void Update(RentalContract contract)
    {
        _context.RentalContracts.Update(contract);
    }
}