using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyPlatformSecurityModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlatformAccessGroups_PlatformApplications_PlatformApplicationId",
                table: "PlatformAccessGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_PlatformAccessGroups_PlatformModules_PlatformModuleId",
                table: "PlatformAccessGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_PlatformFeatures_PlatformApplications_PlatformApplicationId",
                table: "PlatformFeatures");

            migrationBuilder.DropForeignKey(
                name: "FK_PlatformFeatures_PlatformModules_PlatformModuleId",
                table: "PlatformFeatures");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAccessGroupAssignments_PlatformApplications_PlatformApplicationId",
                table: "UserAccessGroupAssignments");

            migrationBuilder.DropTable(
                name: "PlatformModules");

            migrationBuilder.DropTable(
                name: "UserApplicationAccesses");

            migrationBuilder.DropTable(
                name: "PlatformApplications");

            migrationBuilder.DropIndex(
                name: "IX_UserAccessGroupAssignments_PlatformApplicationId",
                table: "UserAccessGroupAssignments");

            migrationBuilder.DropIndex(
                name: "IX_PlatformFeatures_PlatformApplicationId",
                table: "PlatformFeatures");

            migrationBuilder.DropIndex(
                name: "IX_PlatformFeatures_PlatformModuleId",
                table: "PlatformFeatures");

            migrationBuilder.DropIndex(
                name: "IX_PlatformFeatures_TenantId_PlatformApplicationId_Code",
                table: "PlatformFeatures");

            migrationBuilder.DropIndex(
                name: "IX_PlatformAccessGroups_PlatformApplicationId",
                table: "PlatformAccessGroups");

            migrationBuilder.DropIndex(
                name: "IX_PlatformAccessGroups_PlatformModuleId",
                table: "PlatformAccessGroups");

            migrationBuilder.DropIndex(
                name: "IX_PlatformAccessGroups_TenantId_PlatformApplicationId_Code",
                table: "PlatformAccessGroups");

            migrationBuilder.DropColumn(
                name: "PlatformApplicationId",
                table: "UserAccessGroupAssignments");

            migrationBuilder.DropColumn(
                name: "PlatformApplicationId",
                table: "PlatformFeatures");

            migrationBuilder.DropColumn(
                name: "PlatformModuleId",
                table: "PlatformFeatures");

            migrationBuilder.DropColumn(
                name: "PlatformApplicationId",
                table: "PlatformAccessGroups");

            migrationBuilder.DropColumn(
                name: "PlatformModuleId",
                table: "PlatformAccessGroups");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformFeatures_TenantId_Code",
                table: "PlatformFeatures",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccessGroups_TenantId_Code",
                table: "PlatformAccessGroups",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PlatformFeatures_TenantId_Code",
                table: "PlatformFeatures");

            migrationBuilder.DropIndex(
                name: "IX_PlatformAccessGroups_TenantId_Code",
                table: "PlatformAccessGroups");

            migrationBuilder.AddColumn<Guid>(
                name: "PlatformApplicationId",
                table: "UserAccessGroupAssignments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PlatformApplicationId",
                table: "PlatformFeatures",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PlatformModuleId",
                table: "PlatformFeatures",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PlatformApplicationId",
                table: "PlatformAccessGroups",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "PlatformModuleId",
                table: "PlatformAccessGroups",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PlatformApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EntryUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformModules_PlatformApplications_PlatformApplicationId",
                        column: x => x.PlatformApplicationId,
                        principalTable: "PlatformApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserApplicationAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserApplicationAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserApplicationAccesses_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserApplicationAccesses_PlatformApplications_PlatformApplicationId",
                        column: x => x.PlatformApplicationId,
                        principalTable: "PlatformApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessGroupAssignments_PlatformApplicationId",
                table: "UserAccessGroupAssignments",
                column: "PlatformApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformFeatures_PlatformApplicationId",
                table: "PlatformFeatures",
                column: "PlatformApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformFeatures_PlatformModuleId",
                table: "PlatformFeatures",
                column: "PlatformModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformFeatures_TenantId_PlatformApplicationId_Code",
                table: "PlatformFeatures",
                columns: new[] { "TenantId", "PlatformApplicationId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccessGroups_PlatformApplicationId",
                table: "PlatformAccessGroups",
                column: "PlatformApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccessGroups_PlatformModuleId",
                table: "PlatformAccessGroups",
                column: "PlatformModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccessGroups_TenantId_PlatformApplicationId_Code",
                table: "PlatformAccessGroups",
                columns: new[] { "TenantId", "PlatformApplicationId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformApplications_TenantId_Code",
                table: "PlatformApplications",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformModules_PlatformApplicationId",
                table: "PlatformModules",
                column: "PlatformApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformModules_TenantId_PlatformApplicationId_Code",
                table: "PlatformModules",
                columns: new[] { "TenantId", "PlatformApplicationId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UserApplicationAccesses_ApplicationUserId_PlatformApplicationId",
                table: "UserApplicationAccesses",
                columns: new[] { "ApplicationUserId", "PlatformApplicationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserApplicationAccesses_PlatformApplicationId",
                table: "UserApplicationAccesses",
                column: "PlatformApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformAccessGroups_PlatformApplications_PlatformApplicationId",
                table: "PlatformAccessGroups",
                column: "PlatformApplicationId",
                principalTable: "PlatformApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformAccessGroups_PlatformModules_PlatformModuleId",
                table: "PlatformAccessGroups",
                column: "PlatformModuleId",
                principalTable: "PlatformModules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformFeatures_PlatformApplications_PlatformApplicationId",
                table: "PlatformFeatures",
                column: "PlatformApplicationId",
                principalTable: "PlatformApplications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlatformFeatures_PlatformModules_PlatformModuleId",
                table: "PlatformFeatures",
                column: "PlatformModuleId",
                principalTable: "PlatformModules",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccessGroupAssignments_PlatformApplications_PlatformApplicationId",
                table: "UserAccessGroupAssignments",
                column: "PlatformApplicationId",
                principalTable: "PlatformApplications",
                principalColumn: "Id");
        }
    }
}
