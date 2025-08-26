using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Data.Migrations.Auth
{
    /// <inheritdoc />
    public partial class InitialSetup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginDate",
                schema: "auth",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordChangeRequiredDate",
                schema: "auth",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientId",
                schema: "auth",
                table: "AuthenticationAuditLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                schema: "auth",
                table: "AuthenticationAuditLogs",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLoginDate",
                schema: "auth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PasswordChangeRequiredDate",
                schema: "auth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ClientId",
                schema: "auth",
                table: "AuthenticationAuditLogs");

            migrationBuilder.DropColumn(
                name: "UserEmail",
                schema: "auth",
                table: "AuthenticationAuditLogs");
        }
    }
}
