ALTER TABLE "email_templates" DROP CONSTRAINT "email_templates_workspace_id_template_type_id_unique";--> statement-breakpoint
ALTER TABLE "email_templates" ALTER COLUMN "id" SET GENERATED ALWAYS;--> statement-breakpoint
ALTER TABLE "email_templates" ADD CONSTRAINT "email_templates_workspace_id_template_type_id_unique" UNIQUE("workspace_id","template_type_id");