using LogicFlowEnterpriseFramework.Application.DTOs;

namespace LogicFlowEnterpriseFramework.Application.Interfaces;

public interface ICompanyUserSyncService
{
    Task<SyncJobSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<CompanyUserSyncStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<CompanyUserSyncStatusResponse> RunSyncAsync(long? sourceCompanyId = null, CancellationToken cancellationToken = default);
}
