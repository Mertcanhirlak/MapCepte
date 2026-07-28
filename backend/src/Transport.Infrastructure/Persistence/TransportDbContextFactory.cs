using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Transport.Infrastructure.Persistence;

public sealed class TransportDbContextFactory
    : IDesignTimeDbContextFactory<TransportDbContext>
{
    public TransportDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__TransportDb")
            ?? "Host=localhost;Port=5432;Database=mapcepte;Username=mapcepte;Password=mapcepte_dev";

        var options = new DbContextOptionsBuilder<TransportDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.UseNetTopologySuite())
            .Options;

        return new TransportDbContext(options);
    }
}
