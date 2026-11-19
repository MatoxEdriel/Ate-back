# Fichas de restaurante — Diseño

Fecha: 2026-11-18 · Alcance: `Ate-back` y `Ate` (frontend) · Estado: listo para aprobación

## 1. Intención

El colaborador elige dónde pedir sin saber cómo le fue a otros con ese restaurante. Las **fichas** son
insignias que acompañan al restaurante cuando alguien lo abre para pedir. Se ganan con las calificaciones
de los colaboradores y sirven para **darle confianza** al elegir.

Funcionan como los trofeos del colaborador, pero para restaurantes y con una diferencia: una ficha **se
puede perder**. Solo se pierde por un número claro de quejas recientes en la misma categoría, así que una
mala experiencia suelta no se la quita.

## 2. Objetivos, no objetivos y anti-objetivos

**Objetivos**
- Después de recibir un pedido, el colaborador lo califica en pocos segundos (👍/👎 por categoría).
- El restaurante gana y pierde fichas según reglas fijas y explicables.
- Las fichas se ven en la tarjeta del restaurante y en el panel del menú, al momento de pedir.
- El restaurante ve sus fichas y sus totales de calificaciones.

**No objetivos (v1)**
- Comentarios en texto libre.
- Estrellas o una nota promedio.
- Que el restaurante responda a las calificaciones.
- Notificar al restaurante cuando gana o pierde una ficha.
- Una vista para SuperAdmin o RRHH.

**Anti-objetivos (sería un fracaso aunque funcione)**
- Que una ficha engañe: mostrar «Puntual» a un restaurante que hoy llega tarde de forma sostenida.
- Que el restaurante pueda saber **quién** lo calificó.
- Que calificar sea pesado o insistente: el modal aparece una vez por pedido y se puede omitir.
- Que se pueda calificar un pedido no entregado o calificar dos veces el mismo.

## 3. Categorías y fichas

La ficha es la misma para todos los pedidos; el **texto** de la pregunta cambia según el modo del pedido
(pedido por el usuario: en entrega a domicilio se pregunta si llegó bien; al retirar, si estuvo listo en el
tiempo indicado). Los textos los define el backend.

| Código | Ficha | Entrega a domicilio (`entrega`) | Retiro (`retiro`) | Comer en el local (`local`) |
|---|---|---|---|---|
| `punctual` | **Puntual** | ¿Llegó a la hora esperada? | ¿Estuvo listo en el tiempo que te indicaron? | ¿Estuvo listo en el tiempo que te indicaron? |
| `accurate` | **Pedido exacto** | ¿Recibiste exactamente lo que pediste? | igual | igual |
| `tasty` | **Buen sabor** | ¿La comida estaba buena? | igual | igual |
| `packaging` | **Bien empacado** | ¿Llegó en buen estado y bien empacado? | ¿Te lo entregaron bien empacado? | No se pregunta |

Cada pregunta se puede dejar sin responder. Una respuesta 👎 es una **queja** en esa categoría.

## 4. Reglas de ganar y perder

Por restaurante y por categoría:

Las reglas combinan una cantidad mínima y un porcentaje. Así un restaurante pequeño no gana fichas con
la mitad de quejas, y uno grande no las pierde por unas pocas.

- **Ganar:** el restaurante no tiene la ficha y, contando las respuestas de la categoría desde la última vez
  que la perdió (o desde siempre, si nunca la tuvo), tiene **al menos 10 👍** y **al menos 80 % de 👍**.
- **Mantener:** una vez ganada, se queda.
- **Perder:** contando las respuestas de la categoría de los **últimos 90 días** posteriores a la fecha en que
  la ganó, tiene **al menos 10 👎** y **al menos 20 % de 👎**. Las quejas anteriores a ganarla no cuentan.
- **Recuperar:** después de perderla, se vuelve a contar desde cero con las respuestas posteriores a la
  pérdida.

Ejemplos (categoría «Puntual»):

| Respuestas | Tiene la ficha | Resultado |
|---|---|---|
| 10 👍, 9 👎 | No | No la gana (53 % 👍) |
| 12 👍, 2 👎 | No | **La gana** (86 % 👍) |
| 2.000 👍, 10 👎 en 90 días | Sí | **La mantiene** (0,5 % 👎) |
| 30 👍, 12 👎 en 90 días | Sí | **La pierde** (12 👎, 29 %) |

Los umbrales (10 👍, 80 %, 10 👎, 20 %, 90 días) se guardan por ficha en el catálogo, no en el código:
`earn_min_positive`, `earn_min_ratio`, `lose_min_negative`, `lose_min_ratio`, `lose_window_days`.

## 5. Enfoque

| | Enfoque | A favor | En contra | Decisión |
|---|---|---|---|---|
| **A** | Calificación dentro del pedido (Mongo); al calificar, recalcular al instante las fichas de **ese** restaurante y guardarlas en PostgreSQL | Simple; el cambio se ve enseguida; un pedido tiene como máximo una calificación | Un conteo por cada calificación (pequeño, un solo restaurante) | **Elegido** por el usuario |
| B | Igual, pero recalcular en segundo plano con la cola de los trofeos | Mismo patrón que los trofeos | Cola y worker para un cálculo rápido; retraso de segundos | Descartado |
| — | Calcular las fichas en cada consulta, sin guardarlas | Sin estado | La regla de perder necesita saber si ya la tenía | Descartado |

## 6. Datos

### MongoDB — `employee_orders`

Se agrega al pedido un campo opcional `Rating`:

