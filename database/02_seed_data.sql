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
    '0943676157',
    CURRENT_TIMESTAMP,
    FALSE
)
ON CONFLICT (document_type, document_number) DO NOTHING;

UPDATE persons
SET document_number = '0943676157'
WHERE user_id = '01928000-0000-7000-8000-000000000010';

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
    logo_url,
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
    '/assets/companies/acme-logo.svg',
    TRUE,
    CURRENT_TIMESTAMP,
    FALSE
)
ON CONFLICT (tax_id) WHERE is_deleted = FALSE DO UPDATE
SET logo_url = EXCLUDED.logo_url;

-- 6.1 LINK COMPANY ADMIN TO ITS COMPANY
UPDATE users
SET company_id = (
    SELECT id FROM companies WHERE tax_id = '0990000001001' AND is_deleted = FALSE
)
WHERE email = 'admin@ate.com' AND company_id IS NULL;

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
    '0943676156',
    CURRENT_TIMESTAMP,
    FALSE
)
ON CONFLICT (document_type, document_number) DO NOTHING;

UPDATE persons
SET document_number = '0943676156'
WHERE user_id = '01928000-0000-7000-8000-000000000030';

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
    monthly_allowance,
    is_intern,
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
    120.00,
    FALSE,
    '01928000000070008000000000000032',
    TRUE,
    CURRENT_TIMESTAMP,
    FALSE
)
ON CONFLICT DO NOTHING;

UPDATE employees
SET monthly_allowance = 120.00
WHERE id = '01928000-0000-7000-8000-000000000032' AND monthly_allowance = 0;

-- 7.5 DEFAULT MERCHANT ACCOUNT (For affiliated merchant role)
-- Default Credentials:
--   Identifier / Email: comercio@ate.com
--   Identifier / Document: 0943676158
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
    '01928000-0000-7000-8000-000000000040',
    'comercio@ate.com',
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
    '01928000-0000-7000-8000-000000000041',
    '01928000-0000-7000-8000-000000000040',
    'Comercio',
    'Aliado',
    'NationalId',
    '0943676158',
    CURRENT_TIMESTAMP,
    FALSE
)
ON CONFLICT (document_type, document_number) DO NOTHING;

UPDATE persons
SET document_number = '0943676158'
WHERE user_id = '01928000-0000-7000-8000-000000000040';

INSERT INTO user_roles (
    user_id,
    role_id,
    assigned_at
)
VALUES (
    '01928000-0000-7000-8000-000000000040',
    4,
    CURRENT_TIMESTAMP
)
ON CONFLICT (user_id, role_id) DO NOTHING;

-- 8. SAMPLE EMPLOYEES FOR ACME (Development demo data)
--   Password for all sample accounts: Admin123*Ate
INSERT INTO users (id, email, password_hash, is_active, email_confirmed, must_change_password, created_at, is_deleted)
VALUES
    ('01928000-0000-7000-8000-000000000106', 'maria.cevallos@acme.com', '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY', TRUE, TRUE, FALSE, CURRENT_TIMESTAMP - INTERVAL '60 days', FALSE),
    ('01928000-0000-7000-8000-000000000109', 'jose.paredes@acme.com', '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY', TRUE, TRUE, FALSE, CURRENT_TIMESTAMP - INTERVAL '56 days', FALSE),
    ('01928000-0000-7000-8000-000000000112', 'ana.torres@acme.com', '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY', TRUE, TRUE, FALSE, CURRENT_TIMESTAMP - INTERVAL '52 days', FALSE),
    ('01928000-0000-7000-8000-000000000115', 'luis.zambrano@acme.com', '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY', TRUE, TRUE, FALSE, CURRENT_TIMESTAMP - INTERVAL '48 days', FALSE),
    ('01928000-0000-7000-8000-000000000118', 'gabriela.salazar@acme.com', '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY', TRUE, TRUE, FALSE, CURRENT_TIMESTAMP - INTERVAL '44 days', FALSE),
    ('01928000-0000-7000-8000-000000000121', 'diego.herrera@acme.com', '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY', TRUE, TRUE, FALSE, CURRENT_TIMESTAMP - INTERVAL '40 days', FALSE),
    ('01928000-0000-7000-8000-000000000124', 'valeria.montalvo@acme.com', '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY', TRUE, TRUE, FALSE, CURRENT_TIMESTAMP - INTERVAL '36 days', FALSE),
    ('01928000-0000-7000-8000-000000000127', 'carlos.villacis@acme.com', '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY', TRUE, TRUE, FALSE, CURRENT_TIMESTAMP - INTERVAL '32 days', FALSE),
    ('01928000-0000-7000-8000-000000000130', 'daniela.aguirre@acme.com', '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY', TRUE, TRUE, FALSE, CURRENT_TIMESTAMP - INTERVAL '28 days', FALSE),
    ('01928000-0000-7000-8000-000000000133', 'miguel.rivadeneira@acme.com', '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY', TRUE, TRUE, FALSE, CURRENT_TIMESTAMP - INTERVAL '24 days', FALSE),
    ('01928000-0000-7000-8000-000000000136', 'sofia.benitez@acme.com', '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY', TRUE, TRUE, FALSE, CURRENT_TIMESTAMP - INTERVAL '20 days', FALSE),
    ('01928000-0000-7000-8000-000000000139', 'andres.jaramillo@acme.com', '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY', TRUE, TRUE, FALSE, CURRENT_TIMESTAMP - INTERVAL '16 days', FALSE),
    ('01928000-0000-7000-8000-000000000142', 'paola.navarrete@acme.com', '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY', TRUE, TRUE, FALSE, CURRENT_TIMESTAMP - INTERVAL '12 days', FALSE),
    ('01928000-0000-7000-8000-000000000145', 'ricardo.espinoza@acme.com', '735zKhX+F8ep7goJoiIsrwyQv5wzp5GQOgbzwK7yH/tNCOCsLNWyWmwJUcKiRPzY', TRUE, TRUE, FALSE, CURRENT_TIMESTAMP - INTERVAL '8 days', FALSE)
