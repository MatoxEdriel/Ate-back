# Resumen de cambios — 14 de noviembre de 2026

Cambios en backend (`Ate-back`) y frontend (`Ate`), publicados en `dev` y `main`.

## 1. Desactivar restaurantes con contraseña

Desactivar un restaurante ahora funciona igual que desactivar una empresa.

- **Backend:** `PATCH admin/restaurants/{id}/status` exige `currentPassword` al desactivar
  y la verifica contra el usuario logueado. Si falta o es incorrecta responde 400.
  Activar no pide contraseña.
- **Frontend:** modal con campo de contraseña (mostrar/ocultar), error dentro del modal,
  estado de carga y cierre con Escape. El botón "Desactivar" es rojo, como en empresas.

## 2. Títulos de pestaña

- Rol colaborador: la pestaña dice **"Menú | Ate"** (antes "Alimentación | Ate").
- Restaurante: se quitó el título fijo "Comercio Aliado | Ate"; la pestaña muestra el
  **nombre del restaurante**.

## 3. Perfil: datos de la empresa según el rol

- El **colaborador** ya no recibe el RUC, correo, teléfono ni dirección de la empresa. Se
  filtra en el backend, así que esos datos tampoco viajan por la red. Solo ve el nombre,
  el logo y el estado de su empresa.
- El **admin de empresa** sigue viendo todos los datos.
- Nueva sección **"Tu puesto"**: área, tipo (pasante o regular) y código de colaborador.
- Se rediseñó la tarjeta de empresa del perfil.

## 4. Seguridad: cuentas desactivadas pierden el acceso

**Revisión de endpoints:**

- Todos los endpoints exigen el rol correcto.
- Cada rol solo accede a sus propios datos: la identidad sale del token y los servicios
  filtran por empresa, colaborador o restaurante.
- Login y recuperación de contraseña siguen públicos.
- Los endpoints de desarrollo responden 404 fuera de Development.

**Problemas corregidos:**

- Una cuenta desactivada seguía usando la API hasta 8 horas, que es lo que dura el token.
  Ahora, en cada petición, `UserAccessValidator` (enganchado en `OnTokenValidated`)
  verifica que el usuario, su ficha de colaborador y su empresa sigan activos. Si no,
  responde 401 y el frontend cierra la sesión. El resultado se reutiliza 30 segundos.
- Un colaborador desactivado por RRHH **podía seguir iniciando sesión**: RRHH marcaba la
  ficha de colaborador y el login solo miraba el usuario. El login ahora usa la misma
  validación.

Probado contra la API local: roles, token alterado, colaborador desactivado con sesión
abierta, login de un desactivado y reactivación.

## 5. Estados del colaborador

Nuevo enum `EmployeeStatus`, que reemplaza al booleano `is_active` de `employees`:

| Estado | Significado |
|---|---|
| `Active` — Activo | Puede ingresar y pedir. |
| `Inactive` — Inactivo | Suspendido por RRHH; se puede reactivar. |
| `NotCurrent` — No vigente | Ya no pertenece a la empresa. No se reactiva desde el panel. |

- Se guarda como texto (`status`), y la base rechaza cualquier otro valor
  (`ck_employees_status`).
- `01_init_schema.sql` migra las bases existentes una sola vez (`true` → `Active`,
  `false` → `Inactive`) y elimina `is_active`. **Fuera de Development hay que ejecutar el
  script a mano al desplegar.**
- La API devuelve `status` y conserva `isActive` por compatibilidad.
- Frontend: etiqueta "No vigente", filtro por estado y panel sin botón de reactivar.
- El paso a "No vigente" se hará en el flujo de salida (pendiente). El caso No vigente no
  se probó contra la base.

## 6. Diseño del cierre de nómina (pendiente de aprobar)

En `docs/superpowers/specs/2026-11-14-cierre-nomina-design.md`. En resumen:

- Cortes por colaborador de tipo `Monthly` (día 1, se descuenta del sueldo) y
  `Termination` (al marcar No vigente, se descuenta de la liquidación).
- Cortes congelados en MongoDB, con un índice único para no cobrar dos veces.
- RRHH solo ve el total a descontar por persona (PDF) y el uso agregado de toda la
  empresa. El colaborador ve su detalle.
- Se eliminará el endpoint con el que RRHH ve los pedidos de cada colaborador.

## Pendiente

- Aprobar el diseño del cierre de nómina y planificar su implementación.
- Flujo de salida del colaborador (marcar No vigente + fecha de salida).
- Eliminar `GET company/employees/{id}/orders`.
- Limpiar el campo `Employee.MonthlyAllowance`, que no se usa.
- Carpeta `Library/` en el repo del back: basura de macOS, sin commitear. Borrarla o
  ignorarla.
