using CarRental.Domain.Entities;

namespace CarRental.Application.Abstractions.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
}
