using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using WidgetPlatform.Models;

namespace WidgetPlatform.Repositories;

public sealed class PostgresWidgetRepository : IWidgetRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresWidgetRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    private const string SelectColumns =
        "id, tenant_id, type, title, fields_json, is_active, version, created_at_utc";

    public async Task<IReadOnlyList<Widget>> GetAllForTenantAsync(Guid tenantId, CancellationToken ct)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM widgets
            WHERE tenant_id = $1
            ORDER BY created_at_utc DESC;
            """;

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue(tenantId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var widgets = new List<Widget>();
        while (await reader.ReadAsync(ct))
            widgets.Add(Map(reader));

        return widgets;
    }

    public async Task<Widget?> GetByIdForTenantAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        var sql = $"""
            SELECT {SelectColumns}
            FROM widgets
            WHERE id = $1 AND tenant_id = $2;
            """;

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(tenantId);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }

    public async Task<Widget> AddAsync(Widget widget, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO widgets (id, tenant_id, type, title, fields_json, is_active, version, created_at_utc)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8);
            """;

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue(widget.Id);
        cmd.Parameters.AddWithValue(widget.TenantId);
        cmd.Parameters.AddWithValue(widget.Type);
        cmd.Parameters.AddWithValue(widget.Title);
        cmd.Parameters.Add(new NpgsqlParameter
        {
            Value = JsonSerializer.Serialize(widget.Fields),
            NpgsqlDbType = NpgsqlDbType.Jsonb
        });
        cmd.Parameters.AddWithValue(widget.IsActive);
        cmd.Parameters.AddWithValue(widget.Version);
        cmd.Parameters.AddWithValue(widget.CreatedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);
        return widget;
    }

    public async Task<bool> UpdateAsync(Widget widget, CancellationToken ct)
    {
        const string sql = """
            UPDATE widgets
            SET type = $3, title = $4, fields_json = $5, is_active = $6, version = version + 1
            WHERE id = $1 AND tenant_id = $2;
            """;

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue(widget.Id);
        cmd.Parameters.AddWithValue(widget.TenantId);
        cmd.Parameters.AddWithValue(widget.Type);
        cmd.Parameters.AddWithValue(widget.Title);
        cmd.Parameters.Add(new NpgsqlParameter
        {
            Value = JsonSerializer.Serialize(widget.Fields),
            NpgsqlDbType = NpgsqlDbType.Jsonb
        });
        cmd.Parameters.AddWithValue(widget.IsActive);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        const string sql = "DELETE FROM widgets WHERE id = $1 AND tenant_id = $2;";

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue(id);
        cmd.Parameters.AddWithValue(tenantId);

        return await cmd.ExecuteNonQueryAsync(ct) > 0;
    }

    private static Widget Map(NpgsqlDataReader reader) => new(
        reader.GetGuid(0),
        reader.GetGuid(1),
        reader.GetString(2),
        reader.GetString(3),
        JsonSerializer.Deserialize<List<WidgetField>>(reader.GetString(4))!,
        reader.GetBoolean(5),
        reader.GetInt32(6),
        reader.GetDateTime(7));

    public async Task<Widget?> GetActiveByIdAsync(Guid id, CancellationToken ct)
    {
        var sql = $"""
        SELECT {SelectColumns}
        FROM widgets
        WHERE id = $1 AND is_active = TRUE;
        """;

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue(id);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? Map(reader) : null;
    }
}