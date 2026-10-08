using CarRental.Domain.Enums;

namespace CarRental.Domain.Exceptions;

public sealed class RentalRequestNotCancellableException : DomainException
{
    public override int StatusCode => 400;

    public RentalRequestNotCancellableException(Guid requestId, RentalRequestStatus status)
        : base($"Заявку '{requestId}' в статусе '{status}' нельзя отменить. Отменить можно только заявку в статусе '{RentalRequestStatus.Pending}'.") { }
}
