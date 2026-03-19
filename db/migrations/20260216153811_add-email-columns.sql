-- migrate:up
ALTER TABLE
    public.emails
ADD
    COLUMN sent_at timestamp with time zone NULL;

ALTER TABLE
    public.emails RENAME COLUMN bounce_description TO error_message;

ALTER TABLE
    public.emails DROP COLUMN IF EXISTS state_updated_at;

-- migrate:down
ALTER TABLE
    public.emails DROP COLUMN sent_at;

ALTER TABLE
    public.emails RENAME COLUMN error_message TO bounce_description;

ALTER TABLE
    public.emails
ADD
    COLUMN state_updated_at timestamp with time zone NULL;