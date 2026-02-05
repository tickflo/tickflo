-- migrate:up
-- Add missing index on file_storage.path column
-- Supports efficient file retrieval queries by path

CREATE INDEX idx_file_storage_path 
    ON public.file_storage(path);

-- migrate:down
-- Remove the index for rollback

DROP INDEX IF EXISTS public.idx_file_storage_path;
