namespace CarRental.Domain.Exceptions;

public sealed class CarAlreadyDeletedException : DomainException
{
    public override int StatusCode => 409;

    public CarAlreadyDeletedException(Guid carId)
        : base($"Автомобиль с идентификатором '{carId}' уже удалён.") { }
}
