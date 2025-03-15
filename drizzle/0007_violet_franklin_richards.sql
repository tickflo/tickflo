ALTER TABLE "roles" RENAME COLUMN "role" TO "name";--> statement-breakpoint
ALTER TABLE "roles" ADD COLUMN "admin" boolean DEFAULT false NOT NULL;