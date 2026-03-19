using Microsoft.EntityFrameworkCore;
using MyAPI.Data;
using MyAPI.DTOs;
using MyAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// === Product Endpoints ===

app.MapGet("/api/products", async (AppDbContext db) =>
{
    var products = await db.Products.AsNoTracking().ToListAsync();
    return Results.Ok(products.Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Brand)));
})
.WithName("GetProducts")
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
