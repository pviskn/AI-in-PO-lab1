using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;

namespace CarRental.Domain.Entities;

public class RentalRequest
{
    private RentalRequest() { }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public Guid CarId { get; private set; }

    public Car Car { get; private set; } = null!;

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public RentalRequestStatus Status { get; private set; }

    public string? ManagerComment { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public RentalContract? Contract { get; private set; }

    public RentalRequest(Guid id, Guid userId, Guid carId, DateOnly startDate, DateOnly endDate)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id заявки не может быть пустым", nameof(id));

        if (userId == Guid.Empty)
            throw new ArgumentException("Id пользователя не может быть пустым", nameof(userId));

        if (carId == Guid.Empty)
            throw new ArgumentException("Id автомобиля не может быть пустым", nameof(carId));

        if (startDate > endDate)
            throw new ArgumentException("дата начала аренды не может быть позже даты окончания", nameof(startDate));

        if (startDate < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("дата начала аренды не может быть в прошлом", nameof(startDate));

        Id = id;
        UserId = userId;
        CarId = carId;
        StartDate = startDate;
        EndDate = endDate;
        Status = RentalRequestStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void Approve()
    {
        if (Status != RentalRequestStatus.Pending)
            throw new InvalidOperationException("подтвердить можно только заявку pending");

        Status = RentalRequestStatus.Approved;
    }

    public void Reject(string comment)
    {
        if (Status != RentalRequestStatus.Pending)
            throw new InvalidOperationException("отклонить можно только заявку pending");

        ManagerComment = comment;
        Status = RentalRequestStatus.Rejected;
    }

    public void Complete()
    {
        if (Status != RentalRequestStatus.Approved)
            throw new InvalidOperationException("завершить можно только заявку approved");

        Status = RentalRequestStatus.Completed;
    }

    public void Cancel(Guid userId)
    {
        if (UserId != userId)
            throw new RentalRequestNotOwnedException(Id, userId);

        if (Status != RentalRequestStatus.Pending)
            throw new RentalRequestNotCancellableException(Id, Status);

        Status = RentalRequestStatus.Cancelled;
    }
}
