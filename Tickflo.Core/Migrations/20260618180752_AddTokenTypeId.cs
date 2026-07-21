#nullable disable

namespace Tickflo.Core.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;

/// <inheritdoc />
public partial class AddTokenTypeId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<int>(
            name: "type_id",
            table: "tokens",
            type: "integer",
            nullable: false,
            defaultValue: 1);

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
            name: "type_id",
            table: "tokens");
}
