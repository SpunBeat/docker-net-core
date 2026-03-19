using Microsoft.EntityFrameworkCore;
using MyAPI.Data;
using MyAPI.DTOs;
using MyAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);

    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Swagger (only in Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "0");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
    await next();
});

app.UseHttpsRedirection();

// === Product Endpoints ===

app.MapGet("/api/products", async (AppDbContext db) =>
{
    var products = await db.Products.AsNoTracking().ToListAsync();
    return Results.Ok(products.Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Brand)));
})
.WithName("GetProducts")
.WithOpenApi();

app.MapGet("/api/products/search", async (
    AppDbContext db,
    string? name,
    string? brand,
    decimal? minPrice,
    decimal? maxPrice,
    string? sort,
    int page = 1,
    int pageSize = 10) =>
{
    // Validation
    if (page < 1) page = 1;
    pageSize = Math.Clamp(pageSize, 1, 50);

    // Base query (composable)
    var query = db.Products.AsNoTracking().AsQueryable();

    // Filters (only applied when parameter is present)
    if (!string.IsNullOrWhiteSpace(name))
        query = query.Where(p => EF.Functions.ILike(p.Name, $"%{name}%"));

    if (!string.IsNullOrWhiteSpace(brand))
        query = query.Where(p => EF.Functions.ILike(p.Brand, $"%{brand}%"));

    if (minPrice.HasValue)
        query = query.Where(p => p.Price >= minPrice.Value);

    if (maxPrice.HasValue)
        query = query.Where(p => p.Price <= maxPrice.Value);

    // Total count (before pagination, after filters)
    var totalItems = await query.CountAsync();

    // Sorting
    query = sort?.ToLowerInvariant() switch
    {
        "price_asc"  => query.OrderBy(p => p.Price),
        "price_desc" => query.OrderByDescending(p => p.Price),
        "name_asc"   => query.OrderBy(p => p.Name),
        "name_desc"  => query.OrderByDescending(p => p.Name),
        _            => query.OrderBy(p => p.Id)
    };

    // Pagination
    var items = await query
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Brand))
        .ToListAsync();

    var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

    return Results.Ok(new PagedResponse<ProductDto>(items, page, pageSize, totalItems, totalPages));
})
.WithName("SearchProducts")
.WithOpenApi();

app.MapGet("/api/products/{id}", async (int id, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();
    return Results.Ok(new ProductDto(product.Id, product.Name, product.Price, product.Brand));
})
.WithName("GetProduct")
.WithOpenApi();

app.MapPost("/api/products", async (CreateProductDto dto, AppDbContext db) =>
{
    var product = new Product
    {
        Name = dto.Name,
        Price = dto.Price,
        Brand = dto.Brand
    };

    db.Products.Add(product);
    await db.SaveChangesAsync();

    var result = new ProductDto(product.Id, product.Name, product.Price, product.Brand);
    return Results.Created($"/api/products/{product.Id}", result);
})
.WithName("CreateProduct")
.WithOpenApi();

app.MapPut("/api/products/{id}", async (int id, UpdateProductDto dto, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    product.Name = dto.Name;
    product.Price = dto.Price;
    product.Brand = dto.Brand;

    await db.SaveChangesAsync();
    return Results.Ok(new ProductDto(product.Id, product.Name, product.Price, product.Brand));
})
.WithName("UpdateProduct")
.WithOpenApi();

app.MapDelete("/api/products/{id}", async (int id, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.WithName("DeleteProduct")
.WithOpenApi();

app.Run();
