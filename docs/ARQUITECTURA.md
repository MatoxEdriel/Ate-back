# Ate — Arquitectura del Backend (.NET 10 + PostgreSQL 16)

> Documento generado a partir del análisis real del proyecto Ate.
> Última revisión: 2026-10-01.

---

## 1. Mapa del proyecto

```
d:\Ate\backend\
├── Ate.slnx                              ← Archivo de solución .NET 10 (formato SLNX, más ligero que .sln)
│
├── Ate.Domain/                           ← Capa de DOMINIO — entidades puras, sin dependencias externas
│   └── Ate.Domain.csproj                    0 ProjectReference, 0 PackageReference
│
├── Ate.Application/                      ← Capa de APLICACIÓN — interfaces, DTOs, casos de uso
│   └── Ate.Application.csproj               → referencia Ate.Domain
│
├── Ate.Infrastructure/                   ← Capa de INFRAESTRUCTURA — EF Core, repositorios, servicios externos
│   └── Ate.Infrastructure.csproj            → referencia Ate.Application + Ate.Domain
│                                            → paquete Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3
│
├── Ate.Api/                              ← Capa de PRESENTACIÓN — Web API, controllers, middlewares
│   ├── Ate.Api.csproj                       → referencia Ate.Application + Ate.Infrastructure
│   │                                        → paquetes: Microsoft.AspNetCore.OpenApi 10.0.2
│   │                                                     Microsoft.EntityFrameworkCore.Design 10.0.12
│   ├── Program.cs                           Punto de entrada de la aplicación
│   ├── appsettings.json                     Configuración base (ConnectionStrings, JwtSettings)
│   ├── appsettings.Development.json         Override para entorno local
│   ├── Properties/
│   │   └── launchSettings.json              Puerto local: http://localhost:5252
│   └── Ate.Api.http                         Archivo de pruebas HTTP (endpoint /weatherforecast)
│
├── database/                             ← Scripts SQL manuales (DDL + Seed)
│   ├── 01_init_schema.sql                   Esquema completo: 6 tablas, índices, restricciones
│   ├── 02_seed_data.sql                     Datos de prueba para desarrollo
│   └── README.md                            Guía de uso de los scripts
│
├── docs/                                 ← Documentación del proyecto
│   ├── architecture.md                      Visión general de capas y referencias
│   ├── configuration.md                     Guía de secretos y variables de entorno
│   └── ARQUITECTURA.md                      ← Este documento
│
├── docker-compose.yml                    ← Contenedor de PostgreSQL 16 con auto-seed
└── .gitignore                            ← Excluye bin/, obj/, secretos, node_modules, etc.
```

---

## 2. Visión general: ¿Qué es Clean Architecture?

Clean Architecture es un patrón de diseño que organiza el código en **capas concéntricas** donde las dependencias solo apuntan **hacia adentro** (de lo externo a lo interno). El objetivo es que la lógica de negocio (Domain) no sepa ni le importe si usas PostgreSQL, SQL Server, una API externa o un archivo de texto.

### Diagrama de dependencias de Ate (basado en los `ProjectReference` reales de los `.csproj`)

```
    ┌──────────────────────────────────────────┐
    │              Ate.Api (webapi)             │   ← Capa más EXTERNA
    │  refs: Ate.Application, Ate.Infrastructure│
    └───────────┬──────────────┬───────────────┘
                │              │
                ▼              ▼
    ┌───────────────┐  ┌──────────────────────────┐
    │Ate.Application│  │  Ate.Infrastructure       │
    │  refs: Domain │  │  refs: Application, Domain│
    └───────┬───────┘  └──────────┬────────────────┘
            │                     │
            ▼                     ▼
    ┌─────────────────────────────────────────┐
    │           Ate.Domain (classlib)          │   ← Capa más INTERNA
    │           refs: NINGUNA                  │
    └─────────────────────────────────────────┘
```

### ¿Por qué Domain no depende de nada?

Porque Domain contiene las **reglas de negocio puras**. Si Domain dependiera de EF Core o de ASP.NET, cualquier cambio en esas librerías podría romper tu lógica de negocio. Al mantenerlo aislado:

- Puedes cambiar de PostgreSQL a SQL Server **sin tocar Domain**.
- Puedes reemplazar la Web API por una aplicación de consola **sin tocar Domain**.
- Puedes escribir tests unitarios de la lógica de negocio **sin necesitar una base de datos real**.

---

## 3. Cada capa en detalle

### 3.1 Ate.Domain

**¿Qué contiene actualmente?** Solo el `.csproj` vacío. No hay entidades, enums ni value objects todavía.

