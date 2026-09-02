using ElectroPi.Infrastructure.Persistance;
using Microsoft.EntityFrameworkCore;

namespace ElectroPi.Tests.Common
{
    // A dedicated database (never the app's real "ElectroPi" database) on the same local
    // SQL Server Express instance the app uses, so tests see the exact same engine/dialect
    // as production (GETUTCDATE(), identity columns, etc.) instead of an approximation.
    // Override with ELECTROPI_TEST_CONNECTION_STRING to point at a different instance (e.g. in CI).
    public class SqlServerTestDatabaseFixture : IAsyncLifetime
    {
        private const string DefaultConnectionString =
            "Server=DESKTOP-NAPUIAL\\SQLEXPRESS;Database=ElectroPi_Tests;Trusted_Connection=True;Encrypt=False;TrustServerCertificate=True";

        public string ConnectionString { get; } =
            Environment.GetEnvironmentVariable("ELECTROPI_TEST_CONNECTION_STRING") ?? DefaultConnectionString;

        public async Task InitializeAsync()
        {
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
        }

        private AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            return new AppDbContext(options);
        }
    }

    [CollectionDefinition(Name)]
    public class TicketSqlServerCollection : ICollectionFixture<SqlServerTestDatabaseFixture>
    {
        public const string Name = "TicketSqlServer";
    }
}
