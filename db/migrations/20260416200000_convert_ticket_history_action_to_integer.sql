-- migrate:up
ALTER TABLE public.ticket_history
    ALTER COLUMN action TYPE integer
    USING CASE action
        WHEN 'created' THEN 1
        WHEN 'field_changed' THEN 2
        WHEN 'assigned' THEN 3
        WHEN 'team_assigned' THEN 4
        WHEN 'unassigned' THEN 5
        WHEN 'reassignment_note' THEN 6
        WHEN 'closed' THEN 7
        WHEN 'reopened' THEN 8
        WHEN 'resolved' THEN 9
        WHEN 'cancelled' THEN 10
        ELSE 1
    END;

-- migrate:down
ALTER TABLE public.ticket_history
    ALTER COLUMN action TYPE text
    USING CASE action
        WHEN 1 THEN 'created'
        WHEN 2 THEN 'field_changed'
        WHEN 3 THEN 'assigned'
        WHEN 4 THEN 'team_assigned'
        WHEN 5 THEN 'unassigned'
        WHEN 6 THEN 'reassignment_note'
        WHEN 7 THEN 'closed'
        WHEN 8 THEN 'reopened'
        WHEN 9 THEN 'resolved'
        WHEN 10 THEN 'cancelled'
        ELSE 'created'
    END;
