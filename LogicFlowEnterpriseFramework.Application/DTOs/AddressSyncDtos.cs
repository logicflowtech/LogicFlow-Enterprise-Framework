namespace LogicFlowEnterpriseFramework.Application.DTOs;

public sealed record AddressSyncResponse(
    string SourceObjectName,
    string? SourceConnectionStringName,
    bool SourceConnectionConfigured,
    int ImportedRows,
    int LinkedUserProfiles,
    int LinkedCompanyProfiles,
    long LocalAddressCount,
    DateTimeOffset CompletedAt,
    string Message);
