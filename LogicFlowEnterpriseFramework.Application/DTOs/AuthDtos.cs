namespace LogicFlowEnterpriseFramework.Application.DTOs;

public sealed record RegisterRequest(string Email, string Password, string FullName, Guid? TenantId);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt, UserProfileResponse User);
public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string FullName,
    Guid TenantId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions,
    IReadOnlyCollection<string> FeatureCodes);
