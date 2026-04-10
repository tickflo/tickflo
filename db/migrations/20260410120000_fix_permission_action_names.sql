-- migrate:up
-- Rename action 'read' -> 'view' and 'update' -> 'edit' to match application expectations
UPDATE public.permissions SET action = 'view' WHERE action = 'read';
UPDATE public.permissions SET action = 'edit' WHERE action = 'update';

-- Rename 'workspace' resource to 'settings' to match managed sections
UPDATE public.permissions SET resource = 'settings' WHERE resource = 'workspace';

-- Add 'dashboard' resource permissions
INSERT INTO public.permissions (resource, action) VALUES
    ('dashboard', 'view'),
    ('dashboard', 'edit'),
    ('dashboard', 'create')
ON CONFLICT (action, resource) DO NOTHING;

-- migrate:down
DELETE FROM public.permissions WHERE resource = 'dashboard';

UPDATE public.permissions SET resource = 'workspace' WHERE resource = 'settings';

UPDATE public.permissions SET action = 'update' WHERE action = 'edit';
UPDATE public.permissions SET action = 'read' WHERE action = 'view';
