using System.Net;
using System.Net.Http.Json;
using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CarInsurance.IntegrationTests;

public class CarsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _rootScope;
    private readonly AppDbContext _db;
    private readonly HttpClient _client;

    private const string CarsBase = "/api/cars";

    public CarsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
                services.RemoveAll<AppDbContext>();

                var dbName = "it_db_" + Guid.NewGuid();
                services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(dbName));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
                Seed(db);
            });
        });

        _rootScope = _factory.Services.CreateScope();
        _db = _rootScope.ServiceProvider.GetRequiredService<AppDbContext>();

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
    [Fact]
    public async Task Should_ReturnTrue_When_DateEqualsPolicyStart()
    {
        var res = await _client.GetAsync($"{CarsBase}/1/insurance-valid?date=2024-01-01");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<InsuranceValidityResponse>();
        Assert.NotNull(body);
        Assert.True(body!.Valid);
    }

    [Fact]
    public async Task Should_ReturnTrue_When_DateEqualsPolicyEnd()
    {
        var res = await _client.GetAsync($"{CarsBase}/1/insurance-valid?date=2024-12-31");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<InsuranceValidityResponse>();
        Assert.NotNull(body);
        Assert.True(body!.Valid);
    }


    [Fact]
    public async Task Should_ReturnCarList_When_CarsExist()
    {
        var res = await _client.GetAsync(CarsBase);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var cars = await res.Content.ReadFromJsonAsync<List<CarDto>>();
        Assert.NotNull(cars);
        Assert.NotEmpty(cars!);
    }

    [Fact]
    public async Task Should_ReturnTrue_When_DateIsWithinInsuranceRange()
    {
        var res = await _client.GetAsync($"{CarsBase}/1/insurance-valid?date=2024-06-01");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<InsuranceValidityResponse>();
        Assert.NotNull(body);
        Assert.True(body!.Valid);
        Assert.Equal(1, body.CarId);
        Assert.Equal("2024-06-01", body.Date);
    }

    [Fact]
    public async Task Should_ReturnFalse_When_DateIsOutsideInsuranceRange()
    {
        var res = await _client.GetAsync($"{CarsBase}/1/insurance-valid?date=2025-01-05");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadFromJsonAsync<InsuranceValidityResponse>();
        Assert.NotNull(body);
        Assert.False(body!.Valid);
    }

    [Fact]
    public async Task Should_ReturnBadRequest_When_DateFormatIsInvalid()
    {
        var res = await _client.GetAsync($"{CarsBase}/1/insurance-valid?date=06-01-2024"); 
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        var text = await res.Content.ReadAsStringAsync();
        Assert.Contains("Invalid date format", text);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_CarDoesNotExist()
    {
        var res = await _client.GetAsync($"{CarsBase}/999/insurance-valid?date=2024-06-01");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private static void Seed(AppDbContext db)
    {
        if (db.Owners.Any()) return;

        var owner = new Owner { Name = "Test Owner", Email = "test@example.com" };
        db.Owners.Add(owner);
        db.SaveChanges();

        var car = new Car
        {
            Id = 1,
            Vin = "VIN-IT-001",
            Make = "Dacia",
            Model = "Logan",
            YearOfManufacture = 2020,
            OwnerId = owner.Id
        };
        db.Cars.Add(car);
        db.SaveChanges();

        db.Policies.Add(new InsurancePolicy
        {
            CarId = car.Id,
            Provider = "Allianz",
            StartDate = new DateOnly(2024, 1, 1),
            EndDate = new DateOnly(2024, 12, 31)
        });
        db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        _rootScope.Dispose();
        GC.SuppressFinalize(this);
    }
}
