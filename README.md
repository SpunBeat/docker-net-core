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

```bash
docker compose up --build
```

Esto levanta:
- **API** en `http://localhost:8080` (configurable via `API_PORT`)
- **PostgreSQL** en `localhost:5432` (configurable via `DB_PORT`)

Las migraciones se aplican automáticamente al iniciar la API.

### 3. Swagger UI

Accede a la documentación interactiva en:

```
http://localhost:8080/swagger
```

> Swagger solo esta disponible cuando `ASPNETCORE_ENVIRONMENT=Development`.

### 4. Detener el proyecto

```bash
docker compose down
```

Para eliminar también los datos de PostgreSQL:

```bash
docker compose down -v
```

## Endpoints

| Método | Ruta                  | Descripción           |
|--------|-----------------------|-----------------------|
| GET    | /api/products         | Listar productos      |
| GET    | /api/products/{id}    | Obtener producto      |
| POST   | /api/products         | Crear producto        |
| PUT    | /api/products/{id}    | Actualizar producto   |
| DELETE | /api/products/{id}    | Eliminar producto     |

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

## Variables de entorno

| Variable | Descripción | Valor por defecto |
|----------|-------------|-------------------|
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución | `Development` |
| `POSTGRES_USER` | Usuario de PostgreSQL | `postgres` |
| `POSTGRES_PASSWORD` | Password de PostgreSQL | *(sin default, requerido)* |
| `POSTGRES_DB` | Nombre de la base de datos | `myapi` |
| `API_PORT` | Puerto expuesto de la API | `8080` |
| `DB_PORT` | Puerto expuesto de PostgreSQL | `5432` |
