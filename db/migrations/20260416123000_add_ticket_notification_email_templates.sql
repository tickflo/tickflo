-- migrate:up
INSERT INTO
    public.email_templates (template_type_id, version, subject, body)
VALUES
    (
        8,
        1,
        'You were assigned work order #{{ticket_id}}',
        'Hello {{recipient_name}},

{{actor_name}} assigned you work order #{{ticket_id}} in {{workspace_name}}.

Work order: {{ticket_subject}}

{{change_summary}}

Open the work order:
{{ticket_link}}

Best regards,
Tickflo Team'
    ),
    (
        9,
        1,
        'Work order #{{ticket_id}} was updated',
        'Hello {{recipient_name}},

{{actor_name}} updated work order #{{ticket_id}} in {{workspace_name}}.

Work order: {{ticket_subject}}

{{change_summary}}

Open the work order:
{{ticket_link}}

Best regards,
Tickflo Team'
    );

-- migrate:down
DELETE FROM public.email_templates
WHERE version = 1 AND template_type_id IN (8, 9);
