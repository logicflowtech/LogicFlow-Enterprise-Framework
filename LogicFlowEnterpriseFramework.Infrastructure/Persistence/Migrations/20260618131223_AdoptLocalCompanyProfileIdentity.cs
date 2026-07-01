using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdoptLocalCompanyProfileIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CompanyProfiles",
                table: "CompanyProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CompanyProfiles_SourceModifiedDateTime",
                table: "CompanyProfiles");

            migrationBuilder.RenameColumn(
                name: "SourceCompanyId",
                table: "CompanyProfiles",
                newName: "MigratedId");

            migrationBuilder.AlterColumn<long>(
                name: "MigratedId",
                table: "CompanyProfiles",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "CompanyProfiles",
                type: "bigint",
                nullable: false)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CompanyProfiles",
                table: "CompanyProfiles",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_MigratedId",
                table: "CompanyProfiles",
                column: "MigratedId",
                unique: true,
                filter: "[MigratedId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_SourceModifiedDateTime",
                table: "CompanyProfiles",
                columns: new[] { "SourceModifiedDateTime", "MigratedId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_CompanyProfiles",
                table: "CompanyProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CompanyProfiles_MigratedId",
                table: "CompanyProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CompanyProfiles_SourceModifiedDateTime",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "CompanyProfiles");

            migrationBuilder.RenameColumn(
                name: "MigratedId",
                table: "CompanyProfiles",
                newName: "SourceCompanyId");

            migrationBuilder.AlterColumn<long>(
                name: "SourceCompanyId",
                table: "CompanyProfiles",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_CompanyProfiles",
                table: "CompanyProfiles",
                column: "SourceCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_SourceModifiedDateTime",
                table: "CompanyProfiles",
                columns: new[] { "SourceModifiedDateTime", "SourceCompanyId" });
        }
    }
}
