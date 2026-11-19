# Idempotencia (Idempotency-Key) — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que crear un pedido y calificar sean idempotentes con la cabecera `Idempotency-Key`, mediante un atributo reutilizable en el backend y un mecanismo reutilizable en el frontend.

**Architecture:** Un filtro de acción `[Idempotent]` reserva la clave `{userId}:{key}` en una colección Mongo con índice único y TTL de 24 h, ejecuta la acción y guarda el código y el cuerpo; los reintentos reciben la respuesta guardada. La decisión vive en una función pura con tests. En el frontend, `IdempotencyKeys` mantiene una clave por intención y un interceptor la manda como cabecera.

**Tech Stack:** ASP.NET Core 10 (filtros de acción), MongoDB driver, xUnit; Angular 22 (interceptores funcionales, `HttpContextToken`), Vitest.

**Spec:** `docs/superpowers/specs/2026-11-18-idempotencia-design.md`

## Global Constraints

- Cabecera `Idempotency-Key`; clave por usuario `{userId}:{key}`; vence a las **24 h** (TTL de Mongo).
- Sin cabecera → comportamiento actual. Clave vacía o de más de **100** caracteres → 400 `Idempotency.InvalidKey`.
- Primera ya terminada (2xx/4xx) → misma respuesta + cabecera `Idempotent-Replayed: true`. En curso → 409 `Idempotency.InProgress`. Contenido distinto → 422 `Idempotency.KeyReused`. 5xx o excepción → se borra la reserva.
- Endpoints protegidos en v1: `POST /api/v1/employee/orders` y `POST /api/v1/employee/orders/{orderId}/rating`.
- Mensajes al usuario en español; código en inglés. Sin commits sin pedido explícito del usuario.

## Review Focus

1. **Doble clic al pedir:** dos POST simultáneos con la misma clave → uno crea el pedido, el otro recibe 409 en curso o la misma respuesta; nunca dos pedidos ni un error de «ya tienes un pedido». *(Task 2, prueba en vivo)*
2. **Reintento tras red caída:** la primera petición sí llegó y creó el pedido; el reintento con la misma clave recibe el mismo pedido (200), no 409 de slot. *(Task 2 en vivo; Task 3 mantiene la clave ante error de red)*
3. **Cambiar de plato y volver a pedir:** otra intención → otra clave; no debe recibir la respuesta del pedido anterior. *(Task 3, spec de `IdempotencyKeys`)*
4. **Error de servidor en la primera:** el reintento se ejecuta de verdad, no repite el 500. *(Task 1, test `Execute_WhenPreviousAttemptFailed` cubierto porque la reserva se borra; verificación en Task 2)*
5. **Otro usuario con la misma clave:** no ve la respuesta de nadie (la clave incluye el userId). *(Task 2, en vivo)*

---

### Task 1: Decisión pura y almacén Mongo

Backend.

**Files:**
- Create: `Ate.Application/Common/Idempotency/IdempotencyDecision.cs`, `Ate.Application/Common/Interfaces/IIdempotencyStore.cs`, `Ate.Domain/Entities/IdempotencyRecord.cs`, `Ate.Infrastructure/Services/IdempotencyStore.cs`, `Ate.Tests/Idempotency/IdempotencyDecisionTests.cs`
- Modify: `Ate.Application/Common/Constants/MongoCollections.cs` (`IdempotencyKeys = "idempotency_keys"`), `Ate.Infrastructure/Persistence/MongoDb/MongoDbInitializer.cs` (TTL en `ExpiresAt`, `ExpireAfter = TimeSpan.Zero`)

**Interfaces:**
- Produces:
  - `IdempotencyRecord { Id: string ("{userId}:{key}"), RequestHash: string, Completed: bool, StatusCode: int?, Body: string?, CreatedAt, ExpiresAt }`
  - `enum IdempotencyOutcome { Execute, Replay, InProgress, KeyReused }`
  - `static IdempotencyOutcome IdempotencyDecision.Decide(IdempotencyRecord? existing, string requestHash)`
  - `IIdempotencyStore`: `Task<bool> TryReserveAsync(string id, string requestHash, DateTimeOffset now, CancellationToken)` (false si ya existe, por la clave duplicada de Mongo), `Task<IdempotencyRecord?> GetAsync(string id, CancellationToken)`, `Task CompleteAsync(string id, int statusCode, string body, CancellationToken)`, `Task ReleaseAsync(string id, CancellationToken)`.

- [ ] **Step 1: Tests que fallan** — `IdempotencyDecisionTests`:
  - `Execute_WhenNoRecord`: `Decide(null, "h")` → `Execute`.
  - `Replay_WhenCompletedWithSameHash`: completado, hash `"h"` → `Replay`.
  - `InProgress_WhenNotCompletedWithSameHash` → `InProgress`.
  - `KeyReused_WhenHashDiffers`: completado o no, hash `"otro"` → `KeyReused`.
