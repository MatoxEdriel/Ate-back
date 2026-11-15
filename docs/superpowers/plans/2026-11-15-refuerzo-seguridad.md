# Refuerzo de seguridad — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Aplicar en código y configuración las acciones del plan de refuerzo (OWASP Top 10 / ISO 27001 Anexo A) que dependen del repo.

**Architecture:** Endurecer la API por capas. Primero el arranque y el despliegue (secretos, entorno, CORS). Luego HTTP (cabeceras, HTTPS, proxy), credenciales (hash v2, política, contraseña temporal), sesiones (refresh rotativo en cookie `HttpOnly`, access token corto en memoria, sello de seguridad) y trazabilidad (eventos de seguridad, límite de intentos persistente). El frontend cambia solo su manejo de sesión.

**Tech Stack:** .NET 10, EF Core + PostgreSQL, MongoDB, Angular 22 (signals, interceptores funcionales), GitHub Actions.

**Spec:** `docs/security/2026-11-15-plan-refuerzo-seguridad.md`

## Global Constraints

- **Sin tests automatizados nuevos.** El usuario limitó los tests al cálculo de nómina; cada tarea se verifica a mano contra la API local y con `dotnet build` / `npx ng lint` / `npm test` / `npm run build`.
- Access token: **15 minutos**. Refresh token guardado como **SHA-256**, rotado en cada uso y con vencimiento deslizante según el rol: **30 días** si el usuario solo tiene el rol `Employee`; **7 días** si tiene `CompanyAdmin`, `Merchant` o `SuperAdmin` (gana el más corto). Ventana de gracia ante reuso: **30 s**.
- Cookie del refresh: `ate_rt`, `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/v1/auth`.
- Contraseñas: PBKDF2-SHA256 con **600.000** iteraciones; mínimo **12** caracteres; contraseña temporal válida **72 h**.
- Tamaño máximo de petición: **2 MB**.
- El esquema se cambia en `database/01_init_schema.sql` con `ADD COLUMN IF NOT EXISTS` / `CREATE TABLE IF NOT EXISTS` (es idempotente y se aplica en cada arranque de Development).
- Ningún secreto en archivos versionados; los textos de interfaz en español y el código en inglés.
- Fuera de alcance: rotación real de credenciales (1.1), gestor de secretos (1.5), TOTP (3.6), respaldos, pentest y controles organizacionales.

## Review Focus

1. **Recarga de página con sesión abierta:** el access token vive en memoria. Al recargar, el frontend debe recuperar la sesión con la cookie, sin mandar al login. *(Task 11, verificación manual)*
2. **Dos pestañas refrescando a la vez:** la rotación no debe cerrar la sesión de ambas. La gracia de 30 s acepta el token recién rotado. *(Task 5, verificación con dos `curl` seguidos con la misma cookie)*
3. **API detrás de un proxy sin `X-Forwarded-Proto`:** forzar HTTPS no debe crear un bucle de redirecciones. Solo se redirige si `Security:EnforceHttps=true`. *(Task 2)*
4. **Usuarios con hash antiguo (100.000 iteraciones):** deben poder entrar, y su hash se actualiza en ese login. *(Task 3, verificación con el usuario del seed)*
5. **Cambio de contraseña con otras sesiones abiertas:** el access token anterior queda inválido en ≤ 30 s, por el sello de seguridad y la caché de `UserAccessValidator`. *(Task 4)*

---

### Task 1: Arranque seguro y configuración de despliegue

**Files:**
- Create: `Ate.Infrastructure/Services/JwtOptionsValidator.cs`, `docker-compose.prod.yml`, `deploy/nginx.conf`, `docs/security/despliegue.md`
- Modify: `Ate.Infrastructure/DependencyInjection.cs`, `Ate.Api/Program.cs`, `Ate.Infrastructure/Persistence/AteDbSeeder.cs`, `database/02_seed_data.sql`, `database/README.md`, `Ate.Api/appsettings.json`, `.env.example`

**Interfaces:**
- Produces: `JwtOptionsValidator : IValidateOptions<JwtOptions>` (registered with `ValidateOnStart`); config sections `Cors:AllowedOrigins` (string[]) and `Seed:DefaultPassword` (string, dev only).

