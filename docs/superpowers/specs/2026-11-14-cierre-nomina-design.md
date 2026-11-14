# Cierre de nómina por colaborador — Diseño

Fecha: 2026-11-14 · Alcance: backend (`Ate-back`) y frontend (`Ate`)

## Intención

Cada colaborador paga el 100% de lo que pide en Ate, y ese monto se le descuenta por
nómina. RRHH necesita, al final de cada mes, saber **cuánto descontarle a cada
colaborador**. Cuando alguien sale de la empresa, lo consumido en su último mes se
descuenta de la **liquidación**.

El consumo es información privada del colaborador: RRHH solo conoce el **total a
descontar** por persona y el uso **agregado** de la plataforma en su empresa. Nunca qué
pidió, dónde ni cuándo.

## Objetivos

1. Generar automáticamente un **corte** por colaborador al cerrar cada mes y al
   desactivarlo (salida de la empresa).
2. Guardar los cortes como **historial congelado** que no cambia después de generado.
3. RRHH descarga un **PDF** por corte con nombre, cédula y total a descontar.
4. El colaborador ve su gasto del mes en curso y el detalle de cada corte, y puede
   descargar su PDF.
5. RRHH ve el uso de la plataforma en su empresa, solo en agregado de toda la empresa.

## No objetivos

- Subsidio o cupo de la empresa: no existe. El colaborador paga todo.
- Límite de gasto por colaborador.
- Exportar a Excel o integrarse con un sistema de nómina.
- Ajustes, devoluciones o anulación de un corte ya generado. El enum se puede ampliar
  después con un tipo nuevo, por ejemplo `Adjustment`.
- Enviar los cortes por correo.

## Anti-objetivos (sería un fracaso aunque funcione)

- RRHH ve platos, restaurantes, fechas o el gasto del mes en curso de una persona.
- Un consumo se cobra dos veces (en la liquidación **y** en el mensual).
- Un corte ya entregado a RRHH cambia de monto después.
- El agregado de uso permite deducir el gasto de una persona; por eso no se divide por área.

## Decisiones tomadas con el usuario

| Tema | Decisión |
|---|---|
| Quién paga | El colaborador, 100%. No hay subsidio. |
| Uso del corte | RRHH lo usa para descontar del sueldo o de la liquidación. |
| Tipos de corte | Enum `SettlementType { Monthly, Termination }`, guardado como texto. |
| Estados del colaborador | Enum `EmployeeStatus { Active, Inactive, NotCurrent }` (ya implementado). `Inactive` = suspensión temporal, sin liquidación. `NotCurrent` ("No vigente") = ya no pertenece a la empresa. |
| Salida | El corte `Termination` se genera **solo** cuando RRHH marca al colaborador como **No vigente**, en un flujo de salida aparte (pendiente). Desactivar (`Inactive`) no genera liquidación. |
| Privacidad | RRHH solo ve el total por persona en los cortes y el uso agregado de toda la empresa. |
| Endpoint actual | Se elimina `GET company/employees/{id}/orders` (RRHH veía los pedidos de cada colaborador). |
| Formato | PDF. |
| Almacenamiento | MongoDB, junto a los pedidos. |

## Enfoques considerados

- **A. Cortes guardados y congelados (elegido).** Se generan solos el día 1 y al
  desactivar. Se guardan con el detalle copiado y no cambian. Dejan un registro fijo de
  lo cobrado y evitan cobrar dos veces.
- **B. Reporte calculado en el momento.** Descartado: el monto de un mes ya cobrado
  podría cambiar, no queda prueba de lo descontado y es fácil cobrar dos veces.
- **C. Vista previa para RRHH + cierre.** Descartado: RRHH no debe ver el gasto en curso
  de cada persona.

## Diseño

### Qué cuenta para el cobro

- Pedidos en estado `Confirmed`, `Ready` o `Delivered`. No cuentan `Cancelled` ni
  `Pending`; un `Pending` nunca se preparó.
- El período se toma por **`ServiceDate`** (el día en que se come), en hora de negocio
  `America/Guayaquil`, no por `OrderedAt`.
- Se usa el `TotalAmount` congelado en el pedido.

### Modelo (MongoDB, colección `payroll_settlements`)

```
PayrollSettlement
  Id, Type (SettlementType), CompanyId, EmployeeId, UserId
  EmployeeName, EmployeeDocument            // copia al momento del corte
  PeriodStart, PeriodEnd                    // yyyy-MM-dd, inclusivo, por ServiceDate
  TotalAmount, Currency, OrderCount
  Lines[]: OrderId, TicketNumber, ServiceDate, RestaurantName, MealType, Items[], Amount
  GeneratedAt, GeneratedByUserId?           // null cuando lo genera el worker
```

