namespace CarRental.Domain.Exceptions;

public sealed class DuplicateVinException : DomainException
{
    public override int StatusCode => 409;

    public DuplicateVinException(string vin)
        : base($"Автомобиль с VIN '{vin}' уже зарегистрирован в системе.") { }
}
