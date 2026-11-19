# Fichas de restaurante — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que el colaborador califique con 👍/👎 cada pedido entregado y que, con esas calificaciones, el restaurante gane o pierda fichas visibles al momento de pedir.

**Architecture:** La calificación se guarda dentro del pedido (`EmployeeOrder.Rating`, MongoDB). Al calificar, `RestaurantBadgeService` recalcula en el momento las fichas de **ese** restaurante con una función pura (`RestaurantBadgeCalculator`) y guarda el estado en PostgreSQL (`restaurant_badges`). El catálogo (`restaurant_badge_definitions`) es data, como el de trofeos. El frontend agrega un modal raíz de calificación (mismo patrón que `AchievementNotifier`), muestra las fichas en la tarjeta y en el panel del menú, y una tarjeta «Tus fichas» en el panel del comercio.

**Tech Stack:** .NET 10, EF Core + PostgreSQL, MongoDB driver, xUnit (`Ate.Tests`); Angular 22 (signals, standalone, OnPush), Vitest.

**Spec:** `docs/superpowers/specs/2026-11-18-fichas-restaurante-design.md`

## Global Constraints

- Calificación **solo 👍/👎 por categoría. Sin comentarios de texto** (confirmado por el usuario).
- Categorías (código → ficha): `punctual` → Puntual, `accurate` → Pedido exacto, `tasty` → Buen sabor, `packaging` → Bien empacado (`packaging` solo si `DeliveryMode` es `retiro` o `entrega`).
- Ganar: `earn_min_positive = 10` y `earn_min_ratio = 0.80`. Perder: `lose_min_negative = 10`, `lose_min_ratio = 0.20`, `lose_window_days = 90`. Valores en el catálogo, no en el código.
- Se califica una sola vez por pedido, solo `Delivered`, hasta **7 días** después de la entrega (fecha del cambio a `Delivered` en `StatusHistory`). «Omitir» (`skipped: true`) es definitivo.
- Las calificaciones son anónimas para el restaurante: ninguna respuesta del merchant incluye ids, nombres ni pedidos.
- Esquema en `database/01_init_schema.sql`, idempotente (`CREATE TABLE IF NOT EXISTS`, seed con `ON CONFLICT DO UPDATE`).
- Textos de interfaz y mensajes de error en español; código en inglés.
- **Commits:** la regla del repo prohíbe commit/push sin pedido explícito. Los pasos «Commit» dejan el mensaje listo; se ejecutan solo cuando el usuario lo autorice.
- Repos: backend en `back/Ate-back`, frontend en `frontend/Ate` (rutas de cada tarea relativas a su repo).

## Review Focus

1. **Doble envío** (doble clic, dos pestañas): la segunda calificación del mismo pedido responde 409 `AlreadyRated` y no cuenta doble. *(Task 4, verificación con dos `curl` seguidos)*
2. **Pedido viejo:** entregado hace más de 7 días → `pending-rating` no lo devuelve y `POST rating` responde 400. *(Task 4)*
3. **`packaging` en un pedido `local`:** el modal no muestra la pregunta y la API rechaza la respuesta con 400. *(Task 4 y Task 7)*
4. **Modal fuera de lugar:** no aparece para Merchant/CompanyAdmin, ni con cambio de contraseña pendiente, ni encima del toast de un trofeo. *(Task 7, spec del store y verificación manual)*
5. **Restaurante sin fichas o ficha desactivada (`is_active = false`):** no se muestra nada; nunca «0 fichas». *(Task 5 y Task 6)*

---

### Task 1: Modelo de datos y esquema

Backend.

**Files:**
- Create: `Ate.Domain/Entities/RestaurantBadgeDefinition.cs`, `Ate.Domain/Entities/RestaurantBadge.cs`, `Ate.Domain/Constants/RatingCategories.cs`, `Ate.Infrastructure/Persistence/Configurations/RestaurantBadgeConfiguration.cs`
- Modify: `Ate.Domain/Entities/EmployeeOrder.cs`, `Ate.Application/Common/Interfaces/IApplicationDbContext.cs`, `Ate.Infrastructure/Persistence/AteDbContext.cs`, `Ate.Infrastructure/Persistence/MongoDb/MongoDbInitializer.cs`, `database/01_init_schema.sql`

