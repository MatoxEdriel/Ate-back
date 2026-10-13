# 🐳 Ate Backend — Docker Guide (.NET 10 + PostgreSQL 16)

> **Propósito:** Esta guía contiene la especificación completa de la imagen Docker de la API de **Ate**, el archivo `Dockerfile`, `.dockerignore`, `docker-compose.yml` y los comandos listos para copiar y usar en cualquier entorno (local, VPS, CI/CD o Kubernetes).

---

## 📌 1. Imágenes Oficiales Recomendadas

* **Compilación (Build Stage):** `mcr.microsoft.com/dotnet/sdk:10.0-alpine`
  * Imagen oficial de Microsoft para compilar .NET 10.
  * Basada en Alpine Linux para máxima velocidad de descarga y ligereza.
* **Ejecución (Runtime Stage):** `mcr.microsoft.com/dotnet/aspnet:10.0-alpine`
  * Imagen oficial de runtime para ASP.NET Core 10.
  * Tamaño mínimo (~110 MB en disco).
  * Incluye por defecto el usuario no privilegiado `app` (seguridad hardened).
  * Puerto interno por defecto en contenedores .NET 10: **`8080`**.

---

## 📄 2. Dockerfile Completo (Multi-Stage)

Ubicación recomendada: `backend/Dockerfile`

```dockerfile
# =============================================================================
# Build Stage (.NET 10 SDK)
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# Copiar proyectos primero para aprovechar el caché de capas de Docker
COPY ["Ate.Domain/Ate.Domain.csproj", "Ate.Domain/"]
COPY ["Ate.Application/Ate.Application.csproj", "Ate.Application/"]
COPY ["Ate.Infrastructure/Ate.Infrastructure.csproj", "Ate.Infrastructure/"]
COPY ["Ate.Api/Ate.Api.csproj", "Ate.Api/"]

# Restaurar dependencias de NuGet
RUN dotnet restore "Ate.Api/Ate.Api.csproj"

# Copiar todo el código fuente
COPY . .

# Compilar y publicar binarios optimizados en Release
RUN dotnet publish "Ate.Api/Ate.Api.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# =============================================================================
# Runtime Stage (ASP.NET Core 10 Runtime - Alpine Linux)
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Exponer el puerto del contenedor
EXPOSE 8080

# Variables de entorno por defecto
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Copiar artefactos publicados desde la etapa de compilación
COPY --from=build /app/publish .

# Ejecutar como usuario no-root por seguridad
USER app

ENTRYPOINT ["dotnet", "Ate.Api.dll"]
```

---

## 📄 3. Archivo .dockerignore

Ubicación: `backend/.dockerignore`

```text
**/.git
**/.gitignore
**/.vs
**/.vscode
**/bin
**/obj
**/docker-compose*.yml
**/*.user
**/*.suo
**/node_modules
docs/
```

---

## 📄 4. docker-compose.yml (Orquestación Completa)

Ubicación: `backend/docker-compose.yml`

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16-alpine
    container_name: ate_postgres_db
    restart: always
    environment:
      POSTGRES_DB: ate_db
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - ./database/01_init_schema.sql:/docker-entrypoint-initdb.d/01_init_schema.sql
      - ./database/02_seed_data.sql:/docker-entrypoint-initdb.d/02_seed_data.sql
      - postgres_ate_data:/var/lib/postgresql/data

  api:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: ate_backend_api
    restart: always
    ports:
      - "5252:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=ate_db;Username=postgres;Password=postgres
    depends_on:
      - postgres

volumes:
  postgres_ate_data:
```

---

## 🚀 5. Comandos para Levantar y Gestionar

### A. Levantar API + PostgreSQL en conjunto (Recomendado)
```bash
# Construir y levantar en segundo plano
docker compose up -d --build

# Ver logs en tiempo real
docker compose logs -f api

# Detener los contenedores
docker compose down

# Detener y eliminar volúmenes (limpieza total de base de datos)
docker compose down -v
```

### B. Construir y correr solo la imagen de la API
```bash
# 1. Construir la imagen
docker build -t ate-api:1.0 .

# 2. Correr el contenedor conectando a tu PostgreSQL externo
docker run -d \
  --name ate-api \
  -p 5252:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__DefaultConnection="Host=tu-host;Port=5432;Database=ate_db;Username=postgres;Password=postgres" \
  ate-api:1.0
```

---

## 🌐 6. URLs del Contenedor una vez levantado

* **Scalar API Reference (Documentación interactiva):** `http://localhost:5252/scalar/v1`
* **OpenAPI JSON Spec:** `http://localhost:5252/openapi/v1.json`
* **Health Check:** `http://localhost:5252/api/health`
* **Conexión a PostgreSQL (DBeaver / psql):** `localhost:5432` (Base de datos: `ate_db`, Usuario: `postgres`, Password: `postgres`)
