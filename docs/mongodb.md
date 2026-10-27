# MongoDB en Ate

MongoDB guarda la data no relacional de Ate. Por ahora son los pedidos de comida de cada colaborador. El resto del sistema (usuarios, empresas, empleados) sigue en PostgreSQL.

## Conexión rápida (local)

Copia esta línea y pégala en Compass → **New Connection** → **URI**:

```
mongodb://ate_admin:ate_mongo_2026@127.0.0.1:27017/
```

Para el backend (`appsettings.json` / user-secrets):

```
mongodb://ate_admin:ate_mongo_2026@localhost:27017/?authSource=admin
```

Base de datos: `ate_nosql_db` · Colección: `employee_orders`

## Contenedor

| Dato | Valor |
|---|---|
| Imagen | `mongo:8.0` (8.0 es la versión estable con soporte largo; funciona con `MongoDB.Driver 3.12.0`) |
| Contenedor | `ate_mongo_db` |
| Puerto | `27017` |
| Volumen | `mongo_ate_data` (los datos se conservan aunque reinicies el contenedor) |
| Usuario root | `ate_admin` |
| Password | `ate_mongo_2026` |
| Base de autenticación | `admin` |
| Base de datos de la app | `ate_nosql_db` |

> Estas credenciales son **solo para desarrollo local**. En otros ambientes usa otras credenciales y pásalas por variables de entorno o secretos, nunca en el repositorio.

### Comandos (desde `D:\Ate`)

```bash
make mongo         # levanta solo MongoDB
make mongo-stop    # detiene MongoDB
make mongo-logs    # logs en tiempo real
make mongo-shell   # abre mongosh dentro del contenedor, ya autenticado
make dev           # levanta Postgres + Mongo + backend
```

Requisito: Docker Desktop corriendo. Si Docker no arranca y dice que falta el kernel de WSL 2, ejecuta `wsl --update` como administrador y reinicia Docker Desktop.

## Guía: levantar la imagen de MongoDB

### Opción A: con docker compose (recomendada)

El servicio ya está definido en `docker-compose.yml`:

```yaml
services:
  mongo:
    image: mongo:8.0
    container_name: ate_mongo_db
    restart: always
    environment:
      MONGO_INITDB_ROOT_USERNAME: ate_admin
      MONGO_INITDB_ROOT_PASSWORD: ate_mongo_2026
      MONGO_INITDB_DATABASE: ate_nosql_db
    ports:
      - "27017:27017"
    volumes:
      - mongo_ate_data:/data/db

volumes:
  mongo_ate_data:
```

```bash
docker compose up -d mongo      # descarga la imagen (solo la primera vez) y levanta el contenedor
docker compose ps               # debe aparecer ate_mongo_db en estado "Up"
docker compose logs -f mongo    # ver logs
```

### Opción B: con docker run (sin compose)

```bash
docker volume create mongo_ate_data

docker run -d \
  --name ate_mongo_db \
  --restart always \
  -p 27017:27017 \
  -e MONGO_INITDB_ROOT_USERNAME=ate_admin \
  -e MONGO_INITDB_ROOT_PASSWORD=ate_mongo_2026 \
  -e MONGO_INITDB_DATABASE=ate_nosql_db \
  -v mongo_ate_data:/data/db \
  mongo:8.0
```

En PowerShell cambia las `\` del final de cada línea por `` ` `` o escribe todo en una sola línea.

### Verificar que funciona

```bash
docker exec -it ate_mongo_db mongosh -u ate_admin -p ate_mongo_2026 --authenticationDatabase admin --eval "db.adminCommand({ ping: 1 })"
```

Debe responder `{ ok: 1 }`.

### Importante sobre las credenciales

`MONGO_INITDB_ROOT_USERNAME` y `MONGO_INITDB_ROOT_PASSWORD` **solo se aplican la primera vez**, cuando el volumen está vacío. Si después cambias el password en el compose, Mongo sigue usando el anterior. Para aplicar uno nuevo:

- **Sin datos importantes:** borra el volumen y vuelve a levantar.

  ```bash
  docker compose down
  docker volume rm ate_mongo_ate_data
  docker compose up -d mongo
  ```

- **Con datos:** cambia el password desde mongosh.

  ```js
  use admin
  db.changeUserPassword("ate_admin", "NUEVO_PASSWORD")
  ```

## Guía: desplegar MongoDB en un VPS