**Interfaces:**
- Produces:
  - `RatingCategories`: constantes `Punctual = "punctual"`, `Accurate = "accurate"`, `Tasty = "tasty"`, `Packaging = "packaging"`; `All: IReadOnlyList<string>`; `bool AppliesTo(string category, string deliveryMode)` (`packaging` solo para `retiro`/`entrega`).
  - `EmployeeOrder.Rating: EmployeeOrderRating?` con `Dictionary<string, bool> Answers`, `bool Skipped`, `DateTimeOffset RatedAt`.
  - `RestaurantBadgeDefinition { Code, Name, Description, Icon, Category, EarnMinPositive:int, EarnMinRatio:decimal, LoseMinNegative:int, LoseMinRatio:decimal, LoseWindowDays:int, SortOrder, IsActive }`.
  - `RestaurantBadge { RestaurantId:Guid, BadgeCode:string, Badge:RestaurantBadgeDefinition, IsHeld:bool, EarnedAt:DateTimeOffset?, LostAt:DateTimeOffset?, UpdatedAt }`, PK `(restaurant_id, badge_code)`, FK a `restaurants` y a la definición, ambas `ON DELETE CASCADE`.
  - `DbSet<RestaurantBadgeDefinition> RestaurantBadgeDefinitions`, `DbSet<RestaurantBadge> RestaurantBadges`.

- [ ] **Step 1:** entidades, constantes y configuración EF, siguiendo `AchievementConfiguration.cs` (tablas `restaurant_badge_definitions`, `restaurant_badges`; `code`/`category` VARCHAR(50), `name` 80, `description` 200, `icon` 30; ratios `NUMERIC(3,2)`).
- [ ] **Step 2:** SQL en una sección nueva `-- 13. RESTAURANT BADGES` de `01_init_schema.sql`. CHECKs: `earn_min_positive > 0`, `lose_min_negative > 0`, ratios entre 0 y 1, `lose_window_days > 0`. Seed:

  | code | name | description | icon | category | sort |
  |---|---|---|---|---|---|
  | `punctual` | Puntual | Los colaboradores dicen que sus pedidos llegan a tiempo. | `clock` | `punctual` | 10 |
  | `accurate` | Pedido exacto | Los colaboradores reciben lo que pidieron. | `check` | `accurate` | 20 |
  | `tasty` | Buen sabor | A los colaboradores les gusta su comida. | `chef` | `tasty` | 30 |
  | `packaging` | Bien empacado | Sus pedidos para llevar llegan bien empacados. | `package` | `packaging` | 40 |

  Todas con 10 / 0.80 / 10 / 0.20 / 90.
- [ ] **Step 3:** índice Mongo en `MongoDbInitializer`: `keys.Ascending(o => o.RestaurantId).Ascending("Rating.RatedAt")`, nombre `ix_employee_orders_restaurant_rated_at`, parcial con `Filter.Exists("Rating")`.
- [ ] **Step 4: Verificar.** `dotnet build` → 0 errores. Arrancar la API en Development → el log no muestra errores de esquema; `SELECT count(*) FROM restaurant_badge_definitions` → 4.
- [ ] **Step 5: Commit** `feat(badges): restaurant badge catalog, state table and order rating model`.

### Task 2: Calculadora de fichas (función pura, TDD)

Backend.

**Files:**
- Create: `Ate.Application/Features/RestaurantBadges/RestaurantBadgeCalculator.cs`, `Ate.Tests/RestaurantBadges/RestaurantBadgeCalculatorTests.cs`

**Interfaces:**
- Consumes: `RestaurantBadgeDefinition` (Task 1).
- Produces:
  - `record BadgeAnswer(bool Positive, DateTimeOffset RatedAt)`
  - `record BadgeState(bool IsHeld, DateTimeOffset? EarnedAt, DateTimeOffset? LostAt)`
  - `static BadgeState RestaurantBadgeCalculator.Next(RestaurantBadgeDefinition definition, BadgeState current, IReadOnlyCollection<BadgeAnswer> answers, DateTimeOffset now)`

Regla (una sola transición por llamada):
- Si `!IsHeld`: cuenta las respuestas con `RatedAt > LostAt` (todas si `LostAt` es null). Si `positivas ≥ EarnMinPositive` y `positivas / total ≥ EarnMinRatio` → `IsHeld = true`, `EarnedAt = now`, `LostAt` se conserva.
- Si `IsHeld`: cuenta las respuestas con `RatedAt > EarnedAt` y `RatedAt ≥ now − LoseWindowDays`. Si `negativas ≥ LoseMinNegative` y `negativas / total ≥ LoseMinRatio` → `IsHeld = false`, `LostAt = now`, `EarnedAt` se conserva.
- En otro caso devuelve `current` sin cambios.

