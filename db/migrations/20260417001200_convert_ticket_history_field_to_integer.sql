-- migrate:up
ALTER TABLE public.ticket_history
    ALTER COLUMN field TYPE integer
    USING CASE field
        WHEN 'Subject' THEN 1
        WHEN 'Description' THEN 2
        WHEN 'Type' THEN 3
        WHEN 'TicketTypeId' THEN 3
        WHEN 'Priority' THEN 4
        WHEN 'PriorityId' THEN 4
        WHEN 'Status' THEN 5
        WHEN 'StatusId' THEN 5
        WHEN 'ContactId' THEN 6
        WHEN 'AssignedUserId' THEN 7
        WHEN 'AssignedTeamId' THEN 8
        WHEN 'LocationId' THEN 9
        WHEN 'Inventory' THEN 10
        WHEN 'DueDate' THEN 11
        ELSE NULL
    END;

-- migrate:down
ALTER TABLE public.ticket_history
    ALTER COLUMN field TYPE text
    USING CASE field
        WHEN 1 THEN 'Subject'
        WHEN 2 THEN 'Description'
        WHEN 3 THEN 'Type'
        WHEN 4 THEN 'Priority'
        WHEN 5 THEN 'Status'
        WHEN 6 THEN 'ContactId'
        WHEN 7 THEN 'AssignedUserId'
        WHEN 8 THEN 'AssignedTeamId'
        WHEN 9 THEN 'LocationId'
        WHEN 10 THEN 'Inventory'
        WHEN 11 THEN 'DueDate'
        ELSE NULL
    END;
