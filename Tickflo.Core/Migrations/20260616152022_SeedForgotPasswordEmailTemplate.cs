using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tickflo.Core.Migrations
{
    /// <inheritdoc />
    public partial class SeedForgotPasswordEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows pre-date the TokenType concept. Default them
            // all to Login (1) so the auth middleware can still resolve
            // them as session tokens.
            migrationBuilder.AddColumn<int>(
                name: "type_id",
                table: "tokens",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.InsertData(
                table: "email_templates",
                columns: new[] { "template_type_id", "version", "subject", "body", "created_at", "created_by", "updated_at", "updated_by" },
                values: new object[,]
                {
                    {
                        2, // EmailTemplateType.ForgotPassword
                        1,
                        "Reset your Tickflo password",
                        "Hi {{recipient_name}},\n\nWe received a request to reset your Tickflo password. Click the link below to choose a new one. This link will expire in {{expires_in}}.\n\n{{reset_link}}\n\nIf you didn't request this, you can ignore this email — your password will stay the same.\n\nThanks,\nThe Tickflo team",
                        new DateTime(2026, 6, 16, 14, 0, 0, DateTimeKind.Utc),
                        null,
                        null,
                        null
                    },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM email_templates WHERE template_type_id = 2 AND version = 1;");

            migrationBuilder.DropColumn(
                name: "type_id",
                table: "tokens");
        }
    }
}
