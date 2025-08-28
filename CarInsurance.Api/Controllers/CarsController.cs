using CarInsurance.Api.Dtos;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace CarInsurance.Api.Controllers;

[ApiController]
[Route("api")]
public class CarsController(ICarService service) : ControllerBase
{
    private readonly ICarService _service = service;

    [HttpGet("cars")]
    public async Task<ActionResult<List<CarDto>>> GetCars()
        => Ok(await _service.ListCarsAsync());

    [HttpGet("cars/{carId:long:min(1)}/insurance-valid")]
    public async Task<ActionResult<InsuranceValidityResponse>> IsInsuranceValid(long carId, [FromQuery] string date)
    {
        if (string.IsNullOrWhiteSpace(date))
            return BadRequest("Parameter date is required");

        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd",default, DateTimeStyles.None, out var parsed))
            return BadRequest("Invalid date format");
        try
        {
            var valid = await _service.IsInsuranceValidAsync(carId, parsed);
            return Ok(new InsuranceValidityResponse(carId, parsed.ToString("yyyy-MM-dd"), valid));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
    [HttpPost("cars/{carId:long}/claims")]
    public async Task<IActionResult> CreateClaim(long carId, [FromBody] CreateClaimDto dto, [FromServices] ICarHistoryService svc, CancellationToken ct)
    {
        try { return CreatedAtAction(nameof(GetHistory), new { carId }, await svc.RegisterClaimAsync(carId, dto, ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("cars/{carId:long}/history")]
    public async Task<IActionResult> GetHistory(long carId, [FromServices] ICarHistoryService svc, CancellationToken ct)
    {
        try { return Ok(await svc.GetHistoryAsync(carId, ct)); }
        catch (KeyNotFoundException) { return NotFound(); }
    }


}
