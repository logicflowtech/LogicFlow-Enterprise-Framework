using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Domain.Entities;
using LogicFlowEnterpriseFramework.Infrastructure.Persistence;
using LogicFlowEnterpriseFramework.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LogicFlowEnterpriseFramework.Infrastructure.Identity;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ApplicationDbContext dbContext,
    IOptions<JwtOptions> jwtOptions)
    : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = request.TenantId ?? await dbContext.Tenants
            .Where(x => x.Identifier == "default")
            .Select(x => x.Id)
            .FirstAsync(cancellationToken);

        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            FullName = request.FullName,
            TenantId = tenantId,
            EmailConfirmed = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "registration"
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }

        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            throw new UnauthorizedAccessException("Account is temporarily locked.");
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return await CreateAuthResponseAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var existingToken = await dbContext.RefreshTokens
            .Include(x => x.User)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Token == HashRefreshToken(request.RefreshToken), cancellationToken);

        if (existingToken is null || !existingToken.IsActive || !existingToken.User.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        existingToken.RevokedAt = DateTimeOffset.UtcNow;
        var replacementToken = GenerateRefreshToken();
        existingToken.ReplacedByToken = HashRefreshToken(replacementToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await CreateAuthResponseAsync(existingToken.User, cancellationToken, replacementToken);
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User not found.");

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await GetPermissionsAsync(user);
        var featureCodes = await GetFeatureCodesAsync(user, cancellationToken);
        return new UserProfileResponse(user.Id, user.Email ?? string.Empty, user.FullName, user.TenantId, roles.ToArray(), permissions, featureCodes);
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(ApplicationUser user, CancellationToken cancellationToken, string? refreshTokenValue = null)
    {
        var permissions = await GetPermissionsAsync(user);
        var roles = await userManager.GetRolesAsync(user);
        var featureCodes = await GetFeatureCodesAsync(user, cancellationToken);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var accessToken = CreateAccessToken(user, roles, permissions, expiresAt);
        refreshTokenValue ??= GenerateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Token = HashRefreshToken(refreshTokenValue),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var profile = new UserProfileResponse(user.Id, user.Email ?? string.Empty, user.FullName, user.TenantId, roles.ToArray(), permissions, featureCodes);
        return new AuthResponse(accessToken, refreshTokenValue, expiresAt, profile);
    }

    private string CreateAccessToken(ApplicationUser user, IEnumerable<string> roles, IEnumerable<string> permissions, DateTimeOffset expiresAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
            new(AuthConstants.TenantClaimType, user.TenantId.ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim(AuthConstants.PermissionClaimType, permission)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_jwtOptions.Issuer, _jwtOptions.Audience, claims, expires: expiresAt.UtcDateTime, signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<IReadOnlyCollection<string>> GetPermissionsAsync(ApplicationUser user)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var platformPermissionCodes = await GetPlatformPermissionCodesAsync(user.Id);

        foreach (var permissionCode in platformPermissionCodes)
        {
            permissions.Add(permissionCode);
        }

        var roles = await userManager.GetRolesAsync(user);
        foreach (var roleName in roles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            var claims = await roleManager.GetClaimsAsync(role);
            foreach (var claim in claims.Where(x => x.Type == AuthConstants.PermissionClaimType))
            {
                permissions.Add(claim.Value);
            }
        }

        var hasAssignedCompany = await dbContext.CompanyProfileUserAssignments
            .AsNoTracking()
            .AnyAsync(assignment => assignment.ApplicationUserId == user.Id && assignment.IsActive && !assignment.IsDeleted);

        if (hasAssignedCompany)
        {
            permissions.Add(Permissions.CompanyProfilesRead);
            permissions.Add(Permissions.ApplicantDashboardRead);
            permissions.Add(Permissions.ApplicantTasksRead);
            permissions.Add(Permissions.ApplicantApplicationsRead);
            permissions.Add(Permissions.ApplicantCompanyProfileRead);
        }

        return permissions;
    }

    private async Task<IReadOnlyCollection<string>> GetFeatureCodesAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var platformPermissionCodes = await GetPlatformPermissionCodesAsync(user.Id);
        return platformPermissionCodes
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<IReadOnlyCollection<string>> GetPlatformPermissionCodesAsync(Guid userId)
    {
        var directGroupFeatureCodes = await dbContext.UserAccessGroupAssignments
            .AsNoTracking()
            .Where(x => x.ApplicationUserId == userId && x.IsEnabled && x.PlatformAccessGroup.IsActive)
            .SelectMany(x => x.PlatformAccessGroup.GroupFeatures
                .Where(link => link.IsEnabled && link.PlatformFeature.IsActive && !link.PlatformFeature.IsDeprecated)
                .Select(link => link.PlatformFeature.Code))
            .ToArrayAsync();

        var roleFeatureCodes = await dbContext.UserAccessGroupAssignments
            .AsNoTracking()
            .Where(x => x.ApplicationUserId == userId && x.IsEnabled && x.PlatformAccessGroup.IsActive)
            .SelectMany(x => x.PlatformAccessGroup.GroupRoles
                .Where(roleLink => roleLink.IsEnabled && roleLink.PlatformAccessRole.IsActive)
                .SelectMany(roleLink => roleLink.PlatformAccessRole.RoleFeatures
                    .Where(featureLink => featureLink.IsEnabled && featureLink.PlatformFeature.IsActive && !featureLink.PlatformFeature.IsDeprecated)
                    .Select(featureLink => featureLink.PlatformFeature.Code)))
            .ToArrayAsync();

        IReadOnlyCollection<string> permissionCodes = directGroupFeatureCodes
            .Concat(roleFeatureCodes)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return permissionCodes;
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    private static string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }
}
