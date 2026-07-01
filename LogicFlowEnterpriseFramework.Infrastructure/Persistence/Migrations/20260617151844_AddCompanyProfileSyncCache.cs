using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyProfileSyncCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'dbo.CompanyProfiles', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.CompanyProfiles
                    (
                        SourceCompanyId BIGINT NOT NULL,
                        CompanyName NVARCHAR(100) NULL,
                        RegistrationNo NVARCHAR(50) NULL,
                        RegistrationDate DATETIME2(3) NULL,
                        DateOfIncorporation DATETIME2(3) NULL,
                        TelephoneNo NVARCHAR(100) NULL,
                        FaxNo NVARCHAR(100) NULL,
                        Website NVARCHAR(100) NULL,
                        Email NVARCHAR(250) NULL,
                        IncomeTaxNo NVARCHAR(50) NULL,
                        EpfNo NVARCHAR(20) NULL,
                        SocsoNo NVARCHAR(20) NULL,
                        UserId INT NULL,
                        CompanySignatureId BIGINT NULL,
                        CompanyType INT NULL,
                        IsCompanyCertified BIT NULL,
                        CompanyApprovalStatus INT NULL,
                        IsPaid BIT NULL,
                        IsCompanyLocal BIT NULL,
                        CreatedBySourceUserId INT NULL,
                        SourceCreatedDateTime DATETIME2(3) NULL,
                        ModifiedBySourceUserId INT NULL,
                        SourceModifiedDateTime DATETIME2(3) NULL,
                        AddressId BIGINT NULL,
                        BackgroundDescription1 NVARCHAR(MAX) NULL,
                        NewSsmCompanyRegNo NVARCHAR(50) NULL,
                        CompanyStatusId INT NULL,
                        TotalEmployment INT NULL,
                        AnnualClosingDateDay INT NULL,
                        AnnualClosingDateMonth INT NULL,
                        AprNo NVARCHAR(50) NULL,
                        NonCode NVARCHAR(2) NULL,
                        LastSyncedAt DATETIME2(3) NOT NULL CONSTRAINT DF_CompanyProfiles_LastSyncedAt DEFAULT SYSUTCDATETIME(),
                        CONSTRAINT PK_CompanyProfiles PRIMARY KEY CLUSTERED (SourceCompanyId)
                    );
                END;

                IF OBJECT_ID(N'dbo.CompanyProfileSyncState', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.CompanyProfileSyncState
                    (
                        SourceName NVARCHAR(100) NOT NULL,
                        LastSourceModifiedDateTime DATETIME2(3) NULL,
                        LastSourceCompanyId BIGINT NULL,
                        LastStartedAt DATETIME2(3) NULL,
                        LastCompletedAt DATETIME2(3) NULL,
                        LastRunSucceeded BIT NULL,
                        LastProcessedRows INT NOT NULL CONSTRAINT DF_CompanyProfileSyncState_LastProcessedRows DEFAULT (0),
                        LastRunMessage NVARCHAR(4000) NULL,
                        CONSTRAINT PK_CompanyProfileSyncState PRIMARY KEY CLUSTERED (SourceName)
                    );
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyProfiles_CompanyName' AND object_id = OBJECT_ID(N'dbo.CompanyProfiles'))
                BEGIN
                    CREATE INDEX IX_CompanyProfiles_CompanyName ON dbo.CompanyProfiles (CompanyName);
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyProfiles_NewSsmCompanyRegNo' AND object_id = OBJECT_ID(N'dbo.CompanyProfiles'))
                BEGIN
                    CREATE INDEX IX_CompanyProfiles_NewSsmCompanyRegNo ON dbo.CompanyProfiles (NewSsmCompanyRegNo);
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyProfiles_RegistrationNo' AND object_id = OBJECT_ID(N'dbo.CompanyProfiles'))
                BEGIN
                    CREATE INDEX IX_CompanyProfiles_RegistrationNo ON dbo.CompanyProfiles (RegistrationNo);
                END;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CompanyProfiles_SourceModifiedDateTime' AND object_id = OBJECT_ID(N'dbo.CompanyProfiles'))
                BEGIN
                    CREATE INDEX IX_CompanyProfiles_SourceModifiedDateTime ON dbo.CompanyProfiles (SourceModifiedDateTime, SourceCompanyId);
                END;

                IF NOT EXISTS (SELECT 1 FROM dbo.CompanyProfileSyncState WHERE SourceName = N'syn_Company')
                BEGIN
                    INSERT INTO dbo.CompanyProfileSyncState (SourceName, LastRunSucceeded, LastProcessedRows, LastRunMessage)
                    VALUES (N'syn_Company', NULL, 0, N'Not started');
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'dbo.CompanyProfiles', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE dbo.CompanyProfiles;
                END;

                IF OBJECT_ID(N'dbo.CompanyProfileSyncState', N'U') IS NOT NULL
                BEGIN
                    DROP TABLE dbo.CompanyProfileSyncState;
                END;
                """);
        }
    }
}
