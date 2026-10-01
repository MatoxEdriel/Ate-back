# 🗄️ Ate — Guía de Base de Datos, Esquema & Seed Data (PostgreSQL 16)

Este directorio contiene los scripts SQL oficiales para inicializar, poblar y recrear la base de datos de **Ate** en cualquier entorno (Local, Docker, Servidores de Pruebas o Producción).

---

## 📁 Archivos Disponibles

1. **[`01_init_schema.sql`](file:///d:/Ate/backend/database/01_init_schema.sql):**
   - Define el esquema completo DDL en **PostgreSQL 16 LTS**.
   - Crea las tablas `empresas`, `empleados`, `tipos_beneficio`, `saldos_empleado`, `comercios_afiliados` y `transacciones`.
   - Define claves primarias UUID/Identity, claves foráneas, restricciones `CHECK` e índices de alto rendimiento.

2. **[`02_seed_data.sql`](file:///d:/Ate/backend/database/02_seed_data.sql):**
   - Datos semilla iniciales (*Seed Data*) para desarrollo y pruebas.
   - Registra una empresa cliente de prueba (`TechCorp Solutions`), trabajadores, tipos de beneficios (`ALIMENTACION`, `SALUD`, `GENERAL`), saldos por periodo y consumos simulados vía QR.

---

## 🚀 Opciones de Ejecución

### Opción A: Con Docker Compose (Recomendado para Dev Local)

Al ejecutar Docker Compose desde la raíz del proyecto, PostgreSQL ejecutará automáticamente los scripts `01_init_schema.sql` y `02_seed_data.sql` al crear el contenedor por primera vez:

```bash
docker compose up -d
```

---

### Opción B: Ejecución Manual con `psql` o cliente SQL (DBeaver / pgAdmin / VS Code)

Si ya tienes PostgreSQL instalado localmente o en un servidor remoto:

```bash
# 1. Crear el esquema de tablas
psql -h localhost -U postgres -d ate_db -f backend/database/01_init_schema.sql

# 2. Insertar los datos semilla (Seed Data)
psql -h localhost -U postgres -d ate_db -f backend/database/02_seed_data.sql
```
