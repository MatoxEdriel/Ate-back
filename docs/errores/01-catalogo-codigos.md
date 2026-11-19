# 01 · Catálogo de códigos de error

Generado del código del backend (`Ate.Domain/Errors` y servicios) el 2026-11-19 · 74 códigos.

Cada respuesta de error trae `code` (fijo, para el programa), `message` (para la persona), `errors` (detalle por campo, si aplica) y `traceId` (para soporte). Ver las reglas en el [README](README.md).

## Errores de «ya registrado en otro lado» (duplicados)

Ocurren cuando el dato ya pertenece a otra cuenta, empresa o restaurante. Todos responden **409** y el frontend puede marcar el campo exacto.

| Código | Campo afectado | Mensaje |
|---|---|---|
| `Company.AdminDocumentAlreadyExists` | Cédula | Ya existe una persona registrada con la cédula del administrador. |
| `Company.AdminEmailAlreadyExists` | Correo | Ya existe una cuenta registrada con el correo del administrador. |
| `Company.TaxIdAlreadyExists` | RUC | Ya existe una empresa registrada con este RUC. |
| `Department.NameAlreadyExists` | Nombre | Ya existe un departamento con este nombre en tu empresa. |
| `Employee.CodeAlreadyExists` | Código | Ya existe un colaborador con este código en tu empresa. |
| `Employee.DocumentAlreadyExists` | Cédula | Ya existe una persona registrada con esta cédula. |
| `Employee.EmailAlreadyExists` | Correo | Ya existe una cuenta registrada con este correo. |
| `Restaurant.MerchantDocumentAlreadyExists` | Cédula | Ya existe una persona registrada con la cédula del responsable. |
| `Restaurant.MerchantEmailAlreadyExists` | Correo | Ya existe una cuenta registrada con el correo del responsable. |
| `Restaurant.TaxIdAlreadyExists` | RUC | Ya existe un restaurante registrado con este RUC. |

## Inicio de sesión y sesión (`Auth.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `Auth.ChangePassword` | 400 | Revisa los datos ingresados. (detalle de cada campo en errors) |
| `Auth.CompanyInactive` | 403 | Tu empresa está desactivada en Ate. Comunícate con el administrador de la plataforma. |
| `Auth.Forbidden` | 403 | No tienes permiso para realizar esta acción. |
| `Auth.InvalidCredentials` | 401 | Cédula o contraseña incorrecta. |
| `Auth.InvalidCurrentPassword` | 400 | La contraseña actual no es correcta. |
| `Auth.InvalidToken` | 401 | No pudimos verificar tu identidad. Vuelve a iniciar sesión. |
| `Auth.MissingCredentials` | 400 | Ingresa tu cédula y contraseña. |
| `Auth.PasswordChangeRequired` | 403 | Debes cambiar tu contraseña temporal antes de continuar. |
| `Auth.PasswordReused` | 400 | La nueva contraseña debe ser distinta de la temporal. |
| `Auth.TemporaryPasswordExpired` | 401 | Tu contraseña temporal venció. Usa «Olvidé mi contraseña» para crear una nueva. |
| `Auth.TooManyLoginAttempts` | 429 | Demasiados intentos de inicio de sesión. Espera {n} minutos e inténtalo de nuevo. (con cabecera Retry-After) |
| `Auth.UserInactive` | 403 | Tu cuenta está desactivada o bloqueada. Comunícate con un administrador. |
| `Auth.UserNotFound` | 401 | Cédula o contraseña incorrecta. |

## Recuperación de contraseña (`PasswordRecovery.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `PasswordRecovery.CodeExpired` | 400 | El código expiró. Solicita uno nuevo. |
| `PasswordRecovery.InvalidCode` | 400 | El código no es correcto. Revísalo e inténtalo de nuevo. |
| `PasswordRecovery.InvalidResetToken` | 400 | Tu sesión de recuperación no es válida. Solicita un código nuevo. |
| `PasswordRecovery.ResetTokenExpired` | 400 | Tu sesión de recuperación expiró. Solicita un código nuevo. |
| `PasswordRecovery.TooManyAttempts` | 429 | Superaste el número de intentos. Solicita un código nuevo. |
| `PasswordRecovery.Validation` | 400 | Revisa los datos ingresados. (detalle en errors) |

## Empresas (`Company.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `Company.AdminDocumentAlreadyExists` | 409 | Ya existe una persona registrada con la cédula del administrador. |
| `Company.AdminEmailAlreadyExists` | 409 | Ya existe una cuenta registrada con el correo del administrador. |
| `Company.InvalidData` | 400 | Revisa los datos de la empresa. (detalle de cada campo en errors) |
| `Company.InvalidPassword` | 400 | La contraseña es incorrecta. |
| `Company.NotFound` | 404 | No encontramos esta empresa. |
| `Company.PasswordRequired` | 400 | Debes ingresar tu contraseña para desactivar la empresa. |
| `Company.TaxIdAlreadyExists` | 409 | Ya existe una empresa registrada con este RUC. |

## Restaurantes (administración) (`Restaurant.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `Restaurant.InvalidData` | 400 | Revisa los datos del restaurante. (detalle de cada campo en errors) |
| `Restaurant.InvalidPassword` | 400 | La contraseña es incorrecta. |
| `Restaurant.MerchantDocumentAlreadyExists` | 409 | Ya existe una persona registrada con la cédula del responsable. |
| `Restaurant.MerchantEmailAlreadyExists` | 409 | Ya existe una cuenta registrada con el correo del responsable. |
| `Restaurant.NotFound` | 404 | No encontramos este restaurante. |
| `Restaurant.PasswordRequired` | 400 | Debes ingresar tu contraseña para desactivar el restaurante. |
| `Restaurant.TaxIdAlreadyExists` | 409 | Ya existe un restaurante registrado con este RUC. |

