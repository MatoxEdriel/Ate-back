-- =============================================================================
-- SISTEMA ATE - ESQUEMA INICIAL DE BASE DE DATOS (PostgreSQL 16 LTS)
-- Archivo: 01_init_schema.sql
-- Descripción: Creación de tablas, llaves foráneas, índices y restricciones.
-- =============================================================================

-- Habilitar extensión para generación de UUID v4 si no existe
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- -----------------------------------------------------------------------------
-- 1. TABLA: empresas (Empresas cliente que otorgan beneficios/saldos)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS empresas (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nombre VARCHAR(150) NOT NULL,
    ruc_tax_id VARCHAR(20) NOT NULL UNIQUE,
    email_contacto VARCHAR(150) NOT NULL,
    telefono VARCHAR(20),
    direccion VARCHAR(255),
    estado VARCHAR(20) NOT NULL DEFAULT 'Activo' CHECK (estado IN ('Activo', 'Inactivo', 'Suspendido')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ
);

-- -----------------------------------------------------------------------------
-- 2. TABLA: empleados (Trabajadores de las empresas que consumen beneficios)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS empleados (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    empresa_id UUID NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    nombres VARCHAR(100) NOT NULL,
    apellidos VARCHAR(100) NOT NULL,
    documento_identidad VARCHAR(20) NOT NULL UNIQUE,
    email VARCHAR(150) NOT NULL UNIQUE,
    telefono VARCHAR(20),
    qr_code_hash VARCHAR(255) UNIQUE,
    estado VARCHAR(20) NOT NULL DEFAULT 'Activo' CHECK (estado IN ('Activo', 'Inactivo', 'Bloqueado')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ
);

-- -----------------------------------------------------------------------------
-- 3. TABLA: tipos_beneficio (Alimentación, Salud, General, etc.)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tipos_beneficio (
    id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    codigo VARCHAR(30) NOT NULL UNIQUE, -- Ej: 'ALIMENTACION', 'SALUD', 'GENERAL'
    nombre VARCHAR(100) NOT NULL,
    descripcion TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- -----------------------------------------------------------------------------
-- 4. TABLA: saldos_empleado (Saldos asignados a cada empleado por periodo)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS saldos_empleado (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    empleado_id UUID NOT NULL REFERENCES empleados(id) ON DELETE CASCADE,
    tipo_beneficio_id INT NOT NULL REFERENCES tipos_beneficio(id),
    monto_asignado NUMERIC(12, 2) NOT NULL DEFAULT 0.00 CHECK (monto_asignado >= 0),
    monto_consumido NUMERIC(12, 2) NOT NULL DEFAULT 0.00 CHECK (monto_consumido >= 0),
    moneda VARCHAR(5) NOT NULL DEFAULT 'USD',
    periodo_mes INT NOT NULL CHECK (periodo_mes BETWEEN 1 AND 12),
    periodo_anio INT NOT NULL CHECK (periodo_anio >= 2024),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ,
    CONSTRAINT uk_empleado_beneficio_periodo UNIQUE (empleado_id, tipo_beneficio_id, periodo_mes, periodo_anio)
);

-- -----------------------------------------------------------------------------
-- 5. TABLA: comercios_afiliados (Restaurantes, farmacias, supermercados)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS comercios_afiliados (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    nombre_comercial VARCHAR(150) NOT NULL,
    ruc VARCHAR(20) NOT NULL UNIQUE,
    categoria VARCHAR(50) NOT NULL CHECK (categoria IN ('Restaurante', 'Farmacia', 'Supermercado', 'Salud', 'General')),
    direccion VARCHAR(255),
    estado VARCHAR(20) NOT NULL DEFAULT 'Activo' CHECK (estado IN ('Activo', 'Inactivo')),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ
);

-- -----------------------------------------------------------------------------
-- 6. TABLA: transacciones (Registro de consumos vía QR o POS)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS transacciones (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    empleado_id UUID NOT NULL REFERENCES empleados(id),
    comercio_id UUID NOT NULL REFERENCES comercios_afiliados(id),
    saldo_empleado_id UUID NOT NULL REFERENCES saldos_empleado(id),
    monto NUMERIC(12, 2) NOT NULL CHECK (monto > 0),
    moneda VARCHAR(5) NOT NULL DEFAULT 'USD',
    codigo_qr_usado VARCHAR(255),
    estado VARCHAR(20) NOT NULL DEFAULT 'Aprobada' CHECK (estado IN ('Aprobada', 'Rechazada', 'Anulada')),
    metodo_pago VARCHAR(30) NOT NULL DEFAULT 'QR_APP',
    descripcion VARCHAR(255),
    fecha_transaccion TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- =============================================================================
-- ÍNDICES DE RENDIMIENTO DE BÚSQUEDA
-- =============================================================================
CREATE INDEX IF NOT EXISTS idx_empleados_empresa_id ON empleados(empresa_id);
CREATE INDEX IF NOT EXISTS idx_empleados_documento ON empleados(documento_identidad);
CREATE INDEX IF NOT EXISTS idx_empleados_qr_hash ON empleados(qr_code_hash);
CREATE INDEX IF NOT EXISTS idx_saldos_empleado_id ON saldos_empleado(empleado_id);
CREATE INDEX IF NOT EXISTS idx_transacciones_empleado_id ON transacciones(empleado_id);
CREATE INDEX IF NOT EXISTS idx_transacciones_fecha ON transacciones(fecha_transaccion DESC);
