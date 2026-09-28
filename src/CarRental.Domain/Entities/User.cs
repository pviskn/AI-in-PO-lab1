namespace CarRental.Domain.Entities;

public class User
{
    private User() { }

    public Guid Id { get; private set; }

    public string Username { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string FirstName { get; private set; } = string.Empty;

    public string LastName { get; private set; } = string.Empty;

    public DateOnly DateOfBirth { get; private set; }

    public DateOnly LicenseDate { get; private set; }

    public string Email { get; private set; } = string.Empty;

    private readonly List<Role> _roles = new();

    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();

    private readonly List<RentalRequest> _rentalRequests = new();

    public IReadOnlyCollection<RentalRequest> RentalRequests => _rentalRequests.AsReadOnly();

    public User(Guid id, string username, string passwordHash, string firstName, string lastName, DateOnly dateOfBirth, DateOnly licenseDate, string email)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id пользователя не может быть пустым", nameof(id));

        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username не может быть пустым", nameof(username));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("пароль не может быть пустым", nameof(passwordHash));

        if (licenseDate < dateOfBirth)
            throw new ArgumentException("дата получения прав не может быть раньше рождения", nameof(licenseDate));

        Id = id;
        Username = username.Trim();
        PasswordHash = passwordHash;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        DateOfBirth = dateOfBirth;
        LicenseDate = licenseDate;
        Email = email.Trim();
    }

    public int AgeInYears => CountYears(DateOfBirth, DateOnly.FromDateTime(DateTime.Today));

    public int DrivingExperienceYears => CountYears(LicenseDate, DateOnly.FromDateTime(DateTime.Today));

    public void AddRole(Role role)
    {
        if (_roles.Any(r => r.Name == role.Name))
            throw new InvalidOperationException("эта роль уже назначена");

        _roles.Add(role);
    }

    private static int CountYears(DateOnly startDate, DateOnly currentDate)
    {
        int years = currentDate.Year - startDate.Year;

        if (currentDate < startDate.AddYears(years))
            years--;

        return years;
    }
}
