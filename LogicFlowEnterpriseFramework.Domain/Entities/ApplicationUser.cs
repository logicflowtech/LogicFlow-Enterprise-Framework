using Microsoft.AspNetCore.Identity;

namespace LogicFlowEnterpriseFramework.Domain.Entities;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public int? LegacyUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public UserProfile? UserProfile { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<EscalationAssignment> EscalationAssignments { get; set; } = new List<EscalationAssignment>();
    public ICollection<ServiceCenterUserAccess> ServiceCenterAccesses { get; set; } = new List<ServiceCenterUserAccess>();
    public ICollection<UserAccessGroupAssignment> AccessGroupAssignments { get; set; } = new List<UserAccessGroupAssignment>();
    public ICollection<CompanyProfileUserAssignment> CompanyAssignments { get; set; } = new List<CompanyProfileUserAssignment>();
}