En un VPS la base queda expuesta a internet, así que **no copies la configuración local tal cual**. Los cambios clave son tres: un password fuerte, no publicar el puerto 27017 y respaldos.

### 1. Instalar Docker en el VPS (Ubuntu/Debian)

```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER   # cierra sesión y vuelve a entrar para que aplique
docker --version
docker compose version
```

### 2. Crear la carpeta y el archivo `.env`

```bash
mkdir -p ~/ate && cd ~/ate
nano .env
```

Contenido del `.env` (genera un password fuerte con `openssl rand -base64 32`):

```env
MONGO_ROOT_USER=ate_admin
MONGO_ROOT_PASSWORD=PON_AQUI_UN_PASSWORD_LARGO_Y_ALEATORIO
MONGO_DB=ate_nosql_db
```

```bash
chmod 600 .env   # solo tu usuario puede leerlo
```

> No subas el `.env` al repositorio.
>
> Si el password tiene caracteres especiales (`@ : / ? # [ ] %`), tienes que codificarlos en el connection string: `@` se escribe `%40`, `:` `%3A`, `/` `%2F`. Para evitarte eso, usa un password solo con letras y números, por ejemplo `openssl rand -hex 24`.

### 3. `docker-compose.yml` para el VPS

```yaml
services:
  mongo:
    image: mongo:8.0
    container_name: ate_mongo_db
    restart: always
    env_file: .env
    environment:
      MONGO_INITDB_ROOT_USERNAME: ${MONGO_ROOT_USER}
      MONGO_INITDB_ROOT_PASSWORD: ${MONGO_ROOT_PASSWORD}
      MONGO_INITDB_DATABASE: ${MONGO_DB}
    ports:
      - "127.0.0.1:27017:27017"   # solo accesible desde el propio VPS, NO desde internet
    volumes:
      - mongo_ate_data:/data/db

volumes:
  mongo_ate_data:
```

```bash
docker compose up -d mongo
docker compose ps
```

**Por qué `127.0.0.1:27017`:** con `"27017:27017"` Docker abre el puerto a todo internet, **y se salta el firewall (ufw)**. Hay bots que buscan Mongos abiertos todo el tiempo. Con `127.0.0.1` solo llegan las apps del mismo VPS.

### 4. Conectar el backend en el VPS

- **Si la API corre en el mismo `docker-compose`**, usa el nombre del servicio:

  ```
  ConnectionStrings__MongoDbConnection=mongodb://ate_admin:TU_PASSWORD@mongo:27017/?authSource=admin
  MongoDbSettings__DatabaseName=ate_nosql_db
  ```

- **Si la API corre directo en el VPS, fuera de Docker:**

  ```
  mongodb://ate_admin:TU_PASSWORD@127.0.0.1:27017/?authSource=admin
  ```

Pásalo como **variable de entorno**, no en `appsettings.json`.

### 5. Conectarte con Compass al Mongo del VPS (por túnel SSH)

Como el puerto no está abierto a internet, Compass entra a través de SSH:

1. Compass → **New Connection** → URI: `mongodb://ate_admin:TU_PASSWORD@127.0.0.1:27017/`
2. **Advanced Connection Options → Proxy/SSH → SSH with Password** (o **Identity File** si usas llave):
   - SSH Hostname: la IP de tu VPS
   - SSH Port: `22`
   - SSH Username: tu usuario del VPS
   - SSH Password o tu llave privada
3. Haz clic en **Connect**.

Otra opción es abrir el túnel a mano y conectarte como si fuera local:

```bash
ssh -L 27018:127.0.0.1:27017 usuario@IP_DEL_VPS
# deja esa terminal abierta y en Compass usa:
# mongodb://ate_admin:TU_PASSWORD@127.0.0.1:27018/
```

### 6. Firewall

```bash
sudo ufw allow OpenSSH
sudo ufw allow 80
sudo ufw allow 443
sudo ufw enable
sudo ufw status
```

No abras el 27017 en ufw. Si alguna vez necesitas exponerlo, limítalo a una IP específica y activa TLS.

### 7. Respaldos

```bash
# Crear un backup comprimido
docker exec ate_mongo_db sh -c 'mongodump -u "$MONGO_INITDB_ROOT_USERNAME" -p "$MONGO_INITDB_ROOT_PASSWORD" --authenticationDatabase admin --db ate_nosql_db --archive --gzip' > backup_$(date +%F).gz

# Restaurar
docker exec -i ate_mongo_db sh -c 'mongorestore -u "$MONGO_INITDB_ROOT_USERNAME" -p "$MONGO_INITDB_ROOT_PASSWORD" --authenticationDatabase admin --archive --gzip' < backup_2026-10-26.gz
```

