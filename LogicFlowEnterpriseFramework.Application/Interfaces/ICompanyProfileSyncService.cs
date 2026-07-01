using LogicFlowEnterpriseFramework.Application.DTOs;

namespace LogicFlowEnterpriseFramework.Application.Interfaces;

public interface ICompanyProfileSyncService
{
    Task<CompanyProfileSyncStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<CompanyProfileSyncStatusResponse> RunSyncAsync(CancellationToken cancellationToken = default);
}
