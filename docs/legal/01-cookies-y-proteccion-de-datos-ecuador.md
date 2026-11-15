# 01 · Cookies y protección de datos personales en Ecuador

Fecha de la recopilación: 2026-11-15 · Estado: **borrador, pendiente de validación legal**

## 1. Pregunta de origen

¿Existe una obligación legal de avisar que la aplicación usa cookies? ¿Qué puntos de la Ley de Protección de
Datos Personales de Ecuador debe considerar Ate?

## 2. Marco normativo encontrado

| Norma | Qué es | Fuente |
|---|---|---|
| **Ley Orgánica de Protección de Datos Personales (LOPDP)** | Ley principal. Registro Oficial Suplemento 459, 26 de mayo de 2021. Plenamente exigible desde mayo de 2023 | [Texto de la ley](https://www.consejodecomunicacion.gob.ec/wp-content/uploads/downloads/2021/07/lotaip/Ley%20Org%C3%A1nica%20de%20Protecci%C3%B3n%20de%20Datos%20Personales.pdf) |
| **Reglamento General de la LOPDP** | Decreto Ejecutivo 904, 6 de noviembre de 2023 | [NMS Law](https://nmslaw.com.ec/?p=13184) |
| **Superintendencia de Protección de Datos Personales (SPDP)** | Autoridad de control. Emite resoluciones y oficios con criterios | [spdp.gob.ec](https://spdp.gob.ec/consultas2026/) |
| Oficio **SPDP-IRD-2026-0028-O** | Alcance del deber de información cuando los datos se obtienen del titular | [EY](https://www.ey.com/es_ec/technical/tax/tax-alerts-ecuador/alcance-del-deber-de-informacion), [NMS](https://nmslaw.com.ec/blog/2026/01/26/ecuador-spdp-alcance-deber-informacion-datos-titular/) |
| Resolución SPDP-SPD-2025-0030-R | Seudonimización, anonimización, bloqueo, suspensión y eliminación de datos | [NMS](https://nmslaw.com.ec/blog/2025/08/12/spdp-reglamento-datos-personales-ecuador/) |
| Resolución SPDP-SPD-2025-0006-R | Cláusulas de protección de datos en relaciones contractuales | [NMS](https://nmslaw.com.ec/?p=15177) |

## 3. Lo que está respaldado por las fuentes

- **Deber de información (art. 12 LOPDP).** Cuando los datos se obtienen directamente de la persona, hay que
  informarle **antes, en el momento de la recogida**. Según el oficio SPDP-IRD-2026-0028-O (reseñado por EY y
  NMS), si el tratamiento se basa en consentimiento hay que informar **los 17 contenidos mínimos del art. 12
  que apliquen**, sin omitir ninguno; omitirlos afecta la validez del consentimiento.
- **Consentimiento** libre, específico, informado e inequívoco
  ([DGA Legal](https://www.dgalegal.com/en/noticia-juridica-proteccion-de-datos-personales/)).
- **Obligaciones del responsable y el encargado (art. 47):** un catálogo de quince obligaciones
  ([Derecho Ecuador](https://derechoecuador.com/proteccion-de-datos-personales-en-ecuador-mito-o-realidad/)).
- La LOPDP tiene fuerte influencia del **GDPR europeo**, por lo que los criterios europeos sirven de
  referencia, aunque no son derecho aplicable en Ecuador.

## 4. Lo que NO se pudo confirmar (por confirmar)

- Que la LOPDP o su Reglamento mencionen **cookies** de forma expresa.
- Si Ecuador exige un **banner de cookies** y si el consentimiento debe ser **previo** (*opt-in*) para las
  cookies no esenciales. Un sitio de cumplimiento no oficial afirma que el modelo es opt-in, pero no es fuente
  oficial.
- Si existe **guía de la SPDP sobre cookies o rastreo en línea**.
- Si una cookie de sesión persistente (la de Ate dura 7 o 30 días) cuenta como "estrictamente necesaria".

## 5. Referencia europea (no aplica directamente en Ecuador)

- [Directiva ePrivacy, art. 5(3)](https://www.cookieyes.com/blog/cookie-consent-exemption-for-strictly-necessary-cookies/):
  exige consentimiento previo para guardar o leer información en el dispositivo, salvo lo **estrictamente
  necesario** para un servicio que el usuario pidió.
- [Opinión WP 194 del Grupo de Trabajo del Art. 29](https://Datenschutz.Hessen.de/sites/datenschutz.hessen.de/files/2022-11/wp_194.pdf):
  considera exentas las cookies de sesión y de autenticación. Las de publicidad y analítica de terceros **sí**
  requieren consentimiento. Sobre las cookies persistentes de "mantener sesión" las fuentes discrepan.

## 6. Qué guarda hoy Ate (inventario)

### En el navegador del usuario

| Dato | Dónde | Para qué | Duración | Tipo |
|---|---|---|---|---|
| `ate_rt` | **Cookie** `HttpOnly`, `Secure`, `SameSite=Strict`, ruta `/api/v1/auth` | Mantener la sesión (refresh token) | 30 días colaborador; 7 días RRHH, restaurante y SuperAdmin; se renueva con el uso | Autenticación, propia |
| `ate_user` | `localStorage` | Perfil del usuario (nombre, correo, roles, empresa) para mostrar la interfaz | Hasta cerrar sesión | Funcional, propia |
| `theme` | `localStorage` | Recordar modo claro u oscuro | Indefinida | Preferencia, propia |
| Token de acceso | Memoria de la página | Autorizar peticiones | 15 minutos; se pierde al recargar | Autenticación |

No hay analítica, publicidad ni cookies de terceros propias.

### Servicios de terceros que reciben datos del navegador (IP y cabeceras)

| Servicio | Para qué | Dominio |
|---|---|---|
| Google Fonts | Tipografías | `fonts.googleapis.com`, `fonts.gstatic.com` |
| Esri (ArcGIS) | Mapa de ubicaciones | `server.arcgisonline.com` |
| OpenStreetMap | Mapa incrustado | `www.openstreetmap.org` |
| Google Maps | Mapa incrustado | `maps.google.com` |

### Datos personales que trata el sistema

| Dato | Se guarda en | Quién lo ve |
|---|---|---|
| Cédula, nombres, apellidos, correo, teléfono, foto | PostgreSQL | El propio usuario; RRHH ve a sus colaboradores |
| Pedidos de comida (platos, restaurante, fechas, montos) | MongoDB | El colaborador y el restaurante; **RRHH no** |
| Cortes de nómina (totales y detalle) | MongoDB | El colaborador ve el detalle; RRHH solo el total |
| Dirección IP y tipo de evento (inicio de sesión, cambios de estado, descargas de nómina) | PostgreSQL (`security_events`) y registros del servidor | Equipo técnico |
| Intentos de inicio de sesión | PostgreSQL (`login_attempt_windows`), solo con hash | Nadie (no es legible) |
| Contraseñas | PostgreSQL, solo como hash | Nadie |
| Correo y nombre al enviar notificaciones | Servidor SMTP de Gmail | Proveedor de correo |

## 7. Puntos a considerar de la LOPDP (lista de trabajo)

Cada punto se revisa contra el texto oficial antes de darse por cumplido.

| # | Tema | Pregunta para Ate | Estado |
|---|---|---|---|
| 1 | **Base legitimadora** | ¿Cuál es la base de cada tratamiento: consentimiento, ejecución del contrato con la empresa, obligación legal? ¿El colaborador acepta algo al registrarse o lo registra su empleador? | Por decidir |
| 2 | **Deber de información (art. 12)** | Política de privacidad con los 17 contenidos aplicables, visible **antes** de recoger datos (login, registro) | Pendiente |
| 3 | **Responsable y encargado** | ¿Quién es el responsable del tratamiento: Ate, la empresa cliente, ambos? Define quién firma qué | Por decidir |
| 4 | **Delegado de protección de datos** | ¿Hay obligación de designarlo según el volumen o tipo de datos? | Por confirmar |
| 5 | **Derechos del titular** | Acceso, rectificación, eliminación, oposición, portabilidad, etc. ¿Hay un canal y un procedimiento con plazos? | Pendiente |
| 6 | **Cookies y almacenamiento local** | Aviso de la cookie de sesión y del `localStorage`; evaluar si requiere consentimiento | Por confirmar |
| 7 | **Terceros y transferencias internacionales** | La base de datos de desarrollo está en Supabase (región us-west-2, EE. UU.); correo vía Gmail; mapas y fuentes de Google/Esri. ¿Qué exige la ley para transferir datos fuera del país? | Por confirmar |
| 8 | **Contratos con encargados** | Cláusulas de protección de datos con hosting, correo y proveedores (ver resolución SPDP-SPD-2025-0006-R) | Pendiente |
| 9 | **Seguridad** | Medidas técnicas y organizativas. Ver [plan de refuerzo de seguridad](../security/2026-11-15-plan-refuerzo-seguridad.md) | En curso |
| 10 | **Notificación de brechas** | Procedimiento y plazos para avisar a la SPDP y a los afectados | Pendiente |
| 11 | **Conservación y eliminación** | ¿Cuánto tiempo se guardan pedidos, cortes de nómina, registros de seguridad e IP? ¿Qué pasa cuando un colaborador sale (estado "No vigente")? | Por decidir |
| 12 | **Registro de actividades de tratamiento** | Inventario de tratamientos (el punto 6 de este documento es un primer borrador) | Borrador |
| 13 | **Datos de menores** | ¿Hay colaboradores menores de edad (pasantes)? Regla especial | Por confirmar |
| 14 | **Registro ante la SPDP** | ¿Hay que registrar la base o designar apoderado especial? (hay una guía técnica de la SPDP, resolución SPDP-SPDP-2024-0002-R) | Por confirmar |

## 8. Decisiones tomadas

_Ninguna todavía._

## 9. Próximos pasos

1. Obtener el texto oficial del Reglamento y revisar si menciona cookies y consentimiento previo.
2. Revisar el oficio SPDP-IRD-2026-0028-O completo y la lista de los 17 contenidos del art. 12.
3. Consultar con un abogado de protección de datos de Ecuador: puntos 1, 3, 4, 6, 7, 13 y 14 de la tabla.
4. Redactar el borrador de la política de privacidad y del aviso de cookies a partir de este inventario.
5. Decidir dónde se publican: página pública y enlace en el login.

## 10. Fuentes

- [LOPDP, Registro Oficial Suplemento 459](https://www.consejodecomunicacion.gob.ec/wp-content/uploads/downloads/2021/07/lotaip/Ley%20Org%C3%A1nica%20de%20Protecci%C3%B3n%20de%20Datos%20Personales.pdf)
- [EY: Alcance del deber de información (art. 12)](https://www.ey.com/es_ec/technical/tax/tax-alerts-ecuador/alcance-del-deber-de-informacion)
- [NMS: La SPDP precisa el alcance del deber de información](https://nmslaw.com.ec/blog/2026/01/26/ecuador-spdp-alcance-deber-informacion-datos-titular/)
- [NMS: Reglamento a la LOPDP](https://nmslaw.com.ec/?p=13184)
- [NMS: Seudonimización, anonimización y eliminación](https://nmslaw.com.ec/blog/2025/08/12/spdp-reglamento-datos-personales-ecuador/)
- [NMS: Cláusulas contractuales](https://nmslaw.com.ec/?p=15177)
- [DGA Legal: Protección de datos personales](https://www.dgalegal.com/en/noticia-juridica-proteccion-de-datos-personales/)
- [Derecho Ecuador: ¿Mito o realidad?](https://derechoecuador.com/proteccion-de-datos-personales-en-ecuador-mito-o-realidad/)
- [SPDP: consultas 2026](https://spdp.gob.ec/consultas2026/)
- [CookieYes: cookies estrictamente necesarias](https://www.cookieyes.com/blog/cookie-consent-exemption-for-strictly-necessary-cookies/)
- [Grupo de Trabajo del Art. 29, WP 194](https://Datenschutz.Hessen.de/sites/datenschutz.hessen.de/files/2022-11/wp_194.pdf)