- [ ] **Step 1: Tests que fallan** (`now = 2026-11-18T12:00Z`; respuestas fechadas 1 día antes salvo que se indique):

  | Test | Estado inicial | Respuestas | Esperado |
  |---|---|---|---|
  | `DoesNotEarn_WhenRatioBelow80` | sin ficha | 10 👍, 9 👎 | `IsHeld == false` |
  | `Earns_WhenTenPositivesAndRatioAbove80` | sin ficha | 12 👍, 2 👎 | `IsHeld == true`, `EarnedAt == now` |
  | `Earns_AtExactly80Percent` | sin ficha | 12 👍, 3 👎 | `IsHeld == true` |
  | `DoesNotEarn_WithFewerThanTenPositives` | sin ficha | 9 👍, 0 👎 | `IsHeld == false` |
  | `Keeps_WhenComplaintsAreSmallShareOfVolume` | ganada hace 200 días | 2000 👍, 10 👎 | `IsHeld == true` |
  | `Loses_WhenTenComplaintsAndRatioAbove20` | ganada hace 200 días | 30 👍, 12 👎 | `IsHeld == false`, `LostAt == now` |
  | `Keeps_WhenComplaintsAreOlderThanWindow` | ganada hace 200 días | 12 👎 de hace 100 días | `IsHeld == true` |
  | `Keeps_WhenComplaintsPredateEarning` | ganada hace 5 días | 12 👎 de hace 10 días | `IsHeld == true` |
  | `DoesNotReEarn_WithPositivesBeforeLoss` | perdida hace 5 días | 20 👍 de hace 10 días | `IsHeld == false` |
  | `ReEarns_WithPositivesAfterLoss` | perdida hace 5 días | 10 👍 de hace 1 día | `IsHeld == true`, `LostAt` igual al inicial |

- [ ] **Step 2:** `dotnet test --filter RestaurantBadgeCalculatorTests` → FAIL (no compila: `RestaurantBadgeCalculator` no existe).
- [ ] **Step 3:** implementar `Next` según la regla.
- [ ] **Step 4:** `dotnet test --filter RestaurantBadgeCalculatorTests` → 10 PASS; `dotnet test` completo → PASS.
- [ ] **Step 5: Commit** `feat(badges): pure badge earn/lose calculator with tests`.

### Task 3: Servicio de fichas

Backend.

**Files:**
- Create: `Ate.Application/Common/Interfaces/IRestaurantBadgeService.cs`, `Ate.Application/Features/RestaurantBadges/DTOs/RestaurantBadgeDtos.cs`, `Ate.Infrastructure/Services/RestaurantBadgeService.cs`

**Interfaces:**
- Consumes: Task 1 (entidades, `DbSet`s, colección `employee_orders`), Task 2 (`RestaurantBadgeCalculator.Next`).
- Produces (`IRestaurantBadgeService : IScopedDependency`):
  - `Task RecalculateAsync(Guid restaurantId, CancellationToken ct)`
  - `Task<IReadOnlyDictionary<Guid, IReadOnlyList<RestaurantBadgeDto>>> GetHeldAsync(IReadOnlyCollection<Guid> restaurantIds, CancellationToken ct)`; restaurantes sin fichas no aparecen en el diccionario.
  - `Task<Result<IReadOnlyList<MerchantBadgeSummaryDto>>> GetMerchantSummaryAsync(Guid merchantUserId, CancellationToken ct)`
  - `record RestaurantBadgeDto(string Code, string Name, string Description, string Icon)`
  - `record MerchantBadgeSummaryDto(string Code, string Name, string Description, string Icon, bool IsHeld, DateTimeOffset? EarnedAt, int Positive90d, int Negative90d)`

- [ ] **Step 1:** `RecalculateAsync`:
  - Lee de Mongo, con el índice de Task 1, los pedidos del restaurante con `Rating` no omitido.
  - Por cada definición activa: arma las `BadgeAnswer` de su categoría, carga el estado (`BadgeState(false, null, null)` si no hay fila), llama a `Next` y guarda solo si cambió (upsert; `UpdatedAt = now`).
  - Usa `IDateTimeProvider` para `now`.