- Índice único `(EmployeeId, Type, PeriodStart)`: impide generar dos veces el mismo corte.
- Índices `(CompanyId, PeriodStart)` y `(EmployeeId, PeriodStart)` para los listados.
- `SettlementType` en `Ate.Domain/Enums`. Mongo ya guarda los enums como texto
  (`EnumRepresentationConvention`).
- **Las `Lines` no salen nunca en un endpoint de RRHH.**

### Corte mensual

- Un `BackgroundService` nuevo (como `AchievementWorker`) corre a diario y, el día 1 en
  hora de negocio, genera los cortes del mes anterior. Es idempotente: si ya existe, lo
  salta, gracias al índice único. Así, si el servidor estuvo caído el día 1, se recupera
  al día siguiente.
- Incluye a cada colaborador con consumo mayor que 0 en el período.
- **No se cobra dos veces:** si el colaborador ya tiene un corte `Termination` dentro de
  ese mes, el mensual empieza el día siguiente a ese corte. Si después ya no tuvo
  consumo, no se genera.

### Corte por salida (Termination)

- En el flujo de salida (pendiente de diseñar la pantalla), RRHH marca al colaborador
  como **No vigente** e indica la **fecha de salida**, que por defecto es hoy. Es una
  acción distinta de desactivar (`Inactive`).
- En la misma operación:
  1. Se cancelan sus pedidos activos con `ServiceDate` posterior a la fecha de salida.
  2. Se genera el corte `Termination` desde el inicio de ese mes, o desde el día
     siguiente a su último corte, hasta la fecha de salida.
- Si se reactiva al colaborador, el siguiente mensual empieza el día siguiente a la fecha
  de salida.

### API

| Rol | Endpoint | Devuelve |
|---|---|---|
| Employee | `GET employee/settlements/current` | Gasto del mes en curso: total y pedidos que cuentan. |
| Employee | `GET employee/settlements` | Historial de sus cortes, con detalle. |
| Employee | `GET employee/settlements/{id}/pdf` | PDF con detalle de su corte. |
| CompanyAdmin | `GET company/settlements?month=yyyy-MM` | Cortes de su empresa: tipo, período, nombre, cédula, total. Sin `Lines`. |
| CompanyAdmin | `GET company/settlements/monthly/{yyyy-MM}/pdf` | PDF del cierre mensual: una fila por colaborador. |
| CompanyAdmin | `GET company/settlements/{id}/pdf` | PDF de una liquidación: un colaborador. |
| CompanyAdmin | `GET company/usage?month=yyyy-MM` | Uso agregado: colaboradores que pidieron, pedidos, consumo total de la empresa. |

- Todo se filtra por la identidad del token: el colaborador solo ve sus cortes y RRHH
  solo los de su empresa. Es el mismo patrón que el resto del backend.
- **Se elimina** `GET company/employees/{employeeId}/orders` y
  `GetCompanyEmployeeOrdersAsync`.

### PDF

- **RRHH, mensual:** empresa, mes, tabla con cédula, nombre y total, más el total general
  y la fecha de generación.
- **RRHH, liquidación:** empresa, colaborador, cédula, período, total y fecha de salida.
- **Colaborador:** su corte con la tabla de pedidos (fecha, restaurante, comida, monto) y
  el total.

### Frontend

- **Colaborador:** una sección "Mis consumos" con el gasto del mes en curso, el historial
  de cortes con su tipo ("Mensual" / "Liquidación") y la descarga del PDF.
- **RRHH:**
  - Una página "Nómina" con un selector de mes, la lista de cortes (tipo, nombre, cédula,
    total) y la descarga de los PDF.
  - Una tarjeta de uso agregado en el dashboard de empresa.
- **Desactivar colaborador:** se pide la fecha de salida y se avisa que se generará la
  liquidación.
- El tipo se representa en TypeScript como `'Monthly' | 'Termination'`, con la etiqueta en
  español solo al mostrarlo.

## Decisiones mías (revisar)

- **Librería de PDF:** QuestPDF. Su licencia Community es gratuita para empresas con
  ingresos menores a USD 1M al año; hay que confirmarlo.
- Solo se generan cortes con consumo mayor que 0.
- Si el estado de un pedido de un mes ya cerrado cambia después del cierre, el corte no
  se actualiza. Es una limitación conocida que se resolvería con el tipo de ajuste en el
  futuro.
- El campo `Employee.MonthlyAllowance` no se usa. Se deja tal cual y se limpia aparte.

## Queda para quien lo construya

- Cómo se organizan internamente el worker y el servicio de cortes.
- El diseño visual de las pantallas y los PDF, siguiendo el sistema de diseño actual.
- Las pruebas: el cálculo del período, la regla de no cobrar dos veces y los filtros de
  privacidad son lo crítico.
