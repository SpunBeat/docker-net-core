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

### Levantar el proyecto

```bash
docker compose up --build
```

Esto levanta:
- **API** en `http://localhost:8080`
- **PostgreSQL** en `localhost:5432`

Las migraciones se aplican automáticamente al iniciar la API.

### Swagger UI

Accede a la documentación interactiva en:

```
http://localhost:8080/swagger
```

### Detener el proyecto

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

| Variable | Descripción |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Cadena de conexión a PostgreSQL |
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución (Development/Production) |
