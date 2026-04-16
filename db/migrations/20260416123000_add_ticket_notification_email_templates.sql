-- migrate:up
INSERT INTO
    public.email_templates (template_type_id, version, subject, body)
VALUES
    (
        8,
        1,
        'You were assigned ticket #{{ticket_id}}',
        'Hello {{recipient_name}},

{{actor_name}} assigned you ticket #{{ticket_id}} in {{workspace_name}}.

Ticket: {{ticket_subject}}

{{change_summary}}

Open the ticket:
{{ticket_link}}

Best regards,
Tickflo Team'
    ),
    (
        9,
        1,
        'Ticket #{{ticket_id}} was updated',
        'Hello {{recipient_name}},

{{actor_name}} updated ticket #{{ticket_id}} in {{workspace_name}}.

Ticket: {{ticket_subject}}

{{change_summary}}

Open the ticket:
{{ticket_link}}

Best regards,
Tickflo Team'
    );

-- migrate:down
DELETE FROM public.email_templates
WHERE version = 1 AND template_type_id IN (8, 9);
