using CarRental.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CarRental.Application.DTOs;

public sealed record CreateCarDto(
    [Required, StringLength(17, MinimumLength = 17, ErrorMessage = "VIN должен содержать ровно 17 символов.")]
    string Vin,

    [Required, StringLength(100)]
    string Make,

    [Required, StringLength(100)]
    string Model,

    [Range(1900, 2100, ErrorMessage = "Год выпуска должен быть в диапазоне 1900–2100.")]
    int Year,

    CarCategory Category,

    [Range(0.01, double.MaxValue, ErrorMessage = "Стоимость аренды должна быть больше нуля.")]
    decimal PricePerDay,

    [Range(0, int.MaxValue, ErrorMessage = "Пробег не может быть отрицательным.")]
    int Mileage);
