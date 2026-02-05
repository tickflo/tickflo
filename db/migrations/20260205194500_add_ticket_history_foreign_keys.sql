-- migrate:up
-- Add missing foreign key constraints to ticket_history table
-- Ensures referential integrity and proper cascade behavior

ALTER TABLE public.ticket_history
    ADD CONSTRAINT fk_ticket_history_ticket 
        FOREIGN KEY (ticket_id) REFERENCES public.tickets(id) ON DELETE CASCADE;

ALTER TABLE public.ticket_history
    ADD CONSTRAINT fk_ticket_history_workspace 
        FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id) ON DELETE CASCADE;

-- migrate:down
-- Remove the foreign key constraints for rollback

ALTER TABLE public.ticket_history
    DROP CONSTRAINT IF EXISTS fk_ticket_history_ticket;

ALTER TABLE public.ticket_history
    DROP CONSTRAINT IF EXISTS fk_ticket_history_workspace;
