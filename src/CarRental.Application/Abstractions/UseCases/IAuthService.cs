using CarRental.Application.DTOs;

namespace CarRental.Application.Abstractions.UseCases;

public interface IAuthService
{
    Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default);

    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);
}
