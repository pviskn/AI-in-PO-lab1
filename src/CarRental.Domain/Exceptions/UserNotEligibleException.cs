namespace CarRental.Domain.Exceptions;

public sealed class UserNotEligibleException : DomainException
{
    public override int StatusCode => 403;

    public UserNotEligibleException(Guid userId, string reason)
        : base($"Пользователь '{userId}' не может воспользоваться услугой аренды: {reason}") { }
}
