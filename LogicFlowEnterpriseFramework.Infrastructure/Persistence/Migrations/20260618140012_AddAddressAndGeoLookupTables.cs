using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressAndGeoLookupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LookupCountries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    MigratedId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupCountries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    MigratedId = table.Column<long>(type: "bigint", nullable: true),
                    CountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LookupStates_LookupCountries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "LookupCountries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LookupCities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    MigratedId = table.Column<long>(type: "bigint", nullable: true),
                    CountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupCities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LookupCities_LookupCountries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "LookupCountries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LookupCities_LookupStates_StateId",
                        column: x => x.StateId,
                        principalTable: "LookupStates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                    MigratedId = table.Column<long>(type: "bigint", nullable: true),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AddressLine3 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CountryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CountryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Postcode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceCreatedByLegacyUserId = table.Column<int>(type: "int", nullable: true),
                    SourceCreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    SourceUpdatedByLegacyUserId = table.Column<int>(type: "int", nullable: true),
                    SourceUpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Addresses_LookupCities_CityId",
                        column: x => x.CityId,
                        principalTable: "LookupCities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Addresses_LookupCountries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "LookupCountries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Addresses_LookupStates_StateId",
                        column: x => x.StateId,
                        principalTable: "LookupStates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_CityId",
                table: "Addresses",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_CountryId",
                table: "Addresses",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_MigratedId",
                table: "Addresses",
                column: "MigratedId",
                unique: true,
                filter: "[MigratedId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_StateId",
                table: "Addresses",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_LookupCities_CountryId",
                table: "LookupCities",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_LookupCities_MigratedId",
                table: "LookupCities",
                column: "MigratedId",
                unique: true,
                filter: "[MigratedId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LookupCities_StateId",
                table: "LookupCities",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_LookupCountries_MigratedId",
                table: "LookupCountries",
                column: "MigratedId",
                unique: true,
                filter: "[MigratedId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LookupStates_CountryId",
                table: "LookupStates",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_LookupStates_MigratedId",
                table: "LookupStates",
                column: "MigratedId",
                unique: true,
                filter: "[MigratedId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "LookupCities");

            migrationBuilder.DropTable(
                name: "LookupStates");

            migrationBuilder.DropTable(
                name: "LookupCountries");
        }
    }
}
