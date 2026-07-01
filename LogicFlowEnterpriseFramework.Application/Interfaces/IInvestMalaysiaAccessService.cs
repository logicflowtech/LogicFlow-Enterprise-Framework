using LogicFlowEnterpriseFramework.Application.DTOs;

namespace LogicFlowEnterpriseFramework.Application.Interfaces;

public interface IInvestMalaysiaAccessService
{
    Task<SyncJobSummaryResponse> GetSyncSummaryAsync(CancellationToken cancellationToken = default);
    Task<SyncJobSummaryResponse> RunSyncAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InvestMalaysiaGroupCatalogResponse>> GetGroupCatalogAsync(CancellationToken cancellationToken = default);
    Task<InvestMalaysiaGroupCatalogResponse> CreateGroupMappingAsync(CreateInvestMalaysiaGroupMappingRequest request, CancellationToken cancellationToken = default);
    Task<InvestMalaysiaGroupCatalogResponse> UpdateGroupMappingAsync(Guid mappingId, UpdateInvestMalaysiaGroupMappingRequest request, CancellationToken cancellationToken = default);
    Task DeleteGroupMappingAsync(Guid mappingId, CancellationToken cancellationToken = default);
}
