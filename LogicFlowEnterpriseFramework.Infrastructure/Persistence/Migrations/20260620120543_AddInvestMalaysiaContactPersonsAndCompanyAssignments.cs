using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestMalaysiaContactPersonsAndCompanyAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyProfileUserAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegacyContactPersonId = table.Column<long>(type: "bigint", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyProfileUserAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyProfileUserAssignments_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompanyProfileUserAssignments_CompanyProfiles_CompanyProfileId",
                        column: x => x.CompanyProfileId,
                        principalTable: "CompanyProfiles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InvestMalaysiaContactPersons",
                columns: table => new
                {
                    LegacyContactPersonId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserDesignationId = table.Column<int>(type: "int", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    TelephoneNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FaxNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LegacyCompanyId = table.Column<long>(type: "bigint", nullable: true),
                    TempContactPersonId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    ContactPersonApprovalStatus = table.Column<int>(type: "int", nullable: true),
                    LegacyUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SourceCreatedDateTime = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    ModifiedByUserId = table.Column<int>(type: "int", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SourceModifiedDateTime = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    TitleId = table.Column<int>(type: "int", nullable: true),
                    TitleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OtherDesignationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestMalaysiaContactPersons", x => x.LegacyContactPersonId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfileUserAssignments_ApplicationUserId_CompanyProfileId",
                table: "CompanyProfileUserAssignments",
                columns: new[] { "ApplicationUserId", "CompanyProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfileUserAssignments_CompanyProfileId",
                table: "CompanyProfileUserAssignments",
                column: "CompanyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestMalaysiaContactPersons_LegacyCompanyId",
                table: "InvestMalaysiaContactPersons",
                column: "LegacyCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestMalaysiaContactPersons_LegacyUserId",
                table: "InvestMalaysiaContactPersons",
                column: "LegacyUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyProfileUserAssignments");

            migrationBuilder.DropTable(
                name: "InvestMalaysiaContactPersons");
        }
    }
}
