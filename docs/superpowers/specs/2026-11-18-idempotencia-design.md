# Idempotencia de acciones (patrón Idempotency-Key) — Diseño corto

Fecha: 2026-11-18 · Alcance: `Ate-back` y `Ate` (frontend) · Estado: aprobado por el usuario («ok prosigue»)

## Intención

Evitar el envío doble al **crear pedidos** y al **calificar** (doble clic, red lenta, reintentos), con un
mecanismo **reutilizable** basado en un token. Hoy ya no se duplican datos (un pedido activo por comida;
una calificación por pedido), pero el reintento recibe un error. Con idempotencia, el reintento recibe **la
misma respuesta** que el primer envío.

## Enfoques

| | Enfoque | Decisión |
|---|---|---|
| A | Claves en **MongoDB**: índice único y TTL de 24 h que las borra solo | **Elegido** (recomendado; sin tarea de limpieza; pedidos y calificaciones viven en Mongo) |
| B | Claves en PostgreSQL con clave única y limpieza periódica | Descartado: necesita una tarea programada |
| — | Caché en memoria | Descartado: no sirve con varias instancias ni sobrevive a reinicios |

## Diseño

**Contrato HTTP** (estándar de facto, borrador IETF `Idempotency-Key`):
- El cliente envía la cabecera `Idempotency-Key: <uuid>` en las peticiones protegidas.
- La clave es por usuario: `{userId}:{key}`. Dura 24 h.
- Sin cabecera, la petición se procesa como hoy (compatibilidad con clientes viejos).
- Clave inválida (vacía o > 100 caracteres) → 400.

| Caso con la misma clave | Respuesta |
|---|---|
| Primera vez | Se ejecuta y se guarda el código y el cuerpo de la respuesta |
| La primera ya terminó (2xx o 4xx) | La misma respuesta guardada, con cabecera `Idempotent-Replayed: true` |
| La primera sigue en curso | 409 `Idempotency.InProgress` |
| Mismo usuario y clave, contenido distinto | 422 `Idempotency.KeyReused` |
| La primera falló con 5xx o excepción | La clave se libera: el reintento se ejecuta de nuevo |

**Backend reutilizable:** atributo `[Idempotent]` (filtro de acción) que se pone en cualquier endpoint. La
decisión (ejecutar, repetir, en curso, contenido distinto) es una función pura con tests. La huella del
contenido es SHA-256 de método + ruta + argumentos de la acción.

**Frontend reutilizable:**
- `IdempotencyKeys` (servicio en `core`): `keyFor(intent)` da la misma clave mientras la misma intención se
  reintenta; `release(intent)` la descarta cuando hay una respuesta definitiva (éxito o 4xx).
- Un interceptor agrega la cabecera cuando la petición trae la clave en su `HttpContext`
  (`withIdempotencyKey(key)`).
- Intenciones: crear pedido = restaurante + comida + plato + opciones; calificar = id del pedido.

**Endpoints protegidos en v1:** `POST /employee/orders` y `POST /employee/orders/{id}/rating`.

## No objetivos

- Proteger los demás endpoints (el atributo queda listo para agregarlos).
- Reintentos automáticos en el frontend.