```
Rating {
  Answers: { "punctual": true, "accurate": true, "tasty": false }   // solo las respondidas
  Skipped: bool        // el colaborador pulsó «Omitir»
  RatedAt: DateTimeOffset
}
```

- Índice nuevo: `(RestaurantId, Rating.RatedAt)` para los conteos.
- No se guarda nada del restaurante en la calificación más allá de lo que el pedido ya tiene.

### PostgreSQL

`restaurant_badge_definitions` (catálogo, con seed de las 4 fichas):
`code PK, name, description, icon, category, earn_min_positive INT, earn_min_ratio NUMERIC(3,2),
lose_min_negative INT, lose_min_ratio NUMERIC(3,2), lose_window_days INT, sort_order, is_active`.

`restaurant_badges` (estado por restaurante):
`restaurant_id, badge_code, (PK compuesta), is_held BOOL, earned_at TIMESTAMPTZ NULL,
lost_at TIMESTAMPTZ NULL, updated_at`.

Se cambian en `database/01_init_schema.sql` con `CREATE TABLE IF NOT EXISTS`, como el resto del esquema.

## 7. Backend

**Calificar** — `POST /api/v1/employee/orders/{id}/rating`, rol `Employee`.
Body: `{ answers: { punctual?: bool, accurate?: bool, tasty?: bool, packaging?: bool }, skipped: bool }`.
- 404 si el pedido no es del usuario.
- 400 si no está `Delivered`, si ya tiene `Rating`, si pasaron más de **7 días** desde que se entregó, si
  `packaging` viene en un pedido `local`, o si no es `skipped` y no trae ninguna respuesta.
- Guarda con un update condicional (`Rating` no existe) para que dos envíos simultáneos no califiquen dos
  veces.
- Si no fue omitido, recalcula las fichas del restaurante (`RestaurantBadgeService.RecalculateAsync`).

**Pendiente de calificar** — `GET /api/v1/employee/orders/pending-rating`, rol `Employee`.
Devuelve el pedido `Delivered` más reciente, sin `Rating` y entregado hace ≤ 7 días, o `null`. Trae lo
necesario para el modal: id, restaurante, platos, `deliveryMode`.

**Restaurantes para el colaborador** — `GET /employee/restaurants` agrega a cada restaurante
`badges: [{ code, name, description, icon }]` con las fichas que tiene (`is_held`), en orden.

**Restaurante (merchant)** — `GET /api/v1/merchant/restaurant/badges`, rol `Merchant`.
Por categoría: si tiene la ficha, desde cuándo, y los 👍/👎 de los últimos 90 días. Sin datos de quién
calificó.

**Recálculo** — una función pura (`RestaurantBadgeCalculator`) recibe la definición, el estado actual y las
calificaciones del restaurante en esa categoría, y devuelve el nuevo estado. El servicio lee de Mongo, llama
a la función por cada ficha activa y guarda en PostgreSQL. Es idempotente: si dos recálculos corren a la vez,
los dos llegan al mismo resultado.

## 8. Frontend

**Modal de calificación** — `features/ratings`, montado una vez en `app.html` junto a
`<app-achievement-notifier />`.
- Solo para el rol `Employee` sin cambio de contraseña pendiente.
- Consulta `pending-rating` al iniciar, al volver a la pestaña y cada 60 s (mismo patrón que los trofeos).
- No se muestra encima del toast de un trofeo: espera a que no haya toast.
- «Omitir» envía `skipped: true`, y ese pedido no vuelve a aparecer.
- Accesible: foco atrapado, cierre con Escape (cuenta como omitir), botones 👍/👎 con `aria-pressed`.

```
┌─ ¿Qué tal tu pedido de El Sabor? ──────┐
│  Almuerzo · Seco de pollo              │
│  Llegó a tiempo          👍   👎       │
│  Llegó lo que pediste    👍   👎       │
│  La comida estaba buena  👍   👎       │
│  Llegó bien empacado     👍   👎       │  ← solo retiro/entrega
│            [Omitir]   [Enviar]         │
└────────────────────────────────────────┘
```

**Fichas al pedir**
- `restaurant-card`: hasta 4 iconos pequeños con tooltip (nombre de la ficha).
- `restaurant-menu-panel`, en el encabezado bajo el nombre: chips con icono y nombre. Al tocarlos se
  explica qué significa (por ejemplo: «Puntual: los colaboradores dicen que sus pedidos llegan a tiempo»).
- Sin fichas no se muestra nada (no se muestra «0 fichas»).

**Restaurante** — en el panel del comercio, una tarjeta «Tus fichas» con cada categoría: ficha sí/no y los
👍/👎 de los últimos 90 días.

## 9. Decisiones tomadas por Claude (revisar)

- Las 4 categorías y sus textos (el usuario solo nombró la puntualidad).
- 👍/👎 en vez de estrellas.
- Los valores 80 % y 20 % de la regla de porcentaje (el usuario aprobó combinar cantidad y porcentaje).
- El conteo de quejas solo en los últimos 90 días.
- Calificaciones anónimas para el restaurante.
- Plazo de 7 días para calificar; «Omitir» es definitivo.
- El modal no se repite por pedidos viejos: solo aparece el más reciente sin calificar.
- La vista del restaurante con sus totales entra en v1.
- Tests: la función pura `RestaurantBadgeCalculator` es la candidata natural; se sigue lo que el usuario
  decida sobre tests en el plan.

## 10. Decisiones del usuario

- Las fichas se pueden perder, pero solo con 10 quejas en la misma categoría.
- Las reglas combinan cantidad y porcentaje (opción A), para no castigar a los restaurantes de mucho volumen.
- Enfoque A: calificación dentro del pedido y recálculo al instante.
