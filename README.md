# MyAPI - Product CRUD con .NET 8 + PostgreSQL

API REST con Minimal APIs, Entity Framework Core y PostgreSQL, todo corriendo en Docker.

## Tecnologías

- .NET 8 (Minimal APIs)
- Entity Framework Core + Npgsql
- PostgreSQL 16
- Docker & Docker Compose
- Swagger/OpenAPI

## Despliegue

### Requisitos previos

- [Docker](https://docs.docker.com/get-docker/) instalado

### 1. Configurar variables de entorno

```bash
cp .env.example .env
```

Edita el archivo `.env` y ajusta los valores segun tu entorno. Como minimo, cambia `POSTGRES_PASSWORD`.

> **IMPORTANTE:** El archivo `.env` contiene credenciales y esta excluido de Git. Nunca lo subas al repositorio.

### 2. Levantar el proyecto

#### Produccion

```bash
docker compose up --build
```

Usa `Dockerfile` (multi-stage build, imagen optimizada, usuario non-root). La API sirve el binario compilado en modo Release.

#### Desarrollo (con hot reload)

```bash
docker compose -f compose.yml -f compose.dev.yml up --build
```

Usa `Dockerfile.dev` (imagen SDK, `dotnet watch run`). Los cambios en el codigo se reflejan automaticamente sin reconstruir el contenedor.

> Ver seccion [Comando de desarrollo en detalle](#comando-de-desarrollo-en-detalle) para una explicacion completa.

Ambos modos levantan:
- **API** en `http://localhost:8080` (configurable via `API_PORT`)
- **PostgreSQL** en `localhost:5432` (configurable via `DB_PORT`)

Las migraciones se aplican automaticamente al iniciar la API.

### 3. Swagger UI

Accede a la documentacion interactiva en:

```
http://localhost:8080/swagger
```

> Swagger solo esta disponible cuando `ASPNETCORE_ENVIRONMENT=Development`.

### 4. Detener el proyecto

```bash
docker compose down
```

Para eliminar tambien los datos de PostgreSQL:

```bash
docker compose down -v
```

## Endpoints

| Método | Ruta                    | Descripción              |
|--------|-------------------------|--------------------------|
| GET    | /api/products           | Listar productos         |
| GET    | /api/products/search    | Busqueda avanzada        |
| GET    | /api/products/{id}      | Obtener producto         |
| POST   | /api/products           | Crear producto           |
| PUT    | /api/products/{id}      | Actualizar producto      |
| DELETE | /api/products/{id}      | Eliminar producto        |

## Ejemplo de uso

### Crear un producto

```bash
curl -X POST http://localhost:8080/api/products \
  -H "Content-Type: application/json" \
  -d '{"name": "Laptop", "price": 999.99, "brand": "Dell"}'
```

### Listar productos

```bash
curl http://localhost:8080/api/products
```

### Busqueda avanzada

**Parametros disponibles:**

| Parametro  | Tipo    | Default | Descripcion                                         |
|------------|---------|---------|-----------------------------------------------------|
| `name`     | string  | -       | Busqueda parcial por nombre (case-insensitive)       |
| `brand`    | string  | -       | Filtro por marca (case-insensitive)                  |
| `minPrice` | decimal | -       | Precio minimo                                        |
| `maxPrice` | decimal | -       | Precio maximo                                        |
| `sort`     | string  | id asc  | Ordenamiento: `price_asc`, `price_desc`, `name_asc`, `name_desc` |
| `page`     | int     | 1       | Numero de pagina (minimo 1)                          |
| `pageSize` | int     | 10      | Resultados por pagina (1-50)                         |

**Ejemplos de request:**

```bash
# Buscar por nombre
curl "http://localhost:8080/api/products/search?name=laptop"

# Filtrar por marca y rango de precios
curl "http://localhost:8080/api/products/search?brand=dell&minPrice=500&maxPrice=1500"

# Paginacion con ordenamiento
curl "http://localhost:8080/api/products/search?sort=price_desc&page=1&pageSize=5"

# Combinacion completa
curl "http://localhost:8080/api/products/search?name=laptop&brand=dell&minPrice=500&maxPrice=2000&sort=price_asc&page=1&pageSize=10"
```

**Ejemplo de response:**

```json
{
  "items": [
    {
      "id": 1,
      "name": "Laptop Inspiron 15",
      "price": 899.99,
      "brand": "Dell"
    },
    {
      "id": 3,
      "name": "Laptop XPS 13",
      "price": 1299.99,
      "brand": "Dell"
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalItems": 2,
  "totalPages": 1
}
```

## Comando de desarrollo en detalle

### Anatomia del comando

```bash
docker compose -f compose.yml -f compose.dev.yml up --build
```

El comando tiene 4 partes. Cada una cumple un rol especifico:

```
docker compose                        → Motor de orquestacion
  -f compose.yml                      → Archivo BASE (servicios, red, volumen, healthcheck)
  -f compose.dev.yml                  → Archivo OVERRIDE (sobreescribe config para desarrollo)
  up --build                          → Levanta servicios y reconstruye imagenes
```

### Como funciona la combinacion de archivos (`-f ... -f ...`)

Docker Compose **fusiona** los archivos YAML en orden de izquierda a derecha. El segundo archivo sobreescribe o extiende las claves del primero. Lo que no se menciona en el override, se hereda intacto del base.

**compose.yml** (base) define la arquitectura completa:
```yaml
services:
  api:
    build: .                                  # Usa Dockerfile (multi-stage, Release)
    ports:
      - "${API_PORT:-8080}:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
    depends_on: ...
    restart: unless-stopped

  db:
    image: postgres:16-alpine                 # Se hereda tal cual
    healthcheck: ...                          # Se hereda tal cual
```

**compose.dev.yml** (override) sobreescribe solo lo necesario para desarrollo:
```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile.dev              # SOBREESCRIBE: usa SDK en vez de multi-stage
    working_dir: /src                         # AGREGA: directorio de trabajo
    command: dotnet watch run --urls=...      # SOBREESCRIBE: hot reload en vez de binario
    environment:
      - ASPNETCORE_ENVIRONMENT=Development    # SOBREESCRIBE: fuerza Development
    volumes:
      - .:/src:cached                         # AGREGA: bind mount del codigo fuente
```

> El servicio `db` no aparece en `compose.dev.yml`, por lo tanto se hereda completo desde `compose.yml`.

### Resultado de la fusion

Esto es lo que Docker Compose ejecuta internamente despues de combinar ambos archivos:

```yaml
services:
  api:
    build:
      context: .
      dockerfile: Dockerfile.dev        # ← viene de compose.dev.yml
    working_dir: /src                    # ← viene de compose.dev.yml
    command: dotnet watch run ...        # ← viene de compose.dev.yml
    ports:
      - "8080:8080"                     # ← heredado de compose.yml
    environment:
      - ASPNETCORE_ENVIRONMENT=Development  # ← sobreescrito por compose.dev.yml
      - ConnectionStrings__Default...       # ← sobreescrito por compose.dev.yml
    volumes:
      - .:/src:cached                    # ← viene de compose.dev.yml
    depends_on:
      db:
        condition: service_healthy       # ← heredado de compose.yml
    restart: unless-stopped              # ← heredado de compose.yml

  db:                                    # ← heredado COMPLETO de compose.yml
    image: postgres:16-alpine
    environment: ...
    ports: ...
    volumes: ...
    healthcheck: ...
    restart: unless-stopped
```

Puedes verificar esta fusion en cualquier momento con:

```bash
docker compose -f compose.yml -f compose.dev.yml config
```

### Que implica cada flag

| Flag | Que hace | Que pasa si no lo usas |
|------|----------|------------------------|
| `-f compose.yml` | Carga el archivo base con toda la infraestructura | Docker Compose busca `compose.yml` automaticamente, pero al usar `-f` explicito debes declarar **todos** los archivos |
| `-f compose.dev.yml` | Aplica overrides de desarrollo sobre el base | Se usa el `Dockerfile` de produccion (multi-stage, sin hot reload, sin bind mount) |
| `up` | Crea e inicia los contenedores | - |
| `--build` | Fuerza la reconstruccion de la imagen antes de iniciar | Se reutiliza la imagen cacheada, ignorando cambios en el `Dockerfile` o en el `.csproj` |

### Implicaciones reales en desarrollo

**Hot reload activado** — `dotnet watch run` detecta cambios en archivos `.cs` y recompila automaticamente. No necesitas hacer `docker compose down && up` cada vez que modificas codigo.

**Bind mount del codigo** — El volumen `.:/src:cached` monta tu directorio local dentro del contenedor. Cuando guardas un archivo en tu editor, el contenedor lo ve inmediatamente. La flag `:cached` optimiza el rendimiento en macOS.

**Imagen SDK (no runtime)** — `Dockerfile.dev` usa `dotnet/sdk:8.0` que incluye el compilador. Es mas pesada (~900MB vs ~220MB), pero necesaria para compilar dentro del contenedor.

**Environment forzado** — `compose.dev.yml` fuerza `ASPNETCORE_ENVIRONMENT=Development` independientemente de lo que diga tu `.env`. Esto garantiza que Swagger, detailed errors y sensitive data logging esten activos.

### Comparacion: Desarrollo vs Produccion

| Aspecto | `docker compose up` | `docker compose -f ... -f ... up` |
|---------|---------------------|-------------------------------------|
| Dockerfile | `Dockerfile` (multi-stage) | `Dockerfile.dev` (SDK completo) |
| Imagen base | `aspnet:8.0` (~220MB) | `sdk:8.0` (~900MB) |
| Ejecucion | Binario compilado (Release) | `dotnet watch run` (hot reload) |
| Codigo fuente | Copiado en build, inmutable | Bind mount, cambios en vivo |
| Usuario | `app` (non-root) | root (necesario para watch) |
| Uso | Produccion / staging / CI | Desarrollo local |

## Variables de entorno

| Variable | Descripción | Valor por defecto |
|----------|-------------|-------------------|
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución | `Development` |
| `POSTGRES_USER` | Usuario de PostgreSQL | `postgres` |
| `POSTGRES_PASSWORD` | Password de PostgreSQL | *(sin default, requerido)* |
| `POSTGRES_DB` | Nombre de la base de datos | `myapi` |
| `API_PORT` | Puerto expuesto de la API | `8080` |
| `DB_PORT` | Puerto expuesto de PostgreSQL | `5432` |
