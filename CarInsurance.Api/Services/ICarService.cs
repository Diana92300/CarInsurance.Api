namespace CarInsurance.Api.Services;

using CarInsurance.Api.Dtos;

public interface ICarService
{
    Task<List<CarDto>> ListCarsAsync();
    Task<bool> IsInsuranceValidAsync(long carId, DateOnly date);
}