Para automatizarlo, prográmalo con `crontab -e`, por ejemplo todos los días a las 3:00. Guarda las copias fuera del VPS.

### 8. Actualizar la imagen

```bash
docker compose pull mongo
docker compose up -d mongo
```

Mantén el tag `mongo:8.0`. Así recibes los parches de seguridad de 8.0 sin saltar a otra versión mayor por accidente. Antes de cambiar a 8.2 o 9.0, saca un backup.

### Checklist para el VPS

- [ ] Password fuerte, guardado en `.env` con `chmod 600`
- [ ] Puerto publicado como `127.0.0.1:27017:27017`
- [ ] ufw activo, sin el 27017 abierto
- [ ] Connection string pasado como variable de entorno
- [ ] Backups automáticos y copiados fuera del VPS
- [ ] Compass conectado por túnel SSH

## Connection strings

| Desde dónde | Connection string |
|---|---|
| Tu máquina (Compass, `dotnet run`, `make back`) | `mongodb://ate_admin:ate_mongo_2026@localhost:27017/?authSource=admin` |
| Otro contenedor del compose (servicio `api`) | `mongodb://ate_admin:ate_mongo_2026@mongo:27017/?authSource=admin` |

Si `localhost` da problemas, usa `127.0.0.1`.

## Conectarse con MongoDB Compass

### Opción 1: pegar el connection string (la más rápida)

1. Abre Compass → **New Connection**.
2. En el campo **URI** pega:

   ```
   mongodb://ate_admin:ate_mongo_2026@127.0.0.1:27017/
   ```

3. Haz clic en **Connect**.

No hace falta `?authSource=admin`. Cuando la URL no indica una base de datos, Mongo autentica contra `admin` por defecto.

> **Ojo al copiar:** el connection string tiene que quedar en **una sola línea y sin espacios**. Si lo copias desde una terminal, el salto de línea visual puede meter un espacio en medio, por ejemplo `l ocalhost` o `authsour ce`. Entonces Compass muestra errores como:
> - `getaddrinfo ENOTFOUND l ocalhost`
> - `option authsour ce is not supported`
>
> Para evitarlo, cópialo desde este archivo o escríbelo a mano.

### Opción 2: llenar los campos por separado

Sirve si el pegado sigue fallando:

1. En **URI** deja solo `mongodb://127.0.0.1:27017/`.
2. Abre **Advanced Connection Options → Authentication → Username/Password**.
3. Username: `ate_admin`
4. Password: `ate_mongo_2026`
5. Authentication Database: `admin`
6. Haz clic en **Connect**.

Al entrar verás `ate_nosql_db` en la barra izquierda, y dentro la colección `employee_orders`.

## Configuración en el backend

El backend lee la conexión en este orden y usa el primer valor que no esté vacío:

1. `ConnectionStrings:MongoDbConnection`
2. `MongoDbSettings:ConnectionString`

El nombre de la base sale de `MongoDbSettings:DatabaseName` (por defecto `ate_nosql_db`).

Dónde está configurado:

- **`Ate.Api/appsettings.json`**: valores locales por defecto.
- **user-secrets** (`Ate.Api`), que tienen prioridad sobre `appsettings.json`:

  ```bash
  dotnet user-secrets set "ConnectionStrings:MongoDbConnection" "mongodb://ate_admin:ate_mongo_2026@localhost:27017/?authSource=admin" --project Ate.Api
  dotnet user-secrets set "MongoDbSettings:ConnectionString" "mongodb://ate_admin:ate_mongo_2026@localhost:27017/?authSource=admin" --project Ate.Api
  dotnet user-secrets set "MongoDbSettings:DatabaseName" "ate_nosql_db" --project Ate.Api
  dotnet user-secrets list --project Ate.Api
  ```

- **docker-compose**: variable de entorno `ConnectionStrings__MongoDbConnection` del servicio `api`.

Si no hay ningún connection string configurado, Mongo no se registra en el backend y los endpoints de pedidos fallan.

## Colección `employee_orders`

Cada documento es un pedido de comida de un colaborador. Guarda una **copia** del restaurante y del plato (nombre, precio, proteína), así el historial no cambia aunque después se modifique el menú en PostgreSQL.

### De dónde salen los restaurantes (PostgreSQL)

