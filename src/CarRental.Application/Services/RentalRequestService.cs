using CarRental.Application.Abstractions.Repositories;
using CarRental.Application.Abstractions.UseCases;
using CarRental.Application.Common;
using CarRental.Application.DTOs;
using CarRental.Application.Mappings;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;

namespace CarRental.Application.Services;

public class RentalRequestService : IRentalRequestService
{
    private readonly IRentalRequestRepository _requestRepo;
    private readonly IRentalContractRepository _contractRepo;
    private readonly ICarRepository _carRepo;
    private readonly IUserRepository _userRepo;
    private readonly IUnitOfWork _uow;
    private readonly IRentalEligibilityService _eligibility;
    private readonly IRentalPricingService _pricing;

    public RentalRequestService(
        IRentalRequestRepository requestRepo,
        IRentalContractRepository contractRepo,
        ICarRepository carRepo,
        IUserRepository userRepo,
        IUnitOfWork uow,
        IRentalEligibilityService eligibility,
        IRentalPricingService pricing)
    {
        _requestRepo = requestRepo;
        _contractRepo = contractRepo;
        _carRepo = carRepo;
        _userRepo = userRepo;
        _uow = uow;
        _eligibility = eligibility;
        _pricing = pricing;
    }

    public async Task<RentalRequestDto> CreateRequestAsync(
        CreateRentalRequestDto dto, Guid clientId, CancellationToken cancellationToken = default)
    {
        User? user = await _userRepo.GetByIdWithRolesAsync(clientId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("Пользователь не найден.");

        Car? car = await _carRepo.GetByIdAsync(dto.CarId, cancellationToken);
        if (car == null)
            throw new KeyNotFoundException("Автомобиль не найден.");

        if (car.Status != CarStatus.Available)
            throw new CarNotAvailableException(car.Id);

        _eligibility.EnsureEligible(user, car.Category);

        bool hasOverlap = await _requestRepo.HasOverlappingRequestsAsync(
            car.Id, dto.StartDate, dto.EndDate, null, cancellationToken);
        if (hasOverlap)
            throw new CarNotAvailableException(car.Id);

        var request = new RentalRequest(
            Guid.NewGuid(),
            user.Id,
            car.Id,
            dto.StartDate,
            dto.EndDate);

        _requestRepo.Add(request);
        await _uow.SaveChangesAsync(cancellationToken);

        return request.ToDto();
    }

    public async Task<PagedResult<RentalRequestDto>> GetRequestsAsync(
        Guid currentUserId,
        bool isManager,
        RentalRequestStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        int realPage = Math.Max(1, page);
        int realPageSize = Math.Clamp(pageSize, 1, 100);
        Guid? userId = isManager ? null : currentUserId;

        (IReadOnlyList<RentalRequest> items, int totalCount) = await _requestRepo.GetPagedAsync(
            userId,
            status,
            realPage,
            realPageSize,
            cancellationToken);

        return new PagedResult<RentalRequestDto>(items.Select(request => request.ToDto()).ToList(), totalCount, realPage, realPageSize);
    }

    public async Task<RentalContractDto> ApproveRequestAsync(
        Guid requestId, CancellationToken cancellationToken = default)
    {
        RentalRequest? request = await _requestRepo.GetByIdWithDetailsAsync(requestId, cancellationToken);
        if (request == null)
            throw new KeyNotFoundException("Заявка не найдена");

        if (request.Status != RentalRequestStatus.Pending)
            throw new InvalidOperationException("Заявка не находится в статусе ожидания");

        decimal basePrice = _pricing.CalculateBasePrice(request.Car, request.StartDate, request.EndDate);

        var contract = new RentalContract(
            Guid.NewGuid(),
            request.Id,
            basePrice);

        request.Approve();
        request.Car.Rent();

        _contractRepo.Add(contract);
        _requestRepo.Update(request);
        _carRepo.Update(request.Car);
        await _uow.SaveChangesAsync(cancellationToken);

        return contract.ToDto();
    }

    public async Task RejectRequestAsync(
        Guid requestId, string reason, CancellationToken cancellationToken = default)
    {
        RentalRequest? request = await _requestRepo.GetByIdWithDetailsAsync(requestId, cancellationToken);
        if (request == null)
            throw new KeyNotFoundException("Заявка не найдена.");

        if (request.Status != RentalRequestStatus.Pending)
            throw new InvalidOperationException("Заявка не находится в статусе ожидания.");

        request.Reject(reason);
        _requestRepo.Update(request);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<RentalContractDto> CompleteRentalAsync(
        Guid requestId, CompleteRentalDto dto, CancellationToken cancellationToken = default)
    {
        RentalRequest? request = await _requestRepo.GetByIdWithDetailsAsync(requestId, cancellationToken);
        if (request == null)
            throw new KeyNotFoundException("Заявка не найдена.");

        if (request.Status != RentalRequestStatus.Approved)
            throw new InvalidOperationException("Заявка не одобрена.");

        RentalContract? contract = await _contractRepo.GetByRentalRequestIdAsync(requestId, cancellationToken);
        if (contract == null)
            throw new KeyNotFoundException("Договор не найден.");

        decimal lateFee = _pricing.CalculateLateFee(
            request.Car, request.EndDate, dto.ActualReturnDate);

        contract.Complete(dto.ActualReturnDate, lateFee, dto.DamageFee);
        request.Car.Complete();

        _contractRepo.Update(contract);
        _carRepo.Update(request.Car);
        await _uow.SaveChangesAsync(cancellationToken);

        return contract.ToDto();
    }

    public async Task<RentalRequestDto> CancelRequestAsync(
        Guid requestId, Guid clientId, CancellationToken cancellationToken = default)
    {
        RentalRequest? request = await _requestRepo.GetByIdWithDetailsAsync(requestId, cancellationToken);
        if (request == null)
            throw new KeyNotFoundException("Заявка не найдена.");

        if (request.UserId != clientId)
            throw new InvalidOperationException("Нельзя отменить заявку другого клиента.");

        if (request.Status != RentalRequestStatus.Pending)
            throw new InvalidOperationException("Заявка не находится в статусе ожидания.");

        request.Cancel(clientId);
        _requestRepo.Update(request);
        await _uow.SaveChangesAsync(cancellationToken);

        return request.ToDto();
    }
}
