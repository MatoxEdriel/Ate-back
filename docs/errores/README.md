# Errores

Carpeta con la información de los códigos de error de la API de Ate: qué significa cada uno, cuándo aparece
y cómo se crean nuevos. Va junto a [`legal`](../legal/README.md) y [`security`](../security/).

## Índice

| # | Documento | Contenido |
|---|---|---|
| 01 | [Catálogo de códigos](01-catalogo-codigos.md) | Todos los códigos por área, con su HTTP y su mensaje. Incluye los de «ya registrado en otro lado» |

## Forma de una respuesta de error

```json
{
  "success": false,
  "message": "Ya existe una cuenta registrada con este correo.",
  "data": null,
  "errors": [],
  "code": "Employee.EmailAlreadyExists",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "timestamp": "2026-11-19T15:04:05Z"
}
```

| Campo | Para quién | Regla |
|---|---|---|
| `code` | El programa (frontend, integraciones) | Fijo. Es lo único con lo que el frontend decide qué hacer |
| `message` | La persona | En español, claro, sin detalles internos. Se puede cambiar o traducir sin romper nada |
| `errors` | La persona | Detalle por campo en errores de validación |
| `traceId` | Soporte | Identificador de la petición. También va en la cabecera `X-Trace-Id` y en cada línea de log (propiedad `TraceId`) |
| HTTP | Proxies, monitoreo | 400 validación · 401 sin sesión · 403 sin permiso · 404 no existe · 409 conflicto o duplicado · 422 ticket reutilizado · 429 demasiados intentos · 500 inesperado |

Es la misma idea de RFC 9457 («Problem Details»): un código estable separado del texto, más un identificador
para rastrear el caso.

## Reglas para los códigos

1. **Formato `Área.Motivo`** en PascalCase: `Employee.EmailAlreadyExists`, `Payroll.InvalidMonth`.
2. **Nunca cambiar ni reutilizar** un código publicado: el frontend depende de él. Si cambia el significado, se
   crea uno nuevo.
3. **Duplicados** («ya registrado en otro lado»): `Área.<Campo>AlreadyExists`, siempre 409.
4. **Validación de formulario**: `Área.InvalidData` con el detalle de cada campo en `errors`.
5. **No exponer detalles internos** (excepciones, SQL, rutas). El `traceId` lleva al detalle en los logs.

## Cómo agregar un código

1. Definirlo en `Ate.Domain/Errors/<Área>Errors.cs`:

   ```csharp
   public static readonly Error EmailAlreadyExists =
       Error.Conflict("Employee.EmailAlreadyExists", "Ya existe una cuenta registrada con este correo.");
   ```

   El código sale **tal cual se escribe**. `Error.ValidationFor("Entidad", ...)` es la única forma que arma un
   código genérico `Entidad.Validation`.
2. Devolverlo desde el servicio con `Result.Failure(...)`. `ResultExtensions` lo convierte en el HTTP correcto.
3. Agregarlo al [catálogo](01-catalogo-codigos.md).
4. Si el frontend reacciona a él, usar la constante del código, nunca el texto del mensaje.

## Cómo usa soporte el `traceId`

1. El usuario reporta el error; el frontend tiene el `traceId` de la respuesta (o la cabecera `X-Trace-Id`).
2. Se busca en los logs la propiedad `TraceId` con ese valor: aparecen todas las líneas de esa petición,
   incluida la excepción si fue un 500.
