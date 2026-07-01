using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyServiceCenterRoutingMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceCenterUserAccesses_ServiceTeams_ServiceTeamId",
                table: "ServiceCenterUserAccesses");

            migrationBuilder.DropTable(
                name: "ServiceQueueMemberships");

            migrationBuilder.DropTable(
                name: "ServiceTeams");

            migrationBuilder.DropIndex(
                name: "IX_ServiceCenterUserAccesses_ServiceTeamId",
                table: "ServiceCenterUserAccesses");

            migrationBuilder.DropColumn(
                name: "ServiceCenterRole",
                table: "ServiceCenterUserAccesses");

            migrationBuilder.DropColumn(
                name: "ServiceTeamId",
                table: "ServiceCenterUserAccesses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ServiceCenterRole",
                table: "ServiceCenterUserAccesses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceTeamId",
                table: "ServiceCenterUserAccesses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceQueueMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsEscalationOwner = table.Column<bool>(type: "bit", nullable: false),
                    IsPrimaryOwner = table.Column<bool>(type: "bit", nullable: false),
                    QueueName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceQueueMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceQueueMemberships_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiceTeams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTeams", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCenterUserAccesses_ServiceTeamId",
                table: "ServiceCenterUserAccesses",
                column: "ServiceTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceQueueMemberships_ApplicationUserId",
                table: "ServiceQueueMemberships",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTeams_TenantId_Code",
                table: "ServiceTeams",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceCenterUserAccesses_ServiceTeams_ServiceTeamId",
                table: "ServiceCenterUserAccesses",
                column: "ServiceTeamId",
                principalTable: "ServiceTeams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
