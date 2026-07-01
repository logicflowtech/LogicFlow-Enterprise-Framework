using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LinkCompanyProfilesToLocalAddresses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LegacyAddressId",
                table: "CompanyProfiles",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE dbo.CompanyProfiles
                SET LegacyAddressId = AddressId
                WHERE AddressId IS NOT NULL;
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "LocalAddressId",
                table: "CompanyProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE company
                SET LocalAddressId = address.Id
                FROM dbo.CompanyProfiles AS company
                INNER JOIN dbo.Addresses AS address ON address.MigratedId = company.LegacyAddressId
                WHERE company.LegacyAddressId IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "CompanyProfiles");

            migrationBuilder.RenameColumn(
                name: "LocalAddressId",
                table: "CompanyProfiles",
                newName: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProfiles_AddressId",
                table: "CompanyProfiles",
                column: "AddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyProfiles_Addresses_AddressId",
                table: "CompanyProfiles",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyProfiles_Addresses_AddressId",
                table: "CompanyProfiles");

            migrationBuilder.DropIndex(
                name: "IX_CompanyProfiles_AddressId",
                table: "CompanyProfiles");

            migrationBuilder.AddColumn<long>(
                name: "LegacyAddressKey",
                table: "CompanyProfiles",
                type: "bigint",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE dbo.CompanyProfiles
                SET LegacyAddressKey = LegacyAddressId
                WHERE LegacyAddressId IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "CompanyProfiles");

            migrationBuilder.RenameColumn(
                name: "LegacyAddressKey",
                table: "CompanyProfiles",
                newName: "AddressId");

            migrationBuilder.DropColumn(
                name: "LegacyAddressId",
                table: "CompanyProfiles");
        }
    }
}
