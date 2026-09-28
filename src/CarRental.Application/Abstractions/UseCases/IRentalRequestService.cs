using CarRental.Application.Common;
using CarRental.Application.DTOs;
using CarRental.Domain.Enums;

namespace CarRental.Application.Abstractions.UseCases;

public interface IRentalRequestService
{
    Task<RentalRequestDto> CreateRequestAsync(CreateRentalRequestDto dto, Guid clientId, CancellationToken cancellationToken = default);

    Task<PagedResult<RentalRequestDto>> GetRequestsAsync(
        Guid currentUserId,
        bool isManager,
        RentalRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<RentalContractDto> ApproveRequestAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task RejectRequestAsync(Guid requestId, string reason, CancellationToken cancellationToken = default);

    Task<RentalContractDto> CompleteRentalAsync(Guid requestId, CompleteRentalDto dto, CancellationToken cancellationToken = default);
}
