# Plan de refuerzo de seguridad

Fecha: 2026-11-15 · Alcance: `Ate-back`, `Ate` (frontend) y `ate_land_page`
Base: auditoría de código y configuración del 2026-11-14 (no fue una prueba de intrusión).
Referencias: OWASP Top 10 (2021) y controles técnicos del Anexo A de ISO/IEC 27001:2022.

## Ya hecho (2026-11-15)

- **Secretos fuera de los archivos versionados.**
  - `appsettings.json` ya no trae contraseñas ni la clave JWT de ejemplo ("REEMPLAZAR…").
  - `docker-compose.yml` (back y raíz) lee los valores de un `.env` local que git ignora. Hay un `.env.example` de guía.
  - `docs/docker.md` ya no muestra contraseñas.
- **La API ya no arranca con secretos de ejemplo.** Sin configuración se detiene al iniciar. En local los valores salen de los *user-secrets* de .NET y en Docker del `.env`.

## Fase 1 · Urgente, antes de cualquier despliegue

| # | Acción | Riesgo que cierra | OWASP / ISO |
|---|---|---|---|
| 1.1 | **Rotar** las contraseñas de PostgreSQL y Mongo, la clave JWT y la contraseña de aplicación de Gmail. Siguen en el historial de git y en copias locales. | Credenciales filtradas | A07 · A.5.17 |
| 1.2 | Crear un `docker-compose.prod.yml` con `ASPNETCORE_ENVIRONMENT=Production`. Sin puertos publicados para PostgreSQL y Mongo (o solo `127.0.0.1`). Sin montar `02_seed_data.sql`. | `dev-token` sin credenciales, cuentas de ejemplo, bases expuestas | A05 · A.8.31 |
| 1.3 | En la API, al arrancar, **negarse a iniciar** si el entorno no es Development y la clave JWT tiene menos de 32 bytes aleatorios o coincide con un valor conocido. | Tokens falsificables | A02 · A.8.24 |
| 1.4 | Quitar del repo las contraseñas de las cuentas de ejemplo (`database/02_seed_data.sql`, `database/README.md`). Que el seed genere contraseñas aleatorias y las muestre una sola vez en consola. | Acceso con cuentas conocidas | A07 · A.5.17 |
| 1.5 | Guardar los secretos de producción en un gestor (Azure Key Vault, AWS Secrets Manager o los *secrets* de la plataforma de despliegue), nunca en archivos. | Fuga por archivos | A05 · A.8.24 |

## Fase 2 · Alta, primeras 2 semanas

| # | Acción | Riesgo que cierra | OWASP / ISO |
|---|---|---|---|
| 2.1 | `UseHttpsRedirection` + `UseHsts`. Cabeceras: `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy` y `Permissions-Policy`. | Tráfico en claro, clickjacking | A05 · A.8.20 |
| 2.2 | Content-Security-Policy para el frontend, en el servidor web que lo sirve (orígenes permitidos: la API, Google Fonts, los mapas). | Impacto de un XSS | A03 · A.8.26 |
| 2.3 | CORS por configuración (`Cors:AllowedOrigins`) en vez de `localhost` fijo en el código. | Orígenes indebidos | A05 |
| 2.4 | **Sesiones:** token de acceso corto (15 min); endpoints `refresh` y `logout`; refresh token rotativo, guardado con hash y en cookie `HttpOnly` + `Secure` + `SameSite=Strict`, no en `localStorage`. | Robo de sesión por XSS, sesiones imposibles de cerrar | A07 · A.8.5 |
| 2.5 | Invalidar los tokens vigentes al cambiar o recuperar la contraseña: guardar un `SecurityStamp` en el usuario, ponerlo en el JWT y compararlo en `UserAccessValidator`. | Sesión robada que sobrevive al cambio de clave | A07 · A.8.5 |
| 2.6 | Actualizar dependencias: `Microsoft.OpenApi`, `@angular/router` ≥ 22.2.1 y `zod` ≥ 4.6.6 (`npm audit fix`). Activar Dependabot en los 3 repos. | Vulnerabilidades conocidas | A06 · A.8.8 |
| 2.7 | Escapar con `html.escape` todos los datos del usuario en las plantillas de correo (`OrderConfirmation`, `PasswordReset`, `WelcomeUser`, `WelcomeMerchant`). Restringir los nombres a letras, espacios y signos comunes. | HTML inyectado en correos oficiales | A03 |