**¿Qué debe ir aquí?**
- **Entidades**: las clases C# que representan los conceptos del negocio (`Empresa`, `Empleado`, `TipoBeneficio`, `SaldoEmpleado`, `ComercioAfiliado`, `Transaccion`), basadas en las tablas definidas en `01_init_schema.sql`.
- **Enums**: por ejemplo, `EstadoEmpresa { Activo, Inactivo, Suspendido }` en lugar de strings sueltos.
- **Value Objects**: tipos que se comparan por valor, como `Dinero(decimal Monto, string Moneda)`.
- **Excepciones de dominio**: como `SaldoInsuficienteException`.
- **Interfaces de repositorio** (opcionalmente; algunos equipos las ponen en Application).

**¿Qué NO debe ir aquí?**
- Nada que tenga `using Microsoft.EntityFrameworkCore`.
- Nada que dependa de ASP.NET, HttpClient, JWT o cualquier librería externa.
- DTOs, validaciones de request/response, lógica de infraestructura.

**Paquetes NuGet:** Ninguno. Cero dependencias externas. Eso es intencional.

```xml
<!-- Ate.Domain.csproj — limpio, sin dependencias -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

---

### 3.2 Ate.Application

**¿Qué contiene actualmente?** Solo el `.csproj` con una referencia a `Ate.Domain`.

**¿Qué debe ir aquí?**
- **Interfaces** que define Application y que Infrastructure implementa. Por ejemplo:
  ```csharp
  // Ate.Application/Contracts/IApplicationDbContext.cs
  public interface IApplicationDbContext
  {
      DbSet<Empresa> Empresas { get; }
      DbSet<Empleado> Empleados { get; }
      Task<int> SaveChangesAsync(CancellationToken ct = default);
  }
  ```
- **DTOs** (Data Transfer Objects): las formas en que los datos entran y salen de la API, sin exponer las entidades internas.
- **Servicios de caso de uso** (o Handlers si usas CQRS/MediatR).
- **Validaciones** de entrada (con FluentValidation, por ejemplo).
- **Mapeos** entre entidades y DTOs.

**¿Qué NO debe ir aquí?**
- Implementaciones concretas de acceso a datos (nada de `DbContext`, `SqlConnection`, etc.).
- Clases que dependan de `HttpContext`, controllers, middlewares.

**Paquetes NuGet:** Ninguno instalado actualmente. En el futuro podrías agregar `FluentValidation` o `MediatR` aquí.

---

### 3.3 Ate.Infrastructure

**¿Qué contiene actualmente?** Solo el `.csproj` con las referencias y un paquete NuGet.

**¿Qué debe ir aquí?**
- **`AteDbContext`**: la clase que hereda de `DbContext` de EF Core. Es el puente entre tus entidades C# y PostgreSQL.
- **Configurations**: clases `IEntityTypeConfiguration<T>` que definen cómo cada entidad se mapea a su tabla (columnas, tipos, índices, restricciones).
- **Migrations**: la carpeta generada por `dotnet ef migrations add`.
- **Repositorios**: implementaciones concretas de las interfaces definidas en Application.
- **Servicios externos**: JWT provider, generador de QR, clientes HTTP, etc.
- **`DependencyInjection.cs`**: método de extensión para registrar todos los servicios de esta capa en el contenedor de IoC.

**¿Qué NO debe ir aquí?**
- Lógica de negocio (eso va en Domain o Application).
- Controllers, middlewares o configuración de ASP.NET (eso va en Api).

**Paquetes NuGet instalados:**

| Paquete | Versión | ¿Para qué? |
|:---|:---|:---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.3 | El **proveedor** que le dice a EF Core cómo hablar con PostgreSQL. Sin esto, EF Core no sabe generar SQL de Postgres. Instala automáticamente `Npgsql` (el driver ADO.NET puro) y `Microsoft.EntityFrameworkCore` como dependencias transitivas. |

---

### 3.4 Ate.Api

**¿Qué contiene actualmente?** El `Program.cs` con la plantilla por defecto de .NET 10 (`/weatherforecast`), los archivos de configuración y un archivo `.http` de pruebas.

**¿Qué debe ir aquí?**
- **Program.cs**: registro de servicios (DI), middlewares, mapeo de rutas.
- **Controllers** (o Minimal APIs): reciben HTTP requests, delegan a Application y devuelven responses.
- **Middlewares**: manejo global de excepciones, CORS, autenticación JWT.
- **Filtros y configuración de Swagger/OpenAPI**.
- **`DependencyInjection.cs`** para registrar servicios propios de la capa API.

**¿Qué NO debe ir aquí?**
- Acceso directo a la base de datos (nada de `DbContext` en un controller).
- Lógica de negocio compleja.

**Paquetes NuGet instalados:**

| Paquete | Versión | ¿Para qué? |
|:---|:---|:---|
| `Microsoft.AspNetCore.OpenApi` | 10.0.2 | Genera la documentación OpenAPI/Swagger de tus endpoints. |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.12 | Herramientas de diseño que el CLI `dotnet ef` necesita para generar migraciones. Se marca con `<PrivateAssets>all</PrivateAssets>` para que **no se publique** en el binario final — solo se usa en desarrollo. |

> **¿Por qué `EF.Design` va en Api y no en Infrastructure?**
> Porque `dotnet ef` necesita un **startup project** que sea una aplicación ejecutable (un `webapi` o `console`). `Ate.Infrastructure` es un `classlib`, no se puede ejecutar. EF necesita ejecutar tu `Program.cs` para descubrir el `DbContext` y la connection string.

---

## 4. Cómo arranca la app: Program.cs línea por línea

Tu `Program.cs` actual es la plantilla por defecto de .NET 10:

```csharp
// 1. Crear el builder — aquí se registran TODOS los servicios antes de construir la app
var builder = WebApplication.CreateBuilder(args);

