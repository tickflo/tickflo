-- migrate:up
-- Add missing foreign key constraints to file_storage table
-- Ensures referential integrity for workspace and user relationships

ALTER TABLE public.file_storage
    ADD CONSTRAINT fk_file_storage_workspace 
        FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id) ON DELETE CASCADE;

ALTER TABLE public.file_storage
    ADD CONSTRAINT fk_file_storage_user 
        FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE RESTRICT;

ALTER TABLE public.file_storage
    ADD CONSTRAINT fk_file_storage_created_by 
        FOREIGN KEY (created_by) REFERENCES public.users(id) ON DELETE SET NULL;

ALTER TABLE public.file_storage
    ADD CONSTRAINT fk_file_storage_updated_by 
        FOREIGN KEY (updated_by) REFERENCES public.users(id) ON DELETE SET NULL;

-- migrate:down
-- Remove the foreign key constraints for rollback

ALTER TABLE public.file_storage
    DROP CONSTRAINT IF EXISTS fk_file_storage_workspace;

ALTER TABLE public.file_storage
    DROP CONSTRAINT IF EXISTS fk_file_storage_user;

ALTER TABLE public.file_storage
    DROP CONSTRAINT IF EXISTS fk_file_storage_created_by;

ALTER TABLE public.file_storage
    DROP CONSTRAINT IF EXISTS fk_file_storage_updated_by;
