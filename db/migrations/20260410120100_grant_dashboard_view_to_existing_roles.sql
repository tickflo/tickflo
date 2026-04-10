-- migrate:up
-- Grant dashboard view permission to all non-admin roles that already have
-- at least one view-level permission on another section. The dashboard is the
-- workspace home page and any role with configured access should be able to see it.
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT DISTINCT
    rp.role_id,
    p.id,
    (SELECT created_by FROM public.roles WHERE id = rp.role_id LIMIT 1),
    NOW()
FROM public.role_permissions rp
JOIN public.permissions existing_p ON existing_p.id = rp.permission_id AND existing_p.action = 'view'
CROSS JOIN public.permissions p
WHERE p.resource = 'dashboard' AND p.action = 'view'
    AND rp.role_id NOT IN (
        SELECT rp2.role_id
        FROM public.role_permissions rp2
        JOIN public.permissions p2 ON p2.id = rp2.permission_id
        WHERE p2.resource = 'dashboard' AND p2.action = 'view'
    )
ON CONFLICT DO NOTHING;

-- migrate:down
-- Remove dashboard view from roles that don't have other dashboard permissions
DELETE FROM public.role_permissions
WHERE permission_id = (SELECT id FROM public.permissions WHERE resource = 'dashboard' AND action = 'view');
