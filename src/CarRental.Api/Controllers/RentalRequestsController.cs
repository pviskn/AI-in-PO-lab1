using CarRental.Application.Abstractions.Services;
using CarRental.Application.Abstractions.UseCases;
using CarRental.Application.Common;
using CarRental.Application.DTOs;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Api.Controllers;

[ApiController]
[Route("api/rental-requests")]
[Authorize]
[Produces("application/json")]
public class RentalRequestsController : ControllerBase
{
    private readonly IRentalRequestService _rentalRequestService;

    private readonly ICurrentUserService _currentUserService;

    public RentalRequestsController(
        IRentalRequestService rentalRequestService,
        ICurrentUserService currentUserService)
    {
        _rentalRequestService = rentalRequestService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ProducesResponseType(typeof(RentalRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateRequest(
    [FromBody] CreateRentalRequestDto dto,
    CancellationToken cancellationToken = default)
    {
        try
        {
            Guid userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("пользователь не авторизован");
            RentalRequestDto result = await _rentalRequestService.CreateRequestAsync(dto, userId, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (CarNotAvailableException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RentalRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRequests(
        [FromQuery] RentalRequestStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("пользователь не авторизован");
        bool isManager = _currentUserService.Roles.Contains(UserRole.Manager.ToString())
            || _currentUserService.Roles.Contains(UserRole.Admin.ToString());

        PagedResult<RentalRequestDto> result = await _rentalRequestService.GetRequestsAsync(
            userId,
            isManager,
            status,
            page,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Manager,Admin")]
    [ProducesResponseType(typeof(RentalContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveRequest(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            RentalContractDto result = await _rentalRequestService.ApproveRequestAsync(id, cancellationToken);
            return Ok(result);
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

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Manager,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectRequest(
        Guid id,
        [FromBody] RejectRentalRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _rentalRequestService.RejectRequestAsync(id, dto.Reason, cancellationToken);
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

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Manager,Admin")]
    [ProducesResponseType(typeof(RentalContractDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteRental(
        Guid id,
        [FromBody] CompleteRentalDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            RentalContractDto result = await _rentalRequestService.CompleteRentalAsync(id, dto, cancellationToken);
            return Ok(result);
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

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Client")]
    [ProducesResponseType(typeof(RentalRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelRequest(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Guid userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("пользователь не авторизован");
            RentalRequestDto result = await _rentalRequestService.CancelRequestAsync(id, userId, cancellationToken);
            return Ok(result);
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