- [ ] **Step 2:** `GetHeldAsync`: solo `IsHeld` y definición `IsActive`, ordenado por `SortOrder`. Una sola consulta para todos los ids.
- [ ] **Step 3:** `GetMerchantSummaryAsync`: busca el restaurante por `MerchantUserId` (como `MerchantRestaurantService.GetAsync`; si no existe, el mismo error que ese servicio). Devuelve todas las definiciones activas con el estado y los 👍/👎 de los últimos 90 días por categoría. Sin datos de colaboradores.
- [ ] **Step 4: Verificar.** `dotnet build` → 0 errores.
- [ ] **Step 5: Commit** `feat(badges): badge service with on-the-spot recalculation`.

### Task 4: Calificar un pedido (API)

Backend.

**Files:**
- Create: `Ate.Application/Features/EmployeeOrders/DTOs/RateOrderRequest.cs`, `Ate.Application/Features/EmployeeOrders/DTOs/PendingRatingDto.cs`, `Ate.Application/Features/EmployeeOrders/Validators/RateOrderRequestValidator.cs`
- Modify: `Ate.Application/Common/Interfaces/IEmployeeOrderService.cs`, `Ate.Infrastructure/Services/EmployeeOrderService.cs`, `Ate.Api/Controllers/EmployeeOrdersController.cs`, `Ate.Domain/Errors/EmployeeOrderErrors.cs`, `Ate.Application/Features/EmployeeOrders/DTOs/EmployeeOrderDto.cs` (agregar `bool CanRate`)

**Interfaces:**
- Consumes: `RatingCategories` (Task 1), `IRestaurantBadgeService.RecalculateAsync` (Task 3).
- Produces:
  - `record RateOrderRequest(Dictionary<string, bool>? Answers, bool Skipped)`
  - `record PendingRatingDto(string OrderId, Guid RestaurantId, string RestaurantName, string MealType, IReadOnlyList<string> DishNames, string DeliveryMode, IReadOnlyList<string> Categories)`; `Categories` ya filtradas con `RatingCategories.AppliesTo`.
  - `Task<Result> RateAsync(Guid userId, string orderId, RateOrderRequest request, CancellationToken ct)`
  - `Task<Result<PendingRatingDto?>> GetPendingRatingAsync(Guid userId, CancellationToken ct)`
  - `POST api/v1/employee/orders/{orderId}/rating` → 204; `GET api/v1/employee/orders/pending-rating` → 200 (`data: null` si no hay).
  - Errores nuevos en `EmployeeOrderErrors`:
    - `NotRatable` (Validation): «Solo puedes calificar pedidos entregados en los últimos 7 días.»
    - `AlreadyRated` (Conflict): «Ya calificaste este pedido.»
    - `InvalidRating` (Validation): «Responde al menos una pregunta o elige omitir.»

- [ ] **Step 1:** validador. Claves de `Answers` ⊂ `RatingCategories.All`; si `Skipped == false`, al menos una respuesta; si `Skipped == true`, `Answers` vacío o nulo.
- [ ] **Step 2:** `RateAsync`:
  - Pedido de otro usuario o inexistente → `NotFound`.
  - No `Delivered`, o la entrega (último `StatusHistory` con `Delivered`) fue hace más de 7 días → `NotRatable`.
  - Una respuesta en una categoría que no aplica al `DeliveryMode` → `InvalidRating`.
  - `UpdateOne` con filtro `Id == orderId && UserId == userId && !Exists(Rating)`. Si `ModifiedCount == 0` → `AlreadyRated`.
  - Si no fue omitido → `RecalculateAsync(order.RestaurantId)`.
- [ ] **Step 3:** `GetPendingRatingAsync`: el pedido `Delivered` más reciente del usuario, sin `Rating` y entregado hace ≤ 7 días. Si no hay, `null`.
- [ ] **Step 4:** `CanRate` en `EmployeeOrderDto` con la misma regla (lo usa el historial en el futuro; en v1 solo se expone).
- [ ] **Step 5: Verificar** con `curl` y la API local (un pedido marcado `Delivered` desde el panel del comercio):
  - `GET pending-rating` → el pedido.
  - `POST rating {answers:{punctual:true}}` → 204; repetirlo → 409 `AlreadyRated`.
  - `POST rating {answers:{packaging:true}}` en un pedido `local` → 400.
  - `POST rating {answers:{}, skipped:false}` → 400.
  - Fijar en Mongo la fecha de `Delivered` en hace 8 días → `pending-rating` devuelve `null` y `POST` → 400.
  - En `restaurant_badges` aparece la fila cuando se cumple la regla (cargar 10 calificaciones de prueba).
