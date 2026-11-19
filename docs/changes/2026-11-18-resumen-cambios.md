# Resumen de cambios — 18 de noviembre de 2026

Cambios en backend (`Ate-back`), publicados en `dev` y `main`. Solo documentación, sin cambios de código.

## 1. Documento legal de cookies: razón de la cookie de sesión

En `docs/legal/01-cookies-y-proteccion-de-datos-ecuador.md`, sección 6:

- **Por qué la sesión usa una cookie.** Antes, los tokens estaban en `localStorage`, y un XSS podía llevarse
  el refresh token. Ahora el token de acceso vive en memoria (15 min) y el refresh token en la cookie `ate_rt`
  (`HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/v1/auth`), rotado y guardado con hash. También se
  explica qué no resuelve (un XSS con la pestaña abierta) y la condición de despliegue: frontend y API en el
  mismo sitio.
- **¿Hace falta el aviso «Aceptar / Configurar cookies»?** Recomendación técnica, pendiente de validar con un
  abogado: hoy no hace falta un banner de consentimiento, porque Ate solo usa una cookie de autenticación,
  necesaria para el servicio, y no hay analítica ni publicidad. Sí hace falta informar, con una sección en la
  política de privacidad y una línea en el login.
- **Puntos grises:** el iframe de Google Maps en `location-picker` puede poner cookies de Google; la duración
  de 30 días de la sesión; y, si se agrega analítica o publicidad, el banner pasa a ser obligatorio.
- El punto 6 de la lista de la LOPDP remite a esta sección.

## 2. Changelog

- Este archivo.