- [ ] **Step 1:** `JwtOptionsValidator`. Fuera de Development falla si la clave tiene menos de 32 bytes en UTF-8, o si contiene (sin distinguir mayúsculas) `REEMPLAZAR`, `CHANGE`, `SECRET`, `DEFECTO`, `EXAMPLE` o `CLAVE`. Mensaje: `"JwtSettings:SecretKey must be a random value of at least 32 bytes."`.
- [ ] **Step 2:** CORS desde `Cors:AllowedOrigins`. El `appsettings.json` trae por defecto `["http://localhost:4200","http://localhost:3000"]`. Fuera de Development, si la lista está vacía, la API no arranca.
- [ ] **Step 3:** `Kestrel.Limits.MaxRequestBodySize = 2 MB`. `/api/health` deja de mostrar `version`.
- [ ] **Step 4:** el seeder toma la contraseña de `Seed:DefaultPassword`. Si no existe, genera una con `TemporaryPasswordGenerator` y la escribe **una vez** en el log como `LogWarning`. En `02_seed_data.sql` y en `database/README.md` se borran las contraseñas en texto claro, con una nota de que el script es solo para desarrollo.
- [ ] **Step 5:** `docker-compose.prod.yml`:
  - `ASPNETCORE_ENVIRONMENT=Production`.
  - Postgres y Mongo sin `ports`, y sin montar `02_seed_data.sql`.
  - La API recibe por `${...}` las variables `AllowedHosts`, `Cors__AllowedOrigins__0`, `JwtSettings__SecretKey`, las conexiones y `Security__EnforceHttps=true`.
  - Se agregan esas variables a `.env.example`.
- [ ] **Step 6:** `deploy/nginx.conf` para servir el frontend con estas cabeceras:
  - CSP con `default-src 'self'`; `connect-src` igual a la API; `font-src`/`style-src` para Google Fonts; `img-src 'self' data: blob:` más los tiles del mapa; `frame-src` para Google Maps y OpenStreetMap; `frame-ancestors 'none'`.
  - HSTS, `X-Content-Type-Options`, `Referrer-Policy` y `Permissions-Policy`.
  - `docs/security/despliegue.md` explica cómo usar el compose de producción, el `.env` y nginx.
- [ ] **Step 7: Verificar**
  - `dotnet build` → 0 errores.
  - Production sin clave → no arranca.
  - Production con la clave de ejemplo → no arranca, con el mensaje del Step 1.
  - `docker compose -f docker-compose.prod.yml config -q` → OK.
  - Development con *user-secrets* → login 200.
- [ ] **Step 8:** Commit `feat(security): safe startup, production compose, CORS config and seed without plaintext passwords`.

### Task 2: Cabeceras HTTP, HTTPS y proxy

**Files:** Create `Ate.Api/Middlewares/SecurityHeadersMiddleware.cs`; Modify `Ate.Api/Program.cs`, `Ate.Api/appsettings.json`.

**Interfaces:**
- Produces: config `Security:EnforceHttps` (bool, default false) and `ForwardedHeaders:KnownNetworks` / `ForwardedHeaders:KnownProxies` (string[]).

- [ ] **Step 1:** el middleware agrega `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` y `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` (son respuestas JSON). En las rutas `/scalar` no pone CSP, para no romper la documentación en desarrollo.
- [ ] **Step 2:** `UseForwardedHeaders` (`XForwardedFor | XForwardedProto`), solo si hay redes o proxies configurados, y antes de todo lo demás.
- [ ] **Step 3:** fuera de Development, `UseHsts()`. `UseHttpsRedirection()` solo si `Security:EnforceHttps` es true.
- [ ] **Step 4: Verificar**
  - `curl -I /api/health` muestra las 4 cabeceras.
  - Con `KnownNetworks=127.0.0.1/32` y `X-Forwarded-For: 203.0.113.9`, el log de la petición registra la IP 203.0.113.9.
- [ ] **Step 5:** Commit `feat(security): security headers, HSTS/HTTPS and forwarded headers`.

### Task 3: Hash v2, política de contraseñas y contraseña temporal con vencimiento

**Files:**
- Create: `Ate.Application/Common/Security/PasswordPolicy.cs`, `Ate.Application/Common/Security/CommonPasswords.cs`
- Modify: `Ate.Infrastructure/Services/PasswordHasher.cs`, `Ate.Application/Common/Interfaces/IPasswordHasher.cs`, `ChangePasswordRequestValidator.cs`, `PasswordRecoveryValidators.cs`, `AuthService.cs`, `EmployeeService.cs`, `CompanyService.cs`, `AdminRestaurantService.cs`, `Ate.Domain/Entities/User.cs`, `UserConfiguration.cs`, `database/01_init_schema.sql`, `Ate.Domain/Errors/AuthErrors.cs`

