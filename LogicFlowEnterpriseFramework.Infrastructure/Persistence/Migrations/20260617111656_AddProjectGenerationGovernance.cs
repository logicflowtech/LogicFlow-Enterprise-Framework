using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectGenerationGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProjectLifecycleStatus",
                table: "PlatformApplications",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.CreateTable(
                name: "ProjectGenerationHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Template = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TemplateVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RootNamespace = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RelativeOutputDirectory = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    OutputDirectory = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ProjectFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DatabaseMode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DatabaseProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    GenerateSharedManifest = table.Column<bool>(type: "bit", nullable: false),
                    LocalManifestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SharedManifestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AddedToSolution = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ProjectGenerationHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectGenerationHistories_PlatformApplications_PlatformApplicationId",
                        column: x => x.PlatformApplicationId,
                        principalTable: "PlatformApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectGenerationHistories_PlatformApplicationId_CreatedAt",
                table: "ProjectGenerationHistories",
                columns: new[] { "PlatformApplicationId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectGenerationHistories");

            migrationBuilder.DropColumn(
                name: "ProjectLifecycleStatus",
                table: "PlatformApplications");
        }
    }
}
