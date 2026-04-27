using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tickflo.Core.Migrations
{
    /// <inheritdoc />
    public partial class ExpandFileStorageSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "contact_id",
                table: "file_storage",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "file_storage",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "deleted_by_user_id",
                table: "file_storage",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "file_storage",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_public",
                table: "file_storage",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "metadata",
                table: "file_storage",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "related_entity_id",
                table: "file_storage",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "related_entity_type",
                table: "file_storage",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ticket_id",
                table: "file_storage",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_file_storage_related_entity_type_related_entity_id",
                table: "file_storage",
                columns: new[] { "related_entity_type", "related_entity_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_file_storage_related_entity_type_related_entity_id",
                table: "file_storage");

            migrationBuilder.DropColumn(
                name: "contact_id",
                table: "file_storage");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "file_storage");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "file_storage");

            migrationBuilder.DropColumn(
                name: "description",
                table: "file_storage");

            migrationBuilder.DropColumn(
                name: "is_public",
                table: "file_storage");

            migrationBuilder.DropColumn(
                name: "metadata",
                table: "file_storage");

            migrationBuilder.DropColumn(
                name: "related_entity_id",
                table: "file_storage");

            migrationBuilder.DropColumn(
                name: "related_entity_type",
                table: "file_storage");

            migrationBuilder.DropColumn(
                name: "ticket_id",
                table: "file_storage");
        }
    }
}
