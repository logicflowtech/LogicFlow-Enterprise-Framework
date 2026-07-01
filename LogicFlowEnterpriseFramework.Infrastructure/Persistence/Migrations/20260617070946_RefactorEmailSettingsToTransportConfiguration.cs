using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogicFlowEnterpriseFramework.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefactorEmailSettingsToTransportConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FooterSignature",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "NotificationsEnabled",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "SendAssignmentAlerts",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "SendDailyDigest",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "SendRequestCreated",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "SendSlaBreachAlerts",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "SenderDisplayName",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "SendSlaWarningAlerts",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "SubjectPrefix",
                table: "EmailSettings");

            migrationBuilder.AlterColumn<string>(
                name: "SenderEmail",
                table: "EmailSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<bool>(
                name: "EnableSsl",
                table: "EmailSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedPassword",
                table: "EmailSettings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Host",
                table: "EmailSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Port",
                table: "EmailSettings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provider",
                table: "EmailSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Smtp");

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "EmailSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncryptedPassword",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "Host",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "Port",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "EmailSettings");

            migrationBuilder.DropColumn(
                name: "EnableSsl",
                table: "EmailSettings");

            migrationBuilder.AlterColumn<string>(
                name: "SenderEmail",
                table: "EmailSettings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FooterSignature",
                table: "EmailSettings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotificationsEnabled",
                table: "EmailSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SendAssignmentAlerts",
                table: "EmailSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SendDailyDigest",
                table: "EmailSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SendRequestCreated",
                table: "EmailSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SendSlaBreachAlerts",
                table: "EmailSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SendSlaWarningAlerts",
                table: "EmailSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SenderDisplayName",
                table: "EmailSettings",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubjectPrefix",
                table: "EmailSettings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);
        }
    }
}
