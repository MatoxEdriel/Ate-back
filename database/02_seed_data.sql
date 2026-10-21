-- =============================================================================
-- Ate Platform - Seed Data Script
-- Purpose: Initial catalog roles and default SuperAdmin account.
-- Idempotent: Can be run multiple times safely (ON CONFLICT DO NOTHING).
-- =============================================================================

-- 1. ROLES CATALOG
INSERT INTO roles (id, name, description, created_at)
OVERRIDING SYSTEM VALUE
VALUES
    (1, 'SuperAdmin', 'Platform super administrator', CURRENT_TIMESTAMP),
    (2, 'CompanyAdmin', 'Company client administrator', CURRENT_TIMESTAMP),
    (3, 'Employee', 'Company employee', CURRENT_TIMESTAMP),
    (4, 'Merchant', 'Affiliated merchant', CURRENT_TIMESTAMP)
ON CONFLICT (name) DO NOTHING;

SELECT setval(pg_get_serial_sequence('roles', 'id'), COALESCE(MAX(id), 1)) FROM roles;

-- 2. DEFAULT SUPERADMIN USER
-- Default Credentials:
--   Identifier / Email: admin@ate.com
--   Identifier / Document: 0943676155
--   Password: Admin123*Ate (PBKDF2 SHA-256 with 100,000 iterations)
INSERT INTO users (
    id,
    email,
    password_hash,
    is_active,
    email_confirmed,
    must_change_password,
    created_at,
    is_deleted
)
VALUES (
    '01928000-0000-7000-8000-000000000001',
    'admin@ate.com',
    '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY',
    TRUE,
    TRUE,
    FALSE,
    CURRENT_TIMESTAMP,
    FALSE
)
ON CONFLICT (email) DO NOTHING;

-- 3. DEFAULT SUPERADMIN PERSON PROFILE
INSERT INTO persons (
    id,
    user_id,
    first_name,
    last_name,
    document_type,
    document_number,
    created_at,
    is_deleted
)
VALUES (
    '01928000-0000-7000-8000-000000000002',
    '01928000-0000-7000-8000-000000000001',
    'SuperAdmin',
    'Ate',
    'NationalId',
    '0943676155',
    CURRENT_TIMESTAMP,
    FALSE
)
ON CONFLICT (document_type, document_number) DO NOTHING;

-- 4. ASSIGN SUPERADMIN ROLE TO ADMIN USER
INSERT INTO user_roles (
    user_id,
    role_id,
    assigned_at
)
VALUES (
    '01928000-0000-7000-8000-000000000001',
    1,
    CURRENT_TIMESTAMP
)
ON CONFLICT (user_id, role_id) DO NOTHING;
