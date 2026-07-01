using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformAccessRolesHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformAccessRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsSystem = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PlatformAccessRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GroupAccessRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformAccessGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformAccessRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_GroupAccessRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupAccessRoleAssignments_PlatformAccessGroups_PlatformAccessGroupId",
                        column: x => x.PlatformAccessGroupId,
                        principalTable: "PlatformAccessGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupAccessRoleAssignments_PlatformAccessRoles_PlatformAccessRoleId",
                        column: x => x.PlatformAccessRoleId,
                        principalTable: "PlatformAccessRoles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlatformRoleFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformAccessRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformFeatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PlatformRoleFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformRoleFeatures_PlatformAccessRoles_PlatformAccessRoleId",
                        column: x => x.PlatformAccessRoleId,
                        principalTable: "PlatformAccessRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlatformRoleFeatures_PlatformFeatures_PlatformFeatureId",
                        column: x => x.PlatformFeatureId,
                        principalTable: "PlatformFeatures",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupAccessRoleAssignments_PlatformAccessGroupId_PlatformAccessRoleId",
                table: "GroupAccessRoleAssignments",
                columns: new[] { "PlatformAccessGroupId", "PlatformAccessRoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupAccessRoleAssignments_PlatformAccessRoleId",
                table: "GroupAccessRoleAssignments",
                column: "PlatformAccessRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformAccessRoles_TenantId_Code",
                table: "PlatformAccessRoles",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoleFeatures_PlatformAccessRoleId_PlatformFeatureId",
                table: "PlatformRoleFeatures",
                columns: new[] { "PlatformAccessRoleId", "PlatformFeatureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoleFeatures_PlatformFeatureId",
                table: "PlatformRoleFeatures",
                column: "PlatformFeatureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupAccessRoleAssignments");

            migrationBuilder.DropTable(
                name: "PlatformRoleFeatures");

            migrationBuilder.DropTable(
                name: "PlatformAccessRoles");
        }
    }
}
