-- migrate:up
UPDATE public.email_templates
SET
    subject = 'You were assigned ticket #{{ticket_id}}',
    body = 'Hello {{recipient_name}},

{{actor_name}} assigned you ticket #{{ticket_id}} in {{workspace_name}}.

Ticket: {{ticket_subject}}

{{change_summary}}

Open the ticket:
{{ticket_link}}

Best regards,
Tickflo Team'
WHERE template_type_id = 8;

UPDATE public.email_templates
SET
    subject = 'Ticket #{{ticket_id}} was updated',
    body = 'Hello {{recipient_name}},

{{actor_name}} updated ticket #{{ticket_id}} in {{workspace_name}}.

Ticket: {{ticket_subject}}

{{change_summary}}

Open the ticket:
{{ticket_link}}

Best regards,
Tickflo Team'
WHERE template_type_id = 9;

UPDATE public.email_templates
SET
    subject = 'New comment on ticket #{{ticket_id}}',
    body = 'Hello {{recipient_name}},

{{actor_name}} added a comment to ticket #{{ticket_id}} in {{workspace_name}}.

Ticket: {{ticket_subject}}

{{change_summary}}

Open the ticket:
{{ticket_link}}

Best regards,
Tickflo Team'
WHERE template_type_id = 10;

-- migrate:down
UPDATE public.email_templates
SET
    subject = 'You were assigned work order #{{ticket_id}}',
    body = 'Hello {{recipient_name}},

{{actor_name}} assigned you work order #{{ticket_id}} in {{workspace_name}}.

Work order: {{ticket_subject}}

{{change_summary}}

Open the work order:
{{ticket_link}}

Best regards,
Tickflo Team'
WHERE template_type_id = 8;

UPDATE public.email_templates
SET
    subject = 'Work order #{{ticket_id}} was updated',
    body = 'Hello {{recipient_name}},

{{actor_name}} updated work order #{{ticket_id}} in {{workspace_name}}.

Work order: {{ticket_subject}}

{{change_summary}}

Open the work order:
{{ticket_link}}

Best regards,
Tickflo Team'
WHERE template_type_id = 9;

UPDATE public.email_templates
SET
    subject = 'New comment on work order #{{ticket_id}}',
    body = 'Hello {{recipient_name}},

{{actor_name}} added a comment to work order #{{ticket_id}} in {{workspace_name}}.

Work order: {{ticket_subject}}

{{change_summary}}

Open the work order:
{{ticket_link}}

Best regards,
Tickflo Team'
WHERE template_type_id = 10;
