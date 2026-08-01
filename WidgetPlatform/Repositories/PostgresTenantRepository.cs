using Npgsql;
using WidgetPlatform.Models;

namespace WidgetPlatform.Repositories;

public sealed class PostgresTenantRepository : ITenantRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgresTenantRepository(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<Tenant?> GetByEmailAsync(string email, CancellationToken ct)
    {
        const string sql = """
            SELECT id, email, password_hash, created_at_utc
            FROM tenants
            WHERE email = $1;
            """;

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue(email);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            return null;

        return new Tenant(
            reader.GetGuid(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetDateTime(3));
    }

    public async Task<Tenant> AddAsync(string email, string passwordHash, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO tenants (id, email, password_hash, created_at_utc)
            VALUES ($1, $2, $3, $4);
            """;

        var tenant = new Tenant(Guid.NewGuid(), email, passwordHash, DateTime.UtcNow);

        await using var cmd = _dataSource.CreateCommand(sql);
        cmd.Parameters.AddWithValue(tenant.Id);
        cmd.Parameters.AddWithValue(tenant.Email);
        cmd.Parameters.AddWithValue(tenant.PasswordHash);
        cmd.Parameters.AddWithValue(tenant.CreatedAtUtc);

        await cmd.ExecuteNonQueryAsync(ct);
        return tenant;
    }
}