ON CONFLICT DO NOTHING;

INSERT INTO persons (id, user_id, first_name, last_name, document_type, document_number, created_at, is_deleted)
VALUES
    ('01928000-0000-7000-8000-000000000107', '01928000-0000-7000-8000-000000000106', 'María Belén', 'Cevallos Ruiz', 'NationalId', '0923456781', CURRENT_TIMESTAMP, FALSE),
    ('01928000-0000-7000-8000-000000000110', '01928000-0000-7000-8000-000000000109', 'José Andrés', 'Paredes Vera', 'NationalId', '0934567812', CURRENT_TIMESTAMP, FALSE),
    ('01928000-0000-7000-8000-000000000113', '01928000-0000-7000-8000-000000000112', 'Ana Lucía', 'Torres Mendoza', 'NationalId', '0945678123', CURRENT_TIMESTAMP, FALSE),
    ('01928000-0000-7000-8000-000000000116', '01928000-0000-7000-8000-000000000115', 'Luis Fernando', 'Zambrano Ortiz', 'NationalId', '0956781234', CURRENT_TIMESTAMP, FALSE),
    ('01928000-0000-7000-8000-000000000119', '01928000-0000-7000-8000-000000000118', 'Gabriela', 'Salazar Moreira', 'NationalId', '0967812345', CURRENT_TIMESTAMP, FALSE),
    ('01928000-0000-7000-8000-000000000122', '01928000-0000-7000-8000-000000000121', 'Diego Alejandro', 'Herrera Castro', 'NationalId', '0978123456', CURRENT_TIMESTAMP, FALSE),
    ('01928000-0000-7000-8000-000000000125', '01928000-0000-7000-8000-000000000124', 'Valeria', 'Montalvo Quiñónez', 'NationalId', '0989234567', CURRENT_TIMESTAMP, FALSE),
    ('01928000-0000-7000-8000-000000000128', '01928000-0000-7000-8000-000000000127', 'Carlos Eduardo', 'Villacís Romero', 'NationalId', '0991345678', CURRENT_TIMESTAMP, FALSE),
    ('01928000-0000-7000-8000-000000000131', '01928000-0000-7000-8000-000000000130', 'Daniela', 'Aguirre Pazmiño', 'NationalId', '0902456789', CURRENT_TIMESTAMP, FALSE),
    ('01928000-0000-7000-8000-000000000134', '01928000-0000-7000-8000-000000000133', 'Miguel Ángel', 'Rivadeneira León', 'NationalId', '0913567890', CURRENT_TIMESTAMP, FALSE),
    ('01928000-0000-7000-8000-000000000137', '01928000-0000-7000-8000-000000000136', 'Sofía', 'Benítez Andrade', 'NationalId', '0924678901', CURRENT_TIMESTAMP, FALSE),
    ('01928000-0000-7000-8000-000000000140', '01928000-0000-7000-8000-000000000139', 'Andrés Sebastián', 'Jaramillo Cruz', 'NationalId', '0935789012', CURRENT_TIMESTAMP, FALSE),
    ('01928000-0000-7000-8000-000000000143', '01928000-0000-7000-8000-000000000142', 'Paola Estefanía', 'Navarrete Solís', 'NationalId', '0946890123', CURRENT_TIMESTAMP, FALSE),
    ('01928000-0000-7000-8000-000000000146', '01928000-0000-7000-8000-000000000145', 'Ricardo', 'Espinoza Guerrero', 'NationalId', '0957901234', CURRENT_TIMESTAMP, FALSE)
