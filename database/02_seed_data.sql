-- =============================================================================
-- Ate Platform - Seed Data Script
-- Purpose: Initial catalog roles, CompanyAdmin, SuperAdmin and Employee accounts.
-- Idempotent: Can be run multiple times safely (ON CONFLICT DO NOTHING / UPDATE).
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

-- 2. COMPANY ADMIN USER (Main dev account)
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

-- 3. COMPANY ADMIN PERSON PROFILE
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
    'Admin',
    'Empresa',
    'NationalId',
    '0943676155',
    CURRENT_TIMESTAMP,
    FALSE
)
ON CONFLICT (document_type, document_number) DO UPDATE
SET first_name = 'Admin', last_name = 'Empresa';

-- 4. ASSIGN COMPANYADMIN ROLE TO USER
INSERT INTO user_roles (
    user_id,
    role_id,
    assigned_at
)
VALUES (
    '01928000-0000-7000-8000-000000000001',
    2,
    CURRENT_TIMESTAMP
)
ON CONFLICT (user_id, role_id) DO NOTHING;

-- Clean legacy SuperAdmin assignment from this user if present
DELETE FROM user_roles
WHERE user_id = '01928000-0000-7000-8000-000000000001' AND role_id = 1;

-- 5. SUPERADMIN ACCOUNT
-- Default Credentials:
--   Identifier / Email: superadmin@ate.com
--   Identifier / Document: 0999999999
--   Password: Admin123*Ate
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
    '01928000-0000-7000-8000-000000000010',
    'superadmin@ate.com',
    '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY',
    TRUE,
    TRUE,
    FALSE,
    CURRENT_TIMESTAMP,
    FALSE
)
ON CONFLICT (email) DO NOTHING;

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
    '01928000-0000-7000-8000-000000000011',
    '01928000-0000-7000-8000-000000000010',
    'SuperAdmin',
    'Ate',
    'NationalId',
    '0999999999',
    CURRENT_TIMESTAMP,
    FALSE
)
ON CONFLICT (document_type, document_number) DO NOTHING;

INSERT INTO user_roles (
    user_id,
    role_id,
    assigned_at
)
VALUES (
    '01928000-0000-7000-8000-000000000010',
    1,
    CURRENT_TIMESTAMP
)
ON CONFLICT (user_id, role_id) DO NOTHING;

-- 6. DEFAULT SAMPLE COMPANY
INSERT INTO companies (
    id,
    business_name,
    trade_name,
    tax_id,
    email,
    phone,
    address,
    is_active,
    created_at,
    is_deleted
)
VALUES (
    '01928000-0000-7000-8000-000000000020',
    'Acme Corporation S.A.',
    'Acme Corp',
    '0990000001001',
    'contacto@acme.com',
    '042000000',
    'Av. Principal 123 y Calle B',
    TRUE,
    CURRENT_TIMESTAMP,
    FALSE
)
ON CONFLICT (tax_id) DO NOTHING;

-- 7. DEFAULT EMPLOYEE ACCOUNT (For collaborator meal dashboard)
-- Default Credentials:
--   Identifier / Email: empleado@ate.com
--   Identifier / Document: 0912345678
--   Password: Admin123*Ate
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
    '01928000-0000-7000-8000-000000000030',
    'empleado@ate.com',
    '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY',
    TRUE,
    TRUE,
    FALSE,
    CURRENT_TIMESTAMP,
    FALSE
)
ON CONFLICT (email) DO NOTHING;

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
    '01928000-0000-7000-8000-000000000031',
    '01928000-0000-7000-8000-000000000030',
    'Carlos',
    'Mendoza',
    'NationalId',
    '0912345678',
    CURRENT_TIMESTAMP,
    FALSE
)
ON CONFLICT (document_type, document_number) DO NOTHING;

INSERT INTO user_roles (
    user_id,
    role_id,
    assigned_at
)
VALUES (
    '01928000-0000-7000-8000-000000000030',
    3,
    CURRENT_TIMESTAMP
)
ON CONFLICT (user_id, role_id) DO NOTHING;

INSERT INTO employees (
    id,
    company_id,
    person_id,
    employee_code,
    department,
    qr_code_token,
    is_active,
    created_at,
    is_deleted
)
VALUES (
    '01928000-0000-7000-8000-000000000032',
    '01928000-0000-7000-8000-000000000020',
    '01928000-0000-7000-8000-000000000031',
    'EMP-001',
    'Tecnología',
    '01928000000070008000000000000032',
    TRUE,
    CURRENT_TIMESTAMP,
    FALSE
)
ON CONFLICT DO NOTHING;
