using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Data.Migrations.Auth
{
    /// <inheritdoc />
    public partial class RefactorToIdentityProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthenticationAuditLogs_Tenants_TenantId",
                schema: "auth",
                table: "AuthenticationAuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Roles_Tenants_TenantId",
                schema: "auth",
                table: "Roles");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                schema: "auth",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "auth");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId",
                schema: "auth",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_NormalizedEmail",
                schema: "auth",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_NormalizedUserName",
                schema: "auth",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Roles_TenantId",
                schema: "auth",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Roles_TenantId_NormalizedName",
                schema: "auth",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_AuthenticationAuditLogs_Tenant_User_Timestamp",
                schema: "auth",
                table: "AuthenticationAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuthenticationAuditLogs_TenantId",
                schema: "auth",
                table: "AuthenticationAuditLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "auth",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "auth",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                schema: "auth",
                table: "AuthenticationAuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationAuditLogs_User_Timestamp",
                schema: "auth",
                table: "AuthenticationAuditLogs",
                columns: new[] { "UserId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuthenticationAuditLogs_User_Timestamp",
                schema: "auth",
                table: "AuthenticationAuditLogs");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "auth",
                table: "Users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "auth",
                table: "Roles",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                schema: "auth",
                table: "AuthenticationAuditLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminEmail = table.Column<string>(type: "text", nullable: true),
                    AllowConcurrentSessions = table.Column<bool>(type: "boolean", nullable: false),
                    AllowedOrigins = table.Column<string>(type: "text", nullable: true),
                    AllowedPostLogoutRedirectUris = table.Column<string>(type: "text", nullable: true),
                    AllowedRedirectUris = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    DeactivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Domain = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutDurationMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    MaxConcurrentSessions = table.Column<int>(type: "integer", nullable: false),
                    MaxFailedAccessAttempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    MaxPasswordAge = table.Column<int>(type: "integer", nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: true),
                    MinPasswordLength = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PasswordExpirationDays = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PasswordHistoryCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PrimaryColor = table.Column<string>(type: "text", nullable: true),
                    ProhibitedPasswordPatterns = table.Column<string>(type: "text", nullable: true),
                    RequireDigit = table.Column<bool>(type: "boolean", nullable: false),
                    RequireEmailConfirmation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RequireLowercase = table.Column<bool>(type: "boolean", nullable: false),
                    RequireNonAlphanumeric = table.Column<bool>(type: "boolean", nullable: false),
                    RequireTwoFactor = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RequireUniqueChars = table.Column<int>(type: "integer", nullable: false),
                    RequireUppercase = table.Column<bool>(type: "boolean", nullable: false),
                    SecondaryColor = table.Column<string>(type: "text", nullable: true),
                    SessionTimeoutMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    Subdomain = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubscriptionExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SubscriptionPlan = table.Column<string>(type: "text", nullable: true),
                    SupportEmail = table.Column<string>(type: "text", nullable: true),
                    SupportPhone = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId",
                schema: "auth",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_NormalizedEmail",
                schema: "auth",
                table: "Users",
                columns: new[] { "TenantId", "NormalizedEmail" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_NormalizedUserName",
                schema: "auth",
                table: "Users",
                columns: new[] { "TenantId", "NormalizedUserName" },
                unique: true);

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
                name: "IX_Tenants_Subdomain",
                schema: "auth",
                table: "Tenants",
                column: "Subdomain",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AuthenticationAuditLogs_Tenants_TenantId",
                schema: "auth",
                table: "AuthenticationAuditLogs",
                column: "TenantId",
                principalSchema: "auth",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Roles_Tenants_TenantId",
                schema: "auth",
                table: "Roles",
                column: "TenantId",
                principalSchema: "auth",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                schema: "auth",
                table: "Users",
                column: "TenantId",
                principalSchema: "auth",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
