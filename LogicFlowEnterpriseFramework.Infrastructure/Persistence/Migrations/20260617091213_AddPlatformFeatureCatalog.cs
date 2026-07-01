using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformFeatureCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PlatformModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformModules_PlatformApplications_PlatformApplicationId",
                        column: x => x.PlatformApplicationId,
                        principalTable: "PlatformApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlatformAccessGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_PlatformAccessGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformAccessGroups_PlatformApplications_PlatformApplicationId",
                        column: x => x.PlatformApplicationId,
                        principalTable: "PlatformApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlatformAccessGroups_PlatformModules_PlatformModuleId",
                        column: x => x.PlatformModuleId,
                        principalTable: "PlatformModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "PlatformFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeprecated = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PlatformFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformFeatures_PlatformApplications_PlatformApplicationId",
                        column: x => x.PlatformApplicationId,
                        principalTable: "PlatformApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlatformFeatures_PlatformModules_PlatformModuleId",
                        column: x => x.PlatformModuleId,
                        principalTable: "PlatformModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "UserAccessGroupAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformAccessGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_UserAccessGroupAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccessGroupAssignments_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAccessGroupAssignments_PlatformAccessGroups_PlatformAccessGroupId",
                        column: x => x.PlatformAccessGroupId,
                        principalTable: "PlatformAccessGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAccessGroupAssignments_PlatformApplications_PlatformApplicationId",
                        column: x => x.PlatformApplicationId,
                        principalTable: "PlatformApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "PlatformGroupFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformAccessGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_PlatformGroupFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformGroupFeatures_PlatformAccessGroups_PlatformAccessGroupId",
                        column: x => x.PlatformAccessGroupId,
                        principalTable: "PlatformAccessGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlatformGroupFeatures_PlatformFeatures_PlatformFeatureId",
                        column: x => x.PlatformFeatureId,
                        principalTable: "PlatformFeatures",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

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
                name: "IX_PlatformGroupFeatures_PlatformAccessGroupId_PlatformFeatureId",
                table: "PlatformGroupFeatures",
                columns: new[] { "PlatformAccessGroupId", "PlatformFeatureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformGroupFeatures_PlatformFeatureId",
                table: "PlatformGroupFeatures",
                column: "PlatformFeatureId");

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
                name: "IX_UserAccessGroupAssignments_ApplicationUserId_PlatformAccessGroupId",
                table: "UserAccessGroupAssignments",
                columns: new[] { "ApplicationUserId", "PlatformAccessGroupId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessGroupAssignments_PlatformAccessGroupId",
                table: "UserAccessGroupAssignments",
                column: "PlatformAccessGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessGroupAssignments_PlatformApplicationId",
                table: "UserAccessGroupAssignments",
                column: "PlatformApplicationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformGroupFeatures");

            migrationBuilder.DropTable(
                name: "UserAccessGroupAssignments");

            migrationBuilder.DropTable(
                name: "PlatformFeatures");

            migrationBuilder.DropTable(
                name: "PlatformAccessGroups");

            migrationBuilder.DropTable(
                name: "PlatformModules");
        }
    }
}
