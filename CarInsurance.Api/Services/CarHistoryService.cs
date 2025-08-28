using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class CarHistoryService:ICarHistoryService
{
    private readonly AppDbContext _db;
    public CarHistoryService(AppDbContext db) => _db = db;

    public async Task<ClaimDto> RegisterClaimAsync(long carId, CreateClaimDto dto, CancellationToken ct)
    {
        var exists = await _db.Cars.AnyAsync(c => c.Id == carId, ct);
        if (!exists) throw new KeyNotFoundException($"car {carId} not found");

        if (string.IsNullOrWhiteSpace(dto.Description))
            throw new ArgumentException("description required");
        if (dto.Amount <= 0)
            throw new ArgumentException("amount must be > 0");

        var claim = new Claim
        {
            CarId = carId,
            ClaimDate = dto.ClaimDate,
            Description = dto.Description,
            Amount = dto.Amount
        };

        _db.Claims.Add(claim);
        await _db.SaveChangesAsync(ct);

        return new ClaimDto(claim.Id, claim.ClaimDate, claim.Description, claim.Amount);
    }
    public async Task<IReadOnlyList<TimelineItemDto>> GetHistoryAsync(long carId, CancellationToken ct)
    {
        var exists = await _db.Cars.AnyAsync(c => c.Id == carId, ct);
        if (!exists) throw new KeyNotFoundException($"car {carId} not found");

        var policies = await _db.Policies
            .Where(p => p.CarId == carId)
            .Select(p => new TimelineItemDto("policy", p.StartDate, p.EndDate, p.Provider, null, null))
            .ToListAsync(ct);

        var claims = await _db.Claims
            .Where(cl => cl.CarId == carId)
            .Select(cl => new TimelineItemDto("claim", cl.ClaimDate, null, null, cl.Amount, cl.Description))
            .ToListAsync(ct);

        return policies.Concat(claims)
                       .OrderBy(i => i.Start)
                       .ToList();
    }
}