- [ ] **Step 2:** `dotnet test --filter IdempotencyDecisionTests` → FAIL (no compila).
- [ ] **Step 3:** implementar `Decide`, la entidad, la interfaz y el almacén Mongo (`InsertOne` con captura de `MongoWriteException` con `ServerErrorCategory.DuplicateKey` → false).
- [ ] **Step 4:** `dotnet test` → todo PASS; `dotnet build` → 0 errores.

### Task 2: Atributo `[Idempotent]` en los endpoints

Backend.

**Files:**
- Create: `Ate.Api/Filters/IdempotentAttribute.cs`, `Ate.Domain/Errors/IdempotencyErrors.cs`
- Modify: `Ate.Api/Controllers/EmployeeOrdersController.cs` (`[Idempotent]` en `Create` y `RateOrder`)

**Interfaces:**
- Consumes: Task 1.
- Produces: `[Idempotent]` (`TypeFilterAttribute` → `IdempotencyFilter : IAsyncActionFilter`); constantes `IdempotentAttribute.HeaderName = "Idempotency-Key"`, `ReplayedHeader = "Idempotent-Replayed"`.
- Errores: `InvalidKey` (Validation) «La clave de la operación no es válida.», `InProgress` (Conflict) «Tu solicitud anterior todavía se está procesando. Espera un momento.», `KeyReused` (422) «Esta clave ya se usó para otra operación.».

- [ ] **Step 1:** filtro:
  - Sin cabecera → `next()`.
  - Hash = SHA-256 hex de `método + " " + ruta + "\n" + JSON(ActionArguments sin CancellationToken)`.
  - `TryReserveAsync`; si falla, `GetAsync` + `Decide`. `Replay` → `ContentResult` con el código y el cuerpo guardados, `application/json` y `Idempotent-Replayed: true`.
  - Tras `next()`: `ObjectResult` con código < 500 → `CompleteAsync` con el cuerpo serializado con las `JsonOptions` de MVC. Excepción o ≥ 500 → `ReleaseAsync`.
- [ ] **Step 2: Verificar** `dotnet build` → 0 errores; `dotnet test` → PASS.
- [ ] **Step 3: Verificar en vivo** (API local, como en las fichas):
  - Dos POST de pedido simultáneos con la misma clave → un 200 y el otro 409 `Idempotency.InProgress` o 200 con `Idempotent-Replayed: true`; un solo pedido en Mongo.
  - El mismo POST repetido después → 200 con `Idempotent-Replayed: true` y el mismo `id`.
  - Misma clave con otro plato → 422.
  - Otro usuario con la misma clave → se ejecuta de forma independiente.
  - Calificar dos veces con la misma clave → 200 y 200 repetido (no 409 `AlreadyRated`).
  - Limpiar los datos de prueba.

### Task 3: Frontend reutilizable

Frontend.

**Files:**
- Create: `src/app/core/services/idempotency-keys.ts`, `src/app/core/services/idempotency-keys.spec.ts`, `src/app/core/interceptors/idempotency.interceptor.ts`
- Modify: `src/app/app.config.ts` (agregar `idempotencyInterceptor`), `src/app/features/dashboard/data-access/dashboard.api.ts`, `dashboard.store.ts`, `src/app/features/ratings/data-access/ratings.api.ts`, `ratings.store.ts`

**Interfaces:**
- Produces:
  - `IdempotencyKeys` (`providedIn: 'root'`): `keyFor(intent: string): string`, `release(intent: string): void`.
  - `IDEMPOTENCY_KEY = new HttpContextToken<string | null>(() => null)`; `withIdempotencyKey(key: string, context = new HttpContext()): HttpContext`.
  - `idempotencyInterceptor`: agrega `Idempotency-Key` si el contexto trae la clave.
  - `DashboardApi.createOrder(selection, idempotencyKey: string)`; `RatingsApi.rate(orderId, answers, skipped, idempotencyKey: string)`.

- [ ] **Step 1: Tests que fallan** — `idempotency-keys.spec.ts`:
  - la misma intención devuelve la misma clave dos veces;
  - otra intención devuelve otra clave;
  - después de `release(intent)`, `keyFor(intent)` devuelve una clave nueva.
  Y en `ratings.store.spec.ts`: la petición de `submit` lleva la cabecera `Idempotency-Key`, y un reintento tras un error de red (status 0) usa la misma clave.
- [ ] **Step 2:** `npx ng test --include` de ambos specs → FAIL.
- [ ] **Step 3:** implementar. Intenciones:
  - pedido: `order:` + JSON de `restaurantId`, `mealType`, `dishId`, `withRice`, `drink`, `deliveryMode`;
  - calificación: `rating:` + `orderId`.
  Se libera la clave ante éxito o error HTTP 4xx (respuesta definitiva); se mantiene ante status 0 o 5xx.
- [ ] **Step 4:** `npx ng lint`, `npm test` y `npm run build` pasan.
