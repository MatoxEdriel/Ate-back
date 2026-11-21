# Ate — Backend (API .NET 10)

API REST de **Ate**: ASP.NET Core 10 con arquitectura limpia, **PostgreSQL 16** como base principal y
**MongoDB 8** para la data no relacional (pedidos, catálogo de platos).

---

## Índice

1. [Requisitos](#1-requisitos)
2. [Inicio rápido (5 minutos)](#2-inicio-rápido-5-minutos)
3. [Formas de levantar el back](#3-formas-de-levantar-el-back)
4. [Configuración y secretos](#4-configuración-y-secretos)
5. [Qué pasa al arrancar](#5-qué-pasa-al-arrancar)
6. [URLs útiles y autenticación en desarrollo](#6-urls-útiles-y-autenticación-en-desarrollo)
7. [Pruebas](#7-pruebas)
8. [Estructura del proyecto](#8-estructura-del-proyecto)
9. [Comandos de referencia](#9-comandos-de-referencia)
10. [Problemas frecuentes](#10-problemas-frecuentes)
11. [Producción](#11-producción)
12. [Más documentación](#12-más-documentación)

---

## 1. Requisitos

| Herramienta | Versión | Para qué |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | **10.0** (`dotnet --version` → `10.0.x`) | Compilar y correr la API |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Reciente, con `docker compose` v2 | PostgreSQL y MongoDB locales |
| `make` | Cualquiera (viene en macOS/Linux) | Atajos del `Makefile` (opcional) |
| `openssl` | Cualquiera | Generar secretos aleatorios |

Opcionales: DBeaver / `psql` para PostgreSQL y MongoDB Compass / `mongosh` para Mongo.

---

## 2. Inicio rápido (5 minutos)

Todos los comandos se ejecutan desde la carpeta `Ate-back/`.

```bash
# 1. Crear el .env con secretos (no se sube a git)
cp .env.example .env
#    Completa POSTGRES_PASSWORD, MONGO_PASSWORD y JWT_SECRET_KEY.
#    Para generar valores:  openssl rand -base64 48

# 2. Levantar las bases de datos
docker compose up -d --wait postgres mongo

# 3. Decirle a la API cómo conectarse (user-secrets, fuera del repo)
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Port=5432;Database=ate_db;Username=postgres;Password=<POSTGRES_PASSWORD>" --project Ate.Api
dotnet user-secrets set "ConnectionStrings:MongoDbConnection" \
  "mongodb://ate_admin:<MONGO_PASSWORD>@localhost:27017/?authSource=admin" --project Ate.Api
dotnet user-secrets set "JwtSettings:SecretKey" "<JWT_SECRET_KEY>" --project Ate.Api
dotnet user-secrets set "Seed:DefaultPassword" "<una-contraseña-para-cuentas-de-ejemplo>" --project Ate.Api

# 4. Correr la API con hot reload
make back          # o: dotnet watch --project Ate.Api
```

Abre <http://localhost:5252> → redirige a la documentación interactiva (**Scalar**).
Comprueba que está viva: `curl http://localhost:5252/api/health`.

> Reemplaza `<POSTGRES_PASSWORD>`, `<MONGO_PASSWORD>` y `<JWT_SECRET_KEY>` por los **mismos valores** que pusiste en `.env`.

---

## 3. Formas de levantar el back

### Opción A — API local + bases en Docker (recomendada para desarrollar)

Es la del inicio rápido. Tienes hot reload, depuración desde el IDE y logs en la consola.

```bash
docker compose up -d --wait postgres mongo
make back                # = make mongo + dotnet watch --project Ate.Api
```

> `make back` solo levanta **Mongo** en Docker: el `Makefile` asume que PostgreSQL ya está corriendo
> (en Docker con el comando de arriba, o un PostgreSQL instalado/compartido). Por eso conviene levantar
> `postgres` primero.

También puedes correrla desde **Visual Studio / Rider / VS Code** con el perfil `http` de
`Ate.Api/Properties/launchSettings.json` (puerto `5252`, abre Scalar automáticamente).

### Opción B — Todo en Docker (API + PostgreSQL + MongoDB)

Útil para probar el frontend sin instalar .NET, o para reproducir un entorno parecido al servidor.

```bash
docker compose up -d --build
docker compose logs -f api     # ver logs de la API
```

Las cadenas de conexión y la clave JWT las arma `docker-compose.yml` a partir del `.env`; no hace falta
configurar user-secrets. La API queda en <http://localhost:5252>.

> En esta modalidad, `Seed:DefaultPassword` no está definido: la API genera una contraseña aleatoria
> para las cuentas de ejemplo y la imprime **una vez** en el log (`docker compose logs api | grep Seed`).

### Opción C — Sin Docker

Si ya tienes PostgreSQL 16 y MongoDB 8 instalados en tu máquina (o en otro servidor), configura las cadenas
de conexión con user-secrets (paso 3 del inicio rápido) apuntando a ellos y ejecuta:

```bash
make run           # = dotnet run --project Ate.Api
```

Crea antes la base vacía `ate_db` en PostgreSQL; el esquema lo aplica la API al arrancar (ver sección 5).

---

## 4. Configuración y secretos

ASP.NET Core lee la configuración en cascada; cada nivel **sobrescribe** al anterior:

```
appsettings.json  →  appsettings.Development.json  →  user-secrets  →  variables de entorno
   (en git)              (en git)                      (tu máquina)     (Docker / servidor)
```

**Regla:** ningún secreto en archivos versionados. En local usa `dotnet user-secrets` (API) y `.env` (Docker).

### Claves que debes definir

| Clave (user-secrets) | Variable de entorno equivalente | Obligatoria | Descripción |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings__DefaultConnection` | Sí | Conexión a PostgreSQL |
| `ConnectionStrings:MongoDbConnection` | `ConnectionStrings__MongoDbConnection` | Recomendada | Conexión a MongoDB. Si falta, la API arranca pero sin pedidos/catálogo |
| `JwtSettings:SecretKey` | `JwtSettings__SecretKey` | Sí | Firma de los JWT. **Mínimo 32 caracteres aleatorios** |
| `Seed:DefaultPassword` | `Seed__DefaultPassword` | No | Contraseña de las cuentas de ejemplo (solo Development) |
| `SmtpSettings:Password` | `SmtpSettings__Password` | No | App Password de Gmail para enviar correos (recuperar contraseña) |

### Variables del `.env` (Docker Compose)

| Variable | Uso |
|---|---|
| `POSTGRES_PASSWORD` | Contraseña del usuario `postgres` del contenedor |
| `MONGO_PASSWORD` | Contraseña del usuario root `ate_admin` de Mongo |
| `JWT_SECRET_KEY` | Clave JWT que recibe la API cuando corre en Docker |

### Comandos útiles de user-secrets

```bash
dotnet user-secrets list   --project Ate.Api          # ver lo configurado
dotnet user-secrets remove "Seed:DefaultPassword" --project Ate.Api
dotnet user-secrets clear  --project Ate.Api          # borrar todo
```

Otros valores ajustables (CORS, expiración de tokens, límites de login, almacenamiento de archivos, logs) están
en `Ate.Api/appsettings.json`. Detalle completo: [docs/configuration.md](docs/configuration.md).

---

## 5. Qué pasa al arrancar

En **Development** la API prepara todo sola, no hay migraciones manuales:

1. **MongoDB:** crea las colecciones e índices que falten (`ate_nosql_db`).
2. **PostgreSQL:** aplica el esquema de `database/01_init_schema.sql` (idempotente).
3. **Seed:** crea roles, el SuperAdmin, la empresa de ejemplo *Acme Corp* con sus colaboradores y restaurantes
   de ejemplo. Usan la contraseña `Seed:DefaultPassword`; si no la definiste, se genera una y aparece en el log:
   ```
   Seed:DefaultPassword is not set. New sample accounts will use this generated password: ...
   ```
4. **Catálogo de platos:** sincroniza los platos de PostgreSQL hacia Mongo.

Si un paso falla (por ejemplo, Mongo apagado), se registra el error en el log y la API sigue arrancando.

Usuario administrador de ejemplo: `admin@ate.com` (documento `0943676155`). Ver [database/README.md](database/README.md).

> En **Production** no se aplica el esquema ni el seed automáticamente; ver [sección 11](#11-producción).

---

## 6. URLs útiles y autenticación en desarrollo

| Recurso | URL |
|---|---|
| Documentación interactiva (Scalar) | <http://localhost:5252/scalar/v1> |
| Especificación OpenAPI | <http://localhost:5252/openapi/v1.json> |
| Health check | <http://localhost:5252/api/health> |
| Archivos públicos (logos) | <http://localhost:5252/files/...> |
| PostgreSQL | `localhost:5432` · base `ate_db` · usuario `postgres` |
| MongoDB | `localhost:27017` · base `ate_nosql_db` · usuario `ate_admin` (authSource `admin`) |

Scalar y OpenAPI **solo existen en Development**.

### Token de desarrollo (sin credenciales)

Para probar endpoints protegidos rápido, en Development existe:

```bash
# Token con el rol por defecto
curl http://localhost:5252/api/v1/auth/dev-token

# Token con un rol o usuario concreto
curl "http://localhost:5252/api/v1/auth/dev-token?role=CompanyAdmin"
curl "http://localhost:5252/api/v1/auth/dev-token?email=admin@ate.com"
```

Roles: `SuperAdmin`, `CompanyAdmin`, `Employee`, `Merchant`. Copia el `accessToken` de la respuesta y úsalo en
Scalar (botón **Authorize**) o como cabecera `Authorization: Bearer <token>`.
Este endpoint responde **404** fuera de Development.

También puedes usar `Ate.Api/Ate.Api.http` desde VS Code (REST Client) o Rider.

### Frontend local

CORS en Development acepta `http://localhost:4200` (Angular) y `http://localhost:3000`
(`appsettings.Development.json`). Si el front corre en otro puerto, agrégalo ahí.

---

## 7. Pruebas

```bash
dotnet test Ate.Tests
```

Pruebas xUnit de reglas de negocio y seguridad (nómina, fichas de restaurante, almacenamiento, idempotencia).
No necesitan bases de datos. Qué cubre cada una: [docs/pruebas/README.md](docs/pruebas/README.md).

---

## 8. Estructura del proyecto

```
Ate-back/
├── Ate.Api/              # Capa de presentación: controladores, middlewares, filtros, Program.cs
├── Ate.Application/      # Casos de uso, DTOs, validaciones (FluentValidation), interfaces
├── Ate.Domain/           # Entidades y reglas de negocio puras (sin dependencias)
├── Ate.Infrastructure/   # EF Core + PostgreSQL, MongoDB, JWT, correo, PDF, almacenamiento
├── Ate.Tests/            # Pruebas xUnit
├── database/             # Scripts SQL (esquema, seed, usuario de mínimo privilegio) y Mongo
├── deploy/               # nginx.conf para producción
├── docs/                 # Documentación técnica
├── Dockerfile            # Imagen multi-stage de la API
├── docker-compose.yml        # Desarrollo: postgres + mongo + api
├── docker-compose.prod.yml   # Producción
├── Makefile              # Atajos: back, mongo, run, build
└── Ate.slnx              # Solución
```

Dependencias entre capas: `Api → Application → Domain` e `Infrastructure → Application`.
Más detalle: [docs/ARQUITECTURA.md](docs/ARQUITECTURA.md).

Stack principal: ASP.NET Core 10 · EF Core 10 + Npgsql · MongoDB.Driver 3 · FluentValidation · Serilog ·
Scalar/OpenAPI · QuestPDF · xUnit.

Logs: consola y archivos diarios en `Ate.Api/logs/ate-api-AAAAMMDD.log` (se guardan 14 días).

---

## 9. Comandos de referencia

| Comando | Qué hace |
|---|---|
| `make back` | Levanta Mongo en Docker y la API con hot reload |
| `make run` | Corre la API sin hot reload ni Docker |
| `make build` | Compila la API |
| `make mongo` / `make mongo-down` / `make mongo-logs` | Inicia, detiene o muestra logs de Mongo |
| `docker compose up -d --wait postgres mongo` | Levanta solo las bases |
| `docker compose up -d --build` | Levanta todo (incluida la API) |
| `docker compose down` | Detiene los contenedores (conserva datos) |
| `docker compose down -v` | Detiene y **borra los datos** de las bases |
| `dotnet build Ate.slnx` | Compila toda la solución |
| `dotnet test Ate.Tests` | Ejecuta las pruebas |

---

## 10. Problemas frecuentes

**`Define POSTGRES_PASSWORD en .env` (o `MONGO_PASSWORD`, `JWT_SECRET_KEY`) al hacer `docker compose up`**
Falta el `.env` o tiene valores vacíos. Copia `.env.example` a `.env` y complétalo.

**`Connection string 'DefaultConnection' was not found`**
No configuraste la conexión de PostgreSQL. Ver paso 3 del [inicio rápido](#2-inicio-rápido-5-minutos).

**`password authentication failed for user "postgres"`**
La contraseña de user-secrets no coincide con la del contenedor. Ojo: PostgreSQL toma `POSTGRES_PASSWORD`
**solo la primera vez** que crea el volumen. Si la cambiaste después, usa la antigua o recrea el volumen con
`docker compose down -v` (borra los datos).

**`MongoDB is not configured; skipping collection initialization.`**
Falta `ConnectionStrings:MongoDbConnection`. La API arranca, pero los pedidos y el catálogo no funcionarán.

**`Address already in use` en el puerto 5252, 5432 o 27017**
Otro proceso usa el puerto (por ejemplo, un PostgreSQL instalado localmente o el contenedor `ate_backend_api`).
Búscalo con `lsof -i :5432` y detenlo, o cambia el puerto publicado en `docker-compose.yml`.

**No sé la contraseña de las cuentas de ejemplo**
Si no definiste `Seed:DefaultPassword`, búscala en el log del primer arranque. Si ya pasó, define
`Seed:DefaultPassword` y recrea la base (`docker compose down -v`) para que el seed vuelva a crear las cuentas.

**El frontend recibe errores de CORS**
Agrega su origen en `Cors:AllowedOrigins` de `appsettings.Development.json`.

**Errores de NuGet o git al compilar desde la terminal del IDE**
Usa los comandos del `Makefile`: fijan `NUGET_PACKAGES` y desactivan las consultas de git del SDK.

**La API devuelve un código de error que no entiendo**
Consulta el catálogo: [docs/errores/01-catalogo-codigos.md](docs/errores/01-catalogo-codigos.md). Cada respuesta
trae un `traceId` (cabecera de respuesta) que puedes buscar en los logs.

---

## 11. Producción

Resumen (guía completa en [docs/security/despliegue.md](docs/security/despliegue.md)):

```bash
# Variables en un .env.prod fuera del repo (ver la sección "Producción" de .env.example)
docker compose -f docker-compose.prod.yml --env-file .env.prod up -d --build
```

- La API corre en `Production`: sin Scalar, sin `dev-token` y sin seed.
- PostgreSQL y Mongo no publican puertos; la API escucha solo en `127.0.0.1:5252`, detrás de un proxy con TLS.
- La API se conecta con usuarios de **mínimo privilegio** (`database/03_app_role.sql`,
  `database/mongo/create-app-user.js`).
- La API no arranca si `JWT_SECRET_KEY` es corta o de ejemplo, o si falta `FRONTEND_ORIGIN` (CORS).

---

## 12. Más documentación

| Documento | Contenido |
|---|---|
| [docs/ARQUITECTURA.md](docs/ARQUITECTURA.md) | Arquitectura y capas |
| [docs/configuration.md](docs/configuration.md) | Configuración, secretos y variables de entorno |
| [docs/docker.md](docs/docker.md) | Imagen Docker y compose |
| [docs/mongodb.md](docs/mongodb.md) | MongoDB: colecciones y conexión |
| [database/README.md](database/README.md) | Scripts SQL y datos de ejemplo |
| [docs/almacenamiento/README.md](docs/almacenamiento/README.md) | Almacenamiento de archivos (Local, Azure, S3, SFTP) |
| [docs/errores/README.md](docs/errores/README.md) | Códigos de error |
| [docs/pruebas/README.md](docs/pruebas/README.md) | Pruebas automáticas |
| [docs/security/despliegue.md](docs/security/despliegue.md) | Despliegue seguro |
| [docs/changes/](docs/changes/) | Resumen de cambios |