ON CONFLICT DO NOTHING;

INSERT INTO user_roles (user_id, role_id, assigned_at)
VALUES
    ('01928000-0000-7000-8000-000000000106', 3, CURRENT_TIMESTAMP),
    ('01928000-0000-7000-8000-000000000109', 3, CURRENT_TIMESTAMP),
    ('01928000-0000-7000-8000-000000000112', 3, CURRENT_TIMESTAMP),
    ('01928000-0000-7000-8000-000000000115', 3, CURRENT_TIMESTAMP),
    ('01928000-0000-7000-8000-000000000118', 3, CURRENT_TIMESTAMP),
    ('01928000-0000-7000-8000-000000000121', 3, CURRENT_TIMESTAMP),
    ('01928000-0000-7000-8000-000000000124', 3, CURRENT_TIMESTAMP),
    ('01928000-0000-7000-8000-000000000127', 3, CURRENT_TIMESTAMP),
    ('01928000-0000-7000-8000-000000000130', 3, CURRENT_TIMESTAMP),
    ('01928000-0000-7000-8000-000000000133', 3, CURRENT_TIMESTAMP),
    ('01928000-0000-7000-8000-000000000136', 3, CURRENT_TIMESTAMP),
    ('01928000-0000-7000-8000-000000000139', 3, CURRENT_TIMESTAMP),
    ('01928000-0000-7000-8000-000000000142', 3, CURRENT_TIMESTAMP),
    ('01928000-0000-7000-8000-000000000145', 3, CURRENT_TIMESTAMP)
ON CONFLICT DO NOTHING;

INSERT INTO employees (id, company_id, person_id, employee_code, department, monthly_allowance, is_intern, qr_code_token, is_active, created_at, is_deleted)
SELECT v.id::uuid, c.id, p.id, v.code, v.department, v.allowance, v.is_intern, replace(v.id, '-', ''), v.is_active, CURRENT_TIMESTAMP - (v.days_ago || ' days')::interval, FALSE
FROM (VALUES
    ('01928000-0000-7000-8000-000000000108', '0923456781', 'EMP-002', 'Talento Humano', 150.00, FALSE, TRUE, 60),
    ('01928000-0000-7000-8000-000000000111', '0934567812', 'EMP-003', 'Tecnología', 120.00, FALSE, TRUE, 56),
    ('01928000-0000-7000-8000-000000000114', '0945678123', 'EMP-004', 'Finanzas', 180.00, FALSE, TRUE, 52),
    ('01928000-0000-7000-8000-000000000117', '0956781234', 'EMP-005', 'Operaciones', 100.00, FALSE, TRUE, 48),
    ('01928000-0000-7000-8000-000000000120', '0967812345', 'EMP-006', 'Ventas', 130.00, FALSE, TRUE, 44),
    ('01928000-0000-7000-8000-000000000123', '0978123456', 'EMP-007', 'Tecnología', 160.00, FALSE, TRUE, 40),
    ('01928000-0000-7000-8000-000000000126', '0989234567', 'EMP-008', 'Administración', 110.00, FALSE, FALSE, 36),
    ('01928000-0000-7000-8000-000000000129', '0991345678', 'EMP-009', 'Operaciones', 100.00, FALSE, TRUE, 32),
    ('01928000-0000-7000-8000-000000000132', '0902456789', 'EMP-010', 'Ventas', 140.00, FALSE, TRUE, 28),
    ('01928000-0000-7000-8000-000000000135', '0913567890', 'EMP-011', 'Finanzas', 200.00, FALSE, TRUE, 24),
    ('01928000-0000-7000-8000-000000000138', '0924678901', 'EMP-012', 'Talento Humano', 120.00, FALSE, FALSE, 20),
    ('01928000-0000-7000-8000-000000000141', '0935789012', 'EMP-013', 'Tecnología', 175.00, FALSE, TRUE, 16),
    ('01928000-0000-7000-8000-000000000144', '0946890123', 'EMP-014', 'Administración', 115.00, TRUE, TRUE, 12),
    ('01928000-0000-7000-8000-000000000147', '0957901234', 'EMP-015', 'Operaciones', 105.00, TRUE, TRUE, 8)
) AS v(id, document_number, code, department, allowance, is_intern, is_active, days_ago)
JOIN persons p ON p.document_number = v.document_number AND p.document_type = 'NationalId' AND p.is_deleted = FALSE
JOIN companies c ON c.tax_id = '0990000001001' AND c.is_deleted = FALSE
ON CONFLICT DO NOTHING;

UPDATE employees
SET is_intern = TRUE
WHERE employee_code IN ('EMP-014', 'EMP-015');

UPDATE employees
SET is_intern = FALSE
WHERE employee_code NOT IN ('EMP-014', 'EMP-015');
