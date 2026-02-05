-- migrate:up
-- Remove duplicate ticket_inventories table (keeping ticket_inventory as the canonical table)
-- The entity TicketInventory maps to ticket_inventory (singular) by EF Core convention

-- First, migrate any data from ticket_inventories to ticket_inventory if it exists
INSERT INTO public.ticket_inventory (ticket_id, inventory_id, quantity, unit_price)
SELECT ticket_id, inventory_id, quantity, unit_price
FROM public.ticket_inventories
WHERE NOT EXISTS (
    SELECT 1 FROM public.ticket_inventory ti 
    WHERE ti.ticket_id = ticket_inventories.ticket_id 
    AND ti.inventory_id = ticket_inventories.inventory_id
);

-- Drop the duplicate table (will cascade drop foreign keys and indexes)
DROP TABLE IF EXISTS public.ticket_inventories CASCADE;

-- migrate:down
-- Recreate the ticket_inventories table (for rollback purposes)
CREATE TABLE public.ticket_inventories (
    id integer NOT NULL GENERATED ALWAYS AS IDENTITY,
    ticket_id integer NOT NULL,
    inventory_id integer NOT NULL,
    quantity integer NOT NULL,
    unit_price numeric(10,2) NOT NULL,
    PRIMARY KEY (id),
    FOREIGN KEY (ticket_id) REFERENCES public.tickets(id) ON DELETE CASCADE,
    FOREIGN KEY (inventory_id) REFERENCES public.inventory(id) ON DELETE CASCADE
);

CREATE INDEX idx_ticket_inventories_ticket_id ON public.ticket_inventories USING btree (ticket_id);
CREATE INDEX idx_ticket_inventories_inventory_id ON public.ticket_inventories USING btree (inventory_id);
