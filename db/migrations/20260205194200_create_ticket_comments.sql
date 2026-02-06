-- migrate:up
-- Create missing ticket_comments table to match TicketComment entity

CREATE TABLE public.ticket_comments (
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY,
    workspace_id integer NOT NULL,
    ticket_id integer NOT NULL,
    created_by_user_id integer NOT NULL,
    created_by_contact_id integer,
    content text NOT NULL,
    is_visible_to_client boolean NOT NULL DEFAULT false,
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone,
    updated_by_user_id integer,
    PRIMARY KEY (id),
    FOREIGN KEY (workspace_id) REFERENCES public.workspaces(id) ON DELETE CASCADE,
    FOREIGN KEY (ticket_id) REFERENCES public.tickets(id) ON DELETE CASCADE,
    FOREIGN KEY (created_by_user_id) REFERENCES public.users(id) ON DELETE RESTRICT,
    FOREIGN KEY (updated_by_user_id) REFERENCES public.users(id) ON DELETE SET NULL,
    FOREIGN KEY (created_by_contact_id) REFERENCES public.contacts(id) ON DELETE SET NULL
);

-- Create composite index for efficient queries by workspace, ticket, and time
CREATE INDEX idx_ticket_comments_ws_ticket_time 
    ON public.ticket_comments(workspace_id, ticket_id, created_at);

-- Create index for queries by ticket
CREATE INDEX idx_ticket_comments_ticket_id 
    ON public.ticket_comments(ticket_id);

-- migrate:down
DROP TABLE IF EXISTS public.ticket_comments CASCADE;
