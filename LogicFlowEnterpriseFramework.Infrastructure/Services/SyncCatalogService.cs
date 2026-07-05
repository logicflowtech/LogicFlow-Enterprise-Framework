using LogicFlowEnterpriseFramework.Application.DTOs;
using LogicFlowEnterpriseFramework.Application.Interfaces;

namespace LogicFlowEnterpriseFramework.Infrastructure.Services;

public sealed class SyncCatalogService(
    ICompanyProfileSyncService companyProfileSyncService,
    ICompanyUserSyncService companyUserSyncService,
    ICompanyRelatedDataSyncService companyRelatedDataSyncService,
    ICompanyFinancialDataSyncService companyFinancialDataSyncService,
    IReferenceDataSyncService referenceDataSyncService,
    IApplicationLookupSyncService applicationLookupSyncService,
    IAddressSyncService addressSyncService,
    IInvestMalaysiaAccessService investMalaysiaAccessService) : ISyncCatalogService
{
    public async Task<IReadOnlyList<SyncJobSummaryResponse>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var companyStatus = await companyProfileSyncService.GetStatusAsync(cancellationToken);
        var companyUserJob = await companyUserSyncService.GetSummaryAsync(cancellationToken);
        var companyRelatedDataJob = await companyRelatedDataSyncService.GetSummaryAsync(cancellationToken);
        var companyFinancialDataJob = await companyFinancialDataSyncService.GetSummaryAsync(cancellationToken);
        var referenceJobs = await referenceDataSyncService.GetCatalogAsync(cancellationToken);
        var applicationLookupJobs = await applicationLookupSyncService.GetCatalogAsync(cancellationToken);
        var addressJob = await addressSyncService.GetSummaryAsync(cancellationToken);
        var investMalaysiaJob = await investMalaysiaAccessService.GetSyncSummaryAsync(cancellationToken);

        List<SyncJobSummaryResponse> result =
        [
            new(
                "company-profiles",
                "Company Profile Sync",
                "Incremental sync of company records into the local application cache.",
                companyStatus.SourceObjectName,
                "[dbo].[CompanyProfiles]",
                companyStatus.ScheduleEnabled,
                companyStatus.ScheduleMinutes,
                companyStatus.BatchSize,
                companyStatus.UseLocalSynonym,
                companyStatus.SourceConnectionStringName,
                companyStatus.SourceConnectionConfigured,
                companyStatus.LocalRowCount,
                companyStatus.LastStartedAt,
                companyStatus.LastCompletedAt,
                companyStatus.LastRunSucceeded,
                companyStatus.LastProcessedRows,
                companyStatus.LastRunMessage,
                "/configuration/company-profiles")
        ];

        result.Add(companyUserJob);
        result.Add(companyRelatedDataJob);
        result.Add(companyFinancialDataJob);
        result.AddRange(referenceJobs);
        result.AddRange(applicationLookupJobs);
        result.Add(addressJob);
        result.Add(investMalaysiaJob);
        return result;
    }

    public async Task<SyncJobSummaryResponse> RunAsync(string syncKey, CancellationToken cancellationToken = default)
    {
        return syncKey switch
        {
            "company-profiles" => ToSummary(await companyProfileSyncService.RunSyncAsync(cancellationToken)),
            "company-users" => await RunCompanyUserSyncAsync(cancellationToken),
            "company-related-data" => await RunCompanyRelatedDataSyncAsync(cancellationToken),
            "company-financial-data" => await RunCompanyFinancialDataSyncAsync(cancellationToken),
            "addresses" => await RunAddressSyncAsync(cancellationToken),
            "invest-malaysia-access" => await investMalaysiaAccessService.RunSyncAsync(cancellationToken),
            "application-categories" or "application-fors" or "application-types" or "application-statuses" or "application-category-fors" or "application-for-types"
                => await applicationLookupSyncService.RunAsync(syncKey, cancellationToken),
            _ => await referenceDataSyncService.RunAsync(syncKey, cancellationToken)
        };
    }

    private async Task<SyncJobSummaryResponse> RunCompanyUserSyncAsync(CancellationToken cancellationToken)
    {
        await companyUserSyncService.RunSyncAsync(cancellationToken: cancellationToken);
        return await companyUserSyncService.GetSummaryAsync(cancellationToken);
    }

    private async Task<SyncJobSummaryResponse> RunCompanyRelatedDataSyncAsync(CancellationToken cancellationToken)
    {
        await companyRelatedDataSyncService.RunSyncAsync(cancellationToken: cancellationToken);
        return await companyRelatedDataSyncService.GetSummaryAsync(cancellationToken);
    }

    private async Task<SyncJobSummaryResponse> RunCompanyFinancialDataSyncAsync(CancellationToken cancellationToken)
    {
        await companyFinancialDataSyncService.RunSyncAsync(cancellationToken: cancellationToken);
        return await companyFinancialDataSyncService.GetSummaryAsync(cancellationToken);
    }

    private async Task<SyncJobSummaryResponse> RunAddressSyncAsync(CancellationToken cancellationToken)
    {
        await addressSyncService.RunSyncAsync(cancellationToken);
        return await addressSyncService.GetSummaryAsync(cancellationToken);
    }

    private static SyncJobSummaryResponse ToSummary(CompanyProfileSyncStatusResponse status)
    {
        return new SyncJobSummaryResponse(
            "company-profiles",
            "Company Profile Sync",
            "Incremental sync of company records into the local application cache.",
            status.SourceObjectName,
            "[dbo].[CompanyProfiles]",
            status.ScheduleEnabled,
            status.ScheduleMinutes,
            status.BatchSize,
            status.UseLocalSynonym,
            status.SourceConnectionStringName,
            status.SourceConnectionConfigured,
            status.LocalRowCount,
            status.LastStartedAt,
            status.LastCompletedAt,
            status.LastRunSucceeded,
            status.LastProcessedRows,
            status.LastRunMessage,
            "/configuration/company-profiles");
    }
}
