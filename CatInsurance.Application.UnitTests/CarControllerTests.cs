using CarInsurance.Api.Controllers;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace CatInsurance.Application.UnitTests
{
    public class CarsControllerTests
    {
        [Fact]
        public async Task Should_ReturnBadRequest_When_DateFormatIsInvalid()
        {
            var svc = Substitute.For<ICarService>();
            var ctrl = new CarsController(svc);

            var res = await ctrl.IsInsuranceValid(1, "06-01-2024"); 

            var bad = Assert.IsType<BadRequestObjectResult>(res.Result);
            Assert.Contains("Invalid date format", bad.Value!.ToString());
            await svc.DidNotReceiveWithAnyArgs().IsInsuranceValidAsync(default, default);
        }

        [Fact]
        public async Task Should_ReturnNotFound_When_CarDoesNotExist()
        {
            var svc = Substitute.For<ICarService>();
            svc.IsInsuranceValidAsync(Arg.Any<long>(), Arg.Any<DateOnly>())
               .Returns(_ => Task.FromException<bool>(new KeyNotFoundException()));

            var ctrl = new CarsController(svc);

            var res = await ctrl.IsInsuranceValid(999, "2024-06-01");

            Assert.IsType<NotFoundResult>(res.Result);
        }

        [Fact]
        public async Task Should_ReturnOk_When_InsuranceIsValid()
        {
            var svc = Substitute.For<ICarService>();
            svc.IsInsuranceValidAsync(1, new DateOnly(2024, 6, 1))
               .Returns(Task.FromResult(true));

            var ctrl = new CarsController(svc);

            var res = await ctrl.IsInsuranceValid(1, "2024-06-01");

            var ok = Assert.IsType<OkObjectResult>(res.Result);
            var body = Assert.IsType<InsuranceValidityResponse>(ok.Value);
            Assert.True(body.Valid);
            Assert.Equal(1, body.CarId);
            Assert.Equal("2024-06-01", body.Date);
        }

        [Fact]
        public async Task Should_ReturnBadRequest_When_DateParameterIsMissing()
        {
            var svc = Substitute.For<ICarService>();
            var ctrl = new CarsController(svc);

            var res = await ctrl.IsInsuranceValid(1, ""); 

            var bad = Assert.IsType<BadRequestObjectResult>(res.Result);
            Assert.Contains("Parameter date is required", bad.Value!.ToString());
            await svc.DidNotReceiveWithAnyArgs().IsInsuranceValidAsync(default, default);
        }
    }
}
