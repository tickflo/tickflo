-- migrate:up
-- Standardize all timestamp columns to use 'timestamp with time zone' (timestamptz)
-- This ensures consistent timezone handling across the database

-- contacts.last_interaction: Change from timestamp to timestamptz
ALTER TABLE public.contacts 
    ALTER COLUMN last_interaction TYPE timestamp with time zone;

-- file_storage.created_at: Change from timestamp to timestamptz
ALTER TABLE public.file_storage 
    ALTER COLUMN created_at TYPE timestamp with time zone;

-- file_storage.updated_at: Change from timestamp to timestamptz
ALTER TABLE public.file_storage 
    ALTER COLUMN updated_at TYPE timestamp with time zone;

-- migrate:down
-- Revert to timestamp without time zone for rollback

ALTER TABLE public.contacts 
    ALTER COLUMN last_interaction TYPE timestamp without time zone;

ALTER TABLE public.file_storage 
    ALTER COLUMN created_at TYPE timestamp without time zone;

ALTER TABLE public.file_storage 
    ALTER COLUMN updated_at TYPE timestamp without time zone;