// 2. Registrar el servicio de OpenAPI (Swagger)
builder.Services.AddOpenApi();

// 3. Construir la app — a partir de aquí ya no puedes registrar servicios
var app = builder.Build();

// 4. Solo en Development: habilitar el endpoint /openapi/v1.json
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 5. Endpoint de prueba /weatherforecast (plantilla por defecto, se eliminará)
var summaries = new[] { "Freezing", "Bracing", /* ... */ };
app.MapGet("/weatherforecast", () => { /* ... */ });

// 6. Arrancar el servidor HTTP en http://localhost:5252
app.Run();
```

### ¿Qué falta agregar? (en orden)

Cuando implementes las capas, tu `Program.cs` se verá algo así:

```csharp
var builder = WebApplication.CreateBuilder(args);

// ── Registro de servicios por capa ──
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration); // ← registra AteDbContext + repositorios
builder.Services.AddApplication();                         // ← registra servicios de caso de uso
builder.Services.AddControllers();                         // ← o seguir con Minimal APIs

var app = builder.Build();

// ── Migración automática al arrancar (solo en Development) ──
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AteDbContext>();
    await db.Database.MigrateAsync(); // Aplica migraciones pendientes
    // Aquí podrías llamar a un Seeder también
}

// ── Pipeline de middlewares ──
app.UseExceptionHandler("/error");  // manejo global de errores
app.UseAuthentication();            // JWT
app.UseAuthorization();
app.MapOpenApi();
app.MapControllers();

app.Run();
```

El método `AddInfrastructure` viviría en `Ate.Infrastructure/DependencyInjection.cs`:

```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AteDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        // Registrar repositorios, JWT provider, etc.
        return services;
    }
}
```

---

## 5. Recorrido de una petición

Desde que el cliente (frontend Angular o Postman) hace un HTTP request hasta que vuelve el response:

```
 Cliente (Angular / Postman)
    │
    │  POST http://localhost:5252/api/transacciones
    │  Body: { empleadoId: "...", comercioId: "...", monto: 45.50 }
    ▼
┌────────────────────────────────────────────┐
│  Ate.Api                                    │
│  TransaccionesController.Crear(dto)         │
│  → Valida el request (Model Binding)        │
│  → Llama al servicio de Application         │
└──────────────────┬─────────────────────────┘
                   │
                   ▼
┌────────────────────────────────────────────┐
│  Ate.Application                            │
│  CrearTransaccionService.Handle(dto)         │
│  → Valida reglas de negocio                 │
│  → Verifica saldo suficiente               │
│  → Llama a la interfaz IApplicationDbContext│
└──────────────────┬─────────────────────────┘
                   │
                   ▼
┌────────────────────────────────────────────┐
│  Ate.Infrastructure                         │
│  AteDbContext (implementa la interfaz)      │
│  → EF Core traduce la operación C# a SQL   │
│  → Npgsql envía el SQL a PostgreSQL         │
└──────────────────┬─────────────────────────┘
                   │
                   ▼
┌────────────────────────────────────────────┐
│  PostgreSQL 16 (ate_db)                     │
│  INSERT INTO transacciones (...)            │
│  UPDATE saldos_empleado SET monto_consumido │
│  → Devuelve filas afectadas                 │
└──────────────────┬─────────────────────────┘
                   │
                   ▼  (todo el camino de vuelta)
    ← 201 Created { id: "e0000...", estado: "Aprobada" }
