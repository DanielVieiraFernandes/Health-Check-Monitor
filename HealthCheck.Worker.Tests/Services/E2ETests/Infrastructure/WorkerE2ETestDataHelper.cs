using HealthCheck.Framework.Enums;
using HealthCheck.Framework.Models;
using Npgsql;

namespace HealthCheck.Worker.Tests.Services.E2ETests.Infrastructure;

public static class WorkerE2ETestDataHelper
{
    public static async Task EnsureSchemaObjectsAsync(WorkerE2ETestFixture fixture)
    {
        var schema = WorkerE2ETestFixture.SchemaName;

        await using var conn = await fixture.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
CREATE SCHEMA IF NOT EXISTS {schema};

CREATE TABLE IF NOT EXISTS {schema}.users (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password VARCHAR(255) NOT NULL,
    history TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS {schema}.monitored_systems (
    id UUID PRIMARY KEY,
    user_id UUID REFERENCES {schema}.users(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    description TEXT NOT NULL DEFAULT '',
    url TEXT NOT NULL,
    last_status INT NOT NULL DEFAULT 1,
    last_checked_at TIMESTAMP WITHOUT TIME ZONE,
    next_check_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    history TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    UNIQUE(user_id, url)
);

CREATE INDEX IF NOT EXISTS idx_monitored_systems_next_check_at
ON {schema}.monitored_systems(next_check_at);

CREATE TABLE IF NOT EXISTS {schema}.system_checks (
    id BIGSERIAL PRIMARY KEY,
    user_id UUID REFERENCES {schema}.users(id) ON DELETE CASCADE,
    system_id UUID REFERENCES {schema}.monitored_systems(id) ON DELETE CASCADE,
    status INT NOT NULL,
    latency_ms BIGINT NOT NULL,
    checked_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    system_response TEXT DEFAULT NULL,
    error_message TEXT DEFAULT NULL,
    exception_type VARCHAR(150) DEFAULT NULL,
    stack_trace TEXT DEFAULT NULL
);

CREATE TABLE IF NOT EXISTS {schema}.worker_config (
    id SMALLINT PRIMARY KEY CHECK (id = 1),
    monitoring_interval_seconds SMALLINT NOT NULL DEFAULT 30,
    timeout_seconds SMALLINT NOT NULL DEFAULT 10,
    max_concurrent_checks SMALLINT NOT NULL DEFAULT 10,
    max_retries SMALLINT NOT NULL DEFAULT 0,
    delay_between_retries_ms SMALLINT NOT NULL DEFAULT 0,
    updated_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT NOW(),
    user_uuid_last_modified UUID REFERENCES {schema}.users(id) ON DELETE SET NULL
);
";

        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task ResetSchemaDataAsync(WorkerE2ETestFixture fixture)
    {
        var schema = WorkerE2ETestFixture.SchemaName;

        await using var conn = await fixture.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
TRUNCATE TABLE {schema}.system_checks RESTART IDENTITY CASCADE;
TRUNCATE TABLE {schema}.monitored_systems RESTART IDENTITY CASCADE;
TRUNCATE TABLE {schema}.worker_config RESTART IDENTITY CASCADE;
TRUNCATE TABLE {schema}.users RESTART IDENTITY CASCADE;
";

        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task SeedWorkerConfigAsync(WorkerE2ETestFixture fixture, WorkerConfig config)
    {
        await using var conn = await fixture.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO worker_config (
    id,
    monitoring_interval_seconds,
    timeout_seconds,
    max_concurrent_checks,
    max_retries,
    delay_between_retries_ms,
    updated_at,
    user_uuid_last_modified)
VALUES (1, @MonitoringIntervalSeconds, @TimeoutSeconds, @MaxConcurrentChecks, @MaxRetries, @DelayBetweenRetriesMs, NOW(), @UserUUIDLastModified)
ON CONFLICT (id) DO UPDATE SET
    monitoring_interval_seconds = EXCLUDED.monitoring_interval_seconds,
    timeout_seconds = EXCLUDED.timeout_seconds,
    max_concurrent_checks = EXCLUDED.max_concurrent_checks,
    max_retries = EXCLUDED.max_retries,
    delay_between_retries_ms = EXCLUDED.delay_between_retries_ms,
    updated_at = NOW(),
    user_uuid_last_modified = EXCLUDED.user_uuid_last_modified;";

        cmd.Parameters.AddWithValue("MonitoringIntervalSeconds", config.MonitoringIntervalSeconds);
        cmd.Parameters.AddWithValue("TimeoutSeconds", config.TimeoutSeconds);
        cmd.Parameters.AddWithValue("MaxConcurrentChecks", config.MaxConcurrentChecks);
        cmd.Parameters.AddWithValue("MaxRetries", config.MaxRetries);
        cmd.Parameters.AddWithValue("DelayBetweenRetriesMs", config.DelayBetweenRetriesMs);
        cmd.Parameters.AddWithValue("UserUUIDLastModified", config.UserUUIDLastModified == Guid.Empty ? DBNull.Value : config.UserUUIDLastModified);

        await cmd.ExecuteNonQueryAsync();
    }

    public static async Task<Guid> SeedUserAsync(WorkerE2ETestFixture fixture, string runId, string? email = null)
    {
        var userId = Guid.NewGuid();
        var userEmail = email ?? $"e2e-{runId}-{Guid.NewGuid():N}@example.com";

        await using var conn = await fixture.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO users (id, name, email, password, history, created_at, updated_at)
VALUES (@Id, @Name, @Email, @Password, '', NOW(), NOW());";

        cmd.Parameters.AddWithValue("Id", userId);
        cmd.Parameters.AddWithValue("Name", $"E2E User {runId}");
        cmd.Parameters.AddWithValue("Email", userEmail);
        cmd.Parameters.AddWithValue("Password", "e2e-password");

        await cmd.ExecuteNonQueryAsync();

        return userId;
    }

    public static async Task<List<Guid>> SeedMonitoredSystemsAsync(
        WorkerE2ETestFixture fixture,
        Guid userId,
        string runId,
        int count,
        Func<int, string> urlFactory,
        HealthStatus lastStatus = HealthStatus.Healthy)
    {
        var ids = new List<Guid>(count);

        await using var conn = await fixture.OpenConnectionAsync();

        for (var i = 0; i < count; i++)
        {
            var systemId = Guid.NewGuid();
            ids.Add(systemId);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO monitored_systems (
    id,
    user_id,
    name,
    description,
    url,
    last_status,
    last_checked_at,
    next_check_at,
    history,
    created_at,
    updated_at)
VALUES (@Id, @UserId, @Name, '', @Url, @LastStatus, NULL, NOW() - INTERVAL '1 minute', '', NOW(), NOW());";

            cmd.Parameters.AddWithValue("Id", systemId);
            cmd.Parameters.AddWithValue("UserId", userId);
            cmd.Parameters.AddWithValue("Name", $"E2E-{runId}-System-{i}");
            cmd.Parameters.AddWithValue("Url", urlFactory(i));
            cmd.Parameters.AddWithValue("LastStatus", (int)lastStatus);

            await cmd.ExecuteNonQueryAsync();
        }

        return ids;
    }

    public static async Task<int> CountChecksByRunIdAsync(WorkerE2ETestFixture fixture, string runId)
    {
        await using var conn = await fixture.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT COUNT(*)
FROM system_checks c
INNER JOIN monitored_systems ms ON ms.id = c.system_id
WHERE ms.name ILIKE @RunName;";
        cmd.Parameters.AddWithValue("RunName", $"%E2E-{runId}-%");

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public static async Task<int> CountSystemsByStatusAsync(WorkerE2ETestFixture fixture, string runId, HealthStatus status)
    {
        await using var conn = await fixture.OpenConnectionAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT COUNT(*)
FROM monitored_systems
WHERE name ILIKE @RunName
  AND last_status = @Status;";

        cmd.Parameters.AddWithValue("RunName", $"%E2E-{runId}-%");
        cmd.Parameters.AddWithValue("Status", (int)status);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }
}
