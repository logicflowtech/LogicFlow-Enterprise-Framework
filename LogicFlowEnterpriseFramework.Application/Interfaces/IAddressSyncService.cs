using LogicFlowEnterpriseFramework.Application.DTOs;

namespace LogicFlowEnterpriseFramework.Application.Interfaces;

public interface IAddressSyncService
{
    Task<SyncJobSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<AddressSyncResponse> RunSyncAsync(CancellationToken cancellationToken = default);
}