```

**¿Por qué tantas capas?** Porque cada capa tiene **una sola razón para cambiar**:
- ¿Cambias de PostgreSQL a SQL Server? Solo tocas Infrastructure.
- ¿Cambias la regla "saldo mínimo 10 USD"? Solo tocas Application o Domain.
- ¿Agregas un endpoint nuevo? Solo tocas Api.

---

## 6. La capa de datos a fondo

Esta es la sección más importante para entender cómo tu código C# se conecta con PostgreSQL.

### 6.1 ¿Qué es un ORM, EF Core y Npgsql?

**ORM (Object-Relational Mapper)**: una herramienta que te permite trabajar con objetos C# en lugar de escribir SQL a mano. Tú escribes `context.Empleados.Where(e => e.Estado == "Activo")` y el ORM genera `SELECT * FROM empleados WHERE estado = 'Activo'`.

**Entity Framework Core (EF Core)**: el ORM oficial de Microsoft para .NET. Es agnóstico de base de datos — él solo sabe traducir entre C# y un modelo relacional abstracto. Necesita un **proveedor** para hablar con una base de datos específica.

**Npgsql**: es ese proveedor. La cadena de responsabilidad es:

```
Tu código C#  →  EF Core (traduce a SQL abstracto)  →  Npgsql (traduce a SQL de PostgreSQL)  →  PostgreSQL
```

En tu proyecto, la conexión está definida en `appsettings.json`:

```json
"ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ate_db;Username=postgres;Password=postgres"
}
```

Y tu `docker-compose.yml` levanta un PostgreSQL que coincide exactamente con esos valores:

```yaml
environment:
    POSTGRES_DB: ate_db         # ← coincide con Database=ate_db
    POSTGRES_USER: postgres     # ← coincide con Username=postgres
    POSTGRES_PASSWORD: postgres # ← coincide con Password=postgres
ports:
    - "5432:5432"               # ← coincide con Port=5432
```

---

### 6.2 El AteDbContext (todavía no existe — cómo se verá)

El `DbContext` es la clase central de EF Core. Piensa en él como una **sesión de trabajo con la base de datos**. Cada instancia abre una conexión, permite consultar y modificar datos, y al llamar `SaveChanges()` envía todo como una transacción.

Basándome en las 6 tablas definidas en `01_init_schema.sql`, tu `AteDbContext` se vería así:

```csharp
// Ate.Infrastructure/Persistence/AteDbContext.cs
namespace Ate.Infrastructure.Persistence;

public class AteDbContext : DbContext, IApplicationDbContext
{
    public AteDbContext(DbContextOptions<AteDbContext> options) : base(options) { }

    // Cada DbSet<T> mapea a una tabla en PostgreSQL
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<TipoBeneficio> TiposBeneficio => Set<TipoBeneficio>();
    public DbSet<SaldoEmpleado> SaldosEmpleado => Set<SaldoEmpleado>();
    public DbSet<ComercioAfiliado> ComerciosAfiliados => Set<ComercioAfiliado>();
    public DbSet<Transaccion> Transacciones => Set<Transaccion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Esto escanea el assembly de Infrastructure buscando todas las clases
        // que implementen IEntityTypeConfiguration<T> y las aplica automáticamente.
        // Así no tienes que registrar cada configuración a mano.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AteDbContext).Assembly);
    }
}
```

**¿Qué es `DbSet<T>`?** Es una colección virtual que representa una tabla. Cuando escribes `context.Empleados.Add(nuevo)`, EF Core sabe que tiene que hacer un `INSERT INTO empleados`. Cuando escribes `context.Empleados.Where(...)`, genera un `SELECT`.

**¿Qué es `OnModelCreating`?** Es el método donde configuras cómo las entidades C# se mapean a las tablas de PostgreSQL: nombres de columnas, tipos, restricciones, relaciones, índices. Se ejecuta **una sola vez** cuando EF Core crea el modelo en memoria.

**¿Qué es `ApplyConfigurationsFromAssembly`?** En lugar de poner toda la configuración dentro de `OnModelCreating` (que se volvería un método de 500 líneas), separas cada entidad en su propia clase de configuración. `ApplyConfigurationsFromAssembly` las encuentra todas automáticamente.

---

### 6.3 Configurations (IEntityTypeConfiguration)

Cada entidad debería tener su propia clase de configuración en `Ate.Infrastructure/Persistence/Configurations/`. Ejemplo basado en tu tabla `empresas`:

```csharp
// Ate.Infrastructure/Persistence/Configurations/EmpresaConfiguration.cs
namespace Ate.Infrastructure.Persistence.Configurations;

