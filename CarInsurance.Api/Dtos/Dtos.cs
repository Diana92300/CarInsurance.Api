namespace CarInsurance.Api.Dtos;

public record CarDto(long Id, string Vin, string? Make, string? Model, int Year, long OwnerId, string OwnerName, string? OwnerEmail);
public record InsuranceValidityResponse(long CarId, string Date, bool Valid);
public record CreateClaimDto(DateOnly ClaimDate, string Description, decimal Amount);
public record ClaimDto(long Id, DateOnly ClaimDate, string Description, decimal Amount);
public record TimelineItemDto(string Type, DateOnly Start, DateOnly? End, string? Provider, decimal? Amount, string? Description);
