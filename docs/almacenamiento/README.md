# Almacenamiento de archivos

Cómo guarda Ate sus archivos (logos hoy; documentos, imágenes y PDFs después) y cómo se agrega un proveedor
nuevo: Azure Blob Storage, Amazon S3 o SFTP.

## Idea: un puerto y un adaptador por proveedor (patrón Adapter)

La aplicación solo conoce la interfaz `IFileStorage` (`Ate.Application/Common/Storage/IFileStorage.cs`). Cada
proveedor es un adaptador que la implementa y se elige en la configuración, sin tocar el resto del código:

```
                    ┌──────────────────────┐
 servicios ───────▶ │     IFileStorage     │  (puerto: lo único que conoce la aplicación)
 (LogoFiles, …)     └──────────┬───────────┘
                               │ Storage:Provider
        ┌──────────────┬───────┴───────┬──────────────┐
        ▼              ▼               ▼              ▼
 LocalFileStorage  AzureBlobStorage   S3Storage    SftpStorage
   (hecho)          (pendiente)      (pendiente)   (pendiente)
```

## El contrato

| Método | Qué hace | Notas para el adaptador |
|---|---|---|
| `SaveAsync(area, key, stream, contentType)` | Guarda o reemplaza | Guardar el `contentType` como metadato del archivo |
| `OpenReadAsync(area, key)` | Abre para leer; `null` si no existe | Devolver un stream que el llamador cierra |
| `DeleteAsync(area, key)` | Borra; no falla si no existe | Idempotente |
| `ExistsAsync(area, key)` | ¿Existe? | |
| `GetPublicUrl(key)` | URL pública de un archivo del área `Public` | Sin llamadas de red: solo arma la URL |

**Áreas (`FileArea`)**

| Área | Para qué | Cómo se entrega |
|---|---|---|
| `Public` | Logos e imágenes que cualquiera puede ver | URL directa (`GetPublicUrl`), con caché larga |
| `Private` | Documentos: nómina, contratos, comprobantes | Nunca URL directa: por la API con permisos, o enlace temporal (SAS / URL prefirmada) |

**Claves (`StorageKeys`)**: rutas en minúsculas con `a-z 0-9 - _ . /`, sin `..` ni segmentos vacíos. El nombre
del archivo lo genera el sistema (un GUID), nunca el nombre que trae el usuario. La misma clave sirve en todos
los proveedores. En la base de datos **se guarda solo la clave**, no la URL: así se cambia de proveedor sin
migrar datos.

Ejemplos:

```
logos/restaurants/{restaurantId}/{guid}.webp       (Public)
logos/companies/{companyId}/{guid}.png             (Public)
documents/payroll/{companyId}/{periodo}/{guid}.pdf (Private, futuro)
```

## Configuración

```json
"Storage": {
  "Provider": "Local",
  "PublicBaseUrl": "http://localhost:5252/files",
  "Local": { "RootPath": "storage" }
}
```

| Clave | Para qué |
|---|---|
| `Storage:Provider` | `Local` · `AzureBlob` · `S3` · `Sftp` |
| `Storage:PublicBaseUrl` | Base de las URL públicas. Local: `{api}/files`. Nube: el contenedor o la CDN |
| `Storage:Local:RootPath` | Carpeta raíz del adaptador local (relativa a la API o absoluta) |

En producción se configuran con variables de entorno (`Storage__Provider`, `Storage__PublicBaseUrl`, …); ver
`docker-compose.prod.yml`. Las credenciales de la nube van en el gestor de secretos, **nunca** en appsettings.

## Adaptador local (el que hay hoy)

- `Ate.Infrastructure/Storage/LocalFileStorage.cs`. Cada área es una subcarpeta (`public/`, `private/`).
- La API sirve el área pública en `GET /files/{key}` (`FilesController`) con caché de un año.
- Escribe a un temporal y luego mueve, para que nadie lea un archivo a medias.
- **Límites:** sirve para desarrollo o un solo servidor con un volumen persistente. No sirve con varias
  instancias. Con una base de datos compartida (Supabase de desarrollo) cada desarrollador tiene sus propios
  archivos: un logo subido en una máquina no se ve en otra. Para un equipo, usar un adaptador de nube.

## Cómo agregar un adaptador

1. **Paquete** en `Ate.Infrastructure`:

   | Proveedor | Paquete | Notas |
   |---|---|---|
   | Azure Blob Storage | `Azure.Storage.Blobs` | Un contenedor por área (`public` con acceso de lectura anónimo o CDN; `private` sin acceso público) |
   | Amazon S3 | `AWSSDK.S3` | Un bucket por área, o prefijos `public/` y `private/` con políticas distintas |
   | SFTP | `SSH.NET` | **SFTP, no FTP**: el FTP simple envía contraseñas y archivos sin cifrar |

2. **Opciones**: agregar su sección a `StorageOptions` (por ejemplo `AzureBlob: { ConnectionString, PublicContainer, PrivateContainer }`).
3. **Clase** `Ate.Infrastructure/Storage/<Proveedor>FileStorage.cs` que implemente `IFileStorage`:
   - validar la clave con `StorageKeys.IsValid` antes de usarla;
   - `GetPublicUrl` arma `{PublicBaseUrl}/{key}` sin llamadas de red;
   - `DeleteAsync` no falla si el archivo no existe;
   - guardar el `contentType` (Azure: `BlobHttpHeaders.ContentType`; S3: `ContentType` del `PutObjectRequest`).
4. **Registrarlo** en `StorageServiceCollectionExtensions.AddFileStorage`: un `case` nuevo con el nombre de
   `StorageProviders`.
5. **Probarlo**: subir un logo desde el panel del restaurante y ver que se muestra; quitarlo y ver que el archivo
   se borra.

Para documentos privados se agregará un método `GetTemporaryUrlAsync(key, expiresIn)` al contrato (SAS de Azure,
URL prefirmada de S3; en local y SFTP, un endpoint de la API con permisos). Se agrega cuando exista el primer
documento privado, no antes.

## Buenas prácticas que ya se aplican

- Tipo real del archivo comprobado por sus bytes (PNG, JPEG, WebP; nunca SVG porque puede llevar scripts), no
  por el nombre ni por lo que dice el navegador (`ImageDataUrl`).
- Límite de tamaño en la validación de la petición.
- Nombres generados y únicos: el contenido de una clave nunca cambia, por eso la caché puede ser de un año.
- Al reemplazar un logo, el anterior se borra **después** de guardar la base, nunca antes. Si falla la base, se
  borra el archivo recién subido.
- Lo que ya estaba en la base sigue funcionando: los logos antiguos en base64 y las rutas estáticas
  (`/assets/...`) se devuelven tal cual (`LogoFiles.ToPublicUrl`).

## Pendiente

- Migrar los logos antiguos (base64 en la base) al almacenamiento cuando haya un proveedor compartido (nube).
  Con el adaptador local y una base compartida, migrarlos dejaría los archivos solo en una máquina.
- Volver la columna `logo_url` a `VARCHAR(500)` cuando ya no queden logos en base64.
