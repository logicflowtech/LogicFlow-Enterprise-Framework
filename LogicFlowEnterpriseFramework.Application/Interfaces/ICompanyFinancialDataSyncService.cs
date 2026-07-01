using LogicFlowEnterpriseFramework.Application.DTOs;

namespace LogicFlowEnterpriseFramework.Application.Interfaces;

public interface ICompanyFinancialDataSyncService
{
    Task<SyncJobSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<CompanyFinancialDataSyncStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<CompanyFinancialDataSyncStatusResponse> RunSyncAsync(long? sourceCompanyId = null, CancellationToken cancellationToken = default);
}
