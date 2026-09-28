using CarRental.Application.DTOs;
using CarRental.Domain.Entities;

namespace CarRental.Application.Mappings;

public static class EntityMappingExtensions
{
    public static CarDto ToDto(this Car car)
    {
        return new CarDto(
            car.Id,
            car.Vin.Value,
            car.Make,
            car.Model,
            car.Year,
            car.Category,
            car.Status,
            car.DailyRate,
            car.Mileage);
    }

    public static UserDto ToDto(this User user)
    {
        return new UserDto(
            user.Id,
            user.Username,
            user.FirstName,
            user.LastName,
            user.Email,
            user.DateOfBirth,
            user.LicenseDate,
            user.Roles.Select(role => role.Name.ToString()).ToList());
    }

    public static RentalRequestDto ToDto(this RentalRequest request)
    {
        return new RentalRequestDto(
            request.Id,
            request.UserId,
            request.User.Username,
            request.CarId,
            request.Car.Vin.Value,
            request.Car.Make,
            request.Car.Model,
            request.StartDate,
            request.EndDate,
            request.Status,
            request.ManagerComment,
            request.CreatedAt);
    }

    public static RentalContractDto ToDto(this RentalContract contract)
    {
        return new RentalContractDto(
            contract.Id,
            contract.RentalRequestId,
            contract.BasePrice,
            contract.LateFee,
            contract.DamageFee,
            contract.TotalPrice,
            contract.ActualReturnDate,
            contract.CreatedAt);
    }
}