public class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        // ── Tabla ──
        builder.ToTable("empresas");        // nombre exacto en PostgreSQL

        // ── Clave primaria ──
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("uuid_generate_v4()"); // PostgreSQL genera el UUID

        // ── Columnas ──
        builder.Property(e => e.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.RucTaxId)
            .HasColumnName("ruc_tax_id")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Estado)
            .HasColumnName("estado")
            .HasMaxLength(20)
            .HasDefaultValue("Activo");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // ── Índices ──
        builder.HasIndex(e => e.RucTaxId).IsUnique();

        // ── Relaciones ──
        builder.HasMany(e => e.Empleados)       // una empresa tiene muchos empleados
            .WithOne(emp => emp.Empresa)          // cada empleado pertenece a una empresa
            .HasForeignKey(emp => emp.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);    // si borras la empresa, se borran los empleados
    }
}
```

**¿Por qué separar las configuraciones en archivos individuales?**
- **Legibilidad**: cada archivo tiene 30-50 líneas, no un método monolítico de 300.
- **Responsabilidad única**: cuando necesites cambiar algo de `Empresa`, sabes exactamente dónde ir.
- **Git diffs limpios**: los cambios en la configuración de una entidad no contaminan las de otras.

---

### 6.4 Migraciones: todo lo que necesitas saber

#### ¿Qué es una migración y qué problema resuelve?

Una migración es un archivo C# que describe **un cambio incremental** en el esquema de la base de datos. Sin migraciones, tendrías que alterar las tablas a mano con `ALTER TABLE`, coordinarte con tu equipo y rezar para que todos apliquen los cambios en el mismo orden. Las migraciones automatizan todo eso.

EF Core compara tu modelo actual (tus entidades y configuraciones en C#) contra el último estado conocido (el ModelSnapshot) y genera el código SQL necesario para sincronizarlos.

#### Los 3 archivos que genera EF Core

Cuando ejecutas `dotnet ef migrations add`, se crean 3 archivos en la carpeta de migraciones:

```
Ate.Infrastructure/
└── Migrations/
    ├── 20261001053000_CreateInitialSchema.cs           ← [1] Los cambios (Up y Down)
    ├── 20261001053000_CreateInitialSchema.Designer.cs  ← [2] Snapshot parcial (NO TOCAR)
    └── AteDbContextModelSnapshot.cs                    ← [3] Foto del modelo completo
