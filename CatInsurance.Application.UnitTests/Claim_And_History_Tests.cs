using CarInsurance.Api.Controllers;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace CatInsurance.Application.UnitTests
{
    public class Claim_And_History_Tests
    {
        [Fact]
        public async Task Should_CreateClaim_When_InputIsValid()
        {
            var svc = Substitute.For<ICarHistoryService>();
            var carSvc = Substitute.For<ICarService>(); 
            var ctrl = new CarsController(carSvc);

            var dto = new CreateClaimDto(
                ClaimDate: new DateOnly(2025, 8, 28),
                Description: "Accident test",
                Amount: 1000m
            );

            var returned = new ClaimDto(1, dto.ClaimDate, dto.Description, dto.Amount);

            svc.RegisterClaimAsync(1, dto, Arg.Any<CancellationToken>())
               .Returns(Task.FromResult(returned));

            var res = await ctrl.CreateClaim(1, dto, svc, default);

            var created = Assert.IsType<CreatedAtActionResult>(res);
            var body = Assert.IsType<ClaimDto>(created.Value);
            Assert.Equal(1, body.Id);
            Assert.Equal(dto.Amount, body.Amount);
        }

        [Fact]
        public async Task Should_ReturnNotFound_When_CarDoesNotExist_OnClaimCreation()
        {
            var svc = Substitute.For<ICarHistoryService>();
            var ctrl = new CarsController(Substitute.For<ICarService>());

            var dto = new CreateClaimDto(new DateOnly(2025, 1, 1), "x", 1);

            svc.RegisterClaimAsync(999, dto, Arg.Any<CancellationToken>())
               .Returns(_ => Task.FromException<ClaimDto>(new KeyNotFoundException()));

            var res = await ctrl.CreateClaim(999, dto, svc, default);

            Assert.IsType<NotFoundResult>(res);
        }

        [Fact]
        public async Task Should_ReturnBadRequest_When_ClaimInputIsInvalid()
        {
            var svc = Substitute.For<ICarHistoryService>();
            var ctrl = new CarsController(Substitute.For<ICarService>());

            var dto = new CreateClaimDto(new DateOnly(2025, 1, 1), "", 0); 

            svc.RegisterClaimAsync(1, dto, Arg.Any<CancellationToken>())
               .Returns(_ => Task.FromException<ClaimDto>(new ArgumentException("Amount must be > 0")));

            var res = await ctrl.CreateClaim(1, dto, svc, default);

            var bad = Assert.IsType<BadRequestObjectResult>(res);
            Assert.Contains("Amount", bad.Value!.ToString());
        }

        [Fact]
        public async Task Should_ReturnHistoryList_When_CarExists()
        {
            var historySvc = Substitute.For<ICarHistoryService>();
            var ctrl = new CarsController(Substitute.For<ICarService>());

            var items = new List<TimelineItemDto>
            {
                new("policy", new DateOnly(2025,1,1), new DateOnly(2025,12,31), "Allianz", null, null),
                new("claim",  new DateOnly(2025,6,1),  null,                     null,     500m, "Scratch")
            };

            historySvc.GetHistoryAsync(1, Arg.Any<CancellationToken>())
                      .Returns(Task.FromResult<IReadOnlyList<TimelineItemDto>>(items));

            var res = await ctrl.GetHistory(1, historySvc, default);

            var ok = Assert.IsType<OkObjectResult>(res);
            var body = Assert.IsAssignableFrom<IReadOnlyList<TimelineItemDto>>(ok.Value);
            Assert.Equal(2, body.Count);
            Assert.Equal("policy", body[0].Type);
            Assert.Equal("claim", body[1].Type);
        }

        [Fact]
        public async Task Should_ReturnNotFound_When_CarDoesNotExist_OnHistoryRequest()
        {
            var historySvc = Substitute.For<ICarHistoryService>();
            var ctrl = new CarsController(Substitute.For<ICarService>());

            historySvc.GetHistoryAsync(999, Arg.Any<CancellationToken>())
                      .Returns(_ => Task.FromException<IReadOnlyList<TimelineItemDto>>(new KeyNotFoundException()));

            var res = await ctrl.GetHistory(999, historySvc, default);

            Assert.IsType<NotFoundResult>(res);
        }
    }
}
