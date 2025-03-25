ALTER TABLE "emails" ADD COLUMN "state" varchar(20) DEFAULT 'created' NOT NULL;--> statement-breakpoint
ALTER TABLE "emails" ADD COLUMN "state_updated_at" timestamp with time zone;--> statement-breakpoint
ALTER TABLE "emails" ADD COLUMN "bounce_description" varchar(254);