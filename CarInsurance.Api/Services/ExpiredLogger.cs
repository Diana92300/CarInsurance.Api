using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services
{
    public class ExpiredLogger(IServiceScopeFactory scopeFactory,
                           ILogger<ExpiredLogger> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await CheckOnce(stoppingToken);

            var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CheckOnce(stoppingToken);
            }
        }
        private async Task CheckOnce(CancellationToken ct)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            var midnight = today.ToDateTime(TimeOnly.MinValue);
            var candidates = await db.Policies
                .Where(p => p.EndDate == today)
                .ToListAsync(ct);

            foreach (var p in candidates)
            {
                if (now < midnight || now > midnight.AddHours(1))
                    continue;

                var already = await db.PolicyExpirationEvents
                    .AnyAsync(e => e.PolicyId == p.Id && e.ExpiredOn == p.EndDate, ct);
                if (already) continue;

                logger.LogInformation(
                    "Policy {PolicyId} for car {CarId} (provider {Provider}) expired on {ExpiredOn:yyyy-MM-dd}",
                    p.Id, p.CarId, p.Provider, p.EndDate);

                db.PolicyExpirationEvents.Add(new PolicyExpiration
                {
                    PolicyId = p.Id,
                    ExpiredOn = p.EndDate,
                    LoggedAtUtc = DateTime.UtcNow
                });

                await db.SaveChangesAsync(ct);
            }
        }
    }
}
