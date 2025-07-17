ALTER TABLE "portal_questions" ALTER COLUMN "label" SET NOT NULL;--> statement-breakpoint
ALTER TABLE "portal_questions" ADD COLUMN "field_id" integer;