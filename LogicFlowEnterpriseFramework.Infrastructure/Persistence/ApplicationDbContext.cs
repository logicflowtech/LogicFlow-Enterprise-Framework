using LogicFlowEnterpriseFramework.Application.Interfaces;
using LogicFlowEnterpriseFramework.Domain.Entities;
using LogicFlowEnterpriseFramework.Domain.Entities.Workflow;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentUserService currentUserService,
    ITenantProvider tenantProvider)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PlatformFeature> PlatformFeatures => Set<PlatformFeature>();
    public DbSet<PlatformAccessRole> PlatformAccessRoles => Set<PlatformAccessRole>();
    public DbSet<PlatformAccessGroup> PlatformAccessGroups => Set<PlatformAccessGroup>();
    public DbSet<PlatformGroupFeature> PlatformGroupFeatures => Set<PlatformGroupFeature>();
    public DbSet<PlatformRoleFeature> PlatformRoleFeatures => Set<PlatformRoleFeature>();
    public DbSet<GroupAccessRoleAssignment> GroupAccessRoleAssignments => Set<GroupAccessRoleAssignment>();
    public DbSet<UserAccessGroupAssignment> UserAccessGroupAssignments => Set<UserAccessGroupAssignment>();
    public DbSet<ServiceCenterUserAccess> ServiceCenterUserAccesses => Set<ServiceCenterUserAccess>();
    public DbSet<EscalationAssignment> EscalationAssignments => Set<EscalationAssignment>();
    public DbSet<EmailSettings> EmailSettings => Set<EmailSettings>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<LookupCountry> LookupCountries => Set<LookupCountry>();
    public DbSet<LookupState> LookupStates => Set<LookupState>();
    public DbSet<LookupCity> LookupCities => Set<LookupCity>();
    public DbSet<LookupTitle> LookupTitles => Set<LookupTitle>();
    public DbSet<LookupIdentificationType> LookupIdentificationTypes => Set<LookupIdentificationType>();
    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();
    public DbSet<CompanyAuthorizedPerson> CompanyAuthorizedPersons => Set<CompanyAuthorizedPerson>();
    public DbSet<CompanyBoardDirector> CompanyBoardDirectors => Set<CompanyBoardDirector>();
    public DbSet<CompanyAttachmentDocument> CompanyAttachmentDocuments => Set<CompanyAttachmentDocument>();
    public DbSet<CompanyProfileFinancialDetail> CompanyProfileFinancialDetails => Set<CompanyProfileFinancialDetail>();
    public DbSet<CompanyProfileAuthorizedCapital> CompanyProfileAuthorizedCapitals => Set<CompanyProfileAuthorizedCapital>();
    public DbSet<CompanyProfileEquityStructure> CompanyProfileEquityStructures => Set<CompanyProfileEquityStructure>();
    public DbSet<CompanyProfileFinancialPerformanceRecord> CompanyProfileFinancialPerformanceRecords => Set<CompanyProfileFinancialPerformanceRecord>();
    public DbSet<CompanyProfilePaidUpCapital> CompanyProfilePaidUpCapitals => Set<CompanyProfilePaidUpCapital>();
    public DbSet<CompanyProfilePaidUpCapitalMalaysianIndividuals> CompanyProfilePaidUpCapitalMalaysianIndividuals => Set<CompanyProfilePaidUpCapitalMalaysianIndividuals>();
    public DbSet<CompanyProfilePaidUpCapitalForeignCompany> CompanyProfilePaidUpCapitalForeignCompanies => Set<CompanyProfilePaidUpCapitalForeignCompany>();
    public DbSet<CompanyProfilePaidUpCapitalCompanyMalaysia> CompanyProfilePaidUpCapitalCompaniesMalaysia => Set<CompanyProfilePaidUpCapitalCompanyMalaysia>();
    public DbSet<CompanyProfileCompanyIncorporated> CompanyProfileCompanyIncorporatedEntries => Set<CompanyProfileCompanyIncorporated>();
    public DbSet<CompanyProfileCompanyIncorporatedCountry> CompanyProfileCompanyIncorporatedCountries => Set<CompanyProfileCompanyIncorporatedCountry>();
    public DbSet<CompanyProfileLoan> CompanyProfileLoans => Set<CompanyProfileLoan>();
    public DbSet<CompanyProfileLoanDomestic> CompanyProfileLoanDomestics => Set<CompanyProfileLoanDomestic>();
    public DbSet<CompanyProfileLoanForeign> CompanyProfileLoanForeigns => Set<CompanyProfileLoanForeign>();
    public DbSet<CompanyProfileTotalFinancing> CompanyProfileTotalFinancings => Set<CompanyProfileTotalFinancing>();
    public DbSet<CompanyProfileOtherSource> CompanyProfileOtherSources => Set<CompanyProfileOtherSource>();
    public DbSet<CompanyProfileUltimateParentHoldingCompany> CompanyProfileUltimateParentHoldingCompanies => Set<CompanyProfileUltimateParentHoldingCompany>();
    public DbSet<CompanyProfileSyncState> CompanyProfileSyncStates => Set<CompanyProfileSyncState>();
    public DbSet<InvestMalaysiaUser> InvestMalaysiaUsers => Set<InvestMalaysiaUser>();
    public DbSet<InvestMalaysiaGroup> InvestMalaysiaGroups => Set<InvestMalaysiaGroup>();
    public DbSet<InvestMalaysiaRole> InvestMalaysiaRoles => Set<InvestMalaysiaRole>();
    public DbSet<InvestMalaysiaGroupUser> InvestMalaysiaGroupUsers => Set<InvestMalaysiaGroupUser>();
    public DbSet<InvestMalaysiaGroupRole> InvestMalaysiaGroupRoles => Set<InvestMalaysiaGroupRole>();
    public DbSet<InvestMalaysiaUserRole> InvestMalaysiaUserRoles => Set<InvestMalaysiaUserRole>();
    public DbSet<InvestMalaysiaContactPerson> InvestMalaysiaContactPersons => Set<InvestMalaysiaContactPerson>();
    public DbSet<InvestMalaysiaGroupMapping> InvestMalaysiaGroupMappings => Set<InvestMalaysiaGroupMapping>();
    public DbSet<InvestMalaysiaUserSyncState> InvestMalaysiaUserSyncStates => Set<InvestMalaysiaUserSyncState>();
    public DbSet<CompanyProfileUserAssignment> CompanyProfileUserAssignments => Set<CompanyProfileUserAssignment>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowDraft> WorkflowDrafts => Set<WorkflowDraft>();
    public DbSet<WorkflowVersion> WorkflowVersions => Set<WorkflowVersion>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowInstanceNode> WorkflowInstanceNodes => Set<WorkflowInstanceNode>();
    public DbSet<WorkflowTask> WorkflowTasks => Set<WorkflowTask>();
    public DbSet<WorkflowTaskComment> WorkflowTaskComments => Set<WorkflowTaskComment>();
    public DbSet<WorkflowTaskAssignment> WorkflowTaskAssignments => Set<WorkflowTaskAssignment>();
    public DbSet<WorkflowVariable> WorkflowVariables => Set<WorkflowVariable>();
    public DbSet<WorkflowTimer> WorkflowTimers => Set<WorkflowTimer>();
    public DbSet<WorkflowEventSubscription> WorkflowEventSubscriptions => Set<WorkflowEventSubscription>();
    public DbSet<WorkflowExecutionLog> WorkflowExecutionLogs => Set<WorkflowExecutionLog>();
    public DbSet<WorkflowAuditLog> WorkflowAuditLogs => Set<WorkflowAuditLog>();
    public DbSet<WorkflowOutbox> WorkflowOutbox => Set<WorkflowOutbox>();

    private static void ConfigureFinancialChild<TEntity>(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> entity,
        string tableName,
        string financialDetailsIndexName,
        ITenantProvider tenantProvider)
        where TEntity : BaseEntity
    {
        entity.ToTable(tableName);
        entity.HasKey(x => x.Id);
        entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
        entity.Property("FinancialDetailsId");
        entity.Property<DateTime>("LastSyncedAt").HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        entity.Property<DateTime?>("SourceCreatedAt").HasColumnType("datetime2(3)");
        entity.Property<DateTime?>("SourceUpdatedAt").HasColumnType("datetime2(3)");
        entity.HasIndex("FinancialDetailsId").IsUnique().HasDatabaseName(financialDetailsIndexName);
        entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(x => x.Identifier).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Identifier).HasMaxLength(100).IsRequired();
            entity.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            entity.HasIndex(x => x.LegacyUserId).IsUnique().HasFilter("[LegacyUserId] IS NOT NULL");
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(256);
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.Property(x => x.Token).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.Token).IsUnique();
            entity.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<PlatformFeature>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.Category).HasMaxLength(100);
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<PlatformAccessGroup>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<PlatformAccessRole>(entity =>
        {
            entity.Property(x => x.Code).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<PlatformGroupFeature>(entity =>
        {
            entity.HasIndex(x => new { x.PlatformAccessGroupId, x.PlatformFeatureId }).IsUnique();
            entity.HasOne(x => x.PlatformAccessGroup).WithMany(x => x.GroupFeatures).HasForeignKey(x => x.PlatformAccessGroupId);
            entity.HasOne(x => x.PlatformFeature).WithMany(x => x.GroupFeatures).HasForeignKey(x => x.PlatformFeatureId).OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<PlatformRoleFeature>(entity =>
        {
            entity.HasIndex(x => new { x.PlatformAccessRoleId, x.PlatformFeatureId }).IsUnique();
            entity.HasOne(x => x.PlatformAccessRole).WithMany(x => x.RoleFeatures).HasForeignKey(x => x.PlatformAccessRoleId);
            entity.HasOne(x => x.PlatformFeature).WithMany(x => x.RoleFeatures).HasForeignKey(x => x.PlatformFeatureId).OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<GroupAccessRoleAssignment>(entity =>
        {
            entity.HasIndex(x => new { x.PlatformAccessGroupId, x.PlatformAccessRoleId }).IsUnique();
            entity.HasOne(x => x.PlatformAccessGroup).WithMany(x => x.GroupRoles).HasForeignKey(x => x.PlatformAccessGroupId);
            entity.HasOne(x => x.PlatformAccessRole).WithMany(x => x.GroupAssignments).HasForeignKey(x => x.PlatformAccessRoleId).OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<UserAccessGroupAssignment>(entity =>
        {
            entity.HasIndex(x => new { x.ApplicationUserId, x.PlatformAccessGroupId }).IsUnique();
            entity.HasOne(x => x.ApplicationUser).WithMany(x => x.AccessGroupAssignments).HasForeignKey(x => x.ApplicationUserId);
            entity.HasOne(x => x.PlatformAccessGroup).WithMany(x => x.UserAssignments).HasForeignKey(x => x.PlatformAccessGroupId);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<CompanyProfileUserAssignment>(entity =>
        {
            entity.ToTable("CompanyProfileUserAssignments");
            entity.HasIndex(x => new { x.ApplicationUserId, x.CompanyProfileId }).IsUnique();
            entity.HasOne(x => x.ApplicationUser)
                .WithMany(x => x.CompanyAssignments)
                .HasForeignKey(x => x.ApplicationUserId);
            entity.HasOne(x => x.CompanyProfile)
                .WithMany(x => x.UserAssignments)
                .HasForeignKey(x => x.CompanyProfileId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<ServiceCenterUserAccess>(entity =>
        {
            entity.HasIndex(x => x.ApplicationUserId).IsUnique();
            entity.HasOne(x => x.ApplicationUser).WithMany(x => x.ServiceCenterAccesses).HasForeignKey(x => x.ApplicationUserId);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<EscalationAssignment>(entity =>
        {
            entity.Property(x => x.Category).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Priority).HasMaxLength(50).IsRequired();
            entity.HasOne(x => x.ApplicationUser).WithMany(x => x.EscalationAssignments).HasForeignKey(x => x.ApplicationUserId);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<EmailSettings>(entity =>
        {
            entity.Property(x => x.Provider).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Host).HasMaxLength(256);
            entity.Property(x => x.UserName).HasMaxLength(256);
            entity.Property(x => x.EncryptedPassword).HasMaxLength(4000);
            entity.Property(x => x.DefaultFromAddress).HasColumnName("SenderEmail").HasMaxLength(256);
            entity.Property(x => x.DefaultReplyToAddress).HasColumnName("ReplyToEmail").HasMaxLength(256);
            entity.HasIndex(x => x.TenantId).IsUnique();
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("UserProfiles");
            entity.HasKey(x => x.ApplicationUserId);
            entity.Property(x => x.Nric).HasMaxLength(15);
            entity.Property(x => x.PassportNumber).HasMaxLength(50);
            entity.Property(x => x.CustomDesignationName).HasMaxLength(100);
            entity.Property(x => x.FaxNumber).HasMaxLength(100);
            entity.Property(x => x.TitleDisplayName).HasMaxLength(50);
            entity.Property(x => x.SourceCreatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceUpdatedAt).HasColumnType("datetime2(3)");
            entity.HasOne(x => x.ApplicationUser)
                .WithOne(x => x.UserProfile)
                .HasForeignKey<UserProfile>(x => x.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Address>()
                .WithMany()
                .HasForeignKey(x => x.AddressId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<Address>(entity =>
        {
            entity.ToTable("Addresses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.AddressLine1).HasMaxLength(200);
            entity.Property(x => x.AddressLine2).HasMaxLength(200);
            entity.Property(x => x.AddressLine3).HasMaxLength(200);
            entity.Property(x => x.CountryName).HasMaxLength(200);
            entity.Property(x => x.StateName).HasMaxLength(200);
            entity.Property(x => x.CityName).HasMaxLength(200);
            entity.Property(x => x.Postcode).HasMaxLength(100);
            entity.Property(x => x.SourceCreatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceUpdatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasFilter("[MigratedId] IS NOT NULL").HasDatabaseName("IX_Addresses_MigratedId");
            entity.HasOne<LookupCountry>().WithMany().HasForeignKey(x => x.CountryId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<LookupState>().WithMany().HasForeignKey(x => x.StateId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<LookupCity>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<LookupCountry>(entity =>
        {
            entity.ToTable("LookupCountries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.DisplayOrder).HasDefaultValue(0);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasFilter("[MigratedId] IS NOT NULL").HasDatabaseName("IX_LookupCountries_MigratedId");
        });

        builder.Entity<LookupState>(entity =>
        {
            entity.ToTable("LookupStates");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.DisplayOrder).HasDefaultValue(0);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasFilter("[MigratedId] IS NOT NULL").HasDatabaseName("IX_LookupStates_MigratedId");
            entity.HasOne<LookupCountry>().WithMany().HasForeignKey(x => x.CountryId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<LookupCity>(entity =>
        {
            entity.ToTable("LookupCities");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.DisplayOrder).HasDefaultValue(0);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasFilter("[MigratedId] IS NOT NULL").HasDatabaseName("IX_LookupCities_MigratedId");
            entity.HasOne<LookupCountry>().WithMany().HasForeignKey(x => x.CountryId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<LookupState>().WithMany().HasForeignKey(x => x.StateId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<LookupTitle>(entity =>
        {
            entity.ToTable("LookupTitles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
            entity.Property(x => x.NameBm).HasMaxLength(50);
            entity.Property(x => x.DisplayOrder).HasDefaultValue(0);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasFilter("[MigratedId] IS NOT NULL").HasDatabaseName("IX_LookupTitles_MigratedId");
        });

        builder.Entity<LookupIdentificationType>(entity =>
        {
            entity.ToTable("LookupIdentificationTypes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
            entity.Property(x => x.DisplayOrder).HasDefaultValue(0);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasFilter("[MigratedId] IS NOT NULL").HasDatabaseName("IX_LookupIdentificationTypes_MigratedId");
        });

        builder.Entity<CompanyProfile>(entity =>
        {
            entity.ToTable("CompanyProfiles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.MigratedId);
            entity.Property(x => x.CompanyName).HasMaxLength(100);
            entity.Property(x => x.RegistrationNo).HasMaxLength(50);
            entity.Property(x => x.RegistrationDate).HasColumnType("datetime2(3)");
            entity.Property(x => x.DateOfIncorporation).HasColumnType("datetime2(3)");
            entity.Property(x => x.TelephoneNo).HasMaxLength(100);
            entity.Property(x => x.FaxNo).HasMaxLength(100);
            entity.Property(x => x.Website).HasMaxLength(100);
            entity.Property(x => x.Email).HasMaxLength(250);
            entity.Property(x => x.IncomeTaxNo).HasMaxLength(50);
            entity.Property(x => x.EpfNo).HasMaxLength(20);
            entity.Property(x => x.SocsoNo).HasMaxLength(20);
            entity.Property(x => x.CompanySignatureId);
            entity.Property(x => x.SourceCreatedDateTime).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceModifiedDateTime).HasColumnType("datetime2(3)");
            entity.Property(x => x.BackgroundDescription1).HasColumnType("nvarchar(max)");
            entity.Property(x => x.NewSsmCompanyRegNo).HasMaxLength(50);
            entity.Property(x => x.AprNo).HasMaxLength(50);
            entity.Property(x => x.NonCode).HasMaxLength(2);
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasFilter("[MigratedId] IS NOT NULL").HasDatabaseName("IX_CompanyProfiles_MigratedId");
            entity.HasIndex(x => x.RegistrationNo).HasDatabaseName("IX_CompanyProfiles_RegistrationNo");
            entity.HasIndex(x => x.NewSsmCompanyRegNo).HasDatabaseName("IX_CompanyProfiles_NewSsmCompanyRegNo");
            entity.HasIndex(x => x.CompanyName).HasDatabaseName("IX_CompanyProfiles_CompanyName");
            entity.HasIndex(x => new { x.SourceModifiedDateTime, x.MigratedId }).HasDatabaseName("IX_CompanyProfiles_SourceModifiedDateTime");
            entity.HasOne<Address>()
                .WithMany()
                .HasForeignKey(x => x.AddressId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<CompanyAuthorizedPerson>(entity =>
        {
            entity.ToTable("CompanyAuthorizedPersons");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.FullName).HasMaxLength(100);
            entity.Property(x => x.Designation).HasMaxLength(100);
            entity.Property(x => x.IdentityNumber).HasMaxLength(50);
            entity.Property(x => x.Email).HasMaxLength(250);
            entity.Property(x => x.TelephoneNo).HasMaxLength(20);
            entity.Property(x => x.SourceCreatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceUpdatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasDatabaseName("IX_CompanyAuthorizedPersons_MigratedId");
            entity.HasIndex(x => x.CompanyProfileId).HasDatabaseName("IX_CompanyAuthorizedPersons_CompanyProfileId");
            entity.HasOne(x => x.CompanyProfile)
                .WithMany(x => x.AuthorizedPersons)
                .HasForeignKey(x => x.CompanyProfileId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<CompanyBoardDirector>(entity =>
        {
            entity.ToTable("CompanyBoardDirectors");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.Name).HasMaxLength(50);
            entity.Property(x => x.SharePercentage).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SourceCreatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceUpdatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasDatabaseName("IX_CompanyBoardDirectors_MigratedId");
            entity.HasIndex(x => x.CompanyProfileId).HasDatabaseName("IX_CompanyBoardDirectors_CompanyProfileId");
            entity.HasOne(x => x.CompanyProfile)
                .WithMany(x => x.BoardDirectors)
                .HasForeignKey(x => x.CompanyProfileId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<CompanyAttachmentDocument>(entity =>
        {
            entity.ToTable("CompanyAttachmentDocuments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.FileName).HasMaxLength(300);
            entity.Property(x => x.FileType).HasMaxLength(100);
            entity.Property(x => x.FileContent).HasColumnType("varbinary(max)");
            entity.Property(x => x.SourceCreatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceUpdatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasDatabaseName("IX_CompanyAttachmentDocuments_MigratedId");
            entity.HasIndex(x => x.CompanyProfileId).HasDatabaseName("IX_CompanyAttachmentDocuments_CompanyProfileId");
            entity.HasOne(x => x.CompanyProfile)
                .WithMany(x => x.AttachmentDocuments)
                .HasForeignKey(x => x.CompanyProfileId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<CompanyProfileFinancialDetail>(entity =>
        {
            entity.ToTable("CompanyProfileFinancialDetails");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.EffectiveDate).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceCreatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceUpdatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasDatabaseName("IX_CompanyProfileFinancialDetails_MigratedId");
            entity.HasIndex(x => x.CompanyProfileId).HasDatabaseName("IX_CompanyProfileFinancialDetails_CompanyProfileId");
            entity.HasOne(x => x.CompanyProfile).WithMany(x => x.FinancialDetails).HasForeignKey(x => x.CompanyProfileId).OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<CompanyProfileAuthorizedCapital>(entity =>
        {
            entity.ToTable("CompanyProfileAuthorizedCapitals");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.AuthorizedCapital).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SourceCreatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceUpdatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.CompanyProfileId).HasDatabaseName("IX_CompanyProfileAuthorizedCapitals_CompanyProfileId");
            entity.HasIndex(x => x.MigratedCompanyId).IsUnique().HasDatabaseName("IX_CompanyProfileAuthorizedCapitals_MigratedCompanyId");
            entity.HasOne(x => x.CompanyProfile).WithMany(x => x.AuthorizedCapitalRecords).HasForeignKey(x => x.CompanyProfileId).OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        ConfigureFinancialChild(builder.Entity<CompanyProfileEquityStructure>(), "CompanyProfileEquityStructures", "IX_CompanyProfileEquityStructures_FinancialDetailsId", tenantProvider);
        ConfigureFinancialChild(builder.Entity<CompanyProfileFinancialPerformanceRecord>(), "CompanyProfileFinancialPerformanceRecords", "IX_CompanyProfileFinancialPerformanceRecords_FinancialDetailsId", tenantProvider);
        ConfigureFinancialChild(builder.Entity<CompanyProfilePaidUpCapital>(), "CompanyProfilePaidUpCapitals", "IX_CompanyProfilePaidUpCapitals_FinancialDetailsId", tenantProvider);
        ConfigureFinancialChild(builder.Entity<CompanyProfilePaidUpCapitalMalaysianIndividuals>(), "CompanyProfilePaidUpCapitalMalaysianIndividuals", "IX_CompanyProfilePaidUpCapitalMalaysianIndividuals_FinancialDetailsId", tenantProvider);
        ConfigureFinancialChild(builder.Entity<CompanyProfileLoan>(), "CompanyProfileLoans", "IX_CompanyProfileLoans_FinancialDetailsId", tenantProvider);
        ConfigureFinancialChild(builder.Entity<CompanyProfileLoanDomestic>(), "CompanyProfileLoanDomestics", "IX_CompanyProfileLoanDomestics_FinancialDetailsId", tenantProvider);
        ConfigureFinancialChild(builder.Entity<CompanyProfileTotalFinancing>(), "CompanyProfileTotalFinancings", "IX_CompanyProfileTotalFinancings_FinancialDetailsId", tenantProvider);

        builder.Entity<CompanyProfilePaidUpCapitalForeignCompany>(entity =>
        {
            entity.ToTable("CompanyProfilePaidUpCapitalForeignCompanies");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.CompanyName).HasMaxLength(300);
            entity.Property(x => x.AmountRm).HasColumnType("decimal(18,2)");
            entity.Property(x => x.AmountPercent).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SourceCreatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceUpdatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasDatabaseName("IX_CompanyProfilePaidUpCapitalForeignCompanies_MigratedId");
            entity.HasIndex(x => x.FinancialDetailsId).HasDatabaseName("IX_CompanyProfilePaidUpCapitalForeignCompanies_FinancialDetailsId");
            entity.HasOne(x => x.FinancialDetails).WithMany().HasForeignKey(x => x.FinancialDetailsId).OnDelete(DeleteBehavior.NoAction);
            entity.Ignore(x => x.UltimateParents);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<CompanyProfilePaidUpCapitalCompanyMalaysia>(entity =>
        {
            entity.ToTable("CompanyProfilePaidUpCapitalCompaniesMalaysia");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.CompanyName).HasMaxLength(300);
            entity.Property(x => x.AmountRm).HasColumnType("decimal(18,2)");
            entity.Property(x => x.AmountPercent).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SourceCreatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceUpdatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasDatabaseName("IX_CompanyProfilePaidUpCapitalCompaniesMalaysia_MigratedId");
            entity.HasIndex(x => x.FinancialDetailsId).HasDatabaseName("IX_CompanyProfilePaidUpCapitalCompaniesMalaysia_FinancialDetailsId");
            entity.HasOne(x => x.FinancialDetails).WithMany().HasForeignKey(x => x.FinancialDetailsId).OnDelete(DeleteBehavior.NoAction);
            entity.Ignore(x => x.CompanyIncorporatedEntries);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<CompanyProfileCompanyIncorporated>(entity =>
        {
            entity.ToTable("CompanyProfileCompanyIncorporated");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.BumiPercent).HasColumnType("decimal(18,2)");
            entity.Property(x => x.NonBumiPercent).HasColumnType("decimal(18,2)");
            entity.Property(x => x.ForeignPercent).HasColumnType("decimal(18,2)");
            entity.Property(x => x.TotalPercent).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SourceCreatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceUpdatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasDatabaseName("IX_CompanyProfileCompanyIncorporated_MigratedId");
            entity.HasIndex(x => x.FinancialDetailsId).HasDatabaseName("IX_CompanyProfileCompanyIncorporated_FinancialDetailsId");
            entity.HasOne(x => x.FinancialDetails).WithMany().HasForeignKey(x => x.FinancialDetailsId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.PaidUpCapitalCompanyMalaysiaEntry).WithMany(x => x.CompanyIncorporatedEntries).HasForeignKey(x => x.PaidUpCapitalCompanyMalaysiaEntryId).OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<CompanyProfileCompanyIncorporatedCountry>(entity =>
        {
            entity.ToTable("CompanyProfileCompanyIncorporatedCountries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.CountryPercent).HasColumnType("decimal(18,2)");
            entity.Property(x => x.AmountRm).HasColumnType("decimal(18,2)");
            entity.Property(x => x.PercentOverTotal).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SourceCreatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceUpdatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasDatabaseName("IX_CompanyProfileCompanyIncorporatedCountries_MigratedId");
            entity.HasIndex(x => x.CompanyIncorporatedEntryId).HasDatabaseName("IX_CompanyProfileCompanyIncorporatedCountries_CompanyIncorporatedEntryId");
            entity.HasOne(x => x.CompanyIncorporatedEntry).WithMany(x => x.Countries).HasForeignKey(x => x.CompanyIncorporatedEntryId).OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<CompanyProfileLoanForeign>(entity =>
        {
            entity.ToTable("CompanyProfileLoanForeigns");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.AmountRm).HasColumnType("decimal(18,2)");
            entity.Property(x => x.AmountPercent).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SourceCreatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceUpdatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasDatabaseName("IX_CompanyProfileLoanForeigns_MigratedId");
            entity.HasIndex(x => x.FinancialDetailsId).HasDatabaseName("IX_CompanyProfileLoanForeigns_FinancialDetailsId");
            entity.HasOne(x => x.FinancialDetails).WithMany().HasForeignKey(x => x.FinancialDetailsId).OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<CompanyProfileOtherSource>(entity =>
        {
            entity.ToTable("CompanyProfileOtherSources");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.OtherSources).HasMaxLength(500);
            entity.Property(x => x.AmountRm).HasColumnType("decimal(18,2)");
            entity.Property(x => x.AmountPercent).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SourceCreatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceUpdatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasDatabaseName("IX_CompanyProfileOtherSources_MigratedId");
            entity.HasIndex(x => x.FinancialDetailsId).HasDatabaseName("IX_CompanyProfileOtherSources_FinancialDetailsId");
            entity.HasOne(x => x.FinancialDetails).WithMany(x => x.OtherSources).HasForeignKey(x => x.FinancialDetailsId).OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<CompanyProfileUltimateParentHoldingCompany>(entity =>
        {
            entity.ToTable("CompanyProfileUltimateParentHoldingCompanies");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasDefaultValueSql("NEWSEQUENTIALID()");
            entity.Property(x => x.UltimateCompany).HasMaxLength(300);
            entity.Property(x => x.SourceCreatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceUpdatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.MigratedId).IsUnique().HasDatabaseName("IX_CompanyProfileUltimateParentHoldingCompanies_MigratedId");
            entity.HasIndex(x => x.FinancialDetailsId).HasDatabaseName("IX_CompanyProfileUltimateParentHoldingCompanies_FinancialDetailsId");
            entity.HasOne(x => x.FinancialDetails).WithMany().HasForeignKey(x => x.FinancialDetailsId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.PaidUpCapitalForeignCompanyEntry).WithMany(x => x.UltimateParents).HasForeignKey(x => x.PaidUpCapitalForeignCompanyEntryId).OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<CompanyProfilePaidUpCapital>().Ignore(x => x.MalaysianIndividuals);
        builder.Entity<CompanyProfilePaidUpCapital>().Ignore(x => x.ForeignCompanies);
        builder.Entity<CompanyProfilePaidUpCapital>().Ignore(x => x.CompaniesMalaysia);
        builder.Entity<CompanyProfileLoan>().Ignore(x => x.DomesticBreakdowns);
        builder.Entity<CompanyProfileLoan>().Ignore(x => x.ForeignBreakdowns);

        builder.Entity<CompanyProfileSyncState>(entity =>
        {
            entity.ToTable("CompanyProfileSyncState");
            entity.HasKey(x => x.SourceName);
            entity.Property(x => x.SourceName).HasMaxLength(100).ValueGeneratedNever();
            entity.Property(x => x.LastSourceModifiedDateTime).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastStartedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastCompletedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastRunMessage).HasMaxLength(4000);
            entity.Property(x => x.LastProcessedRows).HasDefaultValue(0);
        });

        builder.Entity<InvestMalaysiaUser>(entity =>
        {
            entity.ToTable("InvestMalaysiaUsers");
            entity.HasKey(x => x.LegacyUserId);
            entity.Property(x => x.Name).HasMaxLength(256);
            entity.Property(x => x.MobilePhone).HasMaxLength(20);
            entity.Property(x => x.Email).HasMaxLength(250);
            entity.Property(x => x.UserName).HasMaxLength(250);
            entity.Property(x => x.ExternalId).HasMaxLength(36);
            entity.Property(x => x.SourceCreatedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastLoginAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        });

        builder.Entity<InvestMalaysiaGroup>(entity =>
        {
            entity.ToTable("InvestMalaysiaGroups");
            entity.HasKey(x => x.LegacyGroupId);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        });

        builder.Entity<InvestMalaysiaRole>(entity =>
        {
            entity.ToTable("InvestMalaysiaRoles");
            entity.HasKey(x => x.LegacyRoleId);
            entity.Property(x => x.Name).HasMaxLength(256).IsRequired();
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
        });

        builder.Entity<InvestMalaysiaGroupUser>(entity =>
        {
            entity.ToTable("InvestMalaysiaGroupUsers");
            entity.HasKey(x => new { x.LegacyGroupId, x.LegacyUserId });
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Group)
                .WithMany(x => x.UserAssignments)
                .HasForeignKey(x => x.LegacyGroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany(x => x.GroupAssignments)
                .HasForeignKey(x => x.LegacyUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InvestMalaysiaGroupRole>(entity =>
        {
            entity.ToTable("InvestMalaysiaGroupRoles");
            entity.HasKey(x => new { x.LegacyGroupId, x.LegacyRoleId });
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.Group)
                .WithMany(x => x.RoleAssignments)
                .HasForeignKey(x => x.LegacyGroupId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role)
                .WithMany(x => x.GroupAssignments)
                .HasForeignKey(x => x.LegacyRoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InvestMalaysiaUserRole>(entity =>
        {
            entity.ToTable("InvestMalaysiaUserRoles");
            entity.HasKey(x => new { x.LegacyUserId, x.LegacyRoleId });
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(x => x.User)
                .WithMany(x => x.DirectRoleAssignments)
                .HasForeignKey(x => x.LegacyUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role)
                .WithMany(x => x.UserAssignments)
                .HasForeignKey(x => x.LegacyRoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InvestMalaysiaContactPerson>(entity =>
        {
            entity.ToTable("InvestMalaysiaContactPersons");
            entity.HasKey(x => x.LegacyContactPersonId);
            entity.Property(x => x.FullName).HasMaxLength(256);
            entity.Property(x => x.Email).HasMaxLength(250);
            entity.Property(x => x.TelephoneNo).HasMaxLength(100);
            entity.Property(x => x.FaxNo).HasMaxLength(100);
            entity.Property(x => x.CreatedBy).HasMaxLength(150);
            entity.Property(x => x.ModifiedBy).HasMaxLength(150);
            entity.Property(x => x.TitleName).HasMaxLength(50);
            entity.Property(x => x.OtherDesignationName).HasMaxLength(100);
            entity.Property(x => x.SourceCreatedDateTime).HasColumnType("datetime2(3)");
            entity.Property(x => x.SourceModifiedDateTime).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastSyncedAt).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(x => x.LegacyCompanyId).HasDatabaseName("IX_InvestMalaysiaContactPersons_LegacyCompanyId");
            entity.HasIndex(x => x.LegacyUserId).HasDatabaseName("IX_InvestMalaysiaContactPersons_LegacyUserId");
        });

        builder.Entity<InvestMalaysiaGroupMapping>(entity =>
        {
            entity.ToTable("InvestMalaysiaGroupMappings");
            entity.Property(x => x.InvestMalaysiaGroupName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.NormalizedInvestMalaysiaGroupName).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.NormalizedInvestMalaysiaGroupName }).IsUnique();
            entity.HasOne(x => x.PlatformAccessGroup)
                .WithMany()
                .HasForeignKey(x => x.PlatformAccessGroupId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.IsDeleted && (!tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId));
        });

        builder.Entity<InvestMalaysiaUserSyncState>(entity =>
        {
            entity.ToTable("InvestMalaysiaUserSyncState");
            entity.HasKey(x => x.SourceName);
            entity.Property(x => x.SourceName).HasMaxLength(100).ValueGeneratedNever();
            entity.Property(x => x.LastStartedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastCompletedAt).HasColumnType("datetime2(3)");
            entity.Property(x => x.LastRunMessage).HasMaxLength(4000);
            entity.Property(x => x.LastProcessedRows).HasDefaultValue(0);
        });

        builder.Entity<WorkflowDefinition>(entity =>
        {
            entity.ToTable("WorkflowDefinitions", "wf");
            entity.HasIndex(x => x.WorkflowCode).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.WorkflowCode).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(x => x.UpdatedBy).HasMaxLength(200);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasQueryFilter(x => !x.TenantId.HasValue || !tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId);
        });

        builder.Entity<WorkflowDraft>(entity =>
        {
            entity.ToTable("WorkflowDrafts", "wf");
            entity.HasIndex(x => x.WorkflowDefinitionId).IsUnique();
            entity.Property(x => x.ValidationStatus).HasMaxLength(30).IsRequired();
            entity.Property(x => x.LockedBy).HasMaxLength(200);
            entity.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
            entity.Property(x => x.UpdatedBy).HasMaxLength(200);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasOne<WorkflowDefinition>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WorkflowVersion>(entity =>
        {
            entity.ToTable("WorkflowVersions", "wf");
            entity.HasIndex(x => new { x.WorkflowDefinitionId, x.VersionNumber }).IsUnique();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.PublishedBy).HasMaxLength(200);
            entity.Property(x => x.PublishMessage).HasMaxLength(2000);
            entity.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
            entity.HasOne<WorkflowDefinition>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WorkflowInstance>(entity =>
        {
            entity.ToTable("WorkflowInstances", "wf");
            entity.Property(x => x.BusinessKey).HasMaxLength(200);
            entity.Property(x => x.CorrelationId).HasMaxLength(200);
            entity.Property(x => x.Title).HasMaxLength(300);
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.StartedBy).HasMaxLength(200).IsRequired();
            entity.Property(x => x.StartedByDisplayName).HasMaxLength(400);
            entity.Property(x => x.FailureCode).HasMaxLength(100);
            entity.Property(x => x.FailureMessage).HasMaxLength(2000);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.BusinessKey).HasFilter("[BusinessKey] IS NOT NULL");
            entity.HasOne<WorkflowDefinition>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowDefinitionId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<WorkflowVersion>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowVersionId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<WorkflowInstance>()
                .WithMany()
                .HasForeignKey(x => x.ParentWorkflowInstanceId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<WorkflowInstance>()
                .WithMany()
                .HasForeignKey(x => x.RootWorkflowInstanceId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.StartedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.TenantId.HasValue || !tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId);
        });

        builder.Entity<WorkflowInstanceNode>(entity =>
        {
            entity.ToTable("WorkflowInstanceNodes", "wf");
            entity.Property(x => x.NodeId).HasMaxLength(150).IsRequired();
            entity.Property(x => x.NodeType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.NodeName).HasMaxLength(200);
            entity.Property(x => x.BranchKey).HasMaxLength(100);
            entity.Property(x => x.JoinGroupKey).HasMaxLength(100);
            entity.Property(x => x.ExecutionStatus).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ErrorCode).HasMaxLength(100);
            entity.Property(x => x.ErrorMessage).HasMaxLength(4000);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.WorkflowInstanceId, x.ExecutionStatus, x.ActivatedAtUtc });
            entity.HasOne<WorkflowInstance>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WorkflowTask>(entity =>
        {
            entity.ToTable("WorkflowTasks", "wf", table =>
            {
                table.HasCheckConstraint("CK_wf_WorkflowTasks_TaskMode", "[TaskMode] IN (N'approval', N'review', N'dataEntry', N'acknowledgement', N'manualAction', N'exception')");
                table.HasCheckConstraint("CK_wf_WorkflowTasks_Priority", "[Priority] IS NULL OR [Priority] IN (N'Low', N'Medium', N'High', N'Critical')");
                table.HasCheckConstraint("CK_wf_WorkflowTasks_SlaStatus", "[SlaStatus] IS NULL OR [SlaStatus] IN (N'OnTrack', N'DueSoon', N'Overdue', N'Escalated')");
            });
            entity.Property(x => x.NodeId).HasMaxLength(150).IsRequired();
            entity.Property(x => x.TaskCode).HasMaxLength(100);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.TaskMode).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Priority).HasMaxLength(20);
            entity.Property(x => x.EntityType).HasMaxLength(80);
            entity.Property(x => x.EntityId).HasMaxLength(120);
            entity.Property(x => x.FormKey).HasMaxLength(120);
            entity.Property(x => x.ListViewKey).HasMaxLength(120);
            entity.Property(x => x.DetailViewKey).HasMaxLength(120);
            entity.Property(x => x.AssignmentType).HasMaxLength(50).IsRequired();
            entity.Property(x => x.AssignedToDisplayName).HasMaxLength(400);
            entity.Property(x => x.QueueKey).HasMaxLength(120);
            entity.Property(x => x.ClaimedBy).HasMaxLength(200);
            entity.Property(x => x.CompletedBy).HasMaxLength(200);
            entity.Property(x => x.EscalationPolicyKey).HasMaxLength(120);
            entity.Property(x => x.SlaStatus).HasMaxLength(30);
            entity.Property(x => x.Outcome).HasMaxLength(60);
            entity.Property(x => x.Comment).HasMaxLength(2000);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.AssignedToUserId, x.Status, x.DueAtUtc }).HasFilter("[AssignedToUserId] IS NOT NULL");
            entity.HasIndex(x => new { x.AssignedToGroupId, x.Status, x.DueAtUtc }).HasFilter("[AssignedToGroupId] IS NOT NULL");
            entity.HasIndex(x => new { x.AssignedToRoleId, x.Status, x.DueAtUtc }).HasFilter("[AssignedToRoleId] IS NOT NULL");
            entity.HasIndex(x => new { x.Status, x.Priority, x.DueAtUtc, x.CreatedAtUtc }).HasDatabaseName("IX_wf_WorkflowTasks_StatusPriorityDue");
            entity.HasIndex(x => new { x.TaskMode, x.Status, x.DueAtUtc }).HasDatabaseName("IX_wf_WorkflowTasks_TaskModeStatus");
            entity.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("IX_wf_WorkflowTasks_Entity").HasFilter("[EntityType] IS NOT NULL AND [EntityId] IS NOT NULL");
            entity.HasIndex(x => new { x.Status, x.EscalationAtUtc }).HasDatabaseName("IX_wf_WorkflowTasks_Escalation").HasFilter("[EscalationAtUtc] IS NOT NULL");
            entity.HasIndex(x => new { x.Status, x.ReminderAtUtc }).HasDatabaseName("IX_wf_WorkflowTasks_Reminder").HasFilter("[ReminderAtUtc] IS NOT NULL");
            entity.HasOne<WorkflowInstance>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<WorkflowInstanceNode>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowInstanceNodeId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<PlatformAccessGroup>().WithMany().HasForeignKey(x => x.AssignedToGroupId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<PlatformAccessRole>().WithMany().HasForeignKey(x => x.AssignedToRoleId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ClaimedByUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CompletedByUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasQueryFilter(x => !x.TenantId.HasValue || !tenantProvider.TenantId.HasValue || x.TenantId == tenantProvider.TenantId);
        });

        builder.Entity<WorkflowTaskComment>(entity =>
        {
            entity.ToTable("WorkflowTaskComments", "wf", table =>
            {
                table.HasCheckConstraint("CK_wf_WorkflowTaskComments_CommentType", "[CommentType] IN (N'Comment', N'Decision', N'SystemNote', N'Escalation', N'AssignmentReason')");
                table.HasCheckConstraint("CK_wf_WorkflowTaskComments_Visibility", "[Visibility] IN (N'Internal', N'Participant', N'Watcher')");
            });
            entity.Property(x => x.CommentType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Body).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Visibility).HasMaxLength(30).IsRequired();
            entity.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.WorkflowTaskId, x.CreatedAtUtc }).HasDatabaseName("IX_wf_WorkflowTaskComments_TaskCreated");
            entity.HasOne<WorkflowTask>().WithMany().HasForeignKey(x => x.WorkflowTaskId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<WorkflowTaskAssignment>(entity =>
        {
            entity.ToTable("WorkflowTaskAssignments", "wf", table =>
            {
                table.HasCheckConstraint("CK_wf_WorkflowTaskAssignments_ActionType", "[ActionType] IN (N'Assigned', N'Claimed', N'Unclaimed', N'Delegated', N'Reassigned', N'Escalated', N'AutoRouted')");
            });
            entity.Property(x => x.ActionType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.Property(x => x.PerformedBy).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.WorkflowTaskId, x.CreatedAtUtc }).HasDatabaseName("IX_wf_WorkflowTaskAssignments_TaskCreated");
            entity.HasIndex(x => new { x.PerformedByUserId, x.CreatedAtUtc }).HasDatabaseName("IX_wf_WorkflowTaskAssignments_PerformedBy").HasFilter("[PerformedByUserId] IS NOT NULL");
            entity.HasOne<WorkflowTask>().WithMany().HasForeignKey(x => x.WorkflowTaskId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.FromUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ToUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PerformedByUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<PlatformAccessGroup>().WithMany().HasForeignKey(x => x.FromGroupId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<PlatformAccessGroup>().WithMany().HasForeignKey(x => x.ToGroupId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<PlatformAccessRole>().WithMany().HasForeignKey(x => x.FromRoleId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<PlatformAccessRole>().WithMany().HasForeignKey(x => x.ToRoleId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<WorkflowVariable>(entity =>
        {
            entity.ToTable("WorkflowInstanceVariables", "wf");
            entity.HasIndex(x => new { x.WorkflowInstanceId, x.VariableName }).IsUnique();
            entity.Property(x => x.VariableName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ValueType).HasMaxLength(40).IsRequired();
            entity.HasOne<WorkflowInstance>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WorkflowTimer>(entity =>
        {
            entity.ToTable("WorkflowTimers", "wf");
            entity.Property(x => x.TimerType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.Status, x.DueAtUtc });
            entity.HasOne<WorkflowInstance>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<WorkflowInstanceNode>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowInstanceNodeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<WorkflowEventSubscription>(entity =>
        {
            entity.ToTable("WorkflowEventSubscriptions", "wf");
            entity.Property(x => x.EventName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.CorrelationKey).HasMaxLength(200);
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => new { x.Status, x.EventName, x.CorrelationKey, x.ExpiresAtUtc });
            entity.HasOne<WorkflowInstance>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<WorkflowInstanceNode>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowInstanceNodeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<WorkflowExecutionLog>(entity =>
        {
            entity.ToTable("WorkflowExecutionLogs", "wf");
            entity.Property(x => x.LogLevel).HasMaxLength(20).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(2000);
            entity.HasIndex(x => new { x.WorkflowInstanceId, x.CreatedAtUtc });
            entity.HasOne<WorkflowInstance>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<WorkflowInstanceNode>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowInstanceNodeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<WorkflowAuditLog>(entity =>
        {
            entity.ToTable("WorkflowAuditLogs", "wf");
            entity.HasIndex(x => new { x.WorkflowInstanceId, x.CreatedAtUtc });
            entity.Property(x => x.ActorType).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ActorId).HasMaxLength(200);
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(1000);
            entity.Property(x => x.FromNodeId).HasMaxLength(150);
            entity.Property(x => x.ToNodeId).HasMaxLength(150);
            entity.HasOne<WorkflowInstance>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowInstanceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<WorkflowTask>()
                .WithMany()
                .HasForeignKey(x => x.WorkflowTaskId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.ActorUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<WorkflowOutbox>(entity =>
        {
            entity.ToTable("WorkflowOutbox", "wf", table =>
            {
                table.HasCheckConstraint("CK_wf_WorkflowOutbox_Status", "[Status] IN (N'Pending', N'Processing', N'Processed', N'Failed', N'DeadLettered')");
            });
            entity.Property(x => x.AggregateType).HasMaxLength(80).IsRequired();
            entity.Property(x => x.AggregateId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(30).IsRequired();
            entity.Property(x => x.ProcessorName).HasMaxLength(120);
            entity.Property(x => x.ErrorCode).HasMaxLength(100);
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.HasIndex(x => new { x.Status, x.NextAttemptAtUtc, x.OccurredAtUtc, x.RetryCount }).HasDatabaseName("IX_wf_WorkflowOutbox_StatusNextAttempt");
            entity.HasIndex(x => new { x.Status, x.LockedAtUtc }).HasDatabaseName("IX_wf_WorkflowOutbox_LockedAt").HasFilter("[LockedAtUtc] IS NOT NULL");
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAudit();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAudit()
    {
        var now = DateTimeOffset.UtcNow;
        var userName = currentUserService.UserName ?? "system";

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userName;
                entry.Entity.TenantId ??= tenantProvider.TenantId;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userName;
            }

            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = now;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userName;
            }
        }
    }
}