**Interfaces:**
- Produces:
  - `IPasswordHasher.NeedsRehash(string hash): bool`
  - `PasswordPolicy.MinLength = 12`, `PasswordPolicy.MaxLength = 128`, `PasswordPolicy.IsCommon(string password): bool`
  - `User.TemporaryPasswordExpiresAt: DateTimeOffset?` (columna `temporary_password_expires_at`)
  - `AuthErrors.TemporaryPasswordExpired`

- [ ] **Step 1:** formato nuevo del hash: `v2$600000$<base64(salt+hash)>`. `VerifyPassword` acepta también el formato antiguo, de 48 bytes y 100.000 iteraciones. `NeedsRehash` es true si el hash no empieza con `v2$`.
- [ ] **Step 2:** en `LoginAsync`, tras validar la contraseña, si `NeedsRehash` → se guarda el hash nuevo.
- [ ] **Step 3:** `CommonPasswords` es un `HashSet` (sin distinguir mayúsculas) con ~100 contraseñas comunes en español e inglés, del estilo de `123456789012`, `contraseña123` o `password1234`. Los validadores de cambio y recuperación usan `MinLength` y rechazan las comunes con el mensaje `"Esa contraseña es muy común. Elige otra."`.
- [ ] **Step 4:** al crear una cuenta con contraseña temporal (los 3 servicios) se fija `TemporaryPasswordExpiresAt = now + 72 h`. Cambiar o recuperar la contraseña lo pone en null. En el login, si `MustChangePassword` y venció → `AuthErrors.TemporaryPasswordExpired` (401) con el texto `"Tu contraseña temporal venció. Usa «Olvidé mi contraseña» para crear una nueva."`.
- [ ] **Step 5: Verificar**
  - Login de `0943676156` → 200, y en la base su `password_hash` empieza con `v2$600000$`.
  - Cambiar a `contraseña123` → 400 "muy común".
  - Cambiar a 11 caracteres → 400.
  - Un usuario con la temporal vencida (se fija la fecha en la base) → 401 con el texto de arriba.
- [ ] **Step 6:** Commit `feat(security): PBKDF2 600k with transparent rehash, stronger password policy, expiring temporary passwords`.

### Task 4: Sello de seguridad y cédula fuera del JWT

**Files:** Modify `User.cs`, `UserConfiguration.cs`, `01_init_schema.sql`, `JwtTokenGenerator.cs`, `UserAccessValidator.cs`, `AuthService.cs`, `PasswordRecoveryService.cs`, `AuthController.cs`, `IAuthService.cs`, `ClaimsPrincipalExtensions.cs`, `Ate.Domain/Constants/CustomClaimTypes.cs`

**Interfaces:**
- Produces:
  - `User.SecurityStamp: string` (columna `security_stamp`, default `gen_random_uuid()::text`)
  - claim `CustomClaimTypes.SecurityStamp = "sstamp"`
  - `IAuthService.GetCurrentUserAsync(Guid userId): Task<Result<CurrentUserDto>>`
  - `IUserAccessValidator.ValidateAsync(Guid userId, string? securityStamp, CancellationToken)`: compara el sello solo cuando no es null.

- [ ] **Step 1:** el token lleva `sstamp`. `UserAccessValidator` devuelve `AuthErrors.InvalidToken` si el sello no coincide. La caché de 30 s pasa a usar como clave `userId:stamp`.
- [ ] **Step 2:** al cambiar o recuperar la contraseña se genera un sello nuevo.
- [ ] **Step 3:** se quita la cédula del token; `/auth/me` la lee de la base con `GetCurrentUserAsync`.
- [ ] **Step 4: Verificar**
  - Sesión A y sesión B; B cambia la contraseña; A recibe 401 en ≤ 30 s en `/profile`.
  - El token decodificado ya no contiene la cédula.
  - `/auth/me` → 200 con `documentNumber`.
- [ ] **Step 5:** Commit `feat(security): security stamp invalidates old sessions; remove ID number from JWT`.

### Task 5: Sesiones con refresh rotativo en cookie (backend)