```

**[1] El `.cs` principal** (el único que puedes editar con cuidado):
```csharp
public partial class CreateInitialSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // SQL que se ejecuta al APLICAR la migración
        migrationBuilder.CreateTable(name: "empresas", /* ... */);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // SQL que se ejecuta al REVERTIR la migración
        migrationBuilder.DropTable(name: "empresas");
    }
}
```

**[2] El `.Designer.cs`**: contiene una foto del modelo en ese punto del tiempo. **NUNCA lo edites manualmente** — EF Core lo usa internamente para calcular las diferencias con la siguiente migración.

**[3] El `ModelSnapshot.cs`**: la foto acumulativa de todo el modelo actual. **NUNCA lo edites manualmente**. Se actualiza automáticamente cada vez que agregas una migración.

#### El timestamp del nombre

`20261001053000_CreateInitialSchema` — los primeros 14 dígitos son un timestamp UTC (`2026-10-01 05:30:00`). **El timestamp define el orden de aplicación**. EF Core aplica las migraciones en orden cronológico. Si dos desarrolladores crean migraciones al mismo tiempo, los timestamps evitan conflictos de orden.

#### Convención de nombres

| Nombre | ¿Bueno? | ¿Por qué? |
|:---|:---|:---|
| `CreateInitialSchema` | ✅ | Describe qué hace |
| `AddColumnaEmailToEmpleados` | ✅ | Dice qué agrega y dónde |
| `UpdateSaldosAddMonedaColumn` | ✅ | Específico y claro |
| `Migration1` | ❌ | No dice nada |
| `Fix` | ❌ | ¿Qué arregla? |
| `Changes` | ❌ | ¿Qué cambios? |
| `asdfg` | ❌ | Obviamente no |

#### Comandos con tus rutas reales

Todos estos comandos se ejecutan desde `d:\Ate\backend\` (la raíz donde está `Ate.slnx`):

**Crear una migración nueva:**
```bash
dotnet ef migrations add CreateInitialSchema -p Ate.Infrastructure/Ate.Infrastructure.csproj -s Ate.Api/Ate.Api.csproj -o Migrations
```
- `-p` (project): el proyecto que contiene el `DbContext` → `Ate.Infrastructure`.
- `-s` (startup): el proyecto ejecutable que tiene `Program.cs` y la connection string → `Ate.Api`.
- `-o` (output): carpeta dentro del `-p` donde se guardan los archivos generados.

**Aplicar todas las migraciones pendientes a PostgreSQL:**
```bash
dotnet ef database update -p Ate.Infrastructure/Ate.Infrastructure.csproj -s Ate.Api/Ate.Api.csproj
```

**Listar migraciones (y ver cuáles están aplicadas):**
```bash
dotnet ef migrations list -p Ate.Infrastructure/Ate.Infrastructure.csproj -s Ate.Api/Ate.Api.csproj
```

**Eliminar la última migración (solo si NO fue aplicada a la base de datos):**
```bash
dotnet ef migrations remove -p Ate.Infrastructure/Ate.Infrastructure.csproj -s Ate.Api/Ate.Api.csproj
```

**Generar un script SQL idempotente (para producción):**
```bash
dotnet ef migrations script --idempotent -p Ate.Infrastructure/Ate.Infrastructure.csproj -s Ate.Api/Ate.Api.csproj -o migrations.sql
```
`--idempotent` genera un script con `IF NOT EXISTS` que se puede ejecutar múltiples veces sin error. Ideal para CI/CD y para revisión por un DBA antes de aplicar en producción.

**Revertir la base de datos a una migración anterior:**
```bash
dotnet ef database update NombreDeLaMigracionAnterior -p Ate.Infrastructure/Ate.Infrastructure.csproj -s Ate.Api/Ate.Api.csproj
```
Esto ejecuta los métodos `Down()` de todas las migraciones posteriores a la indicada, en orden inverso.

#### Reglas de oro de migraciones

1. **NUNCA edites una migración que ya fue aplicada a la base de datos** o que ya subiste al repositorio. Si cometiste un error, crea una nueva migración que lo corrija.
2. **Una migración por cambio lógico**: "agregar tabla Empleado" es una migración, "agregar columna email a Empleado" es otra. No mezcles 10 cambios en una sola migración.
3. **Revisa el SQL generado** antes de aplicar: usa `dotnet ef migrations script` y lee lo que va a ejecutar. Especialmente importante con `DROP COLUMN` o `ALTER TABLE` que puedan perder datos.
4. **Haz commit de las migraciones al repositorio**. Los 3 archivos (`.cs`, `.Designer.cs`, `ModelSnapshot.cs`) deben estar en Git.

#### ¿Qué pasa cuando dos personas crean migraciones a la vez?

Situación: Tú creas `AddColumnaEmailToEmpresas` y tu compañero crea `AddTablaCategorias`. Ambos modifican el `ModelSnapshot.cs`. Cuando uno intenta hacer merge, habrá un **conflicto en el ModelSnapshot**.

**Cómo resolverlo:**
1. Haz merge del código normalmente (resuelve conflictos en el `.cs` y `.Designer.cs` si los hay).
2. Elimina la migración que generó el conflicto: `dotnet ef migrations remove`.
3. Regenera tu migración: `dotnet ef migrations add AddColumnaEmailToEmpresas`.
4. El nuevo `ModelSnapshot` ahora incluye AMBOS cambios.

---

### 6.5 Seed: datos iniciales

**¿Qué es seeding?** Es la inserción de datos iniciales que la aplicación necesita para funcionar (catálogos, roles, configuraciones por defecto).

Tu proyecto actualmente usa **scripts SQL manuales** (`02_seed_data.sql`) que se ejecutan a través de Docker. Hay dos enfoques en EF Core:

#### Enfoque A: `HasData` (declarativo, dentro de las Configurations)

```csharp
// Dentro de TipoBeneficioConfiguration.cs
builder.HasData(
    new TipoBeneficio { Id = 1, Codigo = "ALIMENTACION", Nombre = "Saldo de Alimentación" },
    new TipoBeneficio { Id = 2, Codigo = "SALUD", Nombre = "Saldo de Salud & Medicinas" },
    new TipoBeneficio { Id = 3, Codigo = "GENERAL", Nombre = "Saldo General Corporativo" }
);
```

**Ventaja**: los datos se incluyen en las migraciones, se versionan con Git y se aplican con `dotnet ef database update`.

**Limitaciones de `HasData`**:
- Debes especificar TODAS las propiedades, incluyendo la clave primaria (no puede ser autogenerada).
- No puedes usar lógica compleja (bucles, condicionales, servicios externos).
- No puede generar datos aleatorios o dinámicos.

#### Enfoque B: Seeder al arrancar (imperativo, en Program.cs)

```csharp
// Ate.Infrastructure/Persistence/AteDbSeeder.cs
public static class AteDbSeeder
{
    public static async Task SeedAsync(AteDbContext context)
    {
        if (!await context.TiposBeneficio.AnyAsync())
        {
            context.TiposBeneficio.AddRange(
                new TipoBeneficio { Codigo = "ALIMENTACION", Nombre = "Saldo de Alimentación" },
                new TipoBeneficio { Codigo = "SALUD", Nombre = "Saldo de Salud & Medicinas" }
            );
            await context.SaveChangesAsync();
        }
    }
}

// En Program.cs:
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AteDbContext>();
await AteDbSeeder.SeedAsync(db);
```

**Ventaja**: puedes usar lógica compleja, condicionales, y no necesitas especificar claves primarias.

**Recomendación**: usa `HasData` para catálogos fijos (tipos de beneficio, roles). Usa un Seeder imperativo para datos de prueba en desarrollo.

---

### 6.6 Auditoría automática (CreatedAt / UpdatedAt)

Tu esquema SQL define `created_at` y `updated_at` en casi todas las tablas. Puedes automatizar estos campos sobreescribiendo `SaveChangesAsync` en el `DbContext`:

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    foreach (var entry in ChangeTracker.Entries<BaseEntity>())
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.CreatedAt = DateTime.UtcNow;
                break;
            case EntityState.Modified:
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                break;
        }
    }
    return await base.SaveChangesAsync(ct);
}
```

