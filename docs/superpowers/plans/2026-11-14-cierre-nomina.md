# Cierre de nómina — Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generar cortes de nómina congelados por colaborador (Mensual y Liquidación), con PDF para RRHH y para el colaborador, el uso agregado de la empresa, y eliminar el acceso de RRHH a los pedidos de cada colaborador.

**Architecture:** Las reglas de cálculo (qué cuenta, períodos, armado del corte) son funciones puras en `Ate.Application/Features/Payroll`, cubiertas por un proyecto xUnit nuevo. **Esos son los únicos tests del plan**: el usuario pidió tests solo para el cálculo del total, porque ahí se maneja dinero. El resto se verifica a mano. `PayrollSettlementService` (Infrastructure) lee `employee_orders` y escribe `payroll_settlements` en MongoDB. Un `BackgroundService` genera el cierre mensual de forma idempotente. QuestPDF genera los PDF. En el frontend hay una feature nueva `settlements` para el colaborador y una página "Nómina" en `company`.

**Tech Stack:** .NET 10, MongoDB.Driver 3.12, QuestPDF, xUnit · Angular 22, signals, Vitest 5.

**Spec:** `docs/superpowers/specs/2026-11-14-cierre-nomina-design.md`

## Global Constraints

- El colaborador paga el 100%; no existe subsidio ni cupo. `Employee.MonthlyAllowance` no se usa.
- Cuentan para el cobro solo los estados `Confirmed`, `Ready` y `Delivered`.
- El período se mide por `EmployeeOrder.ServiceDate` (`yyyy-MM-dd`, hora `America/Guayaquil`), inclusivo en ambos extremos.
- Se usa `EmployeeOrder.TotalAmount` (precio congelado en el pedido).
- `SettlementType { Monthly, Termination }`, guardado como texto (la convención de Mongo ya lo hace).
- Un corte nunca cambia después de generado. Índice único `(EmployeeId, Type, PeriodStart)`.
- **Ningún endpoint de RRHH devuelve `Lines`** ni datos de pedidos: solo tipo, período, nombre, cédula, total y cantidad de pedidos.
- El uso agregado de RRHH es de **toda la empresa**, nunca por área ni por persona.
- Solo se generan cortes con total mayor que 0.
- Textos de interfaz en español; código, archivos y commits en inglés (AGENTS.md del frontend).
- **Tests:** solo los de `SettlementRules` (cálculo del total y de los períodos, Task 1). No se escriben tests para controllers, servicios, PDF ni frontend.
- Frontend: antes de terminar cada tarea, `npx ng lint`, `npm test` (los tests existentes deben seguir pasando) y `npm run build` sin errores.
- Fuera del alcance: el flujo para marcar No vigente (endpoint y pantalla), cancelar pedidos futuros en la salida, el envío por correo y los ajustes.

## Review Focus

Los puntos 1 a 3 los cubren los tests de Task 1. El 4 y el 5 se verifican a mano (Task 3 y Task 4).

1. **Doble cobro:** un colaborador con un corte `Termination` el día 10 que después se reactiva y vuelve a pedir: el mensual de ese mes cubre solo del 11 en adelante.
2. **Bordes del mes:** un pedido con `ServiceDate` el último día del mes entra en ese mes; uno del día 1 del mes siguiente, no, aunque se haya pedido el día anterior.
3. **Worker reiniciado o caído el día 1:** volver a ejecutar el cierre del mismo mes no duplica cortes ni falla (clave duplicada = se omite).
4. **Fuga a RRHH:** el JSON de `GET company/settlements` no contiene `lines`, platos ni restaurantes; un RRHH no puede pedir el PDF de un corte de otra empresa (404).
5. **Colaborador ajeno:** un colaborador no puede descargar el PDF de un corte que no es suyo (404).

---

### Task 1: Modelo, reglas de cálculo y proyecto de tests

**Files:**
- Create: `Ate.Domain/Enums/SettlementType.cs`
- Create: `Ate.Domain/Entities/PayrollSettlement.cs`
- Create: `Ate.Application/Features/Payroll/SettlementRules.cs`
- Modify: `Ate.Application/Common/Constants/MongoCollections.cs`
- Create: `Ate.Tests/Ate.Tests.csproj` (xUnit, references `Ate.Application` and `Ate.Domain`); add it to `Ate.slnx`
- Test: `Ate.Tests/Payroll/SettlementRulesTests.cs`

