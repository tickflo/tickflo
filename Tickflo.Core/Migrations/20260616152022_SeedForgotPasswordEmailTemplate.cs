#nullable disable

namespace Tickflo.Core.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class SeedForgotPasswordEmailTemplate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // The session-token middleware looks tokens up by value on every
        // authenticated request. The composite PK (user_id, token) does
        // not help that query, so add a unique index on the value column.
        migrationBuilder.CreateIndex(
            name: "ix_tokens_token",
            table: "tokens",
            column: "token",
            unique: true);

        migrationBuilder.InsertData(
            table: "email_templates",
            columns: ["template_type_id", "version", "subject", "body", "created_at", "created_by", "updated_at", "updated_by"],
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

        migrationBuilder.DropIndex(
            name: "ix_tokens_token",
            table: "tokens");
    }
}
