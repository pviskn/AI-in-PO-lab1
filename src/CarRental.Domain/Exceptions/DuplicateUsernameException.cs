namespace CarRental.Domain.Exceptions;

public sealed class DuplicateUsernameException : DomainException
{
    public override int StatusCode => 409;

    public DuplicateUsernameException(string username)
        : base($"Пользователь с именем '{username}' уже существует в системе.") { }
}
