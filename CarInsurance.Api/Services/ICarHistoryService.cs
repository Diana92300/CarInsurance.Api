namespace CarInsurance.Api.Services;

using CarInsurance.Api.Dtos;

public interface ICarHistoryService
{
    Task<ClaimDto> RegisterClaimAsync(long carId, CreateClaimDto dto, CancellationToken ct);
    Task<IReadOnlyList<TimelineItemDto>> GetHistoryAsync(long carId, CancellationToken ct);
}
