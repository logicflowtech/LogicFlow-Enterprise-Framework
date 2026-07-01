namespace LogicFlowEnterpriseFramework.Application.DTOs;

public sealed record CompanyFinancialDataSyncStatusResponse(
    string SourceConnectionStringName,
    bool SourceConnectionConfigured,
    int BatchSize,
    long LocalRowCount,
    DateTimeOffset? LastStartedAt,
    DateTimeOffset? LastCompletedAt,
    bool? LastRunSucceeded,
    int LastProcessedRows,
    string? LastRunMessage,
    long? LastSourceCompanyId);