- [ ] **Step 6: Commit** `feat(badges): rate delivered orders and pending-rating endpoint`.

### Task 5: Fichas en las respuestas de restaurantes

Backend.

**Files:**
- Modify: `Ate.Application/Features/Restaurants/DTOs/RestaurantDto.cs`, `Ate.Infrastructure/Services/RestaurantService.cs`, `Ate.Api/Controllers/MerchantRestaurantController.cs`

**Interfaces:**
- Consumes: `GetHeldAsync`, `GetMerchantSummaryAsync` (Task 3).
- Produces:
  - `RestaurantDto` con un último parámetro nuevo, `IReadOnlyList<RestaurantBadgeDto> Badges` (lista vacía si no tiene).
  - `GET api/v1/merchant/restaurant/badges` → `ApiResponse<IReadOnlyList<MerchantBadgeSummaryDto>>`, rol `Merchant`.

- [ ] **Step 1:** `GetForEmployeeAsync` llama una vez a `GetHeldAsync` con todos los ids y completa `Badges`.
- [ ] **Step 2:** endpoint merchant.
- [ ] **Step 3: Verificar.**
  - `GET /employee/restaurants` → `badges: []` en los que no tienen ficha, y la ficha en el que la ganó en Task 4.
  - Con `UPDATE restaurant_badge_definitions SET is_active=false WHERE code='punctual'` → desaparece.
  - `GET /merchant/restaurant/badges` → 4 categorías con totales y sin datos de colaboradores.
- [ ] **Step 4: Commit** `feat(badges): expose held badges to employees and summary to merchants`.

### Task 6: Fichas en el dashboard del colaborador

Frontend.

**Files:**
- Create: `src/app/shared/ui/badge-icon/badge-icon.ts`, `src/app/shared/ui/restaurant-badges/restaurant-badges.ts` (+ `.html`)
- Modify: `src/app/shared/ui/index.ts`, `src/app/features/dashboard/data-access/dashboard.models.ts`, `src/app/features/dashboard/data-access/dashboard.demo.ts`, `src/app/features/dashboard/ui/restaurant-card/restaurant-card.html`, `src/app/features/dashboard/ui/restaurant-menu-panel/restaurant-menu-panel.html`

**Interfaces:**
- Consumes: `badges` de `GET /employee/restaurants` (Task 5).
- Produces:
  - `interface RestaurantBadge { code: string; name: string; description: string; icon: string }`; `Restaurant.badges: RestaurantBadge[]`
  - `<app-badge-icon [name]="'clock'|'check'|'chef'|'package'" />`: SVG de trazo, viewBox 24, mismo estilo que `trophy-icon`.
  - `<app-restaurant-badges [badges]="..." [variant]="'compact'|'full'" />`
    - `compact`: solo iconos, con `title` y `aria-label` = nombre.
    - `full`: chips con icono y nombre; al pulsar uno se muestra su `description` debajo (uno abierto a la vez).
    - Con lista vacía no renderiza nada.

- [ ] **Step 1:** componentes y modelos; datos demo con una ficha en un restaurante.
- [ ] **Step 2:** `compact` en `restaurant-card`; `full` en el encabezado de `restaurant-menu-panel`, debajo del nombre.
- [ ] **Step 3: Verificar.** `npx ng lint`, `npm test` y `npm run build` pasan. Con la API local: el restaurante con ficha muestra el icono en la tarjeta y el chip en el panel; los demás no muestran nada; se ve bien en modo claro y oscuro y a 360 px.
- [ ] **Step 4: Commit** `feat(badges): show restaurant badges on card and menu panel`.

### Task 7: Modal de calificación

Frontend.

