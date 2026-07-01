using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Net.Http.Headers;
using LogicFlowEnterpriseFramework.Blazor.Models;

namespace LogicFlowEnterpriseFramework.Blazor.Services;

public sealed class LogicFlowApiClient(HttpClient httpClient, AuthSession authSession)
{
    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/auth/register", request, cancellationToken);
        return await ReadResponseAsync<AuthResponse>(response, cancellationToken);
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/auth/login", request, cancellationToken);
        return await ReadResponseAsync<AuthResponse>(response, cancellationToken);
    }

    public async Task<ApiResponse<UserProfile>> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<UserProfile>(response, cancellationToken);
    }

    public async Task<ApiResponse<List<ServiceCenterAccessUserSummary>>> GetAccessUsersAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/service-center/access/users");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<List<ServiceCenterAccessUserSummary>>(response, cancellationToken);
    }

    public async Task<ApiResponse<List<RoleOption>>> GetAccessRolesAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/service-center/access/roles");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<List<RoleOption>>(response, cancellationToken);
    }

    public async Task<ApiResponse<List<PlatformFeatureCatalogItemModel>>> GetAccessFeatureCatalogAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/service-center/access/catalog/features");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<List<PlatformFeatureCatalogItemModel>>(response, cancellationToken);
    }

    public async Task<ApiResponse<PlatformFeatureCatalogItemModel>> CreateAccessFeatureAsync(CreatePlatformFeatureRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/service-center/access/catalog/features");
        AddBearerToken(request);
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<PlatformFeatureCatalogItemModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<PlatformFeatureCatalogItemModel>> UpdateAccessFeatureAsync(Guid featureId, UpdatePlatformFeatureRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/service-center/access/catalog/features/{featureId}");
        AddBearerToken(request);
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<PlatformFeatureCatalogItemModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<object>> DeleteAccessFeatureAsync(Guid featureId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/service-center/access/catalog/features/{featureId}");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<object>(response, cancellationToken);
    }

    public async Task<ApiResponse<List<AccessRoleCatalogItemModel>>> GetAccessRolesCatalogAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/service-center/access/catalog/access-roles");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<List<AccessRoleCatalogItemModel>>(response, cancellationToken);
    }

    public async Task<ApiResponse<AccessRoleCatalogItemModel>> CreateAccessRoleAsync(CreateAccessRoleRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/service-center/access/catalog/access-roles");
        AddBearerToken(request);
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<AccessRoleCatalogItemModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<AccessRoleCatalogItemModel>> UpdateAccessRoleAsync(Guid roleId, UpdateAccessRoleRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/service-center/access/catalog/access-roles/{roleId}");
        AddBearerToken(request);
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<AccessRoleCatalogItemModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<object>> DeleteAccessRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/service-center/access/catalog/access-roles/{roleId}");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<object>(response, cancellationToken);
    }

    public async Task<ApiResponse<List<AccessGroupCatalogItemModel>>> GetAccessGroupsCatalogAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/service-center/access/catalog/groups");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<List<AccessGroupCatalogItemModel>>(response, cancellationToken);
    }

    public async Task<ApiResponse<List<InvestMalaysiaGroupCatalogItemModel>>> GetInvestMalaysiaAccessGroupsAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/invest-malaysia/access/groups");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<List<InvestMalaysiaGroupCatalogItemModel>>(response, cancellationToken);
    }

    public async Task<ApiResponse<InvestMalaysiaGroupCatalogItemModel>> CreateInvestMalaysiaGroupMappingAsync(CreateInvestMalaysiaGroupMappingRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/platform/invest-malaysia/access/group-mappings");
        AddBearerToken(request);
        request.Content = JsonContent.Create(new
        {
            investMalaysiaGroupName = requestModel.InvestMalaysiaGroupName,
            platformAccessGroupId = requestModel.PlatformAccessGroupId,
            isActive = requestModel.IsActive
        });
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<InvestMalaysiaGroupCatalogItemModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<InvestMalaysiaGroupCatalogItemModel>> UpdateInvestMalaysiaGroupMappingAsync(Guid mappingId, UpdateInvestMalaysiaGroupMappingRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/platform/invest-malaysia/access/group-mappings/{mappingId}");
        AddBearerToken(request);
        request.Content = JsonContent.Create(new
        {
            investMalaysiaGroupName = requestModel.InvestMalaysiaGroupName,
            platformAccessGroupId = requestModel.PlatformAccessGroupId,
            isActive = requestModel.IsActive
        });
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<InvestMalaysiaGroupCatalogItemModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<object>> DeleteInvestMalaysiaGroupMappingAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/platform/invest-malaysia/access/group-mappings/{mappingId}");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<object>(response, cancellationToken);
    }

    public async Task<ApiResponse<AccessGroupCatalogItemModel>> CreateAccessGroupAsync(CreateAccessGroupRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/service-center/access/catalog/groups");
        AddBearerToken(request);
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<AccessGroupCatalogItemModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<AccessGroupCatalogItemModel>> UpdateAccessGroupAsync(Guid groupId, UpdateAccessGroupRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/service-center/access/catalog/groups/{groupId}");
        AddBearerToken(request);
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<AccessGroupCatalogItemModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<object>> DeleteAccessGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/service-center/access/catalog/groups/{groupId}");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<object>(response, cancellationToken);
    }

    public async Task<ApiResponse<List<PermissionCatalogItemModel>>> GetAccessPermissionCatalogAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/service-center/access/catalog/permissions");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<List<PermissionCatalogItemModel>>(response, cancellationToken);
    }

    public async Task<ApiResponse<List<SecurityRoleCatalogItemModel>>> GetSecurityRolesCatalogAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/service-center/access/catalog/security-roles");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<List<SecurityRoleCatalogItemModel>>(response, cancellationToken);
    }

    public async Task<ApiResponse<SecurityRoleCatalogItemModel>> CreateSecurityRoleAsync(CreateSecurityRoleRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/service-center/access/catalog/security-roles");
        AddBearerToken(request);
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<SecurityRoleCatalogItemModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<object>> DeleteSecurityRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/service-center/access/catalog/security-roles/{roleId}");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<object>(response, cancellationToken);
    }

    public async Task<ApiResponse<ServiceCenterAccessDetail>> GetAccessUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/service-center/access/users/{userId}");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<ServiceCenterAccessDetail>(response, cancellationToken);
    }

    public async Task<ApiResponse<ServiceCenterAccessDetail>> CreateAccessUserAsync(ServiceCenterCreateUserRequest requestModel, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/service-center/access/users");
        AddBearerToken(request);
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<ServiceCenterAccessDetail>(response, cancellationToken);
    }

    public async Task<ApiResponse<ServiceCenterAccessDetail>> UpdateAccessUserAsync(Guid userId, ServiceCenterAccessUpdateRequest requestModel, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/service-center/access/users/{userId}");
        AddBearerToken(request);
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<ServiceCenterAccessDetail>(response, cancellationToken);
    }

    public async Task<ApiResponse<object>> DeleteAccessUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/service-center/access/users/{userId}");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<object>(response, cancellationToken);
    }

    public async Task<ApiResponse<EmailTransportConfigurationModel>> GetEmailConfigurationAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/service-center/configuration/email");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<EmailTransportConfigurationModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<EmailTransportConfigurationModel>> SaveEmailConfigurationAsync(EmailTransportConfigurationModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/service-center/configuration/email");
        AddBearerToken(request);
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<EmailTransportConfigurationModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<TestEmailResponseModel>> SendTestEmailAsync(TestEmailRequestModel requestModel, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/service-center/configuration/email/test");
        AddBearerToken(request);
        request.Content = JsonContent.Create(requestModel);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<TestEmailResponseModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<List<SyncJobSummaryModel>>> GetSyncJobsAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/sync-jobs");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<List<SyncJobSummaryModel>>(response, cancellationToken);
    }

    public async Task<ApiResponse<SyncJobSummaryModel>> RunSyncJobAsync(string syncKey, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/sync-jobs/{Uri.EscapeDataString(syncKey)}/run");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<SyncJobSummaryModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<CompanyProfileListModel>> GetCompanyProfilesAsync(int pageNumber = 1, int pageSize = 25, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = $"/api/platform/company-profiles?pageNumber={pageNumber}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, query);
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<CompanyProfileListModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<CompanyProfileDetailModel>> GetCompanyProfileAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/platform/company-profiles/{id}");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<CompanyProfileDetailModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<CompanyProfileIrpmModel>> GetCompanyProfileIrpmAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/platform/company-profiles/{id}/irpm");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<CompanyProfileIrpmModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<CompanyProfileFinancialDetailsModel>> GetCompanyProfileFinancialDetailsAsync(Guid id, Guid? financialDetailId = null, CancellationToken cancellationToken = default)
    {
        var path = financialDetailId.HasValue
            ? $"/api/platform/company-profiles/{id}/financial-details?financialDetailId={financialDetailId.Value}"
            : $"/api/platform/company-profiles/{id}/financial-details";

        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<CompanyProfileFinancialDetailsModel>(response, cancellationToken);
    }

    public async Task<DownloadFilePayload> DownloadCompanyProfileDocumentAsync(Guid companyId, long migratedDocumentId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/platform/company-profiles/{companyId}/documents/{migratedDocumentId}/download");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = response.Content is null
                ? $"Download failed with HTTP {(int)response.StatusCode}."
                : await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(message) ? $"Download failed with HTTP {(int)response.StatusCode}." : message);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? $"document-{migratedDocumentId}";
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

        return new DownloadFilePayload(
            fileName.Trim('"'),
            contentType,
            bytes);
    }

    public async Task<ApiResponse<CompanyProfileSyncStatusModel>> GetCompanyProfileSyncStatusAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/company-profiles/sync/status");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<CompanyProfileSyncStatusModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<CompanyProfileSyncStatusModel>> RunCompanyProfileSyncAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/platform/company-profiles/sync/run");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<CompanyProfileSyncStatusModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<CompanyUserSyncStatusModel>> GetCompanyUserSyncStatusAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/company-users/sync/status");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<CompanyUserSyncStatusModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<CompanyUserSyncStatusModel>> RunCompanyUserSyncAsync(long sourceCompanyId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/company-users/sync/run?sourceCompanyId={sourceCompanyId}");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<CompanyUserSyncStatusModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<CompanyRelatedDataSyncStatusModel>> GetCompanyRelatedDataSyncStatusAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/platform/company-related-data/sync/status");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<CompanyRelatedDataSyncStatusModel>(response, cancellationToken);
    }

    public async Task<ApiResponse<CompanyRelatedDataSyncStatusModel>> RunCompanyRelatedDataSyncAsync(long sourceCompanyId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/platform/company-related-data/sync/run?sourceCompanyId={sourceCompanyId}");
        AddBearerToken(request);
        var response = await httpClient.SendAsync(request, cancellationToken);
        return await ReadResponseAsync<CompanyRelatedDataSyncStatusModel>(response, cancellationToken);
    }

    private void AddBearerToken(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(authSession.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authSession.AccessToken);
        }
    }

    private static async Task<ApiResponse<T>> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = response.Content is null
            ? string.Empty
            : await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(content))
        {
            return new ApiResponse<T>
            {
                Succeeded = false,
                Message = $"API returned HTTP {(int)response.StatusCode} with an empty response body."
            };
        }

        try
        {
            var apiResponse = JsonSerializer.Deserialize<ApiResponse<T>>(content);
            if (apiResponse is not null)
            {
                return apiResponse;
            }
        }
        catch (JsonException)
        {
            var preview = content.Length > 220 ? $"{content[..220]}..." : content;

            return new ApiResponse<T>
            {
                Succeeded = false,
                Message = $"API returned HTTP {(int)response.StatusCode} with a non-JSON response: {preview}"
            };
        }

        return new ApiResponse<T>
        {
            Succeeded = false,
            Message = $"API returned HTTP {(int)response.StatusCode}."
        };
    }
}
