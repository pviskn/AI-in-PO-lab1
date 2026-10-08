namespace CarRental.Domain.Exceptions;

public sealed class RentalRequestNotOwnedException : DomainException
{
    public override int StatusCode => 400;

    public RentalRequestNotOwnedException(Guid requestId, Guid userId)
        : base($"Пользователь '{userId}' не является автором заявки '{requestId}' и не может её отменить.") { }
}
