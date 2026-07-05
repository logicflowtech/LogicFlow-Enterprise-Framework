using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Domain.Entities;
using LogicFlowEnterpriseFramework.Domain.Interfaces;
using LogicFlowEnterpriseFramework.Infrastructure.Identity;
using LogicFlowEnterpriseFramework.Infrastructure.Notifications;
using LogicFlowEnterpriseFramework.Infrastructure.Persistence;
using LogicFlowEnterpriseFramework.Infrastructure.Repositories;
using LogicFlowEnterpriseFramework.Infrastructure.Services;
using LogicFlowEnterpriseFramework.Shared.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogicFlowEnterpriseFramework.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BootstrapAdminOptions>(options =>
        {
            var section = configuration.GetSection(BootstrapAdminOptions.SectionName);
            options.Email = section["Email"] ?? "admin@logicflow.local";
            options.FullName = section["FullName"] ?? "System Administrator";
            options.InitialPassword = section["InitialPassword"] ?? string.Empty;
        });
        services.Configure<EmailTransportOptions>(options =>
        {
            var section = configuration.GetSection(EmailTransportOptions.SectionName);
            options.Provider = section["Provider"] ?? "Smtp";
            options.Host = section["Host"];
            options.Port = int.TryParse(section["Port"], out var port) ? port : 587;
            options.EnableSsl = bool.TryParse(section["EnableSsl"], out var enableSsl) ? enableSsl : true;
            options.UserName = section["UserName"];
            options.Password = section["Password"];
            options.DefaultFromAddress = section["DefaultFromAddress"];
            options.DefaultReplyToAddress = section["DefaultReplyToAddress"];
        });
        services.Configure<CompanyProfileSyncOptions>(options =>
        {
            var section = configuration.GetSection(CompanyProfileSyncOptions.SectionName);
            options.ScheduleEnabled = bool.TryParse(section["ScheduleEnabled"], out var scheduleEnabled) && scheduleEnabled;
            options.UseLocalSynonym = !bool.TryParse(section["UseLocalSynonym"], out var useLocalSynonym) || useLocalSynonym;
            options.LocalSynonymName = section["LocalSynonymName"] ?? "[dbo].[syn_Company]";
            options.SourceConnectionStringName = section["SourceConnectionStringName"] ?? "CompanyProfileSource";
            options.SourceConnectionString = section["SourceConnectionString"];
            options.SourceObjectName = section["SourceObjectName"] ?? "[dbo].[OSUSR_1sw_Company]";
            options.ScheduleMinutes = int.TryParse(section["ScheduleMinutes"], out var scheduleMinutes) ? scheduleMinutes : 60;
            options.BatchSize = int.TryParse(section["BatchSize"], out var batchSize) ? batchSize : 1000;
            options.CommandTimeoutSeconds = int.TryParse(section["CommandTimeoutSeconds"], out var commandTimeoutSeconds) ? commandTimeoutSeconds : 120;
        });
        services.Configure<AddressSyncOptions>(options =>
        {
            var section = configuration.GetSection(AddressSyncOptions.SectionName);
            options.SourceConnectionStringName = section["SourceConnectionStringName"] ?? "CompanyProfileSource";
            options.SourceConnectionString = section["SourceConnectionString"];
            options.SourceObjectName = section["SourceObjectName"] ?? "[dbo].[OSUSR_Z5Z_ADDRESS]";
            options.BatchSize = int.TryParse(section["BatchSize"], out var batchSize) ? batchSize : 1000;
            options.CommandTimeoutSeconds = int.TryParse(section["CommandTimeoutSeconds"], out var commandTimeoutSeconds) ? commandTimeoutSeconds : 120;
        });
        services.Configure<ReferenceDataSyncOptions>(options =>
        {
            var section = configuration.GetSection(ReferenceDataSyncOptions.SectionName);
            options.SourceConnectionStringName = section["SourceConnectionStringName"] ?? "CompanyProfileSource";
            options.SourceConnectionString = section["SourceConnectionString"];
            options.TitleSourceObjectName = section["TitleSourceObjectName"] ?? "[dbo].[OSUSR_P6Z_TITLE]";
            options.IdentificationTypeSourceObjectName = section["IdentificationTypeSourceObjectName"] ?? "[dbo].[OSUSR_P6Z_IDENTIFICATIONTYPE]";
            options.GeoSourceObjectName = section["GeoSourceObjectName"] ?? "[dbo].[OSUSR_Z5Z_ADDRESS]";
            options.BatchSize = int.TryParse(section["BatchSize"], out var batchSize) ? batchSize : 1000;
            options.CommandTimeoutSeconds = int.TryParse(section["CommandTimeoutSeconds"], out var commandTimeoutSeconds) ? commandTimeoutSeconds : 120;
            options.CountryMappings = section.GetSection("CountryMappings")
                .GetChildren()
                .Select(child => new ReferenceCountryMappingOptions
                {
                    MigratedId = long.TryParse(child["MigratedId"], out var migratedId) ? migratedId : 0,
                    Name = child["Name"] ?? string.Empty,
                    Code = child["Code"],
                    DisplayOrder = int.TryParse(child["DisplayOrder"], out var displayOrder) ? displayOrder : 0,
                    IsActive = !bool.TryParse(child["IsActive"], out var isActive) || isActive
                })
                .Where(item => item.MigratedId > 0)
                .ToList();
        });
        services.Configure<ApplicationLookupSyncOptions>(options =>
        {
            var section = configuration.GetSection(ApplicationLookupSyncOptions.SectionName);
            options.SourceConnectionStringName = section["SourceConnectionStringName"] ?? "CompanyProfileSource";
            options.SourceConnectionString = section["SourceConnectionString"];
            options.ApplicationCategorySourceObjectName = section["ApplicationCategorySourceObjectName"] ?? "[dbo].[OSUSR_D22_APPLICATIONCATEGORY]";
            options.ApplicationForSourceObjectName = section["ApplicationForSourceObjectName"] ?? "[dbo].[OSUSR_D22_APPLICATIONFOR]";
            options.ApplicationTypeSourceObjectName = section["ApplicationTypeSourceObjectName"] ?? "[dbo].[OSUSR_D22_APPLICATIONTYPE]";
            options.ApplicationStatusSourceObjectName = section["ApplicationStatusSourceObjectName"] ?? "[dbo].[OSUSR_D22_APPLICATIONSTATUS]";
            options.ApplicationForCategorySourceObjectName = section["ApplicationForCategorySourceObjectName"] ?? "[dbo].[OSUSR_D22_APPLICATIONFORCATEGORY]";
            options.ApplicationForApplicationTypeSourceObjectName = section["ApplicationForApplicationTypeSourceObjectName"] ?? "[dbo].[OSUSR_D22_APPLICATIONFORAPPLICATIONTYPE]";
            options.BatchSize = int.TryParse(section["BatchSize"], out var batchSize) ? batchSize : 1000;
            options.CommandTimeoutSeconds = int.TryParse(section["CommandTimeoutSeconds"], out var commandTimeoutSeconds) ? commandTimeoutSeconds : 120;
        });
        services.Configure<InvestMalaysiaAccessSyncOptions>(options =>
        {
            var section = configuration.GetSection(InvestMalaysiaAccessSyncOptions.SectionName);
            options.SourceConnectionStringName = section["SourceConnectionStringName"] ?? "CompanyProfileSource";
            options.SourceConnectionString = section["SourceConnectionString"];
            options.UserSourceObjectName = section["UserSourceObjectName"] ?? "[dbo].[OSSYS_USER]";
            options.GroupSourceObjectName = section["GroupSourceObjectName"] ?? "[dbo].[OSSYS_GROUP]";
            options.RoleSourceObjectName = section["RoleSourceObjectName"] ?? "[dbo].[OSSYS_ROLE]";
            options.GroupUserSourceObjectName = section["GroupUserSourceObjectName"] ?? "[dbo].[OSSYS_GROUP_USER]";
            options.GroupRoleSourceObjectName = section["GroupRoleSourceObjectName"] ?? "[dbo].[OSSYS_GROUP_ROLE]";
            options.UserRoleSourceObjectName = section["UserRoleSourceObjectName"] ?? "[dbo].[OSSYS_USER_ROLE]";
            options.ContactPersonSourceObjectName = section["ContactPersonSourceObjectName"] ?? "[dbo].[OSUSR_1sw_ContactPerson]";
            options.BatchSize = int.TryParse(section["BatchSize"], out var batchSize) ? batchSize : 1000;
            options.CommandTimeoutSeconds = int.TryParse(section["CommandTimeoutSeconds"], out var commandTimeoutSeconds) ? commandTimeoutSeconds : 120;
        });
        services.Configure<CompanyUserSyncOptions>(options =>
        {
            var section = configuration.GetSection(CompanyUserSyncOptions.SectionName);
            options.SourceConnectionStringName = section["SourceConnectionStringName"] ?? "CompanyProfileSource";
            options.SourceConnectionString = section["SourceConnectionString"];
            options.UserSourceObjectName = section["UserSourceObjectName"] ?? "[dbo].[OSSYS_USER]";
            options.GroupSourceObjectName = section["GroupSourceObjectName"] ?? "[dbo].[OSSYS_GROUP]";
            options.RoleSourceObjectName = section["RoleSourceObjectName"] ?? "[dbo].[OSSYS_ROLE]";
            options.GroupUserSourceObjectName = section["GroupUserSourceObjectName"] ?? "[dbo].[OSSYS_GROUP_USER]";
            options.GroupRoleSourceObjectName = section["GroupRoleSourceObjectName"] ?? "[dbo].[OSSYS_GROUP_ROLE]";
            options.UserRoleSourceObjectName = section["UserRoleSourceObjectName"] ?? "[dbo].[OSSYS_USER_ROLE]";
            options.ContactPersonSourceObjectName = section["ContactPersonSourceObjectName"] ?? "[dbo].[OSUSR_1sw_ContactPerson]";
            options.IndividualSourceObjectName = section["IndividualSourceObjectName"] ?? "[dbo].[OSUSR_p6z_Individual]";
            options.BatchSize = int.TryParse(section["BatchSize"], out var batchSize) ? batchSize : 1000;
            options.CommandTimeoutSeconds = int.TryParse(section["CommandTimeoutSeconds"], out var commandTimeoutSeconds) ? commandTimeoutSeconds : 120;
        });
        services.Configure<CompanyRelatedDataSyncOptions>(options =>
        {
            var section = configuration.GetSection(CompanyRelatedDataSyncOptions.SectionName);
            options.SourceConnectionStringName = section["SourceConnectionStringName"] ?? "CompanyProfileSource";
            options.SourceConnectionString = section["SourceConnectionString"];
            options.AuthorizedPersonSourceObjectName = section["AuthorizedPersonSourceObjectName"] ?? "[dbo].[OSUSR_1sw_AuthorizedPerson]";
            options.BoardDirectorSourceObjectName = section["BoardDirectorSourceObjectName"] ?? "[dbo].[OSUSR_1sw_BoardOfDirector]";
            options.AttachmentDocumentSourceObjectName = section["AttachmentDocumentSourceObjectName"] ?? "[dbo].[OSUSR_1sw_CompanyAttachmentDocument]";
            options.BatchSize = int.TryParse(section["BatchSize"], out var batchSize) ? batchSize : 1000;
            options.CommandTimeoutSeconds = int.TryParse(section["CommandTimeoutSeconds"], out var commandTimeoutSeconds) ? commandTimeoutSeconds : 120;
        });
        services.Configure<CompanyFinancialDataSyncOptions>(options =>
        {
            var section = configuration.GetSection(CompanyFinancialDataSyncOptions.SectionName);
            options.SourceConnectionStringName = section["SourceConnectionStringName"] ?? "CompanyProfileSource";
            options.SourceConnectionString = section["SourceConnectionString"];
            options.ProjectSourceObjectName = section["ProjectSourceObjectName"] ?? "[dbo].[OSUSR_LPP_PROJECT]";
            options.ProjectFinancingSourceObjectName = section["ProjectFinancingSourceObjectName"] ?? "[dbo].[OSUSR_LPP_PROJECTFINANCING]";
            options.FinancingStructureSourceObjectName = section["FinancingStructureSourceObjectName"] ?? "[dbo].[OSUSR_LPP_FINANCINGSTRUCTURE]";
            options.AuthorizedCapitalSourceObjectName = section["AuthorizedCapitalSourceObjectName"] ?? "[dbo].[OSUSR_LPP_COMPFIN_AUTHORIZEDCAPITAL1]";
            options.EquityStructureSourceObjectName = section["EquityStructureSourceObjectName"] ?? "[dbo].[OSUSR_LPP_COMPFIN_EQUITYSTRUCTURE]";
            options.FinancialPerformanceSourceObjectName = section["FinancialPerformanceSourceObjectName"] ?? "[dbo].[OSUSR_LPP_COMPFIN_FINANCIALPERFORMANCERECORD]";
            options.PaidUpCapitalSourceObjectName = section["PaidUpCapitalSourceObjectName"] ?? "[dbo].[OSUSR_LPP_COMPFIN_PUC_PAIDUPCAPITAL]";
            options.MalaysianIndividualsSourceObjectName = section["MalaysianIndividualsSourceObjectName"] ?? "[dbo].[OSUSR_LPP_COMPFIN_PUC_MALAYSIANINDIVIDUALS]";
            options.ForeignCompanySourceObjectName = section["ForeignCompanySourceObjectName"] ?? "[dbo].[OSUSR_LPP_COMPFIN_PUC_FOREIGNCOMPANY]";
            options.CompanyMalaysiaSourceObjectName = section["CompanyMalaysiaSourceObjectName"] ?? "[dbo].[OSUSR_LPP_COMPFIN_PUC_COMPANYMALAYSIA]";
            options.LoanSourceObjectName = section["LoanSourceObjectName"] ?? "[dbo].[OSUSR_LPP_COMPFIN_LOAN]";
            options.LoanForeignSourceObjectName = section["LoanForeignSourceObjectName"] ?? "[dbo].[OSUSR_LPP_COMPFIN_LOAN_FOREIGN]";
            options.TotalFinancingSourceObjectName = section["TotalFinancingSourceObjectName"] ?? "[dbo].[OSUSR_LPP_COMPFIN_TOTALFINANCING]";
            options.OtherSourcesSourceObjectName = section["OtherSourcesSourceObjectName"] ?? "[dbo].[OSUSR_LPP_COMPFIN_OTHERSOURCES]";
            options.BatchSize = int.TryParse(section["BatchSize"], out var batchSize) ? batchSize : 1000;
            options.CommandTimeoutSeconds = int.TryParse(section["CommandTimeoutSeconds"], out var commandTimeoutSeconds) ? commandTimeoutSeconds : 120;
        });
        services.Configure<JwtOptions>(options =>
        {
            var section = configuration.GetSection(JwtOptions.SectionName);
            options.Issuer = section["Issuer"] ?? string.Empty;
            options.Audience = section["Audience"] ?? string.Empty;
            options.Secret = section["Secret"] ?? string.Empty;
            options.AccessTokenMinutes = int.TryParse(section["AccessTokenMinutes"], out var accessMinutes) ? accessMinutes : 60;
            options.RefreshTokenDays = int.TryParse(section["RefreshTokenDays"], out var refreshDays) ? refreshDays : 7;
        });

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ServiceCenterAccessService>();
        services.AddScoped<IAccessManagementService>(provider => provider.GetRequiredService<ServiceCenterAccessService>());
        services.AddScoped<IEmailConfigurationService, EmailConfigurationService>();
        services.AddScoped<IAddressSyncService, AddressSyncService>();
        services.AddScoped<ICompanyProfileSyncService, CompanyProfileSyncService>();
        services.AddScoped<ICompanyUserSyncService, CompanyUserSyncService>();
        services.AddScoped<ICompanyRelatedDataSyncService, CompanyRelatedDataSyncService>();
        services.AddScoped<ICompanyFinancialDataSyncService, CompanyFinancialDataSyncService>();
        services.AddScoped<IReferenceDataSyncService, ReferenceDataSyncService>();
        services.AddScoped<IApplicationLookupSyncService, ApplicationLookupSyncService>();
        services.AddScoped<IInvestMalaysiaAccessService, InvestMalaysiaAccessService>();
        services.AddScoped<ISyncCatalogService, SyncCatalogService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddHostedService<CompanyProfileSyncHostedService>();

        return services;
    }
}