Esto requiere que todas tus entidades hereden de una clase base:

```csharp
// Ate.Domain/Common/BaseEntity.cs
public abstract class BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

---

## 7. Flujo de trabajo: agregar una tabla nueva

Checklist paso a paso con los nombres y rutas reales de Ate:

```
☐ 1. Crear la entidad en Ate.Domain
      → Ate.Domain/Entities/NuevaEntidad.cs

☐ 2. Crear la configuración en Ate.Infrastructure
      → Ate.Infrastructure/Persistence/Configurations/NuevaEntidadConfiguration.cs
      (definir tabla, columnas, restricciones, relaciones, índices)

☐ 3. Agregar el DbSet en AteDbContext
      → public DbSet<NuevaEntidad> NuevasEntidades => Set<NuevaEntidad>();

☐ 4. Crear la migración
      → dotnet ef migrations add AddTablaNuevaEntidad -p Ate.Infrastructure/Ate.Infrastructure.csproj -s Ate.Api/Ate.Api.csproj -o Migrations

☐ 5. Revisar el SQL generado
      → dotnet ef migrations script --idempotent -p Ate.Infrastructure/Ate.Infrastructure.csproj -s Ate.Api/Ate.Api.csproj

☐ 6. Aplicar la migración a la base de datos local
      → dotnet ef database update -p Ate.Infrastructure/Ate.Infrastructure.csproj -s Ate.Api/Ate.Api.csproj

☐ 7. Crear el DTO en Ate.Application
      → Ate.Application/DTOs/NuevaEntidadDto.cs

☐ 8. Crear la interfaz del repositorio en Ate.Application
      → Ate.Application/Contracts/INuevaEntidadRepository.cs

☐ 9. Implementar el repositorio en Ate.Infrastructure
      → Ate.Infrastructure/Persistence/Repositories/NuevaEntidadRepository.cs

☐ 10. Crear el servicio/caso de uso en Ate.Application
       → Ate.Application/Services/NuevaEntidadService.cs

☐ 11. Crear el controller en Ate.Api
       → Ate.Api/Controllers/NuevasEntidadesController.cs

☐ 12. Registrar en DI (si no usas auto-discovery)
       → Ate.Infrastructure/DependencyInjection.cs
       → services.AddScoped<INuevaEntidadRepository, NuevaEntidadRepository>();

☐ 13. Probar en Swagger o con el archivo .http
       → GET http://localhost:5252/api/nuevas-entidades
