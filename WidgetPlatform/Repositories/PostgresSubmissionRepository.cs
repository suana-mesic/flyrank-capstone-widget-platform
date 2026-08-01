using Npgsql;
using NpgsqlTypes;
using WidgetPlatform.Models;

namespace WidgetPlatform.Repositories;

public sealed class PostgresSubmissionRepository : ISubmissionRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresSubmissionRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<Submission> AddAsync(Submission s, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO submissions
                (id, widget_id, tenant_id, data_json, ip_address, country, city, created_at_utc)
            VALUES ($1, $2, $3, $4, $5, $6, $7, $8);
            """;

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue(s.Id);
        cmd.Parameters.AddWithValue(s.WidgetId);
        cmd.Parameters.AddWithValue(s.TenantId);
        cmd.Parameters.Add(new NpgsqlParameter
        {
            Value = s.DataJson,
            NpgsqlDbType = NpgsqlDbType.Jsonb
        });
        cmd.Parameters.AddWithValue((object?)s.IpAddress ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)s.Country ?? DBNull.Value);
        cmd.Parameters.AddWithValue((object?)s.City ?? DBNull.Value);
        cmd.Parameters.AddWithValue(s.CreatedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);
        return s;
    }

    public async Task<IReadOnlyList<Submission>> GetForTenantAsync(
    Guid tenantId, Guid? widgetId, int limit, int offset, CancellationToken ct)
    {
        const string sql = """
        SELECT id, widget_id, tenant_id, data_json, ip_address, country, city, created_at_utc
        FROM submissions
        WHERE tenant_id = $1
          AND ($2::uuid IS NULL OR widget_id = $2)
        ORDER BY created_at_utc DESC
        LIMIT $3 OFFSET $4;
        """;

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue(tenantId);
        cmd.Parameters.Add(new NpgsqlParameter
        {
            Value = (object?)widgetId ?? DBNull.Value,
            NpgsqlDbType = NpgsqlDbType.Uuid
        });
        cmd.Parameters.AddWithValue(limit);
        cmd.Parameters.AddWithValue(offset);

        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var list = new List<Submission>();
        while (await reader.ReadAsync(ct))
        {
            list.Add(new Submission(
                reader.GetGuid(0),
                reader.GetGuid(1),
                reader.GetGuid(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.GetDateTime(7)));
        }

        return list;
    }

    public async Task<SubmissionStats> GetStatsAsync(Guid tenantId, CancellationToken ct)
    {
        const string totalsSql = """
        SELECT count(*) AS total,
               count(*) FILTER (WHERE created_at_utc >= now() - interval '7 days') AS last7
        FROM submissions
        WHERE tenant_id = $1;
        """;

        int total = 0, last7 = 0;

        await using (var cmd = _dataSource.CreateCommand(totalsSql))
        {
            cmd.Parameters.AddWithValue(tenantId);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            if (await r.ReadAsync(ct))
            {
                total = (int)r.GetInt64(0);
                last7 = (int)r.GetInt64(1);
            }
        }

        const string byWidgetSql = """
        SELECT s.widget_id, w.title, count(*)
        FROM submissions s
        JOIN widgets w ON w.id = s.widget_id
        WHERE s.tenant_id = $1
        GROUP BY s.widget_id, w.title
        ORDER BY count(*) DESC;
        """;

        var byWidget = new List<WidgetCount>();

        await using (var cmd = _dataSource.CreateCommand(byWidgetSql))
        {
            cmd.Parameters.AddWithValue(tenantId);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                byWidget.Add(new WidgetCount(r.GetGuid(0), r.GetString(1), (int)r.GetInt64(2)));
        }

        const string byCountrySql = """
        SELECT coalesce(country, 'Unknown'), count(*)
        FROM submissions
        WHERE tenant_id = $1
        GROUP BY coalesce(country, 'Unknown')
        ORDER BY count(*) DESC
        LIMIT 10;
        """;

        var byCountry = new List<CountryCount>();

        await using (var cmd = _dataSource.CreateCommand(byCountrySql))
        {
            cmd.Parameters.AddWithValue(tenantId);
            await using var r = await cmd.ExecuteReaderAsync(ct);
            while (await r.ReadAsync(ct))
                byCountry.Add(new CountryCount(r.GetString(0), (int)r.GetInt64(1)));
        }

        return new SubmissionStats(total, last7, byWidget, byCountry);
    }
}