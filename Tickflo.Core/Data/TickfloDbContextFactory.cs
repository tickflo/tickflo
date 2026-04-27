namespace Tickflo.Core.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class TickfloDbContextFactory : IDesignTimeDbContextFactory<TickfloDbContext>
{
    public TickfloDbContext CreateDbContext(string[] args)
    {
        var host = Environment.GetEnvironmentVariable("PostgresHost") ?? "localhost";
        var database = Environment.GetEnvironmentVariable("PostresDatabase") ?? "tickflo";
        var user = Environment.GetEnvironmentVariable("PostgresUser") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("PostgresPassword") ?? "postgres";

        var connectionString = $"Host={host};Port=5432;Database={database};Username={user};Password={password}";

        var optionsBuilder = new DbContextOptionsBuilder<TickfloDbContext>();
        optionsBuilder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention();

        return new TickfloDbContext(optionsBuilder.Options);
    }
}
