namespace CarRental.Application.DTOs;

public sealed record AuthResponseDto(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    UserDto User);
