using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace SimplexLawFirm.Data;

// EnsureCreatedAsync only builds the schema the first time a SQLite database file is created; it is a
// no-op against an existing file, so every table/column added to the model afterwards silently never
// reaches an already-deployed database. This reconciles a live SQLite database against the current
// model by diffing it against a throwaway in-memory database built fresh from the same model, adding
// only what's missing (new tables, new columns) — it never drops or alters existing data.
public static class SqliteSchemaSync
{
    public static async Task EnsureUpToDateAsync(ApplicationDbContext context, ILogger logger, CancellationToken ct = default)
    {
        var liveConnection = (SqliteConnection)context.Database.GetDbConnection();
        var wasOpen = liveConnection.State == System.Data.ConnectionState.Open;
        if (!wasOpen) await liveConnection.OpenAsync(ct);
        try
        {
            await using var shadowConnection = new SqliteConnection("Data Source=:memory:");
            await shadowConnection.OpenAsync(ct);
            var shadowOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(shadowConnection).Options;
            await using var shadow = new ApplicationDbContext(shadowOptions);
            await shadow.Database.EnsureCreatedAsync(ct);

            var targetTables = await GetTableCreateSqlAsync(shadowConnection, ct);
            var liveTables = await GetTableNamesAsync(liveConnection, ct);

            await using (var fkOff = liveConnection.CreateCommand())
            {
                fkOff.CommandText = "PRAGMA foreign_keys=OFF;";
                await fkOff.ExecuteNonQueryAsync(ct);
            }

            foreach (var (table, createSql) in targetTables)
            {
                if (!liveTables.Contains(table))
                {
                    logger.LogWarning("SQLite schema sync: creating missing table {Table}", table);
                    await using var create = liveConnection.CreateCommand();
                    create.CommandText = createSql;
                    await create.ExecuteNonQueryAsync(ct);
                    continue;
                }

                var targetColumns = await GetColumnsAsync(shadowConnection, table, ct);
                var liveColumns = await GetColumnsAsync(liveConnection, table, ct);
                foreach (var column in targetColumns)
                {
                    if (liveColumns.Any(c => string.Equals(c.Name, column.Name, StringComparison.OrdinalIgnoreCase))) continue;
                    var defaultLiteral = column.DefaultValue ?? (column.NotNull ? FallbackDefaultLiteral(column.Type) : null);
                    var defaultClause = defaultLiteral is null ? "" : $" DEFAULT {defaultLiteral}";
                    var notNullClause = column.NotNull ? " NOT NULL" : "";
                    var sql = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column.Name}\" {column.Type}{notNullClause}{defaultClause};";
                    try
                    {
                        logger.LogWarning("SQLite schema sync: adding missing column {Table}.{Column}", table, column.Name);
                        await using var alter = liveConnection.CreateCommand();
                        alter.CommandText = sql;
                        await alter.ExecuteNonQueryAsync(ct);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "SQLite schema sync: failed to add {Table}.{Column}", table, column.Name);
                    }
                }
            }

            await using (var fkOn = liveConnection.CreateCommand())
            {
                fkOn.CommandText = "PRAGMA foreign_keys=ON;";
                await fkOn.ExecuteNonQueryAsync(ct);
            }
        }
        finally
        {
            if (!wasOpen) await liveConnection.CloseAsync();
        }
    }

    private static string FallbackDefaultLiteral(string sqliteType)
    {
        var t = sqliteType.ToUpperInvariant();
        if (t.Contains("INT")) return "0";
        if (t.Contains("REAL") || t.Contains("DOUB") || t.Contains("FLOA") || t.Contains("DECIMAL") || t.Contains("NUMERIC")) return "0";
        return "''";
    }

    private static async Task<List<(string Table, string CreateSql)>> GetTableCreateSqlAsync(SqliteConnection connection, CancellationToken ct)
    {
        var result = new List<(string, string)>();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name, sql FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name <> '__EFMigrationsHistory'";
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            result.Add((reader.GetString(0), reader.GetString(1)));
        return result;
    }

    private static async Task<HashSet<string>> GetTableNamesAsync(SqliteConnection connection, CancellationToken ct)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(reader.GetString(0));
        return result;
    }

    private sealed record ColumnInfo(string Name, string Type, bool NotNull, string? DefaultValue);

    private static async Task<List<ColumnInfo>> GetColumnsAsync(SqliteConnection connection, string table, CancellationToken ct)
    {
        var result = new List<ColumnInfo>();
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{table}\")";
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var defaultValue = reader.IsDBNull(4) ? null : reader.GetString(4);
            result.Add(new ColumnInfo(reader.GetString(1), reader.GetString(2), reader.GetInt32(3) == 1, defaultValue));
        }
        return result;
    }
}
