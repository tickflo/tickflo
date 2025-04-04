CREATE TABLE "user_email_changes" (
	"user_id" integer NOT NULL,
	"old" varchar(254) NOT NULL,
	"new" varchar(254) NOT NULL,
	"confirm_token" varchar(100) NOT NULL,
	"undo_token" varchar(100) NOT NULL,
	"created_at" timestamp with time zone DEFAULT now() NOT NULL,
	"confirm_max_age" integer NOT NULL,
	"undo_max_age" integer NOT NULL
);
--> statement-breakpoint
ALTER TABLE "user_email_changes" ADD CONSTRAINT "user_email_changes_user_id_users_id_fk" FOREIGN KEY ("user_id") REFERENCES "public"."users"("id") ON DELETE no action ON UPDATE no action;