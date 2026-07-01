namespace LogicFlowEnterpriseFramework.Application.DTOs;

public sealed record InvestMalaysiaGroupCatalogResponse(
    Guid? MappingId,
    int? LegacyGroupId,
    string InvestMalaysiaGroupName,
    int UserCount,
    int RoleCount,
    bool IsDiscoveredFromSync,
    Guid? PlatformAccessGroupId,
    string? PlatformAccessGroupCode,
    string? PlatformAccessGroupName,
    bool IsMapped,
    bool IsActive,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);

public sealed record CreateInvestMalaysiaGroupMappingRequest(
    string InvestMalaysiaGroupName,
    Guid PlatformAccessGroupId,
    bool IsActive);

public sealed record UpdateInvestMalaysiaGroupMappingRequest(
    string InvestMalaysiaGroupName,
    Guid PlatformAccessGroupId,
    bool IsActive);
