namespace LogicFlowEnterpriseFramework.Application.DTOs;

public sealed record CreateServiceCenterUserRequest(
    string Email,
    string FullName,
    string Password,
    IReadOnlyCollection<Guid> RoleIds,
    bool IsAccessEnabled,
    IReadOnlyCollection<ServiceCenterEscalationAssignmentRequest> Escalations);