**Files:**
- Create: `Ate.Application/Features/Auth/DTOs/AuthSession.cs`, `Ate.Api/Extensions/RefreshCookie.cs`
- Modify: `RefreshToken.cs`, `RefreshTokenConfiguration.cs`, `01_init_schema.sql`, `LoginResponse.cs`, `IAuthService.cs`, `AuthService.cs`, `AuthController.cs`, `JwtTokenGenerator.cs`, `appsettings.json` (`ExpirationInMinutes: 15`), `PasswordRecoveryService.cs`

**Interfaces:**
- Produces:
  - `record AuthSession(LoginResponse Response, string RefreshToken, DateTimeOffset RefreshExpiresAt)`
  - `LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, UserDto User)`, sin `RefreshToken`
  - `IAuthService.LoginAsync`, `ChangePasswordAsync` y `GenerateDevTokenAsync` → `Task<Result<AuthSession>>`
  - `IAuthService.RefreshAsync(string refreshToken, CancellationToken): Task<Result<AuthSession>>`
  - `IAuthService.LogoutAsync(string? refreshToken, CancellationToken): Task`
  - `RefreshToken`: `Token` guarda el SHA-256 en hexadecimal, y se agregan `RevokedAt: DateTimeOffset?` y `RevocationReason: string?` (`rotated` | `logout` | `password` | `reuse`)
  - Endpoints `POST /api/v1/auth/refresh` y `POST /api/v1/auth/logout`, ambos `[AllowAnonymous]` y `[AllowWhilePasswordChangeRequired]`, con la política de límite `Login`

- [ ] **Step 1:** esquema. `ALTER TABLE refresh_tokens ADD COLUMN IF NOT EXISTS revoked_at`, `revocation_reason`, y `token` pasa a `VARCHAR(64)`. Una sola vez se revocan los tokens existentes en texto plano: `UPDATE refresh_tokens SET is_revoked = TRUE WHERE length(token) <> 64`.
- [ ] **Step 2:** `RefreshAsync`.
  - Busca el token por su hash.
  - Si está vencido o no existe → `InvalidToken`.
  - Si está revocado con `rotated` y `RevokedAt` hace ≤ 30 s → se acepta.
  - Si está revocado por cualquier otro motivo, o fuera de la gracia → revoca **todos** los tokens del usuario con `reuse`, y devuelve `InvalidToken`.
  - Si es válido: pasa `UserAccessValidator`, revoca el token con `rotated`, emite un refresh nuevo y un access nuevo, con los roles leídos de la base. El vencimiento del refresh lo da `RefreshLifetime.For(IReadOnlyCollection<string> roles)`: 30 días solo-`Employee`, 7 días en cualquier otro caso. Lo usan también el login y el cambio de contraseña.
- [ ] **Step 3:** `RefreshCookie.Set(HttpResponse, AuthSession)`, `RefreshCookie.Read(HttpRequest)` y `RefreshCookie.Clear(HttpResponse)`, con los atributos de Global Constraints. `Secure` va siempre, porque los navegadores lo aceptan en `localhost`. Login, cambio de contraseña, dev-token y refresh ponen la cookie; logout la borra y revoca el token con `logout`.
- [ ] **Step 4:** cambiar o recuperar la contraseña revoca los refresh con `password`; el cambio entrega una sesión nueva.
- [ ] **Step 5: Verificar** con `curl -c/-b`:
  - El login pone una cookie `ate_rt` con `HttpOnly` y `Path=/api/v1/auth`, y el JSON ya no trae `refreshToken`.
  - `refresh` → 200 y una cookie nueva.
  - Reusar la cookie vieja en menos de 30 s → 200 (gracia).
  - Reusar la cookie vieja después de 30 s → 401, y además la cookie nueva queda inválida (detección de reuso).
  - `logout` → 204 y luego `refresh` → 401.
  - En la base, `token` tiene 64 caracteres hexadecimales.
- [ ] **Step 6:** Commit `feat(security): rotating hashed refresh tokens in HttpOnly cookie with reuse detection; refresh and logout endpoints`.

### Task 6: Límite de intentos persistente y progresivo

**Files:** Modify `ILoginAttemptLimiter.cs`, `LoginAttemptLimiter.cs`, `AuthService.cs`, `01_init_schema.sql`, `AteDbContext.cs`; Create `Ate.Domain/Entities/LoginAttemptWindow.cs` and its configuration.

