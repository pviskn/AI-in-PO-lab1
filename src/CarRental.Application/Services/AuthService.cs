using CarRental.Application.Abstractions.Repositories;
using CarRental.Application.Abstractions.Services;
using CarRental.Application.Abstractions.UseCases;
using CarRental.Application.DTOs;
using CarRental.Application.Mappings;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;

namespace CarRental.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;

    private readonly IRoleRepository _roleRepo;

    private readonly IPasswordHasher _passwordHasher;

    private readonly IJwtTokenGenerator _jwtGenerator;

    private readonly IUnitOfWork _uow;

    public AuthService(IUserRepository userRepo, IRoleRepository roleRepo, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtGenerator, IUnitOfWork uow)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _passwordHasher = passwordHasher;
        _jwtGenerator = jwtGenerator;
        _uow = uow;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        User? user = await _userRepo.GetByUsernameWithRolesAsync(dto.Username, cancellationToken);
        if (user == null)
            throw new UnauthorizedAccessException("Ќеверный логин или пароль.");

        if (!_passwordHasher.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Ќеверный логин или пароль.");

        string token = _jwtGenerator.GenerateToken(user);

        return new AuthResponseDto(token, "Bearer", 3600, user.ToDto());
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        if (await _userRepo.ExistsByUsernameAsync(dto.Username, cancellationToken))
            throw new DuplicateUsernameException(dto.Username);

        Role? clientRole = await _roleRepo.GetByRoleTypeAsync(UserRole.Client, cancellationToken);
        if (clientRole == null)
            throw new InvalidOperationException("роль не найдена в системе");

        string passwordHash = _passwordHasher.Hash(dto.Password);

        var user = new User(
            Guid.NewGuid(),
            dto.Username,
            passwordHash,
            dto.FirstName,
            dto.LastName,
            dto.DateOfBirth,
            dto.LicenseDate,
            dto.Email);

        user.AddRole(clientRole);

        _userRepo.Add(user);
        await _uow.SaveChangesAsync(cancellationToken);

        string token = _jwtGenerator.GenerateToken(user);

        return new AuthResponseDto(token, "Bearer", 3600, user.ToDto());
    }
}
