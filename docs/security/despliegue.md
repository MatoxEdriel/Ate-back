# Despliegue seguro

Guía para levantar Ate en producción sin exponer secretos ni funciones de desarrollo.

## 1. Secretos

- Nunca en archivos versionados. Local: `dotnet user-secrets` (API) y `.env` (Docker). Producción: el gestor de
  secretos de la plataforma, o un `.env.prod` fuera del repo con permisos `600`.
- Variables que pide `docker-compose.prod.yml`: ver la sección "Producción" de `.env.example`.
- `JWT_SECRET_KEY`: 32 bytes aleatorios como mínimo (`openssl rand -base64 48`). Fuera de Development la API no
  arranca con una clave corta o de ejemplo.
- Las cuentas de ejemplo solo existen en Development y usan `Seed:DefaultPassword`.

## 2. API

```bash
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
```

- Corre en `Production`: sin `/auth/dev-token`, sin documentación Scalar y sin seed.
- PostgreSQL y Mongo no publican puertos; la API solo escucha en `127.0.0.1:5252`, detrás del proxy.
- **El proxy debe terminar TLS** y enviar `X-Forwarded-For` y `X-Forwarded-Proto`. Su red va en
  `PROXY_NETWORK`. Con eso la API ve la IP real (límite de intentos, auditoría) y fuerza HTTPS sin bucles.
- `FRONTEND_ORIGIN` es el único origen que CORS acepta. `API_ALLOWED_HOSTS` limita el encabezado `Host`.
- **Frontend y API en el mismo dominio** (`app.ate.com` y `api.ate.com`): la sesión usa una cookie
  `SameSite=Strict` que los navegadores bloquean entre dominios distintos.

## 3. Bases de datos con mínimo privilegio

1. El esquema (`01_init_schema.sql`) lo aplica el dueño (`POSTGRES_OWNER_USER`); en el primer arranque lo hace
   el contenedor de PostgreSQL.
2. Crear el usuario de la aplicación, sin permisos de DDL:
   ```bash
   psql -h <host> -U postgres -d ate_db -v app_password="$POSTGRES_APP_PASSWORD" -f database/03_app_role.sql
   ```
3. Mongo:
   ```bash
   mongosh "mongodb://<root>:<pwd>@<host>:27017/?authSource=admin" \
     --eval "const appPassword='$MONGO_APP_PASSWORD'" database/mongo/create-app-user.js
   ```
4. Los cambios de esquema futuros se aplican con el dueño, nunca con `ate_app`.

## 4. Frontend

- Servir `dist/AteFrontend/browser` con `deploy/nginx.conf`: HTTPS, HSTS, CSP y demás cabeceras.
- Ajustar `server_name` y `connect-src` (la URL real de la API).
- Si cambia un `<script>` en línea de `index.html`, recalcular su hash para `script-src`:
  ```bash
  python3 -c "import re,hashlib,base64,sys;s=open(sys.argv[1]).read();[print(\"'sha256-\"+base64.b64encode(hashlib.sha256(x.encode()).digest()).decode()+\"'\") for x in re.findall(r'<script(?![^>]*src=)[^>]*>(.*?)</script>',s,re.S)]" dist/AteFrontend/browser/index.html
  ```

## 5. Después de desplegar

- Revisar que `GET /api/v1/auth/dev-token` responda 404.
- Revisar las cabeceras con `curl -I https://api.ate.com/api/health` y `https://app.ate.com`.
- Revisar que los eventos de seguridad lleguen a la tabla `security_events`.
