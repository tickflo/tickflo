#nullable disable

namespace Tickflo.Core.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

/// <inheritdoc />
public partial class AddWidget : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "widgets",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                type_id = table.Column<int>(type: "integer", nullable: false),
                url = table.Column<string>(type: "text", nullable: false),
                api_key_ciphertext = table.Column<string>(type: "text", nullable: true),
                config_json = table.Column<string>(type: "text", nullable: false),
                is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                refresh_interval_seconds = table.Column<int>(type: "integer", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<int>(type: "integer", nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_widgets", x => x.id);
                table.ForeignKey(
                    name: "fk_widgets_workspaces_workspace_id",
                    column: x => x.workspace_id,
                    principalTable: "workspaces",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "ix_widgets_workspace_id_sort_order",
            table: "widgets",
            columns: ["workspace_id", "sort_order"]);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(
        name: "widgets");
}