**Interfaces:**
- Produces:
  - `enum SettlementType { Monthly, Termination }`
  - `class PayrollSettlement { string Id; SettlementType Type; Guid CompanyId; Guid EmployeeId; Guid UserId; string EmployeeName; string EmployeeDocument; string PeriodStart; string PeriodEnd; decimal TotalAmount; string Currency; int OrderCount; List<PayrollSettlementLine> Lines; DateTimeOffset GeneratedAt; Guid? GeneratedByUserId; }`. Periods are `yyyy-MM-dd` strings, the same format as `ServiceDate`.
  - `class PayrollSettlementLine { string OrderId; string TicketNumber; string ServiceDate; string RestaurantName; MealType MealType; List<EmployeeOrderItem> Items; decimal Amount; }`
  - `MongoCollections.PayrollSettlements = "payroll_settlements"`
  - `static class SettlementRules`:
    - `bool IsChargeable(string status)`
    - `DateOnly MonthlyStart(DateOnly monthStart, DateOnly? lastSettledEnd)`: returns `lastSettledEnd + 1` when it falls inside the month, otherwise `monthStart`.
    - `DateOnly TerminationStart(DateOnly exitDate, DateOnly? lastSettledEnd)`: returns the first day of the exit month, or `lastSettledEnd + 1` when that is later.
    - `PayrollSettlement? Build(SettlementType type, DateOnly start, DateOnly end, IReadOnlyList<EmployeeOrder> orders, DateTimeOffset generatedAt, Guid? generatedBy)`. Keeps chargeable orders with `start <= ServiceDate <= end`. Returns null when the total is 0. Takes the name, document, CompanyId and UserId from the most recent order. Orders the lines by `ServiceDate`.

- [ ] **Step 1: Create `Ate.Tests` and write the failing tests**

```csharp
[Theory]
[InlineData("Confirmed", true)] [InlineData("Ready", true)] [InlineData("Delivered", true)]
[InlineData("Pending", false)] [InlineData("Cancelled", false)]
public void IsChargeable_OnlyConfirmedReadyDelivered(string status, bool expected)
    => Assert.Equal(expected, SettlementRules.IsChargeable(status));

[Fact] public void MonthlyStart_NoPreviousSettlement_IsMonthStart()
    => Assert.Equal(new DateOnly(2026,11,1), SettlementRules.MonthlyStart(new(2026,11,1), null));

[Fact] public void MonthlyStart_AfterTerminationOnThe10th_StartsOnThe11th()
    => Assert.Equal(new DateOnly(2026,11,11), SettlementRules.MonthlyStart(new(2026,11,1), new(2026,11,10)));

[Fact] public void MonthlyStart_SettlementFromPreviousMonth_IsIgnored()
    => Assert.Equal(new DateOnly(2026,11,1), SettlementRules.MonthlyStart(new(2026,11,1), new(2026,10,31)));

[Fact] public void TerminationStart_FirstDayOfExitMonth()
    => Assert.Equal(new DateOnly(2026,11,1), SettlementRules.TerminationStart(new(2026,11,20), new(2026,10,31)));

[Fact] public void Build_CountsOnlyChargeableOrdersInsidePeriod()
// orders: 2026-10-31 Delivered 5.00, 2026-11-01 Delivered 3.50, 2026-11-15 Cancelled 4.00,
//         2026-11-30 Confirmed 2.25, 2026-12-01 Delivered 9.00
// Build(Monthly, 2026-11-01, 2026-11-30) -> TotalAmount 5.75, OrderCount 2, PeriodStart "2026-11-01", PeriodEnd "2026-11-30"

[Fact] public void Build_AfterTerminationOnThe10th_ChargesOnlyFromThe11th()
// orders: 2026-11-05 Delivered 4.00 (ya cobrado en la liquidación), 2026-11-20 Delivered 6.00
// start = MonthlyStart(2026-11-01, 2026-11-10); Build(Monthly, start, 2026-11-30) -> TotalAmount 6.00, OrderCount 1

[Fact] public void Build_SumsDecimalAmountsExactly()
// 3 Delivered orders of 2.10, 3.35 and 1.05 -> TotalAmount 6.50m (decimal, without rounding differences)

[Fact] public void Build_NothingChargeable_ReturnsNull()
```

- [ ] **Step 2: Run the tests and confirm they fail**

Run: `dotnet test Ate.Tests`
Expected: compilation FAIL, `SettlementRules` does not exist.

- [ ] **Step 3: Implement the enum, the entity, the collection constant and `SettlementRules`** with the signatures listed above.

- [ ] **Step 4: Run the tests and confirm they pass**