Los restaurantes y sus menús viven en PostgreSQL. Mongo solo guarda los pedidos.

| Tabla | Qué guarda |
|---|---|
| `restaurants` | Restaurantes aliados: nombre, cocina, color de marca, ícono/logo, tiempo de preparación y hora de corte de cada comida |
| `restaurant_dishes` | Menú de cada restaurante por comida (`breakfast`, `lunch`, `dinner`): plato, descripción, proteína, si lleva arroz, bebida y precio |
| `company_restaurant_agreements` | **Convenios**: qué restaurantes ofrece cada empresa, la distancia a la empresa y las fechas de vigencia |

Un colaborador solo ve, y solo puede pedir en, los restaurantes que tienen un **convenio activo** con su empresa: `is_active = true`, ya empezó y no ha vencido.

En desarrollo, el seed crea 4 restaurantes (El Patio Criollo, Comedor Doña Mercedes, Verde Bowl, La Sazón Manabita) con sus menús y su convenio con Acme. Precios: desayuno $2.50, almuerzo $3.50, merienda $3.00. Horas de corte: 09:00, 11:30 y 18:30.

### Cómo se crea

La colección y sus índices se crean **automáticamente al arrancar la API** (`MongoDbInitializer.EnsureCollectionsAsync`, llamado desde `DatabaseExtensions`). El proceso:

- Es idempotente: si la colección o los índices ya existen, no hace nada.
- Tiene un timeout de 10 segundos. Si Mongo no responde, se registra un error en el log y la API **arranca igual**.

Índices:

| Nombre | Campos | Para qué |
|---|---|---|
| `ix_employee_orders_employee_ordered_at` | `employeeId: 1, orderedAt: -1` | Historial de un colaborador |
| `ix_employee_orders_company_ordered_at` | `companyId: 1, orderedAt: -1` | Reportes por empresa |
| `ix_employee_orders_restaurant_service_date` | `restaurantId: 1, serviceDate: 1, status: 1` | Pedidos del día de un restaurante |
| `ux_employee_orders_ticket_number` | `ticketNumber: 1` (único) | Buscar un pedido por ticket |
| `ux_employee_orders_active_slot` | `activeSlot: 1` (único, solo si existe) | Impide dos pedidos activos de la misma comida el mismo día |

### Estructura del documento

```js
{
  _id: ObjectId("..."),
  ticketNumber: "ATE-261027-P5LCC",      // ATE-{yyMMdd del día de servicio}-{5 caracteres}
  employeeId: UUID("..."),               // employees.id en PostgreSQL
  companyId: UUID("..."),                // companies.id
  userId: UUID("..."),                   // users.id del colaborador
  employeeCode: "EMP-002",
  employeeName: "María Belén Cevallos Ruiz",
  employeeDocument: "0923456781",
  department: "Talento Humano",
  restaurantId: UUID("..."),             // restaurants.id
  restaurantName: "Comedor Doña Mercedes",
  mealType: "Breakfast",                 // Breakfast | Lunch | Dinner
  serviceDate: "2026-10-27",             // día en que se sirve (hora de Ecuador)
  activeSlot: "{employeeId}:2026-10-27:Breakfast", // solo mientras el pedido está activo
  items: [
    {
      dishId: UUID("..."),               // restaurant_dishes.id
      name: "Humitas",
      description: "Dos humitas con queso.",
      protein: "Vegetariano",
      quantity: 1,
      unitPrice: Decimal128("2.50"),
      subtotal: Decimal128("2.50"),
      withRice: true,                    // solo si el plato ofrece arroz
      drink: "Café"                      // bebida del plato o "Agua"
    }
  ],
  totalAmount: Decimal128("2.50"),
  currency: "USD",
  status: "Pending",                     // Pending | Confirmed | Ready | Delivered | Cancelled
  statusHistory: [                       // tracking: cada cambio de estado
    { status: "Pending", changedAt: ISODate("..."), changedByUserId: UUID("..."), note: "Pedido registrado por el colaborador." }
  ],
  orderedAt: ISODate("..."),
  createdAt: ISODate("..."),
  updatedAt: ISODate("...")              // al cambiar de estado
}
```

Convenciones del mapeo (configuradas en `MongoDbExtensions`):

- Los campos se guardan en **camelCase**.
- Los `Guid` se guardan como `UUID` estándar.
- Los montos (`decimal`) se guardan como `Decimal128`, sin pérdida de precisión.
- Las fechas (`DateTimeOffset`) se guardan como `ISODate` en UTC.
- Los enums se guardan como texto (`"Breakfast"`).
- Los campos con valor `null` no se guardan.

