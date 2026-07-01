using LogicFlowEnterpriseFramework.Application.DTOs;

namespace LogicFlowEnterpriseFramework.Application.Interfaces;

public interface IReferenceDataSyncService
{
    Task<IReadOnlyList<SyncJobSummaryResponse>> GetCatalogAsync(CancellationToken cancellationToken = default);
    Task<SyncJobSummaryResponse> RunAsync(string syncKey, CancellationToken cancellationToken = default);
}
