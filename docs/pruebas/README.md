# Pruebas automáticas

Tests que se conservan en `Ate.Tests` (xUnit) y qué protege cada uno. Fecha: 2026-11-19.

Criterio: solo se conservan tests de **reglas del negocio o de seguridad** donde un error costaría dinero,
confianza o datos y no se notaría a simple vista en la pantalla. No se prueban textos ni conexiones internas.

| Grupo | Archivo | Tests | Qué protege |
|---|---|---|---|
| Nómina mensual | `Payroll/SettlementRulesTests.cs` | 15 (19 ejecuciones) | Cuánto se descuenta del sueldo y en qué período |
| Fichas de restaurante | `RestaurantBadges/RestaurantBadgeCalculatorTests.cs` | 10 | Cuándo un restaurante gana o pierde una ficha |
| Imágenes y rutas | `Storage/StorageRulesTests.cs` | 4 (12 ejecuciones) | Que no entren archivos falsos ni rutas peligrosas |
| Ticket único | `Idempotency/IdempotencyDecisionTests.cs` | 6 (7 ejecuciones) | Que un doble clic no cree dos pedidos |

**Total:** 48 ejecuciones. Comando: `dotnet test Ate.Tests`.

## 1. Nómina mensual (`SettlementRules`)

Calcula el total que se descuenta por nómina a cada colaborador.

| Test | Qué comprueba |
|---|---|
| `IsChargeable_OnlyConfirmedReadyDelivered` | Solo se cobran pedidos Confirmados, Listos o Entregados; nunca Pendientes ni Cancelados |
| `MonthlyStart_NoPreviousSettlement_IsMonthStart` | Sin cortes previos, el período empieza el día 1 del mes |
| `MonthlyStart_AfterTerminationOnThe10th_StartsOnThe11th` | Si hubo una liquidación por salida el día 10, el mensual empieza el 11 |
| `MonthlyStart_SettlementFromPreviousMonth_IsIgnored` | Un corte del mes anterior no cambia el inicio del mes actual |
| `TerminationStart_FirstDayOfExitMonth` | La liquidación por salida empieza el día 1 del mes de salida |
| `TerminationStart_AfterSettlementInsideExitMonth_StartsNextDay` | Si ya hubo un corte en ese mes, empieza al día siguiente del corte |
| `LatestCoveredEnd_MonthlyEndingAfterBackdatedExit_IsFound` | Salida registrada con fecha anterior a un mensual ya cerrado: no se cobra dos veces |
| `LatestCoveredEnd_IgnoresSettlementsOutsideRange` | Los cortes fuera del rango no cuentan |
| `LatestCoveredEnd_TakesTheLatestOverlappingEnd` | Entre varios cortes que se cruzan, toma el que termina más tarde |
| `MonthToClose_OnDay1_IsNotYetDue` | El día 1 todavía no se cierra el mes anterior |
| `MonthToClose_FromDay2_IsPreviousMonth` | Desde el día 2 se cierra el mes anterior |
| `Build_CountsOnlyChargeableOrdersInsidePeriod` | Suma solo los pedidos cobrables del período (ej.: 2 pedidos = $5,75) |
| `Build_AfterTerminationOnThe10th_ChargesOnlyFromThe11th` | Tras una salida el 10, solo cobra desde el 11 |
| `Build_SumsDecimalAmountsExactly` | Suma con decimales exactos, sin errores de redondeo |
| `Build_NothingChargeable_ReturnsNull` | Si no hay nada que cobrar, no se genera corte |

## 2. Fichas de restaurante (`RestaurantBadgeCalculator`)

Regla: se **gana** con al menos 10 👍 y 80 % positivas; se **pierde** con al menos 10 👎 en 90 días que sean
el 20 % o más. Los valores viven en la tabla `restaurant_badge_definitions`.

| Test | Situación | Resultado esperado |
|---|---|---|
| `DoesNotEarn_WhenRatioBelow80` | 10 👍 y 9 👎 (53 %) | No la gana |
| `Earns_WhenTenPositivesAndRatioAbove80` | 12 👍 y 2 👎 (86 %) | La gana |
| `Earns_AtExactly80Percent` | 12 👍 y 3 👎 (80 % justo) | La gana |
| `DoesNotEarn_WithFewerThanTenPositives` | 9 👍 y 0 👎 | No la gana (faltan 👍) |
| `Keeps_WhenComplaintsAreSmallShareOfVolume` | Tiene la ficha; 2.000 👍 y 10 👎 | La mantiene (0,5 % de quejas) |
| `Loses_WhenTenComplaintsAndRatioAbove20` | Tiene la ficha; 30 👍 y 12 👎 | La pierde (29 % de quejas) |
| `Keeps_WhenComplaintsAreOlderThanWindow` | 12 👎 de hace 100 días | La mantiene (fuera de los 90 días) |
| `Keeps_WhenComplaintsPredateEarning` | 12 👎 anteriores a ganarla | La mantiene |
| `DoesNotReEarn_WithPositivesBeforeLoss` | La perdió; 20 👍 de antes de perderla | No la recupera |
| `ReEarns_WithPositivesAfterLoss` | La perdió; 10 👍 nuevas | La recupera |

## 3. Imágenes y rutas (`ImageDataUrl`, `StorageKeys`)

Seguridad del almacenamiento de archivos (logos hoy, documentos después).

| Test | Qué comprueba |
|---|---|
| `Decodes_WhenTheBytesMatchTheDeclaredType` | Acepta PNG, JPG y WebP cuando sus bytes reales coinciden con el tipo declarado |
| `Rejects_WhenTheBytesDoNotMatchTheDeclaredType` | Rechaza un archivo que dice ser PNG pero no lo es |
| `Rejects_AnythingThatIsNotAnImageDataUrl` | Rechaza enlaces externos, SVG (puede llevar scripts) y base64 inválido |
| `ValidatesStorageKeys` | Acepta rutas como `logos/restaurants/{id}/{archivo}.webp`; rechaza `..`, rutas absolutas, `\`, `//`, mayúsculas y vacías |

## 4. Ticket único (`IdempotencyDecision`)

Qué hace el servidor cuando llega un ticket `Idempotency-Key` (crear pedido, calificar).

| Test | Situación | Respuesta |
|---|---|---|
| `Execute_WhenNoRecord` | Ticket nuevo | Se procesa |
| `Replay_WhenCompletedWithSameHash` | Ya terminó, mismo contenido | Devuelve la misma respuesta (no repite el pedido) |
| `InProgress_WhenNotCompletedWithSameHash` | Todavía se procesa | 409 «en proceso» |
| `KeyReused_WhenHashDiffers` | Mismo ticket, otro contenido | 422 «clave ya usada» |
| `Execute_WhenInProgressTicketIsOlderThanTheLease` | Quedó «en proceso» más de 60 s (se cortó la conexión) | Se procesa de nuevo, no queda trabado 24 h |
| `Replay_EvenWhenCompletedTicketIsOld` | Terminó hace una hora | Sigue devolviendo la misma respuesta |

## Pendiente (flujos sin tests todavía)

1. Crear y cancelar pedido (hora de corte, un pedido activo por comida).
2. Inicio de sesión (bloqueo por intentos, renovación con cookie, cierre de sesión).
3. Recuperar contraseña (vencimiento del código, límite de intentos).

Para probar la API de punta a punta sin tocar la base compartida: tests de integración con
`WebApplicationFactory` y Testcontainers (PostgreSQL y Mongo desechables).
