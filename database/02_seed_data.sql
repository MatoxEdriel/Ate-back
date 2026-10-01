-- =============================================================================
-- SISTEMA ATE - DATOS SEMILLA / SEED DATA (PostgreSQL 16 LTS)
-- Archivo: 02_seed_data.sql
-- Descripción: Carga inicial de datos de prueba para desarrollo y testing.
-- =============================================================================

-- Limpiar tablas si es necesario (opcional en reinicios)
-- TRUNCATE empresas, empleados, tipos_beneficio, saldos_empleado, comercios_afiliados, transacciones RESTART IDENTITY CASCADE;

-- 1. INSERTAR TIPOS DE BENEFICIO
INSERT INTO tipos_beneficio (id, codigo, nombre, descripcion) VALUES
(1, 'ALIMENTACION', 'Saldo de Alimentación', 'Saldo exclusivo para restaurantes, cafeterías y supermercados'),
(2, 'SALUD', 'Saldo de Salud & Medicinas', 'Saldo exclusivo para farmacias, clínicas y consultas de salud'),
(3, 'GENERAL', 'Saldo General Corporativo', 'Saldo libre de libre disposición en comercios afiliados')
ON CONFLICT (codigo) DO NOTHING;

-- 2. INSERTAR EMPRESA DEMO
INSERT INTO empresas (id, nombre, ruc_tax_id, email_contacto, telefono, direccion, estado) VALUES
('a0000000-0000-0000-0000-000000000001', 'TechCorp Solutions S.A.C.', '20123456789', 'contacto@techcorp.com', '+51 987654321', 'Av. Javier Prado 1234, Lima', 'Activo')
ON CONFLICT (ruc_tax_id) DO UPDATE SET nombre = EXCLUDED.nombre;

-- 3. INSERTAR EMPLEADOS DEMO
INSERT INTO empleados (id, empresa_id, nombres, apellidos, documento_identidad, email, telefono, qr_code_hash, estado) VALUES
('b0000000-0000-0000-0000-000000000001', 'a0000000-0000-0000-0000-000000000001', 'Juan Carlos', 'Pérez Gómez', '45678901', 'juan.perez@techcorp.com', '+51 912345678', 'QR-EMP-45678901-HASH-2026', 'Activo'),
('b0000000-0000-0000-0000-000000000002', 'a0000000-0000-0000-0000-000000000001', 'María Elena', 'García López', '45678902', 'maria.garcia@techcorp.com', '+51 912345679', 'QR-EMP-45678902-HASH-2026', 'Activo')
ON CONFLICT (documento_identidad) DO NOTHING;

-- 4. INSERTAR SALDOS ASIGNADOS A EMPLEADO (PERIODO ACTUAL)
INSERT INTO saldos_empleado (id, empleado_id, tipo_beneficio_id, monto_asignado, monto_consumido, moneda, periodo_mes, periodo_anio) VALUES
('c0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 1, 150.00, 45.50, 'USD', 10, 2026),
('c0000000-0000-0000-0000-000000000002', 'b0000000-0000-0000-0000-000000000001', 2, 100.00, 0.00, 'USD', 10, 2026)
ON CONFLICT (empleado_id, tipo_beneficio_id, periodo_mes, periodo_anio) DO UPDATE 
SET monto_asignado = EXCLUDED.monto_asignado, monto_consumido = EXCLUDED.monto_consumido;

-- 5. INSERTAR COMERCIOS AFILIADOS
INSERT INTO comercios_afiliados (id, nombre_comercial, ruc, categoria, direccion, estado) VALUES
('d0000000-0000-0000-0000-000000000001', 'Restaurante El Gourmet Ate', '20987654321', 'Restaurante', 'Calle Los Olivos 456, Ate', 'Activo'),
('d0000000-0000-0000-0000-000000000002', 'Farmacia SaludTotal', '20987654322', 'Farmacia', 'Av. Central 789, Ate', 'Activo')
ON CONFLICT (ruc) DO NOTHING;

-- 6. INSERTAR TRANSACCIONES DEMO
INSERT INTO transacciones (id, empleado_id, comercio_id, saldo_empleado_id, monto, moneda, codigo_qr_usado, estado, metodo_pago, descripcion, fecha_transaccion) VALUES
('e0000000-0000-0000-0000-000000000001', 'b0000000-0000-0000-0000-000000000001', 'd0000000-0000-0000-0000-000000000001', 'c0000000-0000-0000-0000-000000000001', 45.50, 'USD', 'QR-EMP-45678901-HASH-2026', 'Aprobada', 'QR_APP', 'Almuerzo menú ejecutivo en Restaurante El Gourmet Ate', CURRENT_TIMESTAMP - INTERVAL '2 hours')
ON CONFLICT (id) DO NOTHING;