Run: `dotnet test Ate.Tests`
Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add Ate.Tests Ate.slnx Ate.Domain Ate.Application
git commit -m "feat(payroll): settlement model, calculation rules and test project"
```

---

### Task 2: Settlement service, Mongo indexes and monthly close worker

**Files:**
- Modify: `Ate.Infrastructure/Persistence/MongoDb/MongoDbInitializer.cs` (collection plus indexes)
- Create: `Ate.Application/Common/Interfaces/IPayrollSettlementService.cs`
- Create: `Ate.Infrastructure/Services/PayrollSettlementService.cs`
- Create: `Ate.Infrastructure/Services/PayrollCloseWorker.cs`
- Modify: `Ate.Infrastructure/DependencyInjection.cs` (`AddHostedService<PayrollCloseWorker>()`)

**Interfaces:**
- Consumes: `SettlementRules`, `PayrollSettlement` (Task 1); `IMongoDbContext`; `IDateTimeProvider.BusinessNow`.
- Produces: `IPayrollSettlementService` (IScopedDependency):
  - `Task<int> GenerateMonthlyAsync(DateOnly monthStart, CancellationToken ct)`: returns how many settlements it created.
  - `Task<Result<PayrollSettlement?>> GenerateTerminationAsync(Guid employeeId, DateOnly exitDate, Guid? generatedByUserId, CancellationToken ct)`: for the future exit flow; nothing calls it yet.

- [ ] **Step 1: Indexes in `MongoDbInitializer`**: `ux_payroll_settlements_employee_type_period` (unique: `EmployeeId`, `Type`, `PeriodStart`), `ix_payroll_settlements_company_period` (`CompanyId`, `PeriodStart`) and `ix_payroll_settlements_employee_period` (`EmployeeId`, `PeriodStart` desc). Same pattern as the existing `employee_orders` indexes.

- [ ] **Step 2: Implement `GenerateMonthlyAsync`.** Read the orders whose `ServiceDate` falls in the month and whose status is chargeable, and group them by `EmployeeId`. For each employee: find their last settlement whose `PeriodEnd` falls in the month, then compute `start = SettlementRules.MonthlyStart(...)` and `SettlementRules.Build(Monthly, start, monthEnd, ...)`. Insert when the result is not null. A `MongoWriteException` with category `DuplicateKey` counts as already generated and is skipped.

- [ ] **Step 3: Implement `GenerateTerminationAsync`** with `SettlementRules.TerminationStart` and `Build(Termination, start, exitDate, ...)`. It treats a duplicate key the same way.

- [ ] **Step 4: Implement `PayrollCloseWorker`.** It uses a `PeriodicTimer` that ticks every hour. On each tick it computes the previous month from `BusinessNow`. When that month differs from the last one closed in memory, it calls `GenerateMonthlyAsync` in a new scope, logs how many it created, and remembers the month. Exceptions get the same `try/catch` and logging as `AchievementWorker`. The first run happens at startup.

- [ ] **Step 5: Manual check against your local DB**

Run the API in Development and check the log for `Payroll close for 2026-10: N settlements created.`. Restart the API: the next run must report `0` (idempotent). In Mongo, `db.payroll_settlements.countDocuments()` must not change between the two runs.

- [ ] **Step 6: Commit**

```bash
git add Ate.Application Ate.Infrastructure
git commit -m "feat(payroll): settlement generation service and monthly close worker"
```

---

### Task 3: Employee API (current spending, history, PDF) and PDF rendering

**Files:**
- Modify: `Ate.Infrastructure/Ate.Infrastructure.csproj` (add QuestPDF; set `QuestPDF.Settings.License = LicenseType.Community` in `Ate.Infrastructure/DependencyInjection.cs`)
- Create: `Ate.Application/Features/Payroll/DTOs/EmployeeSettlementDto.cs`, `CurrentSpendingDto.cs`
- Create: `Ate.Application/Common/Interfaces/IPayrollPdfRenderer.cs`, `Ate.Infrastructure/Services/PayrollPdfRenderer.cs`
- Modify: `IPayrollSettlementService` / `PayrollSettlementService`
- Create: `Ate.Api/Controllers/EmployeeSettlementsController.cs` (`api/v1/employee/settlements`, `[Authorize(Roles = RoleConstants.Employee)]`)

**Interfaces:**
- Consumes: Task 1 and Task 2.
- Produces:
  - `record SettlementLineDto(string TicketNumber, string ServiceDate, string RestaurantName, string MealType, IReadOnlyList<string> Items, decimal Amount)`
  - `record EmployeeSettlementDto(string Id, string Type, string PeriodStart, string PeriodEnd, decimal TotalAmount, string Currency, int OrderCount, DateTimeOffset GeneratedAt, IReadOnlyList<SettlementLineDto> Lines)`
  - `record CurrentSpendingDto(string PeriodStart, string PeriodEnd, decimal TotalAmount, string Currency, int OrderCount, IReadOnlyList<SettlementLineDto> Lines)`
  - Service: `GetCurrentSpendingAsync(Guid userId)`, `GetEmployeeSettlementsAsync(Guid userId)`, `GetEmployeeSettlementPdfAsync(Guid userId, string settlementId)`. Each one resolves the active `EmployeeId` from `userId`, the same way `EmployeeOrderService.GetEmployeeIdAsync` does.
  - `IPayrollPdfRenderer`: `byte[] RenderEmployeeSettlement(PayrollSettlement s, string companyName)`, `byte[] RenderCompanyMonthly(string companyName, string month, IReadOnlyList<PayrollSettlement> settlements)`, `byte[] RenderCompanyTermination(string companyName, PayrollSettlement s)`. The two company methods **must not** print `Lines`.
  - Endpoints: `GET current`, `GET` (history, newest first), `GET {id}/pdf` → `File(bytes, "application/pdf", "consumo-{PeriodStart}.pdf")`.

- [ ] **Step 1: Implement the DTOs, the service methods and the controller.** The current spending uses `SettlementRules.MonthlyStart(firstDayOfBusinessMonth, lastSettledEnd)` up to the current business day, without saving anything. A PDF for someone else's settlement → `Result.Failure(PayrollErrors.NotFound)` → 404. Add `Ate.Domain/Errors/PayrollErrors.cs` with `NotFound` ("No encontramos este corte.").

- [ ] **Step 2: Implement `PayrollPdfRenderer`** following the content the spec sets in its PDF section, with the currency in USD and the generation date in business time.

- [ ] **Step 3: Manual check**

Log in as `empleado@ate.com` (`0943676156` / `Admin123*Ate`): `GET /api/v1/employee/settlements/current` → 200 with `lines`. `GET .../settlements/{id-of-another-employee}/pdf` → 404. Open a downloaded PDF and confirm it shows the order detail.

- [ ] **Step 4: Commit**

```bash
git add Ate.Api Ate.Application Ate.Infrastructure Ate.Domain
git commit -m "feat(payroll): employee settlements API and PDF rendering"
```

---

### Task 4: Company API (settlements, PDF, aggregate usage) and removal of the orders endpoint

**Files:**
- Create: `Ate.Application/Features/Payroll/DTOs/CompanySettlementDto.cs`, `CompanyUsageDto.cs`
- Create: `Ate.Api/Controllers/CompanySettlementsController.cs` (`api/v1/company`, `[Authorize(Roles = RoleConstants.CompanyAdmin)]`)
- Modify: `IPayrollSettlementService` / `PayrollSettlementService`
- Modify: `Ate.Api/Controllers/CompanyEmployeesController.cs` (remove `GetEmployeeOrders`), `Ate.Application/Common/Interfaces/IEmployeeOrderService.cs` and `Ate.Infrastructure/Services/EmployeeOrderService.cs` (remove `GetCompanyEmployeeOrdersAsync`)

**Interfaces:**
- Consumes: Task 1 to Task 3 (`IPayrollPdfRenderer`).
- Produces:
  - `record CompanySettlementDto(string Id, string Type, string PeriodStart, string PeriodEnd, string EmployeeName, string EmployeeDocument, decimal TotalAmount, string Currency, int OrderCount)`, with a static factory `From(PayrollSettlement s)`.
  - `record CompanyUsageDto(string Month, int EmployeesWithOrders, int OrderCount, decimal TotalAmount, string Currency)`
  - Endpoints: `GET settlements?month=yyyy-MM`, `GET settlements/monthly/{month}/pdf`, `GET settlements/{id}/pdf` (Termination only; for a Monthly settlement it returns 404), `GET usage?month=yyyy-MM`. All of them filter by the `CompanyId` of the admin (`Users.CompanyId`). An invalid `month` → 400.

- [ ] **Step 1: Implement the DTOs, the service and the controller.** The usage is computed live from `employee_orders` with `CompanyId` and the month by `ServiceDate`, counting only chargeable orders: distinct `EmployeeId`, number of orders and the sum of `TotalAmount`.

- [ ] **Step 2: Remove the orders endpoint and `GetCompanyEmployeeOrdersAsync`.** Run `dotnet build` → 0 errors.

- [ ] **Step 3: Manual check (Review Focus 4)**

As HR (`0943676155` / `Admin123*Ate`):
- `GET /api/v1/company/settlements?month=2026-10` → 200, with no `lines` key in the JSON.
- `GET /api/v1/company/employees/{id}/orders` → 404.
- `GET .../settlements/{id-from-another-company}/pdf` → 404.
- `GET .../usage?month=2026-10` → 200 with the 4 fields.

- [ ] **Step 4: Commit**

```bash
git add Ate.Api Ate.Application Ate.Infrastructure
git commit -m "feat(payroll): company settlements, payroll PDF and aggregate usage; remove HR access to employee orders"
```

---

### Task 5: Frontend — "Mis consumos" for the employee

Repo: `frontend/Ate`.

**Files:**
- Create: `src/app/features/settlements/data-access/settlements.models.ts`, `settlements.api.ts`, `settlements.store.ts`
- Create: `src/app/features/settlements/pages/my-spending-page.ts` / `.html`
- Create: `src/app/features/settlements/settlements.routes.ts`, `index.ts`
- Modify: `src/app/app.routes.ts` (`path: 'consumos'`, `title: 'Mis consumos | Ate'`, `roleGuard([ROLE_CONSTANTS.EMPLOYEE])`); employee menu entry in `core/layout/main-layout.html`, next to "Mis pedidos"

**Interfaces:**
- Consumes: Task 3 endpoints.
- Produces:
  - `type SettlementType = 'Monthly' | 'Termination'`
  - `SETTLEMENT_TYPE_LABELS: Record<SettlementType, string> = { Monthly: 'Mensual', Termination: 'Liquidación' }`
  - Interfaces `SettlementLine`, `EmployeeSettlement`, `CurrentSpending` that mirror the Task 3 DTOs.
  - `SettlementsApi.getCurrent()`, `getHistory()`, `downloadPdf(id): Observable<Blob>` (`responseType: 'blob'`).
  - `SettlementsStore`: signals `current`, `history`, `isLoading`, `error`; `load()`; `download(settlement)`, which saves `consumo-{periodStart}.pdf`.

- [ ] **Step 1: Implement the models, the API and the store** (same pattern as `features/orders`).

- [ ] **Step 2: Page.** At the top, a card with the month's spending: the big total and "N pedidos · del {periodStart} al hoy". Below it, the list of orders that count. Then the history: each settlement with its type label, period, total and a "Descargar PDF" button that expands to show its lines.

- [ ] **Step 3: Verify** `npx ng lint`, `npm test`, `npm run build` → no errors. Log in as an employee and check that `/consumos` shows the month's spending and downloads the PDF.

- [ ] **Step 4: Commit**

```bash
git add src/app/features/settlements src/app/app.routes.ts src/app/core/layout
git commit -m "feat(settlements): employee spending and settlement history"
```

---

### Task 6: Frontend — "Nómina" page and usage card for HR

Repo: `frontend/Ate`.

**Files:**
- Create: `src/app/features/company/data-access/company-payroll.models.ts`, `company-payroll.api.ts`, `company-payroll.store.ts`
- Create: `src/app/features/company/pages/company-payroll-page.ts` / `.html`
- Modify: `src/app/features/company/company.routes.ts` (`path: 'payroll'`, `title: 'Nómina | Ate'`), `ui/company-layout.html` (menu item "Nómina"), `pages/company-dashboard-page.html` / `.ts` (usage card)

**Interfaces:**
- Consumes: Task 4 endpoints; `SettlementType` and `SETTLEMENT_TYPE_LABELS` from `@features/settlements` (imported through its `index.ts`).
- Produces: `CompanyPayrollStore` with `month` (signal `yyyy-MM`, defaults to the previous month), `settlements`, `monthlyTotal` (computed: sum of the Monthly settlements), `usage`, `load()`, `downloadMonthly()`, `downloadTermination(id)`.

- [ ] **Step 1: Implement the models, the API and the store.**

- [ ] **Step 2: Page.** It has a month selector. The table has the columns Tipo, Cédula, Colaborador, Período and Total a descontar. The button "Descargar PDF del mes" covers the Monthly settlements, and each Termination row has its own button. The empty state reads "No hay cortes para este mes." No order detail appears.

- [ ] **Step 3: Dashboard usage card**: "Uso de Ate este mes", with colaboradores que pidieron, pedidos and consumo total.

- [ ] **Step 4: Verify** `npx ng lint`, `npm test`, `npm run build` → no errors. Log in as HR, open "Nómina", download the month's PDF and confirm it shows only cédula, name and total.

- [ ] **Step 5: Commit**

```bash
git add src/app/features/company
git commit -m "feat(company): payroll page with settlement PDFs and aggregate usage card"
```