**Interfaces:**
- Produces:
  - `ILoginAttemptLimiter`: `Task<TimeSpan?> GetRetryAfterAsync(string documentNumber, string? clientIp, CancellationToken)`, `Task RegisterFailureAsync(...)`, `Task ResetAsync(...)`; pasa a ser `IScopedDependency`
  - Tabla `login_attempt_windows(key CHAR(64) PK, failures INT, window_start TIMESTAMPTZ, blocked_until TIMESTAMPTZ NULL)`

- [ ] **Step 1:** las claves siguen siendo un SHA-256 (nunca la cédula). El par cédula+IP mantiene el límite actual (`MaxFailuresPerDocumentAndIp` en `WindowMinutes`). Por cédula sola, al superar `MaxFailuresPerDocument` se bloquea `min(30 s × 2^(fallos − límite), 15 min)` en vez del corte por toda la ventana.
- [ ] **Step 2: Verificar**
  - 6 logins fallidos con la misma cédula e IP → 429 con `Retry-After`.
  - Al reiniciar la API sigue en 429.
  - El login correcto, después de la espera, borra el contador.
- [ ] **Step 3:** Commit `feat(security): persistent progressive login throttling`.

### Task 7: Registro de eventos de seguridad

**Files:** Create `Ate.Domain/Entities/SecurityEvent.cs`, its configuration, `Ate.Application/Common/Interfaces/ISecurityEventLog.cs`, `Ate.Infrastructure/Services/SecurityEventLog.cs`, `Ate.Domain/Constants/SecurityEventTypes.cs`; Modify `01_init_schema.sql`, `AuthService.cs`, `PasswordRecoveryService.cs`, `CompanyService.cs`, `AdminRestaurantService.cs`, `EmployeeService.cs`, `PayrollSettlementService.cs`, `AuthController.cs`.

**Interfaces:**
- Produces:
  - `ISecurityEventLog.RecordAsync(string type, Guid? userId, string outcome, string? ip = null, string? detail = null, CancellationToken ct = default)`
  - Tabla `security_events(id UUID, occurred_at, event_type VARCHAR(50), user_id UUID NULL, outcome VARCHAR(20), ip VARCHAR(45) NULL, detail VARCHAR(500) NULL)` con índice `(event_type, occurred_at)`
  - Tipos: `login`, `login_blocked`, `logout`, `password_changed`, `password_reset_requested`, `password_reset_completed`, `refresh_reuse`, `company_status`, `restaurant_status`, `employee_status`, `payroll_pdf`
  - `outcome`: `success` | `failure`

- [ ] **Step 1:** `RecordAsync` también escribe un log con la categoría `Security`. **Nunca** guarda contraseñas, tokens, cédulas ni correos. Para un login fallido sin usuario conocido, `user_id` queda null y no se guarda la cédula.
- [ ] **Step 2:** registrar cada evento en el punto donde ocurre. La IP la pasa el controller, como hoy hace el login.
- [ ] **Step 3: Verificar** con un login correcto, uno fallido, un logout, un cambio de estado de colaborador y un PDF de nómina → 5 filas con el tipo y el resultado correctos. Ninguna fila contiene la cédula.
- [ ] **Step 4:** Commit `feat(security): security event audit trail`.

### Task 8: Correos sin HTML inyectado y nombres válidos

**Files:** Modify `Ate.Infrastructure/Templates/Emails/*.html`; Create `Ate.Application/Common/Validation/PersonNameRules.cs`; Modify the validators of employee, company admin and merchant.

**Interfaces:** Produces `PersonNameRules.IsValid(string? name): bool`, que acepta letras Unicode, espacios, `'`, `-` y `.`, entre 2 y 100 caracteres.

- [ ] **Step 1:** todo `{{ campo }}` que venga del usuario lleva `| html.escape`: nombres, correos, restaurante, empresa y comida. Excepciones: `content`, que es el HTML interno ya renderizado, y los valores generados por el sistema (código, URLs, minutos).
- [ ] **Step 2:** los validadores de nombres y apellidos de persona usan `PersonNameRules` con el mensaje `"Usa solo letras, espacios, apóstrofo o guion."`.
- [ ] **Step 3: Verificar**
  - Crear un colaborador con el nombre `<a href=x>Ana</a>` → 400.
  - En `EmailService`, renderizar `OrderConfirmation` con el restaurante `<b>X</b>` → el HTML trae `&lt;b&gt;`.
