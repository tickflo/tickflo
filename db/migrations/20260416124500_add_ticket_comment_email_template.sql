-- migrate:up
INSERT INTO
    public.email_templates (template_type_id, version, subject, body)
VALUES
    (
        10,
        1,
        'New comment on work order #{{ticket_id}}',
        'Hello {{recipient_name}},

{{actor_name}} added a comment to work order #{{ticket_id}} in {{workspace_name}}.

Work order: {{ticket_subject}}

{{change_summary}}

Open the work order:
{{ticket_link}}

Best regards,
Tickflo Team'
    );

-- migrate:down
DELETE FROM public.email_templates
WHERE version = 1 AND template_type_id = 10;
