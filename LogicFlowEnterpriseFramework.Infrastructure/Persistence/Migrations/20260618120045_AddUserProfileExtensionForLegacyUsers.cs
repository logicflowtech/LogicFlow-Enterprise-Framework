using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileExtensionForLegacyUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastLoginAt",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LegacyUserId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserProfiles",
                columns: table => new
                {
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nric = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    PassportNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LegacyIdentificationTypeId = table.Column<int>(type: "int", nullable: true),
                    CustomDesignationName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FaxNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LegacyAddressId = table.Column<long>(type: "bigint", nullable: true),
                    LegacyDesignationId = table.Column<int>(type: "int", nullable: true),
                    LegacyTitleId = table.Column<int>(type: "int", nullable: true),
                    TitleDisplayName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceCreatedByLegacyUserId = table.Column<int>(type: "int", nullable: true),
                    SourceCreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    SourceUpdatedByLegacyUserId = table.Column<int>(type: "int", nullable: true),
                    SourceUpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProfiles", x => x.ApplicationUserId);
                    table.ForeignKey(
                        name: "FK_UserProfiles_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_LegacyUserId",
                table: "AspNetUsers",
                column: "LegacyUserId",
                unique: true,
                filter: "[LegacyUserId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_LegacyUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LegacyUserId",
                table: "AspNetUsers");
        }
    }
}
