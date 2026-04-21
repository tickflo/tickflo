-- =============================================
-- Tickflo Seed Data (includes production-safe demo reset)
-- =============================================

-- Reset only data previously inserted by this seed (no global wipes)
DO $$
BEGIN
    -- Delete data for seeded workspaces
    DELETE FROM public.notifications WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'));
    DELETE FROM public.report_runs WHERE report_id IN (
        SELECT id FROM public.reports WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'))
    );
    DELETE FROM public.reports WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'));
    DELETE FROM public.ticket_history WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'));
    DELETE FROM public.ticket_comments WHERE ticket_id IN (
        SELECT id FROM public.tickets WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'))
    );
    DELETE FROM public.ticket_inventory WHERE ticket_id IN (
        SELECT id FROM public.tickets WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'))
    );
    DELETE FROM public.tickets WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'));
    DELETE FROM public.inventory WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'));
    DELETE FROM public.contact_locations WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'));
    DELETE FROM public.contacts WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'));
    DELETE FROM public.team_members WHERE team_id IN (
        SELECT id FROM public.teams WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'))
    );
    DELETE FROM public.teams WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'));
    DELETE FROM public.ticket_statuses WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'));
    DELETE FROM public.ticket_priorities WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'));
    DELETE FROM public.ticket_types WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'));
    DELETE FROM public.locations WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'));
    DELETE FROM public.user_workspace_roles WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'));
    DELETE FROM public.roles WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'));
    DELETE FROM public.user_workspaces WHERE workspace_id IN (SELECT id FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services'));

    -- Delete seeded workspaces themselves
    DELETE FROM public.workspaces WHERE slug IN ('tickflo-demo','techstart','global-services');

    -- Delete demo users and related preferences/tokens/changes
    DELETE FROM public.user_notification_preferences WHERE user_id IN (
        SELECT id FROM public.users WHERE email IN ('admin@demo.com','sarah@demo.com','mike@demo.com','lisa@demo.com','tom@demo.com','emma@demo.com')
    );
    DELETE FROM public.tokens WHERE user_id IN (
        SELECT id FROM public.users WHERE email IN ('admin@demo.com','sarah@demo.com','mike@demo.com','lisa@demo.com','tom@demo.com','emma@demo.com')
    );
    DELETE FROM public.user_email_changes WHERE user_id IN (
        SELECT id FROM public.users WHERE email IN ('admin@demo.com','sarah@demo.com','mike@demo.com','lisa@demo.com','tom@demo.com','emma@demo.com')
    );
    -- Clear emails queued/sent on behalf of demo users so the user rows can be removed.
    -- The new email-notification flow (PR #113) produces rows in public.emails with
    -- created_by/updated_by referencing demo users.
    DELETE FROM public.emails WHERE
        "to" IN ('admin@demo.com','sarah@demo.com','mike@demo.com','lisa@demo.com','tom@demo.com','emma@demo.com')
        OR created_by IN (SELECT id FROM public.users WHERE email IN ('admin@demo.com','sarah@demo.com','mike@demo.com','lisa@demo.com','tom@demo.com','emma@demo.com'))
        OR updated_by IN (SELECT id FROM public.users WHERE email IN ('admin@demo.com','sarah@demo.com','mike@demo.com','lisa@demo.com','tom@demo.com','emma@demo.com'));
    DELETE FROM public.users WHERE email IN ('admin@demo.com','sarah@demo.com','mike@demo.com','lisa@demo.com','tom@demo.com','emma@demo.com');

    -- Resynchronize identity sequences so INSERTs below allocate IDs above any
    -- rows that survived the reset (e.g. non-demo workspaces, app-generated data,
    -- or restores that set explicit IDs without advancing the sequence).
    PERFORM pg_catalog.setval(pg_get_serial_sequence('public.ticket_history', 'id'),
        COALESCE((SELECT MAX(id) FROM public.ticket_history), 0) + 1, false);
    PERFORM pg_catalog.setval(pg_get_serial_sequence('public.ticket_comments', 'id'),
        COALESCE((SELECT MAX(id) FROM public.ticket_comments), 0) + 1, false);
    PERFORM pg_catalog.setval(pg_get_serial_sequence('public.notifications', 'id'),
        COALESCE((SELECT MAX(id) FROM public.notifications), 0) + 1, false);
    PERFORM pg_catalog.setval(pg_get_serial_sequence('public.tickets', 'id'),
        COALESCE((SELECT MAX(id) FROM public.tickets), 0) + 1, false);
    PERFORM pg_catalog.setval(pg_get_serial_sequence('public.reports', 'id'),
        COALESCE((SELECT MAX(id) FROM public.reports), 0) + 1, false);
    PERFORM pg_catalog.setval(pg_get_serial_sequence('public.report_runs', 'id'),
        COALESCE((SELECT MAX(id) FROM public.report_runs), 0) + 1, false);
    PERFORM pg_catalog.setval(pg_get_serial_sequence('public.emails', 'id'),
        COALESCE((SELECT MAX(id) FROM public.emails), 0) + 1, false);
END $$;


-- Clear existing data (in reverse order of dependencies)

-- =============================================
-- Users
-- Demo users are created with NULL passwords
-- They must set a password on first login
-- =============================================
INSERT INTO public.users (name, email, email_confirmed, password_hash, system_admin, created_at) VALUES
('John Admin', 'admin@demo.com', true, NULL, true, NOW()),
('Sarah Manager', 'sarah@demo.com', true, NULL, false, NOW()),
('Mike Technician', 'mike@demo.com', true, NULL, false, NOW()),
('Lisa Support', 'lisa@demo.com', true, NULL, false, NOW()),
('Tom Developer', 'tom@demo.com', true, NULL, false, NOW()),
('Emma Sales', 'emma@demo.com', true, NULL, false, NOW());

-- =============================================
-- Workspaces
-- =============================================
INSERT INTO public.workspaces (name, slug, created_by, created_at) VALUES
('Tickflo Demo', 'tickflo-demo', (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()),
('TechStart Inc', 'techstart', (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()),
('Global Services', 'global-services', (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW());

-- =============================================
-- User Workspaces (User-Workspace associations)
-- =============================================
INSERT INTO public.user_workspaces (user_id, workspace_id, accepted, created_by, created_at)
SELECT u.id, w.id, true, (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()
FROM (VALUES
    ('admin@demo.com', 'tickflo-demo'),
    ('sarah@demo.com', 'tickflo-demo'),
    ('mike@demo.com', 'tickflo-demo'),
    ('lisa@demo.com', 'tickflo-demo'),
    ('admin@demo.com', 'techstart'),
    ('tom@demo.com', 'techstart'),
    ('emma@demo.com', 'techstart'),
    ('admin@demo.com', 'global-services'),
    ('sarah@demo.com', 'global-services')
) AS v(user_email, workspace_slug)
JOIN public.users u ON u.email = v.user_email
JOIN public.workspaces w ON w.slug = v.workspace_slug;

-- =============================================
-- Permissions
-- =============================================
INSERT INTO public.permissions (resource, action) VALUES
('tickets', 'create'),
('tickets', 'read'),
('tickets', 'update'),
('tickets', 'delete'),
('tickets', 'assign'),
('contacts', 'create'),
('contacts', 'read'),
('contacts', 'update'),
('contacts', 'delete'),
('locations', 'create'),
('locations', 'read'),
('locations', 'update'),
('locations', 'delete'),
('inventory', 'create'),
('inventory', 'read'),
('inventory', 'update'),
('inventory', 'delete'),
('teams', 'create'),
('teams', 'read'),
('teams', 'update'),
('teams', 'delete'),
('reports', 'create'),
('reports', 'read'),
('reports', 'update'),
('reports', 'delete'),
('reports', 'execute'),
('roles', 'create'),
('roles', 'read'),
('roles', 'update'),
('roles', 'delete'),
('roles', 'assign'),
('users', 'invite'),
('users', 'read'),
('users', 'update'),
('users', 'delete'),
('workspace', 'manage')
ON CONFLICT (action, resource) DO NOTHING;

-- =============================================
-- Roles
-- =============================================
INSERT INTO public.roles (workspace_id, name, admin, created_by, created_at)
SELECT w.id, v.name, v.admin, (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()
FROM (VALUES
    ('tickflo-demo', 'Admin', true),
    ('tickflo-demo', 'Manager', false),
    ('tickflo-demo', 'Technician', false),
    ('tickflo-demo', 'Support Agent', false),
    ('techstart', 'Admin', true),
    ('techstart', 'Developer', false),
    ('techstart', 'Sales Rep', false),
    ('global-services', 'Admin', true),
    ('global-services', 'Operator', false)
) AS v(workspace_slug, name, admin)
JOIN public.workspaces w ON w.slug = v.workspace_slug;

-- =============================================
-- Role Permissions
-- =============================================
-- Tickflo Demo Admin - All permissions
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT r.id, p.id, (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()
FROM public.permissions p
CROSS JOIN (SELECT r.id FROM public.roles r JOIN public.workspaces w ON w.id = r.workspace_id WHERE w.slug = 'tickflo-demo' AND r.name = 'Admin') r;

-- Tickflo Demo Manager
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT r.id, p.id, (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()
FROM public.permissions p
CROSS JOIN (SELECT r.id FROM public.roles r JOIN public.workspaces w ON w.id = r.workspace_id WHERE w.slug = 'tickflo-demo' AND r.name = 'Manager') r
WHERE p.resource IN ('tickets', 'contacts', 'locations', 'inventory', 'teams', 'reports', 'users');

-- Tickflo Demo Technician
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT r.id, p.id, (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()
FROM public.permissions p
CROSS JOIN (SELECT r.id FROM public.roles r JOIN public.workspaces w ON w.id = r.workspace_id WHERE w.slug = 'tickflo-demo' AND r.name = 'Technician') r
WHERE p.resource IN ('tickets', 'contacts', 'inventory') AND p.action IN ('create', 'read', 'update');

-- Tickflo Demo Support Agent
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT r.id, p.id, (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()
FROM public.permissions p
CROSS JOIN (SELECT r.id FROM public.roles r JOIN public.workspaces w ON w.id = r.workspace_id WHERE w.slug = 'tickflo-demo' AND r.name = 'Support Agent') r
WHERE p.resource IN ('tickets', 'contacts') AND p.action IN ('create', 'read', 'update');

-- TechStart Admin - All permissions
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT r.id, p.id, (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()
FROM public.permissions p
CROSS JOIN (SELECT r.id FROM public.roles r JOIN public.workspaces w ON w.id = r.workspace_id WHERE w.slug = 'techstart' AND r.name = 'Admin') r;

-- TechStart Developer
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT r.id, p.id, (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()
FROM public.permissions p
CROSS JOIN (SELECT r.id FROM public.roles r JOIN public.workspaces w ON w.id = r.workspace_id WHERE w.slug = 'techstart' AND r.name = 'Developer') r
WHERE p.resource IN ('tickets', 'contacts', 'inventory');

-- TechStart Sales Rep
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT r.id, p.id, (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()
FROM public.permissions p
CROSS JOIN (SELECT r.id FROM public.roles r JOIN public.workspaces w ON w.id = r.workspace_id WHERE w.slug = 'techstart' AND r.name = 'Sales Rep') r
WHERE p.resource IN ('contacts') OR (p.resource = 'tickets' AND p.action = 'read');

-- Global Services Admin - All permissions
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT r.id, p.id, (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()
FROM public.permissions p
CROSS JOIN (SELECT r.id FROM public.roles r JOIN public.workspaces w ON w.id = r.workspace_id WHERE w.slug = 'global-services' AND r.name = 'Admin') r;

-- Global Services Operator
INSERT INTO public.role_permissions (role_id, permission_id, created_by, created_at)
SELECT r.id, p.id, (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()
FROM public.permissions p
CROSS JOIN (SELECT r.id FROM public.roles r JOIN public.workspaces w ON w.id = r.workspace_id WHERE w.slug = 'global-services' AND r.name = 'Operator') r
WHERE p.resource IN ('tickets', 'locations', 'inventory');

-- =============================================
-- User Workspace Roles
-- =============================================
INSERT INTO public.user_workspace_roles (user_id, workspace_id, role_id, created_by, created_at)
SELECT u.id, w.id, r.id, (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()
FROM (VALUES
    ('admin@demo.com', 'tickflo-demo', 'Admin'),
    ('sarah@demo.com', 'tickflo-demo', 'Manager'),
    ('mike@demo.com', 'tickflo-demo', 'Technician'),
    ('lisa@demo.com', 'tickflo-demo', 'Support Agent'),
    ('admin@demo.com', 'techstart', 'Admin'),
    ('tom@demo.com', 'techstart', 'Developer'),
    ('emma@demo.com', 'techstart', 'Sales Rep'),
    ('admin@demo.com', 'global-services', 'Admin'),
    ('sarah@demo.com', 'global-services', 'Operator')
) AS v(user_email, workspace_slug, role_name)
JOIN public.users u ON u.email = v.user_email
JOIN public.workspaces w ON w.slug = v.workspace_slug
JOIN public.roles r ON r.workspace_id = w.id AND r.name = v.role_name;

-- =============================================
-- Locations
-- =============================================
INSERT INTO public.locations (workspace_id, name, address, active, created_by, created_at)
SELECT w.id, v.name, v.address, true, (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()
FROM (VALUES
    ('tickflo-demo', 'Headquarters', '123 Main Street, New York, NY 10001'),
    ('tickflo-demo', 'West Coast Office', '456 Tech Blvd, San Francisco, CA 94105'),
    ('tickflo-demo', 'Chicago Branch', '789 Lake Shore Dr, Chicago, IL 60611'),
    ('tickflo-demo', 'Warehouse A', '321 Industrial Pkwy, Newark, NJ 07102'),
    ('techstart', 'Main Office', '555 Startup Ave, Austin, TX 78701'),
    ('techstart', 'R&D Lab', '777 Innovation Dr, Seattle, WA 98101'),
    ('global-services', 'Regional Hub', '999 Commerce St, Dallas, TX 75201'),
    ('global-services', 'Service Center', '111 Support Way, Phoenix, AZ 85001')
) AS v(workspace_slug, name, address)
JOIN public.workspaces w ON w.slug = v.workspace_slug;

-- =============================================
-- Contacts
-- =============================================
INSERT INTO public.contacts (workspace_id, name, email, phone, company, title, notes, tags, preferred_channel, priority, status, assigned_user_id, last_interaction, created_at)
SELECT
    w.id,
    v.name,
    v.email,
    v.phone,
    v.company,
    v.title,
    v.notes,
    v.tags,
    v.preferred_channel,
    v.priority,
    v.status,
    assigned_user.id,
    v.last_interaction,
    v.created_at
FROM (VALUES
    ('tickflo-demo', 'Robert Johnson', 'robert.j@client1.com', '555-0101', 'Client Corp A', 'IT Director', 'Key decision maker for IT purchases', 'vip,enterprise', 'email', 'High', 'Active', 'sarah@demo.com', NOW() - INTERVAL '2 days', NOW() - INTERVAL '30 days'),
    ('tickflo-demo', 'Jennifer Williams', 'jennifer.w@client2.com', '555-0102', 'Client Corp B', 'Operations Manager', 'Handles day-to-day operations', 'enterprise,support', 'phone', 'Normal', 'Active', 'mike@demo.com', NOW() - INTERVAL '5 days', NOW() - INTERVAL '60 days'),
    ('tickflo-demo', 'Michael Brown', 'michael.b@client3.com', '555-0103', 'Small Business Inc', 'Owner', 'Small business client', 'smb,friendly', 'email', 'Normal', 'Active', 'lisa@demo.com', NOW() - INTERVAL '1 day', NOW() - INTERVAL '15 days'),
    ('tickflo-demo', 'Emily Davis', 'emily.d@client4.com', '555-0104', 'Enterprise Solutions', 'CTO', 'Technical contact for large projects', 'vip,technical', 'email', 'High', 'Active', 'sarah@demo.com', NOW() - INTERVAL '3 days', NOW() - INTERVAL '45 days'),
    ('tickflo-demo', 'David Martinez', 'david.m@client5.com', '555-0105', 'MidSize Corp', 'Facilities Manager', 'Manages facility issues', 'facility,regular', 'phone', 'Normal', 'Active', 'mike@demo.com', NOW() - INTERVAL '7 days', NOW() - INTERVAL '90 days'),
    ('tickflo-demo', 'Jessica Taylor', 'jessica.t@client6.com', '555-0106', 'Startup X', 'CEO', 'Fast-growing startup', 'startup,urgent', 'email', 'High', 'Active', 'sarah@demo.com', NOW(), NOW() - INTERVAL '10 days'),
    ('techstart', 'Christopher Anderson', 'chris.a@prospect1.com', '555-0201', 'Prospect Alpha', 'VP Technology', 'Evaluating our platform', 'prospect,interested', 'email', 'High', 'Active', 'tom@demo.com', NOW() - INTERVAL '1 day', NOW() - INTERVAL '5 days'),
    ('techstart', 'Amanda White', 'amanda.w@customer1.com', '555-0202', 'Customer Beta', 'Product Manager', 'Existing customer, very happy', 'customer,advocate', 'email', 'Normal', 'Active', 'emma@demo.com', NOW() - INTERVAL '4 days', NOW() - INTERVAL '120 days'),
    ('techstart', 'James Thomas', 'james.t@partner1.com', '555-0203', 'Partner Gamma', 'Business Development', 'Strategic partner', 'partner,strategic', 'phone', 'High', 'Active', 'tom@demo.com', NOW() - INTERVAL '2 days', NOW() - INTERVAL '20 days'),
    ('global-services', 'Linda Harris', 'linda.h@globclient1.com', '555-0301', 'Global Client 1', 'Regional Manager', 'Multi-location client', 'global,enterprise', 'email', 'High', 'Active', 'sarah@demo.com', NOW() - INTERVAL '1 day', NOW() - INTERVAL '40 days'),
    ('global-services', 'Daniel Clark', 'daniel.c@globclient2.com', '555-0302', 'Global Client 2', 'Operations Director', 'Complex service needs', 'global,complex', 'phone', 'Normal', 'Active', 'sarah@demo.com', NOW() - INTERVAL '6 days', NOW() - INTERVAL '75 days')
) AS v(workspace_slug, name, email, phone, company, title, notes, tags, preferred_channel, priority, status, assigned_user_email, last_interaction, created_at)
JOIN public.workspaces w ON w.slug = v.workspace_slug
LEFT JOIN public.users assigned_user ON assigned_user.email = v.assigned_user_email;

-- =============================================
-- Contact Locations (many-to-many)
-- =============================================
INSERT INTO public.contact_locations (contact_id, location_id, workspace_id)
SELECT c.id, l.id, w.id
FROM (VALUES
    ('tickflo-demo', 'robert.j@client1.com', 'Headquarters'),
    ('tickflo-demo', 'jennifer.w@client2.com', 'West Coast Office'),
    ('tickflo-demo', 'michael.b@client3.com', 'Chicago Branch'),
    ('tickflo-demo', 'emily.d@client4.com', 'Headquarters'),
    ('tickflo-demo', 'emily.d@client4.com', 'West Coast Office'),
    ('tickflo-demo', 'david.m@client5.com', 'Chicago Branch'),
    ('tickflo-demo', 'jessica.t@client6.com', 'Headquarters'),
    ('techstart', 'chris.a@prospect1.com', 'Main Office'),
    ('techstart', 'amanda.w@customer1.com', 'Main Office'),
    ('techstart', 'james.t@partner1.com', 'R&D Lab'),
    ('global-services', 'linda.h@globclient1.com', 'Regional Hub'),
    ('global-services', 'linda.h@globclient1.com', 'Service Center'),
    ('global-services', 'daniel.c@globclient2.com', 'Service Center')
) AS v(workspace_slug, contact_email, location_name)
JOIN public.workspaces w ON w.slug = v.workspace_slug
JOIN public.contacts c ON c.workspace_id = w.id AND c.email = v.contact_email
JOIN public.locations l ON l.workspace_id = w.id AND l.name = v.location_name;

-- =============================================
-- Teams
-- =============================================
INSERT INTO public.teams (workspace_id, name, description, created_by, created_at)
SELECT w.id, v.name, v.description, (SELECT id FROM public.users WHERE email = 'admin@demo.com'), NOW()
FROM (VALUES
    ('tickflo-demo', 'Field Services', 'On-site technical support team'),
    ('tickflo-demo', 'Help Desk', 'First line of support'),
    ('tickflo-demo', 'Infrastructure', 'IT infrastructure maintenance'),
    ('techstart', 'Engineering', 'Product development team'),
    ('techstart', 'Customer Success', 'Customer support and onboarding'),
    ('global-services', 'Operations', 'Daily operations team')
) AS v(workspace_slug, name, description)
JOIN public.workspaces w ON w.slug = v.workspace_slug;

-- =============================================
-- Team Members
-- =============================================
INSERT INTO public.team_members (team_id, user_id)
SELECT t.id, u.id
FROM (VALUES
    ('tickflo-demo', 'Field Services', 'mike@demo.com'),
    ('tickflo-demo', 'Help Desk', 'lisa@demo.com'),
    ('tickflo-demo', 'Infrastructure', 'mike@demo.com'),
    ('techstart', 'Engineering', 'tom@demo.com'),
    ('techstart', 'Customer Success', 'emma@demo.com'),
    ('global-services', 'Operations', 'sarah@demo.com')
) AS v(workspace_slug, team_name, user_email)
JOIN public.workspaces w ON w.slug = v.workspace_slug
JOIN public.teams t ON t.workspace_id = w.id AND t.name = v.team_name
JOIN public.users u ON u.email = v.user_email;

-- =============================================
-- Inventory
-- =============================================
INSERT INTO public.inventory (workspace_id, sku, name, description, quantity, category, status, location_id, cost, price)
SELECT w.id, v.sku, v.name, v.description, v.quantity, v.category, v.status, l.id, v.cost, v.price
FROM (VALUES
    ('tickflo-demo', 'LAPTOP-001', 'Dell Latitude 7420', 'i7-1185G7, 16GB RAM, 512GB SSD - Serial: DL7420-XYZ123', 1, 'Laptops', 'active', 'Headquarters', 1299.99, 1499.99),
    ('tickflo-demo', 'LAPTOP-002', 'Dell Latitude 7420', 'i7-1185G7, 16GB RAM, 512GB SSD - Serial: DL7420-XYZ124', 1, 'Laptops', 'active', 'West Coast Office', 1299.99, 1499.99),
    ('tickflo-demo', 'DESKTOP-001', 'HP EliteDesk 800 G6', 'i7-10700, 32GB RAM, 1TB SSD - Serial: HP800-ABC456', 1, 'Desktops', 'active', 'Headquarters', 1099.99, 1299.99),
    ('tickflo-demo', 'MONITOR-001', 'Dell UltraSharp U2720Q', '27" 4K IPS Monitor - Serial: DU27-MON789', 1, 'Monitors', 'active', 'Headquarters', 499.99, 599.99),
    ('tickflo-demo', 'MONITOR-002', 'Dell UltraSharp U2720Q', '27" 4K IPS Monitor - Serial: DU27-MON790', 1, 'Monitors', 'active', 'West Coast Office', 499.99, 599.99),
    ('tickflo-demo', 'PRINTER-001', 'HP LaserJet Pro M404dn', 'Monochrome laser printer - Serial: HPLJ-PRT001', 1, 'Printers', 'active', 'Headquarters', 299.99, 399.99),
    ('tickflo-demo', 'ROUTER-001', 'Cisco Catalyst 9300', '48-port managed switch - Serial: CC9300-NET001', 1, 'Network', 'active', 'Headquarters', 4199.99, 4999.99),
    ('tickflo-demo', 'SERVER-001', 'Dell PowerEdge R740', 'Dual Xeon, 128GB RAM, RAID storage - Serial: DPE-R740-SRV01', 1, 'Servers', 'active', 'Warehouse A', 7999.99, 8999.99),
    ('tickflo-demo', 'PHONE-001', 'iPhone 14 Pro', '256GB, Space Black - Serial: IP14P-PH001', 1, 'Mobile Devices', 'active', 'Headquarters', 899.99, 1099.99),
    ('tickflo-demo', 'TABLET-001', 'iPad Pro 12.9"', '512GB, Wi-Fi + Cellular - Serial: IPP129-TAB01', 1, 'Mobile Devices', 'active', 'Headquarters', 1099.99, 1299.99),
    ('techstart', 'DEV-LAPTOP-001', 'MacBook Pro 16"', 'M2 Max, 32GB RAM, 1TB SSD - Serial: MBP16-DEV01', 1, 'Laptops', 'active', 'Main Office', 3099.99, 3499.99),
    ('techstart', 'DEV-LAPTOP-002', 'MacBook Pro 16"', 'M2 Max, 32GB RAM, 1TB SSD - Serial: MBP16-DEV02', 1, 'Laptops', 'active', 'Main Office', 3099.99, 3499.99),
    ('techstart', 'DEV-MONITOR-001', 'LG UltraWide 38"', '38" Curved UltraWide - Serial: LG38-MON01', 1, 'Monitors', 'active', 'Main Office', 1099.99, 1299.99),
    ('global-services', 'GS-LAPTOP-001', 'Lenovo ThinkPad X1 Carbon', 'i7-1260P, 16GB RAM, 512GB SSD - Serial: LTX1C-GS01', 1, 'Laptops', 'active', 'Regional Hub', 1699.99, 1899.99)
) AS v(workspace_slug, sku, name, description, quantity, category, status, location_name, cost, price)
JOIN public.workspaces w ON w.slug = v.workspace_slug
JOIN public.locations l ON l.workspace_id = w.id AND l.name = v.location_name;

-- =============================================
-- Ticket Statuses
-- =============================================
INSERT INTO public.ticket_statuses (workspace_id, name, color, sort_order)
SELECT w.id, v.name, v.color, v.sort_order
FROM (VALUES
    ('tickflo-demo', 'New', 'info', 0),
    ('tickflo-demo', 'In Progress', 'warning', 1),
    ('tickflo-demo', 'Waiting', 'neutral', 2),
    ('tickflo-demo', 'Resolved', 'success', 3),
    ('tickflo-demo', 'Closed', 'neutral', 4),
    ('techstart', 'Open', 'info', 0),
    ('techstart', 'Working', 'warning', 1),
    ('techstart', 'Done', 'success', 2),
    ('global-services', 'Submitted', 'info', 0),
    ('global-services', 'Assigned', 'warning', 1),
    ('global-services', 'Completed', 'success', 2)
) AS v(workspace_slug, name, color, sort_order)
JOIN public.workspaces w ON w.slug = v.workspace_slug;

-- =============================================
-- Priorities
-- =============================================
INSERT INTO public.ticket_priorities (workspace_id, name, color, sort_order)
SELECT w.id, v.name, v.color, v.sort_order
FROM (VALUES
    ('tickflo-demo', 'Low', 'success', 0),
    ('tickflo-demo', 'Normal', 'info', 1),
    ('tickflo-demo', 'High', 'warning', 2),
    ('tickflo-demo', 'Urgent', 'error', 3),
    ('techstart', 'P3', 'neutral', 0),
    ('techstart', 'P2', 'warning', 1),
    ('techstart', 'P1', 'error', 2),
    ('global-services', 'Low', 'success', 0),
    ('global-services', 'Medium', 'warning', 1),
    ('global-services', 'High', 'error', 2)
) AS v(workspace_slug, name, color, sort_order)
JOIN public.workspaces w ON w.slug = v.workspace_slug;

-- =============================================
-- Ticket Types
-- =============================================
INSERT INTO public.ticket_types (workspace_id, name, color, sort_order)
SELECT w.id, v.name, v.color, v.sort_order
FROM (VALUES
    ('tickflo-demo', 'Incident', 'error', 0),
    ('tickflo-demo', 'Service Request', 'info', 1),
    ('tickflo-demo', 'Change Request', 'warning', 2),
    ('tickflo-demo', 'Problem', 'error', 3),
    ('tickflo-demo', 'Question', 'info', 4),
    ('techstart', 'Bug', 'error', 0),
    ('techstart', 'Feature Request', 'success', 1),
    ('techstart', 'Support', 'info', 2),
    ('global-services', 'Maintenance', 'warning', 0),
    ('global-services', 'Repair', 'error', 1),
    ('global-services', 'Installation', 'info', 2)
) AS v(workspace_slug, name, color, sort_order)
JOIN public.workspaces w ON w.slug = v.workspace_slug;

-- =============================================
-- Tickets
-- =============================================
INSERT INTO public.tickets (workspace_id, contact_id, location_id, subject, description, ticket_type_id, priority_id, status_id, assigned_user_id, assigned_team_id, created_at, updated_at)
SELECT
    w.id,
    c.id,
    l.id,
    v.subject,
    v.description,
    tt.id AS ticket_type_id,
    tp.id AS priority_id,
    ts.id AS status_id,
    assigned_user.id,
    assigned_team.id,
    v.created_at,
    v.updated_at
FROM (VALUES
    -- Acme Corporation - Recent and varied
    ('tickflo-demo', 'robert.j@client1.com', 'Headquarters', 'Email not syncing on mobile device', 'Robert reports that his iPhone is not syncing emails since this morning. He can receive but not send.', 'Incident', 'High', 'In Progress', 'mike@demo.com', 'Field Services', NOW() - INTERVAL '2 hours', NOW() - INTERVAL '1 hour'),
    ('tickflo-demo', 'jennifer.w@client2.com', 'West Coast Office', 'Request new laptop for new hire', 'Jennifer needs a new laptop configured for their new IT specialist starting next Monday.', 'Service Request', 'Normal', 'New', 'sarah@demo.com', NULL, NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
    ('tickflo-demo', 'michael.b@client3.com', 'Chicago Branch', 'Printer paper jam recurring issue', 'Michael says the office printer keeps jamming. Might need maintenance or replacement.', 'Problem', 'Normal', 'Waiting', 'mike@demo.com', 'Field Services', NOW() - INTERVAL '3 days', NOW() - INTERVAL '1 day'),
    ('tickflo-demo', 'emily.d@client4.com', 'Headquarters', 'VPN connection dropping frequently', 'Emily experiences VPN disconnections every 10-15 minutes when working remotely.', 'Incident', 'High', 'In Progress', 'mike@demo.com', 'Infrastructure', NOW() - INTERVAL '4 hours', NOW() - INTERVAL '2 hours'),
    ('tickflo-demo', 'david.m@client5.com', 'Chicago Branch', 'Install new access control system', 'David requests installation of card readers at the Chicago facility entrance.', 'Change Request', 'Normal', 'New', NULL, NULL, NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
    ('tickflo-demo', 'jessica.t@client6.com', 'Headquarters', 'Software license inquiry', 'Jessica asks about available licenses for Adobe Creative Cloud for her design team.', 'Question', 'Low', 'Resolved', 'lisa@demo.com', 'Help Desk', NOW() - INTERVAL '5 days', NOW() - INTERVAL '4 days'),
    ('tickflo-demo', 'robert.j@client1.com', 'Headquarters', 'Cannot access shared drive', 'Robert cannot access the Finance shared drive. Getting "Access Denied" error.', 'Incident', 'High', 'Resolved', 'mike@demo.com', 'Field Services', NOW() - INTERVAL '7 days', NOW() - INTERVAL '6 days'),
    ('tickflo-demo', 'jennifer.w@client2.com', 'West Coast Office', 'Monitor flickering issue', 'Jennifer''s monitor flickers intermittently. Might be cable or monitor issue.', 'Incident', 'Normal', 'Closed', 'mike@demo.com', 'Field Services', NOW() - INTERVAL '10 days', NOW() - INTERVAL '9 days'),
    ('tickflo-demo', 'emily.d@client4.com', 'Headquarters', 'Upgrade Office 365 subscription', 'Emily''s team needs upgraded Office 365 licenses with advanced features.', 'Service Request', 'Normal', 'Closed', 'sarah@demo.com', NULL, NOW() - INTERVAL '15 days', NOW() - INTERVAL '12 days'),
    ('tickflo-demo', 'michael.b@client3.com', 'Chicago Branch', 'WiFi slow in conference room', 'Michael reports very slow WiFi speeds in the main conference room during meetings.', 'Problem', 'Normal', 'Resolved', 'mike@demo.com', 'Infrastructure', NOW() - INTERVAL '8 days', NOW() - INTERVAL '7 days'),
    -- TechStart Inc
    ('techstart', 'chris.a@prospect1.com', 'Main Office', 'Login page not loading', 'Christopher reports that the login page shows a blank screen on Chrome.', 'Bug', 'P1', 'Working', 'tom@demo.com', 'Engineering', NOW() - INTERVAL '3 hours', NOW() - INTERVAL '1 hour'),
    ('techstart', 'amanda.w@customer1.com', 'Main Office', 'Feature request: Dark mode', 'Amanda suggests adding a dark mode option for the dashboard interface.', 'Feature Request', 'P3', 'Open', NULL, NULL, NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),
    ('techstart', 'james.t@partner1.com', 'R&D Lab', 'API documentation outdated', 'James mentions that the API docs for v2.3 still reference old endpoints.', 'Support', 'P2', 'Working', 'tom@demo.com', 'Engineering', NOW() - INTERVAL '2 days', NOW() - INTERVAL '1 day'),
    ('techstart', 'chris.a@prospect1.com', 'Main Office', 'Export function timeout', 'Christopher gets timeout errors when exporting large reports (>10k rows).', 'Bug', 'P2', 'Open', NULL, 'Engineering', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
    ('techstart', 'amanda.w@customer1.com', 'Main Office', 'SSO integration with Azure AD', 'Amanda wants to enable SSO for her team using Azure Active Directory.', 'Feature Request', 'P2', 'Done', 'tom@demo.com', 'Engineering', NOW() - INTERVAL '20 days', NOW() - INTERVAL '15 days'),
    -- Global Services
    ('global-services', 'linda.h@globclient1.com', 'Regional Hub', 'HVAC system making loud noise', 'Linda reports unusual sounds from the air conditioning unit in the server room.', 'Maintenance', 'High', 'Assigned', 'sarah@demo.com', 'Operations', NOW() - INTERVAL '6 hours', NOW() - INTERVAL '4 hours'),
    ('global-services', 'daniel.c@globclient2.com', 'Service Center', 'Replace broken door lock', 'Daniel says the main entrance lock is stuck and needs replacement.', 'Repair', 'Medium', 'Submitted', NULL, NULL, NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
    ('global-services', 'linda.h@globclient1.com', 'Regional Hub', 'Fire alarm system annual test', 'Linda schedules annual fire alarm testing for next week.', 'Maintenance', 'Medium', 'Submitted', 'sarah@demo.com', 'Operations', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),
    ('global-services', 'daniel.c@globclient2.com', 'Service Center', 'Emergency lighting installation', 'Daniel requests installation of emergency exit lighting in the new warehouse section.', 'Installation', 'High', 'Assigned', 'sarah@demo.com', 'Operations', NOW() - INTERVAL '2 days', NOW() - INTERVAL '1 day'),
    ('global-services', 'linda.h@globclient1.com', 'Service Center', 'Security camera not recording', 'Linda notices that camera #3 in the parking lot is not recording footage.', 'Repair', 'High', 'Completed', 'sarah@demo.com', 'Operations', NOW() - INTERVAL '5 days', NOW() - INTERVAL '4 days')
) AS v(workspace_slug, contact_email, location_name, subject, description, type_name, priority_name, status_name, assigned_user_email, assigned_team_name, created_at, updated_at)
JOIN public.workspaces w ON w.slug = v.workspace_slug
JOIN public.contacts c ON c.workspace_id = w.id AND c.email = v.contact_email
JOIN public.locations l ON l.workspace_id = w.id AND l.name = v.location_name
LEFT JOIN public.ticket_types tt ON tt.workspace_id = w.id AND tt.name = v.type_name
LEFT JOIN public.ticket_priorities tp ON tp.workspace_id = w.id AND tp.name = v.priority_name
LEFT JOIN public.ticket_statuses ts ON ts.workspace_id = w.id AND ts.name = v.status_name
LEFT JOIN public.users assigned_user ON assigned_user.email = v.assigned_user_email
LEFT JOIN public.teams assigned_team ON assigned_team.workspace_id = w.id AND assigned_team.name = v.assigned_team_name;

-- =============================================
-- Ticket Inventory (linking tickets to assets)
-- =============================================
INSERT INTO public.ticket_inventory (ticket_id, inventory_id)
SELECT t.id, i.id
FROM (VALUES
    ('tickflo-demo', 'Email not syncing on mobile device', 'PHONE-001'),
    ('tickflo-demo', 'Request new laptop for new hire', 'TABLET-001'),
    ('tickflo-demo', 'Printer paper jam recurring issue', 'PRINTER-001'),
    ('tickflo-demo', 'VPN connection dropping frequently', 'LAPTOP-001'),
    ('tickflo-demo', 'Monitor flickering issue', 'MONITOR-001'),
    ('tickflo-demo', 'WiFi slow in conference room', 'ROUTER-001')
) AS v(workspace_slug, ticket_subject, inventory_sku)
JOIN public.workspaces w ON w.slug = v.workspace_slug
JOIN public.tickets t ON t.workspace_id = w.id AND t.subject = v.ticket_subject
JOIN public.inventory i ON i.workspace_id = w.id AND i.sku = v.inventory_sku;

-- =============================================
-- Ticket History
-- action and field columns are integer-backed enums:
--   TicketHistoryAction: 1=Created, 2=FieldChanged, 3=Assigned, 4=TeamAssigned,
--     5=Unassigned, 6=ReassignmentNote, 7=Closed, 8=Reopened, 9=Resolved, 10=Cancelled
--   TicketHistoryField:  1=Subject, 2=Description, 3=Type, 4=Priority, 5=Status,
--     6=Contact, 7=AssignedUser, 8=AssignedTeam, 9=Location, 10=Inventory, 11=DueDate
-- =============================================
INSERT INTO public.ticket_history (workspace_id, ticket_id, created_by_user_id, action, field, old_value, new_value, note, created_at)
SELECT w.id, t.id, u.id, v.action, v.field, v.old_value, v.new_value, v.note, v.created_at
FROM (VALUES
    ('tickflo-demo', 'Email not syncing on mobile device', 'mike@demo.com', 1, NULL::int, NULL, NULL, NULL, NOW() - INTERVAL '1 hour 30 minutes'),
    ('tickflo-demo', 'Email not syncing on mobile device', 'mike@demo.com', 3, NULL::int, NULL, NULL, 'Assigned to Mike', NOW() - INTERVAL '1 hour 15 minutes'),
    ('tickflo-demo', 'Email not syncing on mobile device', 'mike@demo.com', 2, 5, 'New', 'In Progress', 'Starting investigation', NOW() - INTERVAL '1 hour'),
    ('tickflo-demo', 'VPN connection dropping frequently', 'mike@demo.com', 4, NULL::int, NULL, NULL, 'Routed to infrastructure team', NOW() - INTERVAL '2 hours 30 minutes'),
    ('tickflo-demo', 'VPN connection dropping frequently', 'mike@demo.com', 2, 5, 'New', 'In Progress', 'Assigned to infrastructure team', NOW() - INTERVAL '2 hours'),
    ('tickflo-demo', 'Software license inquiry', 'lisa@demo.com', 2, 5, 'New', 'In Progress', 'Checking license availability', NOW() - INTERVAL '5 days'),
    ('tickflo-demo', 'Software license inquiry', 'lisa@demo.com', 9, NULL::int, NULL, NULL, 'Marked as resolved', NOW() - INTERVAL '4 days'),
    ('tickflo-demo', 'Cannot access shared drive', 'mike@demo.com', 2, 5, 'New', 'In Progress', 'Investigating permissions', NOW() - INTERVAL '7 days'),
    ('tickflo-demo', 'Cannot access shared drive', 'mike@demo.com', 9, NULL::int, NULL, NULL, 'Access restored', NOW() - INTERVAL '6 days'),
    ('tickflo-demo', 'Cannot access shared drive', 'mike@demo.com', 7, NULL::int, NULL, NULL, 'Closed after user confirmation', NOW() - INTERVAL '5 days 12 hours'),
    ('techstart', 'Login page not loading', 'tom@demo.com', 2, 5, 'Open', 'Working', 'Reproducing the issue', NOW() - INTERVAL '2 hours'),
    ('techstart', 'Login page not loading', 'tom@demo.com', 2, 4, 'P2', 'P1', 'Escalating - affects multiple users', NOW() - INTERVAL '1 hour'),
    ('techstart', 'Login page not loading', 'tom@demo.com', 6, NULL::int, NULL, NULL, 'Reassigned from Lisa to Tom for frontend expertise', NOW() - INTERVAL '45 minutes'),
    ('global-services', 'HVAC system making loud noise', 'sarah@demo.com', 1, NULL::int, NULL, NULL, NULL, NOW() - INTERVAL '5 hours'),
    ('global-services', 'HVAC system making loud noise', 'sarah@demo.com', 2, 5, 'Submitted', 'Assigned', 'Scheduled technician visit', NOW() - INTERVAL '4 hours'),
    ('global-services', 'HVAC system making loud noise', 'sarah@demo.com', 5, NULL::int, NULL, NULL, 'Removing previous assignee before routing', NOW() - INTERVAL '3 hours 30 minutes')
) AS v(workspace_slug, ticket_subject, user_email, action, field, old_value, new_value, note, created_at)
JOIN public.workspaces w ON w.slug = v.workspace_slug
JOIN public.tickets t ON t.workspace_id = w.id AND t.subject = v.ticket_subject
JOIN public.users u ON u.email = v.user_email;

-- =============================================
-- Ticket Comments
-- Demo comment threads (mix of internal notes and client-visible replies).
-- =============================================
INSERT INTO public.ticket_comments (workspace_id, ticket_id, created_by_user_id, content, is_visible_to_client, created_at)
SELECT w.id, t.id, u.id, v.content, v.is_visible_to_client, v.created_at
FROM (VALUES
    ('tickflo-demo', 'Email not syncing on mobile device', 'mike@demo.com', 'Checked email server logs. Issue appears to be with device configuration.', false, NOW() - INTERVAL '45 minutes'),
    ('tickflo-demo', 'VPN connection dropping frequently', 'mike@demo.com', 'Updated VPN client to latest version. Monitoring for stability.', true, NOW() - INTERVAL '1 hour'),
    ('tickflo-demo', 'Software license inquiry', 'lisa@demo.com', 'We have 5 available licenses. Sent details to Jessica.', true, NOW() - INTERVAL '4 days 12 hours'),
    ('tickflo-demo', 'Cannot access shared drive', 'mike@demo.com', 'Found the issue - AD group membership was missing. Added user to correct group.', false, NOW() - INTERVAL '6 days 18 hours'),
    ('techstart', 'Login page not loading', 'tom@demo.com', 'Issue traced to caching problem. Deploying fix now.', true, NOW() - INTERVAL '30 minutes'),
    ('global-services', 'HVAC system making loud noise', 'sarah@demo.com', 'HVAC technician will arrive at 2 PM today.', true, NOW() - INTERVAL '3 hours')
) AS v(workspace_slug, ticket_subject, user_email, content, is_visible_to_client, created_at)
JOIN public.workspaces w ON w.slug = v.workspace_slug
JOIN public.tickets t ON t.workspace_id = w.id AND t.subject = v.ticket_subject
JOIN public.users u ON u.email = v.user_email;

-- =============================================
-- Reports
-- =============================================
INSERT INTO public.reports (workspace_id, name, ready, created_by, created_at)
SELECT w.id, v.name, v.ready, (SELECT id FROM public.users WHERE email = 'admin@demo.com'), v.created_at
FROM (VALUES
    ('tickflo-demo', 'Open Tickets by Priority', true, NOW() - INTERVAL '60 days'),
    ('tickflo-demo', 'Monthly Resolution Time', true, NOW() - INTERVAL '60 days'),
    ('tickflo-demo', 'Tickets by Location', true, NOW() - INTERVAL '60 days'),
    ('tickflo-demo', 'Asset Warranty Expiry', true, NOW() - INTERVAL '60 days'),
    ('techstart', 'Bug Report Summary', true, NOW() - INTERVAL '45 days'),
    ('techstart', 'Customer Satisfaction', false, NOW() - INTERVAL '30 days'),
    ('global-services', 'Maintenance Schedule', true, NOW() - INTERVAL '50 days')
) AS v(workspace_slug, name, ready, created_at)
JOIN public.workspaces w ON w.slug = v.workspace_slug;

-- =============================================
-- Report Runs
-- =============================================
INSERT INTO public.report_runs (workspace_id, report_id, status, started_at, finished_at)
SELECT w.id, r.id, v.status, v.started_at, v.finished_at
FROM (VALUES
    ('tickflo-demo', 'Open Tickets by Priority', 'Completed', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
    ('tickflo-demo', 'Open Tickets by Priority', 'Completed', NOW() - INTERVAL '7 days', NOW() - INTERVAL '7 days'),
    ('tickflo-demo', 'Monthly Resolution Time', 'Completed', NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),
    ('tickflo-demo', 'Tickets by Location', 'Completed', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),
    ('tickflo-demo', 'Asset Warranty Expiry', 'Completed', NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),
    ('techstart', 'Bug Report Summary', 'Completed', NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day'),
    ('global-services', 'Maintenance Schedule', 'Completed', NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days')
) AS v(workspace_slug, report_name, status, started_at, finished_at)
JOIN public.workspaces w ON w.slug = v.workspace_slug
JOIN public.reports r ON r.workspace_id = w.id AND r.name = v.report_name;

-- =============================================
-- Update last_run for reports
-- =============================================
UPDATE public.reports SET last_run = NOW() - INTERVAL '1 day'
WHERE workspace_id = (SELECT id FROM public.workspaces WHERE slug = 'tickflo-demo')
    AND name = 'Open Tickets by Priority';
UPDATE public.reports SET last_run = NOW() - INTERVAL '2 days'
WHERE workspace_id = (SELECT id FROM public.workspaces WHERE slug = 'tickflo-demo')
    AND name = 'Monthly Resolution Time';
UPDATE public.reports SET last_run = NOW() - INTERVAL '3 days'
WHERE workspace_id = (SELECT id FROM public.workspaces WHERE slug = 'tickflo-demo')
    AND name = 'Tickets by Location';
UPDATE public.reports SET last_run = NOW() - INTERVAL '5 days'
WHERE workspace_id = (SELECT id FROM public.workspaces WHERE slug = 'tickflo-demo')
    AND name = 'Asset Warranty Expiry';
UPDATE public.reports SET last_run = NOW() - INTERVAL '1 day'
WHERE workspace_id = (SELECT id FROM public.workspaces WHERE slug = 'techstart')
    AND name = 'Bug Report Summary';
UPDATE public.reports SET last_run = NOW() - INTERVAL '4 days'
WHERE workspace_id = (SELECT id FROM public.workspaces WHERE slug = 'global-services')
    AND name = 'Maintenance Schedule';

-- =============================================
-- Summary
-- =============================================
-- This seed data includes:
-- - 6 demo users (1 admin, 5 regular users)
-- - 3 workspaces
-- - 9 roles with appropriate permissions
-- - 8 locations across workspaces
-- - 11 contacts with various attributes
-- - 6 teams with members
-- - 14 inventory items
-- - 20 tickets with various statuses
-- - Ticket history entries showing activity
-- - 7 reports with run history
--
-- Demo Users:
-- All demo users are created with NULL passwords and must set a password on first login.
-- Try logging in with any of these email addresses - you'll be redirected to set a password:
-- - admin@demo.com (System Admin)
-- - sarah@demo.com (Manager)
-- - mike@demo.com (Technician)
-- - lisa@demo.com (Support Agent)
-- - tom@demo.com (Developer)
-- - emma@demo.com (Sales Rep)

-- =============================================
-- Notifications
-- Sample notifications for different types and delivery methods
-- =============================================
INSERT INTO public.notifications (workspace_id, user_id, type, delivery_method, priority, subject, body, status, created_at, created_by)
SELECT w.id, u.id, v.type, v.delivery_method, v.priority, v.subject, v.body, v.status, v.created_at, created_by.id
FROM (VALUES
    ('tickflo-demo', 'sarah@demo.com', 'workspace_invite', 'email', 'high', 'Welcome to Acme Corporation', '<p>You have been invited to join <strong>Acme Corporation</strong> workspace.</p><p>Click here to accept the invitation and get started.</p>', 'sent', NOW() - INTERVAL '3 days', 'admin@demo.com'),
    ('tickflo-demo', 'mike@demo.com', 'ticket_assigned', 'email', 'normal', 'Ticket #1001 assigned to you', '<p>A new ticket has been assigned to you:</p><p><strong>Title:</strong> Email not syncing on mobile device</p><p><strong>Priority:</strong> High</p>', 'sent', NOW() - INTERVAL '2 days', 'sarah@demo.com'),
    ('techstart', 'tom@demo.com', 'ticket_comment', 'email', 'normal', 'New comment on Ticket #1005', '<p>Lisa Johnson added a comment to your ticket:</p><blockquote>I have identified the root cause. Will update shortly.</blockquote>', 'sent', NOW() - INTERVAL '1 day', 'lisa@demo.com'),
    ('tickflo-demo', 'sarah@demo.com', 'ticket_status_changed', 'in_app', 'normal', 'Ticket #1001 status updated', 'Ticket status changed from Open to In Progress', 'sent', NOW() - INTERVAL '2 hours', 'mike@demo.com'),
    ('tickflo-demo', 'mike@demo.com', 'ticket_assigned', 'in_app', 'normal', 'New ticket assigned', 'Ticket #1015 "Network connectivity issues" has been assigned to you', 'sent', NOW() - INTERVAL '1 hour', 'sarah@demo.com'),
    ('tickflo-demo', 'lisa@demo.com', 'ticket_created_team', 'in_app', 'normal', 'New ticket for your team', 'A new ticket was created and routed to your team', 'sent', NOW() - INTERVAL '90 minutes', 'sarah@demo.com'),
    ('tickflo-demo', 'mike@demo.com', 'ticket_unassigned', 'in_app', 'low', 'Ticket unassigned', 'Ticket #1012 was unassigned from you', 'sent', NOW() - INTERVAL '45 minutes', 'sarah@demo.com'),
    ('techstart', 'tom@demo.com', 'ticket_status_changed_team', 'in_app', 'normal', 'Team ticket status updated', 'A ticket assigned to your team changed status', 'sent', NOW() - INTERVAL '20 minutes', 'lisa@demo.com'),
    ('techstart', 'lisa@demo.com', 'report_completed', 'in_app', 'low', 'Weekly report completed', 'Your weekly ticket report has finished processing', 'sent', NOW() - INTERVAL '30 minutes', NULL),
    ('global-services', 'emma@demo.com', 'mention', 'in_app', 'high', 'You were mentioned in a comment', 'Tom Wilson mentioned you in ticket #1012', 'pending', NOW() - INTERVAL '15 minutes', 'tom@demo.com'),
    ('tickflo-demo', 'sarah@demo.com', 'ticket_summary', 'email', 'low', 'Daily Ticket Summary', '<p>Your daily ticket summary for Acme Corporation:</p><ul><li>5 new tickets</li><li>3 resolved</li><li>2 pending your review</li></ul>', 'pending', NOW(), NULL),
    ('tickflo-demo', 'mike@demo.com', 'ticket_summary', 'email', 'low', 'Daily Ticket Summary', '<p>Your daily ticket summary for Acme Corporation:</p><ul><li>3 assigned to you</li><li>1 awaiting response</li></ul>', 'pending', NOW(), NULL),
    ('techstart', 'tom@demo.com', 'password_reset', 'email', 'urgent', 'Password Reset Request', '<p>You requested a password reset for your account.</p><p>Click the link below to reset your password:</p>', 'failed', NOW() - INTERVAL '1 day', NULL)
) AS v(workspace_slug, user_email, type, delivery_method, priority, subject, body, status, created_at, created_by_email)
JOIN public.workspaces w ON w.slug = v.workspace_slug
JOIN public.users u ON u.email = v.user_email
LEFT JOIN public.users created_by ON created_by.email = v.created_by_email;

-- Add failure reason for the failed notification
UPDATE public.notifications SET 
    failed_at = NOW() - INTERVAL '1 day',
    failure_reason = 'SMTP connection timeout'
WHERE status = 'failed';

-- =============================================
-- User Notification Preferences
-- Sample preferences showing different user preferences
-- =============================================
INSERT INTO public.user_notification_preferences (user_id, notification_type, email_enabled, in_app_enabled, sms_enabled, push_enabled, created_at)
SELECT u.id, v.notification_type, v.email_enabled, v.in_app_enabled, v.sms_enabled, v.push_enabled, NOW()
FROM (VALUES
    ('admin@demo.com', 'workspace_invite', true, true, false, false),
    ('admin@demo.com', 'ticket_assigned', true, true, false, false),
    ('admin@demo.com', 'ticket_comment', true, true, false, false),
    ('admin@demo.com', 'ticket_status_changed', true, true, false, false),
    ('sarah@demo.com', 'workspace_invite', true, false, false, false),
    ('sarah@demo.com', 'ticket_assigned', true, true, false, false),
    ('sarah@demo.com', 'ticket_summary', false, true, false, false),
    ('sarah@demo.com', 'mention', true, true, true, false),
    ('mike@demo.com', 'ticket_assigned', false, true, false, false),
    ('mike@demo.com', 'ticket_comment', false, true, false, false),
    ('mike@demo.com', 'ticket_status_changed', false, true, false, false),
    ('mike@demo.com', 'mention', false, true, false, false),
    ('lisa@demo.com', 'workspace_invite', true, true, true, true),
    ('lisa@demo.com', 'ticket_assigned', true, true, false, true),
    ('lisa@demo.com', 'mention', true, true, true, true),
    ('lisa@demo.com', 'password_reset', true, true, true, false),
    ('tom@demo.com', 'ticket_assigned', true, true, false, false),
    ('tom@demo.com', 'ticket_comment', false, true, false, false),
    ('tom@demo.com', 'report_completed', true, false, false, false),
    ('tom@demo.com', 'ticket_summary', true, false, false, false)
) AS v(user_email, notification_type, email_enabled, in_app_enabled, sms_enabled, push_enabled)
JOIN public.users u ON u.email = v.user_email;

-- Note: Users with no preferences will use system defaults:
-- email_enabled=true, in_app_enabled=true, sms_enabled=false, push_enabled=false

-- =============================================
-- Email Templates
-- Global email templates for system-wide messages
-- Template Type IDs:
-- 1 = Email Confirmation Thank You
-- 2 = Workspace Invite (new user)
-- 3 = Email Confirmation Request
-- 4 = Workspace Invite Resend
-- =============================================
-- =============================================
-- Email Templates
-- Email templates are now managed via migrations (see migration 20260116120100)
-- They are global, versioned, and immutable
-- =============================================