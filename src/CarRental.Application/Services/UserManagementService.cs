using CarRental.Application.Abstractions.Repositories;
using CarRental.Application.Abstractions.Services;
using CarRental.Application.Abstractions.UseCases;
using CarRental.Application.Common;
using CarRental.Application.DTOs;
using CarRental.Application.Mappings;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;

namespace CarRental.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _uow;

    public UserManagementService(IUserRepository userRepo, IRoleRepository roleRepo, IPasswordHasher passwordHasher, IUnitOfWork uow)
    {
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _passwordHasher = passwordHasher;
        _uow = uow;
    }

    public async Task<PagedResult<UserDto>> GetUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        int realPage = Math.Max(1, page);
        int realPageSize = Math.Clamp(pageSize, 1, 100);

        (IReadOnlyList<User> items, int totalCount) = await _userRepo.GetPagedAsync(realPage, realPageSize, cancellationToken);
        return new PagedResult<UserDto>(items.Select(user => user.ToDto()).ToList(), totalCount, realPage, realPageSize);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        if (await _userRepo.ExistsByUsernameAsync(dto.Username, cancellationToken))
            throw new DuplicateUsernameException(dto.Username);

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

        // Назначаем роль CLIENT по умолчанию
        Role? clientRole = await _roleRepo.GetByRoleTypeAsync(UserRole.Client, cancellationToken);
        if (clientRole != null)
            user.AddRole(clientRole);

        _userRepo.Add(user);
        await _uow.SaveChangesAsync(cancellationToken);

        return user.ToDto();
    }

    public async Task AssignRoleAsync(Guid userId, AssignRoleDto dto, CancellationToken cancellationToken = default)
    {
        User? user = await _userRepo.GetByIdWithRolesAsync(userId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("Пользователь не найден.");

        Role? newRole = await _roleRepo.GetByRoleTypeAsync(dto.Role, cancellationToken);
        if (newRole == null)
            throw new KeyNotFoundException("Роль не найдена.");

        bool hasRole = user.Roles.Any(r => r.Name == newRole.Name);
        if (hasRole)
            throw new InvalidOperationException("У пользователя уже есть эта роль.");

        user.AddRole(newRole);
        _userRepo.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}