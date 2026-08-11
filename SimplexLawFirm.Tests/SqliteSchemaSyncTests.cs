using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SimplexLawFirm.Data;
using Xunit;

namespace SimplexLawFirm.Tests;

public class SqliteSchemaSyncTests
{
    // Reproduces the production outage: EnsureCreatedAsync builds the schema once and never revisits it,
    // so a column added to the model after a SQLite file already exists is silently missing from it,
    // and every query touching that column throws at runtime. This drops a real column back off an
    // already-created database to simulate that drift, then confirms the sync utility restores it
    // without disturbing existing data.
    [Fact]
    public async Task Restores_a_column_missing_from_an_already_created_database_without_losing_data()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();

        var client = new SimplexLawFirm.Models.Client { FirstName = "Drift", LastName = "Client", Email = "drift@test", Phone = "1" };
        context.Clients.Add(client);
        await context.SaveChangesAsync();
        var matter = new SimplexLawFirm.Models.Case { CaseNumber = "M-DRIFT-1", Title = "Drift matter", CaseType = "Commercial", Status = SimplexLawFirm.Models.CaseStatus.Active, ClientId = client.Id };
        context.Cases.Add(matter);
        await context.SaveChangesAsync();

        await using (var drop = connection.CreateCommand())
        {
            drop.CommandText = "ALTER TABLE CaseHandovers DROP COLUMN ClientNotifiedAtUtc;";
            await drop.ExecuteNonQueryAsync();
        }

        await Assert.ThrowsAsync<SqliteException>(() => context.CaseHandovers.ToListAsync());

        await SqliteSchemaSync.EnsureUpToDateAsync(context, NullLogger.Instance);

        var handovers = await context.CaseHandovers.ToListAsync();
        Assert.Empty(handovers);
        var survivedMatter = await context.Cases.SingleAsync(x => x.CaseNumber == "M-DRIFT-1");
        Assert.Equal("Drift matter", survivedMatter.Title);

        await connection.CloseAsync();
    }
}
