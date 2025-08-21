using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Data.Migrations.Auth
{
    /// <inheritdoc />
    public partial class AddSecurityTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsecutiveLockouts",
                schema: "auth",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Domain",
                schema: "auth",
                table: "Tenants",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "MaxPasswordAge",
                schema: "auth",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProhibitedPasswordPatterns",
                schema: "auth",
                table: "Tenants",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequireUniqueChars",
                schema: "auth",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "auth",
                table: "Roles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "auth",
                table: "Roles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemRole",
                schema: "auth",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "auth",
                table: "Roles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "auth",
                table: "Roles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuthenticationAuditLogs",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AdditionalData = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthenticationAuditLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "auth",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthenticationAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordHistories",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordHistories_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "auth",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TenantId",
                schema: "auth",
                table: "Roles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_TenantId_NormalizedName",
                schema: "auth",
                table: "Roles",
                columns: new[] { "TenantId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationAuditLogs_Tenant_User_Timestamp",
                schema: "auth",
                table: "AuthenticationAuditLogs",
                columns: new[] { "TenantId", "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationAuditLogs_TenantId",
                schema: "auth",
                table: "AuthenticationAuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationAuditLogs_Timestamp",
                schema: "auth",
                table: "AuthenticationAuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationAuditLogs_UserId",
                schema: "auth",
                table: "AuthenticationAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistories_UserId",
                schema: "auth",
                table: "PasswordHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistories_UserId_CreatedAt",
                schema: "auth",
                table: "PasswordHistories",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Tenants_TenantId",
                schema: "auth",
                table: "Roles",
                column: "TenantId",
                principalSchema: "auth",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Tenants_TenantId",
                schema: "auth",
                table: "Roles");

            migrationBuilder.DropTable(
                name: "AuthenticationAuditLogs",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "PasswordHistories",
                schema: "auth");

            migrationBuilder.DropIndex(
                name: "IX_Roles_TenantId",
                schema: "auth",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Roles_TenantId_NormalizedName",
                schema: "auth",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "ConsecutiveLockouts",
                schema: "auth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Domain",
                schema: "auth",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "MaxPasswordAge",
                schema: "auth",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ProhibitedPasswordPatterns",
                schema: "auth",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "RequireUniqueChars",
                schema: "auth",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "auth",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "auth",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "IsSystemRole",
                schema: "auth",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "auth",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "auth",
                table: "Roles");
        }
    }
}
