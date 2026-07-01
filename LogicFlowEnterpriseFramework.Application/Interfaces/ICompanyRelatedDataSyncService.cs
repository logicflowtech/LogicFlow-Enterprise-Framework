using LogicFlowEnterpriseFramework.Application.DTOs;

namespace LogicFlowEnterpriseFramework.Application.Interfaces;

public interface ICompanyRelatedDataSyncService
{
    Task<SyncJobSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<CompanyRelatedDataSyncStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<CompanyRelatedDataSyncStatusResponse> RunSyncAsync(long? sourceCompanyId = null, CancellationToken cancellationToken = default);
}
