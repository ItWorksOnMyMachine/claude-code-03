-- Platform Database Seed Data
-- Creates initial test data for development environment

BEGIN;

-- Insert default tenant
INSERT INTO app.tenants (id, name, display_name, subdomain, status, settings)
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'default', 'Default Tenant', 'default', 'active', 
     '{"theme": "light", "features": ["basic"], "maxUsers": 10}')
ON CONFLICT (id) DO NOTHING;

-- Insert demo tenant
INSERT INTO app.tenants (id, name, display_name, subdomain, status, settings)
VALUES 
    ('22222222-2222-2222-2222-222222222222', 'demo', 'Demo Company', 'demo', 'active',
     '{"theme": "light", "features": ["basic", "advanced"], "maxUsers": 50}')
ON CONFLICT (id) DO NOTHING;

-- Insert test tenant
INSERT INTO app.tenants (id, name, display_name, subdomain, status, settings)
VALUES 
    ('33333333-3333-3333-3333-333333333333', 'test', 'Test Organization', 'test', 'trial',
     '{"theme": "dark", "features": ["basic"], "maxUsers": 5, "trialEndsAt": "2025-12-31"}')
ON CONFLICT (id) DO NOTHING;

-- Insert platform admin user
INSERT INTO app.users (id, email, username, first_name, last_name, display_name, is_platform_admin, status)
VALUES 
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'admin@platform.local', 'admin', 'Platform', 'Admin', 
     'Platform Admin', true, 'active')
ON CONFLICT (id) DO NOTHING;

-- Insert regular users
INSERT INTO app.users (id, email, username, first_name, last_name, display_name, is_platform_admin, status)
VALUES 
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'user@test.local', 'testuser', 'Test', 'User',
     'Test User', false, 'active'),
    ('cccccccc-cccc-cccc-cccc-cccccccccccc', 'demo@test.local', 'demouser', 'Demo', 'User',
     'Demo User', false, 'active'),
    ('dddddddd-dddd-dddd-dddd-dddddddddddd', 'john.doe@example.com', 'johndoe', 'John', 'Doe',
     'John Doe', false, 'active'),
    ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', 'jane.smith@example.com', 'janesmith', 'Jane', 'Smith',
     'Jane Smith', false, 'active')
ON CONFLICT (id) DO NOTHING;

-- Assign users to tenants
INSERT INTO app.user_tenants (user_id, tenant_id, role, is_default)
VALUES 
    -- Platform admin has access to all tenants
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'owner', true),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '22222222-2222-2222-2222-222222222222', 'owner', false),
    ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '33333333-3333-3333-3333-333333333333', 'owner', false),
    
    -- Regular user in default tenant
    ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '11111111-1111-1111-1111-111111111111', 'member', true),
    
    -- Demo user in demo tenant
    ('cccccccc-cccc-cccc-cccc-cccccccccccc', '22222222-2222-2222-2222-222222222222', 'admin', true),
    
    -- John Doe in multiple tenants
    ('dddddddd-dddd-dddd-dddd-dddddddddddd', '11111111-1111-1111-1111-111111111111', 'member', true),
    ('dddddddd-dddd-dddd-dddd-dddddddddddd', '22222222-2222-2222-2222-222222222222', 'viewer', false),
    
    -- Jane Smith as admin in test tenant
    ('eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee', '33333333-3333-3333-3333-333333333333', 'admin', true)
ON CONFLICT (user_id, tenant_id) DO NOTHING;

-- Insert system roles for each tenant
INSERT INTO app.roles (tenant_id, name, display_name, description, permissions, is_system)
SELECT 
    t.id,
    r.name,
    r.display_name,
    r.description,
    r.permissions,
    true
FROM app.tenants t
CROSS JOIN (
    VALUES 
        ('super_admin', 'Super Admin', 'Full system access', '["*"]'::jsonb),
        ('admin', 'Administrator', 'Tenant administration access', 
         '["users.read", "users.write", "users.delete", "settings.read", "settings.write"]'::jsonb),
        ('member', 'Member', 'Standard member access', 
         '["profile.read", "profile.write", "data.read", "data.write"]'::jsonb),
        ('viewer', 'Viewer', 'Read-only access', 
         '["profile.read", "data.read"]'::jsonb)
) AS r(name, display_name, description, permissions)
ON CONFLICT (tenant_id, name) DO NOTHING;

-- Create some audit log entries for testing
INSERT INTO audit.activity_log (tenant_id, user_id, action, entity_type, entity_id, metadata)
VALUES 
    ('11111111-1111-1111-1111-111111111111', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 
     'user.login', 'user', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 
     '{"ip": "127.0.0.1", "browser": "Chrome"}'::jsonb),
    ('22222222-2222-2222-2222-222222222222', 'cccccccc-cccc-cccc-cccc-cccccccccccc',
     'tenant.settings.update', 'tenant', '22222222-2222-2222-2222-222222222222',
     '{"changes": ["theme", "maxUsers"]}'::jsonb),
    ('11111111-1111-1111-1111-111111111111', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
     'data.create', 'record', uuid_generate_v4(),
     '{"recordType": "document", "recordName": "Test Document"}'::jsonb);

COMMIT;

DO $$
BEGIN
    RAISE NOTICE 'Platform seed data created successfully.';
    RAISE NOTICE '';
    RAISE NOTICE 'Test Users:';
    RAISE NOTICE '  - admin@platform.local (Platform Admin)';
    RAISE NOTICE '  - user@test.local (Regular User)';
    RAISE NOTICE '  - demo@test.local (Demo User)';
    RAISE NOTICE '  - john.doe@example.com (Multi-tenant User)';
    RAISE NOTICE '  - jane.smith@example.com (Test Tenant Admin)';
    RAISE NOTICE '';
    RAISE NOTICE 'Test Tenants:';
    RAISE NOTICE '  - Default Tenant (subdomain: default)';
    RAISE NOTICE '  - Demo Company (subdomain: demo)';
    RAISE NOTICE '  - Test Organization (subdomain: test)';
END $$;