## Departamentos (`Department.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `Department.InvalidData` | 400 | Revisa el nombre del departamento. (detalle en errors) |
| `Department.NameAlreadyExists` | 409 | Ya existe un departamento con este nombre en tu empresa. |
| `Department.NotFound` | 404 | No encontramos este departamento en tu empresa. |

## Colaboradores (`Employee.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `Employee.CodeAlreadyExists` | 409 | Ya existe un colaborador con este código en tu empresa. |
| `Employee.CompanyNotAssigned` | 403 | Tu cuenta no está vinculada a ninguna empresa. |
| `Employee.DocumentAlreadyExists` | 409 | Ya existe una persona registrada con esta cédula. |
| `Employee.EmailAlreadyExists` | 409 | Ya existe una cuenta registrada con este correo. |
| `Employee.InvalidData` | 400 | Revisa los datos del colaborador. (detalle en errors) |
| `Employee.NotCurrent` | 400 | Este colaborador ya no pertenece a la empresa y no se puede reactivar desde aquí. |
| `Employee.NotFound` | 404 | No encontramos a este colaborador en tu empresa. |

## Pedidos y calificaciones del colaborador (`EmployeeOrder.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `EmployeeOrder.AlreadyOrdered` | 409 | Ya tienes un pedido en curso para esta comida. Espera a que te lo entreguen o cancélalo si quieres cambiarlo. |
| `EmployeeOrder.AlreadyRated` | 409 | Ya calificaste este pedido. |
| `EmployeeOrder.CannotCancel` | 409 | Este pedido ya no se puede cancelar. |
| `EmployeeOrder.DeliveryAddressMissing` | 400 | Tu empresa no tiene una dirección registrada para entregas. Elige comer en el local o retirarlo para llevar. |
| `EmployeeOrder.DishNotAvailable` | 404 | Este plato ya no está disponible para este horario. |
| `EmployeeOrder.EmployeeNotFound` | 403 | Tu cuenta no está registrada como colaborador activo. |
| `EmployeeOrder.InvalidData` | 400 | Revisa los datos del pedido. (detalle en errors) |
| `EmployeeOrder.InvalidRating` | 400 | Responde al menos una pregunta o elige omitir. |
| `EmployeeOrder.NotFound` | 404 | No encontramos este pedido. |
| `EmployeeOrder.NotRatable` | 400 | Solo puedes calificar pedidos entregados en los últimos 7 días. |
| `EmployeeOrder.OrderingClosed` | 400 | Ya pasó la hora de corte para pedir en este restaurante. |
| `EmployeeOrder.RestaurantNotAvailable` | 404 | Este restaurante no tiene convenio activo con tu empresa. |

## Pedidos del restaurante (`MerchantOrder.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `MerchantOrder.InvalidDate` | 400 | La fecha debe tener el formato yyyy-MM-dd. |
| `MerchantOrder.InvalidStatus` | 400 | El estado debe ser Confirmed, Ready, Delivered o Cancelled. |
| `MerchantOrder.InvalidTransition` | 409 | Este pedido ya no puede pasar a ese estado. Actualiza la lista. |
| `MerchantOrder.RestaurantNotFound` | 403 | Tu cuenta no tiene un restaurante activo asociado. |

## Perfil del restaurante (`MerchantRestaurant.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `MerchantRestaurant.InvalidData` | 400 | Revisa los datos del restaurante. (detalle en errors) |
| `MerchantRestaurant.InvalidPassword` | 400 | La contraseña no es correcta. |
| `MerchantRestaurant.PasswordRequired` | 400 | Ingresa tu contraseña para guardar los datos del local. |

## Nómina (`Payroll.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `Payroll.EmployeeNotFound` | 403 | Tu cuenta no está registrada como colaborador activo. |
| `Payroll.InvalidMonth` | 400 | El mes debe tener el formato AAAA-MM. |
| `Payroll.NotFound` | 404 | No encontramos este corte. |

## Perfil (`Profile.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `Profile.NotFound` | 404 | No encontramos la información de tu perfil. |

## Archivos (`File.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `File.InvalidImage` | 400 | La imagen debe ser un archivo PNG, JPG o WebP válido. |

## Idempotencia (ticket Idempotency-Key) (`Idempotency.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `Idempotency.InProgress` | 409 | Tu solicitud anterior todavía se está procesando. Espera un momento. |
| `Idempotency.InvalidKey` | 400 | La clave de la operación no es válida. |
| `Idempotency.KeyReused` | 422 | Esta clave ya se usó para otra operación. |

## Peticiones (`Request.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `Request.InvalidBody` | 400 | No pudimos leer los datos enviados. Revisa el formulario e inténtalo de nuevo. (en `errors`, los campos afectados) |

## Límite de peticiones (`RateLimit.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `RateLimit.Exceeded` | 429 | Demasiados intentos. Espera {n} segundos e inténtalo de nuevo. (con cabecera Retry-After) |

## Endpoints (`Endpoint.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `Endpoint.NotFound` | 404 | No encontramos lo que buscabas. (endpoints de desarrollo fuera de Development) |

## Errores del servidor (`Server.*`)

| Código | HTTP | Mensaje al usuario |
|---|---|---|
| `Server.UnexpectedError` | 500 | Ocurrió un error inesperado. Intenta de nuevo más tarde. (HTTP 500) |
