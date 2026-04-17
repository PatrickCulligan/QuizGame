using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using QuizGame.Data;

namespace QuizGame.Services.HealthChecks;

public class DbHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DbHealthCheck(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = _dbFactory.CreateDbContext();
            // quick connectivity check; avoid heavy queries
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database reachable")
                : HealthCheckResult.Unhealthy("Database not reachable");
        }
        catch (System.Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check failed", ex);
        }
    }
}