using CarRental.Application.Abstractions.UseCases;
using CarRental.Application.DTOs;
using CarRental.Domain.Enums;
using CarRental.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.Api.Controllers;

[ApiController]
[Route("api/cars")]
[Produces("application/json")]
public class CarsController : ControllerBase
{
    private readonly ICarCatalogService _carCatalogService;

    public CarsController(ICarCatalogService carCatalogService)
    {
        _carCatalogService = carCatalogService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Application.Common.PagedResult<CarDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCars(
        [FromQuery] CarStatus? status,
        [FromQuery] CarCategory? category,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new CarFilterDto(status, category, minPrice, maxPrice, page, pageSize);
        Application.Common.PagedResult<CarDto> result = await _carCatalogService.GetCarsAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCarById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        CarDto? car = await _carCatalogService.GetCarByIdAsync(id, cancellationToken);
        if (car == null)
            return NotFound(new { message = "автомобиль не найден" });
        return Ok(car);
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    [ProducesResponseType(typeof(CarDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddCar(
        [FromBody] CreateCarDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CarDto car = await _carCatalogService.AddCarAsync(dto, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, car);
        }
        catch (DuplicateVinException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Manager,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeCarStatus(
        Guid id,
        [FromBody] ChangeCarStatusDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _carCatalogService.ChangeCarStatusAsync(id, dto.NewStatus, cancellationToken);
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

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Manager,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCar(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _carCatalogService.SoftDeleteCarAsync(id, cancellationToken);
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

    [HttpPost("{id:guid}/restore")]
    [Authorize(Roles = "Manager,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreCar(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _carCatalogService.RestoreCarAsync(id, cancellationToken);
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
