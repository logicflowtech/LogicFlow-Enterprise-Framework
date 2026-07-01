using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestMalaysiaAccessSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvestMalaysiaGroupMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvestMalaysiaGroupName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NormalizedInvestMalaysiaGroupName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PlatformAccessGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_InvestMalaysiaGroupMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvestMalaysiaGroupMappings_PlatformAccessGroups_PlatformAccessGroupId",
                        column: x => x.PlatformAccessGroupId,
                        principalTable: "PlatformAccessGroups",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InvestMalaysiaGroups",
                columns: table => new
                {
                    LegacyGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestMalaysiaGroups", x => x.LegacyGroupId);
                });

            migrationBuilder.CreateTable(
                name: "InvestMalaysiaRoles",
                columns: table => new
                {
                    LegacyRoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestMalaysiaRoles", x => x.LegacyRoleId);
                });

            migrationBuilder.CreateTable(
                name: "InvestMalaysiaUsers",
                columns: table => new
                {
                    LegacyUserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LegacyTenantId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SourceCreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MobilePhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestMalaysiaUsers", x => x.LegacyUserId);
                });

            migrationBuilder.CreateTable(
                name: "InvestMalaysiaUserSyncState",
                columns: table => new
                {
                    SourceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastStartedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    LastCompletedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    LastRunSucceeded = table.Column<bool>(type: "bit", nullable: true),
                    LastProcessedRows = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastRunMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestMalaysiaUserSyncState", x => x.SourceName);
                });

            migrationBuilder.CreateTable(
                name: "InvestMalaysiaGroupRoles",
                columns: table => new
                {
                    LegacyGroupId = table.Column<int>(type: "int", nullable: false),
                    LegacyRoleId = table.Column<int>(type: "int", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestMalaysiaGroupRoles", x => new { x.LegacyGroupId, x.LegacyRoleId });
                    table.ForeignKey(
                        name: "FK_InvestMalaysiaGroupRoles_InvestMalaysiaGroups_LegacyGroupId",
                        column: x => x.LegacyGroupId,
                        principalTable: "InvestMalaysiaGroups",
                        principalColumn: "LegacyGroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvestMalaysiaGroupRoles_InvestMalaysiaRoles_LegacyRoleId",
                        column: x => x.LegacyRoleId,
                        principalTable: "InvestMalaysiaRoles",
                        principalColumn: "LegacyRoleId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvestMalaysiaGroupUsers",
                columns: table => new
                {
                    LegacyGroupId = table.Column<int>(type: "int", nullable: false),
                    LegacyUserId = table.Column<int>(type: "int", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestMalaysiaGroupUsers", x => new { x.LegacyGroupId, x.LegacyUserId });
                    table.ForeignKey(
                        name: "FK_InvestMalaysiaGroupUsers_InvestMalaysiaGroups_LegacyGroupId",
                        column: x => x.LegacyGroupId,
                        principalTable: "InvestMalaysiaGroups",
                        principalColumn: "LegacyGroupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvestMalaysiaGroupUsers_InvestMalaysiaUsers_LegacyUserId",
                        column: x => x.LegacyUserId,
                        principalTable: "InvestMalaysiaUsers",
                        principalColumn: "LegacyUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InvestMalaysiaUserRoles",
                columns: table => new
                {
                    LegacyUserId = table.Column<int>(type: "int", nullable: false),
                    LegacyRoleId = table.Column<int>(type: "int", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestMalaysiaUserRoles", x => new { x.LegacyUserId, x.LegacyRoleId });
                    table.ForeignKey(
                        name: "FK_InvestMalaysiaUserRoles_InvestMalaysiaRoles_LegacyRoleId",
                        column: x => x.LegacyRoleId,
                        principalTable: "InvestMalaysiaRoles",
                        principalColumn: "LegacyRoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvestMalaysiaUserRoles_InvestMalaysiaUsers_LegacyUserId",
                        column: x => x.LegacyUserId,
                        principalTable: "InvestMalaysiaUsers",
                        principalColumn: "LegacyUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvestMalaysiaGroupMappings_PlatformAccessGroupId",
                table: "InvestMalaysiaGroupMappings",
                column: "PlatformAccessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestMalaysiaGroupMappings_TenantId_NormalizedInvestMalaysiaGroupName",
                table: "InvestMalaysiaGroupMappings",
                columns: new[] { "TenantId", "NormalizedInvestMalaysiaGroupName" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InvestMalaysiaGroupRoles_LegacyRoleId",
                table: "InvestMalaysiaGroupRoles",
                column: "LegacyRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestMalaysiaGroupUsers_LegacyUserId",
                table: "InvestMalaysiaGroupUsers",
                column: "LegacyUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestMalaysiaUserRoles_LegacyRoleId",
                table: "InvestMalaysiaUserRoles",
                column: "LegacyRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvestMalaysiaGroupMappings");

            migrationBuilder.DropTable(
                name: "InvestMalaysiaGroupRoles");

            migrationBuilder.DropTable(
                name: "InvestMalaysiaGroupUsers");

            migrationBuilder.DropTable(
                name: "InvestMalaysiaUserRoles");

            migrationBuilder.DropTable(
                name: "InvestMalaysiaUserSyncState");

            migrationBuilder.DropTable(
                name: "InvestMalaysiaGroups");

            migrationBuilder.DropTable(
                name: "InvestMalaysiaRoles");

            migrationBuilder.DropTable(
                name: "InvestMalaysiaUsers");
        }
    }
}
