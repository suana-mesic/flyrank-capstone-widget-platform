using Npgsql;

namespace WidgetPlatform.Database;

public static class DatabaseInitializer
{
    public static async Task InitialiseAsync(NpgsqlDataSource dataSource, CancellationToken ct = default)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Database", "init.sql");
        var sql = await File.ReadAllTextAsync(path, ct);

        await using var cmd = dataSource.CreateCommand(sql);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}