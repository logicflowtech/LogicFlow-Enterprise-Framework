using Microsoft.Data.SqlClient;

namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

internal static class SyncStateStore
{
    public static async Task EnsureRowAsync(
        SqlConnection localConnection,
        string syncKey,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken,
        string initialMessage = "Not started")
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM dbo.CompanyProfileSyncState WHERE SourceName = @SourceName)
            BEGIN
                INSERT INTO dbo.CompanyProfileSyncState (SourceName, LastRunSucceeded, LastProcessedRows, LastRunMessage)
                VALUES (@SourceName, NULL, 0, @InitialMessage);
            END;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = commandTimeoutSeconds;
        command.Parameters.AddWithValue("@SourceName", syncKey);
        command.Parameters.AddWithValue("@InitialMessage", initialMessage);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task<SyncStateSnapshot> ReadAsync(
        SqlConnection localConnection,
        string syncKey,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                LastSourceModifiedDateTime,
                LastSourceCompanyId,
                LastStartedAt,
                LastCompletedAt,
                LastRunSucceeded,
                LastProcessedRows,
                LastRunMessage
            FROM dbo.CompanyProfileSyncState
            WHERE SourceName = @SourceName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = commandTimeoutSeconds;
        command.Parameters.AddWithValue("@SourceName", syncKey);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new SyncStateSnapshot(null, null, null, null, null, 0, null);
        }

        return new SyncStateSnapshot(
            reader.IsDBNull(0) ? null : reader.GetDateTime(0),
            reader.IsDBNull(1) ? null : reader.GetInt64(1),
            reader.IsDBNull(2) ? null : reader.GetDateTime(2),
            reader.IsDBNull(3) ? null : reader.GetDateTime(3),
            reader.IsDBNull(4) ? null : reader.GetBoolean(4),
            reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
            reader.IsDBNull(6) ? null : reader.GetString(6));
    }

    public static async Task MarkStartedAsync(
        SqlConnection localConnection,
        string syncKey,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken,
        string message)
    {
        const string sql = """
            UPDATE dbo.CompanyProfileSyncState
            SET LastStartedAt = @LastStartedAt,
                LastRunSucceeded = NULL,
                LastRunMessage = @LastRunMessage
            WHERE SourceName = @SourceName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = commandTimeoutSeconds;
        command.Parameters.AddWithValue("@LastStartedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@LastRunMessage", message);
        command.Parameters.AddWithValue("@SourceName", syncKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task MarkCompletedAsync(
        SqlConnection localConnection,
        string syncKey,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken,
        int processedRows,
        string message,
        DateTime? lastSourceModifiedDateTime = null,
        long? lastSourceCompanyId = null)
    {
        const string sql = """
            UPDATE dbo.CompanyProfileSyncState
            SET LastSourceModifiedDateTime = COALESCE(@LastSourceModifiedDateTime, LastSourceModifiedDateTime),
                LastSourceCompanyId = COALESCE(@LastSourceCompanyId, LastSourceCompanyId),
                LastCompletedAt = @LastCompletedAt,
                LastRunSucceeded = 1,
                LastProcessedRows = @LastProcessedRows,
                LastRunMessage = @LastRunMessage
            WHERE SourceName = @SourceName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = commandTimeoutSeconds;
        command.Parameters.AddWithValue("@LastSourceModifiedDateTime", lastSourceModifiedDateTime.HasValue ? lastSourceModifiedDateTime.Value : DBNull.Value);
        command.Parameters.AddWithValue("@LastSourceCompanyId", lastSourceCompanyId.HasValue ? lastSourceCompanyId.Value : DBNull.Value);
        command.Parameters.AddWithValue("@LastCompletedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@LastProcessedRows", processedRows);
        command.Parameters.AddWithValue("@LastRunMessage", message);
        command.Parameters.AddWithValue("@SourceName", syncKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public static async Task MarkFailedAsync(
        SqlConnection localConnection,
        string syncKey,
        int commandTimeoutSeconds,
        CancellationToken cancellationToken,
        string message)
    {
        const string sql = """
            UPDATE dbo.CompanyProfileSyncState
            SET LastCompletedAt = @LastCompletedAt,
                LastRunSucceeded = 0,
                LastProcessedRows = 0,
                LastRunMessage = @LastRunMessage
            WHERE SourceName = @SourceName;
            """;

        await using var command = localConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = commandTimeoutSeconds;
        command.Parameters.AddWithValue("@LastCompletedAt", DateTime.UtcNow);
        command.Parameters.AddWithValue("@LastRunMessage", message.Length > 4000 ? message[..4000] : message);
        command.Parameters.AddWithValue("@SourceName", syncKey);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

internal sealed record SyncStateSnapshot(
    DateTime? LastSourceModifiedDateTime,
    long? LastSourceCompanyId,
    DateTime? LastStartedAt,
    DateTime? LastCompletedAt,
    bool? LastRunSucceeded,
    int LastProcessedRows,
    string? LastRunMessage);
