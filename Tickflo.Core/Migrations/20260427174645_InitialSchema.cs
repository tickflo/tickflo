#nullable disable

namespace Tickflo.Core.Migrations;

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

/// <inheritdoc />
public partial class InitialSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "contact_locations",
            columns: table => new
            {
                contact_id = table.Column<int>(type: "integer", nullable: false),
                location_id = table.Column<int>(type: "integer", nullable: false),
                workspace_id = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_contact_locations", x => new { x.contact_id, x.location_id }));

        migrationBuilder.CreateTable(
            name: "contacts",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                email = table.Column<string>(type: "text", nullable: false),
                phone = table.Column<string>(type: "text", nullable: true),
                company = table.Column<string>(type: "text", nullable: true),
                title = table.Column<string>(type: "text", nullable: true),
                notes = table.Column<string>(type: "text", nullable: true),
                tags = table.Column<string>(type: "text", nullable: true),
                preferred_channel = table.Column<string>(type: "text", nullable: true),
                priority = table.Column<string>(type: "text", nullable: true),
                status = table.Column<string>(type: "text", nullable: true),
                assigned_user_id = table.Column<int>(type: "integer", nullable: true),
                last_interaction = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_contacts", x => x.id));

        migrationBuilder.CreateTable(
            name: "email_templates",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                template_type_id = table.Column<int>(type: "integer", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false),
                subject = table.Column<string>(type: "text", nullable: false),
                body = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<int>(type: "integer", nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_email_templates", x => x.id));

        migrationBuilder.CreateTable(
            name: "emails",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                template_id = table.Column<int>(type: "integer", nullable: false),
                vars = table.Column<string>(type: "json", nullable: true),
                from = table.Column<string>(type: "text", nullable: false),
                to = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<int>(type: "integer", nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<int>(type: "integer", nullable: true),
                sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                state = table.Column<string>(type: "text", nullable: true, defaultValueSql: "'created'::character varying"),
                error_message = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_emails", x => x.id));

        migrationBuilder.CreateTable(
            name: "file_storage",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                user_id = table.Column<int>(type: "integer", nullable: false),
                path = table.Column<string>(type: "text", nullable: false),
                file_name = table.Column<string>(type: "text", nullable: false),
                content_type = table.Column<string>(type: "text", nullable: false),
                size = table.Column<long>(type: "bigint", nullable: false),
                file_type = table.Column<string>(type: "text", nullable: false),
                category = table.Column<string>(type: "text", nullable: true),
                description = table.Column<string>(type: "text", nullable: true),
                public_url = table.Column<string>(type: "text", nullable: true),
                is_public = table.Column<bool>(type: "boolean", nullable: false),
                is_archived = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<int>(type: "integer", nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<int>(type: "integer", nullable: true),
                deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                deleted_by_user_id = table.Column<int>(type: "integer", nullable: true),
                metadata = table.Column<string>(type: "text", nullable: true),
                ticket_id = table.Column<int>(type: "integer", nullable: true),
                contact_id = table.Column<int>(type: "integer", nullable: true),
                related_entity_type = table.Column<string>(type: "text", nullable: true),
                related_entity_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_file_storage", x => x.id));

        migrationBuilder.CreateTable(
            name: "inventory",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                sku = table.Column<string>(type: "text", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                quantity = table.Column<int>(type: "integer", nullable: false),
                location_id = table.Column<int>(type: "integer", nullable: true),
                min_stock = table.Column<int>(type: "integer", nullable: true),
                cost = table.Column<decimal>(type: "numeric", nullable: false),
                price = table.Column<decimal>(type: "numeric", nullable: true),
                category = table.Column<string>(type: "text", nullable: true),
                tags = table.Column<string>(type: "text", nullable: true),
                status = table.Column<string>(type: "text", nullable: false),
                supplier = table.Column<string>(type: "text", nullable: true),
                last_restock_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_inventory", x => x.id));

        migrationBuilder.CreateTable(
            name: "locations",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                address = table.Column<string>(type: "text", nullable: false),
                active = table.Column<bool>(type: "boolean", nullable: false),
                default_assignee_user_id = table.Column<int>(type: "integer", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<int>(type: "integer", nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_locations", x => x.id));

        migrationBuilder.CreateTable(
            name: "notifications",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: true),
                user_id = table.Column<int>(type: "integer", nullable: false),
                type = table.Column<string>(type: "text", nullable: false),
                delivery_method = table.Column<string>(type: "text", nullable: false),
                priority = table.Column<string>(type: "text", nullable: false),
                subject = table.Column<string>(type: "text", nullable: false),
                body = table.Column<string>(type: "text", nullable: false),
                data = table.Column<string>(type: "text", nullable: true),
                status = table.Column<string>(type: "text", nullable: false),
                sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                failure_reason = table.Column<string>(type: "text", nullable: true),
                read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<int>(type: "integer", nullable: true),
                scheduled_for = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                batch_id = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_notifications", x => x.id));

        migrationBuilder.CreateTable(
            name: "permissions",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                resource = table.Column<string>(type: "text", nullable: false),
                action = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_permissions", x => x.id));

        migrationBuilder.CreateTable(
            name: "report_runs",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                report_id = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                row_count = table.Column<int>(type: "integer", nullable: false),
                file_path = table.Column<string>(type: "text", nullable: true),
                file_bytes = table.Column<byte[]>(type: "bytea", nullable: true),
                content_type = table.Column<string>(type: "text", nullable: true),
                file_name = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_report_runs", x => x.id));

        migrationBuilder.CreateTable(
            name: "reports",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                ready = table.Column<bool>(type: "boolean", nullable: false),
                last_run = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<int>(type: "integer", nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<int>(type: "integer", nullable: true),
                definition_json = table.Column<string>(type: "text", nullable: true),
                schedule_enabled = table.Column<bool>(type: "boolean", nullable: false),
                schedule_type = table.Column<string>(type: "text", nullable: false),
                schedule_time = table.Column<TimeSpan>(type: "interval", nullable: true),
                schedule_day_of_week = table.Column<int>(type: "integer", nullable: true),
                schedule_day_of_month = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_reports", x => x.id));

        migrationBuilder.CreateTable(
            name: "roles",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<int>(type: "integer", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<int>(type: "integer", nullable: true),
                admin = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_roles", x => x.id));

        migrationBuilder.CreateTable(
            name: "team_members",
            columns: table => new
            {
                team_id = table.Column<int>(type: "integer", nullable: false),
                user_id = table.Column<int>(type: "integer", nullable: false),
                joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_team_members", x => new { x.team_id, x.user_id }));

        migrationBuilder.CreateTable(
            name: "teams",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<int>(type: "integer", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_teams", x => x.id));

        migrationBuilder.CreateTable(
            name: "ticket_history",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                ticket_id = table.Column<int>(type: "integer", nullable: false),
                created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                action = table.Column<int>(type: "integer", nullable: false),
                field = table.Column<int>(type: "integer", nullable: true),
                old_value = table.Column<string>(type: "text", nullable: true),
                new_value = table.Column<string>(type: "text", nullable: true),
                note = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_ticket_history", x => x.id));

        migrationBuilder.CreateTable(
            name: "ticket_priorities",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                color = table.Column<string>(type: "text", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_ticket_priorities", x => x.id));

        migrationBuilder.CreateTable(
            name: "ticket_statuses",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                color = table.Column<string>(type: "text", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                is_closed_state = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_ticket_statuses", x => x.id));

        migrationBuilder.CreateTable(
            name: "ticket_types",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                color = table.Column<string>(type: "text", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_ticket_types", x => x.id));

        migrationBuilder.CreateTable(
            name: "tickets",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                contact_id = table.Column<int>(type: "integer", nullable: true),
                location_id = table.Column<int>(type: "integer", nullable: true),
                subject = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: false),
                ticket_type_id = table.Column<int>(type: "integer", nullable: true),
                priority_id = table.Column<int>(type: "integer", nullable: true),
                status_id = table.Column<int>(type: "integer", nullable: true),
                assigned_user_id = table.Column<int>(type: "integer", nullable: true),
                assigned_team_id = table.Column<int>(type: "integer", nullable: true),
                inventory_ref = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_tickets", x => x.id));

        migrationBuilder.CreateTable(
            name: "tokens",
            columns: table => new
            {
                user_id = table.Column<int>(type: "integer", nullable: false),
                token = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                max_age = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table => table.PrimaryKey("pk_tokens", x => new { x.user_id, x.token }));

        migrationBuilder.CreateTable(
            name: "user_email_changes",
            columns: table => new
            {
                user_id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                old = table.Column<string>(type: "text", nullable: false),
                @new = table.Column<string>(name: "new", type: "text", nullable: false),
                confirm_token = table.Column<string>(type: "text", nullable: false),
                undo_token = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<int>(type: "integer", nullable: false),
                confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                confirm_max_age = table.Column<int>(type: "integer", nullable: false),
                undo_max_age = table.Column<int>(type: "integer", nullable: false),
                undone_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_user_email_changes", x => x.user_id));

        migrationBuilder.CreateTable(
            name: "user_notification_preferences",
            columns: table => new
            {
                user_id = table.Column<int>(type: "integer", nullable: false),
                notification_type = table.Column<string>(type: "text", nullable: false),
                email_enabled = table.Column<bool>(type: "boolean", nullable: false),
                in_app_enabled = table.Column<bool>(type: "boolean", nullable: false),
                sms_enabled = table.Column<bool>(type: "boolean", nullable: false),
                push_enabled = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_user_notification_preferences", x => new { x.user_id, x.notification_type }));

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "text", nullable: false),
                email = table.Column<string>(type: "text", nullable: false),
                recoveryEmail = table.Column<string>(type: "text", nullable: true),
                system_admin = table.Column<bool>(type: "boolean", nullable: false),
                email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                email_confirmation_code = table.Column<string>(type: "text", nullable: true),
                password_hash = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<int>(type: "integer", nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table => table.PrimaryKey("pk_users", x => x.id));

        migrationBuilder.CreateTable(
            name: "role_permissions",
            columns: table => new
            {
                role_id = table.Column<int>(type: "integer", nullable: false),
                permission_id = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<int>(type: "integer", nullable: true),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_role_permissions", x => new { x.role_id, x.permission_id });
                table.ForeignKey(
                    name: "fk_role_permissions_roles_role_id",
                    column: x => x.role_id,
                    principalTable: "roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "user_workspace_roles",
            columns: table => new
            {
                user_id = table.Column<int>(type: "integer", nullable: false),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                role_id = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_workspace_roles", x => new { x.user_id, x.workspace_id, x.role_id });
                table.ForeignKey(
                    name: "fk_user_workspace_roles_roles_role_id",
                    column: x => x.role_id,
                    principalTable: "roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ticket_inventory",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ticket_id = table.Column<int>(type: "integer", nullable: false),
                inventory_id = table.Column<int>(type: "integer", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                unit_price = table.Column<decimal>(type: "numeric", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_ticket_inventory", x => x.id);
                table.ForeignKey(
                    name: "fk_ticket_inventory_inventory_inventory_id",
                    column: x => x.inventory_id,
                    principalTable: "inventory",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_ticket_inventory_tickets_ticket_id",
                    column: x => x.ticket_id,
                    principalTable: "tickets",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ticket_comments",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                ticket_id = table.Column<int>(type: "integer", nullable: false),
                created_by_user_id = table.Column<int>(type: "integer", nullable: false),
                created_by_contact_id = table.Column<int>(type: "integer", nullable: true),
                content = table.Column<string>(type: "text", nullable: false),
                is_visible_to_client = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by_user_id = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_ticket_comments", x => x.id);
                table.ForeignKey(
                    name: "fk_ticket_comments_tickets_ticket_id",
                    column: x => x.ticket_id,
                    principalTable: "tickets",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_ticket_comments_users_created_by_user_id",
                    column: x => x.created_by_user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_ticket_comments_users_updated_by_user_id",
                    column: x => x.updated_by_user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "workspaces",
            columns: table => new
            {
                id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                name = table.Column<string>(type: "text", nullable: false),
                slug = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<int>(type: "integer", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_workspaces", x => x.id);
                table.ForeignKey(
                    name: "fk_workspaces_users_created_by",
                    column: x => x.created_by,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "user_workspaces",
            columns: table => new
            {
                user_id = table.Column<int>(type: "integer", nullable: false),
                workspace_id = table.Column<int>(type: "integer", nullable: false),
                accepted = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_by = table.Column<int>(type: "integer", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                updated_by = table.Column<int>(type: "integer", nullable: true),
                role = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_workspaces", x => new { x.user_id, x.workspace_id });
                table.ForeignKey(
                    name: "fk_user_workspaces_workspaces_workspace_id",
                    column: x => x.workspace_id,
                    principalTable: "workspaces",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_contact_locations_workspace_id_contact_id",
            table: "contact_locations",
            columns: ["workspace_id", "contact_id"]);

        migrationBuilder.CreateIndex(
            name: "ix_contacts_workspace_id_email",
            table: "contacts",
            columns: ["workspace_id", "email"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_file_storage_path",
            table: "file_storage",
            column: "path");

        migrationBuilder.CreateIndex(
            name: "ix_file_storage_related_entity_type_related_entity_id",
            table: "file_storage",
            columns: ["related_entity_type", "related_entity_id"]);

        migrationBuilder.CreateIndex(
            name: "ix_file_storage_workspace_id_category",
            table: "file_storage",
            columns: ["workspace_id", "category"]);

        migrationBuilder.CreateIndex(
            name: "ix_file_storage_workspace_id_created_at",
            table: "file_storage",
            columns: ["workspace_id", "created_at"]);

        migrationBuilder.CreateIndex(
            name: "ix_inventory_workspace_id_sku",
            table: "inventory",
            columns: ["workspace_id", "sku"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_locations_workspace_id_name",
            table: "locations",
            columns: ["workspace_id", "name"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_permissions_action_resource",
            table: "permissions",
            columns: ["action", "resource"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_report_runs_workspace_id_report_id_started_at",
            table: "report_runs",
            columns: ["workspace_id", "report_id", "started_at"],
            descending: [false, false, true]);

        migrationBuilder.CreateIndex(
            name: "ix_reports_workspace_id_name",
            table: "reports",
            columns: ["workspace_id", "name"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_roles_workspace_id_name",
            table: "roles",
            columns: ["workspace_id", "name"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_teams_workspace_id_name",
            table: "teams",
            columns: ["workspace_id", "name"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_ticket_comments_created_by_user_id",
            table: "ticket_comments",
            column: "created_by_user_id");

        migrationBuilder.CreateIndex(
            name: "ix_ticket_comments_ticket_id",
            table: "ticket_comments",
            column: "ticket_id");

        migrationBuilder.CreateIndex(
            name: "ix_ticket_comments_updated_by_user_id",
            table: "ticket_comments",
            column: "updated_by_user_id");

        migrationBuilder.CreateIndex(
            name: "ix_ticket_comments_workspace_id_ticket_id_created_at",
            table: "ticket_comments",
            columns: ["workspace_id", "ticket_id", "created_at"]);

        migrationBuilder.CreateIndex(
            name: "ix_ticket_history_workspace_id_ticket_id_created_at",
            table: "ticket_history",
            columns: ["workspace_id", "ticket_id", "created_at"]);

        migrationBuilder.CreateIndex(
            name: "ix_ticket_inventory_inventory_id",
            table: "ticket_inventory",
            column: "inventory_id");

        migrationBuilder.CreateIndex(
            name: "ix_ticket_inventory_ticket_id",
            table: "ticket_inventory",
            column: "ticket_id");

        migrationBuilder.CreateIndex(
            name: "ix_ticket_priorities_workspace_id_name",
            table: "ticket_priorities",
            columns: ["workspace_id", "name"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_ticket_statuses_workspace_id_name",
            table: "ticket_statuses",
            columns: ["workspace_id", "name"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_ticket_types_workspace_id_name",
            table: "ticket_types",
            columns: ["workspace_id", "name"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_user_workspace_roles_role_id",
            table: "user_workspace_roles",
            column: "role_id");

        migrationBuilder.CreateIndex(
            name: "ix_user_workspaces_workspace_id",
            table: "user_workspaces",
            column: "workspace_id");

        migrationBuilder.CreateIndex(
            name: "ix_workspaces_created_by",
            table: "workspaces",
            column: "created_by");

        migrationBuilder.CreateIndex(
            name: "ix_workspaces_slug",
            table: "workspaces",
            column: "slug",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "contact_locations");

        migrationBuilder.DropTable(
            name: "contacts");

        migrationBuilder.DropTable(
            name: "email_templates");

        migrationBuilder.DropTable(
            name: "emails");

        migrationBuilder.DropTable(
            name: "file_storage");

        migrationBuilder.DropTable(
            name: "locations");

        migrationBuilder.DropTable(
            name: "notifications");

        migrationBuilder.DropTable(
            name: "permissions");

        migrationBuilder.DropTable(
            name: "report_runs");

        migrationBuilder.DropTable(
            name: "reports");

        migrationBuilder.DropTable(
            name: "role_permissions");

        migrationBuilder.DropTable(
            name: "team_members");

        migrationBuilder.DropTable(
            name: "teams");

        migrationBuilder.DropTable(
            name: "ticket_comments");

        migrationBuilder.DropTable(
            name: "ticket_history");

        migrationBuilder.DropTable(
            name: "ticket_inventory");

        migrationBuilder.DropTable(
            name: "ticket_priorities");

        migrationBuilder.DropTable(
            name: "ticket_statuses");

        migrationBuilder.DropTable(
            name: "ticket_types");

        migrationBuilder.DropTable(
            name: "tokens");

        migrationBuilder.DropTable(
            name: "user_email_changes");

        migrationBuilder.DropTable(
            name: "user_notification_preferences");

        migrationBuilder.DropTable(
            name: "user_workspace_roles");

        migrationBuilder.DropTable(
            name: "user_workspaces");

        migrationBuilder.DropTable(
            name: "inventory");

        migrationBuilder.DropTable(
            name: "tickets");

        migrationBuilder.DropTable(
            name: "roles");

        migrationBuilder.DropTable(
            name: "workspaces");

        migrationBuilder.DropTable(
            name: "users");
    }
}