## Fase 3 · Media, primer mes

| # | Acción | Riesgo que cierra | OWASP / ISO |
|---|---|---|---|
| 3.1 | Registro de eventos de seguridad (login correcto y fallido, bloqueo, cambio y recuperación de contraseña, desactivaciones, descargas de nómina). Que incluya quién, cuándo, IP y resultado, sin contraseñas ni tokens. Enviarlo a un almacén central con retención mínima de 1 año. | Sin trazabilidad ante incidentes | A09 · A.8.15, A.8.16 |
| 3.2 | `ForwardedHeaders` con los proxies conocidos, para que el límite de intentos use la IP real. | El límite falla detrás de un proxy | A07 |
| 3.3 | Mover los contadores de intentos de login a un almacén compartido (Redis o la base). Bloqueo progresivo por cédula, en vez de un corte duro. | Contadores que se pierden al reiniciar; bloqueo intencional de cuentas | A07, A04 |
| 3.4 | PBKDF2 a 600.000 iteraciones (o Argon2id), y re-hash transparente en el siguiente login. | Contraseñas más fáciles de romper si se filtra la base | A02 · A.8.24 |
| 3.5 | Política de contraseñas: mínimo 12 caracteres y bloqueo de contraseñas comunes o filtradas (lista local o k-anonimato de HIBP). Contraseña temporal válida por 72 h. | Credenciales débiles | A07 · A.5.17 |
| 3.6 | Segundo factor (TOTP) obligatorio para SuperAdmin y RRHH. | Toma de cuentas con privilegios | A07 · A.8.5 |
| 3.7 | Usuarios de base de datos con mínimo privilegio: uno para la app, sin DDL ni superusuario; el esquema se aplica con otro usuario. Lo mismo en Mongo. | Daño amplio si se compromete la app | A01 · A.8.2 |

## Fase 4 · Baja y mejora continua

- Quitar la cédula del JWT; el backend la obtiene por el id de usuario. *(A02, privacidad)*
- `AllowedHosts` con los dominios reales y límite global del tamaño de las peticiones. *(A05)*
- No mostrar la versión en `/api/health`. *(A05)*
- Análisis estático y de secretos en cada push: CodeQL o Semgrep, más gitleaks. *(A.8.25, A.8.29)*
- Respaldos cifrados de PostgreSQL y Mongo, con prueba de restauración trimestral. *(A.8.13)*
- Prueba de intrusión antes del lanzamiento y luego una vez al año. *(A.8.8, A.8.29)*

## Controles organizacionales de ISO 27001 (fuera del código)

Estos no se resuelven programando, pero la certificación los exige:

- Inventario de activos y de datos personales: cédulas, consumos, nómina (A.5.9, A.5.34).
- Política de control de acceso y revisión periódica de cuentas con privilegios (A.5.15, A.5.18).
- Procedimiento de gestión de incidentes (A.5.24–A.5.27).
- Gestión de proveedores: hosting, Gmail/SMTP, proveedor de mapas (A.5.19–A.5.22).
- Cumplimiento de la Ley Orgánica de Protección de Datos Personales de Ecuador (A.5.34).

## Lo que ya cumple

- Cada endpoint exige su rol, y cada rol solo accede a sus datos.
- Las cuentas desactivadas pierden el acceso en segundos.
- La recuperación de contraseña es sólida: códigos con hash, aleatoriedad criptográfica, límite de intentos, y no revela si la cuenta existe.
- No hay SQL armado a mano ni filtros de Mongo con texto del usuario.
- Los archivos subidos solo aceptan PNG, WebP o JPG.
- Los errores no muestran detalles en producción.
- El contenedor de la API corre sin root.
- Los endpoints de desarrollo responden 404 fuera de Development.