```

---

## 8. Observaciones del análisis

Estos son hallazgos reales del estado actual del proyecto. No son errores — son cosas pendientes de implementar o mejorar:

### 8.1 No hay entidades en Ate.Domain
El proyecto `Ate.Domain` está vacío. Las tablas están definidas en `01_init_schema.sql` (6 tablas completas con restricciones e índices), pero no existen las clases C# correspondientes. Esto significa que EF Core no puede funcionar todavía — no tiene entidades que mapear.

**¿Por qué importa?** Sin entidades, no puedes crear el `DbContext`, no puedes generar migraciones, no puedes consultar datos desde C#. Los scripts SQL solo funcionan si los ejecutas manualmente o vía Docker.

### 8.2 No hay DbContext en Ate.Infrastructure
Consecuencia directa de lo anterior. No existe `AteDbContext`, no hay carpeta `Persistence/`, no hay `Configurations/`, no hay `Migrations/`.

### 8.3 No hay DependencyInjection.cs en ninguna capa
No existe un método de extensión para registrar servicios de Infrastructure o Application en el contenedor de IoC. Actualmente `Program.cs` solo registra OpenAPI.

### 8.4 Program.cs tiene el template por defecto
El endpoint `/weatherforecast` y el record `WeatherForecast` son código de plantilla que debería eliminarse cuando se implementen los endpoints reales.

### 8.5 Doble estrategia de esquema: SQL manual + EF Core
Tienes scripts SQL en `database/` **y** el paquete de EF Core instalado para usar migraciones. Esto puede causar conflictos: si creas tablas con el script SQL y luego generas migraciones con EF Core, EF Core intentará crear las mismas tablas otra vez.

**Recomendación**: Elige una estrategia:
- **Opción A (recomendada)**: Usa EF Core como fuente de verdad. Define entidades → genera migraciones → aplica. Elimina o archiva los scripts SQL manuales.
- **Opción B**: Usa los scripts SQL como fuente de verdad y EF Core solo para consultas (sin migraciones). Esto se logra no generando migraciones y marcando las entidades como "ya existentes".

### 8.6 Connection string con credenciales en appsettings.json
`Password=postgres` está en texto plano en `appsettings.json`, que se sube a Git. Ya tienes User Secrets configurados (`UserSecretsId` en el `.csproj`) pero la password real sigue estando en el archivo público. En producción, deberías dejar un placeholder y usar variables de entorno o User Secrets para los valores reales.

### 8.7 Versiones de EF Core desalineadas
- `Npgsql.EntityFrameworkCore.PostgreSQL` en Infrastructure → **10.0.3**
- `Microsoft.EntityFrameworkCore.Design` en Api → **10.0.12**

Ambos paquetes deberían estar en la misma versión minor para evitar incompatibilidades. Lo ideal es que ambos estén en `10.0.12` (la más reciente) o ambos en `10.0.3`.

### 8.8 docker-compose.yml duplicado
Existe en `d:\Ate\docker-compose.yml` **y** en `d:\Ate\backend\docker-compose.yml`. El de la raíz no está dentro del repositorio Git (solo el de backend lo está), lo que puede causar confusión.

---

## 9. Errores comunes y cómo evitarlos

| Error | Causa | Solución |
|:---|:---|:---|
| `No DbContext was found in assembly 'Ate.Infrastructure'` | No has creado la clase `AteDbContext` todavía, o no la registraste en DI | Crear el DbContext y registrarlo con `AddDbContext<AteDbContext>()` |
| `Unable to create a 'DbContext' of type 'AteDbContext'` | El `-s` no apunta al proyecto ejecutable, o `Program.cs` no registra el DbContext | Verificar que `-s Ate.Api/Ate.Api.csproj` y que `AddDbContext` está en `Program.cs` |
| `relation "empresas" already exists` | La tabla ya fue creada por el script SQL manual y EF Core intenta crearla otra vez con una migración | Elegir una sola estrategia de esquema (ver observación 8.5) |
| `Npgsql.NpgsqlException: Connection refused` | PostgreSQL no está corriendo en `localhost:5432` | Ejecutar `docker compose up -d` en `d:\Ate\backend\` |
| `The migration '...' has already been applied` | Intentas aplicar una migración que ya se ejecutó | Es normal. `dotnet ef database update` solo aplica migraciones **pendientes** |
| Conflicto de merge en `ModelSnapshot.cs` | Dos desarrolladores crearon migraciones en paralelo | Hacer merge, `dotnet ef migrations remove`, y recrear la migración |
| `Column 'id' cannot be null` al hacer Seed con `HasData` | Olvidaste especificar la clave primaria en `HasData` | `HasData` exige que especifiques TODOS los campos obligatorios, incluyendo el Id |
| CORS: `Access-Control-Allow-Origin` faltante | No configuraste CORS en `Program.cs` | Agregar `builder.Services.AddCors(...)` y `app.UseCors(...)` |

---

## 10. Glosario

| Término | Definición |
|:---|:---|
| **ORM** | Object-Relational Mapper. Traduce entre objetos C# y tablas de base de datos, evitando escribir SQL a mano. |
| **DbContext** | Clase central de EF Core que representa una sesión con la base de datos. Coordina consultas, inserciones y transacciones. |
| **DbSet\<T\>** | Propiedad del DbContext que representa una tabla. `DbSet<Empleado>` ↔ tabla `empleados`. Permite hacer LINQ que se traduce a SQL. |
| **Migración** | Archivo C# que describe un cambio incremental en el esquema de la base de datos (crear tabla, agregar columna, etc.). |
| **ModelSnapshot** | Archivo generado por EF Core que contiene la foto completa del modelo actual. Se usa para calcular las diferencias al crear la siguiente migración. |
| **Seed** | Datos iniciales que la aplicación necesita para funcionar (catálogos, roles, configuraciones). Se pueden insertar con `HasData` o con un seeder imperativo. |
| **DTO** | Data Transfer Object. Clase simple que define la forma de los datos que entran o salen de la API, sin lógica de negocio. Evita exponer las entidades internas. |
| **Inyección de dependencias (DI)** | Patrón donde las clases reciben sus dependencias por constructor en lugar de crearlas ellas mismas. Permite desacoplamiento y testing fácil. |
| **Repositorio** | Clase que encapsula el acceso a datos. En lugar de que el servicio use `DbContext` directamente, llama a `repository.GetById(id)`. |
| **Middleware** | Componente que intercepta cada HTTP request/response en el pipeline de ASP.NET. Ejemplo: autenticación, logging, manejo de errores. |
| **Connection string** | Cadena de texto que le dice a EF Core cómo conectarse a la base de datos: servidor, puerto, nombre de BD, usuario y contraseña. |
