using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RebuildCompanyProfilesWithGuidId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyProfiles");

            migrationBuilder.CreateTable(
                name: "CompanyProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    MigratedId = table.Column<long>(type: "bigint", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RegistrationNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RegistrationDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    DateOfIncorporation = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    TelephoneNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FaxNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IncomeTaxNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EpfNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SocsoNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CompanySignatureId = table.Column<long>(type: "bigint", nullable: true),
                    CompanyType = table.Column<int>(type: "int", nullable: true),
                    IsCompanyCertified = table.Column<bool>(type: "bit", nullable: true),
                    CompanyApprovalStatus = table.Column<int>(type: "int", nullable: true),
                    IsPaid = table.Column<bool>(type: "bit", nullable: true),
                    IsCompanyLocal = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBySourceUserId = table.Column<int>(type: "int", nullable: true),
                    SourceCreatedDateTime = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ModifiedBySourceUserId = table.Column<int>(type: "int", nullable: true),
                    SourceModifiedDateTime = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    AddressId = table.Column<long>(type: "bigint", nullable: true),
                    BackgroundDescription1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewSsmCompanyRegNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompanyStatusId = table.Column<int>(type: "int", nullable: true),
                    TotalEmployment = table.Column<int>(type: "int", nullable: true),
                    AnnualClosingDateDay = table.Column<int>(type: "int", nullable: true),
                    AnnualClosingDateMonth = table.Column<int>(type: "int", nullable: true),
                    AprNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NonCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_CompanyName",
                table: "CompanyProfiles",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_MigratedId",
                table: "CompanyProfiles",
                column: "MigratedId",
                unique: true,
                filter: "[MigratedId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_NewSsmCompanyRegNo",
                table: "CompanyProfiles",
                column: "NewSsmCompanyRegNo");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_RegistrationNo",
                table: "CompanyProfiles",
                column: "RegistrationNo");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_SourceModifiedDateTime",
                table: "CompanyProfiles",
                columns: new[] { "SourceModifiedDateTime", "MigratedId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyProfiles");

            migrationBuilder.CreateTable(
                name: "CompanyProfiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MigratedId = table.Column<long>(type: "bigint", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RegistrationNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RegistrationDate = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    DateOfIncorporation = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    TelephoneNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FaxNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IncomeTaxNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EpfNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SocsoNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    CompanySignatureId = table.Column<long>(type: "bigint", nullable: true),
                    CompanyType = table.Column<int>(type: "int", nullable: true),
                    IsCompanyCertified = table.Column<bool>(type: "bit", nullable: true),
                    CompanyApprovalStatus = table.Column<int>(type: "int", nullable: true),
                    IsPaid = table.Column<bool>(type: "bit", nullable: true),
                    IsCompanyLocal = table.Column<bool>(type: "bit", nullable: true),
                    CreatedBySourceUserId = table.Column<int>(type: "int", nullable: true),
                    SourceCreatedDateTime = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ModifiedBySourceUserId = table.Column<int>(type: "int", nullable: true),
                    SourceModifiedDateTime = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    AddressId = table.Column<long>(type: "bigint", nullable: true),
                    BackgroundDescription1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewSsmCompanyRegNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CompanyStatusId = table.Column<int>(type: "int", nullable: true),
                    TotalEmployment = table.Column<int>(type: "int", nullable: true),
                    AnnualClosingDateDay = table.Column<int>(type: "int", nullable: true),
                    AnnualClosingDateMonth = table.Column<int>(type: "int", nullable: true),
                    AprNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NonCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_CompanyName",
                table: "CompanyProfiles",
                column: "CompanyName");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_MigratedId",
                table: "CompanyProfiles",
                column: "MigratedId",
                unique: true,
                filter: "[MigratedId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_NewSsmCompanyRegNo",
                table: "CompanyProfiles",
                column: "NewSsmCompanyRegNo");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_RegistrationNo",
                table: "CompanyProfiles",
                column: "RegistrationNo");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_SourceModifiedDateTime",
                table: "CompanyProfiles",
                columns: new[] { "SourceModifiedDateTime", "MigratedId" });
        }
    }
}
