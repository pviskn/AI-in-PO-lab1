using CarRental.Application.Abstractions.UseCases;
using CarRental.Application.Common;
using CarRental.Application.DTOs;
using CarRental.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;

    public UsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<UserDto> result = await _userManagementService.GetUsersAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            UserDto user = await _userManagementService.CreateUserAsync(dto, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, user);
        }
        catch (DuplicateUsernameException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRole(
        Guid id,
        [FromBody] AssignRoleDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _userManagementService.AssignRoleAsync(id, dto, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