## Endpoints

| Rol | Método y ruta | Descripción |
|---|---|---|
| `Employee` | `GET /api/v1/employee/restaurants` | Restaurantes con convenio activo con su empresa, con menú y horas de corte |
| `Employee` | `POST /api/v1/employee/orders` | Pide un plato |
| `Employee` | `GET /api/v1/employee/orders?pageNumber=1&pageSize=10` | Lista sus pedidos, del más reciente al más antiguo |
| `Employee` | `GET /api/v1/employee/orders/{orderId}` | Detalle de un pedido con su historial de estados |
| `Employee` | `POST /api/v1/employee/orders/{orderId}/cancel` | Cancela un pedido `Pending` antes de la hora de corte |
| `CompanyAdmin` | `GET /api/v1/company/employees/{employeeId}/orders` | Lista los pedidos de un colaborador de su empresa |

`GET /employee/restaurants` usa como claves los códigos de comida del dashboard:

```json
{
  "id": "…", "name": "El Patio Criollo", "cuisine": "Criolla", "distanceM": 250, "readyMinutes": 15,
  "brandColor": "#B5532E", "logoUrl": null, "icon": "cubiertos",
  "cutoff": { "desayuno": "09:00", "almuerzo": "11:30", "merienda": "18:30" },
  "menu": {
    "almuerzo": [
      { "id": "…", "name": "Seco de pollo", "description": "Sopa de quinua, arroz y maduro.", "protein": "Pollo", "hasRice": true, "defaultDrink": "Jugo", "price": 3.50 }
    ]
  }
}
```

Body del `POST /employee/orders`:

```json
{
  "restaurantId": "…",
  "mealType": "almuerzo",
  "dishId": "…",
  "withRice": false,
  "drink": "agua"
}
```

- `mealType`: `desayuno`, `almuerzo` o `merienda`.
- `withRice`: solo aplica si el plato tiene arroz. Si no se envía, se asume con arroz.
- `drink`: `default` (la bebida del plato) o `agua`.

### Reglas del pedido

| Regla | Respuesta si falla |
|---|---|
| El usuario es un colaborador **activo** | `403` Tu cuenta no está registrada como colaborador activo. |
| El restaurante tiene **convenio activo** con su empresa | `404` Este restaurante no tiene convenio activo con tu empresa. |
| El plato pertenece a ese restaurante y a esa comida, y está disponible | `404` Este plato ya no está disponible para este horario. |
| No pasó la **hora de corte** del restaurante (hora de Ecuador) | `400` Ya pasó la hora de corte para pedir en este restaurante. |
| No tiene otro pedido activo de la misma comida ese día | `409` Ya tienes un pedido registrado para esta comida. |

Desde las 22:00, un pedido de `desayuno` se registra para **el día siguiente**, igual que en el dashboard, que muestra "¿Qué desayunas mañana?".

El precio, el total, el ticket y el día de servicio los calcula el servidor; el cliente no los puede enviar.

### Estados (tracking)

`Pending` → `Confirmed` → `Ready` → `Delivered`, o `Cancelled`.

Cada cambio se agrega a `statusHistory` con la fecha, quién lo hizo y una nota. Por ahora el colaborador puede crear un pedido (`Pending`) y cancelarlo (`Cancelled`). Los pasos `Confirmed`, `Ready` y `Delivered` los hará el comercio cuando exista su endpoint.

## Consultas útiles (mongosh / Compass)

```js
use ate_nosql_db

// Últimos pedidos
db.employee_orders.find().sort({ orderedAt: -1 }).limit(10)

// Buscar por ticket
db.employee_orders.findOne({ ticketNumber: "ATE-261027-P5LCC" })

// Pedidos activos de un restaurante para un día
db.employee_orders.find({ restaurantName: "El Patio Criollo", serviceDate: "2026-10-27", status: { $ne: "Cancelled" } })

// Total consumido por colaborador (sin cancelados)
db.employee_orders.aggregate([
  { $match: { status: { $ne: "Cancelled" } } },
  { $group: { _id: "$employeeName", total: { $sum: "$totalAmount" }, pedidos: { $sum: 1 } } },
  { $sort: { total: -1 } }
])
```

En la barra **Filter** de Compass puedes pegar, por ejemplo, `{ status: "Pending" }` o `{ employeeCode: "EMP-002" }`.
