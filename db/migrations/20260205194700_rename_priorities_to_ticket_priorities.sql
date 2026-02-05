-- migrate:up
-- Rename priorities table to ticket_priorities for naming consistency
-- Aligns with other ticket-related tables (ticket_statuses, ticket_types)

ALTER TABLE public.priorities RENAME TO ticket_priorities;

-- migrate:down
-- Revert table name for rollback

ALTER TABLE public.ticket_priorities RENAME TO priorities;