- [ ] **Step 4:** Commit `fix(security): escape user data in email templates and restrict person names`.

### Task 9: Mínimo privilegio en bases de datos

**Files:** Create `database/03_app_role.sql`, `database/mongo/create-app-user.js`; Modify `docs/security/despliegue.md`.

- [ ] **Step 1:** rol `ate_app` (`LOGIN`, contraseña por variable `psql -v app_password=...`): `CONNECT` sobre la base, `USAGE` sobre `public`, `SELECT/INSERT/UPDATE/DELETE` en todas las tablas y `USAGE, SELECT` en las secuencias, más `ALTER DEFAULT PRIVILEGES` para las tablas futuras. Sin DDL.
- [ ] **Step 2:** Mongo, usuario `ate_app` con `readWrite` solo sobre `ate_nosql_db`.
- [ ] **Step 3:** `despliegue.md`: el esquema se aplica con el dueño y la API se conecta con `ate_app`.
- [ ] **Step 4: Verificar** `03_app_role.sql` contra la base local, con `psql -v`: crea el rol, `ate_app` puede hacer `SELECT` en `users` y `CREATE TABLE x()` falla.
- [ ] **Step 5:** Commit `feat(security): least-privilege database roles`.

### Task 10: Dependencias y análisis automático

**Files:** Modify `Ate.Api/Ate.Api.csproj`, `frontend/Ate/package.json` + lock; Create `.github/dependabot.yml` and `.github/workflows/security.yml` in **both** repos.

- [ ] **Step 1:** subir `Microsoft.AspNetCore.OpenApi` a `10.0.15` y fijar `Microsoft.OpenApi` a `2.13.0` como referencia directa. `dotnet list package --vulnerable --include-transitive` → sin hallazgos.
- [ ] **Step 2:** frontend, `npm audit fix` → `npm audit --omit=dev` sin altas. `npm test` y `npm run build` siguen pasando.
- [ ] **Step 3:** `dependabot.yml` semanal (nuget o npm, más github-actions). `security.yml` en cada push y PR: `gitleaks/gitleaks-action@v2`, CodeQL (`csharp` o `javascript-typescript`) y el chequeo de dependencias vulnerables (`dotnet list package --vulnerable` o `npm audit --audit-level=high`).
- [ ] **Step 4:** Commit `chore(security): patch vulnerable dependencies; add Dependabot, gitleaks and CodeQL`.

### Task 11: Sesión en el frontend (token en memoria + refresh)

Repo `frontend/Ate`.

**Files:** Modify `core/services/auth.service.ts`, `core/interceptors/auth.interceptor.ts`, `core/interceptors/security.interceptor.ts`, `core/constants/auth.constants.ts`, `core/models/auth.model.ts`, `app.config.ts`.

**Interfaces:**
- Consumes: Task 5 endpoints. `LoginResponse` ya no trae `refreshToken`.
- Produces: `AuthService.restoreSession(): Promise<void>` (initializer), `AuthService.refresh(): Observable<string>` (comparte la petición en curso), `AuthService.logout(): void` (llama a `POST /auth/logout`).

- [ ] **Step 1:** el access token pasa a un `signal` privado, sin `localStorage`. Se borran las claves `ACCESS_TOKEN` y `REFRESH_TOKEN` y, al iniciar, los valores viejos que queden en `localStorage`. El perfil (`USER`) sigue en `localStorage` porque no es credencial.
- [ ] **Step 2:** `login`, `changePassword`, `refresh` y `logout` usan `withCredentials: true`.
- [ ] **Step 3:** `provideAppInitializer(() => inject(AuthService).restoreSession())`. Si hay perfil guardado intenta `refresh`; si falla, borra el perfil.
- [ ] **Step 4:** ante un 401 de la API (excepto en `/auth/*`), `security.interceptor` intenta **un** `refresh` y repite la petición. Si el refresh falla → logout con el aviso actual.
- [ ] **Step 5: Verificar**
  - `npx ng lint`, `npm test` y `npm run build` pasan.
  - Con la API local: tras el login, `localStorage` no tiene tokens; al recargar, la sesión sigue; a los 15 min de inactividad la siguiente petición se renueva sola.
  - Cerrar sesión borra la cookie.
- [ ] **Step 6:** Commit `feat(security): in-memory access token with cookie-based refresh`.
