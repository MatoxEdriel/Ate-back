-- =============================================================================
-- Usuario de la aplicación con mínimo privilegio (ISO 27001 A.8.2 / OWASP A01).
-- La API se conecta como ate_app: puede leer y escribir datos, pero NO crear, alterar ni borrar tablas.
-- El esquema (01_init_schema.sql) lo aplica el dueño de la base, nunca ate_app.
--
-- Uso (la contraseña llega por variable, nunca escrita aquí):
--   psql -h <host> -U <dueño> -d ate_db -v app_password="$POSTGRES_APP_PASSWORD" -f database/03_app_role.sql
-- =============================================================================

\if :{?app_password}
\else
  \echo 'Falta -v app_password=...'
  \quit
\endif

SELECT format('CREATE ROLE ate_app LOGIN PASSWORD %L', :'app_password')
WHERE NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'ate_app')
\gexec

-- Si ya existía, se actualiza la contraseña.
SELECT format('ALTER ROLE ate_app WITH LOGIN PASSWORD %L', :'app_password')
\gexec

-- Sin privilegios de administración.
ALTER ROLE ate_app NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION;

SELECT format('GRANT CONNECT ON DATABASE %I TO ate_app', current_database())
\gexec

REVOKE CREATE ON SCHEMA public FROM PUBLIC;
REVOKE CREATE ON SCHEMA public FROM ate_app;
GRANT USAGE ON SCHEMA public TO ate_app;

GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO ate_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO ate_app;

-- Tablas y secuencias que el dueño cree en el futuro.
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO ate_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO ate_app;