**Files:**
- Create: `src/app/features/ratings/data-access/ratings.api.ts`, `ratings.models.ts`, `ratings.store.ts`, `ratings.store.spec.ts`; `src/app/features/ratings/ui/rating-modal/rating-modal.ts` (+ `.html`); `src/app/features/ratings/pages/rating-prompter/rating-prompter.ts`; `src/app/features/ratings/index.ts`
- Modify: `src/app/app.html`, `src/app/app.ts`, `src/app/features/achievements/data-access/achievements.store.ts` (solo si hace falta exponer si hay toast visible)

**Interfaces:**
- Consumes: `GET pending-rating`, `POST rating` (Task 4); `AchievementsStore.currentToast` (existente).
- Produces:
  - `RatingsStore`: `pending: Signal<PendingRating | null>`, `checkPending(): void`, `submit(answers: Partial<Record<RatingCategory, boolean>>): void`, `skip(): void`, `reset(): void`.
  - `<app-rating-prompter />` montado en `app.html` junto a `<app-achievement-notifier />`.

Textos de las preguntas: `punctual` «¿Llegó a tiempo?», `accurate` «¿Llegó lo que pediste?», `tasty` «¿La comida estaba buena?», `packaging` «¿Llegó bien empacado?». Título: «¿Qué tal tu pedido de {restaurante}?». Botones: «Omitir» y «Enviar» («Enviar» deshabilitado sin respuestas). **Sin campo de comentarios.**

- [ ] **Step 1: Tests del store que fallan** (`ratings.store.spec.ts`, con el API simulado):
  - `checkPending` con respuesta `null` → `pending()` es `null`.
  - `submit({ punctual: true })` → llama a `POST` con `{ answers: { punctual: true }, skipped: false }` y deja `pending()` en `null`.
  - `skip()` → `POST` con `{ answers: {}, skipped: true }` y `pending()` en `null`.
  - un 409 en `submit` → `pending()` queda en `null`, sin mostrar error.
- [ ] **Step 2:** `npm test -- ratings.store` → FAIL.
- [ ] **Step 3:** store y API.
- [ ] **Step 4:** `npm test -- ratings.store` → PASS.
- [ ] **Step 5:** `RatingPrompter`:
  - Activo solo con rol `Employee` y sin `mustChangePassword` (como `AchievementNotifier`).
  - Consulta al iniciar, al volver a la pestaña y cada 60 s.
  - Muestra el modal solo si `pending()` y no hay toast de trofeo.
  - El modal pinta solo las `categories` que manda el backend; 👍/👎 con `aria-pressed`, y al pulsar la misma opción otra vez se desmarca.
  - Foco atrapado; Escape = omitir.
- [ ] **Step 6: Verificar.**
  - `npx ng lint`, `npm test` y `npm run build` pasan.
  - Con la API local: marcar un pedido `Delivered` desde el comercio → en ≤ 60 s el colaborador ve el modal; «Enviar» lo cierra y no vuelve a salir; un pedido `local` no muestra «empacado».
  - Un merchant no ve el modal.
  - Doble clic en «Enviar» no muestra error.
- [ ] **Step 7: Commit** `feat(badges): post-delivery rating modal`.

### Task 8: «Tus fichas» en el panel del comercio

Frontend.

**Files:**
- Modify: `src/app/features/merchant/data-access/merchant.api.ts`, `merchant.model.ts`, `merchant.store.ts`, `merchant.store.spec.ts`, `src/app/features/merchant/pages/merchant-dashboard-page.html` (+ `.ts`)

**Interfaces:**
- Consumes: `GET /merchant/restaurant/badges` (Task 5); `<app-badge-icon>` (Task 6).
- Produces: `MerchantStore.badges: Signal<MerchantBadgeSummary[]>`, cargado junto con los datos del panel.

- [ ] **Step 1: Test del store que falla:** al cargar, `badges()` tiene las 4 categorías de la respuesta simulada.
- [ ] **Step 2:** `npm test -- merchant.store` → FAIL; implementar; → PASS.
- [ ] **Step 3:** tarjeta «Tus fichas»: por categoría, el icono (apagado si no la tiene), el nombre, «Desde {fecha}» si la tiene, y «👍 n · 👎 n en los últimos 90 días». Debajo, una línea: «Ganas una ficha con al menos 10 👍 y 80 % de calificaciones positivas.»
- [ ] **Step 4: Verificar.** `npx ng lint`, `npm test` y `npm run build` pasan; el panel del comercio muestra la tarjeta con los totales de la API local.
- [ ] **Step 5: Commit** `feat(badges): merchant badge summary card`.
